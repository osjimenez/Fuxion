#if STANDARD_OR_OLD_FRAMEWORKS
using System.Data.Entity;
using System.Linq;
using Fuxion.Linq.Test.Daos;

namespace Fuxion.Linq.Test;

public class DataContextEF : DbContext, IDataContext
{
	public IQueryable<UserDao> GetUsers() => Users;
	public DbSet<UserDao> Users { get; set; } = null!;
}
#endif