using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Text.Json;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public abstract class ErrorTypeJsonConverter
{
	public abstract bool TryRead(JsonElement element, out object? value, JsonSerializerOptions options);
	public abstract bool CanConvert(object? errorType);
	public abstract void Write(Utf8JsonWriter writer, object errorType, JsonSerializerOptions options);
}
public class HttpStatusCodeErrorTypeJsonConverter : ErrorTypeJsonConverter
{
	public override bool TryRead(JsonElement element, out object? value, JsonSerializerOptions options)
	{
		value = default;

		if (element.ValueKind != JsonValueKind.Object)
			return false;

		var count = 0;
		foreach (var _ in element.EnumerateObject())
			count++;

		if (count != 3)
			return false;

		if (!element.TryGetProperty("$type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
			return false;

		if (!element.TryGetProperty("Name", options, out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
			return false;

		if (!element.TryGetProperty("Code", options, out var codeElement) || codeElement.ValueKind != JsonValueKind.Number)
			return false;

		if (!string.Equals(typeElement.GetString(), nameof(HttpStatusCode), StringComparison.Ordinal))
			return false;

		if (!codeElement.TryGetInt32(out var code))
			return false;

		var statusCode = (HttpStatusCode)code;
		if (!string.Equals(nameElement.GetString(), statusCode.ToString(), StringComparison.Ordinal))
			return false;

		value = statusCode;
		return true;
	}
	public override bool CanConvert(object? errorType) => errorType is HttpStatusCode;
	public override void Write(Utf8JsonWriter writer, object errorType, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("$type", nameof(HttpStatusCode));
		var statusCode = (HttpStatusCode)errorType;
		writer.WriteString(options.ApplyNamingPolicy("Name"), statusCode.ToString());
		writer.WriteNumber(options.ApplyNamingPolicy("Code"), (int)statusCode);
		writer.WriteEndObject();
	}
}
public sealed class ErrorJsonConverter : JsonConverter<Error>
{
	// Process-wide startup configuration: converters can be added until the first error is read or written,
	// then the list is frozen so it is never mutated while it is being enumerated.
	static readonly List<ErrorTypeJsonConverter> errorTypeConverters = [new HttpStatusCodeErrorTypeJsonConverter()];
	static bool errorTypeConvertersFrozen;

	public static IReadOnlyList<ErrorTypeJsonConverter> ErrorTypeConverters => errorTypeConverters;
	public static bool WriteSourceInformation { get; set; } = true;

	public static void AddErrorTypeConverter(ErrorTypeJsonConverter converter)
	{
		if (converter is null) throw new ArgumentNullException(nameof(converter));
		lock (errorTypeConverters)
		{
			if (errorTypeConvertersFrozen)
				throw new InvalidOperationException("Error type converters are startup configuration: add them before the first error is serialized or deserialized.");
			errorTypeConverters.Add(converter);
		}
	}

	internal static IReadOnlyList<ErrorTypeJsonConverter> UseErrorTypeConverters()
	{
		lock (errorTypeConverters)
			errorTypeConvertersFrozen = true;
		return errorTypeConverters;
	}

	public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return default;

		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Expected StartObject token when deserializing '{nameof(Error)}'.");

		using var json = JsonDocument.ParseValue(ref reader);
		var root = json.RootElement;
		var comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		var payloadPropertyName = options.ApplyNamingPolicy(nameof(Error.Payload));
		var messagePropertyName = options.ApplyNamingPolicy(nameof(Error.Message));
		var typePropertyName = options.ApplyNamingPolicy(nameof(Error.Type));
		var exceptionPropertyName = options.ApplyNamingPolicy(nameof(Error.Exception));
		var innerErrorsPropertyName = options.ApplyNamingPolicy(nameof(Error.InnerErrors));
		var sourcePropertyName = options.ApplyNamingPolicy(nameof(Error.Source));
		var extensions = new ExtensionsDictionary<Error>(ErrorConstants.ErrorExtensionsReservedKeys, comparer);

		object? payload = null;
		if (root.TryGetProperty(payloadPropertyName, options, out var payloadElement))
			payload = payloadElement.Deserialize<object?>(options);

		string? message = null;
		if (root.TryGetProperty(messagePropertyName, options, out var messageElement))
			message = messageElement.Deserialize<string?>(options);

		object? type = null;
		if (root.TryGetProperty(typePropertyName, options, out var typeElement))
		{
			var converted = false;
			foreach (var converter in UseErrorTypeConverters())
			{
				if (converter.TryRead(typeElement, out var convertedType, options))
				{
					type = convertedType;
					converted = true;
					break;
				}
			}
			if (!converted)
				type = typeElement.Deserialize<object?>(options);
		}

		foreach (var prop in root.EnumerateObject())
			if (!ErrorConstants.ErrorExtensionsReservedKeys.Contains(prop.Name))
				extensions[prop.Name] = prop.Value.Deserialize<object?>(options);

		Exception? exception = null;
		if (root.TryGetProperty(exceptionPropertyName, options, out var exceptionElement))
		{
			var exceptionJson = exceptionElement.Deserialize<ExceptionJson?>(options);
			if (exceptionJson is not null)
				exception = exceptionJson.ToRemoteException();
		}

		Error[]? innerErrors = null;
		if (root.TryGetProperty(innerErrorsPropertyName, options, out var innerErrorsElement))
			innerErrors = innerErrorsElement.Deserialize<Error[]?>(options);

		ErrorSource? source = null;
		if (root.TryGetProperty(sourcePropertyName, options, out var sourceElement))
			source = sourceElement.Deserialize<ErrorSource?>(options);

		return new Error(message, type, payload, exception, extensions, innerErrors, source);
	}

	public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		if (value.Payload is not null)
		{
			writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Payload)));
			JsonSerializer.Serialize(writer, value.Payload, options);
		}

		if (value.Message is not null)
		{
			writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Message)));
			JsonSerializer.Serialize(writer, value.Message, options);
		}

		if (value.Type is not null)
		{
			writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Type)));
			bool fallbackTypeConverter = true;
			foreach (var converter in UseErrorTypeConverters())
			{
				if (converter.CanConvert(value.Type))
				{
					converter.Write(writer, value.Type, options);
					fallbackTypeConverter = false;
					break;
				}
			}
			if (fallbackTypeConverter)
				JsonSerializer.Serialize(writer, value.Type, options);
		}

		if (value.Exception is not null)
		{
			writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Exception)));
			if (value.Exception is RemoteException remote)
				JsonSerializer.Serialize(writer, remote.ExceptionJson, options);
			else
				JsonSerializer.Serialize(writer, value.Exception.ToExceptionJson(options), options);
		}

		if (value.InnerErrors is not null)
		{
			writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.InnerErrors)));
			JsonSerializer.Serialize(writer, value.InnerErrors, options);
		}

		if (WriteSourceInformation && value.Source is not null)
		{
			writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Source)));
			JsonSerializer.Serialize(writer, value.Source, options);
		}

		foreach (var extension in value.Extensions)
		{
			writer.WritePropertyName(extension.Key);
			if (extension.Value is JsonElement element)
				element.WriteTo(writer);
			else
				JsonSerializer.Serialize(writer, extension.Value, options);
		}

		writer.WriteEndObject();
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
