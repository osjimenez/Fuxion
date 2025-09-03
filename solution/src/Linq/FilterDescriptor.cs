using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fuxion.Linq;

public sealed class FilterDescriptor<TEntity, TField> : IFilterDescriptor<TEntity>
{
	private FilterDescriptor(
		string name,
		Expression<Func<TEntity, TField>> sel,
		OperationKind allowed,
		NullHandling nh,
		string? external,
		IReadOnlyList<string> aliases,
		bool ci,
		bool inline,
		IReadOnlyList<LambdaExpression>? extra,
		IReadOnlyDictionary<string, object>? metadata)
	{
		Name = name;
		TypedSelector = sel;
		AllowedOperations = allowed;
		NullHandling = nh;
		ExternalName = external;
		Aliases = aliases;
		CaseInsensitive = ci;
		EnableInlining = inline;
		ExtraSelectors = extra;
		Metadata = metadata;
	}

	public string Name { get; }
	public string? ExternalName { get; }
	public IReadOnlyList<string> Aliases { get; }
	public OperationKind AllowedOperations { get; }
	public NullHandling NullHandling { get; }
	public Expression<Func<TEntity, TField>> TypedSelector { get; }
	public IReadOnlyList<LambdaExpression>? ExtraSelectors { get; }
	public bool CaseInsensitive { get; }
	public bool EnableInlining { get; }
	public IReadOnlyDictionary<string, object>? Metadata { get; }
	LambdaExpression IFilterDescriptor<TEntity>.Selector => TypedSelector;

	public static Builder Create(string name, Expression<Func<TEntity, TField>> sel)
	{
		return new(name, sel);
	}

	#region Nested types

	public sealed class Builder
	{
		internal Builder(string n, Expression<Func<TEntity, TField>> s)
		{
			_name = n;
			_selector = s;
		}

		private readonly List<string> _aliases = new();
		private readonly List<LambdaExpression> _extras = new();
		private readonly string _name;
		private readonly Expression<Func<TEntity, TField>> _selector;
		private OperationKind _allowed;
		private bool _ci;
		private string? _external;
		private bool _inline = true;
		private Dictionary<string, object>? _meta;
		private bool _navigation;
		private NullHandling _nullHandling = NullHandling.Default;
		private bool _opsExplicit;

		public Builder Operations(OperationKind o)
		{
			_allowed = o;
			_opsExplicit = true;
			return this;
		}

		public Builder External(string e)
		{
			_external = e;
			return this;
		}

		public Builder Alias(string a)
		{
			if (!string.IsNullOrWhiteSpace(a)) _aliases.Add(a);
			return this;
		}

		public Builder Nulls(NullHandling nh)
		{
			_nullHandling = nh;
			return this;
		}

		public Builder CaseInsensitive(bool v = true)
		{
			_ci = v;
			return this;
		}

		public Builder Inlining(bool e = true)
		{
			_inline = e;
			return this;
		}

		public Builder Meta(string k, object v)
		{
			_meta ??= new();
			_meta[k] = v;
			return this;
		}

		internal Builder MarkNavigation()
		{
			_navigation = true;
			_meta ??= new();
			_meta["Navigation"] = true;
			return this;
		}

		private OperationKind Infer()
		{
			if (_navigation) return OperationKind.IsNull | OperationKind.IsNotNull;
			var ft = typeof(TField);
			var eff = Nullable.GetUnderlyingType(ft) ?? ft;
			OperationKind ops;
			if (eff == typeof(string)) ops = FilterPresets.Text;
			else if (eff.IsEnum) ops = FilterPresets.Enum;
			else if (typeof(IComparable).IsAssignableFrom(eff)) ops = FilterPresets.Comparable;
			else ops = OperationKind.Equal | OperationKind.NotEqual | OperationKind.In;
			if (!eff.IsValueType || Nullable.GetUnderlyingType(ft) != null || _navigation)
				ops |= OperationKind.IsNull | OperationKind.IsNotNull;
			return ops;
		}

		public FilterDescriptor<TEntity, TField> Build()
		{
			var allowed = _opsExplicit ? _allowed : Infer();
			return new(
				_name,
				_selector,
				allowed,
				_nullHandling,
				_external,
				_aliases.ToArray(),
				_ci,
				_inline,
				_extras.Count == 0 ? null : _extras.ToArray(),
				_meta);
		}
	}

	#endregion
}