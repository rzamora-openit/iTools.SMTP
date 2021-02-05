using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Newtonsoft.Json.Schema;
using OpeniT.SMTP.Web.Methods;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared.Admin
{
    public partial class FormValidator : ComponentBase, IDisposable
    {
        private readonly Func<Task> _handleSubmitDelegate;
        private EditContext? _editContext;
        private EditContext? _previousEditContext;

        private readonly EventHandler<FieldChangedEventArgs>? _onFieldChangedHandler;
        private readonly EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;

        private Dictionary<FieldIdentifier, BaseFieldValidator> fieldIdentifierToFieldValidatorMap = new Dictionary<FieldIdentifier, BaseFieldValidator>();
        private Dictionary<FieldIdentifier, CancellationTokenSource> fieldIdentifierToValidationCtsMap = new Dictionary<FieldIdentifier, CancellationTokenSource>();

        private bool _hasSetEditContextExplicitly;
        private bool _isDisposed;

        public ValidationMessageStore ValidationMessageStore;

        public FormValidator()
        {
            _handleSubmitDelegate = HandleSubmitAsync;

            _onFieldChangedHandler = async (sender, eventArgs) =>
            {
                if (fieldIdentifierToFieldValidatorMap.TryGetValue(eventArgs.FieldIdentifier, out var fieldValidator))
                {
                    fieldIdentifierToValidationCtsMap.GetValueOrDefault(eventArgs.FieldIdentifier)?.Cancel();
                    fieldIdentifierToValidationCtsMap[eventArgs.FieldIdentifier] = new CancellationTokenSource();

                    var cancellationToken = fieldIdentifierToValidationCtsMap[eventArgs.FieldIdentifier].Token;
                    await fieldValidator.Validate(cancellationToken);
                }
            };
            _validationStateChangedHandler = (sender, eventArgs) =>
            {
                StateHasChanged();
            };
        }

        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? Attributes { get; set; }
        [Parameter]
        public EditContext? EditContext
		{
            get => _editContext;
            set
			{
                _editContext = value;
                _hasSetEditContextExplicitly = value != null;
            }
        }
        [Parameter] public object? Model { get; set; }
        [Parameter] public RenderFragment<EditContext>? ChildContent { get; set; }
        [Parameter] public EventCallback<EditContext> OnSubmit { get; set; }
        [Parameter] public EventCallback<EditContext> OnValidSubmit { get; set; }
        [Parameter] public EventCallback<EditContext> OnInvalidSubmit { get; set; }

        protected override void OnParametersSet()
        {
            if (_hasSetEditContextExplicitly && Model != null)
            {
                throw new InvalidOperationException($"{nameof(EditForm)} requires a {nameof(Model)} " +
                    $"parameter, or an {nameof(EditContext)} parameter, but not both.");
            }
            else if (!_hasSetEditContextExplicitly && Model == null)
            {
                throw new InvalidOperationException($"{nameof(EditForm)} requires either a {nameof(Model)} " +
                    $"parameter, or an {nameof(EditContext)} parameter, please provide one of these.");
            }

            if (OnSubmit.HasDelegate && (OnValidSubmit.HasDelegate || OnInvalidSubmit.HasDelegate))
            {
                throw new InvalidOperationException($"When supplying an {nameof(OnSubmit)} parameter to " +
                    $"{nameof(EditForm)}, do not also supply {nameof(OnValidSubmit)} or {nameof(OnInvalidSubmit)}.");
            }

            if (Model != null && Model != _editContext?.Model)
            {
                _editContext = new EditContext(Model!);
            }

            if (_previousEditContext != _editContext)
            {
                this.DetachPreviousEditContextEventHandlers();

                _editContext.OnFieldChanged += _onFieldChangedHandler;
                _editContext.OnValidationStateChanged += _validationStateChangedHandler;

                ValidationMessageStore = new ValidationMessageStore(_editContext);

                _previousEditContext = _editContext;
            }
        }

        private async Task HandleSubmitAsync()
        {
            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(_editContext);
            }
            else
            {
                var isValid = await this.Validate();

                if (isValid && OnValidSubmit.HasDelegate)
                {
                    await OnValidSubmit.InvokeAsync(_editContext);
                }

                if (!isValid && OnInvalidSubmit.HasDelegate)
                {
                    await OnInvalidSubmit.InvokeAsync(_editContext);
                }
            }
        }

        public void SubscribeFieldValidator(FieldIdentifier fileIdentifier, BaseFieldValidator fieldValidator)
        {
            if (fileIdentifier.FieldName != null)
            {
                fieldIdentifierToFieldValidatorMap.Add(fileIdentifier, fieldValidator);
            }
        }

        public void UnsubscribeFieldValidator(FieldIdentifier fileIdentifier)
        {
            if (fileIdentifier.FieldName != null)
            {
                fieldIdentifierToFieldValidatorMap.Remove(fileIdentifier);
            }
        }

        public IEnumerable<string> GetValidationMessages()
        {
            foreach (var fieldIdentifier in fieldIdentifierToFieldValidatorMap.Keys)
            {
                foreach (var validationMessage in ValidationMessageStore[fieldIdentifier])
                {
                    yield return validationMessage;
                }
            }
        }

        public async Task<bool> Validate(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool isValid = true;

                if (fieldIdentifierToFieldValidatorMap.Any())
                {
                    var validationTasks = new List<Task<bool>>();
                    foreach (var fieldValidator in fieldIdentifierToFieldValidatorMap.Values)
					{
                        validationTasks.Add(fieldValidator.Validate(cancellationToken));
                    }
                    
                    var validationResults = await Task.WhenAll(validationTasks);
                    isValid &= validationResults.All(vr => vr);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
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
                _editContext.OnFieldChanged -= _onFieldChangedHandler;
                _editContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }

            _isDisposed = true;
        }

        private void DetachPreviousEditContextEventHandlers()
		{
            if (_previousEditContext != null)
            {
                _previousEditContext.OnFieldChanged -= _onFieldChangedHandler;
                _previousEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }
        }
    }
}
