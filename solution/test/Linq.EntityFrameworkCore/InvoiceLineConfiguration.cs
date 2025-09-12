using Fuxion.Linq.Test.Data.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

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