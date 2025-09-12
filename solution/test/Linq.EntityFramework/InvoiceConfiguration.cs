using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

public class InvoiceConfiguration : EntityTypeConfiguration<InvoiceDao>
{
	public InvoiceConfiguration()
	{
		ToTable("Invoices");

		HasKey(x => x.InvoiceId);

		Property(a => a.InvoiceSerie)
			.HasMaxLength(20)
			.IsRequired();
		
		Property(x => x.InvoiceCode)
			.HasMaxLength(100)
			.IsRequired();
	}
}