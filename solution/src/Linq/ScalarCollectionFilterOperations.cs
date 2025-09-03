using System;
using System.Collections.Generic;

namespace Fuxion.Linq;

/// <summary>
/// Operaciones sobre una colección escalar con soporte Any/All y grupos internos AND/OR de bloques.
/// </summary>
public class ScalarCollectionFilterOperations<TElement> : ICollectionScalarFilterOperations<TElement>
{
	internal readonly List<FilterOperations<TElement>> AnyAndBlocks = new();
	internal readonly List<FilterOperations<TElement>> AnyOrBlocks = new();
	internal readonly List<FilterOperations<TElement>> AllAndBlocks = new();
	internal readonly List<FilterOperations<TElement>> AllOrBlocks = new();

	public void Any(params Action<FilterOperations<TElement>>[] and)
		=> Any(and: and, or: null);
	public void Any(IEnumerable<Action<FilterOperations<TElement>>>? and = null, IEnumerable<Action<FilterOperations<TElement>>>? or = null)
	{
		if (and != null)
			foreach (var cfg in and)
			{
				var blk = new ElementOperations();
				cfg(blk);
				if (blk.HasAny()) AnyAndBlocks.Add(blk);
			}
		if (or != null)
			foreach (var cfg in or)
			{
				var blk = new ElementOperations();
				cfg(blk);
				if (blk.HasAny()) AnyOrBlocks.Add(blk);
			}
	}

	public void All(params Action<FilterOperations<TElement>>[] and)
		=> All(and: and, or: null);
	public void All(IEnumerable<Action<FilterOperations<TElement>>>? and = null, IEnumerable<Action<FilterOperations<TElement>>>? or = null)
	{
		if (and != null)
			foreach (var cfg in and)
			{
				var blk = new ElementOperations();
				cfg(blk);
				if (blk.HasAny()) AllAndBlocks.Add(blk);
			}
		if (or != null)
			foreach (var cfg in or)
			{
				var blk = new ElementOperations();
				cfg(blk);
				if (blk.HasAny()) AllOrBlocks.Add(blk);
			}
	}

	public bool HasAny() => AnyAndBlocks.Count > 0 || AnyOrBlocks.Count > 0 || AllAndBlocks.Count > 0 || AllOrBlocks.Count > 0;

	public sealed class ElementOperations : FilterOperations<TElement> { }
}
