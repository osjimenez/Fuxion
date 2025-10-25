using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

public class InvoiceConfiguration : IEntityTypeConfiguration<InvoiceDao>
{
	public void Configure(EntityTypeBuilder<InvoiceDao> builder)
	{
		builder.ToTable("Invoices");

		builder.HasKey(x => x.InvoiceId);

		builder.Property(a => a.InvoiceSerie)
			.HasMaxLength(20)
			.IsRequired();
		
		builder.Property(x => x.InvoiceCode)
			.HasMaxLength(100)
			.IsRequired();
	}
}