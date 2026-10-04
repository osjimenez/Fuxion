using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Fuxion.Reflection;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public interface IUndefinable : IUnion
{
	bool IsDefined { get; }
	bool IsUndefined { get; }
}

[Union]
[JsonConverter(typeof(UndefinableJsonConverterFactory))]
public readonly struct Undefinable<TValue> : IUndefinable, IEquatable<Undefinable<TValue>>
{
	private const byte UndefinedKind = 0;
	private const byte DefinedKind = 1;

	private readonly byte _kind;
	private readonly TValue? _value;

	static Undefinable()
	{
		if (typeof(TValue) == typeof(None))
			throw new InvalidOperationException($"The {typeof(Undefinable<TValue>).GetSignature()} type arguments are invalid: {nameof(TValue)} cannot be '{nameof(None)}'.");
	}

	public bool IsDefined => _kind == DefinedKind;

	public bool IsUndefined => _kind == UndefinedKind;

	public Undefinable(TValue value)
	{
		_kind = DefinedKind;
		_value = value;
	}

	public Undefinable(None value)
	{
		_kind = UndefinedKind;
		_value = default;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[JsonIgnore]
	public bool HasValue => true;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object? Value => _kind switch
	{
		DefinedKind => _value,
		_ => None.Value
	};

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue(out TValue? value)
	{
		if (IsDefined)
		{
			value = _value;
			return true;
		}

		value = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue(out None value)
	{
		if (IsUndefined)
		{
			value = None.Value;
			return true;
		}

		value = default;
		return false;
	}

	public static implicit operator Undefinable<TValue>(TValue value)
		=> new(value);

	public static implicit operator Undefinable<TValue>(None value)
		=> new(value);

	public static Undefinable<TValue> Undefined => default;

	public bool Equals(Undefinable<TValue> other)
		=> _kind == other._kind &&
			(_kind != DefinedKind || EqualityComparer<TValue?>.Default.Equals(_value, other._value));

	public override bool Equals(object? obj)
		=> obj is Undefinable<TValue> other && Equals(other);

	public override int GetHashCode()
	{
		if (_kind != DefinedKind) return _kind.GetHashCode();
		if (_value is null) return HashCode.Combine(_kind, 0);
		return HashCode.Combine(_kind, EqualityComparer<TValue>.Default.GetHashCode(_value));
	}

	public static bool operator ==(Undefinable<TValue> left, Undefinable<TValue> right)
		=> left.Equals(right);

	public static bool operator !=(Undefinable<TValue> left, Undefinable<TValue> right)
		=> !left.Equals(right);

	public static implicit operator TValue(Undefinable<TValue> value)
		=> value.IsDefined
			? value._value!
			: throw new UndefinedException("Implicit conversion between this undefinable and its value type is not allowed because this undefinable is not defined");

	public static implicit operator None(Undefinable<TValue> value)
		=> value.IsUndefined
			? None.Value
			: throw new UndefinedException("Implicit conversion between this undefinable and None is not allowed because this undefinable is not undefined");
}

/// <summary>
/// Contract customization that omits undefined <see cref="Undefinable{TValue}"/> properties from the JSON output.
/// </summary>
public static class UndefinableJsonTypeInfo
{
	/// <summary>
	/// Omits every property holding an undefined <see cref="Undefinable{TValue}"/> value.
	/// </summary>
	/// <param name="jsonTypeInfo">The contract to modify.</param>
	/// <remarks>
	/// <para>
	/// The absence of a property is the natural JSON representation of an undefined value, and it is the
	/// semantics expected by JSON Merge Patch (RFC 7396), so a consumer does not need to know about this
	/// library to interpret the payload.
	/// </para>
	/// <para>
	/// This cannot be done by a converter, because a converter decides how a value is written but not
	/// whether the property is written at all. Positions without a property, such as the serialization
	/// root, array elements and dictionary values, keep using the marker object emitted by
	/// <see cref="UndefinableJsonConverter{T}"/>.
	/// </para>
	/// <para>
	/// Any previously configured <see cref="JsonPropertyInfo.ShouldSerialize"/> is preserved and still
	/// evaluated for defined values.
	/// </para>
	/// </remarks>
	public static void OmitUndefined(JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object) return;

		foreach (var property in jsonTypeInfo.Properties)
		{
			if (!property.PropertyType.IsSubclassOfGenericDefinition(typeof(Undefinable<>))) continue;

			var previous = property.ShouldSerialize;
			property.ShouldSerialize = (container, value) =>
				value is IUndefinable { IsUndefined: true }
					? false
					: previous is null || previous(container, value);
		}
	}
}

/// <summary>
/// Creates converters for <see cref="Undefinable{TValue}"/> values.
/// </summary>
/// <remarks>
/// The preferred representation of an undefined value is the absence of the JSON property, which is
/// applied by the type info resolver. This factory only covers the positions where omission is not
/// possible, such as the serialization root, array elements and dictionary values.
/// </remarks>
public class UndefinableJsonConverterFactory : JsonConverterFactory
{
	/// <summary>
	/// Name of the marker property used to represent an undefined value when it cannot be omitted.
	/// </summary>
	/// <remarks>
	/// An object marker is used instead of a sentinel string because a string sentinel belongs to the
	/// value domain of <see cref="Undefinable{TValue}"/> of <see cref="string"/> and would silently turn a
	/// legitimate value into an undefined one when round tripping.
	/// </remarks>
	public const string UndefinedMarkerPropertyName = "$undefined";

	public override bool CanConvert(Type type) => type.IsSubclassOfGenericDefinition(typeof(Undefinable<>));

	public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
		=> (JsonConverter?)Activator.CreateInstance(typeof(UndefinableJsonConverter<>).MakeGenericType(type.GetGenericArguments()[0])) ?? null;
}

public class UndefinableJsonConverter<T> : JsonConverter<Undefinable<T?>>
{
	public override bool CanConvert(Type type) => type.IsSubclassOfGenericDefinition(typeof(Undefinable<>));

	public override Undefinable<T?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (IsUndefinedMarker(reader)) 
		{
			reader.Read();
			reader.Read();
			reader.Read();
			return default;
		}

		return new(JsonSerializer.Deserialize<T>(ref reader, options));
	}

	public override void Write(Utf8JsonWriter writer, Undefinable<T?> value, JsonSerializerOptions options)
	{
		if (value.IsUndefined)
		{
			writer.WriteStartObject();
			writer.WriteBoolean(UndefinableJsonConverterFactory.UndefinedMarkerPropertyName, true);
			writer.WriteEndObject();
		}
		else
			JsonSerializer.Serialize(writer, value.Value, options);
	}

	// The reader is a struct, so the copy taken here allows looking ahead without consuming the original.
	static bool IsUndefinedMarker(Utf8JsonReader reader)
	{
		if (reader.TokenType != JsonTokenType.StartObject) return false;
		if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName) return false;
		if (reader.GetString() != UndefinableJsonConverterFactory.UndefinedMarkerPropertyName) return false;
		if (!reader.Read() || reader.TokenType != JsonTokenType.True) return false;
		return reader.Read() && reader.TokenType == JsonTokenType.EndObject;
	}
}

public class UndefinedException(string message) : FuxionException(message);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member