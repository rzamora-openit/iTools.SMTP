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
		[Inject] private IDataRepository portalRepository { get; set; }

		[CascadingParameter] FixedSiteCascadingValueViewModel fixedSiteCascadingValue { get; set; }
		[CascadingParameter] SiteCascadingValueViewModel siteCascadingValue { get; set; }

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
