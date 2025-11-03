using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Fuxion.Linq;

public static class FilterExtensions
{
	public static IQueryable<TValue> Filter<TValue>(this IQueryable<TValue> query, Filter<TValue>? filter)
		=> filter is null ? query : query.Where(filter.Predicate);

	public static IQueryable<TValue> Filter<TValue, T1>(this IQueryable<TValue> query, Fil<TValue, T1>? filter, T1 arg)
		=> query;

	public static IQueryable<TValue> Filter<TValue, T1, T2>(this IQueryable<TValue> query, Fil<TValue, T1, T2>? filter, T1 arg1, T2 arg2)
		=> query;
}

public class Fil<TValue>;
public class Fil<TValue, T1> : Fil<TValue>;
public class Fil<TValue, T1, T2>;
public class Fil<TValue, T1, T2, T3>;
public class Fil<TValue, T1, T2, T3, T4>;
