using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Helpers;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer")]
	[Route(Constants.RouteTemplates.MANAGE_MAILS), 
	Route(Constants.RouteTemplates.ADD_MAIL),
	Route(Constants.RouteTemplates.COPY_MAIL),
	Route(Constants.RouteTemplates.VIEW_MAIL),
	Route(Constants.RouteTemplates.DELETE_MAIL)]
	public partial class ManageMails : ComponentBase, IDisposable
	{
		[Inject] private NavigationManager navigationManager { get; set; }

		[Parameter] public string mailGuidString { get; set; }
		public Guid MailGuid
		{
			get
			{
				if (Guid.TryParse(mailGuidString, out var result))
				{
					return result;
				}

				return default;
			}
		}

		private bool isDisposed = false;

		private Dictionary<Type, ComponentInfoViewModel> componentTypeToComponentInfoMap =
			new Dictionary<Type, ComponentInfoViewModel>()
			{
				{ typeof(TableMails), new ComponentInfoViewModel() { Rendered = false, Shown = false, RouteTemplate = Constants.RouteTemplates.MANAGE_MAILS } },
				{ typeof(AddMail), new ComponentInfoViewModel() { Rendered = false, Shown = false, RouteTemplate = Constants.RouteTemplates.ADD_MAIL } },
				{ typeof(CopyMail), new ComponentInfoViewModel() { Rendered = false, Shown = false, RouteTemplate = Constants.RouteTemplates.COPY_MAIL } },
				{ typeof(ViewMail), new ComponentInfoViewModel() { Rendered = false, Shown = false, RouteTemplate = Constants.RouteTemplates.VIEW_MAIL } },
				{ typeof(DeleteMail), new ComponentInfoViewModel() { Rendered = false, Shown = false, RouteTemplate = Constants.RouteTemplates.DELETE_MAIL } }
			};

		private TableMails table;

		private readonly EventHandler<LocationChangedEventArgs>? locationChanged;

		public ManageMails()
		{
			locationChanged = (sender, eventArgs) =>
			{
				if (!isDisposed)
				{
					if (this.navigationManager.RouteTemplateMatch(Constants.RouteTemplates.MANAGE_MAILS))
					{
						this.SetCurrentVisibleComponents(new[] { typeof(TableMails) });
					}
					else if (this.navigationManager.RouteTemplateMatch(Constants.RouteTemplates.VIEW_MAIL))
					{
						this.SetCurrentVisibleComponents(new[] { typeof(TableMails), typeof(ViewMail) });
					}
					else if (this.navigationManager.RouteTemplateMatch(Constants.RouteTemplates.ADD_MAIL))
					{
						this.SetCurrentVisibleComponents(new[] { typeof(AddMail) });
					}
					else if (this.navigationManager.RouteTemplateMatch(Constants.RouteTemplates.COPY_MAIL))
					{
						this.SetCurrentVisibleComponents(new[] { typeof(CopyMail) });
					}
					else if (this.navigationManager.RouteTemplateMatch(Constants.RouteTemplates.DELETE_MAIL))
					{
						this.SetCurrentVisibleComponents(new[] { typeof(TableMails), typeof(DeleteMail) });
					}
				}
			};
		}

		protected override void OnInitialized()
		{
			this.navigationManager.LocationChanged += locationChanged;
			locationChanged.Invoke(this, new LocationChangedEventArgs(this.navigationManager.Uri, false));
		}

		private void SetCurrentVisibleComponents(IEnumerable<Type> componentTypes)
		{
			foreach (var componentType in componentTypeToComponentInfoMap.Keys.ToList())
			{
				if (componentTypes.Contains(componentType))
				{
					componentTypeToComponentInfoMap[componentType].Rendered = true;
					componentTypeToComponentInfoMap[componentType].Shown = true;
				}
				else
				{
					componentTypeToComponentInfoMap[componentType].Shown = false;
				}

				StateHasChanged();
			}
		}

		private void ComponentDialogIsOpenChanged(bool isOpen, Type componentType)
		{
			if (componentTypeToComponentInfoMap.ContainsKey(componentType))
			{
				var componentState = componentTypeToComponentInfoMap[componentType];
				componentState.Shown = isOpen;

				if (!componentState.Shown && this.navigationManager.RouteTemplateMatch(componentState?.RouteTemplate))
				{
					this.navigationManager.NavigateTo(table?.CurrentFiltersUri ?? Constants.RouteTemplates.MANAGE_MAILS);
				}
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
				this.navigationManager.LocationChanged -= locationChanged;
			}

			isDisposed = true;
		}
	}
}
