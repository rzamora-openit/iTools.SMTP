using MatBlazor;
using Microsoft.AspNetCore.Components;
using OpeniT.SMTP.Web.Models;
using System;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	public partial class DeleteMail : ComponentBase
	{
		[Inject] private IMatToaster matToaster { get; set; }

		[CascadingParameter] public ManageMails ManageMails { get; set; }

		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public SmtpMail Mail { get; set; }

		private bool isBusy = false;
		private bool isRendered = false;

		protected override void OnAfterRender(bool firstRender)
		{
			if (firstRender)
			{
				isRendered = true;
				StateHasChanged();
			}
		}

		public Task Open()
		{
			IsOpen = true;
			return IsOpenChanged.InvokeAsync(IsOpen);
		}

		public Task Close()
		{
			IsOpen = false;
			return IsOpenChanged.InvokeAsync(IsOpen);
		}

		public Task IsOpenIsChanged(bool isOpen)
		{
			if (isOpen)
			{
				return this.Open();
			}
			else
			{
				return this.Close();
			}
		}

		private async Task Save()
		{
			try
			{
				isBusy = true;

				if (await ManageMails.DeleteMail(Mail))
				{
					isBusy = false;

					this.matToaster.Add(message: $"Successfully Deleted Mail", type: MatToastType.Primary, icon: "notifications");
					await this.Close();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}
