using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Constants
{
	public static class RouteTemplates
	{
		public const string HOME = "/";

		public const string MANAGE_MAILS = "/smtp/mails";
		public const string ADD_MAIL = "/smtp/mails/add";
		public const string COPY_MAIL = "/smtp/mails/copy/{mailGuidString}";
		public const string VIEW_MAIL = "/smtp/mails/view/{mailGuidString}";
		public const string DELETE_MAIL = "/smtp/mails/delete/{mailGuidString}";
	}
}
