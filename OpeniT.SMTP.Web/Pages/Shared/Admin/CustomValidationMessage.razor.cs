
using System;
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
    public partial class CustomValidationMessage<TValue> : ComponentBase, IDisposable
    {
        private EditContext? _previousEditContext;
        private Expression<Func<TValue>>? _previousFieldAccessor;
        private HashSet<Func<object, Task<ValidationResult>>>? _previousRemoteValidatorFuncs;

        private readonly EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;
        private readonly EventHandler<ValidationRequestedEventArgs>? _validationRequestedHandler;
        private FieldIdentifier _fieldIdentifier;

        private IEnumerable<string> messages => validationMessages.Concat(remoteValidationMessages);
        private IEnumerable<string> validationMessages => CurrentEditContext?.GetValidationMessages(_fieldIdentifier) ?? Enumerable.Empty<string>();
        private IEnumerable<string> remoteValidationMessages => CurrentCustomRemoteValidator?.GetRemoteValidationMessages(_fieldIdentifier) ?? Enumerable.Empty<string>();

        [CascadingParameter] EditContext CurrentEditContext { get; set; } = default!;

        [CascadingParameter] CustomRemoteValidator CurrentCustomRemoteValidator { get; set; } = default!;

        [Parameter] public Expression<Func<TValue>>? For { get; set; }

        [Parameter] public RenderFragment<string> MessageTemplate { get; set; }

        [Parameter] public string Class { get; set; }

        [Parameter] public string Style { get; set; }

        [Parameter] public HashSet<Func<object, Task<ValidationResult>>> RemoteValidatorFuncs { get; set; } = new HashSet<Func<object, Task<ValidationResult>>>();

        [Parameter] public bool SaveValidationResults { get; set; } = true;

        public ElementReference Ref;

        public CustomValidationMessage()
        {
            _validationRequestedHandler = (sender, eventArgs) =>
            {
                CurrentEditContext.NotifyFieldChanged(_fieldIdentifier);
                StateHasChanged();
            };
            _validationStateChangedHandler = (sender, eventArgs) =>
            {
                StateHasChanged();
            };
        }

		protected override void OnAfterRender(bool firstRender)
		{
            if (firstRender)
            {
                CurrentCustomRemoteValidator?.AddCustomValidationMessageElementReferences(Ref);
            }
        }

		protected override void OnParametersSet()
        {
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
                _fieldIdentifier = FieldIdentifier.Create(For);
                _previousFieldAccessor = For;
            }

            if (CurrentEditContext != _previousEditContext)
            {
                this.DetachPreviousEditContextEventListeners();
                CurrentEditContext.OnValidationStateChanged += _validationStateChangedHandler;
                CurrentEditContext.OnValidationRequested += _validationRequestedHandler;
                _previousEditContext = CurrentEditContext;
            }

            if (!(RemoteValidatorFuncs?.SetEquals(_previousRemoteValidatorFuncs ?? new HashSet<Func<object, Task<ValidationResult>>>())).GetValueOrDefault())
            {
                if (RemoteValidatorFuncs != null && RemoteValidatorFuncs.Any())
                {
                    CurrentCustomRemoteValidator?.AddFieldRemoteValidatorFunc(new KeyValuePair<FieldIdentifier, HashSet<Func<object, Task<ValidationResult>>>>(_fieldIdentifier, RemoteValidatorFuncs));
                }

                _previousRemoteValidatorFuncs = RemoteValidatorFuncs;
            }

            CurrentCustomRemoteValidator?.AddFieldSaveValidationResults(new KeyValuePair<FieldIdentifier, bool>(_fieldIdentifier, SaveValidationResults));
        }

        protected virtual void Dispose(bool disposing) { }

        void IDisposable.Dispose()
        {
            this.DetachPreviousEditContextEventListeners();
            CurrentEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            CurrentEditContext.OnValidationRequested -= _validationRequestedHandler;
            CurrentCustomRemoteValidator?.DisposeFieldRemoteValidator(_fieldIdentifier);
            Dispose(disposing: true);
        }

        private void DetachPreviousEditContextEventListeners()
        {
            if (_previousEditContext != null)
            {
                _previousEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
                _previousEditContext.OnValidationRequested -= _validationRequestedHandler;
            }
        }
    }
}