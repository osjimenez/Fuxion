namespace Fuxion.Linq.Filter.Operations;

public interface IHasFlagFilterOperation<T>
{
	T? HasFlag { get; set; }
}