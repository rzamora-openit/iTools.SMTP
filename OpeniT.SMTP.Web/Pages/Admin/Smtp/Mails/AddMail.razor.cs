using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.Pages.Shared.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpeniT.SMTP.Web.Pages.Shared;
using System.Threading;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer, User-Internal")]
	public partial class AddMail : ComponentBase
	{
		[Inject] private IMatToaster matToaster { get; set; }
		[Inject] private NavigationManager navigationManager { get; set; }
		[Inject] private SMTPMethods smtpMethods { get; set; }

		[CascadingParameter] private ManageMails ManageMails { get; set; }

		[Parameter] public bool Shown { get; set; }

		private bool isBusy = false;
		private bool previousShown;

		private EditContext editContext;
		private SmtpMail model = new SmtpMail();
		private string mailTo;
		private string mailCC;

		private GrapesjsEditor grapesjsEditor;
		private CancellationTokenSource grapesjsEditorValueCts;

		private CustomRemoteValidator customRemoteValidator;

		protected override void OnInitialized()
		{
			this.Clear();
			previousShown = !Shown;
		}

		protected override void OnParametersSet()
		{
			if (previousShown != Shown)
			{
				previousShown = Shown;

				if (Shown)
				{
					this.Clear();
				}
			}
		}

		private void Clear()
		{
			try
			{
				model = new SmtpMail();
				model.From = new SmtpMailAddress();
				model.To = new List<SmtpMailAddress>();
				model.CC = new List<SmtpMailAddress>();
				mailTo = null;
				mailCC = null;

				editContext = new EditContext(model);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private void MailToChanged(string value)
		{
			mailTo = value;

			model.To = Regex.Replace(mailTo, @"\s+", "").Split(",").Select(to => new SmtpMailAddress() { Address = to }).ToList();
			editContext.NotifyFieldChanged(() => model.To);
		}

		private void MailCcChanged(string value)
		{
			mailCC = value;

			model.CC = Regex.Replace(mailCC, @"\s+", "").Split(",").Select(cc => new SmtpMailAddress() { Address = cc }).ToList();
			editContext.NotifyFieldChanged(() => model.CC);
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

						matToaster.Add(message: $"Successfully Send Mail", type: MatToastType.Primary, icon: "notifications");
						this.navigationManager.NavigateTo(ManageMails.CurrentFiltersUri);
					}
				}
				else
				{
					this.matToaster.Add(message: $"Please fill out correctly to save.", type: MatToastType.Primary, icon: "notifications");
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
	}
}
