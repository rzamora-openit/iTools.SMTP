using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.DataRepositories
{
	public class DataSort<TEntity, TOrderKey>
	{
		public Expression<Func<TEntity, TOrderKey>> OrderExpression { get; set; }
		public SortDirection SortDirection { get; set; }
	}

	public enum SortDirection
	{
		ASC,
		DESC,
		None
	}
}
