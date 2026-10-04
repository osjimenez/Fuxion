using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Fuxion.Text.Json;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


/// <summary>
/// Which CLR types must be (de)serialized with System.Text.Json for the union contract to hold: the union
/// types themselves, <see cref="Undefinable{TValue}"/>, and any type whose public property graph reaches one.
/// Lets a host keep its existing serializer for everything else.
/// </summary>
public static class FuxionJsonTypes
{
	static readonly ConcurrentDictionary<Type, bool> cache = new();

	/// <summary>
	/// Whether <paramref name="type"/> is a Fuxion union type (<see cref="Error"/>, <see cref="None"/>,
	/// <see cref="Unit"/>, <see cref="Response{TSuccess}"/>, <see cref="Response{TSuccess, TError}"/>,
	/// <see cref="ResponseMaybe{TSuccess}"/>, <see cref="ResponseMaybe{TSuccess, TError}"/>,
	/// <see cref="Undefinable{TValue}"/>, <see cref="FileContent"/>) or reaches one through its public
	/// property graph (recursively; through arrays, <see cref="IEnumerable{T}"/>, <see cref="Nullable{T}"/>
	/// and the generic arguments of a dictionary).
	/// </summary>
	public static bool RequiresSystemTextJson(Type type)
		=> cache.GetOrAdd(type, static t => Requires(t, new HashSet<Type>()));

	static bool Requires(Type type, HashSet<Type> visiting)
	{
		if (IsFuxionType(type)) return true;
		if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(object) || type == typeof(decimal)
			|| type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid)
			|| type == typeof(JsonElement) || type.Namespace?.StartsWith("System.Text.Json", StringComparison.Ordinal) == true)
			return false;
		if (!visiting.Add(type)) return false; // cycle: the other branch decides

		if (type.IsArray) return Requires(type.GetElementType()!, visiting);
		if (type.IsGenericType)
		{
			// Nullable<T>, IEnumerable<T>, Dictionary<K,V>, tuples...: whatever travels inside decides.
			if (type.GetGenericArguments().Any(a => Requires(a, visiting))) return true;
			if (type.GetGenericTypeDefinition() == typeof(Nullable<>)) return false;
		}

		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead);
		if (properties.Any(p => Requires(p.PropertyType, visiting))) return true;

		// GetProperties on an interface only returns members declared directly on it - a member inherited
		// from a base interface (e.g. IDerived : IBase, with IBase declaring the property) is not included.
		if (type.IsInterface && type.GetInterfaces().Any(i => Requires(i, visiting))) return true;

		return false;
	}

	static bool IsFuxionType(Type type)
	{
		if (type == typeof(Error) || type == typeof(None) || type == typeof(Unit) || type == typeof(FileContent)) return true;
		if (!type.IsGenericType) return false;
		var definition = type.GetGenericTypeDefinition();
		return definition == typeof(Response<>) || definition == typeof(Response<,>)
			|| definition == typeof(ResponseMaybe<>) || definition == typeof(ResponseMaybe<,>)
			|| definition == typeof(Undefinable<>);
	}
}
