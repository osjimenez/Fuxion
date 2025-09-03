using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fuxion.Linq;

public interface IFilterDescriptor<TEntity>
{
	string Name { get; }
	string? ExternalName { get; }
	IReadOnlyList<string> Aliases { get; }
	OperationKind AllowedOperations { get; }
	NullHandling NullHandling { get; }
	LambdaExpression Selector { get; }
	IReadOnlyList<LambdaExpression>? ExtraSelectors { get; }
	bool CaseInsensitive { get; }
	bool EnableInlining { get; }
	IReadOnlyDictionary<string, object>? Metadata { get; }
}