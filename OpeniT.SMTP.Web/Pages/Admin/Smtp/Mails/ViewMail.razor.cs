using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer")]
	public partial class ViewMail : ComponentBase
	{
		[Inject] private IDataRepository dataRepository { get; set; }

		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public Guid MailGuid { get; set; }

		private bool isBusy = false;
		private bool isRendered = false;
		private bool isDisposed = false;

		private Guid previousMailGuid;
		private SmtpMail mail;

		private CancellationTokenSource loadDataCts;

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

					mail = null;
					mail = await this.dataRepository.GetFirst<SmtpMail>(filterExpression: m => m.Guid == MailGuid, includeDepth: 2, cancellationToken: loadDataCts.Token);
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
	}
}