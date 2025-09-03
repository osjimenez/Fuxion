using System.Linq;
using Fuxion.Linq.Test.Daos;

namespace Fuxion.Linq.Test;

public interface IDataContext
{
	IQueryable<UserDao> GetUsers();
}