using Newtonsoft.Json;
using OpeniT.SMTP.Web.Methods;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpeniT.SMTP.Web.ViewModels
{
	public class ComponentStateViewModel
	{
		public Regex UrlRegex { get; set; }

		public bool Rendered { get; set; }

		public bool Shown { get; set; }
	}
}
