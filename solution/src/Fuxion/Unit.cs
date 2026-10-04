using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[JsonConverter(typeof(UnitJsonConverter))]
public readonly struct Unit
{
	public static readonly Unit Value = default;
}

// Public only because consumer source-generated contexts instantiate it (SYSLIB1220 otherwise); not meant to be used directly.
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class UnitJsonConverter : JsonConverter<Unit>
{
	public override Unit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return Unit.Value;

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			reader.Read();
			if (reader.TokenType == JsonTokenType.EndObject)
				return Unit.Value;
		}

		throw new JsonException("Unit must be null or an empty json object");
	}

	public override void Write(Utf8JsonWriter writer, Unit value, JsonSerializerOptions options)
		=> writer.WriteNullValue();
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member