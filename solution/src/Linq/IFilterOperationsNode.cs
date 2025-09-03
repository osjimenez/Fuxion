namespace Fuxion.Linq;

public interface IFilterOperationsNode
{
	bool HasAny();
}

public interface IFilterOperations<T> : IFilterOperationsNode
{
	// Marker genérico; las operaciones concretas están en interfaces específicas (IEqualityFilterOperations, etc.)
}