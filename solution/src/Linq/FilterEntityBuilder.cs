using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fuxion.Linq;

public sealed class FilterEntityBuilder<TEntity>
{
	readonly List<IFilterDescriptor<TEntity>> _fields=new();
	public FilterEntityBuilder<TEntity> Property<TField>(Expression<Func<TEntity,TField>> selector, Action<FilterDescriptor<TEntity,TField>.Builder>? configure=null){ if(selector.Body is not MemberExpression m) throw new ArgumentException("Selector must be member. Use Computed(name, expr) for arbitrary expressions.",nameof(selector)); var b=FilterDescriptor<TEntity,TField>.Create(m.Member.Name, selector); configure?.Invoke(b); _fields.Add(b.Build()); return this; }
	public FilterEntityBuilder<TEntity> Computed<TField>(string name, Expression<Func<TEntity,TField>> selector, Action<FilterDescriptor<TEntity,TField>.Builder>? configure=null){ if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required","name"); var b=FilterDescriptor<TEntity,TField>.Create(name, selector); // mark via metadata
		b.Meta("Computed", true); configure?.Invoke(b); _fields.Add(b.Build()); return this; }
	public FilterEntityBuilder<TEntity> Navigation<TNav>(Expression<Func<TEntity,TNav?>> selector, Action<FilterDescriptor<TEntity,TNav?>.Builder>? configure=null) where TNav:class { if(selector.Body is not MemberExpression m) throw new("Selector must be member"); var b=FilterDescriptor<TEntity,TNav?>.Create(m.Member.Name, selector).MarkNavigation(); configure?.Invoke(b); _fields.Add(b.Build()); return this; }
	public IFilterDescriptor<TEntity>[] Build()=>_fields.ToArray();
}