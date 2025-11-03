using Fuxion.Linq;
using System.Linq;
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
		//.WithExternalData<IQueryable<InvoiceAppointmentDao>>("appointments")
		//	.Property("Appointments", (i, apps) => apps.Where(a => a.InvoiceSerie == i.InvoiceSerie && a.InvoiceCode == i.InvoiceCode))
		.Build();
}

[FilterSchema(nameof(Fields))]
public partial class AppointmentFilter
{
	public static readonly IFilterDescriptor<AppointmentDao>[] Fields = FilterBuilder
		.For<AppointmentDao>("Appointment", "Appointments")
		.Property(i => i.ExternalId)
		//.WithExternalData<IQueryable<InvoiceDao>>("invoices")
		//	.Property("Invoice", (a, invoices) => invoices.FirstOrDefault(i => i.InvoiceSerie == a.InvoiceSerie && i.InvoiceCode == a.InvoiceCode))
		.Build();
}