using System;
using System.Collections.Generic;
using Fuxion.Text.Json.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Fuxion;

[DebuggerDisplay("{IsSuccess} - {Message}")]
public class Response(bool isSuccess, string? message = null, object? type = null, Exception? exception = null) : IResponse
{
	public bool IsSuccess { get; protected init; } = isSuccess;
	[JsonIgnore]
	public bool IsError => !IsSuccess;
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; init; } = message;
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public object? ErrorType { get; init; } = type;
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonConverter(typeof(ExceptionConverter))]
	public Exception? Exception { get; init; } = exception;
	[JsonExtensionData]
	public IDictionary<string, object?> Extensions { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

	// Conversores implícitos con bool
	public static implicit operator bool(Response response) => response.IsSuccess;
	//public static implicit operator Response(bool isSuccess) => new(isSuccess);
//#if STANDARD_OR_OLD_FRAMEWORKS
	public static ResponseExtensions.ResponseGetExtensions Get { get; } = new();
//#endif
}

[DebuggerDisplay("{IsSuccess} - {Message}")]
public class Response<TPayload>(bool isSuccess, TPayload payload, string? message = null, object? type = null, Exception? exception = null)
	: Response(isSuccess, message, type, exception), IResponse<TPayload>
{
	[MemberNotNullWhen(true, nameof(Payload))]
	public new bool IsSuccess
	{
		get => base.IsSuccess;
		protected init => base.IsSuccess = value;
	}
	[MemberNotNullWhen(false, nameof(Payload))]
	[JsonIgnore]
	public new bool IsError => base.IsError;
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public TPayload? Payload
	{
		get;
		init
			=> field = value?.GetType()
				.IsSubclassOfRawGeneric(typeof(Response<>)) ?? false
				? throw new ArgumentException($"Payload is '{value.GetType().GetSignature()}' type, but can't be derived from '{typeof(Response<>).GetSignature()}' to avoid nested responses.",
					nameof(Payload))
				: value;
	} = payload;
	public static implicit operator TPayload?(Response<TPayload> response) => response.Payload;
	public static implicit operator Response<TPayload>(TPayload payload) => new(true, payload);
}