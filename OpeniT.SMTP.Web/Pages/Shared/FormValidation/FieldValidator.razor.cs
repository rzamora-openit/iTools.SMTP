
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.ObjectPool;
using OpeniT.SMTP.Web.Methods;

namespace OpeniT.SMTP.Web.Pages.Shared.Admin
{
    public partial class FieldValidator<TValue> : BaseFieldValidator, IDisposable
    {
        private Expression<Func<TValue>>? _previousFieldAccessor;
        private FieldIdentifier _fieldIdentifier;

        private bool _isDisposed;

        private ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> propertyInfoCache = new ConcurrentDictionary<(Type, string), PropertyInfo?>();
        
        private Dictionary<string, Dictionary<TValue, ValidationResult>> additionalValidationsResultToHistoryMap = new Dictionary<string, Dictionary<TValue, ValidationResult>>();

        public ElementReference Ref;
        public string ClassMapper => $"validation-popover {Class} {(this.GetValidationMessage().Any() != true || CurrentEditContext?.IsModified(_fieldIdentifier) != true ? "d-none" : string.Empty)}";

        [CascadingParameter] FormValidator FormValidator { get; set; } = default!;
        [CascadingParameter] EditContext CurrentEditContext { get; set; } = default!;

        [Parameter] public string Class { get; set; }
        [Parameter] public string Style { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? Attributes { get; set; }
        [Parameter] public Expression<Func<TValue>>? For { get; set; }
        [Parameter] public RenderFragment<IEnumerable<string>> MessagesTemplate { get; set; }
        [Parameter] public IEnumerable<ValidationAttribute> AdditionalValidationAttributes { get; set; }
        [Parameter] public IEnumerable<Func<TValue, Task<ValidationResult>>> AdditionalValidators { get; set; }
        [Parameter] public bool SaveValidationResults { get; set; } = true;

        protected override void OnParametersSet()
        {
            if (FormValidator == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                    $"of type {nameof(FormValidator)}.");
            }

            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                    $"of type {nameof(EditContext)}. For example, you can use {GetType()} inside " +
                    $"an {nameof(EditForm)}.");
            }

            if (For == null) // Not possible except if you manually specify T
            {
                throw new InvalidOperationException($"{GetType()} requires a value for the " +
                    $"{nameof(For)} parameter.");
            }
            else if (For != _previousFieldAccessor)
            {
                FormValidator.UnsubscribeFieldValidator(_fieldIdentifier);

                _fieldIdentifier = FieldIdentifier.Create(For);
                _previousFieldAccessor = For;

                FormValidator.SubscribeFieldValidator(_fieldIdentifier, this);
            }
        }

        public override IEnumerable<string> GetValidationMessage()
        {
            foreach (var validationMessage in FormValidator.ValidationMessageStore[_fieldIdentifier])
            {
                yield return validationMessage;
            }
        }

        public override async Task<bool> Validate(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(150);
            if (!cancellationToken.IsCancellationRequested)
            {
                var validationResults = new List<ValidationResult>();

                if (this.TryGetValidatableProperty(_fieldIdentifier, out var propertyInfo))
                {
                    var propertyValue = propertyInfo.GetValue(_fieldIdentifier.Model);
                    var validationContext = new ValidationContext(_fieldIdentifier.Model)
                    {
                        MemberName = propertyInfo.Name
                    };

                    var dataAnnotationValidationResults = new List<ValidationResult>();
                    Validator.TryValidateProperty(propertyValue, validationContext, dataAnnotationValidationResults);
                    validationResults.AddRange(dataAnnotationValidationResults);

                    if (AdditionalValidationAttributes?.Any() == true)
                    {
                        var additionalValidationAttributesValidationResults = new List<ValidationResult>();
                        Validator.TryValidateValue(propertyValue, validationContext, additionalValidationAttributesValidationResults, AdditionalValidationAttributes);
                        validationResults.AddRange(additionalValidationAttributesValidationResults);
                    }
                }

                if (AdditionalValidators?.Any() == true)
                {
                    var additionalValidationTasks = new List<Task<ValidationResult>>();
                    foreach (var additionalValidator in AdditionalValidators.Distinct())
                    {
                        Task<ValidationResult> validationTask;
                        Func<TValue> fieldMethod = For.Compile();
                        TValue fieldValue = fieldMethod();

                        if (SaveValidationResults)
                        {
                            validationTask = this.FetchResultFromAdditionalValidatorHistory(additionalValidator);
                        }
                        else
                        {
                            validationTask = additionalValidator.Invoke(fieldValue);
                        }

                        additionalValidationTasks.Add(validationTask);
                    }

                    validationResults.AddRange((await Task.WhenAll(additionalValidationTasks)).Where(vr => vr != ValidationResult.Success));
                }

                FormValidator.ValidationMessageStore.Clear(_fieldIdentifier);
                FormValidator.ValidationMessageStore.Add(_fieldIdentifier, validationResults.Select(vr => vr?.ErrorMessage));

                if (!CurrentEditContext.IsModified(_fieldIdentifier))
                {
                    CurrentEditContext.NotifyFieldChanged(_fieldIdentifier);
                }
                CurrentEditContext.NotifyValidationStateChanged();

                return validationResults.Any();
            }

            return false;
        }

        private async Task<ValidationResult> FetchResultFromAdditionalValidatorHistory(Func<TValue, Task<ValidationResult>> additionalValidator)
        {
            ValidationResult validationResult = ValidationResult.Success;
            Func<TValue> fieldMethod = For.Compile();
            TValue fieldValue = fieldMethod();

            var additionalValidationResultHistory = additionalValidationsResultToHistoryMap.GetOrCreate(additionalValidator.Method.GetUniqueName());
            if (fieldValue != null && additionalValidationResultHistory.TryGetValue(fieldValue, out var _validationResult))
            {
                validationResult = _validationResult;
            }
            else
            {
                validationResult = await additionalValidator.Invoke(fieldValue);
                if (fieldValue != null)
                {
                    additionalValidationResultHistory.TryAdd(fieldValue, validationResult);
                }
            }

            return validationResult;
        }

        private bool TryGetValidatableProperty(in FieldIdentifier fieldIdentifier, out PropertyInfo? propertyInfo)
        {
            var cacheKey = (ModelType: fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
            if (!propertyInfoCache.TryGetValue(cacheKey, out propertyInfo))
            {
                // DataAnnotations only validates public properties, so that's all we'll look for
                // If we can't find it, cache 'null' so we don't have to try again next time
                propertyInfo = cacheKey.ModelType.GetProperty(cacheKey.FieldName);

                // No need to lock, because it doesn't matter if we write the same value twice
                propertyInfoCache[cacheKey] = propertyInfo;
            }

            return propertyInfo != null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                FormValidator.UnsubscribeFieldValidator(_fieldIdentifier);
            }

            _isDisposed = true;
        }
    }
}