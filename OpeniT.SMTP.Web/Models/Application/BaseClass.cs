using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpeniT.SMTP.Web.Models
{
	public class BaseClass
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Key, Column(Order = 0)]
		public int Id { get; set; }

		public Guid Guid { get; set; } = Guid.NewGuid();

		public string ClassId { get; set; }

		public DateTime DateCreated { get; set; }

		public string CreatedById { get; set; }

		public DateTime DateUpdated { get; set; }

		public string LastUpdatedById { get; set; }

		public bool Locked { get; set; }
	}
}
