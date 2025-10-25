namespace Fuxion.Linq.Test.Data.Daos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class BaseDao
{
	public required DateTime UpdatedAtUtc { get; set; }
}
