using System.Data.Entity.ModelConfiguration;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFramework.Configuration;

public class StateConfiguration : EntityTypeConfiguration<StateDao>
{
	public StateConfiguration()
	{
		ToTable("States");

		HasKey(x => x.StateId);

		Property(x => x.Code)
			.HasMaxLength(10)
			.IsRequired();

		Property(a => a.Name)
			.HasMaxLength(150)
			.IsRequired();
	}
}