using MatBlazor;
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
	public partial class DeleteMail : ComponentBase, IDisposable
	{
		[Inject] private IDataRepository dataRepository { get; set; }
		[Inject] private IMatToaster matToaster { get; set; }

		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public EventCallback<SmtpMail> OnValidSave { get; set; }
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

		private async Task Save()
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				this.dataRepository.Remove<SmtpMail>(mail);

				if (await this.dataRepository.SaveChangesAsync())
				{
					this.matToaster.Add(message: $"Successfully Deleted Mail", type: MatToastType.Primary, icon: "notifications");

					await this.Close();
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
			}

			isDisposed = true;
		}
	}
}
