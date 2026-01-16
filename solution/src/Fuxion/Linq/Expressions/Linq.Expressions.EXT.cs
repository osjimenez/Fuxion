using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Fuxion.Linq.Expressions;

/// <summary>
/// Provides extension methods for working with LINQ expressions, including member name extraction,
/// property info retrieval, and expression composition.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Gets the member name from a lambda expression. This is an extension method on any object.
	/// </summary>
	/// <typeparam name="T">The type returned by the expression.</typeparam>
	/// <param name="me">The object instance (not used, allows extension method syntax).</param>
	/// <param name="expression">The lambda expression that accesses the member.</param>
	/// <returns>The name of the member referenced in the expression.</returns>
	/// <exception cref="ArgumentException">Thrown when the expression is not a lambda or doesn't contain a valid member expression.</exception>
	/// <example>
	/// <code>
	/// string name = obj.GetMemberName(() => obj.Property);
	/// </code>
	/// </example>
	public static string GetMemberName<T>(this object me, Expression<Func<T>> expression) => expression.GetMemberName();
	
	/// <summary>
	/// Extracts the member name from a lambda expression that takes no parameters.
	/// </summary>
	/// <typeparam name="T">The type returned by the expression.</typeparam>
	/// <param name="expression">The lambda expression that accesses the member.</param>
	/// <returns>The name of the member referenced in the expression.</returns>
	/// <exception cref="ArgumentException">Thrown when the expression is not a lambda, or the body is not a MemberExpression or UnaryExpression.</exception>
	/// <example>
	/// <code>
	/// Expression&lt;Func&lt;string&gt;&gt; expr = () => person.Name;
	/// string memberName = expr.GetMemberName(); // Returns "Name"
	/// </code>
	/// </example>
	public static string GetMemberName<T>(this Expression<Func<T>> expression)
	{
		if (expression.NodeType != ExpressionType.Lambda)
			throw new ArgumentException("La expresión debe ser una lambda", nameof(expression));
		if (expression.Body is MemberExpression body)
			return body.Member.Name;
		if (expression.Body is UnaryExpression { Operand: MemberExpression operand })
			return operand.Member.Name;
		throw new ArgumentException("La expresión lambda debe ser de tipo 'MemberExpression' o 'UnaryExpression'.");
	}
	
	/// <summary>
	/// Extracts the member name from a lambda expression that takes a parameter.
	/// </summary>
	/// <typeparam name="TInstance">The type of the instance that contains the member.</typeparam>
	/// <typeparam name="TMember">The type of the member being accessed.</typeparam>
	/// <param name="expression">The lambda expression that accesses the member.</param>
	/// <returns>The name of the member referenced in the expression.</returns>
	/// <exception cref="ArgumentException">Thrown when the expression is not a lambda, or the body is not a MemberExpression or UnaryExpression.</exception>
	/// <example>
	/// <code>
	/// Expression&lt;Func&lt;Person, string&gt;&gt; expr = p => p.Name;
	/// string memberName = expr.GetMemberName(); // Returns "Name"
	/// </code>
	/// </example>
	public static string GetMemberName<TInstance, TMember>(this Expression<Func<TInstance, TMember>> expression)
	{
		if (expression.NodeType != ExpressionType.Lambda)
			throw new ArgumentException("La expresión debe ser una lambda", nameof(expression));
		if (expression.Body is MemberExpression body)
			return body.Member.Name;
		if (expression.Body is UnaryExpression { Operand: MemberExpression operand })
			return operand.Member.Name;
		throw new ArgumentException("La expresión lambda debe ser de tipo 'MemberExpression' o 'UnaryExpression'.");
	}

	/// <summary>
	/// Retrieves the <see cref="PropertyInfo"/> metadata for a property referenced in a lambda expression.
	/// </summary>
	/// <typeparam name="T">The type returned by the expression.</typeparam>
	/// <param name="expression">The lambda expression that accesses the property.</param>
	/// <returns>The <see cref="PropertyInfo"/> object representing the property.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the expression is not a lambda, the body is not a MemberExpression or UnaryExpression,
	/// or the member is not a property.
	/// </exception>
	/// <example>
	/// <code>
	/// Expression&lt;Func&lt;string&gt;&gt; expr = () => person.Name;
	/// PropertyInfo propInfo = expr.GetPropertyInfo();
	/// </code>
	/// </example>
	public static PropertyInfo GetPropertyInfo<T>(this Expression<Func<T>> expression)
	{
		if (expression.NodeType != ExpressionType.Lambda)
			throw new ArgumentException("La expresión debe ser una lambda", nameof(expression));
		if (expression.Body is MemberExpression body)
		{
			var mem = body.Member;
			if (mem is PropertyInfo pro)
				return pro;
			throw new ArgumentException("La expresión lambda no hace referencia a un miembro de propiedad.");
		}
		if (expression.Body is UnaryExpression unaryExpression)
		{
			var mem = ((MemberExpression)unaryExpression.Operand).Member;
			if (mem is PropertyInfo pro)
				return pro;
			throw new ArgumentException("La expresión lambda no hace referencia a un miembro de propiedad.");
		}
		throw new ArgumentException("La expresión lambda debe ser de tipo 'MemberExpression' o 'UnaryExpression'.");
	}

	/// <summary>
	/// Composes two expressions of the same delegate type by merging their bodies using a specified merge function.
	/// The parameters from the second expression are replaced with parameters from the first expression.
	/// </summary>
	/// <typeparam name="T">The delegate type of both expressions.</typeparam>
	/// <param name="first">The first expression whose parameters will be used in the result.</param>
	/// <param name="second">The second expression whose body will be merged with the first.</param>
	/// <param name="merge">A function that combines the bodies of both expressions.</param>
	/// <returns>A new expression that represents the composition of the two input expressions.</returns>
	/// <example>
	/// <code>
	/// Expression&lt;Func&lt;int, bool&gt;&gt; expr1 = x => x > 5;
	/// Expression&lt;Func&lt;int, bool&gt;&gt; expr2 = x => x &lt; 10;
	/// var composed = expr1.Compose(expr2, Expression.AndAlso);
	/// // Result: x => (x > 5) &amp;&amp; (x &lt; 10)
	/// </code>
	/// </example>
	public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
	{
		// build parameter map (from parameters of second to parameters of first)
		var map = first.Parameters.Select((first, i) => (first, second: second.Parameters[i])).ToDictionary(p => p.second, p => p.first);

		// replace parameters in the second lambda expression with parameters from the first
		var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

		// apply composition of lambda expression bodies to parameters from the first expression 
		return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
	}
	
	/// <summary>
	/// Combines two predicate expressions using a logical AND operation.
	/// </summary>
	/// <typeparam name="T">The type of the parameter in the predicate expressions.</typeparam>
	/// <param name="first">The first predicate expression.</param>
	/// <param name="second">The second predicate expression.</param>
	/// <returns>A new expression that represents the logical AND of both predicates.</returns>
	/// <example>
	/// <code>
	/// Expression&lt;Func&lt;Person, bool&gt;&gt; isAdult = p => p.Age >= 18;
	/// Expression&lt;Func&lt;Person, bool&gt;&gt; hasName = p => !string.IsNullOrEmpty(p.Name);
	/// var combined = isAdult.And(hasName);
	/// // Result: p => (p.Age >= 18) &amp;&amp; (!string.IsNullOrEmpty(p.Name))
	/// </code>
	/// </example>
	public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) => first.Compose(second, Expression.AndAlso);
	
	/// <summary>
	/// Combines two predicate expressions using a logical OR operation.
	/// </summary>
	/// <typeparam name="T">The type of the parameter in the predicate expressions.</typeparam>
	/// <param name="first">The first predicate expression.</param>
	/// <param name="second">The second predicate expression.</param>
	/// <returns>A new expression that represents the logical OR of both predicates.</returns>
	/// <example>
	/// <code>
	/// Expression&lt;Func&lt;Person, bool&gt;&gt; isChild = p => p.Age &lt; 18;
	/// Expression&lt;Func&lt;Person, bool&gt;&gt; isSenior = p => p.Age >= 65;
	/// var combined = isChild.Or(isSenior);
	/// // Result: p => (p.Age &lt; 18) || (p.Age >= 65)
	/// </code>
	/// </example>
	public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) => first.Compose(second, Expression.OrElse);

	/// <summary>
	/// An expression visitor that replaces parameter expressions in an expression tree according to a provided mapping.
	/// This is useful for composing expressions by unifying their parameter references.
	/// </summary>
	public class ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map) : ExpressionVisitor
	{
		readonly Dictionary<ParameterExpression, ParameterExpression> map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
		
		/// <summary>
		/// Replaces parameter expressions in the given expression according to the provided mapping.
		/// </summary>
		/// <param name="map">A dictionary mapping original parameter expressions to their replacements.</param>
		/// <param name="exp">The expression in which to replace parameters.</param>
		/// <returns>A new expression with parameters replaced according to the mapping.</returns>
		public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp) => new ParameterRebinder(map).Visit(exp);
		
		/// <summary>
		/// Visits a parameter expression and replaces it according to the parameter mapping if a replacement exists.
		/// </summary>
		/// <param name="p">The parameter expression to visit.</param>
		/// <returns>The replacement parameter if one exists in the mapping, otherwise the original parameter.</returns>
		protected override Expression VisitParameter(ParameterExpression p)
		{
			if (map.TryGetValue(p, out var replacement)) p = replacement;
			return base.VisitParameter(p);
		}
	}
}