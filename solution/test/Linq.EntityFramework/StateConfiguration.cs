using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

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