#if !STANDARD_OR_OLD_FRAMEWORKS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fuxion;
using Fuxion.Reflection;
using Fuxion.Xunit;
using Microsoft.EntityFrameworkCore;
using Test.Dataset;
using Test.Dataset.Daos;
using Test.Dataset.EntityFrameworkCore.SqlServer;
using Xunit;

namespace Test.Linq;

public interface IPropertyFilter2<TValue>
{
	Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector);
}

public interface IEntityFilter2<TEntity>
{
	Expression<Func<TEntity, bool>> Predicate { get; }
}

public class PropertyFilterDescriptor2Attribute(string canApply, string buildMethod) : Attribute
{
	public string BuildMethod { get; set; } = buildMethod;
	public string CanApply { get; set; } = canApply;
}

public class EntityFilterDescriptor2Attribute(string member) : Attribute
{
	public string Member { get; } = member;
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildEqualPredicate))]
public interface IEqualPropertyFilter2<TValue> : IPropertyFilter2<TValue>
{
	TValue? Equal { get; set; }
	
	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(IEquatable<>));

	Expression<Func<TEntity, bool>> BuildEqualPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector)
	{
		if(Equal is null)
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);
		return Expression.Lambda<Func<TEntity, bool>>(
			Expression.Equal(
				selector.Body,
				Expression.Constant(Equal, typeof(TValue))
			),
			selector.Parameters
		);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildNotEqualPredicate))]
public interface INotEqualPropertyFilter2<TValue> : IPropertyFilter2<TValue>
{
	TValue? NotEqual { get; set; }
	
	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(IEquatable<>));

	Expression<Func<TEntity, bool>> BuildNotEqualPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector)
	{
		if (NotEqual is null)
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		var left = selector.Body;
		var right = Expression.Constant(NotEqual, typeof(TValue));

		// Null-aware para tipos referencia o Nullable<T>:
		var isNullable = left.Type.IsClass || Nullable.GetUnderlyingType(left.Type) is not null;
		var body = isNullable
			? Expression.OrElse(
				Expression.Equal(left, Expression.Constant(null, left.Type)),
				Expression.NotEqual(left, right))
			: Expression.NotEqual(left, right);

		return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildInPredicate))]
public interface IInPropertyFilter2<TValue> : IPropertyFilter2<TValue>
{
	private static readonly MethodInfo EnumerableContainsMethodInfo = typeof(Enumerable).GetMethods()
		.First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

	IReadOnlyCollection<TValue>? In { get; set; }

	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(IEquatable<>));

	Expression<Func<TEntity, bool>> BuildInPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector)
	{
		if (In is not { Count: > 0 })
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		return Expression.Lambda<Func<TEntity, bool>>(
			Expression.Call(
				EnumerableContainsMethodInfo.MakeGenericMethod(typeof(TValue)),
				Expression.Constant(In, typeof(IEnumerable<TValue>)),
				selector.Body
			),
			selector.Parameters
		);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildGreaterThanPredicate))]
public interface IGreaterThanPropertyFilter2<TValue> : IPropertyFilter2<TValue>
{
	TValue? GreaterThan { get; set; }


	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(IComparable<>));

	Expression<Func<TEntity, bool>> BuildGreaterThanPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector)
	{
		// If no value, return true (no-op)
		if (GreaterThan is null)
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		var left = selector.Body;                                        // can be nullable
		var constRight = Expression.Constant(GreaterThan, typeof(TValue));

		// If left is nullable, lift: left != null && left.Value.CompareTo(right) > 0
		var isNullable = Nullable.GetUnderlyingType(left.Type) is not null || left.Type.IsClass;
		var nnLeftType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;

		// Right must be non-nullable for CompareTo
		var nnRightType = Nullable.GetUnderlyingType(constRight.Type) ?? constRight.Type;
		Expression right = nnRightType == constRight.Type ? constRight : Expression.Convert(constRight, nnRightType);

		// leftNN = left or left.Value to call CompareTo(T)
		Expression leftNN = left;
		if (Nullable.GetUnderlyingType(left.Type) is not null)
			leftNN = Expression.Property(left, "Value");

		var compareTo = nnLeftType.GetMethod(nameof(IComparable<object>.CompareTo), new[] { nnRightType });
		if (compareTo is null)
			throw new NotSupportedException($"Type {nnLeftType} doesn’t implement CompareTo({nnRightType}).");

		var cmp = Expression.Call(leftNN, compareTo, right);
		var gt = Expression.GreaterThan(cmp, Expression.Constant(0));

		var body = isNullable
			? Expression.AndAlso(Expression.NotEqual(left, Expression.Constant(null, left.Type)), gt)
			: gt;

		return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
	}
}


[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildOrPredicate))]
public interface IOrPropertyFilter2<TPropertyFilter, TValue> : IPropertyFilter2<TValue>
	where TPropertyFilter : IPropertyFilter2<TValue>
{
	IList<TPropertyFilter>? Or { get; set; }
	static bool CanApply(object _) => true;

	Expression<Func<TEntity, bool>> BuildOrPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector)
	{
		if (Or is not { Count: > 0 })
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		Expression? body = null;
		foreach (var child in Or)
		{
			var part = child.BuildPropertyPredicate<TEntity>(selector).Body;
			body = body is null ? part : Expression.OrElse(body, part);
		}

		return Expression.Lambda<Func<TEntity, bool>>(body!, selector.Parameters);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildAndPredicate))]
public interface IAndPropertyFilter2<TPropertyFilter, TValue> : IPropertyFilter2<TValue>
	where TPropertyFilter : IPropertyFilter2<TValue>
{
	IList<TPropertyFilter>? And { get; set; }

	static bool CanApply(object _) => true;

	Expression<Func<TEntity, bool>> BuildAndPredicate<TEntity>(Expression<Func<TEntity, TValue>> selector)
	{
		if (And is not { Count: > 0 })
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		Expression? body = null;
		foreach (var child in And)
		{
			var part = child.BuildPropertyPredicate<TEntity>(selector).Body;
			body = body is null ? part : Expression.AndAlso(body, part);
		}

		return Expression.Lambda<Func<TEntity, bool>>(body!, selector.Parameters);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildAnyPredicate))]
public interface IAnyPropertyFilter2<TPropertyFilter, TValue> : IPropertyFilter2<ICollection<TValue>>
	where TPropertyFilter : IPropertyFilter2<TValue>
{
	TPropertyFilter? Any { get; set; }

	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(ICollection<>));

	Expression<Func<TEntity, bool>> BuildAnyPredicate<TEntity>(Expression<Func<TEntity, ICollection<TValue>>> selector)
	{
		// Sin criterio -> true
		if (Any is null)
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		var entityParam = selector.Parameters[0];
		var collection = selector.Body;

		// p => p  (selector identidad del elemento)
		var p = Expression.Parameter(typeof(TValue), "p");
		var elementSelector = Expression.Lambda<Func<TValue, TValue>>(p, p);

		// Predicado de elemento a partir del filtro 'Any'
		var elementPredicate = Any.BuildPropertyPredicate(elementSelector);

		// Enumerable.Any<T>(IEnumerable<T>, Func<T,bool>)
		var anyMethod = typeof(Enumerable).GetMethods()
			.First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
			.MakeGenericMethod(typeof(TValue));

		var anyCall = Expression.Call(anyMethod, collection, elementPredicate);

		// null-check de la colección
		var notNull = Expression.NotEqual(collection, Expression.Constant(null, collection.Type));
		var body = Expression.AndAlso(notNull, anyCall);

		return Expression.Lambda<Func<TEntity, bool>>(body, entityParam);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildAllPredicate))]
public interface IAllPropertyFilter2<TPropertyFilter, TValue> : IPropertyFilter2<ICollection<TValue>>
	where TPropertyFilter : IPropertyFilter2<TValue>
{
	TPropertyFilter? All { get; set; }

	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(ICollection<>));

	Expression<Func<TEntity, bool>> BuildAllPredicate<TEntity>(Expression<Func<TEntity, ICollection<TValue>>> selector)
	{
		// Sin criterio -> true
		if (All is null)
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		var entityParam = selector.Parameters[0];
		var collection = selector.Body;

		// p => p  (selector identidad del elemento)
		var p = Expression.Parameter(typeof(TValue), "p");
		var elementSelector = Expression.Lambda<Func<TValue, TValue>>(p, p);

		// Predicado de elemento a partir del filtro 'All'
		var elementPredicate = All.BuildPropertyPredicate(elementSelector);

		// Enumerable.All<T>(IEnumerable<T>, Func<T,bool>)
		var allMethod = typeof(Enumerable).GetMethods()
			.First(m => m.Name == nameof(Enumerable.All) && m.GetParameters().Length == 2)
			.MakeGenericMethod(typeof(TValue));

		var allCall = Expression.Call(allMethod, collection, elementPredicate);

		// null-check de la colección
		var notNull = Expression.NotEqual(collection, Expression.Constant(null, collection.Type));
		var body = Expression.AndAlso(notNull, allCall);

		return Expression.Lambda<Func<TEntity, bool>>(body, entityParam);
	}
}

[PropertyFilterDescriptor2(nameof(CanApply), nameof(BuildAllNonEmptyPredicate))]
public interface IAllNonEmptyPropertyFilter2<TPropertyFilter, TValue> : IPropertyFilter2<ICollection<TValue>>
	where TPropertyFilter : IPropertyFilter2<TValue>
{
	TPropertyFilter? AllNonEmpty { get; set; }

	static bool CanApply(object target) => target.GetType().IsSubclassOfGenericDefinition(typeof(ICollection<>));

	// All no-vacuo: col != null && col.Any(pred) && !col.Any(!pred)
	Expression<Func<TEntity, bool>> BuildAllNonEmptyPredicate<TEntity>(Expression<Func<TEntity, ICollection<TValue>>> selector)
	{
		if (AllNonEmpty is null)
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), selector.Parameters);

		var entityParam = selector.Parameters[0];
		var collection = selector.Body;

		// p => p
		var p = Expression.Parameter(typeof(TValue), "p");
		var elementSelector = Expression.Lambda<Func<TValue, TValue>>(p, p);

		// predicado de elemento (pred)
		var pred = AllNonEmpty.BuildPropertyPredicate(elementSelector);           // Func<TValue,bool>
		// negación (!pred)
		var notBody = Expression.Not(pred.Body);
		var notPred = Expression.Lambda<Func<TValue, bool>>(notBody, pred.Parameters);

		// Enumerable.Any<T>(IEnumerable<T>, Func<T,bool>)
		var any = typeof(Enumerable).GetMethods()
			.First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
			.MakeGenericMethod(typeof(TValue));

		var anyOk = Expression.Call(any, collection, pred);      // col.Any(pred)
		var anyBad = Expression.Call(any, collection, notPred);  // col.Any(!pred)
		var noneBad = Expression.Not(anyBad);                    // !col.Any(!pred)

		var notNull = Expression.NotEqual(collection, Expression.Constant(null, collection.Type));
		var body = Expression.AndAlso(notNull, Expression.AndAlso(anyOk, noneBad));

		return Expression.Lambda<Func<TEntity, bool>>(body, entityParam);
	}
}

// PEND Filtro GreaterOrEqualThan para IComparable<>
// PEND Filtro LessThan para IComparable<>
// PEND Filtro LessOrEqualThan para IComparable<>

// PEND Filtro StartsWith para string
// PEND Filtro EndsWith para string
// PEND Filtro Contains para string
// PEND Filtro Length para string
// PEND Filtro NullEmptyOrWhiteSpace para string

// PEND Filtro IsEmpty para ICollection<>
// PEND Filtro Average para ICollection<> - ¿Solo para números? ¿Y los dates? ¿Y los nullable?
// PEND Filtro Sum para ICollection<>
// PEND Filtro Max para ICollection<>
// PEND Filtro Min para ICollection<>
// PEND Filtro Count para ICollection<> - ¿Count o Length? ¿Se usa el método Count de Linq o la propiedad Count de ICollection<>?
// PEND Filtro First para ICollection<> - ¿First o FirstOrDefault?
// PEND Filtro Last para ICollection<>
// PEND Filtro Single para ICollection<> - ¿Tiene sentido? ¿Se parseará a SQL?

// PEND Estudiar los nullables para crear filtros IsNull, IsNotNull
// PEND Estudiar los case sensitive, cuando se usan por defecto y cuando no (el Guid.ToString() es case sensitive), permitir que el usuario lo elija
// PEND Estudiar HasFlag para enums - ¿Que pasa si la enum la persisto como string?
// PEND DateTime/DateOnly/TimeOnly filtros por partes (Year/Month/Day)

public interface ISourcedFilter2<TSource>
{
	TSource? Source { get; set; }
}

public class SourcedFilterRequiredException(string message) : FuxionException(message);

public class StringEqualFilter : EntityFilter2<string>
{
	public string? Equal { get; set; }

	protected override Expression BuildPredicate(ParameterExpression parameter) => Equal is null ? Expression.Constant(true) : Expression.Equal(parameter, Expression.Constant(Equal));
}

public abstract class Filter2
{
	protected bool IsTrue(Expression e)
	{
		return e is ConstantExpression c && c.Type == typeof(bool) && (bool)c.Value!;
	}

	protected bool IsFalse(Expression e)
	{
		return e is ConstantExpression c && c.Type == typeof(bool) && !(bool)c.Value!;
	}

	protected Expression BuildAnd(params List<Expression> expressions)
	{
		Expression? res = null;
		foreach (var expression in expressions)
		{
			var e = expression is LambdaExpression l ? l.Body : expression; // <- desenrolla lambdas
			if (IsTrue(e)) continue;
			res = res is null ? e : Expression.AndAlso(res, e);
		}

		return res ?? Expression.Constant(true);
	}
	protected Expression BuildOr(params List<Expression> expressions)
	{
		Expression? res = null;
		foreach (var expression in expressions)
		{
			var e = expression is LambdaExpression l ? l.Body : expression; // <- desenrolla lambdas
			if (IsFalse(e)) continue;
			res = res is null ? e : Expression.OrElse(res, e);
		}
		return res ?? Expression.Constant(false);
	}
}

public abstract class EntityFilter2<TEntity> : Filter2, IEntityFilter2<TEntity>, IPropertyFilter2<TEntity?>
{
	public IList<UserFilter2>? Or { get; set; }
	public IList<UserFilter2>? And { get; set; }

	private Expression<Func<TEntity, bool>> Build()
	{
		var parameter = Expression.Parameter(typeof(TEntity), "user");

		var andSelf = BuildPredicate(parameter);

		// OR de entidad (lista de alternativas completas)
		Expression? orBody = null;
		if (Or is not null && Or.Count > 0)
			foreach (var child in Or)
			{
				// Unifica el parámetro del hijo con 'user'
				var childBody = new ReplaceVisitor(child.Predicate.Parameters[0], parameter).Visit(child.Predicate.Body)!;
				if (!IsTrue(childBody))
					orBody = orBody is null ? childBody : Expression.OrElse(orBody, childBody);
			}

		// AND de entidad (todas las alternativas completas)
		Expression? andChildren = null;
		if (And is not null && And.Count > 0)
			foreach (var child in And)
			{
				var childBody = new ReplaceVisitor(child.Predicate.Parameters[0], parameter).Visit(child.Predicate.Body)!;
				if (!IsTrue(childBody))
					andChildren = andChildren is null ? childBody : Expression.AndAlso(andChildren, childBody);
			}

		// Combinación final: AND por defecto entre todo (los True se ignoran en AndAll)
		var finalBody = BuildAnd(andSelf,
			andChildren ?? Expression.Constant(true),
			orBody ?? Expression.Constant(true));

		return Expression.Lambda<Func<TEntity, bool>>(finalBody, parameter);
	}

	protected abstract Expression BuildPredicate(ParameterExpression parameter);

	[field: AllowNull]
	[field: MaybeNull]
	public Expression<Func<TEntity, bool>> Predicate
	{
		get => field ??= Build();
	} = null;

	Expression<Func<TEntity2, bool>> IPropertyFilter2<TEntity?>.BuildPropertyPredicate<TEntity2>(
		Expression<Func<TEntity2, TEntity?>> selector)
	{
		var entityParam = selector.Parameters[0];
		var userParam = Predicate.Parameters[0];

		var inlined = new ReplaceVisitor(userParam, selector.Body).Visit(Predicate.Body)!;

		// Si el predicado inyectado es True, no añadimos null-check ni nada
		if (IsTrue(inlined))
			return Expression.Lambda<Func<TEntity2, bool>>(Expression.Constant(true), entityParam);

		var body = inlined;
		if (selector.Body.Type.IsClass)
		{
			var notNull = Expression.NotEqual(selector.Body, Expression.Constant(null, selector.Body.Type));
			body = Expression.AndAlso(notNull, inlined);
		}

		return Expression.Lambda<Func<TEntity2, bool>>(body, entityParam);
	}

	protected Expression<Func<TEntity, bool>> BuildProperty<TValue>(
		IPropertyFilter2<TValue> filter,
		string propertyName,
		ParameterExpression parameter)
	{
		// Acceso a la propiedad: e.Property
		Expression property = Expression.Property(parameter, propertyName);

		// Si el filtro espera Nullable<T> y la propiedad es T no-nullable, convertir a Nullable<T>
		var targetType = typeof(TValue);
		var targetUnderlying = Nullable.GetUnderlyingType(targetType);
		if (targetUnderlying is not null && property.Type == targetUnderlying)
			property = Expression.Convert(property, targetType);

		// Construir selector unificado con 'parameter'
		var selector = Expression.Lambda<Func<TEntity, TValue>>(property, parameter);
		return filter.BuildPropertyPredicate(selector);
	}

	protected Expression<Func<TEntity, bool>> BuildComputed<TValue>(
		IPropertyFilter2<TValue> filter,
		Expression<Func<TEntity, TValue>> selector,
		ParameterExpression parameter)
	{
		// Sustituye el parámetro del selector por 'parameter' si es distinto
		var body = selector.Parameters[0] == parameter
			? selector.Body
			: new ReplaceVisitor(selector.Parameters[0], parameter).Visit(selector.Body)!;

		var unified = Expression.Lambda<Func<TEntity, TValue>>(body, parameter);
		return filter.BuildPropertyPredicate(unified);
	}

	protected Expression<Func<TEntity, bool>> BuildSourced<TSource, TValue>(
		 ISourcedFilter2<TSource> sourced,
		 IPropertyFilter2<TValue> filter,
		 Expression<Func<TEntity, TSource, TValue>> selector,
		 ParameterExpression parameter)
	{
		// 1) Preflight: si el filtro no aporta criterio, no exigimos Source
		var probeSelector = Expression.Lambda<Func<TEntity, TValue>>(Expression.Default(typeof(TValue)), parameter);
		var probe = filter.BuildPropertyPredicate(probeSelector);
		if (IsTrue(probe.Body))
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), parameter);

		// 2) Aporta criterio ⇒ ahora sí exigimos Source
		if (sourced.Source is null)
			throw new SourcedFilterRequiredException($"Filter '{GetType().GetSignature()}' require a Source of type '{typeof(TSource).GetSignature()}'.");

		// 3) Inline del Source en el selector y delegación al filtro
		var srcConst = Expression.Constant(sourced.Source, typeof(TSource));
		var body = new ReplaceVisitor(selector.Parameters[0], parameter).Visit(selector.Body)!;
		body = new ReplaceVisitor(selector.Parameters[1], srcConst).Visit(body)!;

		var inlinedSelector = Expression.Lambda<Func<TEntity, TValue>>(body, parameter);
		return filter.BuildPropertyPredicate(inlinedSelector);
	}

	protected Expression<Func<TEntity, bool>> BuildSourcedCollection<TSource, TElement>(
		 ISourcedFilter2<TSource> sourced,
		 IPropertyFilter2<ICollection<TElement>> filter,
		 Expression<Func<TEntity, TSource, ICollection<TElement>>> selector,
		 ParameterExpression parameter)
	{
		// 1) Preflight: si el filtro no aporta criterio, no exigimos Source
		var tValue = typeof(ICollection<TElement>);
		var probeSelector = Expression.Lambda<Func<TEntity, ICollection<TElement>>>(Expression.Default(tValue), parameter);
		var probe = filter.BuildPropertyPredicate(probeSelector);
		if (IsTrue(probe.Body))
			return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), parameter);

		// 2) Aporta criterio ⇒ ahora sí exigimos Source
		if (sourced.Source is null)
			throw new SourcedFilterRequiredException($"Filter '{GetType().GetSignature()}' require a Source of type '{typeof(TSource).GetSignature()}'.");

		// 3) Inline del Source y delegación al filtro
		var srcConst = Expression.Constant(sourced.Source, typeof(TSource));
		var body = new ReplaceVisitor(selector.Parameters[0], parameter).Visit(selector.Body)!;
		body = new ReplaceVisitor(selector.Parameters[1], srcConst).Visit(body)!;

		var inlinedSelector = Expression.Lambda<Func<TEntity, ICollection<TElement>>>(body, parameter);
		return filter.BuildPropertyPredicate(inlinedSelector);
	}
}

public abstract class PropertyFilter2<TPropertyFilter, TValue> : Filter2, IOrPropertyFilter2<TPropertyFilter, TValue>,
	IAndPropertyFilter2<TPropertyFilter, TValue>
	where TPropertyFilter : IPropertyFilter2<TValue>
{
	protected abstract Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
		Expression<Func<TEntity, TValue>> selector);

	public IList<TPropertyFilter>? Or { get; set; }
	public IList<TPropertyFilter>? And { get; set; }

	Expression<Func<TEntity, bool>> IPropertyFilter2<TValue>.BuildPropertyPredicate<TEntity>(
		Expression<Func<TEntity, TValue>> selector)
	{
		var andChildren = ((IAndPropertyFilter2<TPropertyFilter, TValue>)this).BuildAndPredicate(selector).Body;
		var orChildren = ((IOrPropertyFilter2<TPropertyFilter, TValue>)this).BuildOrPredicate(selector).Body;
		var derived = BuildPropertyPredicate(selector);

		var body = BuildAnd(andChildren, orChildren, derived);
		return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
	}
}

public partial class UserFilter2 : EntityFilter2<UserDao> // AUTOGENERATED
{
	protected override Expression BuildPredicate(ParameterExpression user)
		=> BuildAnd(
			BuildProperty(FirstName, nameof(UserDao.FirstName), user),
			BuildProperty(LastName, nameof(UserDao.LastName), user),
			BuildProperty(UserId, nameof(UserDao.UserId), user),
			BuildProperty(Phones, nameof(UserDao.Phones), user),
			BuildComputed(FullName, u => u.FirstName + " " + u.LastName, user),
			BuildProperty(Invoices, nameof(UserDao.Invoices), user),
			BuildSourced(
				LastAppointment,
				LastAppointment,
				(u, src) =>
					src.Get<AppointmentDao>()
						.Where(a => a.ExternalId == u.UserId.ToString())
						.OrderByDescending(a => a.AppointmentDate)
						.FirstOrDefault(),
				user));

	public static implicit operator UserFilter2(Guid id) => new() { UserId = { Equal = id } };

	#region UserId

	public sealed class UserIdFilter : PropertyFilter2<UserIdFilter, Guid?>,
		IEqualPropertyFilter2<Guid?>,
		INotEqualPropertyFilter2<Guid?>,
		IInPropertyFilter2<Guid?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
			Expression<Func<TEntity, Guid?>> selector)
		{
			var parts = new List<Expression>();

			if (this is IEqualPropertyFilter2<Guid?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if (this is INotEqualPropertyFilter2<Guid?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if (this is IInPropertyFilter2<Guid?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public Guid? Equal { get; set; }
		public Guid? NotEqual { get; set; }
		public IReadOnlyCollection<Guid?>? In { get; set; }
	}

	public UserIdFilter UserId { get; } = new();

	#endregion UserId

	#region FirstName

	public sealed class FirstNameFilter : PropertyFilter2<FirstNameFilter, string?>,
		IEqualPropertyFilter2<string?>,
		INotEqualPropertyFilter2<string?>,
		IInPropertyFilter2<string?>
	{ 
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
			Expression<Func<TEntity, string?>> selector)
		{
			var parts = new List<Expression>();

			if(this is IEqualPropertyFilter2<string?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if(this is INotEqualPropertyFilter2<string?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if(this is IInPropertyFilter2<string?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public string? Equal { get; set; }
		public string? NotEqual { get; set; }
		public IReadOnlyCollection<string?>? In { get; set; }
	}

	public FirstNameFilter FirstName { get; } = new();

	#endregion FirstName

	#region LastName

	public sealed class LastNameFilter : PropertyFilter2<LastNameFilter, string?>,
		IEqualPropertyFilter2<string?>,
		INotEqualPropertyFilter2<string?>,
		IInPropertyFilter2<string?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
			Expression<Func<TEntity, string?>> selector)
		{
			var parts = new List<Expression>();

			if (this is IEqualPropertyFilter2<string?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if (this is INotEqualPropertyFilter2<string?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if (this is IInPropertyFilter2<string?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public string? Equal { get; set; }
		public string? NotEqual { get; set; }
		public IReadOnlyCollection<string?>? In { get; set; }
	}

	public LastNameFilter LastName { get; } = new();

	#endregion LastName

	#region FullName

	public sealed class FullNameFilter : PropertyFilter2<FullNameFilter, string?>,
		IEqualPropertyFilter2<string?>,
		INotEqualPropertyFilter2<string?>,
		IInPropertyFilter2<string?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
			Expression<Func<TEntity, string?>> selector)
		{
			var parts = new List<Expression>();

			if (this is IEqualPropertyFilter2<string?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if (this is INotEqualPropertyFilter2<string?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if (this is IInPropertyFilter2<string?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public string? Equal { get; set; }
		public string? NotEqual { get; set; }
		public IReadOnlyCollection<string?>? In { get; set; }
	}

	public FullNameFilter FullName { get; } = new();

	#endregion FullName

	#region Phones

	public sealed class PhoneFilter : PropertyFilter2<PhoneFilter, string?>,
		IEqualPropertyFilter2<string?>,
		INotEqualPropertyFilter2<string?>,
		IInPropertyFilter2<string?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(Expression<Func<TEntity, string?>> selector)
		{
			var parts = new List<Expression>();

			if (this is IEqualPropertyFilter2<string?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if (this is INotEqualPropertyFilter2<string?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if (this is IInPropertyFilter2<string?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public string? Equal { get; set; }
		public string? NotEqual { get; set; }
		public IReadOnlyCollection<string?>? In { get; set; }
	}

	public sealed class PhonesFilter : PropertyFilter2<PhonesFilter, ICollection<string?>>,
		IAnyPropertyFilter2<PhoneFilter, string?>,
		IAllPropertyFilter2<PhoneFilter, string?>,
		IAllNonEmptyPropertyFilter2<PhoneFilter, string?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(Expression<Func<TEntity, ICollection<string?>>> selector)
		{
			var parts = new List<Expression>();

			if (this is IAnyPropertyFilter2<PhoneFilter, string?> any)
				parts.Add(any.BuildAnyPredicate(selector).Body);

			if (this is IAllPropertyFilter2<PhoneFilter, string?> all)
				parts.Add(all.BuildAllPredicate(selector).Body);

			if (this is IAllNonEmptyPropertyFilter2<PhoneFilter, string?> allne)
				parts.Add(allne.BuildAllNonEmptyPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}
		public PhoneFilter? Any { get; set; }
		public PhoneFilter? All { get; set; }
		public PhoneFilter? AllNonEmpty { get; set; }
	}

	public PhonesFilter Phones { get; } = new();

	#endregion

	#region Invoices

	public sealed class InvoicesFilter : PropertyFilter2<InvoicesFilter, ICollection<InvoiceDao?>>,
		IAnyPropertyFilter2<InvoiceFilter2, InvoiceDao?>,
		IAllPropertyFilter2<InvoiceFilter2, InvoiceDao?>,
		IAllNonEmptyPropertyFilter2<InvoiceFilter2, InvoiceDao?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(Expression<Func<TEntity, ICollection<InvoiceDao?>>> selector)
		{
			var parts = new List<Expression>();

			if (this is IAnyPropertyFilter2<InvoiceFilter2, InvoiceDao?> any)
				parts.Add(any.BuildAnyPredicate(selector).Body);

			if (this is IAllPropertyFilter2<InvoiceFilter2, InvoiceDao?> all)
				parts.Add(all.BuildAllPredicate(selector).Body);

			if (this is IAllNonEmptyPropertyFilter2<InvoiceFilter2, InvoiceDao?> allne)
				parts.Add(allne.BuildAllNonEmptyPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}
		public InvoiceFilter2? Any { get; set; }
		public InvoiceFilter2? All { get; set; }
		public InvoiceFilter2? AllNonEmpty { get; set; }
	}

	public InvoicesFilter Invoices { get; } = new();

	#endregion Invoices

	#region Appointments

	public AppointmentFilter2 LastAppointment { get; set; } = new();

	#endregion Appointments
}

public class InvoiceFilter2 : EntityFilter2<InvoiceDao> // AUTOGENERATED
{
	protected override Expression BuildPredicate(ParameterExpression invoice)
		=> BuildProperty(Customer, nameof(InvoiceDao.Customer), invoice);

	#region Customer

	public UserFilter2 Customer { get; } = new();

	#endregion Customer
}

public class AppointmentFilter2 : EntityFilter2<AppointmentDao>, ISourcedFilter2<ITestDataContext> // AUTOGENERATED
{
	public ITestDataContext? Source { get; set; }

	protected override Expression BuildPredicate(ParameterExpression appointment)
		=> BuildAnd(
			BuildProperty(ExternalId, nameof(AppointmentDao.ExternalId), appointment),
			BuildProperty(Payload, nameof(AppointmentDao.Payload), appointment));

	#region ExternalId

	public sealed class ExternalIdFilter : PropertyFilter2<ExternalIdFilter, string?>,
		IEqualPropertyFilter2<string?>,
		INotEqualPropertyFilter2<string?>,
		IInPropertyFilter2<string?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
			Expression<Func<TEntity, string?>> selector)
		{
			var parts = new List<Expression>();

			if (this is IEqualPropertyFilter2<string?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if (this is INotEqualPropertyFilter2<string?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if (this is IInPropertyFilter2<string?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public string? Equal { get; set; }
		public string? NotEqual { get; set; }
		public IReadOnlyCollection<string?>? In { get; set; }
	}

	public ExternalIdFilter ExternalId { get; } = new();

	#endregion ExternalId

	#region Payload

	public sealed class PayloadFilter : PropertyFilter2<PayloadFilter, string?>,
		IEqualPropertyFilter2<string?>,
		INotEqualPropertyFilter2<string?>,
		IInPropertyFilter2<string?>
	{
		protected override Expression<Func<TEntity, bool>> BuildPropertyPredicate<TEntity>(
			Expression<Func<TEntity, string?>> selector)
		{
			var parts = new List<Expression>();

			if (this is IEqualPropertyFilter2<string?> eq)
				parts.Add(eq.BuildEqualPredicate(selector).Body);

			if (this is INotEqualPropertyFilter2<string?> neq)
				parts.Add(neq.BuildNotEqualPredicate(selector).Body);

			if (this is IInPropertyFilter2<string?> inf)
				parts.Add(inf.BuildInPredicate(selector).Body);

			var body = BuildAnd(parts);
			return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
		}

		public string? Equal { get; set; }
		public string? NotEqual { get; set; }
		public IReadOnlyCollection<string?>? In { get; set; }
	}

	public PayloadFilter Payload { get; } = new();

	#endregion ExternalId
}

public class AddressFilter2 : IEntityFilter2<AddressDao> // AUTOGENERATED
{
	public Expression<Func<TEntity, bool>> BuildPredicate<TEntity>(Expression<Func<TEntity, AddressDao>> selector)
	{
		throw new NotImplementedException();
	}

	public Expression<Func<AddressDao, bool>> Predicate { get; } = null!;
}

public sealed class ReplaceVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
{
	protected override Expression VisitParameter(ParameterExpression node)
	{
		return node == from ? to : base.VisitParameter(node);
	}
}

public interface IFilterDescriptor2<TValue>;

public class FilterDescriptorBuilder2
{
	public static FilterBuilder2<TEntity> For<TEntity>(string singular, string plural)
	{
		throw new NotImplementedException();
	}
}

public class FilterBuilder2<TEntity>
{
	public FilterBuilder2<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> selector, string? name = null)
	{
		throw new NotImplementedException();
	}

	public FilterBuilder2<TEntity> Implicit() => throw new NotImplementedException();

	public FilterNavigationBuilder2<TEntity, TProperty> Navigation<TProperty>(
		Expression<Func<TEntity, TProperty>> selector)
	{
		throw new NotImplementedException();
	}

	public FilterNavigationBuilder2<TEntity, TProperty> NavigationCollection<TProperty>(
		Expression<Func<TEntity, ICollection<TProperty>?>> selector)
	{
		throw new NotImplementedException();
	}

	//public FilterNavigationBuilder2<TEntity, TProperty> NavigationSourced<TProperty>(
	//	Expression<Func<TEntity, IQueryable<TProperty>, TProperty?>> selector, string? name = null)
	//{
	//	throw new NotImplementedException();
	//}
	public FilterNavigationBuilder2<TEntity, TProperty> NavigationSourced<TSource, TProperty>(
		Expression<Func<TEntity, TSource, TProperty?>> selector, string name)
	{
		throw new NotImplementedException();
	}

	public FilterNavigationBuilder2<TEntity, TProperty> NavigationSourcedCollection<TSource, TProperty>(
		Expression<Func<TEntity, TSource, ICollection<TProperty>?>> selector, string name)
	{
		throw new NotImplementedException();
	}

	public IFilterDescriptor2<TValue> Build<TValue>()
	{
		throw new NotImplementedException();
	}
}

public class FilterNavigationBuilder2<TEntity, TProperty>
{
	public FilterBuilder2<TEntity> WithFilter<TFilter>() where TFilter : IEntityFilter2<TProperty>
	{
		throw new NotImplementedException();
	}
}

[EntityFilterDescriptor2(nameof(Descriptor))]
public partial class UserFilter2
{
	public static readonly IFilterDescriptor2<UserDao> Descriptor = FilterDescriptorBuilder2
		.For<UserDao>("User", "Users")
		.Property(u => u.UserId).Implicit()
		.Property(u => u.FirstName)
		.Property(u => u.Phones)
		.Property(u => u.FirstName + " " + u.LastName, "FullName")
		.Navigation(u => u.Address).WithFilter<AddressFilter2>()
		.NavigationCollection(u => u.Invoices).WithFilter<InvoiceFilter2>()
		.NavigationSourced<ITestDataContext, AppointmentDao>(
			(u, src) => src.Get<AppointmentDao>()
				.Where(a => a.ExternalId == u.UserId.ToString())
				.OrderByDescending(a => a.AppointmentDate)
				.FirstOrDefault(),
			"LastAppointment").WithFilter<AppointmentFilter2>()
		.NavigationSourcedCollection<ITestDataContext, AppointmentDao>(
			(u, src) => src.Get<AppointmentDao>()
				.Where(a => a.ExternalId == u.UserId.ToString())
				.ToList(),
			"Appointments").WithFilter<AppointmentFilter2>()
		.Build<UserDao>();
}

public static class Extensions
{
	public static IQueryable<TEntity> Filter2<TEntity>(this IQueryable<TEntity> query, IEntityFilter2<TEntity>? filter)
	{
		return filter is null ? query : query.Where(filter.Predicate);
	}
}

public class Filter2Test(ITestOutputHelper output) : BaseTest<Filter2Test>(output)
{
	private void PrintSql<TEntity>(IEntityFilter2<TEntity> filter) where TEntity : class
	{
		DbContextOptionsBuilder<SqlServerDbContext> builderSqlServer = new();
		builderSqlServer.UseSqlServer("");
		ITestDataContext context = new SqlServerDbContext(builderSqlServer.Options);
		Output.WriteLine(context.Get<TEntity>().Filter2(filter).ToQueryString());
	}

	[Fact]
	public void ImplicitConversion()
	{
		var users = DataSeed.Users.Values.AsQueryable();
		UserFilter2 filter = Guid.Parse("{B396147A-8261-4476-962D-0A52E726A936}");
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = users.Filter2(filter);
		Assert.Single(res);
	}

	[Fact]
	public void Appointments()
	{
		DbContextOptionsBuilder<SqlServerDbContext> builderSqlServer = new();
		builderSqlServer.UseSqlServer("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
		ITestDataContext context = new SqlServerDbContext(builderSqlServer.Options);

		//var users = context.Get<UserDao>();
		var users = DataSeed.Users.Values.AsQueryable();
		var appointments = DataSeed.Appointments.Values.AsQueryable();
		UserFilter2 filter = new()
		{
			LastAppointment =
			{
				Source = DataSeed.DataContext,
				ExternalId =
				{
					Equal = DataSeed.Users["Bob"].UserId.ToString()
				}
			}
		};
		PrintVariable(filter.Predicate);
		//Output.WriteLine(users.Filter2(filter).ToQueryString());
		var res = users.Filter2(filter);
		Assert.Equal(1, res.Count());

		users = context.Get<UserDao>();
		filter = new()
		{
			LastAppointment =
			{
				Source = context,
				ExternalId =
				{
					Equal = DataSeed.Users["Bob"].UserId.ToString()
				}
			}
		};
		Output.WriteLine(users.Filter2(filter).ToQueryString());
		// Sample of use appointments in users
		//var usersWithAppointments = users.Where(u =>
		//	appointments.Where(a => a.ExternalId == u.UserId.ToString()).Any(a => a.Payload == "Appointment for Bob"));
	}

	[Fact]
	public void Invoices()
	{
		var users = DataSeed.Users.Values.AsQueryable();
		UserFilter2 filter = new()
		{
			Invoices =
			{
				Any = new()
				{
					Customer =
					{
						FirstName =
						{
							Equal = "Alice"
						}
					}
				}
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = users.Filter2(filter);
		Assert.Equal(1, res.Count());

		filter = new()
		{
			Invoices =
			{
				All = new()
				{
					Customer =
					{
						FirstName =
						{
							Equal = "Alice"
						}
					}
				}
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		res = users.Filter2(filter);
		Assert.Equal(4, res.Count()); // Los usuarios que no tienen facturas, cuentan como que cumplen 'All'

		filter = new()
		{
			Invoices =
			{
				AllNonEmpty = new()
				{
					Customer =
					{
						FirstName =
						{
							Equal = "Alice"
						}
					}
				}
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		res = users.Filter2(filter);
		Assert.Equal(1, res.Count());
	}

	[Fact]
	public void Phones()
	{
		var users = DataSeed.Users.Values.AsQueryable();
		UserFilter2 filter = new()
		{
			Phones =
			{
				Any = new()
				{
					Equal = DataSeed.Users["Bob"].Phones[0]
				}
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = users.Filter2(filter);
		Assert.Equal(2, res.Count());

		filter = new()
		{
			Phones =
			{
				All = new()
				{
					Equal = DataSeed.Users["Bob"].Phones[0]
				}
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		res = users.Filter2(filter);
		Assert.Equal(1, res.Count());
	}

	[Fact]
	public void CheckInterfaces()
	{
		string @string = "";
		PrintVariable(@string is IEquatable<string>);
		PrintVariable(@string is IComparable<string>);

		List<string> stringList = [];
		PrintVariable(stringList is IEquatable<List<string>>);
		PrintVariable(stringList is ICollection<string>);

		string[] stringArray = [];
		PrintVariable(stringArray is ICollection<string>);
	}

	[Fact]
	public void Computed()
	{
		var users = DataSeed.Users.Values.AsQueryable();
		UserFilter2 filter = new()
		{
			FullName =
			{
				Equal = "Bob Bobson"
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = users.Filter2(filter);
		Assert.Single(res);
	}
	[Fact]
	public void And()
	{
		var users = DataSeed.Users.Values.AsQueryable();

		UserFilter2 filter = new()
		{
			Or =
			[
				new()
				{
					FirstName =
					{
						And =
						[
							new()
							{
								Equal = "Bob"
							},
							new()
							{
								NotEqual = "Alice"
							}
						]
					}
				},
				new()
				{
					LastName =
					{
						Equal = "Bobson"
					},
					UserId =
					{
						Equal = DataSeed.Users["Bob"].UserId
					}
				}
			]
		};

		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = users.Filter2(filter);
		Assert.Equal(1, res.Count());
	}

	[Fact]
	public void Or()
	{
		var users = DataSeed.Users.Values.AsQueryable();

		UserFilter2 filter = new()
		{
			Or =
			[
				new()
				{
					FirstName =
					{
						Or =
						[
							new()
							{
								Equal = "Alice"
							},
							new()
							{
								In = ["Bob", "None"]
							}
						]
					}
				},
				new()
				{
					LastName =
					{
						Equal = "Bobson"
					}
				}
			]
		};

		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = users.Filter2(filter);
		Assert.Equal(2, res.Count());
	}

	[Fact]
	public void Navigation()
	{
		var invoices = DataSeed.Invoices.Values.AsQueryable();

		InvoiceFilter2 filter = new()
		{
			Customer =
			{
				FirstName =
				{
					Equal = "Alice"
				}
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);
		var res = invoices.Filter2(filter);
		Assert.Equal(2, res.Count());
	}

	[Fact]
	public void StringEqualFilter()
	{
		List<string> strs = ["Alice", "Bob", "Charlie"];
		StringEqualFilter filter = new()
		{
			Equal = "Bob"
		};
		PrintVariable(filter.Predicate);
		var res = strs.AsQueryable().Where(filter.Predicate);
		Assert.Single(res);
	}

	[Fact]
	public void Filter2()
	{
		var users = DataSeed.Users.Values.AsQueryable();

		UserFilter2 filter = new()
		{
			FirstName =
			{
				Equal = "Bob"
			}
		};
		PrintVariable(filter.Predicate);
		PrintSql(filter);

		var res = users.Filter2(filter);
		Assert.Single(res);

		filter = new()
		{
			FirstName =
			{
				NotEqual = "Bob"
			}
		};
		res = users.Filter2(filter);
		Assert.Equal(DataSeed.Users.Count - 1, res.Count());

		filter = new()
		{
			FirstName =
			{
				In = ["Alice", "Bob"]
			}
		};
		res = users.Filter2(filter);
		Assert.Equal(2, res.Count());
	}
}
#endif