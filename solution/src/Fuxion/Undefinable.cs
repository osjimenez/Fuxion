using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Fuxion;

public interface IUndefinable<out T>
{
	T Value { get; }
	bool IsDefined { get; }
	bool IsUndefined { get; }
	T? ValueOrDefault { get; }
}

[JsonConverter(typeof(UndefinableConverterFactory))]
public readonly struct Undefinable<T> : IUndefinable<T>
{
	// This constructor must exist for deserialization
	public Undefinable(T value)
	{
		Value = value;
		IsDefined = true;
	}

	private Undefinable(T value, bool isDefined)
	{
		Value = value;
		IsDefined = isDefined;
	}

	public T Value
	{
		private init;
		get => !IsDefined ? throw new UndefinedException($"Value cannot be get if '{nameof(IsDefined)}' is false") : field;
	}

	// Implicit conversion to T
	public static implicit operator T(Undefinable<T> undefinable) => undefinable.Value;
	public static implicit operator Undefinable<T>(T value) => new(value);

	public bool IsDefined { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsUndefined => !IsDefined;

	public T? ValueOrDefault => IsDefined ? Value : default;

	public static Undefinable<T> Undefined => new(default!, false);

	public override string ToString() => IsDefined ? Value?.ToString() ?? "null" : "undefined";
}

public class UndefinedException(string message) : Exception(message);

public class UndefinableConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => type.IsSubclassOfRawGeneric(typeof(Undefinable<>));

	public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
		=> (JsonConverter?)Activator.CreateInstance(typeof(UndefinableConverter<>).MakeGenericType(type.GetGenericArguments()[0])) ?? null;
}

public class UndefinableConverter<T> : JsonConverter<Undefinable<T?>>
{
	static readonly Dictionary<Type, bool> UndefinableTypes = new();

	public override bool CanConvert(Type type)
	{
		if (UndefinableTypes.ContainsKey(type)) return true;
		if (type.IsSubclassOfRawGeneric(typeof(Undefinable<>)))
		{
			UndefinableTypes.Add(type, true);
			return true;
		}

		return false;
	}

	public override Undefinable<T?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			var node = JsonNode.Parse(ref reader);
			if (node is JsonArray ja && ja.Count == 1 && ja[0] == null) return Undefinable<T?>.Undefined;
			return new(node.Deserialize<T>(options));
		}

		return new(JsonSerializer.Deserialize<T>(ref reader, options));
	}

	public override void Write(Utf8JsonWriter writer, Undefinable<T?> value, JsonSerializerOptions options)
	{
		if (value.IsUndefined)
		{
			writer.WriteStartArray();
			writer.WriteNullValue();
			writer.WriteEndArray();
		} else
			JsonSerializer.Serialize(writer, value.Value, options);
	}
}