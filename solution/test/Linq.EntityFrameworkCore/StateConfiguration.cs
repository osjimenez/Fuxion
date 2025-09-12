using Fuxion.Linq.Test.Data.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

public class StateConfiguration : IEntityTypeConfiguration<StateDao>
{
	public void Configure(EntityTypeBuilder<StateDao> builder)
	{
		builder.ToTable("States");

		builder.HasKey(x => x.StateId);

		builder.Property(x => x.Code)
			.HasMaxLength(10)
			.IsRequired();

		builder.Property(a => a.Name)
			.HasMaxLength(150)
			.IsRequired();
	}
}