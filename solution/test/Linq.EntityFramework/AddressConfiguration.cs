using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

public class AddressConfiguration : EntityTypeConfiguration<AddressDao>
{
	public AddressConfiguration()
	{
		ToTable("Addresses");

		HasKey(x => x.AddressId);

		Property(x => x.Street)
			.HasMaxLength(500)
			.IsRequired();

		Property(a => a.Number)
			.IsRequired();
	}
}