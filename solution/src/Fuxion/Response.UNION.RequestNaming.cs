namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Rewrites the property names of a JSON document so that a case-insensitive binder matches the CLR
/// property names, whatever separated naming policy (snake_case, kebab-case) the sender used. Values
/// are copied verbatim: numbers keep their exact text and strings are never touched.
/// </summary>
/// <remarks>
/// Two shapes are provided. The type-less overloads (<see cref="Transcode(string)"/> and
/// <see cref="Transcode(ReadOnlySpan{byte})"/>) are a purely textual transcoder: they walk the token stream and
/// rewrite every <see cref="JsonTokenType.PropertyName"/> they see, because they cannot tell a CLR property key
/// from a data key. This means dictionary keys, free-form <c>JsonElement</c>/<c>JsonNode</c> content and
/// <c>[JsonExtensionData]</c> keys are rewritten exactly like regular properties: a <c>naming=snake</c> or
/// <c>naming=kebab</c> body read this way must therefore not carry user-supplied keys containing <c>_</c> or
/// <c>-</c> inside a dictionary, or those keys are corrupted on the way in.
/// The type-guided overload (<see cref="Transcode(ReadOnlySpan{byte}, Type, JsonSerializerOptions)"/>) does not
/// share that limitation: it walks the target type's metadata alongside the token stream, so only a key that
/// matches a CLR property is renamed and dictionary/free-form content is left untouched. It is what the
/// ASP.NET Core binding support (<c>RequestNamingEndpoint</c>, <c>ResponseNamingInputFormatter</c>) uses.
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

	/// <summary>
	/// Rewrites separator-cased keys guided by the target type: only keys that match a property are renamed;
	/// dictionary keys, free JSON and unknown keys are left untouched. Malformed input throws <see cref="JsonException"/>.
	/// </summary>
	/// <remarks>
	/// For a polymorphic body (a base type annotated with <see cref="System.Text.Json.Serialization.JsonDerivedTypeAttribute"/>),
	/// only the base type's own declared properties are known while walking the token stream: this method does
	/// not read the type discriminator to resolve which derived type is actually present, so a snake_case or
	/// kebab-case key that exists only on a derived type is left untouched (and therefore fails to bind, since
	/// the deserializer never sees it renamed to match the derived property). Resolving the concrete derived
	/// type from the discriminator up front, so its properties are known too, is a possible follow-up.
	/// </remarks>
	public static byte[] Transcode(ReadOnlySpan<byte> utf8Json, Type bodyType, JsonSerializerOptions options)
	{
		// A freshly constructed JsonSerializerOptions only gets a reflection-based TypeInfoResolver once
		// JsonSerializer.Serialize/Deserialize has run on it at least once; GetTypeInfo alone never populates
		// it and throws NotSupportedException instead. MakeReadOnly(populateMissingResolver: true) is the
		// sanctioned STJ 8+ way to force that: it is idempotent, only fills in the default resolver when
		// none is configured (a caller's own TypeInfoResolverChain is never discarded), and freezes the
		// options explicitly here rather than as a side effect of the first GetTypeInfo call below.
		options.MakeReadOnly(populateMissingResolver: true);

		var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
		using var buffer = new MemoryStream(utf8Json.Length);
		using var writer = new Utf8JsonWriter(buffer);

		var containers = new Stack<JsonTypeInfo?>();
		var pending = Resolve(options, bodyType); // type of the next value to be read

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.StartObject:
					writer.WriteStartObject();
					containers.Push(pending);
					pending = null;
					break;
				case JsonTokenType.StartArray:
					writer.WriteStartArray();
					containers.Push(pending);
					pending = pending is { Kind: JsonTypeInfoKind.Enumerable } array ? Resolve(options, array.ElementType) : null;
					break;
				case JsonTokenType.EndObject:
				case JsonTokenType.EndArray:
					if (reader.TokenType == JsonTokenType.EndObject) writer.WriteEndObject(); else writer.WriteEndArray();
					containers.Pop();
					// Back inside an array: the next value is another element.
					pending = containers.Count > 0 && containers.Peek() is { Kind: JsonTypeInfoKind.Enumerable } parent ? Resolve(options, parent.ElementType) : null;
					break;
				case JsonTokenType.PropertyName:
					var key = reader.GetString()!;
					var container = containers.Peek();
					if (container is { Kind: JsonTypeInfoKind.Object } && TryMatchProperty(container, key, out var property))
					{
						writer.WritePropertyName(property.Name);
						pending = Resolve(options, property.PropertyType);
					}
					else
					{
						writer.WritePropertyName(key);
						pending = container is { Kind: JsonTypeInfoKind.Dictionary } dictionary ? Resolve(options, dictionary.ElementType) : null;
					}
					break;
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

	static JsonTypeInfo? Resolve(JsonSerializerOptions options, Type? type)
	{
		if (type is null) return null;
		if (type.IsGenericType)
		{
			var definition = type.GetGenericTypeDefinition();
			if (definition == typeof(Nullable<>) || definition == typeof(Undefinable<>)) return Resolve(options, type.GetGenericArguments()[0]);
		}
		try { return options.GetTypeInfo(type); }
		catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException) { return null; }
	}

	static bool TryMatchProperty(JsonTypeInfo container, string key, out JsonPropertyInfo property)
	{
		var normalized = NormalizePropertyName(key);
		foreach (var candidate in container.Properties)
		{
			if (candidate.IsExtensionData) continue;
			if (string.Equals(candidate.Name, normalized, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Name, key, StringComparison.OrdinalIgnoreCase))
			{
				property = candidate;
				return true;
			}
		}
		property = null!;
		return false;
	}
}
