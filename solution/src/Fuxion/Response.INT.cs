using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Fuxion.Text.Json.Serialization;

namespace Fuxion;

[JsonConverter(typeof(InterfaceSerializerConverter<IResponse>))]
public interface IResponse
{
	bool IsSuccess { get; }
	bool IsError { get; }
	string? Message { get; }
	object? ErrorType { get; }
	Exception? Exception { get; }
	IDictionary<string, object?> Extensions { get; }
}

public interface IResponse<out TPayload> : IResponse
{
#if !STANDARD_OR_OLD_FRAMEWORKS
	[MemberNotNullWhen(true, nameof(Payload))]
	new bool IsSuccess => (this as IResponse).IsSuccess;
	[MemberNotNullWhen(false, nameof(Payload))]
	new bool IsError => (this as IResponse).IsError;
#else
	[MemberNotNullWhen(true, nameof(Payload))]
	new bool IsSuccess { get; }
	[MemberNotNullWhen(false, nameof(Payload))]
	new bool IsError { get; }
#endif
	TPayload? Payload { get; }
}