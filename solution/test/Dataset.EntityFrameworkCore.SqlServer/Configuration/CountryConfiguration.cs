using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

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