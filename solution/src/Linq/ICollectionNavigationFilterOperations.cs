namespace Fuxion.Linq;

// Operaciones sobre colecciones de navegación (IEnumerable<TChildEntity>) cuyos elementos tienen filtro TChildFilter.
public interface ICollectionNavigationFilterOperations<TChildFilter> : IFilterOperations<TChildFilter> where TChildFilter : IFilter
{
	void Any(params System.Action<TChildFilter>[] and);
	void Any(System.Collections.Generic.IEnumerable<System.Action<TChildFilter>>? and = null,
			 System.Collections.Generic.IEnumerable<System.Action<TChildFilter>>? or = null);

	void All(params System.Action<TChildFilter>[] and);
	void All(System.Collections.Generic.IEnumerable<System.Action<TChildFilter>>? and = null,
			 System.Collections.Generic.IEnumerable<System.Action<TChildFilter>>? or = null);
}
