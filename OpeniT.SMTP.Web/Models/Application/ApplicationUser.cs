using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace OpeniT.SMTP.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public string DisplayName { get; set; }

        public string NameExtension { get; set; }

        public string Designation { get; set; }

        public string Department { get; set; }

        public string Division { get; set; }

        public string Details { get; set; }

        [NotMapped]
        public ICollection<string> Roles { get; set; } = new List<string>();
        public string _Roles
        {
            get { return string.Join(",", Roles.OrderBy(x => x)); }
            set { if (value != null) Roles = value.Split(","); }
        }
    }
}
