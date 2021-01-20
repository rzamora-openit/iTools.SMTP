using Blazored.LocalStorage;
using BlazorPro.BlazorSize;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared.Admin
{
	public partial class MainLayout : LayoutComponentBase, IDisposable
	{
		[Inject] private IJSRuntime jsRuntime { get; set; }
		[Inject] public ILocalStorageService localStorage { get; set; }
		[Inject] private NavigationManager navigationManager { get; set; }
		[Inject] private ResizeListener resizeListener { get; set; }

		private bool isInitialized = false;
		private bool isSizeInitialized = false;
		private bool isDisposing = false;

		private string classMapper => $"wrapper {(siteCascadingValue?.SidebarIsOpen == true ? "sidebar-open" : "sidebar-collapse")}";

		private BrowserSizeStateViewModel browserSizeState = new BrowserSizeStateViewModel();
		private SiteCascadingValueViewModel siteCascadingValue { get; set; } = new SiteCascadingValueViewModel()
		{
			SidebarIsOpen = true,
			Theme = new MatTheme()
			{
				Primary = "#cd172d",
				Secondary = "#cd172d"
			},
			ServicesGroups = new List<ServiceGroupViewModel>()
			{
				Constants.Services.HOME_SERVICE_GROUP,
				Constants.Services.MANAGE_SMTP_SERVICE_GROUP
			}
		};

		private EventHandler<LocationChangedEventArgs> locationChanged;
		private EventHandler<ChangedEventArgs> localStorageChanged;
		private EventHandler<BrowserWindowSize> sizeChanged;

		public MainLayout()
		{
			locationChanged = async (sender, eventArgs) =>
			{
				try
				{
					if (!isDisposing)
					{
						var currentUri = "/" + navigationManager.ToBaseRelativePath(navigationManager.Uri);
						var serviceGroup = siteCascadingValue?.ServicesGroups?.FirstOrDefault(sg => sg?.Services?.Any(s => !string.Equals(s?.Title, "Home") && currentUri?.Contains(s?.Uri) == true) == true);

						if (serviceGroup != null && siteCascadingValue.ServiceGroupKeyToCollapsedMap != null)
						{
							siteCascadingValue.ServiceGroupKeyToCollapsedMap[serviceGroup.Key] = false;
						}

						await this.CloseSidebars();
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					StateHasChanged();
				}
			};
			localStorageChanged = async (object sender, ChangedEventArgs e) =>
			{
				try
				{
					if (!isDisposing && e.Key == "siteCascadingValue")
					{
						await this.ApplySavedSiteCascadingValue(e.NewValue as SiteCascadingValueViewModel);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					StateHasChanged();
				}
			};
			sizeChanged = (object sender, BrowserWindowSize window) =>
			{
				try
				{
					if (!isDisposing)
					{
						browserSizeState.LargeDown = window.Width < 1199.98;
						browserSizeState.MediumDown = window.Width < 991.98;
						browserSizeState.SmallDown = window.Width < 767.98;
						browserSizeState.XSmallDown = window.Width < 576;

						isSizeInitialized = true;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					StateHasChanged();
				}
			};
		}

		protected override void OnInitialized()
		{
			navigationManager.LocationChanged += locationChanged;
			localStorage.Changed += localStorageChanged;
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				resizeListener.OnResized += sizeChanged;

				var savedSiteCascadingValue = await JSIntropMethods.GetSiteCascadingValue(localStorage);
				if (savedSiteCascadingValue == null)
				{
					await JSIntropMethods.SetSiteCascadingValue(localStorage, siteCascadingValue);
				}
				else
				{
					await this.ApplySavedSiteCascadingValue(savedSiteCascadingValue);
				}

				await JSIntropMethods.AdminSiteInit(jsRuntime);
				await JSIntropMethods.SiteInit(jsRuntime);

				locationChanged.Invoke(this, null);
				isInitialized = true;
				StateHasChanged();
			}
		}

		private async Task ApplySavedSiteCascadingValue(SiteCascadingValueViewModel value)
		{
			siteCascadingValue.SidebarIsOpen = value.SidebarIsOpen;

			var siteCascadingValueHasChanged = false;
			if (value.Theme == null)
			{
				siteCascadingValue.Theme = new MatTheme()
				{
					Primary = "#cd172d",
					Secondary = "#cd172d"
				};
				siteCascadingValueHasChanged = true;
			}
			else
			{
				siteCascadingValue.Theme = value.Theme;
			}

			if (value.ServiceGroupKeyToCollapsedMap == null)
			{
				siteCascadingValue.ServiceGroupKeyToCollapsedMap = value?.ServicesGroups?.ToDictionary(sg => sg.Key, sg => true) ?? new Dictionary<int, bool>();
				siteCascadingValueHasChanged = true;
			}
			else
			{
				siteCascadingValue.ServiceGroupKeyToCollapsedMap = value.ServiceGroupKeyToCollapsedMap;
			}
			
			if (siteCascadingValueHasChanged)
			{
				await JSIntropMethods.SetSiteCascadingValue(localStorage, siteCascadingValue);
			}

			await JSIntropMethods.UpdateMatBlazorTheme(jsRuntime, siteCascadingValue.Theme.GetStyle());
		}

		private async Task CloseSidebars()
		{
			if (browserSizeState?.SmallDown == true)
			{
				siteCascadingValue.SidebarIsOpen = false;
			}

			await JSIntropMethods.SetSiteCascadingValue(localStorage, siteCascadingValue);
		}

		protected virtual void Dispose(bool disposing)
		{
			isDisposing = disposing;
		}

		void IDisposable.Dispose()
		{
			navigationManager.LocationChanged -= locationChanged;
			localStorage.Changed -= localStorageChanged;
			resizeListener.OnResized -= sizeChanged;
			Dispose(disposing: true);
		}
	}
}
