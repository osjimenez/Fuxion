using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;
using Fuxion.Text.Json;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

static class ErrorConstants
{
	public static readonly HashSet<string> ErrorExtensionsReservedKeys = new(
		[nameof(Error.Payload),
		nameof(Error.Message),
		nameof(Error.Type),
		nameof(Error.Exception),
		 nameof(Error.InnerErrors),
		nameof(Error.Source)],
		StringComparer.OrdinalIgnoreCase);

	public static ExtensionsDictionary<Error> EnsureErrorReservedKeys(ExtensionsDictionary? extensions = null)
		=> ExtensionsDictionary.EnsureReservedKeys<Error>(extensions, ErrorExtensionsReservedKeys);
}

[JsonConverter(typeof(ErrorJsonConverter))]
public readonly record struct Error
{
	internal Error(
		string? message = null,
		object? type = null,
		object? payload = null,
		Exception? exception = null,
		ExtensionsDictionary? extensions = null,
		Error[]? innerErrors = null,
		ErrorSource? source = null)
	{
		Message = message;
		Type = type;
		Payload = payload;
		Exception = exception;
		InnerErrors = innerErrors is null || innerErrors.Length == 0 ? null : innerErrors;
		Extensions = ErrorConstants.EnsureErrorReservedKeys(extensions);
		Source = source;
	}

	public object? Payload { get; init; }

	public string? Message { get; init; }

	public object? Type { get; init; }

	public Exception? Exception { get; init; }

	public Error[]? InnerErrors { get; init; }

	public ExtensionsDictionary<Error> Extensions
	{
		get => field ?? [with(ErrorConstants.ErrorExtensionsReservedKeys)];
		init => field = ErrorConstants.EnsureErrorReservedKeys(value);
	}

	public ErrorSource? Source { get; }

	public T? GetPayloadAs<T>(JsonSerializerOptions? options = null)
	{
		if (Payload is null)
			return default;

		if (Payload is T value)
			return value;

		if (Payload is JsonElement json)
			return json.Deserialize<T>(options);

		throw new InvalidCastException($"Payload is neither '{typeof(T).GetSignature()}' nor '{nameof(JsonElement)}'.");
	}
	public bool TryGetPayloadAs<T>([NotNullWhen(true)] out T? value, JsonSerializerOptions? options = null)
	{
		try
		{
			value = GetPayloadAs<T>(options);
			return value is not null;
		}
		catch
		{
			value = default;
			return false;
		}
	}

	public T? GetTypeAs<T>(JsonSerializerOptions? options = null)
	{
		if (Type is null)
			return default;

		if (Type is T value)
			return value;

		if (Type is JsonElement json)
			return json.Deserialize<T>(options);

		throw new InvalidCastException($"Type is neither '{typeof(T).GetSignature()}' nor '{nameof(JsonElement)}'.");
	}
	public bool TryGetTypeAs<T>([NotNullWhen(true)] out T? value, JsonSerializerOptions? options = null)
	{
		try
		{
			value = GetTypeAs<T>(options);
			return value is not null;
		}
		catch
		{
			value = default;
			return false;
		}
	}
}
public readonly record struct ErrorSource(string MethodName, string File, int Line)
{
	public static ErrorSource FromStackTrace()
	{
		var frame = new StackTrace(true).GetFrame(1);
		return new(
			frame?.GetMethod()?.Name ?? string.Empty,
			frame?.GetFileName() ?? string.Empty,
			frame?.GetFileLineNumber() ?? 0);
	}
}

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
	public static IList<ErrorTypeJsonConverter> ErrorTypeConverters { get; } = [new HttpStatusCodeErrorTypeJsonConverter()];
	public static bool WriteSourceInformation { get; set; } = true;

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
			foreach (var converter in ErrorTypeConverters)
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
			foreach (var converter in ErrorTypeConverters)
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
public static class ErrorExtensions
{
	extension(Error me)
	{
		public Error Wrap(
			string? message = null,
			object? type = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			=> new(
				message ?? me.Message,
				type ?? me.Type,
				payload,
				exception,
				extensions,
				[me],
				new(callerMemberName, callerFilePath, callerLineNumber));

		public static Error Aggregate(params Error[] innerErrors)
		{
			var message = "One or more errors occurred.";
			if (innerErrors is not null && innerErrors.Length > 0)
				message += string.Concat(innerErrors.Where(e => e.Message.IsNeitherNullNorWhiteSpace()).Select(error => $" ({error.Message})"));

			object? type = HttpStatusCode.InternalServerError;

			if (innerErrors is { Length: > 0 })
			{
				var current = innerErrors[0].Type;
				type = current ?? HttpStatusCode.InternalServerError;
				for (var i = 1; i < innerErrors.Length; i++)
					if (!Equals(current, innerErrors[i].Type))
					{
						type = HttpStatusCode.InternalServerError;
						break;
					}
			}
			return new(
					  message: message,
					  type: type,
					  innerErrors: innerErrors is null || innerErrors.Length == 0 ? null : innerErrors,
					  source: ErrorSource.FromStackTrace());
		}

		public static Error Custom(
			string? message = null,
			object? type = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
				=> message is null && type is null && payload is null && exception is null && extensions is null && innerErrors is null
					? throw new ArgumentException("At least one of the parameters must be provided.")
					: new(message, type, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));

		public static Error NotFound(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.NotFound, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsNotFound => me.Type is HttpStatusCode.NotFound;

		public static Error Forbidden(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.Forbidden, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsPermissionDenied => me.Type is HttpStatusCode.Forbidden;

		public static Error Unauthorized(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.Unauthorized, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsUnauthorized => me.Type is HttpStatusCode.Unauthorized;

		public static Error InvalidData(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.BadRequest, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsInvalidData => me.Type is HttpStatusCode.BadRequest;

		public static Error Conflict(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.Conflict, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsConflict => me.Type is HttpStatusCode.Conflict;

		public static Error Critical(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.InternalServerError, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsCritical => me.Type is HttpStatusCode.InternalServerError;

		public static Error NotImplemented(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.NotImplemented, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsNotImplemented => me.Type is HttpStatusCode.NotImplemented;

		public static Error Unavailable(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.ServiceUnavailable, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsUnavailable => me.Type is HttpStatusCode.ServiceUnavailable;

		public static Error Timeout(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.RequestTimeout, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsTimeout => me.Type is HttpStatusCode.RequestTimeout;

		// PEND Combined/Aggregated
	}

}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member