namespace Fuxion.Linq.Filter.Operations;

public interface IRelationalFilterOperations<T> :
	IGreaterThanFilterOperation<T>,
	IGreaterOrEqualFilterOperation<T>,
	ILessThanFilterOperation<T>,
	ILessOrEqualFilterOperation<T>,
	IBetweenFilterOperation<T>;

public interface IGreaterThanFilterOperation<T> : IFilterOperation<T>
{
	T? GreaterThan { get; set; }
}
public interface IGreaterOrEqualFilterOperation<T> : IFilterOperation<T>
{
	T? GreaterOrEqual { get; set; }
}
public interface ILessThanFilterOperation<T> : IFilterOperation<T>
{
	T? LessThan { get; set; }
}
public interface ILessOrEqualFilterOperation<T> : IFilterOperation<T>
{
	T? LessOrEqual { get; set; }
}
public interface IBetweenFilterOperation<T> : IFilterOperation<T>
{
	T? BetweenFrom { get; set; }
	T? BetweenTo { get; set; }
}