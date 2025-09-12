using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Fuxion.Linq;

public sealed class FilterEntityBuilder<TEntity>
{
	private readonly List<IFilterDescriptor<TEntity>> _fields = new();

	public FilterEntityBuilder<TEntity> Property<TField>(Expression<Func<TEntity, TField>> selector,
		Action<FilterDescriptor<TEntity, TField>.Builder>? configure = null)
	{
		if (selector.Body is not MemberExpression m)
			throw new ArgumentException("Selector must be member. Use Computed(name, expr) for arbitrary expressions.",
				nameof(selector));
		var b = FilterDescriptor<TEntity, TField>.Create(m.Member.Name, selector);
		configure?.Invoke(b);
		_fields.Add(b.Build());
		return this;
	}

	public FilterEntityBuilder<TEntity> Computed<TField>(string name, Expression<Func<TEntity, TField>> selector,
		Action<FilterDescriptor<TEntity, TField>.Builder>? configure = null)
	{
		if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", "name");
		var b = FilterDescriptor<TEntity, TField>.Create(name, selector); // mark via metadata
		b.Meta("Computed", true);
		configure?.Invoke(b);
		_fields.Add(b.Build());
		return this;
	}

	// Navigation con filtro explícito. Deducimos el tipo de la propiedad a partir del selector (soporta simple o colección)
	public FilterEntityBuilder<TEntity> Navigation<TFilter>(Expression<Func<TEntity, object?>> selector)
		where TFilter : IFilter
	{
		// Desenrollar conversiones a object
		MemberExpression? memberExpr = selector.Body as MemberExpression;
		if (memberExpr == null && selector.Body is UnaryExpression ue && ue.Operand is MemberExpression me2)
			memberExpr = me2;
		if (memberExpr == null) throw new ArgumentException("Selector must be member.", nameof(selector));

		var member = memberExpr.Member;
		var propType = member switch
		{
			PropertyInfo pi => pi.PropertyType,
			FieldInfo fi => fi.FieldType,
			_ => throw new ArgumentException("Selector must target a field or property.")
		};

		// Construir FilterDescriptor<TEntity, TField> dinámicamente
		var fdGeneric = typeof(FilterDescriptor<,>).MakeGenericType(typeof(TEntity), propType);
		var create = fdGeneric.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;
		var funcType = typeof(Func<,>).MakeGenericType(typeof(TEntity), propType);
		var lambda = Expression.Lambda(funcType, memberExpr, selector.Parameters);
		var builder = create.Invoke(null, new object?[] { member.Name, lambda })!;
		// MarkNavigation
		var markNav = builder.GetType().GetMethod("MarkNavigation", BindingFlags.Instance | BindingFlags.NonPublic) ?? builder.GetType().GetMethod("MarkNavigation", BindingFlags.Instance | BindingFlags.Public);
		markNav?.Invoke(builder, null);
		// Opcional: tag del filtro expresado (trazabilidad)
		var meta = builder.GetType().GetMethod("Meta", BindingFlags.Instance | BindingFlags.Public);
		meta?.Invoke(builder, new object?[] { "NavigationFilterType", typeof(TFilter) });
		// Build y registrar
		var built = builder.GetType().GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)!.Invoke(builder, null)!;
		_fields.Add((IFilterDescriptor<TEntity>)built);
		return this;
	}

	public IFilterDescriptor<TEntity>[] Build()
	{
		return _fields.ToArray();
	}
}