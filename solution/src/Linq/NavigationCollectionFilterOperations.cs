using System;
using System.Collections.Generic;

namespace Fuxion.Linq;

public class NavigationCollectionFilterOperations<TChildFilter, TChildEntity> : ICollectionNavigationFilterOperations<TChildFilter>
	where TChildFilter : GeneratedFilter<TChildEntity>, new()
{
	internal readonly List<TChildFilter> AnyAndBlocks = new();
	internal readonly List<TChildFilter> AnyOrBlocks = new();
	internal readonly List<TChildFilter> AllAndBlocks = new();
	internal readonly List<TChildFilter> AllOrBlocks = new();

	public void Any(params Action<TChildFilter>[] and) => Any(and: and, or: null);
	public void Any(IEnumerable<Action<TChildFilter>>? and = null, IEnumerable<Action<TChildFilter>>? or = null)
	{
		if (and != null)
			foreach (var cfg in and)
			{
				var f = new TChildFilter();
				cfg(f);
				if (f.HasAny()) AnyAndBlocks.Add(f);
			}
		if (or != null)
			foreach (var cfg in or)
			{
				var f = new TChildFilter();
				cfg(f);
				if (f.HasAny()) AnyOrBlocks.Add(f);
			}
	}

	public void All(params Action<TChildFilter>[] and) => All(and: and, or: null);
	public void All(IEnumerable<Action<TChildFilter>>? and = null, IEnumerable<Action<TChildFilter>>? or = null)
	{
		if (and != null)
			foreach (var cfg in and)
			{
				var f = new TChildFilter();
				cfg(f);
				if (f.HasAny()) AllAndBlocks.Add(f);
			}
		if (or != null)
			foreach (var cfg in or)
			{
				var f = new TChildFilter();
				cfg(f);
				if (f.HasAny()) AllOrBlocks.Add(f);
			}
	}

	public bool HasAny() => AnyAndBlocks.Count>0 || AnyOrBlocks.Count>0 || AllAndBlocks.Count>0 || AllOrBlocks.Count>0;
}
