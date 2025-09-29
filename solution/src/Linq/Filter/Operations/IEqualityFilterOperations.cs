namespace Fuxion.Linq.Filter.Operations;

public interface IEqualityFilterOperations<T> :
	IEqualFilterOperation<T>,
	INotEqualFilterOperation<T>;

public interface IEqualFilterOperation<T>
{
	T? Equal { get; set; }
}
public interface INotEqualFilterOperation<T>
{
	T? NotEqual { get; set; }
}