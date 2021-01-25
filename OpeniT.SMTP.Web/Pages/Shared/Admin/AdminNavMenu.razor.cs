using Blazored.LocalStorage;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared.Admin
{
	public partial class AdminNavMenu : ComponentBase
	{
		[Inject] private ILocalStorageService localStorage { get; set; }
		[Inject] private IJSRuntime jsRuntime { get; set; }
		[Inject] private NavigationManager navigationManager { get; set; }

		[CascadingParameter] FixedSiteCascadingValueViewModel fixedSiteCascadingValue { get; set; }
		[CascadingParameter] SiteCascadingValueViewModel siteCascadingValue { get; set; }

		private bool showControls = false;
		private bool searchIsOpen = false;
		private bool isXSmallScreen => siteCascadingValue?.BrowserSizeState?.XSmallDown == true;

		private MatAutocompleteList<ServiceViewModel> matAutocompleteList;

		private async Task SaveTheme(string themePrimary)
		{
			// correct format
			if (this.ValidateColor(themePrimary))
			{
				siteCascadingValue.Theme.Primary = themePrimary;
				siteCascadingValue.Theme.Secondary = themePrimary;

				await JSIntropMethods.SetSiteCascadingValue(localStorage, siteCascadingValue);
			}
		}

		private bool ValidateColor(string color)
		{
			// correct hex format
			var regex = new Regex(@"(^#[a-fA-F0-9]{6}$)|(rgb\((\d{1,3}), (\d{1,3}), (\d{1,3})\))");
			if (!regex.Match(color).Success)
			{
				return false;
			}

			// far from white
			var rgb = System.Drawing.ColorTranslator.FromHtml(color);
			var grayScale = 0.2126 * rgb.R + 0.7152 * rgb.G + 0.0722 * rgb.B;
			if (grayScale >= 225)
			{
				return false;
			}

			return true;
		}

		private ServiceViewModel selectedService { get; set; }

		private async Task ToggleSideBar()
		{
			siteCascadingValue.SidebarIsOpen = !siteCascadingValue.SidebarIsOpen;

			await JSIntropMethods.SetSiteCascadingValue(localStorage, siteCascadingValue);
		}

		private void ToggleControls()
		{
			showControls = !showControls;
		}

		private void ToggleSearchIsOpen()
		{
			searchIsOpen = !searchIsOpen;
			if (!searchIsOpen)
			{
				matAutocompleteList.ClearText(EventArgs.Empty);
			}
		}

		private void SearchValueChange(ServiceViewModel service)
		{
			if (service != null)
			{
				navigationManager.NavigateTo(service.Uri);
			}
		}
	}
}
