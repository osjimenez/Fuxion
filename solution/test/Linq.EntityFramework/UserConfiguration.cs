using System.Data.Entity.ModelConfiguration;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFramework;

public class UserConfiguration : EntityTypeConfiguration<UserDao>
{
	public UserConfiguration()
	{
		ToTable("Users");

		HasKey(x => x.UserId);

		Property(x => x.FirstName)
			.HasMaxLength(150)
			.IsRequired();

		Property(x => x.LastName)
			.HasMaxLength(150);
	}
}