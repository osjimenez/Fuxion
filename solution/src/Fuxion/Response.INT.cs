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
	[JsonIgnore]
	bool IsError { get; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	string? Message { get; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	object? ErrorType { get; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonConverter(typeof(ExceptionConverter))]
	Exception? Exception { get; }
	[JsonExtensionData]
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
	[JsonIgnore]
	new bool IsError { get; }
#endif
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	TPayload? Payload { get; }
}

public interface IErrorContract;

public interface IErrorContractConsole;