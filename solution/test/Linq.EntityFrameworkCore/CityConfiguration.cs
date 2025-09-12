using Fuxion.Linq.Test.Data.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

public class CityConfiguration : IEntityTypeConfiguration<CityDao>
{
	public void Configure(EntityTypeBuilder<CityDao> builder)
	{
		builder.ToTable("Cities");

		builder.HasKey(x => x.CityId);

		builder.Property(x => x.Code)
			.HasMaxLength(10)
			.IsRequired();

		builder.Property(a => a.Name)
			.HasMaxLength(150)
			.IsRequired();
	}
}