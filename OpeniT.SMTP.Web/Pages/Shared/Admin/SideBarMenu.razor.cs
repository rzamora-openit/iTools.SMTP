using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared.Admin
{
	public partial class SideBarMenu : ComponentBase
	{
		[Inject] private ILocalStorageService localStorage { get; set; }
		[Inject] private IPortalRepository portalRepository { get; set; }

		[CascadingParameter] SiteCascadingValueViewModel siteCascadingValue { get; set; }
		[CascadingParameter] private Task<AuthenticationState> authenticationStateTask { get; set; }

		private ApplicationUser appUser;
		private SemaphoreSlim singleTaskQueue = new SemaphoreSlim(1);

		protected override async Task OnInitializedAsync()
		{
			var user = (await singleTaskQueue.Enqueue(() => authenticationStateTask)).User;
			appUser = await singleTaskQueue.Enqueue(() => this.portalRepository.GetUserByUserName(user.Identity.Name));
		}

		private async Task ToggleServiceGroupCollapsed(ServiceGroupViewModel serviceGroup)
		{
			if (siteCascadingValue.ServiceGroupKeyToCollapsedMap.ContainsKey(serviceGroup.Key))
			{
				siteCascadingValue.ServiceGroupKeyToCollapsedMap[serviceGroup.Key] = !siteCascadingValue.ServiceGroupKeyToCollapsedMap[serviceGroup.Key];
			}
			else
			{
				siteCascadingValue.ServiceGroupKeyToCollapsedMap[serviceGroup.Key] = false;
			}

			await JSIntropMethods.SetSiteCascadingValue(localStorage, siteCascadingValue);
		}
	}
}
