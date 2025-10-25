using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLineDao>
{
	public void Configure(EntityTypeBuilder<InvoiceLineDao> builder)
	{
		builder.ToTable("InvoiceLines");

		builder.HasKey(x => x.InvoiceLineId);

		builder.Property(a => a.Concept)
			.HasMaxLength(500)
			.IsRequired();
	}
}