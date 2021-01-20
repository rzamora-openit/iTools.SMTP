using MatBlazor;
using System;
using System.Collections.Generic;

namespace OpeniT.SMTP.Web.ViewModels
{
	public class SiteCascadingValueViewModel
	{
		public bool SidebarIsOpen { get; set; }
		public MatTheme Theme { get; set; }
		public List<ServiceGroupViewModel> ServicesGroups { get; set; } = new List<ServiceGroupViewModel>();
		public Dictionary<int, bool> ServiceGroupKeyToCollapsedMap { get; set; } = new Dictionary<int, bool>();
	}
}
