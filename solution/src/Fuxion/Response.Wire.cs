using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
	/// <summary>A binary payload (<see cref="IOContent"/>) written as a bare file body. Never enveloped.</summary>
	Binary
}

/// <summary>A business error value that knows which HTTP status it travels with. The service can override it through <see cref="ResponseOptions.BusinessErrorStatus"/>.</summary>
public interface IHttpStatusError
{
	/// <summary>The HTTP status code this business error travels with, unless <see cref="ResponseOptions.BusinessErrorStatus"/> overrides it.</summary>
	HttpStatusCode Status { get; }
}

/// <summary>
/// The framework-agnostic result of mapping a union value: what status, what shape and which value.
/// <see cref="Materialize"/> resolves the content type and serializer options for that shape.
/// </summary>
/// <param name="StatusCode">The HTTP status code to write.</param>
/// <param name="Shape">The body shape, which drives content type, envelope-vs-bare and whether <see cref="Naming"/> may be stamped.</param>
/// <param name="Value">The value to serialize as the body, or <see langword="null"/> for a body-less shape.</param>
/// <param name="ProblemTitle">An override for the RFC 9457 <c>title</c> when <see cref="Shape"/> is <see cref="ResponseWireShape.ProblemError"/>; <see langword="null"/> keeps the status-derived default.</param>
/// <param name="Naming">
/// The requested naming policy to stamp on the wire, or <see langword="null"/> for the server's own policy.
/// Set only for shapes that announce their naming through a vnd media type parameter (<see cref="ResponseWireShape.Unit"/>,
/// <see cref="ResponseWireShape.Envelope"/>, <see cref="ResponseWireShape.NativeError"/>); every other shape
/// (<see cref="ResponseWireShape.Payload"/>, <see cref="ResponseWireShape.RawError"/>, <see cref="ResponseWireShape.ProblemError"/>,
/// <see cref="ResponseWireShape.NoContent"/>, <see cref="ResponseWireShape.Binary"/>) always keeps this
/// <see langword="null"/>, so a client reading only the Content-Type is never misled into materializing a body
/// that was silently transcoded to an un-announced policy.
/// </param>
public sealed record ResponseWireMapping(int StatusCode, ResponseWireShape Shape, object? Value, string? ProblemTitle = null, string? Naming = null)
{
	/// <summary>Serializes as "{}": the body of a Unit response.</summary>
	public static readonly object EmptyObject = new();

	/// <summary>The contract default: Web (camelCase) options, used whenever a shape cannot announce its naming.</summary>
	static readonly JsonSerializerOptions WebDefaults = new(JsonSerializerDefaults.Web);

	public bool HasBody => Shape != ResponseWireShape.NoContent;

	// The keys of the response extensions that this shape has no place for: they only travel inside the envelope. The
	// adapters log a warning when it is not empty; the shape itself never changes because of it.
	public IReadOnlyCollection<string> DroppedExtensions { get; init; } = [];

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
		var effective = Naming is null ? jsonOptions : ResponseNaming.Apply(jsonOptions ?? WebDefaults, Naming);
		var policy = effective?.PropertyNamingPolicy;
		return Shape switch
		{
			ResponseWireShape.NoContent => (null, null, effective),
			ResponseWireShape.Unit => (ResponseNaming.WithNaming(ResponseMediaTypes.UnitJson, policy), EmptyObject, effective),
			ResponseWireShape.Payload => (ResponseMediaTypes.Json, Value, effective),
			ResponseWireShape.RawError => (ResponseMediaTypes.Json, Value, effective),
			ResponseWireShape.Envelope => (ResponseNaming.WithNaming(ResponseMediaTypes.ResponseJson, policy), Value, effective),
			ResponseWireShape.NativeError => (ResponseNaming.WithNaming(ResponseMediaTypes.ErrorJson, policy), Value, effective),
			ResponseWireShape.ProblemError => BuildProblemResult(jsonOptions),
			ResponseWireShape.Binary => (((IOContent)Value!).ContentType, Value, null),
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
		var problem = ((Error)Value!).ToProblemDetails(jsonOptions);
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

	/// <summary>
	/// Whether <paramref name="type"/> is a union shape (or its <see cref="Task{TResult}"/>/<see cref="ValueTask{TResult}"/>
	/// wrapper) that the adapters know how to map. <see langword="null"/> is not supported: some hosts (e.g. Web API 2's
	/// <c>ReflectedHttpActionDescriptor</c>) report a null declared return type for <see langword="void"/> and
	/// non-generic <see cref="Task"/> actions, and those must simply be left untouched rather than crash.
	/// </summary>
	public static bool IsSupportedDeclaredResponseType(Type? type)
		=> type is not null && IsResponseReturnType(UnwrapTaskType(type));

	public static bool TryMap(object? value, ResponseOptions options, out ResponseWireMapping mapping)
	{
		if (!TryMapCore(value, options, out mapping)) return false;
		// The requested naming is stamped only on shapes that announce it through a vnd media type
		// parameter (Unit, Envelope, NativeError). Payload, RawError, ProblemError, NoContent and Binary
		// never carry a naming parameter on the wire, so a client reading Content-Type has no way to know
		// the body was transcoded; stamping it there would make the client silently misread the body.
		if (options.Naming is not null && IsAnnouncedShape(mapping.Shape))
			mapping = mapping with { Naming = options.Naming };
		// The response extensions only have a place inside the envelope (the error's own extensions travel in every error shape).
		if (mapping.Shape != ResponseWireShape.Envelope && value is IResponse { Extensions.Count: > 0 } extended)
			mapping = mapping with { DroppedExtensions = [.. extended.Extensions.Keys] };
		return true;
	}

	// The throwing variant of TryMap, so every adapter reports an unsupported value with the same message.
	public static ResponseWireMapping Map(object? value, ResponseOptions options)
		=> TryMap(value, options, out var mapping)
			? mapping
			: throw new NotSupportedException($"The union response value of type '{((value as IUnion)?.Value ?? value)?.GetType().FullName ?? "null"}' is not supported.");

	static bool IsAnnouncedShape(ResponseWireShape shape)
		=> shape is ResponseWireShape.Unit or ResponseWireShape.Envelope or ResponseWireShape.NativeError;

	static bool TryMapCore(object? value, ResponseOptions options, out ResponseWireMapping mapping)
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

		// default(Response<T>): a programming error, not a business state - but it still follows the error options and Accept.
		mapping = MapError((Response<Unit>)Error.InternalServerError("The Response value is uninitialized (default)."), options);
		return true;
	}

	static ResponseWireMapping MapError(IResponse response, ResponseOptions options)
	{
		// An error response without a supported payload (native Error or business value) is itself a
		// programming error. Normalize it to a critical Error up front so the rest of this method - and
		// therefore the error options and the client's Accept - treat it exactly like any other error.
		if (response.Value is null)
			response = (Response<Unit>)Error.InternalServerError("The response is error but it does not contain a supported error payload.");

		var status = ResolveStatus(response.Value, options);

		if (options.SerializeErrorAsProblemDetails)
		{
			// problem+json wins over the envelope, for native and typed errors alike.
			return response.Value is Error error
				? new(status, ResponseWireShape.ProblemError, error)
				: new(status, ResponseWireShape.ProblemError, new Error { Type = (HttpStatusCode)status, Payload = response.Value }, BusinessErrorTitle);
		}

		if (options.SerializeFullResponses)
			return new(status, ResponseWireShape.Envelope, response);

		return response.Value is Error nativeError
			? new(status, ResponseWireShape.NativeError, nativeError)
			: new(status, ResponseWireShape.RawError, response.Value);
	}

	// The service has the last word, then the error itself, then the contract default.
	static int ResolveStatus(object? value, ResponseOptions options) => value switch
	{
		Error error => error.Type is HttpStatusCode code ? (int)code : 500,
		not null when options.BusinessErrorStatus?.Invoke(value) is { } code => (int)code,
		IHttpStatusError declared => (int)declared.Status,
		_ => 500
	};

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

	static bool TryWrapBinary(object? value, out IOContent file)
	{
		switch (value)
		{
			case IOContent content: file = content; return true;
			case System.IO.Stream stream: file = new IOContent(stream); return true;
			case byte[] bytes: file = IOContent.FromBytes(bytes); return true;
			default: file = null!; return false;
		}
	}
}
