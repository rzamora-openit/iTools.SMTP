using Microsoft.JSInterop;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.ViewModels
{
	public class FixedSiteCascadingValueViewModel
	{
		public ApplicationUser ApplicationUser { get; set; }

		public List<ServiceGroupViewModel> ServicesGroups { get; set; } = new List<ServiceGroupViewModel>();
	}
}
