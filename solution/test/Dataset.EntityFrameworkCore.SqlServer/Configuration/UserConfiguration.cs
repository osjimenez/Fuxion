using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<UserDao>
{
	public void Configure(EntityTypeBuilder<UserDao> builder)
	{
		builder.ToTable("Users");

		builder.HasKey(x => x.UserId);

		builder.Property(x => x.FirstName)
			.HasMaxLength(150)
			.IsRequired();

		builder.Property(x => x.LastName)
			.HasMaxLength(150);
	}
}