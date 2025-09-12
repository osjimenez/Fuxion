using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

public class InvoiceLineConfiguration : EntityTypeConfiguration<InvoiceLineDao>
{
	public InvoiceLineConfiguration()
	{
		ToTable("InvoiceLines");

		HasKey(x => x.InvoiceLineId);

		Property(a => a.Concept)
			.HasMaxLength(500)
			.IsRequired();
	}
}