using Fuxion.Linq;
using Test.Dataset.Daos;

namespace Test.Linq.Filters;

[FilterSchema(nameof(Fields))]
public partial class InvoiceFilter
{
	public static readonly IFilterDescriptor<InvoiceDao>[] Fields = FilterBuilder
		.For<InvoiceDao>("Invoice", "Invoices")
		.Property(i => i.InvoiceSerie)
		.Property(i => i.InvoiceCode)
		.Property(i => i.ExpirationTimes)
		.Build();
}