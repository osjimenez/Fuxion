using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

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

	public ErrorSource? Source { get; init; }

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
		catch (Exception ex) when (ex is InvalidCastException or JsonException or NotSupportedException)
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
		catch (Exception ex) when (ex is InvalidCastException or JsonException or NotSupportedException)
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

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
