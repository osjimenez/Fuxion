using Fuxion.Reflection;
using Fuxion.Text.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.Union.Net.Http;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Reads an <see cref="Error"/> from a RFC 9457 ProblemDetails payload, delegating the
/// conversion logic to <see cref="ErrorProblemDetailsConverter"/> so it is never duplicated.
/// </summary>
public sealed class ErrorFromProblemDetailsJsonConverter : JsonConverter<Error>
{
	public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var problem = JsonSerializer.Deserialize<ResponseProblemDetails>(ref reader, options);
		return ErrorProblemDetailsConverter.ToError(problem, options);
	}

	public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize(writer, ErrorProblemDetailsConverter.ToProblemDetails(value, options), options);
}

public static class ResponseExtensions
{
	// Web defaults are used because the response envelope property names are resolved through the
	// naming policy at read time, and any HTTP server serializing them (ASP.NET Core included) uses
	// JsonSerializerDefaults.Web. A raw JsonSerializerOptions would look for PascalCase names and
	// would never match a camelCase body. Callers passing their own options keep full control.
	static readonly JsonSerializerOptions defaultJsonOptions = new(JsonSerializerDefaults.Web);
	static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> problemDetailsJsonOptionsCache = new();

	static JsonSerializerOptions GetJsonOptions(JsonSerializerOptions? jsonOptions, ResponseSerializerOptions options)
	{
		var source = jsonOptions ?? defaultJsonOptions;
		if (!options.SerializeErrorAsProblemDetails)
			return source;
		return problemDetailsJsonOptionsCache.GetValue(source, static key =>
		{
			var clone = new JsonSerializerOptions(key);
			clone.Converters.Insert(0, new ErrorFromProblemDetailsJsonConverter());
			return clone;
		});
	}

	static InvalidOperationException CreateDeserializationError(string? message, Exception? innerException = null)
		=> new(message ?? "The HTTP response body could not be deserialized.", innerException);

	// A ProblemDetails body is identified by its media type, so it is parsed correctly even when the
	// caller options did not enable that shape. The response kind header is deliberately not used
	// here: it states that the response is an error, not the format that error was serialized in.
	static bool IsProblemDetailsBody(HttpResponseMessage message, ResponseSerializerOptions options)
		=> options.SerializeErrorAsProblemDetails
			|| string.Equals(message.Content.Headers.ContentType?.MediaType, ResponseMediaTypes.ProblemJson, StringComparison.OrdinalIgnoreCase);

	// The full envelope is identified by its media type, mirroring how a ProblemDetails body is
	// detected, so a server that emits the envelope is read correctly even when the caller did not
	// enable that shape. The detection is purely additive: the option still forces the envelope for
	// servers that do not advertise the media type, and it is never turned off by this check.
	static bool IsFullResponseBody(HttpResponseMessage message, ResponseSerializerOptions options)
		=> options.SerializeFullResponses
			|| string.Equals(message.Content.Headers.ContentType?.MediaType, ResponseMediaTypes.ResponseJson, StringComparison.OrdinalIgnoreCase);

	// The token is honoured where the target framework exposes an overload that accepts it. On the
	// older frameworks the read cannot be cancelled, so cancellation is observed before starting it
	// instead of being silently ignored.
	static async Task<string> ReadBodyAsync(HttpResponseMessage me, CancellationToken ct)
	{
#if NET5_0_OR_GREATER
		return await me.Content.ReadAsStringAsync(ct);
#else
		ct.ThrowIfCancellationRequested();
		return await me.Content.ReadAsStringAsync();
#endif
	}

	static Error ReadErrorFromBody(string body, JsonSerializerOptions jsonOptions, bool asProblemDetails)
	{
		if (string.IsNullOrWhiteSpace(body))
			return Error.Critical("The error response body is empty.");

		if (asProblemDetails)
		{
			var deserialization = body.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions);
			return deserialization.IsError
				? Error.Critical(deserialization.Message, exception: deserialization.Exception)
				: ErrorProblemDetailsConverter.ToError(deserialization.Payload, jsonOptions);
		}

		var errorDeserialization = body.Fx.Json.Deserialize<Error>(options: jsonOptions);
		return errorDeserialization.IsError
			? Error.Critical(errorDeserialization.Message, exception: errorDeserialization.Exception)
			: errorDeserialization.Payload;
	}

	// A typed business error carried inside a problem+json body travels in the same errorPayload
	// extension member that Error.Payload uses, so it is recovered through the same Error machinery.
	static TError ReadTypedErrorFromProblemBody<TError>(string body, JsonSerializerOptions jsonOptions)
		where TError : notnull
	{
		var error = ReadErrorFromBody(body, jsonOptions, true);
		if (error.TryGetPayloadAs<TError>(out var typed, jsonOptions))
			return typed;

		throw CreateDeserializationError(
			$"The problem details body does not carry an '{ErrorProblemDetailsConverter.ErrorPayloadExtensionName}' extension member deserializable as '{typeof(TError).GetSignature()}'.");
	}

	static bool TryReadSuccessPayload<TSuccess>(string body, JsonSerializerOptions jsonOptions, HttpStatusCode statusCode, out TSuccess payload, out Error error)
		where TSuccess : notnull
	{
		payload = default!;
		error = default;

		if (string.IsNullOrWhiteSpace(body))
		{
			// Unit is the only success shape that legitimately travels without a body.
			if (typeof(TSuccess) == typeof(Unit))
				return TryGetUnitPayload(out payload);

			error = Error.Critical($"The response status code is '{(int)statusCode}' and the body is empty.");
			return false;
		}

		var deserialization = body.Fx.Json.Deserialize<TSuccess>(options: jsonOptions);
		if (deserialization.IsSuccess)
		{
			payload = deserialization.Payload;
			return true;
		}

		error = Error.Critical(deserialization.Message, exception: deserialization.Exception);
		return false;
	}

	// Semantic shape of a body-less success. Resolved in a single place so that no caller
	// depends on the order in which the Unit and None checks are evaluated.
	enum EmptyResponseKind
	{
		/// <summary>The response does not represent a body-less success.</summary>
		Unknown,
		Unit,
		None
	}

	// The header is the authoritative discriminator, because an intermediary may normalize a
	// body-less 200 into a 204. The status code is only consulted when the header is absent,
	// which keeps compatibility with servers that do not emit it yet.
	static EmptyResponseKind ResolveEmptyResponseKind(HttpResponseMessage message)
	{
		if (message.Headers.TryGetValues(ResponseHeaders.ResponseKind, out var values))
			foreach (var value in values)
			{
				if (string.Equals(value, ResponseHeaders.NoneKind, StringComparison.OrdinalIgnoreCase))
					return EmptyResponseKind.None;
				if (string.Equals(value, ResponseHeaders.UnitKind, StringComparison.OrdinalIgnoreCase))
					return EmptyResponseKind.Unit;
			}

		// Fallback for servers that do not emit the header: only the status code is available,
		// so a 204 is read as None and any other body-less success as Unit. Whether the expected
		// success type can actually hold a Unit is decided by the caller.
		if (message.StatusCode == HttpStatusCode.NoContent)
			return EmptyResponseKind.None;

		return message.IsSuccessStatusCode
			? EmptyResponseKind.Unit
			: EmptyResponseKind.Unknown;
	}

	static bool TryGetUnitPayload<TSuccess>(out TSuccess payload)
		where TSuccess : notnull
	{
		if (typeof(TSuccess) == typeof(Unit))
		{
			payload = (TSuccess)(object)Unit.Value;
			return true;
		}

		payload = default!;
		return false;
	}

	extension(HttpResponseMessage me)
	{
		/// <summary>
		/// Reads the HTTP response as a <see cref="ResponseMaybe{TSuccess}"/>.
		/// </summary>
		/// <remarks>
		/// This overload never throws for a protocol level failure: an empty or undeserializable body is
		/// returned as an <see cref="Error"/> inside the response, because the error shape is known.
		/// The overload that also takes a custom error type cannot do this and throws instead.
		/// </remarks>
		public async Task<ResponseMaybe<TSuccess>> AsResponseAsync<TSuccess>(ResponseSerializerOptions? options = null, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
		{
			options ??= new();
			var currentJsonOptions = GetJsonOptions(jsonOptions, options);

			if (IsFullResponseBody(me, options))
			{
				// Errors are still emitted as ProblemDetails when that shape is in use,
				// so the full response envelope is only present for successful status codes.
				if (!me.IsSuccessStatusCode && IsProblemDetailsBody(me, options))
					return ReadErrorFromBody(await ReadBodyAsync(me, ct), currentJsonOptions, true);

				var fullBody = await ReadBodyAsync(me, ct);
				if (string.IsNullOrWhiteSpace(fullBody))
					return ResolveEmptyResponseKind(me) switch
					{
						EmptyResponseKind.None => None.Value,
						EmptyResponseKind.Unit when TryGetUnitPayload<TSuccess>(out var fullUnit) => fullUnit,
						_ => Error.Critical($"The response status code is '{(int)me.StatusCode}' and the body is empty.")
					};

				var deserialization = fullBody.Fx.Json.Deserialize<ResponseMaybe<TSuccess>>(options: currentJsonOptions);
				return deserialization.IsError
					? Error.Critical(deserialization.Message, exception: deserialization.Exception)
					: deserialization.Payload;
			}

			if (me.IsSuccessStatusCode && ResolveEmptyResponseKind(me) == EmptyResponseKind.None)
				return None.Value;

			var body = await ReadBodyAsync(me, ct);
			if (!me.IsSuccessStatusCode)
				return ReadErrorFromBody(body, currentJsonOptions, IsProblemDetailsBody(me, options));

			return TryReadSuccessPayload<TSuccess>(body, currentJsonOptions, me.StatusCode, out var payload, out var error)
				? payload
				: error;
		}

		/// <summary>
		/// Reads the HTTP response as a <see cref="ResponseMaybe{TSuccess, TError}"/>.
		/// </summary>
		/// <remarks>
		/// Unlike the overload that only takes a success type, this one throws an
		/// <see cref="InvalidOperationException"/> when the body is empty or cannot be deserialized.
		/// A value of <typeparamref name="TError"/> cannot be synthesized for an arbitrary type, so a
		/// protocol level failure has no representation inside the returned response.
		/// </remarks>
		/// <exception cref="InvalidOperationException">The response body is empty or cannot be deserialized.</exception>
		public async Task<ResponseMaybe<TSuccess, TError>> AsResponseAsync<TSuccess, TError>(ResponseSerializerOptions? options = null, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			where TError : notnull
		{
			options ??= new();
			var currentJsonOptions = GetJsonOptions(jsonOptions, options);

			if (IsFullResponseBody(me, options))
			{
				// Errors are still emitted as ProblemDetails when that shape is in use,
				// so the full response envelope is only present for successful status codes.
				if (!me.IsSuccessStatusCode && IsProblemDetailsBody(me, options))
					return typeof(TError) == typeof(Error)
						? (TError)(object)ReadErrorFromBody(await ReadBodyAsync(me, ct), currentJsonOptions, true)
						: ReadTypedErrorFromProblemBody<TError>(await ReadBodyAsync(me, ct), currentJsonOptions);

				var fullBody = await ReadBodyAsync(me, ct);
				if (string.IsNullOrWhiteSpace(fullBody))
					return ResolveEmptyResponseKind(me) switch
					{
						EmptyResponseKind.None => None.Value,
						EmptyResponseKind.Unit when TryGetUnitPayload<TSuccess>(out var fullUnit) => fullUnit,
						_ => throw CreateDeserializationError($"The response status code is '{(int)me.StatusCode}' and the body is empty.")
					};

				var deserialization = fullBody.Fx.Json.Deserialize<ResponseMaybe<TSuccess, TError>>(options: currentJsonOptions);
				return deserialization.IsError
					? throw CreateDeserializationError(deserialization.Message, deserialization.Exception)
					: deserialization.Payload;
			}

			if (me.IsSuccessStatusCode && ResolveEmptyResponseKind(me) == EmptyResponseKind.None)
				return None.Value;

			var body = await ReadBodyAsync(me, ct);
			if (!me.IsSuccessStatusCode)
			{
				if (typeof(TError) == typeof(Error))
					return (TError)(object)ReadErrorFromBody(body, currentJsonOptions, IsProblemDetailsBody(me, options));

				if (string.IsNullOrWhiteSpace(body))
					throw CreateDeserializationError($"The response status code is '{(int)me.StatusCode}' and the body is empty.");

				// A problem+json body carries the typed error in its errorPayload extension member;
				// a plain body is the typed error itself.
				if (IsProblemDetailsBody(me, options))
					return ReadTypedErrorFromProblemBody<TError>(body, currentJsonOptions);

				var errorDeserialization = body.Fx.Json.Deserialize<TError>(options: currentJsonOptions);
				return errorDeserialization.IsSuccess
					? errorDeserialization.Payload
					: throw CreateDeserializationError(
						errorDeserialization.Message ?? $"The error body could not be deserialized as '{typeof(TError).GetSignature()}'.",
						errorDeserialization.Exception);
			}

			if (TryReadSuccessPayload<TSuccess>(body, currentJsonOptions, me.StatusCode, out var payload, out var error))
				return payload;

			throw CreateDeserializationError(error.Message);
		}
	}

	extension(Task<HttpResponseMessage> me)
	{
		public async Task<ResponseMaybe<TSuccess>> AsResponseAsync<TSuccess>(ResponseSerializerOptions? options = null, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			=> await (await me).AsResponseAsync<TSuccess>(options, jsonOptions, ct);

		public async Task<ResponseMaybe<TSuccess, TError>> AsResponseAsync<TSuccess, TError>(ResponseSerializerOptions? options = null, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			where TError : notnull
			=> await (await me).AsResponseAsync<TSuccess, TError>(options, jsonOptions, ct);
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
