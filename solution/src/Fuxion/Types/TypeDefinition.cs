using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fuxion.Types;

/// <summary>
/// The definition of a type: its key, its exact revision, its arity, its base, its interfaces and the JSON Schema of its
/// data in that revision.
/// </summary>
/// <remarks>
/// <para>
/// Only the key is the identity of the type: two revisions of the same major version have the same key, though their
/// schemas differ. The base and the interfaces are named by their <see cref="UriKey"/>: a construction, when they are
/// generic and have arguments.
/// </para>
/// <para>
/// A definition can exist without a .NET type (a type defined as data, as the types of document), and a
/// <see cref="TypeRegistry"/> knows it all the same.
/// </para>
/// </remarks>
public sealed class TypeDefinition
{
	/// <summary>Creates the definition of a type, and checks it.</summary>
	/// <param name="key">Its key: a <see cref="UriKey"/> without arguments.</param>
	/// <param name="revision">Its exact revision, of the same major version as its key.</param>
	/// <param name="arity">How many type arguments it takes: 0 if it is not generic.</param>
	/// <param name="base">Its base, if it has one with a key.</param>
	/// <param name="interfaces">Its interfaces with a key, without repeating any.</param>
	/// <param name="schema">The JSON Schema 2020-12 of its data in this revision (an object or a boolean), if it has one.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="revision"/> is <see langword="null"/>, or an interface is.</exception>
	/// <exception cref="ArgumentException">Something does not fit: the key is a construction, the revision is of another major version, the type is its own base or interface, an interface is repeated, or the schema is neither an object nor a boolean.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="arity"/> is negative.</exception>
	public TypeDefinition(UriKey key, SemanticVersion revision, int arity = 0, UriKey? @base = null, IEnumerable<UriKey>? interfaces = null, JsonElement? schema = null)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (revision is null) throw new ArgumentNullException(nameof(revision));
		if (key.IsConstruction) throw new ArgumentException($"The key of a definition has no arguments: '{key}' is a construction.", nameof(key));
		if (revision.Major != key.Major)
			throw new ArgumentException($"The revision {revision} is not of the major version of its key, v{key.Major}.", nameof(revision));
		if (arity < 0) throw new ArgumentOutOfRangeException(nameof(arity), arity, "The arity of a type is 0 or more.");
		if (@base is not null && @base.Key == key) throw new ArgumentException($"'{key}' cannot be its own base.", nameof(@base));
		List<UriKey> list = [];
		HashSet<UriKey> seen = [];
		foreach (var @interface in interfaces ?? Array.Empty<UriKey>())
		{
			if (@interface is null) throw new ArgumentNullException(nameof(interfaces), "An interface of a definition cannot be null.");
			if (@interface.Key == key) throw new ArgumentException($"'{key}' cannot be its own interface.", nameof(interfaces));
			if (!seen.Add(@interface)) throw new ArgumentException($"The interface '{@interface}' is repeated.", nameof(interfaces));
			list.Add(@interface);
		}
		if (schema is { ValueKind: not (JsonValueKind.Object or JsonValueKind.True or JsonValueKind.False) })
			throw new ArgumentException($"A JSON Schema is an object or a boolean, not a {schema.Value.ValueKind}.", nameof(schema));
		Key = key;
		Revision = revision;
		Arity = arity;
		Base = @base;
		Interfaces = list.AsReadOnly();
		Schema = schema?.Clone();
	}

	/// <summary>Its key, the identity of the type.</summary>
	public UriKey Key { get; }

	/// <summary>Its exact revision.</summary>
	public SemanticVersion Revision { get; }

	/// <summary>How many type arguments it takes: 0 if it is not generic.</summary>
	public int Arity { get; }

	/// <summary>Its base, if it has one with a key.</summary>
	public UriKey? Base { get; }

	/// <summary>Its interfaces with a key.</summary>
	public IReadOnlyList<UriKey> Interfaces { get; }

	/// <summary>The JSON Schema 2020-12 of its data in this revision, if it has one. It is not part of its identity.</summary>
	public JsonElement? Schema { get; }

	/// <summary>Its key and its revision.</summary>
	/// <returns><c>key revision</c>.</returns>
	public override string ToString() => $"{Key} {Revision}";
}
