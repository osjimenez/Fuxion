using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fuxion.Linq;

public sealed class FilterPropertyDescriptor<TEntity, TField> : IFilterDescriptor<TEntity>
{
	internal FilterPropertyDescriptor(
		bool caseInsensitive)
	{
		CaseInsensitive = caseInsensitive;
	}
	public bool CaseInsensitive { get; }
	public static FilterPropertyBuilder<TEntity, TField> Create() => new();

}
public sealed class FilterPropertyBuilder<TEntity, TField>
{
	internal FilterPropertyBuilder() { }
	private bool _caseInsensitive;

	public FilterPropertyBuilder<TEntity, TField> CaseInsensitive(bool value = true)
	{
		_caseInsensitive = value;
		return this;
	}
	public FilterPropertyDescriptor<TEntity, TField> Build() => new(_caseInsensitive);
}