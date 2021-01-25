using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using OpeniT.SMTP.Web.Models;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer, User-Internal")]
	public partial class ViewMail : ComponentBase
	{
		[CascadingParameter] public ManageMails ManageMails { get; set; }

		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public SmtpMail Mail { get; set; } = new SmtpMail();

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
	}
}