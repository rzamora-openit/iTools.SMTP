using System;
using System.Collections.Generic;
using System.Text;

namespace OpeniT.SMTP.Web.Models
{
	public class SmtpMailAddress : BaseCommon
	{
		public string Address { get; set; }
		public string DisplayName { get; set; }
	}
}
