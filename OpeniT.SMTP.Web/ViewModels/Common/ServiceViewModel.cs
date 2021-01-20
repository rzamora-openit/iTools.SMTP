using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.ViewModels
{
	public class ServiceViewModel
	{
		public int Key { get; set; }

		public int OrderOfImportance { get; set; }

		public string Uri { get; set; }

		public NavLinkMatch UriMatch { get; set; } = NavLinkMatch.Prefix;

		public string Title { get; set; }

		public string Description { get; set; }

		public string Icon { get; set; }
	}
}
