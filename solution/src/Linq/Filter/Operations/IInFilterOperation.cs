using System.Collections.Generic;

namespace Fuxion.Linq.Filter.Operations;

public interface IInFilterOperation<T>
{
	IReadOnlyCollection<T>? In { get; set; }
}