using iTools.Utilities;
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
		private FormValidator formValidator;
		private SmtpMail model;
		private string mailFrom;
		private string mailTo;
		private string mailCC;
		private string mailBCC;

		private MatTextField<string> MailFromTextField;
		private MatTextField<string> MailToTextField;
		private MatTextField<string> MailCCTextField;
		private MatTextField<string> MailBCCTextField;
		private Menu MailAddressesMenu;
		private string mailAddressesSearchText =>
			ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailFromTextField?.Ref)
			? model?.From?.Address
			: ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailToTextField?.Ref)
				? model?.To?.LastOrDefault()?.Address
				: ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailCCTextField?.Ref)
					? model?.CC?.LastOrDefault()?.Address
					: ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailBCCTextField?.Ref)
						? model?.BCC?.LastOrDefault()?.Address 
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
				mailBCC = null;

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
			mailBCC = string.Join(", ", model?.BCC?.Select(bcc => bcc?.Address) ?? Enumerable.Empty<string>());
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

				this.MailFromAddressChanged(mailFrom);
				this.MailFromDisplayNameChanged(Profiles?.FirstOrDefault(p => p.Mail == mailFrom)?.DisplayName);
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
			else if (ElementReference.Equals(MailAddressesMenu?.AnchorElement, MailBCCTextField?.Ref))
			{
				mailBCC = mailBCC ?? string.Empty;

				var mailBCCSubstr = mailBCC.Substring(0, mailBCC.LastIndexOf(',') + 1);
				mailBCC = $"{mailBCCSubstr}{(string.IsNullOrWhiteSpace(mailBCCSubstr) ? string.Empty : " ")}{mailAddress}";
				this.MailBccChanged(mailBCC);
			}
		}

		private bool IsMailAddressMenuOpen(ElementReference? element)
		{
			return element != null && MailAddressesMenu?.IsOpen == true && ElementReference.Equals(MailAddressesMenu?.AnchorElement, element.Value);
		}

		private void MailFromAddressChanged(string value)
		{
			model.From = model.From ?? new SmtpMailAddress();
			model.From.Address = value;
			editContext.NotifyFieldChanged(() => model.From.Address);
		}

		private void MailFromDisplayNameChanged(string value)
		{
			model.From = model.From ?? new SmtpMailAddress();
			model.From.DisplayName = value;
			editContext.NotifyFieldChanged(() => model.From.DisplayName);
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

		private void MailBccChanged(string value)
		{
			model.BCC = string.IsNullOrWhiteSpace(value) ? new List<SmtpMailAddress>() : Regex.Replace(value, @"\s+", "").Split(",").Where(bcc => !string.IsNullOrWhiteSpace(bcc)).Select(bcc => new SmtpMailAddress() { Address = bcc }).ToList();
			editContext.NotifyFieldChanged(() => model.BCC);
		}

		private HashSet<Func<string, Task<ValidationResult>>> fromAddressValidators =>
			new HashSet<Func<string, Task<ValidationResult>>>() { FromAddressHasValue, FromAddressIsValid };
		private Task<ValidationResult> FromAddressHasValue(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return Task.FromResult(new ValidationResult("From Address field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}
		private Task<ValidationResult> FromAddressIsValid(string value)
		{
			if (!string.IsNullOrWhiteSpace(value) && !(new EmailAddressAttribute().IsValid(value)))
			{
				return Task.FromResult(new ValidationResult($"{value} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<ICollection<SmtpMailAddress>, Task<ValidationResult>>> toValidators =>
			new HashSet<Func<ICollection<SmtpMailAddress>, Task<ValidationResult>>>() { ToHasValue, ToIsValid };
		private Task<ValidationResult> ToHasValue(ICollection<SmtpMailAddress> value)
		{
			if (value?.Any() != true)
			{
				return Task.FromResult(new ValidationResult("To field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}
		private Task<ValidationResult> ToIsValid(ICollection<SmtpMailAddress> value)
		{
			var notValidAddress = value?.FirstOrDefault(to => !(new EmailAddressAttribute().IsValid(to?.Address)));
			if (notValidAddress != null)
			{
				return Task.FromResult(new ValidationResult($"{notValidAddress?.Address} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<ICollection<SmtpMailAddress>, Task<ValidationResult>>> ccValidators =>
			new HashSet<Func<ICollection<SmtpMailAddress>, Task<ValidationResult>>>() { CcIsValid };
		private Task<ValidationResult> CcIsValid(ICollection<SmtpMailAddress> value)
		{
			var notValidAddress = value?.FirstOrDefault(cc => !(new EmailAddressAttribute().IsValid(cc?.Address)));
			if (notValidAddress != null)
			{
				return Task.FromResult(new ValidationResult($"{notValidAddress?.Address} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<ICollection<SmtpMailAddress>, Task<ValidationResult>>> bccValidators =>
			new HashSet<Func<ICollection<SmtpMailAddress>, Task<ValidationResult>>>() { BccIsValid };
		private Task<ValidationResult> BccIsValid(ICollection<SmtpMailAddress> value)
		{
			var notValidAddress = value?.FirstOrDefault(bcc => !(new EmailAddressAttribute().IsValid(bcc?.Address)));
			if (notValidAddress != null)
			{
				return Task.FromResult(new ValidationResult($"{notValidAddress?.Address} is not a valid email address."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<string, Task<ValidationResult>>> subjectValidators =>
			new HashSet<Func<string, Task<ValidationResult>>>() { SubjectHasValue };
		private Task<ValidationResult> SubjectHasValue(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return Task.FromResult(new ValidationResult("Subject field requires a value."));
			}

			return Task.FromResult(ValidationResult.Success);
		}

		private HashSet<Func<string, Task<ValidationResult>>> bodyValidators =>
			new HashSet<Func<string, Task<ValidationResult>>>() { BodyHasValue };
		private Task<ValidationResult> BodyHasValue(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
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
					var token = grapesjsEditorValueCts.Token;
					model.Body = await grapesjsEditor?.GetValue(token);
				}

				if (await formValidator.Validate())
				{
					if (await smtpMethods.SendMail(model))
					{
						await this.dataRepository.Add<SmtpMail>(model);

						if (await this.dataRepository.SaveChangesAsync())
						{
							matToaster.Add(message: $"Successfully Sent Mail", type: MatToastType.Primary, icon: "notifications");

							await OnValidSave.InvokeAsync(model);

							await this.Close();
						}
					}
				}
				else
				{
					matToaster.Add(message: $"Please fill out correctly to save.", type: MatToastType.Primary, icon: "notifications");
				}
			}
			catch (Exception ex)
			{
				grapesjsEditorValueCts?.Cancel();
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
				grapesjsEditorValueCts?.Cancel();
				loadDataCts?.Cancel();
				loadSiteValuesCts?.Cancel();
			}

			isDisposed = true;
		}
	}
}