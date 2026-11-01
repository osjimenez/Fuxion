using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fuxion.Types;

/// <summary>
/// The types a process knows: their definitions, by their <see cref="UriKey"/>, and the .NET type of each, when it has one.
/// </summary>
/// <remarks>
/// <para>
/// It starts with the types of the system (<see cref="SystemTypes"/>). The types of an assembly are registered by the
/// code that the generator of Fuxion writes in its class marked with <see cref="TypeDefinitionsAttribute"/>; the
/// definitions without a .NET type, with <see cref="Register(TypeDefinition)"/>. Nothing here uses reflection over
/// members or creates types at run time, so it works with trimming and native AOT.
/// </para>
/// <para>
/// From a .NET type to its key, also for a constructed generic type or an array, which it decomposes. From a key to its
/// .NET type, only what is registered: a key gives its type, or the definition of its generic type; a construction, only
/// if it was registered with <see cref="RegisterConstruction{T}"/>, because creating it at run time would need
/// <c>MakeGenericType</c>.
/// </para>
/// <para>A key, a .NET type or a fingerprint is registered once: a second time fails. It is safe to use from several threads.</para>
/// </remarks>
public sealed class TypeRegistry
{
	/// <summary>Creates a registry with the types of the system.</summary>
	public TypeRegistry()
	{
		foreach (var definition in SystemTypes.All) Add(definition, ClrTypeOf(definition));
	}

	readonly object gate = new();
	readonly ConcurrentDictionary<UriKey, TypeDefinition> definitions = new();
	readonly ConcurrentDictionary<UriKey, Type> typesByKey = new();
	readonly ConcurrentDictionary<Type, UriKey> keysByType = new();
	readonly ConcurrentDictionary<string, UriKey> keysByFingerprint = new(StringComparer.Ordinal);

	/// <summary>The definitions it knows, the types of the system among them.</summary>
	public IReadOnlyCollection<TypeDefinition> Definitions => definitions.Values.ToArray();

	/// <summary>Registers a definition without a .NET type: a type defined as data.</summary>
	/// <param name="definition">The definition.</param>
	/// <exception cref="InvalidOperationException">Its key, or its fingerprint, is registered already.</exception>
	public void Register(TypeDefinition definition)
	{
		if (definition is null) throw new ArgumentNullException(nameof(definition));
		Add(definition, null);
	}

	/// <summary>Registers a type that is not generic with its definition.</summary>
	/// <typeparam name="T">The type.</typeparam>
	/// <param name="definition">Its definition, of arity 0.</param>
	/// <exception cref="ArgumentException">The arity of the definition is not 0.</exception>
	/// <exception cref="InvalidOperationException">The key, its fingerprint or the type is registered already.</exception>
	public void Register<T>(TypeDefinition definition) => Register(typeof(T), definition);

	/// <summary>Registers a type with its definition: a type that is not generic, or the definition of a generic one (<c>typeof(Box&lt;&gt;)</c>).</summary>
	/// <param name="type">The type.</param>
	/// <param name="definition">Its definition, with the arity of the type.</param>
	/// <exception cref="ArgumentException">The type is a constructed generic type, an array or a generic parameter, or its arity is not the one of the definition.</exception>
	/// <exception cref="InvalidOperationException">The key, its fingerprint or the type is registered already.</exception>
	public void Register(Type type, TypeDefinition definition)
	{
		if (type is null) throw new ArgumentNullException(nameof(type));
		if (definition is null) throw new ArgumentNullException(nameof(definition));
		if (type.IsArray || type.IsGenericParameter || type.IsPointer || type.IsByRef || (type.IsGenericType && !type.IsGenericTypeDefinition))
			throw new ArgumentException(
				$"A definition goes with a type that is not generic, or with the definition of a generic one (typeof(Box<>)): '{type}' is not; register a construction with {nameof(RegisterConstruction)}.",
				nameof(type));
		var arity = type.IsGenericTypeDefinition ? type.GetGenericArguments().Length : 0;
		if (arity != definition.Arity)
			throw new ArgumentException($"The type '{type}' takes {arity} type arguments, and the definition of '{definition.Key}' {definition.Arity}.", nameof(definition));
		Add(definition, type);
	}

	/// <summary>Registers a construction, a constructed generic type or an array, so that its key gives it back.</summary>
	/// <typeparam name="T">The constructed type: <c>Box&lt;Invoice&gt;</c>, <c>int[]</c>.</typeparam>
	/// <returns>Its key, the construction.</returns>
	/// <exception cref="ArgumentException">The type is not a construction, or it or one of its arguments has no key here.</exception>
	/// <exception cref="InvalidOperationException">The construction, or its fingerprint, is registered with another type.</exception>
	public UriKey RegisterConstruction<T>() => RegisterConstruction(typeof(T));

	/// <summary>Registers a construction, a constructed generic type or an array, so that its key gives it back.</summary>
	/// <param name="type">The constructed type: <c>typeof(Box&lt;Invoice&gt;)</c>, <c>typeof(int[])</c>.</param>
	/// <returns>Its key, the construction.</returns>
	/// <exception cref="ArgumentException">The type is not a construction, or it or one of its arguments has no key here.</exception>
	/// <exception cref="InvalidOperationException">The construction, or its fingerprint, is registered with another type.</exception>
	public UriKey RegisterConstruction(Type type)
	{
		if (type is null) throw new ArgumentNullException(nameof(type));
		if (!(type.IsConstructedGenericType || IsVector(type)) || type.ContainsGenericParameters)
			throw new ArgumentException($"A construction is a closed constructed generic type or a one-dimensional array: '{type}' is not.", nameof(type));
		if (!TryGetKey(type, out var key))
			throw new ArgumentException($"The type '{type}' has no key here: register the definitions of its type and of its arguments first.", nameof(type));
		lock (gate)
		{
			if (typesByKey.TryGetValue(key, out var registered))
			{
				if (registered == type) return key;
				throw new InvalidOperationException($"The construction '{key}' is registered already, with the type '{registered}'.");
			}
			CheckFingerprint(key);
			typesByKey[key] = type;
			keysByType[type] = key;
			keysByFingerprint[key.Fingerprint] = key;
		}
		return key;
	}

	/// <summary>The definition of a key, or of the generic type of a construction.</summary>
	/// <param name="key">The key, or a construction.</param>
	/// <param name="definition">Its definition, if it is registered.</param>
	/// <returns>Whether it is registered.</returns>
	public bool TryGetDefinition(UriKey key, [NotNullWhen(true)] out TypeDefinition? definition)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));
		return definitions.TryGetValue(key.Key, out definition);
	}

	/// <summary>The .NET type of a key (for a generic type, its definition, <c>typeof(Box&lt;&gt;)</c>), or of a registered construction.</summary>
	/// <param name="key">The key, or a construction.</param>
	/// <param name="type">Its .NET type, if it has one here.</param>
	/// <returns>Whether it has one here.</returns>
	public bool TryGetType(UriKey key, [NotNullWhen(true)] out Type? type)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));
		return typesByKey.TryGetValue(key, out type);
	}

	/// <summary>The key of a .NET type: a registered one, or a constructed generic type or an array whose parts are registered.</summary>
	/// <param name="type">The type.</param>
	/// <param name="key">Its key, or its construction.</param>
	/// <returns>Whether it has one here.</returns>
	public bool TryGetKey(Type type, [NotNullWhen(true)] out UriKey? key)
	{
		if (type is null) throw new ArgumentNullException(nameof(type));
		if (keysByType.TryGetValue(type, out key)) return true;
		key = null;
		if (IsVector(type))
		{
			if (!TryGetKey(type.GetElementType()!, out var element)) return false;
			key = SystemTypes.Array.Key.Construct(element);
			return true;
		}
		if (!type.IsConstructedGenericType || !keysByType.TryGetValue(type.GetGenericTypeDefinition(), out var generic)) return false;
		var arguments = type.GenericTypeArguments;
		var keys = new UriKey[arguments.Length];
		for (var i = 0; i < arguments.Length; i++)
		{
			if (!TryGetKey(arguments[i], out var argument)) return false;
			keys[i] = argument;
		}
		key = generic.Construct(keys);
		return true;
	}

	/// <summary>The key of a .NET type, as <see cref="TryGetKey"/>.</summary>
	/// <param name="type">The type.</param>
	/// <returns>Its key, or its construction.</returns>
	/// <exception cref="KeyNotFoundException">It has none here.</exception>
	public UriKey GetKey(Type type) =>
		TryGetKey(type, out var key) ? key : throw new KeyNotFoundException($"The type '{type}' has no key here: register its definition, and those of its arguments.");

	/// <summary>The key of a .NET type, as <see cref="TryGetKey"/>.</summary>
	/// <typeparam name="T">The type.</typeparam>
	/// <returns>Its key, or its construction.</returns>
	/// <exception cref="KeyNotFoundException">It has none here.</exception>
	public UriKey GetKey<T>() => GetKey(typeof(T));

	/// <summary>The key, or the registered construction, that has a fingerprint.</summary>
	/// <param name="fingerprint">The fingerprint (<see cref="UriKey.Fingerprint"/>).</param>
	/// <param name="key">The key or the construction, if it is registered.</param>
	/// <returns>Whether it is registered.</returns>
	public bool TryGetKeyByFingerprint(string fingerprint, [NotNullWhen(true)] out UriKey? key)
	{
		if (fingerprint is null) throw new ArgumentNullException(nameof(fingerprint));
		return keysByFingerprint.TryGetValue(fingerprint, out key);
	}

	/// <summary>
	/// Whether a key or a construction is valid here: every key in it has a definition, and every construction has the
	/// arity of its definition. A key alone names its type, generic or not.
	/// </summary>
	/// <param name="key">The key or the construction.</param>
	/// <param name="error">Why it is not valid.</param>
	/// <returns>Whether it is valid.</returns>
	public bool IsValid(UriKey key, [NotNullWhen(false)] out string? error)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (!definitions.TryGetValue(key.Key, out var definition))
		{
			error = $"'{key.Key}' has no definition here.";
			return false;
		}
		if (key.IsConstruction && key.Arguments.Count != definition.Arity)
		{
			error = $"'{key.Key}' takes {definition.Arity} type arguments, not {key.Arguments.Count}.";
			return false;
		}
		foreach (var argument in key.Arguments)
			if (!IsValid(argument, out error))
				return false;
		error = null;
		return true;
	}

	void Add(TypeDefinition definition, Type? type)
	{
		var key = definition.Key;
		lock (gate)
		{
			if (definitions.ContainsKey(key)) throw new InvalidOperationException($"The key '{key}' is registered already.");
			if (type is not null && keysByType.TryGetValue(type, out var registered))
				throw new InvalidOperationException($"The type '{type}' is registered already, with the key '{registered}'.");
			CheckFingerprint(key);
			definitions[key] = definition;
			keysByFingerprint[key.Fingerprint] = key;
			if (type is null) return;
			typesByKey[key] = type;
			keysByType[type] = key;
		}
	}

	// Two different keys with the same fingerprint: the registry fails
	void CheckFingerprint(UriKey key)
	{
		if (keysByFingerprint.TryGetValue(key.Fingerprint, out var other) && other != key)
			throw new InvalidOperationException($"'{key}' and '{other}' have the same fingerprint, '{key.Fingerprint}': the registry cannot tell them apart.");
	}

	// A one-dimensional array with a lower bound of zero (T[]); not T[*], a one-dimensional array of other bounds
	static bool IsVector(Type type) => type.IsArray && type.GetArrayRank() == 1 && !type.Name.EndsWith("[*]", StringComparison.Ordinal);

	// The .NET type of a type of the system, if this framework has it; the arrays have none
	static Type? ClrTypeOf(TypeDefinition definition) =>
		SystemTypeKeys.All.First(entry => entry.Key == definition.Key.ToString()).ClrName switch
		{
			"System.Boolean" => typeof(bool),
			"System.Byte" => typeof(byte),
			"System.SByte" => typeof(sbyte),
			"System.Int16" => typeof(short),
			"System.UInt16" => typeof(ushort),
			"System.Int32" => typeof(int),
			"System.UInt32" => typeof(uint),
			"System.Int64" => typeof(long),
			"System.UInt64" => typeof(ulong),
			"System.Single" => typeof(float),
			"System.Double" => typeof(double),
			"System.Decimal" => typeof(decimal),
			"System.Char" => typeof(char),
			"System.String" => typeof(string),
			"System.Object" => typeof(object),
			"System.DateTime" => typeof(DateTime),
			"System.DateTimeOffset" => typeof(DateTimeOffset),
#if !STANDARD_OR_OLD_FRAMEWORKS
			"System.DateOnly" => typeof(DateOnly),
			"System.TimeOnly" => typeof(TimeOnly),
#endif
			"System.TimeSpan" => typeof(TimeSpan),
			"System.Guid" => typeof(Guid),
			"System.Uri" => typeof(Uri),
			"System.Collections.Generic.List`1" => typeof(List<>),
			"System.Collections.Generic.Dictionary`2" => typeof(Dictionary<,>),
			_ => null
		};
}
