using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
	public partial class CustomRemoteValidator : ComponentBase, IDisposable
	{
		[Inject] private IJSRuntime jsRuntime { get; set; }

		[CascadingParameter] public EditContext CurrentEditContext { get; set; }
		[Parameter] public RenderFragment ChildContent { get; set; }

		public bool IsValidating = false;

		private EditContext previousEditContext;
		private readonly EventHandler<FieldChangedEventArgs>? _onFieldChangedHandler;

		private readonly List<ElementReference> customValidationMessageElementReferences = new List<ElementReference>();
		private readonly Dictionary<FieldIdentifier, bool> fieldSaveValidationResults = new Dictionary<FieldIdentifier, bool>();
		private readonly Dictionary<FieldIdentifier, List<string>> _fieldRemoteValidationMessages = new Dictionary<FieldIdentifier, List<string>>();
		private readonly Dictionary<FieldIdentifier, Dictionary<string, Dictionary<object, ValidationResult>>> _fieldRemoteValidationResultHistories = new Dictionary<FieldIdentifier, Dictionary<string, Dictionary<object, ValidationResult>>>();
		private readonly Dictionary<FieldIdentifier, HashSet<Func<object, Task<ValidationResult>>>> _fieldRemoteValidatorFuncs = new Dictionary<FieldIdentifier, HashSet<Func<object, Task<ValidationResult>>>>();
		private readonly Dictionary<FieldIdentifier, Dictionary<string, CancellationTokenSource>> _fieldDebounceCTS = new Dictionary<FieldIdentifier, Dictionary<string, CancellationTokenSource>>();

		public CustomRemoteValidator()
		{
			_onFieldChangedHandler = async (sender, eventArgs) =>
			{
				if (_fieldRemoteValidatorFuncs.TryGetValue(eventArgs.FieldIdentifier, out var RemoteValidatorFuncs))
				{
					if (RemoteValidatorFuncs.Any())
					{
						await this.RemoteValidateField(eventArgs.FieldIdentifier, RemoteValidatorFuncs);
					}
				}
			};
		}

		protected override void OnParametersSet()
		{
			if (CurrentEditContext != previousEditContext)
			{
				this.DetachPreviousEditContextEventListeners();
				CurrentEditContext.OnFieldChanged += _onFieldChangedHandler;
				previousEditContext = CurrentEditContext;
			}
		}

		public async Task<IEnumerable<string>> RemoteValidateField(FieldIdentifier fieldIdentifier, IEnumerable<Func<object, Task<ValidationResult>>> remoteValidatorFuncs)
		{
			List<string> newRemoteValidationMessages = null;

			if (remoteValidatorFuncs.Any())
			{
				newRemoteValidationMessages = new List<string>();
				foreach (var remoteValidatorFunc in remoteValidatorFuncs.ToList())
				{
					ValidationResult validationResult;
					if (fieldSaveValidationResults.GetValueOrDefault(fieldIdentifier))
					{
						validationResult = await this.UpdateFieldRemoteValidationHistory(fieldIdentifier, remoteValidatorFunc);
					}
					else
					{
						validationResult = await this.DebounceRemoteValidatorFunc(fieldIdentifier, remoteValidatorFunc);
					}

					if (validationResult != ValidationResult.Success && !(validationResult?.ErrorMessage.Equals("Cancelled")).GetValueOrDefault())
					{
						newRemoteValidationMessages.Add(validationResult.ErrorMessage);
					}
				}

				_fieldRemoteValidationMessages[fieldIdentifier] = newRemoteValidationMessages;
				CurrentEditContext.NotifyValidationStateChanged();
			}

			return newRemoteValidationMessages;
		}

		public void AddCustomValidationMessageElementReferences(ElementReference Ref)
		{
			customValidationMessageElementReferences.Add(Ref);
		}

		public void AddFieldRemoteValidatorFunc(KeyValuePair<FieldIdentifier, HashSet<Func<object, Task<ValidationResult>>>> fieldRemoteValidatorFunc)
		{
			_fieldRemoteValidatorFuncs[fieldRemoteValidatorFunc.Key] = fieldRemoteValidatorFunc.Value;
		}

		public void DisposeFieldRemoteValidator(FieldIdentifier fieldIdentifier)
		{
			_fieldRemoteValidatorFuncs.Remove(fieldIdentifier);
			_fieldRemoteValidationMessages.Remove(fieldIdentifier);
			_fieldRemoteValidationResultHistories.Remove(fieldIdentifier);
		}

		public void AddFieldSaveValidationResults(KeyValuePair<FieldIdentifier, bool> fieldSaveValidationResult)
		{
			fieldSaveValidationResults[fieldSaveValidationResult.Key] = fieldSaveValidationResult.Value;
		}

		public IEnumerable<string> GetRemoteValidationMessages()
		{
			foreach (var remoteValidationMessages in _fieldRemoteValidationMessages)
			{
				foreach (var message in remoteValidationMessages.Value)
				{
					yield return message;
				}
			}
		}

		public IEnumerable<string> GetRemoteValidationMessages(FieldIdentifier fieldIdentifier)
		{
			return _fieldRemoteValidationMessages.GetValueOrDefault(fieldIdentifier);
		}

		public IEnumerable<string> GetAllValidationMessages()
		{
			return CurrentEditContext.GetValidationMessages().Concat(this.GetRemoteValidationMessages() ?? Enumerable.Empty<string>());
		}

		public IEnumerable<string> GetAllValidationMessages(FieldIdentifier fieldIdentifier)
		{
			return CurrentEditContext.GetValidationMessages(fieldIdentifier).Concat(_fieldRemoteValidationMessages.GetValueOrDefault(fieldIdentifier) ?? Enumerable.Empty<string>());
		}

		private async Task<ValidationResult> UpdateFieldRemoteValidationHistory(FieldIdentifier fieldIdentifier, Func<object, Task<ValidationResult>> remoteValidatorFunc)
		{
			ValidationResult validationResult = ValidationResult.Success;
			var fieldValue = fieldIdentifier.Model.GetObjectValue(fieldIdentifier.FieldName);

			var remoteValidationResultHistories = _fieldRemoteValidationResultHistories.GetOrCreate(fieldIdentifier);
			var remoteValidationResultHistory = remoteValidationResultHistories.GetOrCreate(remoteValidatorFunc.Method.GetUniqueName());

			var _validationResult = remoteValidationResultHistory.Where(h => h.Key.Equals(fieldValue ?? fieldValue.InitializeIfNull())).FirstOrDefault();
			if (_validationResult.Key != null)
			{
				validationResult = _validationResult.Value;
			}
			else
			{
				validationResult = await this.DebounceRemoteValidatorFunc(fieldIdentifier, remoteValidatorFunc);
				if (validationResult == ValidationResult.Success || !(validationResult?.ErrorMessage.Equals("Cancelled")).GetValueOrDefault())
				{
					remoteValidationResultHistory.TryAdd(fieldValue ?? fieldValue.InitializeIfNull(), validationResult);
				}
			}

			return validationResult;
		}

		private async Task<ValidationResult> DebounceRemoteValidatorFunc(FieldIdentifier fieldIdentifier, Func<object, Task<ValidationResult>> remoteValidatorFunc, int DebounceMilliseconds = 150)
		{
			var debounceCTS = _fieldDebounceCTS.GetOrCreate(fieldIdentifier);
			debounceCTS[remoteValidatorFunc.Method.GetUniqueName()] = debounceCTS.GetValueOrDefault(remoteValidatorFunc.Method.GetUniqueName()) ?? null;
			debounceCTS[remoteValidatorFunc.Method.GetUniqueName()]?.Cancel();
			debounceCTS[remoteValidatorFunc.Method.GetUniqueName()] = new CancellationTokenSource();
			var cancellationToken = debounceCTS[remoteValidatorFunc.Method.GetUniqueName()].Token;

			await Task.Delay(DebounceMilliseconds);
			if (!cancellationToken.IsCancellationRequested)
			{
				debounceCTS[remoteValidatorFunc.Method.GetUniqueName()] = null;
				var validationResult = await remoteValidatorFunc(fieldIdentifier.Model.GetObjectValue(fieldIdentifier.FieldName));
				return validationResult;
			}
			else
			{
				return new ValidationResult("Cancelled");
			}
		}

		public async Task<bool> Validate()
		{
			IsValidating = true;
			bool isValid = false;

			try
			{
				isValid = CurrentEditContext.Validate();

				if (isValid)
				{
					if (_fieldRemoteValidatorFuncs.Values.SelectMany(f => f).Any())
					{
						var tasks = new List<Task<IEnumerable<string>>>();
						foreach (var fieldRemoteValidatorFunc in _fieldRemoteValidatorFuncs)
						{
							tasks.Add(this.RemoteValidateField(fieldRemoteValidatorFunc.Key, fieldRemoteValidatorFunc.Value));
						}

						await Task.WhenAll(tasks);
					}

					isValid = !this.GetRemoteValidationMessages().Any();
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				if (!isValid)
				{
					await InvokeAsync(StateHasChanged);
					await JSIntropMethods.ScrollToHighest(jsRuntime, customValidationMessageElementReferences);
				}

				IsValidating = false;
			}

			return isValid;
		}

		protected virtual void Dispose(bool disposing) { }

		void IDisposable.Dispose()
		{
			this.DetachPreviousEditContextEventListeners();
			CurrentEditContext.OnFieldChanged -= _onFieldChangedHandler;
			Dispose(disposing: true);
		}

		private void DetachPreviousEditContextEventListeners()
		{
			if (previousEditContext != null)
			{
				previousEditContext.OnFieldChanged -= _onFieldChangedHandler;
			}
		}
	}
}
