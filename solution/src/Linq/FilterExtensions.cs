using System.Linq;

namespace Fuxion.Linq;

public static class FilterExtensions
{
	public static IQueryable<TValue> Filter<TValue>(this IQueryable<TValue> query, Filter<TValue>? filter)
		=> filter is null ? query : query.Where(filter.Predicate);
}
