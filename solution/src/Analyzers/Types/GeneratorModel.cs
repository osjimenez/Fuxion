using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fuxion.Analyzers.Types;

// What an incremental generator keeps between its steps has to compare by value, or every change in the editor would run
// it all again: arrays, locations and diagnostics, without the symbols and the syntax trees they come from.

readonly struct EquatableArray<T>(T[]? items) : IEquatable<EquatableArray<T>>, IEnumerable<T> where T : IEquatable<T>
{
	readonly T[]? items = items;

	public bool IsEmpty => items is null || items.Length == 0;
	public int Length => items?.Length ?? 0;
	public T this[int index] => items![index];

	public bool Equals(EquatableArray<T> other) => (items ?? []).SequenceEqual(other.items ?? []);
	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);
	public override int GetHashCode()
	{
		var hash = 17;
		foreach (var item in items ?? []) hash = unchecked(hash * 31 + (item?.GetHashCode() ?? 0));
		return hash;
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(items ?? [])).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public static implicit operator EquatableArray<T>(T[] items) => new(items);
}

sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
	public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);

	public static LocationInfo? From(Location? location) =>
		location?.SourceTree is null ? null : new(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
}

sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> Arguments)
{
	public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location?.ToLocation(), Arguments.Cast<object>().ToArray());

	public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] arguments) => new(descriptor, location, arguments);
}
