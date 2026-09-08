using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fuxion.Reflection;

namespace Fuxion;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class ResponseExtensions
{
	extension<TSuccess>(Response<TSuccess> me) where TSuccess : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<Error, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out Error error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
	extension<TSuccess, TError>(Response<TSuccess, TError> me) where TSuccess : notnull where TError : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<TError, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out TError? error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
	extension<TSuccess>(ResponseMaybe<TSuccess> me) where TSuccess : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<Error, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out Error error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
	extension<TSuccess, TError>(ResponseMaybe<TSuccess, TError> me) where TSuccess : notnull where TError : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<TError, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out TError? error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
}
public class ResponseSuccessException(string message) : FuxionException(message) { }
public class ExtensionsDictionary : IDictionary<string, object?>
{
	public ExtensionsDictionary() : this([], null) { }

	public ExtensionsDictionary(HashSet<string> reservedKeys) : this(reservedKeys, null) { }

	public ExtensionsDictionary(HashSet<string> reservedKeys, IEqualityComparer<string>? comparer)
	{
		dic = new Dictionary<string, object?>(comparer ?? StringComparer.Ordinal);
		ReservedKeys = reservedKeys ?? throw new ArgumentNullException(nameof(reservedKeys));
	}

	protected ExtensionsDictionary(ExtensionsDictionary extensionDictionary)
	{
		dic = new Dictionary<string, object?>(extensionDictionary.dic, extensionDictionary.dic.Comparer);
		ReservedKeys = extensionDictionary.ReservedKeys;
	}

	protected readonly Dictionary<string, object?> dic;
	private HashSet<string> reservedKeys = [];

	internal HashSet<string> ReservedKeys
	{
		get => reservedKeys;
		set
		{
			reservedKeys = value ?? throw new ArgumentNullException(nameof(value));
			EnsureHasNoReservedKeys();
		}
	}

	internal static ExtensionsDictionary EnsureReservedKeys(ExtensionsDictionary? extensions, HashSet<string> reservedKeys)
	{
		extensions ??= [];
		extensions.ReservedKeys = reservedKeys;
		return extensions;
	}

	internal static ExtensionsDictionary<TExtended> EnsureReservedKeys<TExtended>(ExtensionsDictionary? extensions, HashSet<string> reservedKeys)
		=> [with(EnsureReservedKeys(extensions, reservedKeys))];

	private bool IsReserved(string key)
		=> ReservedKeys.Contains(key);

	private void EnsureHasNoReservedKeys()
	{
		foreach (var key in dic.Keys)
			if (IsReserved(key))
				throw new ReservedKeyExtensionException(key);
	}

	public T? GetAs<T>(string key, JsonSerializerOptions? options = null)
	{
		if (IsReserved(key))
			throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary");
		if (dic.TryGetValue(key, out var value))
		{
			if (value is null)
				return default;
			if (value is T tValue)
				return tValue;
			if (value is JsonElement json)
			{
				var val = json.Deserialize<T>(options);
				this[key] = val;
				return val;
			}
			throw new InvalidCastException($"The value associated with key '{key}' is neither '{typeof(T).GetSignature()}' nor '{nameof(JsonElement)}'.");
		}
		throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary");
	}

	public bool TryGetAs<T>(string key, [NotNullWhen(true)] out T? value, JsonSerializerOptions? options = null)
	{
		if (IsReserved(key))
		{
			value = default;
			return false;
		}

		try
		{
			value = GetAs<T>(key, options);
			return value is not null;
		}
		catch
		{
			value = default;
			return false;
		}
	}

	#region IDictionary<string, object?> implementation
	public object? this[string key]
	{
		get => dic[key];
		set
		{
			if (IsReserved(key))
				throw new ReservedKeyExtensionException(key);
			dic[key] = value;
		}
	}

	public ICollection<string> Keys
	{
		get
		{
			var keys = new List<string>(dic.Count);
			foreach (var key in dic.Keys)
				if (!IsReserved(key))
					keys.Add(key);
			return keys;
		}
	}

	public ICollection<object?> Values
	{
		get
		{
			var values = new List<object?>(dic.Count);
			foreach (var kvp in dic)
				if (!IsReserved(kvp.Key))
					values.Add(kvp.Value);
			return values;
		}
	}

	public int Count
	{
		get
		{
			var count = 0;
			foreach (var key in dic.Keys)
				if (!IsReserved(key))
					count++;
			return count;
		}
	}

	public bool IsReadOnly => false;

	public void Add(string key, object? value)
	{
		if (IsReserved(key))
			throw new ReservedKeyExtensionException(key);
		dic.Add(key, value);
	}

	public void Add(KeyValuePair<string, object?> item)
	{
		if (IsReserved(item.Key))
			throw new ReservedKeyExtensionException(item.Key);
		((IDictionary<string, object?>)dic).Add(item);
	}

	public void Clear() => dic.Clear();

	public bool Contains(KeyValuePair<string, object?> item)
	{
		if (IsReserved(item.Key)) return false;
		return ((ICollection<KeyValuePair<string, object?>>)dic).Contains(item);
	}

	public bool ContainsKey(string key)
	{
		if (IsReserved(key)) return false;
		return dic.ContainsKey(key);
	}

	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));
		if (arrayIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		var count = Count;
		if (array.Length - arrayIndex < count)
			throw new ArgumentException("The destination array has insufficient space.", nameof(array));

		foreach (var kvp in dic)
			if (!IsReserved(kvp.Key))
				array[arrayIndex++] = kvp;
	}

	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
	{
		foreach (var kvp in dic)
			if (!IsReserved(kvp.Key))
				yield return kvp;
	}

	public bool Remove(string key)
	{
		if (IsReserved(key)) return false;
		return dic.Remove(key);
	}

	public bool Remove(KeyValuePair<string, object?> item)
	{
		if (IsReserved(item.Key)) return false;
		return ((IDictionary<string, object?>)dic).Remove(item);
	}

	public bool TryGetValue(string key, out object? value)
	{
		if (IsReserved(key))
		{
			value = null;
			return false;
		}
		return dic.TryGetValue(key, out value);
	}

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	#endregion
}

public class ExtensionsDictionary<TExtended> : ExtensionsDictionary
{
	public ExtensionsDictionary() : base() { }

	public ExtensionsDictionary(HashSet<string> reservedKeys) : base(reservedKeys) { }

	public ExtensionsDictionary(HashSet<string> reservedKeys, IEqualityComparer<string>? comparer) : base(reservedKeys, comparer) { }

	public ExtensionsDictionary(ExtensionsDictionary extensionDictionary) : base(extensionDictionary) { }
}

public class ReservedKeyExtensionException(string key) : FuxionException($"The key '{key}' is reserved and cannot be added to {nameof(ExtensionsDictionary)}.")
{
	public string Key { get; set; } = key;
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member