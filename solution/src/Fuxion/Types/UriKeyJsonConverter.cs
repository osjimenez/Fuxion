using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Types;

/// <summary>A <see cref="UriKey"/> in JSON: a string with its canonical form, also as the name of a property.</summary>
/// <remarks>Public only because the source-generated JSON contexts of the consumers instantiate it; not meant to be used directly.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class UriKeyJsonConverter : JsonConverter<UriKey>
{
	/// <inheritdoc/>
	public override UriKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String) throw new JsonException($"A UriKey is a JSON string with its canonical form, not a {reader.TokenType}.");
		return Parse(reader.GetString());
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, UriKey value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());

	/// <inheritdoc/>
	public override UriKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Parse(reader.GetString());

	/// <inheritdoc/>
	public override void WriteAsPropertyName(Utf8JsonWriter writer, UriKey value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString());

	static UriKey Parse(string? text) => UriKey.TryParse(text, out var key, out var error) ? key : throw new JsonException(error);
}
