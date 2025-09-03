using System.Collections.Generic;

namespace Fuxion.Linq;

public interface ISetFilterOperations<T>
{
	IReadOnlyCollection<T>? In { get; set; }
}