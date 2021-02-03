using Grapesjs;
using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Helpers;
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
	[Authorize(Roles = "Administrator, Developer")]
	public partial class CopyMail : ComponentBase, IDisposable
	{
		[Inject] private IDataRepository dataRepository { get; set; }
		[Inject] private IMatToaster matToaster { get; set; }
		[Inject] private AzureHelper azureHelper { get; set; }
		[Inject] private SMTPMethods smtpMethods { get; set; }

		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public EventCallback<SmtpMail> OnValidSave { get; set; }
		[Parameter] public Guid MailGuid { get; set; }

		private bool isBusy = false;
		private bool isDisposed = false;
		private bool previousIsOpen;

		private Guid previousMailGuid;
		private SmtpMail Mail;

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

		#region SiteValues
		private List<AzureProfile> Profiles = new List<AzureProfile>();
		private List<string> MailAddresses = new List<string>();
		#endregion SiteValues

		private CancellationTokenSource loadDataCts;
		private CancellationTokenSource loadSiteValuesCts;

		protected override async Task OnInitializedAsync()
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				previousIsOpen = !IsOpen;

				model = new SmtpMail();
				model.From = new SmtpMailAddress();
				model.To = new List<SmtpMailAddress>();
				model.CC = new List<SmtpMailAddress>();
				mailFrom = null;
				mailTo = null;
				mailCC = null;

				editContext = new EditContext(model);

				await this.LoadSiteValues();
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

		protected override async Task OnParametersSetAsync()
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				if (previousMailGuid != MailGuid)
				{
					previousMailGuid = MailGuid;

					loadDataCts?.Cancel();
					loadDataCts = new CancellationTokenSource();

					Mail = null;
					Mail = await this.dataRepository.GetFirst<SmtpMail>(filterExpression: m => m.Guid == MailGuid, includeDepth: 2, cancellationToken: loadDataCts.Token);

					this.CopyMailToModel();
				}

				if (previousIsOpen != IsOpen)
				{
					previousIsOpen = IsOpen;

					if (IsOpen)
					{
						this.CopyMailToModel();
					}
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

		public Task Close()
		{
			IsOpen = false;
			return IsOpenChanged.InvokeAsync(IsOpen);
		}

		private void CopyMailToModel()
		{
			model = this.dataRepository.CloneEntry(source: Mail, resetIds: true, deepClone: true) ?? new SmtpMail();
			model.From = model.From ?? new SmtpMailAddress();
			model.To = model.To ?? new List<SmtpMailAddress>();
			model.CC = model.CC ?? new List<SmtpMailAddress>();
			mailFrom = model?.From.Address;
			mailTo = string.Join(", ", model?.To?.Select(to => to?.Address) ?? Enumerable.Empty<string>());
			mailCC = string.Join(", ", model?.CC?.Select(cc => cc?.Address) ?? Enumerable.Empty<string>());
			editContext = new EditContext(model);
		}

		private async Task LoadSiteValues()
		{
			try
			{
				loadSiteValuesCts?.Cancel();
				loadSiteValuesCts = new CancellationTokenSource();

				Profiles = await azureHelper.GetUsers($"?$select=accountEnabled,mail,companyName,displayName,department,givenName,jobTitle,physicalDeliveryOfficeName,surname,userPrincipalName&$top=999", loadSiteValuesCts.Token);
				MailAddresses = Profiles?.Where(p => p != null && p.AccountEnabled && !string.IsNullOrWhiteSpace(p.GivenName) && !string.IsNullOrWhiteSpace(p.Surname))?.Select(p => p.Mail)?.Distinct()?.ToList() ?? new List<string>();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
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

		private bool IsMailAddressMenuOpen(ElementReference? element)
		{
			return element != null && MailAddressesMenu?.IsOpen == true && ElementReference.Equals(MailAddressesMenu?.AnchorElement, element.Value);
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

				if (model.IsBodyHtml)
				{
					grapesjsEditorValueCts?.Cancel();
					grapesjsEditorValueCts = new CancellationTokenSource();
					model.Body = await grapesjsEditor?.GetValue(grapesjsEditorValueCts);
				}

				if (await customRemoteValidator.Validate())
				{
					await this.dataRepository.Add<SmtpMail>(model);

					if (await this.dataRepository.SaveChangesAsync())
					{
						await smtpMethods.SendMail(model);

						matToaster.Add(message: $"Successfully Sent Mail", type: MatToastType.Primary, icon: "notifications");

						await OnValidSave.InvokeAsync(model);

						await this.Close();
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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed)
			{
				return;
			}

			if (disposing)
			{
				loadDataCts?.Cancel();
				loadSiteValuesCts?.Cancel();
			}

			isDisposed = true;
		}
	}
}