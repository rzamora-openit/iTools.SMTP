using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace OpeniT.SMTP.Web.Models
{
	public class SmtpMail : BaseClass
	{
		public SmtpMailAddress From { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public bool IsBodyHtml { get; set; }
		public ICollection<SmtpMailAddress> To { get; set; } = new List<SmtpMailAddress>();
		public ICollection<SmtpMailAddress> CC { get; set; } = new List<SmtpMailAddress>();
		public ICollection<SmtpMailAddress> BCC { get; set; } = new List<SmtpMailAddress>();
	}
}
