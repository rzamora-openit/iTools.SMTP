using MatBlazor;
using System;
using System.Collections.Generic;

namespace OpeniT.SMTP.Web.ViewModels
{
	public class SiteCascadingValueViewModel
	{
		public bool SidebarIsOpen { get; set; }
		public ThemeViewModel Theme { get; set; }
		public BrowserSizeStateViewModel BrowserSizeState { get; set; }
		public Dictionary<int, bool> ServiceGroupKeyToCollapsedMap { get; set; } = new Dictionary<int, bool>();
	}
}
