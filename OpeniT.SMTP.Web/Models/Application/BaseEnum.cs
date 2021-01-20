using System;
using System.ComponentModel.DataAnnotations;

namespace OpeniT.SMTP.Web.Models
{
	public class BaseEnum
	{
		public int Id { get; set; }

		[Required]
		public string Value { get; set; }

		public int? OrderOfImportance { get; set; }

		public DateTime DateCreated { get; set; }

		public string CreatedById { get; set; }

		public DateTime DateUpdated { get; set; }

		public string LastUpdatedById { get; set; }

		public bool Locked { get; set; }
	}
}
