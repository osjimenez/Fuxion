namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>The body shape of a mapped response. The adapter turns it into a concrete HTTP response.</summary>
public enum ResponseWireShape
{
	/// <summary>No body at all (204).</summary>
	NoContent,
	/// <summary>A Unit result: 200 with an empty object body and its own media type.</summary>
	Unit,
	/// <summary>The bare success value as application/json.</summary>
	Payload,
	/// <summary>The full response envelope.</summary>
	Envelope,
	/// <summary>An <see cref="Error"/> written as RFC 9457 ProblemDetails.</summary>
	ProblemError,
	/// <summary>An <see cref="Error"/> written in its native shape.</summary>
	NativeError,
	/// <summary>A typed business error written as its bare value (application/json).</summary>
	RawError,
	/// <summary>A binary payload (<see cref="FileContent"/>) written as a bare file body. Never enveloped.</summary>
	Binary
}

/// <summary>
/// The framework-agnostic result of mapping a union value: what status, what shape and which value.
/// <see cref="Materialize"/> resolves the content type and serializer options for that shape.
/// </summary>
public sealed record ResponseWireMapping(int StatusCode, ResponseWireShape Shape, object? Value, string? ProblemTitle = null)
{
	/// <summary>Serializes as "{}": the body of a Unit response.</summary>
	public static readonly object EmptyObject = new();

	/// <summary>The contract default: Web (camelCase) options, used whenever a shape cannot announce its naming.</summary>
	static readonly JsonSerializerOptions WebDefaults = new(JsonSerializerDefaults.Web);

	public bool HasBody => Shape != ResponseWireShape.NoContent;

	/// <summary>
	/// Resolves the final content type (with the naming parameter for Fuxion types), the object to
	/// serialize and the options to serialize it with, given the JSON options the adapter would
	/// otherwise serialize with.
	/// </summary>
	/// <remarks>
	/// <see cref="ResponseWireShape.ProblemError"/> is always written in camel: <c>application/problem+json</c>
	/// is a standard media type and carries no <c>naming</c> parameter, so a client reading with the
	/// contract default (camel) must be able to find the extension keys regardless of the server's
	/// configured naming policy.
	/// </remarks>
	public (string? ContentType, object? Body, JsonSerializerOptions? SerializerOptions) Materialize(JsonSerializerOptions? jsonOptions)
	{
		var policy = jsonOptions?.PropertyNamingPolicy;
		return Shape switch
		{
			ResponseWireShape.NoContent => (null, null, jsonOptions),
			ResponseWireShape.Unit => (ResponseNaming.WithNaming(ResponseMediaTypes.UnitJson, policy), EmptyObject, jsonOptions),
			ResponseWireShape.Payload => (ResponseMediaTypes.Json, Value, jsonOptions),
			ResponseWireShape.RawError => (ResponseMediaTypes.Json, Value, jsonOptions),
			ResponseWireShape.Envelope => (ResponseNaming.WithNaming(ResponseMediaTypes.ResponseJson, policy), Value, jsonOptions),
			ResponseWireShape.NativeError => (ResponseNaming.WithNaming(ResponseMediaTypes.ErrorJson, policy), Value, jsonOptions),
			ResponseWireShape.ProblemError => BuildProblemResult(jsonOptions),
			ResponseWireShape.Binary => (((FileContent)Value!).ContentType, Value, null),
			_ => throw new NotSupportedException($"Unknown wire shape '{Shape}'.")
		};
	}

	(string? ContentType, object? Body, JsonSerializerOptions? SerializerOptions) BuildProblemResult(JsonSerializerOptions? jsonOptions)
	{
		var camelOptions = ResponseNaming.Apply(jsonOptions ?? WebDefaults, ResponseNaming.Camel);
		return (ResponseMediaTypes.ProblemJson, BuildProblem(camelOptions), camelOptions);
	}

	ResponseProblemDetails BuildProblem(JsonSerializerOptions? jsonOptions)
	{
		var problem = ErrorProblemDetailsConverter.ToProblemDetails((Error)Value!, jsonOptions);
		if (ProblemTitle is not null)
			problem.Title = ProblemTitle;
		return problem;
	}
}

/// <summary>
/// Decides how a union value travels, from the value and the effective options alone. It contains no
/// HTTP framework type, so every adapter (ASP.NET Core, ASP.NET) shares one single decision.
/// </summary>
public static class ResponseWireMapper
{
	public const string BusinessErrorTitle = "Business error";

	public static bool IsSupportedDeclaredResponseType(Type type)
		=> IsResponseReturnType(UnwrapTaskType(type));

	public static bool TryMap(object? value, ResponseOptions options, out ResponseWireMapping mapping)
	{
		// Bare union values are mapped exactly like the wrapper they imply.
		if (value is Error bareError) value = (Response<Unit>)bareError;
		if (value is None) value = (ResponseMaybe<Unit>)None.Value;

		if (value is not IResponse response)
		{
			mapping = null!;
			return false;
		}

		if (response.IsError)
		{
			mapping = MapError(response, options);
			return true;
		}

		// Binary payloads never travel inside the envelope: the HTTP message itself is the envelope
		// (media type, Content-Disposition, ETag...). Stream and byte[] are the minimum viable forms.
		if (TryWrapBinary(response.Value, out var file))
		{
			mapping = new(200, ResponseWireShape.Binary, file);
			return true;
		}

		if (response.Value is Unit)
		{
			mapping = options.SerializeFullResponses
				? new(200, ResponseWireShape.Envelope, response)
				: new(200, ResponseWireShape.Unit, null);
			return true;
		}

		if (response.Value is None)
		{
			mapping = !options.StrictNone && options.SerializeFullResponses
				? new(200, ResponseWireShape.Envelope, response)
				: new(204, ResponseWireShape.NoContent, null);
			return true;
		}

		if (response.Value is not null)
		{
			mapping = options.SerializeFullResponses
				? new(200, ResponseWireShape.Envelope, response)
				: new(200, ResponseWireShape.Payload, response.Value);
			return true;
		}

		// default(Response<T>): a programming error, not a business state.
		mapping = new(500, ResponseWireShape.ProblemError, Error.Critical("The Response value is uninitialized (default)."));
		return true;
	}

	static ResponseWireMapping MapError(IResponse response, ResponseOptions options)
	{
		var status = response.Value is Error { Type: HttpStatusCode code } ? (int)code : 500;

		if (options.SerializeErrorAsProblemDetails)
		{
			// problem+json wins over the envelope, for native and typed errors alike.
			if (response.Value is Error error)
				return new(status, ResponseWireShape.ProblemError, error);
			if (response.Value is not null)
				return new(500, ResponseWireShape.ProblemError, new Error { Payload = response.Value }, BusinessErrorTitle);
			return new(500, ResponseWireShape.ProblemError, Error.Critical("The response is error but it does not contain a supported error payload."));
		}

		if (options.SerializeFullResponses)
			return new(status, ResponseWireShape.Envelope, response);

		return response.Value is Error nativeError
			? new(status, ResponseWireShape.NativeError, nativeError)
			: new(status, ResponseWireShape.RawError, response.Value);
	}

	static Type UnwrapTaskType(Type type)
	{
		if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
			return type.GetGenericArguments()[0];
		return type;
	}

	static bool IsResponseReturnType(Type type)
	{
		if (type == typeof(Error) || type == typeof(None)) return true;
		if (!type.IsGenericType) return false;
		var definition = type.GetGenericTypeDefinition();
		return definition == typeof(Response<>) || definition == typeof(Response<,>)
			|| definition == typeof(ResponseMaybe<>) || definition == typeof(ResponseMaybe<,>);
	}

	static bool TryWrapBinary(object? value, out FileContent file)
	{
		switch (value)
		{
			case FileContent content: file = content; return true;
			case System.IO.Stream stream: file = new FileContent(stream); return true;
			case byte[] bytes: file = FileContent.FromBytes(bytes); return true;
			default: file = null!; return false;
		}
	}
}
