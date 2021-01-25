using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer, User-Internal")]
	[Route("/")]
	public partial class Index : ComponentBase
	{
		[CascadingParameter] FixedSiteCascadingValueViewModel fixedSiteCascadingValue { get; set; }

		[CascadingParameter] MatTheme theme { get; set; }
	}
}
