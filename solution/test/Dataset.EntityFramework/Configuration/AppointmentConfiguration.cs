using System.Data.Entity.ModelConfiguration;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

public class AppointmentConfiguration : EntityTypeConfiguration<AppointmentDao>
{
	public AppointmentConfiguration()
	{
		ToTable("Appointments");

		Property(a => a.ExternalId)
			.HasMaxLength(150)
			.IsRequired();

		HasKey(x => x.AppointmentId);
	}
}