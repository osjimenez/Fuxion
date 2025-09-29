namespace Fuxion.Linq.Filter.Operations;

public interface INullabilityFilterOperations : IIsNullFilterOperation, IIsNotNullFilterOperation;

public interface IIsNullFilterOperation : IFilterOperation
{
	bool? IsNull { get; set; }
}
public interface IIsNotNullFilterOperation : IFilterOperation
{
	bool? IsNotNull { get; set; }
}