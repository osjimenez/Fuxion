using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

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