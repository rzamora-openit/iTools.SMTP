using System;
using System.Collections.Generic;
using System.Text;

namespace OpeniT.SMTP.Web.Models
{
	public interface IBaseBeforeSavingChanges
	{
		public void BeforeSavingChanges();
	}
}
