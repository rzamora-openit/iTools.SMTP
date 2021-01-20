using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.ViewModels
{
	public enum ServiceGroupType
	{
		MainSidebar,
		ControlSidebar,
		Header
	}

	public class ServiceGroupViewModel
	{
		public int Key { get; set; }

		public int OrderOfImportance { get; set; }

		public ServiceGroupType Type { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string Icon { get; set; }

		public IEnumerable<ServiceViewModel> Services { get; set; }
	}
}
