using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Fuxion.Reflection;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// The extensions of a value (a response or an error): part of the value, fixed when it is created and never
/// changed afterwards, so copies of the value never share mutable state. To "add" one, create another value with
/// <c>WithExtension</c>. <see cref="ExtensionsDictionary"/> is the mutable type to build them.
/// </summary>
public abstract class ImmutableExtensions : IReadOnlyDictionary<string, object?>, IEquatable<ImmutableExtensions>
{
	private protected ImmutableExtensions(HashSet<string> reservedKeys, Dictionary<string, object?> items)
	{
		ReservedKeys = reservedKeys;
		Items = items;
	}

	private protected Dictionary<string, object?> Items { get; }

	internal HashSet<string> ReservedKeys { get; }

	// Copies the entries, checking them against the reserved keys of the owner.
	private protected static Dictionary<string, object?> Copy(IEnumerable<KeyValuePair<string, object?>>? source, HashSet<string> reservedKeys, IEqualityComparer<string> comparer)
	{
		var copy = new Dictionary<string, object?>(comparer);
		if (source is null)
			return copy;
		foreach (var pair in source)
		{
			if (reservedKeys.Contains(pair.Key))
				throw new ReservedKeyExtensionException(pair.Key);
			copy[pair.Key] = pair.Value;
		}
		return copy;
	}

	private protected static IEqualityComparer<string> ComparerOf(IEnumerable<KeyValuePair<string, object?>>? source) => source switch
	{
		ImmutableExtensions immutable => immutable.Items.Comparer,
		ExtensionsDictionary dictionary => dictionary.Comparer,
		_ => StringComparer.Ordinal
	};

	public T? GetAs<T>(string key, JsonSerializerOptions? options = null)
	{
		if (!Items.TryGetValue(key, out var value))
			throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary");
		return value switch
		{
			null => default,
			T typed => typed,
			JsonElement json => json.Deserialize<T>(options),
			_ => throw new InvalidCastException($"The value associated with key '{key}' is neither '{typeof(T).GetSignature()}' nor '{nameof(JsonElement)}'.")
		};
	}

	public bool TryGetAs<T>(string key, [NotNullWhen(true)] out T? value, JsonSerializerOptions? options = null)
	{
		if (!Items.ContainsKey(key))
		{
			value = default;
			return false;
		}

		try
		{
			value = GetAs<T>(key, options);
			return value is not null;
		}
		catch (Exception ex) when (ex is InvalidCastException or JsonException or NotSupportedException)
		{
			value = default;
			return false;
		}
	}

	#region IReadOnlyDictionary<string, object?> implementation
	public object? this[string key] => Items[key];
	public IEnumerable<string> Keys => Items.Keys;
	public IEnumerable<object?> Values => Items.Values;
	public int Count => Items.Count;
	public bool ContainsKey(string key) => Items.ContainsKey(key);
	public bool TryGetValue(string key, out object? value) => Items.TryGetValue(key, out value);
	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Items.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion

	#region Equality
	// By content: same keys with equal values, in any order. Values are compared with Equals.
	public bool Equals(ImmutableExtensions? other)
	{
		if (ReferenceEquals(this, other))
			return true;
		if (other is null || other.GetType() != GetType() || other.Count != Count)
			return false;
		foreach (var pair in Items)
			if (!other.Items.TryGetValue(pair.Key, out var value) || !object.Equals(pair.Value, value))
				return false;
		return true;
	}

	public override bool Equals(object? obj) => obj is ImmutableExtensions other && Equals(other);

	// Order-independent and consistent with Equals for any key comparer.
	public override int GetHashCode()
	{
		var hash = Count;
		foreach (var key in Items.Keys)
			hash ^= StringComparer.OrdinalIgnoreCase.GetHashCode(key);
		return hash;
	}

	public static bool operator ==(ImmutableExtensions? left, ImmutableExtensions? right) => left is null ? right is null : left.Equals(right);
	public static bool operator !=(ImmutableExtensions? left, ImmutableExtensions? right) => !(left == right);
	#endregion
}

public sealed class ImmutableExtensions<TExtended> : ImmutableExtensions
{
	ImmutableExtensions(HashSet<string> reservedKeys, Dictionary<string, object?> items) : base(reservedKeys, items) { }

	static readonly ConditionalWeakTable<HashSet<string>, ImmutableExtensions<TExtended>> empties = new();

	/// <summary>The empty extensions for these reserved keys: one shared instance, so an empty value allocates nothing.</summary>
	public static ImmutableExtensions<TExtended> Empty(HashSet<string> reservedKeys)
		=> empties.GetValue(reservedKeys, keys => new(keys, new(StringComparer.Ordinal)));

	/// <summary>
	/// The extensions an owner keeps: the same instance when it was already checked against these reserved keys, a
	/// checked copy otherwise (so a dictionary passed when creating a value can be changed afterwards without effect).
	/// </summary>
	public static ImmutableExtensions<TExtended> For(HashSet<string> reservedKeys, IEnumerable<KeyValuePair<string, object?>>? source)
	{
		if (source is ImmutableExtensions<TExtended> immutable && ReferenceEquals(immutable.ReservedKeys, reservedKeys))
			return immutable;
		var items = Copy(source, reservedKeys, ComparerOf(source));
		return items.Count == 0 ? Empty(reservedKeys) : new(reservedKeys, items);
	}

	public ImmutableExtensions<TExtended> With(string key, object? value)
	{
		if (ReservedKeys.Contains(key))
			throw new ReservedKeyExtensionException(key);
		return new(ReservedKeys, new(Items, Items.Comparer) { [key] = value });
	}

	public ImmutableExtensions<TExtended> With(IEnumerable<KeyValuePair<string, object?>> extensions)
	{
		var items = new Dictionary<string, object?>(Items, Items.Comparer);
		foreach (var pair in Copy(extensions, ReservedKeys, Items.Comparer))
			items[pair.Key] = pair.Value;
		return new(ReservedKeys, items);
	}

	public ImmutableExtensions<TExtended> Without(string key)
	{
		if (!Items.ContainsKey(key))
			return this;
		var items = new Dictionary<string, object?>(Items, Items.Comparer);
		items.Remove(key);
		return items.Count == 0 ? Empty(ReservedKeys) : new(ReservedKeys, items);
	}

	// A snapshot of the dictionary; the owner checks it against its own reserved keys when it receives it.
	public static implicit operator ImmutableExtensions<TExtended>(ExtensionsDictionary extensions)
	{
		if (extensions is null)
			throw new ArgumentNullException(nameof(extensions));
		return new(extensions.ReservedKeys, Copy(extensions, extensions.ReservedKeys, extensions.Comparer));
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
