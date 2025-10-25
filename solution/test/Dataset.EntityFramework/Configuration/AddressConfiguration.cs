using System.Data.Entity.ModelConfiguration;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFramework.Configuration;

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