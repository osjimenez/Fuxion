using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

public class AppointmentConfiguration : IEntityTypeConfiguration<AppointmentDao>
{
	public void Configure(EntityTypeBuilder<AppointmentDao> builder)
	{
		builder.ToTable("Appointments");

		builder.Property(a => a.ExternalId)
			.HasMaxLength(150)
			.IsRequired();

		builder.HasKey(x => x.AppointmentId);
	}
}