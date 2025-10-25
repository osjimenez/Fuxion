using System.Data.Entity.ModelConfiguration;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFramework.Configuration;

public class CityConfiguration : EntityTypeConfiguration<CityDao>
{
	public CityConfiguration()
	{
		ToTable("Cities");

		HasKey(x => x.CityId);

		Property(x => x.Code)
			.HasMaxLength(10)
			.IsRequired();

		Property(a => a.Name)
			.HasMaxLength(150)
			.IsRequired();
	}
}