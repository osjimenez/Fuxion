namespace Fuxion.Linq;

// Operaciones sobre colecciones escalares (IEnumerable<TElement>)
public interface ICollectionScalarFilterOperations<TElement> : IFilterOperations<TElement>
{
	// Any con combinaci�n interna de bloques AND/OR
	void Any(params System.Action<FilterOperations<TElement>>[] and);
	void Any(System.Collections.Generic.IEnumerable<System.Action<FilterOperations<TElement>>>? and = null,
			 System.Collections.Generic.IEnumerable<System.Action<FilterOperations<TElement>>>? or = null);

	// All con combinaci�n interna de bloques AND/OR
	void All(params System.Action<FilterOperations<TElement>>[] and);
	void All(System.Collections.Generic.IEnumerable<System.Action<FilterOperations<TElement>>>? and = null,
			 System.Collections.Generic.IEnumerable<System.Action<FilterOperations<TElement>>>? or = null);
}
