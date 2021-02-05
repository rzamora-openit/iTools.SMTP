using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared
{
	public abstract class BaseFieldValidator : ComponentBase
	{
		public abstract Task<bool> Validate(CancellationToken cancellationToken = default(CancellationToken));
		public abstract IEnumerable<string> GetValidationMessage();
	}
}
