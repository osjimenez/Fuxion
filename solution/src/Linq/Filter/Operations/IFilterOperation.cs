namespace Fuxion.Linq.Filter.Operations;

public interface IFilterOperation
{
	bool IsDefined { get; }
}
public interface IFilterOperation<T> : IFilterOperation;