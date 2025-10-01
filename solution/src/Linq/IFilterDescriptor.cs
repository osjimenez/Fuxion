using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fuxion.Linq;

public interface IFilterDescriptor<TEntity>
{
	bool CaseInsensitive { get; }
}