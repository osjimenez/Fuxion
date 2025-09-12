using Fuxion.Linq.Test.Data.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

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