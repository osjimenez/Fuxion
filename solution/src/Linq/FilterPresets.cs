namespace Fuxion.Linq;

public static class FilterPresets
{
	public static OperationKind Comparable => OperationKind.Equal | OperationKind.NotEqual | OperationKind.GreaterThan |
	                                          OperationKind.GreaterOrEqual | OperationKind.LessThan |
	                                          OperationKind.LessOrEqual | OperationKind.Between | OperationKind.In;

	public static OperationKind Text => OperationKind.Equal | OperationKind.NotEqual | OperationKind.In |
	                                    OperationKind.Contains | OperationKind.StartsWith | OperationKind.EndsWith |
	                                    OperationKind.IsNull | OperationKind.Empty | OperationKind.NotEmpty;

	public static OperationKind Enum => OperationKind.Equal | OperationKind.NotEqual | OperationKind.In |
	                                    OperationKind.HasFlag | OperationKind.IsNull;
}