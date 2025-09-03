//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Numerics;
//using System.Text.Json.Serialization;

//namespace Fuxion.Linq.Expressions;

//public interface IFilter;

//public abstract class Filter<TValue> : IFilter
//{
//	[JsonIgnore] public abstract Expression<Func<TValue, bool>> Predicate { get; }

//	public static Expression<Func<TValue, bool>> ApplyFilter<TFilterValue>(Filter<TFilterValue>? filter, Expression<Func<TValue, TFilterValue>> selector)
//		=> filter?.Predicate is null
//			? x => true
//			: Expression.Lambda<Func<TValue, bool>>(Expression.Invoke(filter.Predicate, selector.Body), selector.Parameters);
//}

//public interface IFilterCombination<TFilter>
//	where TFilter : IFilter
//{
//	ICollection<TFilter>? List { get; internal set; }
//}

//public abstract class AndCombinationFilter<TFilter, TValue> : Filter<TValue>, IFilterCombination<TFilter>
//	where TFilter : Filter<TValue>
//{
//	[JsonIgnore] public abstract Expression<Func<TValue, bool>> AndPredicate { get; }

//	[JsonIgnore]
//	public sealed override Expression<Func<TValue, bool>> Predicate
//	{
//		get
//		{
//			if (((IFilterCombination<TFilter>)this).List is null) return AndPredicate;

//			Expression<Func<TValue, bool>>? current = null;
//			foreach (var filter in ((IFilterCombination<TFilter>)this).List)
//				current = current is null
//					? filter.Predicate
//					: current.And(filter.Predicate);

//			return current is null
//				? AndPredicate
//				: AndPredicate.And(current);
//		}
//	}

//	ICollection<TFilter>? IFilterCombination<TFilter>.List { get; set; }
//}

//public abstract class OrCombinationFilter<TFilter, TValue> : Filter<TValue>, IFilterCombination<TFilter>
//	where TFilter : OrCombinationFilter<TFilter, TValue>
//{
//	[JsonIgnore] public abstract Expression<Func<TValue, bool>> OrPredicate { get; }

//	[JsonIgnore]
//	[Browsable(false)]
//	public sealed override Expression<Func<TValue, bool>> Predicate
//	{
//		get
//		{
//			if (List is null) return OrPredicate;

//			if (IsInClausuleCompliant && List.All(f => f.IsInClausuleCompliant))
//			{
//				List<TValue> inList = [];
//				inList.Add(GetValue());
//				inList.AddRange(List.Select(f => f.GetValue()));
//				return x => inList.Contains(x);
//			}

//			Expression<Func<TValue, bool>>? current = null;
//			foreach (var filter in List)
//				current = current is null
//					? filter.Predicate
//					: current.Or(filter.Predicate);

//			return current is null
//				? OrPredicate
//				: OrPredicate.Or(current);
//		}
//	}
//	public abstract bool IsInClausuleCompliant { get; }
//	public abstract TValue GetValue();

//	[Browsable(false)]
//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public ICollection<TFilter>? List { get; set; }
//}
//#if !STANDARD_OR_OLD_FRAMEWORKS
//internal static class FilterExpressionBuilder
//{
//	public static Expression<Func<T?, bool>> BuildNullableBinary<T>(
//		ExpressionType op,
//		T value,
//		bool requireNotNullGuard
//	) where T : struct, INumber<T>
//	{
//		var p = Expression.Parameter(typeof(T?), "x");
//		var constValue = Expression.Constant(value, typeof(T));
//		if (op == ExpressionType.Equal || op == ExpressionType.NotEqual)
//		{
//			// Lifted comparison: x == (T?)value
//			var lifted = Expression.Convert(constValue, typeof(T?));
//			var body = op == ExpressionType.Equal
//				? Expression.Equal(p, lifted)
//				: Expression.NotEqual(p, lifted);
//			return Expression.Lambda<Func<T?, bool>>(body, p);
//		}
//		var notNull = Expression.NotEqual(p, Expression.Constant(null, typeof(T?)));
//		var unwrapped = Expression.Convert(p, typeof(T));
//		var cmp = op switch
//		{
//			ExpressionType.GreaterThan => Expression.GreaterThan(unwrapped, constValue),
//			ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(unwrapped, constValue),
//			ExpressionType.LessThan => Expression.LessThan(unwrapped, constValue),
//			ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(unwrapped, constValue),
//			_ => throw new NotSupportedException(op.ToString())
//		};
//		var bodyWithNull = requireNotNullGuard ? Expression.AndAlso(notNull, cmp) : cmp;
//		return Expression.Lambda<Func<T?, bool>>(bodyWithNull, p);
//	}

//	public static Expression<Func<T?, bool>> BuildIsNullFlag<T>(bool flag) where T : struct, INumber<T>
//	{
//		var p = Expression.Parameter(typeof(T?), "x");
//		var isNull = Expression.Equal(p, Expression.Constant(null, typeof(T?)));
//		var flagConst = Expression.Constant(flag);
//		var body = Expression.Condition(isNull, flagConst, Expression.Not(flagConst));
//		return Expression.Lambda<Func<T?, bool>>(body, p);
//	}
//}

//public class NumberFilter<T> : OrCombinationFilter<NumberFilter<T>, T?> where T : struct, INumber<T>
//{
//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberEqualFilter<T>? Equal { get; init; }

//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberNotEqualFilter<T>? NotEqual { get; init; }

//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberGraterThanFilter<T>? GraterThan { get; init; }

//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberGraterOrEqualThanFilter<T>? GraterOrEqualThan { get; init; }

//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberLessThanFilter<T>? LessThan { get; init; }

//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberLessOrEqualThanFilter<T>? LessOrEqualThan { get; init; }

//	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//	public NumberIsNullFilter<T>? IsNull { get; init; }

//	public override bool IsInClausuleCompliant =>
//		Equal is not null
//		&& NotEqual is null
//		&& GraterThan is null
//		&& GraterOrEqualThan is null
//		&& LessThan is null
//		&& LessOrEqualThan is null
//		&& IsNull is null;
//	public override T? GetValue() => Equal?.Filter;

//	[JsonIgnore]
//	public override Expression<Func<T?, bool>> OrPredicate
//	{
//		get
//		{
//			var predicate = Equal.PredicateOrTrue()
//				.And(NotEqual.PredicateOrTrue())
//				.And(GraterThan.PredicateOrTrue())
//				.And(GraterOrEqualThan.PredicateOrTrue())
//				.And(LessThan.PredicateOrTrue())
//				.And(LessOrEqualThan.PredicateOrTrue())
//				.And(IsNull.PredicateOrTrue());
//			return predicate;
//			//return x => predicate.Invoke(x);
//		}
//	}

//	public static implicit operator NumberFilter<T>(T x) => new() { Equal = x };

//	public class NumberEqualFilter<R> : OrCombinationFilter<NumberEqualFilter<R>, R?> where R : struct, INumber<R>
//	{
//		public required R Filter { get; init; }
//		public override bool IsInClausuleCompliant => true;
//		public override R? GetValue() => Filter;
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> OrPredicate
//			=> FilterExpressionBuilder.BuildNullableBinary<R>(ExpressionType.Equal, Filter, requireNotNullGuard: false);

//		public static implicit operator NumberEqualFilter<R>(R x) => new() { Filter = x };
//	}

//	public class NumberNotEqualFilter<R> : AndCombinationFilter<NumberNotEqualFilter<R>, R?> where R : struct, INumber<R>
//	{
//		public required R Filter { get; init; }
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> AndPredicate
//			=> FilterExpressionBuilder.BuildNullableBinary<R>(ExpressionType.NotEqual, Filter, requireNotNullGuard: false);
//		public static implicit operator NumberNotEqualFilter<R>(R x) => new() { Filter = x };
//	}

//	public class NumberGraterThanFilter<R> : OrCombinationFilter<NumberGraterThanFilter<R>, R?> where R : struct, INumber<R>
//	{
//		public required R Filter { get; init; }
//		public override bool IsInClausuleCompliant => true;
//		public override R? GetValue() => Filter;
//		//[JsonIgnore] public override Expression<Func<R?, bool>> OrPredicate => x => x != null && x > Filter;
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> OrPredicate
//			=> FilterExpressionBuilder.BuildNullableBinary<R>(ExpressionType.GreaterThan, Filter, requireNotNullGuard: true);

//		public static implicit operator NumberGraterThanFilter<R>(R x) => new() { Filter = x };
//	}

//	public class NumberGraterOrEqualThanFilter<R> : OrCombinationFilter<NumberGraterOrEqualThanFilter<R>, R?> where R : struct, INumber<R>
//	{
//		public required R Filter { get; init; }
//		public override bool IsInClausuleCompliant => true;
//		public override R? GetValue() => Filter;
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> OrPredicate
//			=> FilterExpressionBuilder.BuildNullableBinary<R>(ExpressionType.GreaterThanOrEqual, Filter, requireNotNullGuard: true);
//		public static implicit operator NumberGraterOrEqualThanFilter<R>(R x) => new() { Filter = x };
//	}

//	public class NumberLessThanFilter<R> : OrCombinationFilter<NumberLessThanFilter<R>, R?> where R : struct, INumber<R>
//	{
//		public required R Filter { get; init; }
//		public override bool IsInClausuleCompliant => true;
//		public override R? GetValue() => Filter;
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> OrPredicate
//			=> FilterExpressionBuilder.BuildNullableBinary<R>(ExpressionType.LessThan, Filter, requireNotNullGuard: true);
//		public static implicit operator NumberLessThanFilter<R>(R x) => new() { Filter = x };
//	}

//	public class NumberLessOrEqualThanFilter<R> : OrCombinationFilter<NumberLessOrEqualThanFilter<R>, R?> where R : struct, INumber<R>
//	{
//		public required R Filter { get; init; }
//		public override bool IsInClausuleCompliant => true;
//		public override R? GetValue() => Filter;
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> OrPredicate
//			=> FilterExpressionBuilder.BuildNullableBinary<R>(ExpressionType.LessThanOrEqual, Filter, requireNotNullGuard: true);
//		public static implicit operator NumberLessOrEqualThanFilter<R>(R x) => new() { Filter = x };
//	}

//	public class NumberIsNullFilter<R> : Filter<R?> where R : struct, INumber<R>
//	{
//		public required bool Filter { get; init; }
//		[JsonIgnore]
//		public override Expression<Func<R?, bool>> Predicate
//			=> FilterExpressionBuilder.BuildIsNullFlag<R>(Filter);
//		public static implicit operator NumberIsNullFilter<R>(bool x) => new() { Filter = x };
//	}
//}
//#endif
//public static class FilterExtensions
//{
//	public static IQueryable<TValue> Filter<TValue>(this IQueryable<TValue> query, Filter<TValue>? filter)
//		=> filter is null
//			? query
//			: query.Where(filter.Predicate);

//	public static Expression<Func<TValue, bool>> PredicateOrTrue<TValue>(this Filter<TValue>? me) => me?.Predicate ?? (x => true);
//}
