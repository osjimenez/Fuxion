using System.Data.Entity.ModelConfiguration;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFramework.Configuration;

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

		HasIndex(i => new { i.InvoiceSerie, i.InvoiceCode })
			.IsUnique();
	}
}