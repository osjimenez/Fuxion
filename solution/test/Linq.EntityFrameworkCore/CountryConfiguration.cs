using Fuxion.Linq.Test.Data.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

public class CountryConfiguration : IEntityTypeConfiguration<CountryDao>
{
	public void Configure(EntityTypeBuilder<CountryDao> builder)
	{
		builder.ToTable("Countries");

		builder.HasKey(x => x.CountryId);

		builder.Property(x => x.Code)
			.HasMaxLength(10)
			.IsRequired();

		builder.Property(a => a.Name)
			.HasMaxLength(150)
			.IsRequired();
	}
}