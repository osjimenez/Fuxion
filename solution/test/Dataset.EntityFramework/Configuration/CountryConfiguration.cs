using System.Data.Entity.ModelConfiguration;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFramework.Configuration;

public class CountryConfiguration : EntityTypeConfiguration<CountryDao>
{
	public CountryConfiguration()
	{
		ToTable("Countries");

		HasKey(x => x.CountryId);

		Property(x => x.Code)
			.HasMaxLength(10)
			.IsRequired();

		Property(a => a.Name)
			.HasMaxLength(150)
			.IsRequired();
	}
}