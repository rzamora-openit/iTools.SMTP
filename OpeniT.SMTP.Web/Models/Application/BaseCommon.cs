using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpeniT.SMTP.Web.Models
{
	public class BaseCommon
	{
		public int Id { get; set; }

		public DateTime DateCreated { get; set; }

		public string CreatedById { get; set; }

		public DateTime DateUpdated { get; set; }

		public string LastUpdatedById { get; set; }

		public bool Locked { get; set; }
	}
}
