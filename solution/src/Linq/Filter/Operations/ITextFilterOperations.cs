namespace Fuxion.Linq.Filter.Operations;

public interface ITextFilterOperations<T>
	: IContainsFilterOperation<T>,
		IStartsWithFilterOperation<T>,
		IEndsWithFilterOperation<T>,
		IEmptyFilterOperation,
		INotEmptyFilterOperation,
		ICaseInsensitiveFilterOperation;
public interface IContainsFilterOperation<T> : IFilterOperation<T>
{
	T? Contains { get; set; }
}

public interface IStartsWithFilterOperation<T> : IFilterOperation<T>
{
	T? StartsWith { get; set; }
}

public interface IEndsWithFilterOperation<T> : IFilterOperation<T>
{
	T? EndsWith { get; set; }
}

public interface IEmptyFilterOperation : IFilterOperation
{
	bool? Empty { get; set; }
}

public interface INotEmptyFilterOperation : IFilterOperation
{
	bool? NotEmpty { get; set; }
}

public interface ICaseInsensitiveFilterOperation : IFilterOperation
{
	bool? CaseInsensitive { get; set; }
}