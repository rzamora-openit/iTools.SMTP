using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.Pages.Shared;
using OpeniT.SMTP.Web.Pages.Shared.Admin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer, User-Internal")]
	public partial class CopyMail : ComponentBase
	{
		[Inject] private IMatToaster matToaster { get; set; }
		[Inject] private NavigationManager navigationManager { get; set; }
		[Inject] private SMTPMethods smtpMethods { get; set; }

		[CascadingParameter] public ManageMails ManageMails { get; set; }

		[Parameter] public bool Shown { get; set; }
		[Parameter] public SmtpMail Mail { get; set; } = new SmtpMail();


		private SmtpMail previousMail = new SmtpMail();

		private GrapesjsEditor grapesjsEditor;
		private CancellationTokenSource grapesjsEditorValueCts;

		private EditContext editContext;
		private CustomRemoteValidator customRemoteValidator;
		private SmtpMail model;
		private string mailFrom;
		private string mailTo;
		private string mailCC;

		private MatTextField<string> MailFromTextField;
		private MatTextField<string> MailToTextField;
		private MatTextField<string> MailCCTextField;
		private Menu MailAddressesMenu;
		private string mailAddressesSearchText =>
			ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailFromTextField?.Ref)
			? model?.From?.Address
			: ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailToTextField?.Ref)
				? model?.To?.LastOrDefault()?.Address
				: ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailCCTextField?.Ref)
					? model?.CC?.LastOrDefault()?.Address
					: null;

		private bool isBusy = false;

		protected override void OnInitialized()
		{
			try
			{
				model = new SmtpMail();
				model.From = new SmtpMailAddress();
				model.To = new List<SmtpMailAddress>();
				model.CC = new List<SmtpMailAddress>();
				mailFrom = null;
				mailTo = null;
				mailCC = null;

				editContext = new EditContext(model);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		protected override async Task OnParametersSetAsync()
		{
			if (previousMail != Mail)
			{
				previousMail = Mail;

				model = await ManageMails.GetMailCopy(Mail) ?? new SmtpMail();
				model.From = model.From ?? new SmtpMailAddress();
				model.To = model.To ?? new List<SmtpMailAddress>();
				model.CC = model.CC ?? new List<SmtpMailAddress>();
				mailFrom = model?.From.Address;
				mailTo = string.Join(", ", model?.To?.Select(to => to?.Address) ?? Enumerable.Empty<string>());
				mailCC = string.Join(", ", model?.CC?.Select(cc => cc?.Address) ?? Enumerable.Empty<string>());
				editContext = new EditContext(model);
			}
		}

		private async Task UpdateMailAddressesMenuAnchor(ElementReference element)
		{
			await MailAddressesMenu.CloseAsync();
			await MailAddressesMenu.OpenAsync(element);
		}

		private void MailAddressClicked(string mailAddress)
		{
			if (ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailFromTextField?.Ref))
			{
				mailFrom = mailAddress;
				this.MailFromChanged(mailFrom);
			}
			else if (ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailToTextField?.Ref))
			{
				mailTo = mailTo ?? string.Empty;

				var mailToSubstr = mailTo.Substring(0, mailTo.LastIndexOf(',') + 1);
				mailTo = $"{mailToSubstr}{(string.IsNullOrWhiteSpace(mailToSubstr) ? string.Empty : " ")}{mailAddress}";
				this.MailToChanged(mailTo);
			}
			else if (ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailCCTextField?.Ref))
			{
				mailCC = mailCC ?? string.Empty;

				var mailCCSubstr = mailCC.Substring(0, mailCC.LastIndexOf(',') + 1);
				mailCC = $"{mailCCSubstr}{(string.IsNullOrWhiteSpace(mailCCSubstr) ? string.Empty : " ")}{mailAddress}";
				this.MailCcChanged(mailCC);
			}
		}

		private void MailFromChanged(string value)
		{
			model.From = model.From ?? new SmtpMailAddress();
			model.From.Address = value;
			editContext.NotifyFieldChanged(() => model.From.Address);
		}

		private void MailToChanged(string value)
		{
			model.To = string.IsNullOrWhiteSpace(value) ? new List<SmtpMailAddress>() : Regex.Replace(value, @"\s+", "").Split(",").Where(to => !string.IsNullOrWhiteSpace(to)).Select(to => new SmtpMailAddress() { Address = to }).ToList();
			editContext.NotifyFieldChanged(() => model.To);
		}

		private void MailCcChanged(string value)
		{
			model.CC = string.IsNullOrWhiteSpace(value) ? new List<SmtpMailAddress>() : Regex.Replace(value, @"\s+", "").Split(",").Where(cc => !string.IsNullOrWhiteSpace(cc)).Select(cc => new SmtpMailAddress() { Address = cc }).ToList();
			editContext.NotifyFieldChanged(() => model.CC);
		}

		private HashSet<Func<object, Task<ValidationResult>>> fromValidators =>
			new HashSet<Func<object, Task<ValidationResult>>>() { FromAddressHasValue, FromAddressIsValid };
		private Task<ValidationResult> FromAddressHasValue(object value)
		{
			var fromAddress = value?.ToString();
			if (string.IsNullOrWhiteSpace(fromAddress))
			{
				return Task.FromResult(new ValidationResult("From field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}
		private Task<ValidationResult> FromAddressIsValid(object value)
		{
			var fromAddress = value?.ToString();
			if (!string.IsNullOrWhiteSpace(fromAddress) && !(new EmailAddressAttribute().IsValid(fromAddress)))
			{
				return Task.FromResult(new ValidationResult($"{value} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<object, Task<ValidationResult>>> toValidators =>
			new HashSet<Func<object, Task<ValidationResult>>>() { ToHasValue, ToIsValid };
		private Task<ValidationResult> ToHasValue(object value)
		{
			var to = value as ICollection<SmtpMailAddress>;
			if (to?.Any() != true)
			{
				return Task.FromResult(new ValidationResult("To field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}
		private Task<ValidationResult> ToIsValid(object value)
		{
			var to = value as ICollection<SmtpMailAddress>;
			var notValidAddress = to?.FirstOrDefault(t => !(new EmailAddressAttribute().IsValid(t?.Address)));
			if (notValidAddress != null)
			{
				return Task.FromResult(new ValidationResult($"{notValidAddress?.Address} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<object, Task<ValidationResult>>> ccValidators =>
			new HashSet<Func<object, Task<ValidationResult>>>() { CcIsValid };
		private Task<ValidationResult> CcIsValid(object value)
		{
			var cc = value as ICollection<SmtpMailAddress>;
			var notValidAddress = cc?.FirstOrDefault(c => !(new EmailAddressAttribute().IsValid(c?.Address)));
			if (notValidAddress != null)
			{
				return Task.FromResult(new ValidationResult($"{notValidAddress?.Address} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<object, Task<ValidationResult>>> subjectValidators =>
			new HashSet<Func<object, Task<ValidationResult>>>() { SubjectHasValue };
		private Task<ValidationResult> SubjectHasValue(object value)
		{
			var subject = value?.ToString();
			if (string.IsNullOrWhiteSpace(subject))
			{
				return Task.FromResult(new ValidationResult("Subject field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<object, Task<ValidationResult>>> bodyValidators =>
			new HashSet<Func<object, Task<ValidationResult>>>() { BodyHasValue };
		private Task<ValidationResult> BodyHasValue(object value)
		{
			var body = value?.ToString();
			if (string.IsNullOrWhiteSpace(body))
			{
				return Task.FromResult(new ValidationResult("Body field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private async Task Save()
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				grapesjsEditorValueCts?.Cancel();
				grapesjsEditorValueCts = new CancellationTokenSource();
				model.Body = await grapesjsEditor.GetValue(grapesjsEditorValueCts);

				if (await customRemoteValidator.Validate())
				{
					if (await ManageMails.AddMail(model))
					{
						await smtpMethods.SendMail(model);

						matToaster.Add(message: $"Successfully Sent Mail", type: MatToastType.Primary, icon: "notifications");
						navigationManager.NavigateTo(ManageMails.CurrentFiltersUri);
					}
				}
				else
				{
					matToaster.Add(message: $"Please fill out correctly to save.", type: MatToastType.Primary, icon: "notifications");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				isBusy = false;
				StateHasChanged();
			}
		}
	}
}