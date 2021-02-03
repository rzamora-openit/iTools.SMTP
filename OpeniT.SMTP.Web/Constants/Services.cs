using Microsoft.AspNetCore.Components.Routing;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Constants
{
	public static class Services
	{
		public static readonly ServiceViewModel HOME_SERVICE = new ServiceViewModel()
		{
			Uri = RouteTemplates.HOME,
			UriMatch = NavLinkMatch.All,
			Title = "Home",
			Description = "Description",
			Icon = "home"
		};
		public static readonly ServiceGroupViewModel HOME_SERVICE_GROUP = new ServiceGroupViewModel()
		{
			Key = 1,
			OrderOfImportance = 1,
			Type = ServiceGroupType.MainSidebar,
			Title = "Homepage",
			Description = "Description",
			Icon = "home",
			Services = new ServiceViewModel[]
			{
				HOME_SERVICE
			}
		};

		public static readonly ServiceViewModel MANAGE_SMTP_MAILS_SERVICE = new ServiceViewModel()
		{
			Key = 26,
			OrderOfImportance = 1,
			Uri = RouteTemplates.MANAGE_MAILS,
			Title = "Mails",
			Description = "Description",
			Icon = "email"
		};
		public static readonly ServiceGroupViewModel MANAGE_SMTP_SERVICE_GROUP = new ServiceGroupViewModel()
		{
			Key = 12,
			OrderOfImportance = 12,
			Type = ServiceGroupType.MainSidebar,
			Title = "SMTP",
			Description = "Description",
			Icon = "email",
			Services = new ServiceViewModel[]
			{
				MANAGE_SMTP_MAILS_SERVICE
			}
		};
	}
}
