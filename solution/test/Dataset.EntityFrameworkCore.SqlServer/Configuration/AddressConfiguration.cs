using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

public class AddressConfiguration : IEntityTypeConfiguration<AddressDao>
{
	public void Configure(EntityTypeBuilder<AddressDao> builder)
	{
		builder.ToTable("Addresses");

		builder.HasKey(x => x.AddressId);

		builder.Property(x => x.Street)
			.HasMaxLength(500)
			.IsRequired();

		builder.Property(a => a.Number)
			.IsRequired();
	}
}