namespace Fuxion.Linq.Test.Filters;

using Fuxion.Linq.Test.Data.Daos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[FilterSchema(nameof(Fields))]
public partial class InvoiceFilter
{
	public static readonly IFilterDescriptor<InvoiceDao>[] Fields = FilterBuilder
		.For<InvoiceDao>()
		.Property(i => i.InvoiceSerie)
		.Property(i => i.InvoiceCode)
		.Property(i=>i.ExpirationTimes)
		.Build();
}
