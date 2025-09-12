using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

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