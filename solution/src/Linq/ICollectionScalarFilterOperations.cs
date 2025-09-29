using System;
using System.Collections.Generic;
using Fuxion.Linq.Filter.Operations;

namespace Fuxion.Linq;

using System.Xml.Linq;

// Operaciones sobre colecciones escalares (IEnumerable<TElement>)
public interface ICollectionScalarFilterOperations<TElement> : IFilterOperation<TElement>
{
	// Any con combinación interna de bloques AND/OR
	void Any(params Action<FilterOperations<TElement>>[] and);
	void Any(IEnumerable<Action<FilterOperations<TElement>>>? and = null,
			 IEnumerable<Action<FilterOperations<TElement>>>? or = null);

	// All con combinación interna de bloques AND/OR
	void All(params Action<FilterOperations<TElement>>[] and);
	void All(IEnumerable<Action<FilterOperations<TElement>>>? and = null,
			 IEnumerable<Action<FilterOperations<TElement>>>? or = null);
}

public interface IAnyFilterOperation<T, TOperations> : IFilterOperation<TOperations> where TOperations : IFilterOperation<T>
{
	void Any(params Action<TOperations>[] and);
	void Any(
		IEnumerable<Action<TOperations>>? and = null,
		IEnumerable<Action<TOperations>>? or = null);
}
public interface IAllFilterOperation<TOperation> : IFilterOperation<TOperation> where TOperation : IFilterOperation
{
	void All(params Action<TOperation>[] and);
	void All(
		IEnumerable<Action<TOperation>>? and = null,
		IEnumerable<Action<TOperation>>? or = null);
}

public class Algo
{
	public void Bueno()
	{
		var a = new Algo();
		a.Phones.Any(p => p.Equal = "1");
	}


	public interface IPhonesOperations :
		IFilterOperation<string>,
		IEqualityFilterOperations<string>,
		//IInFilterOperation<string>,
		//IRelationalFilterOperations<string>,
		//INullabilityFilterOperations,
		ITextFilterOperations<string>
	{ }
	public sealed class PhonesOperations : FilterOperations<string>, IPhonesOperations { }

	public interface IPhonesCollectionOperations : IAnyFilterOperation<string, IPhonesOperations>;
	//public sealed class PhonesCollectionOperations : FilterOperations<string>, IPhonesCollectionOperations;
	public IPhonesCollectionOperations Phones { get; } = null!;


}