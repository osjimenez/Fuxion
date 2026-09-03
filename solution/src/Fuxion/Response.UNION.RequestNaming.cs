namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;

/// <summary>
/// Rewrites the property names of a JSON document so that a case-insensitive binder matches the CLR
/// property names, whatever separated naming policy (snake_case, kebab-case) the sender used. Values
/// are copied verbatim: numbers keep their exact text and strings are never touched.
/// </summary>
/// <remarks>
/// This is a textual transcoder: it walks the token stream and rewrites every
/// <see cref="JsonTokenType.PropertyName"/> it sees, because it cannot tell a CLR property key from a
/// data key. This means dictionary keys, free-form <c>JsonElement</c>/<c>JsonNode</c> content and
/// <c>[JsonExtensionData]</c> keys are rewritten exactly like regular properties. A <c>naming=snake</c>
/// or <c>naming=kebab</c> request body must therefore not carry user-supplied keys containing
/// <c>_</c> or <c>-</c> inside a dictionary, or those keys are corrupted on the way in. The documented
/// alternative — resolving the naming per request through dedicated <see cref="JsonSerializerOptions"/>
/// instead of rewriting the wire text — is registered as a follow-up in the project's ideas backlog.
/// </remarks>
public static class JsonNamingTranscoder
{
	public static string NormalizePropertyName(string name)
	{
		if (name.IndexOf('_') < 0 && name.IndexOf('-') < 0) return name;

		var builder = new StringBuilder(name.Length);
		var capitalizeNext = true;
		foreach (var c in name)
		{
			if (c is '_' or '-') { capitalizeNext = true; continue; }
			builder.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
			capitalizeNext = false;
		}
		return builder.ToString();
	}

	public static string Transcode(string json)
		=> Encoding.UTF8.GetString(Transcode(Encoding.UTF8.GetBytes(json)));

	public static byte[] Transcode(ReadOnlySpan<byte> utf8Json)
	{
		// Strict: a snake_case/kebab-case body must not be more lenient than a camelCase one, which is
		// read with the framework's default (strict) reader options.
		var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
		using var buffer = new MemoryStream(utf8Json.Length);
		using var writer = new Utf8JsonWriter(buffer);

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.StartObject: writer.WriteStartObject(); break;
				case JsonTokenType.EndObject: writer.WriteEndObject(); break;
				case JsonTokenType.StartArray: writer.WriteStartArray(); break;
				case JsonTokenType.EndArray: writer.WriteEndArray(); break;
				case JsonTokenType.PropertyName: writer.WritePropertyName(NormalizePropertyName(reader.GetString()!)); break;
				case JsonTokenType.String: writer.WriteStringValue(reader.GetString()); break;
				case JsonTokenType.Number: writer.WriteRawValue(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan, skipInputValidation: true); break;
				case JsonTokenType.True: writer.WriteBooleanValue(true); break;
				case JsonTokenType.False: writer.WriteBooleanValue(false); break;
				case JsonTokenType.Null: writer.WriteNullValue(); break;
			}
		}

		writer.Flush();
		return buffer.ToArray();
	}
}
