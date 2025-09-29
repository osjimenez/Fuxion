using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fuxion.Linq.Filter.Operations;

namespace Fuxion.Linq;

public abstract class GeneratedFilter<TEntity> : Filter<TEntity>
{
	protected static readonly Expression TrueConstant = Expression.Constant(true);
	protected static readonly Expression FalseConstant = Expression.Constant(false);

	internal static readonly MethodInfo ContainsGeneric = typeof(Enumerable).GetMethods()
		.First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
	public static readonly MethodInfo AnyMethod = typeof(Enumerable).GetMethods()
		.First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2);
	public static readonly MethodInfo AllMethod = typeof(Enumerable).GetMethods()
		.First(m => m.Name == nameof(Enumerable.All) && m.GetParameters().Length == 2);

	private static readonly MethodInfo _toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
	private static readonly MethodInfo _contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
	private static readonly MethodInfo _startsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
	private static readonly MethodInfo _endsWith = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;

	// Must be protected to be used in code-generated
	protected static ParameterExpression Parameter<T>(string name) => Expression.Parameter(typeof(T), name);
	// Must be protected to be used in code-generated
	protected static Expression Access(Expression instance, string name) => Expression.Property(instance, name);

	protected internal static Expression And(Expression left, Expression right)
	{
		if (left == TrueConstant) return right;
		if (right == TrueConstant) return left;
		return Expression.AndAlso(left, right);
	}
	protected internal static Expression Or(Expression left, Expression right)
	{
		if (left == FalseConstant) return right;
		if (right == FalseConstant) return left;
		return Expression.OrElse(left, right);
	}
	protected static Expression ApplyNavigation(LambdaExpression nested, Expression navigationAccess, bool nullCheck)
	{
		var param = nested.Parameters.Single();
		var replacer = new ReplaceParameterVisitor(param, navigationAccess);
		var body = replacer.Visit(nested.Body)!;
		if (nullCheck && navigationAccess.Type.IsClass)
		{
			var notNull = Expression.NotEqual(navigationAccess, Expression.Constant(null, navigationAccess.Type));
			body = Expression.AndAlso(notNull, body);
		}
		return body;
	}
	/// <summary>
	/// Aplica operaciones de un nodo de filtro sobre una propiedad escalar (nombre de propiedad) devolviendo la expresión resultante.
	/// </summary>
	protected static Expression ApplyProperty<T>(IFilterOperation<T> ops, ParameterExpression root, string propertyName)
		=> ApplyProperty((FilterOperations<T>)ops, Access(root, propertyName));
	/// <summary>
	/// Aplica operaciones de un nodo sobre una expresión (propiedad o computada) ya construida.
	/// </summary>
	protected static Expression ApplyProperty<T>(IFilterOperation<T> ops, Expression selector)
		=> BuildOperations((FilterOperations<T>)ops, selector);
	/// <summary>
	/// Aplica operaciones de un nodo de filtro sobre una expresión computada definida por un lambda relativo al root.
	/// </summary>
	protected static Expression ApplyComputed<T>(IFilterOperation<T> ops, Expression<Func<TEntity, T>> selector, ParameterExpression root)
		=> ApplyProperty(ops, Computed(selector, root));

	/// <summary>
	/// Construye la expresión para una colección escalar aplicando bloques AnyAnd, AnyOr y All.
	/// </summary>
	protected static Expression ApplyScalarCollection<TElement>(Expression collectionAccess, IReadOnlyList<FilterOperations<TElement>> anyAnd, IReadOnlyList<FilterOperations<TElement>> anyOr, IReadOnlyList<FilterOperations<TElement>> all)
	{
		// EF6 no soporta comparar colecciones con null; evitamos null-checks en colecciones
		Expression result = TrueConstant;
		foreach (var blk in anyAnd)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var p = Parameter<TElement>("ean");
			var inner = BuildOperations(blk, p);
			var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(typeof(TElement)), collectionAccess, Expression.Lambda(inner, p));
			result = And(result, anyCall);
		}
		Expression? orAccum = null;
		foreach (var blk in anyOr)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var p = Parameter<TElement>("eor");
			var inner = BuildOperations(blk, p);
			var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(typeof(TElement)), collectionAccess, Expression.Lambda(inner, p));
			orAccum = orAccum == null ? anyCall : Or(orAccum, anyCall);
		}
		if (orAccum != null) result = And(result, orAccum);
		foreach (var blk in all)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var p = Parameter<TElement>("all");
			var inner = BuildOperations(blk, p);
			var allCall = Expression.Call(AllMethod.MakeGenericMethod(typeof(TElement)), collectionAccess, Expression.Lambda(inner, p));
			result = And(result, allCall);
		}
		return result;
	}
	protected static Expression ApplyScalarCollection<TElement>(Expression collectionAccess, ScalarCollectionFilterOperations<TElement> ops)
	{
		// EF6 no soporta comparar colecciones con null; evitamos null-checks en colecciones
		Expression result = TrueConstant;

		// Any AND groups
		foreach (var blk in ops.AnyAndBlocks)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var pAnyAnd = Parameter<TElement>("ean");
			var inner = BuildOperations(blk, pAnyAnd);
			var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(typeof(TElement)), collectionAccess, Expression.Lambda(inner, pAnyAnd));
			result = And(result, anyCall);
		}
		// Any OR groups (accumulate OR of Any(...))
		Expression? anyOrAccum = null;
		foreach (var blk in ops.AnyOrBlocks)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var pAnyOr = Parameter<TElement>("eor");
			var inner = BuildOperations(blk, pAnyOr);
			var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(typeof(TElement)), collectionAccess, Expression.Lambda(inner, pAnyOr));
			anyOrAccum = anyOrAccum == null ? anyCall : Or(anyOrAccum, anyCall);
		}
		if (anyOrAccum != null) result = And(result, anyOrAccum);

		// All: construct single predicate with a unified parameter
		var pAll = Parameter<TElement>("eall");
		Expression? allAndAccum = null;
		foreach (var blk in ops.AllAndBlocks)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var inner = BuildOperations(blk, pAll);
			allAndAccum = allAndAccum == null ? inner : And(allAndAccum, inner);
		}
		Expression? allOrAccum = null;
		foreach (var blk in ops.AllOrBlocks)
		{
			if (!blk.HasSomeOperationsDefined) continue;
			var inner = BuildOperations(blk, pAll);
			allOrAccum = allOrAccum == null ? inner : Or(allOrAccum, inner);
		}
		Expression? allPredicate = null;
		if (allAndAccum != null && allOrAccum != null) allPredicate = And(allAndAccum, allOrAccum);
		else if (allAndAccum != null) allPredicate = allAndAccum;
		else if (allOrAccum != null) allPredicate = allOrAccum;
		if (allPredicate != null)
		{
			var allCall = Expression.Call(AllMethod.MakeGenericMethod(typeof(TElement)), collectionAccess, Expression.Lambda(allPredicate, pAll));
			result = And(result, allCall);
		}

		return result;
	}

	protected static Expression BuildOperations<T>(FilterOperations<T> ops, Expression selector)
	{
		if (!ops.HasSomeOperationsDefined) return TrueConstant;
		Expression body = TrueConstant;
		var selType = selector.Type;
		var underlying = Nullable.GetUnderlyingType(selType);
		bool isNullable = underlying != null;
		var effectiveType = underlying ?? selType;
		Expression Val(T? v)
		{
			if (v == null) return Expression.Constant(null, selType);
			Expression c = Expression.Constant(v, typeof(T));
			if (isNullable) c = Expression.Convert(c, selType);
			return c;
		}
		Expression ValUnderlying(T? v)
		{
			if (v == null) return Expression.Constant(null, effectiveType);
			return Expression.Constant(v, effectiveType);
		}
		var hasValueExpr = isNullable ? Expression.Property(selector, "HasValue") : null;
		var valueExpr = isNullable ? (Expression)Expression.Property(selector, "Value") : selector;
		if (ops.IsNullSpecified)
		{
			var isNullExpr = Expression.Equal(selector, Expression.Constant(null, selType));
			body = And(body, ops.IsNull == true ? isNullExpr : Expression.Not(isNullExpr));
		}
		if (ops.IsNotNullSpecified)
		{
			var isNullExpr = Expression.Equal(selector, Expression.Constant(null, selType));
			body = And(body, ops.IsNotNull == true ? Expression.Not(isNullExpr) : isNullExpr);
		}
		if (ops.EqualSpecified)
			body = And(body, Expression.Equal(selector, Val(ops.Equal)));
		else
		{
			if (ops.NotEqualSpecified) body = And(body, Expression.NotEqual(selector, Val(ops.NotEqual)));
			if (ops.GreaterThanSpecified && ops.GreaterThan is not null)
			{
				var cmp = Expression.GreaterThan(valueExpr, ValUnderlying(ops.GreaterThan));
				body = And(body, isNullable ? Expression.AndAlso(hasValueExpr!, cmp) : cmp);
			}
			if (ops.GreaterOrEqualSpecified && ops.GreaterOrEqual is not null)
			{
				var cmp = Expression.GreaterThanOrEqual(valueExpr, ValUnderlying(ops.GreaterOrEqual));
				body = And(body, isNullable ? Expression.AndAlso(hasValueExpr!, cmp) : cmp);
			}
			if (ops.LessThanSpecified && ops.LessThan is not null)
			{
				var cmp = Expression.LessThan(valueExpr, ValUnderlying(ops.LessThan));
				body = And(body, isNullable ? Expression.AndAlso(hasValueExpr!, cmp) : cmp);
			}
			if (ops.LessOrEqualSpecified && ops.LessOrEqual is not null)
			{
				var cmp = Expression.LessThanOrEqual(valueExpr, ValUnderlying(ops.LessOrEqual));
				body = And(body, isNullable ? Expression.AndAlso(hasValueExpr!, cmp) : cmp);
			}
			if (ops.BetweenFromSpecified && ops.BetweenFrom is not null)
			{
				var cmp = Expression.GreaterThanOrEqual(valueExpr, ValUnderlying(ops.BetweenFrom));
				body = And(body, isNullable ? Expression.AndAlso(hasValueExpr!, cmp) : cmp);
			}
			if (ops.BetweenToSpecified && ops.BetweenTo is not null)
			{
				var cmp = Expression.LessThanOrEqual(valueExpr, ValUnderlying(ops.BetweenTo));
				body = And(body, isNullable ? Expression.AndAlso(hasValueExpr!, cmp) : cmp);
			}
			if (ops.InSpecified && ops.In is { Count: > 0 })
			{
				var method = ContainsGeneric.MakeGenericMethod(typeof(T));
				var inCall = Expression.Call(method, Expression.Constant(ops.In), selector);
				body = And(body, inCall);
			}
		}
		if (typeof(T) == typeof(string))
		{
			Expression sel = selector;
			if (ops.CaseInsensitiveSpecified && ops.CaseInsensitive == true) sel = Expression.Call(sel, _toLower);
			Expression Str(string? s) => (ops.CaseInsensitiveSpecified && ops.CaseInsensitive == true) ? Expression.Call(Expression.Constant(s, typeof(string)), _toLower) : Expression.Constant(s, typeof(string));
			if (ops.ContainsSpecified && ops.Contains is not null) body = And(body, Expression.Call(sel, _contains, Str(ops.Contains as string)));
			if (ops.StartsWithSpecified && ops.StartsWith is not null) body = And(body, Expression.Call(sel, _startsWith, Str(ops.StartsWith as string)));
			if (ops.EndsWithSpecified && ops.EndsWith is not null) body = And(body, Expression.Call(sel, _endsWith, Str(ops.EndsWith as string)));
			if (ops.EmptySpecified) { var emptyCmp = Expression.Equal(selector, Expression.Constant("")); body = And(body, ops.Empty == true ? emptyCmp : Expression.Not(emptyCmp)); }
			if (ops.NotEmptySpecified) { var emptyCmp = Expression.Equal(selector, Expression.Constant("")); body = And(body, ops.NotEmpty == true ? Expression.Not(emptyCmp) : emptyCmp); }
		}
		if (ops.HasFlagSpecified && ops.HasFlag is not null && typeof(T).IsEnum)
		{
			var hasFlagExpr = Expression.Call(Expression.Convert(selector, typeof(Enum)), typeof(Enum).GetMethod(nameof(Enum.HasFlag))!, Expression.Convert(Expression.Constant(ops.HasFlag), typeof(Enum)));
			body = And(body, hasFlagExpr);
		}
		return body;
	}
	protected static Expression ReplaceParameter(LambdaExpression lambda, ParameterExpression newParam)
	{
		var old = lambda.Parameters[0];
		var replacer = new ReplaceParameterVisitor(old, newParam);
		return replacer.Visit(lambda.Body)!;
	}
	protected static Expression Computed<TValue>(Expression<Func<TEntity, TValue>> selector, ParameterExpression root)
		=> ReplaceParameter(selector, root);

	protected static Expression ApplyNavigationCollection<TChildFilter, TChildEntity>(Expression collectionAccess, NavigationCollectionFilterOperations<TChildFilter, TChildEntity> ops)
		where TChildFilter : GeneratedFilter<TChildEntity>, new()
	{
		// EF6 no soporta comparar colecciones con null; evitamos null-checks en colecciones
		Expression result = TrueConstant;
		// Any AND
		foreach (var blk in ops.AnyAndBlocks)
		{
			if (!blk.IsDefined) continue;
			var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(typeof(TChildEntity)), collectionAccess, blk.Predicate);
			result = And(result, anyCall);
		}
		// Any OR
		Expression? anyOrAccum = null;
		foreach (var blk in ops.AnyOrBlocks)
		{
			if (!blk.IsDefined) continue;
			var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(typeof(TChildEntity)), collectionAccess, blk.Predicate);
			anyOrAccum = anyOrAccum == null ? anyCall : Or(anyOrAccum, anyCall);
		}
		if (anyOrAccum != null) result = And(result, anyOrAccum);
		// All: P(e) = AND(AllAnd) AND OR(AllOr)
		Expression? allAndAccum = null;
		foreach (var blk in ops.AllAndBlocks)
		{
			if (!blk.IsDefined) continue;
			allAndAccum = allAndAccum == null ? blk.Predicate.Body : And(allAndAccum, blk.Predicate.Body);
		}
		Expression? allOrAccum = null;
		foreach (var blk in ops.AllOrBlocks)
		{
			if (!blk.IsDefined) continue;
			allOrAccum = allOrAccum == null ? blk.Predicate.Body : Or(allOrAccum, blk.Predicate.Body);
		}
		Expression? allPredicate = null;
		if (allAndAccum != null && allOrAccum != null) allPredicate = And(allAndAccum, allOrAccum);
		else if (allAndAccum != null) allPredicate = allAndAccum;
		else if (allOrAccum != null) allPredicate = allOrAccum;
		if (allPredicate != null)
		{
			// Necesitamos unificar el parámetro del lambda de cada Predicate
			var p = Parameter<TChildEntity>("ce");
			Expression Rebind(LambdaExpression lam)
			{
				var repl = new ReplaceParameterVisitor(lam.Parameters[0], p);
				return repl.Visit(lam.Body)!;
			}
			Expression? andBody = null;
			foreach (var blk in ops.AllAndBlocks) if (blk.IsDefined) andBody = andBody == null ? Rebind(blk.Predicate) : And(andBody, Rebind(blk.Predicate));
			Expression? orBody = null;
			foreach (var blk in ops.AllOrBlocks) if (blk.IsDefined) orBody = orBody == null ? Rebind(blk.Predicate) : Or(orBody, Rebind(blk.Predicate));
			Expression? final = null;
			if (andBody != null && orBody != null) final = And(andBody, orBody);
			else final = andBody ?? orBody;
			var allCall = Expression.Call(AllMethod.MakeGenericMethod(typeof(TChildEntity)), collectionAccess, Expression.Lambda(final!, p));
			result = And(result, allCall);
		}
		return result;
	}

	//protected abstract bool HasSomeOperationsDefined { get; }// => ((IFilterOperationsNode)this).HasSomeOperationsDefined;
	//bool IFilterOperationsNode.HasSomeOperationsDefined { get; }
	private sealed class ReplaceParameterVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
	}
}