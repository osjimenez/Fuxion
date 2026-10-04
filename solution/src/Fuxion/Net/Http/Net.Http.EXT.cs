using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Reflection;
using Fuxion.Text.Json;

namespace Fuxion.Net.Http;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Names of the extension members a client-side reading failure carries so that a body which could
/// not be understood is not lost: the raw text, the parsed-but-unfit <see cref="JsonElement"/>, and
/// the announced content type/length.
/// </summary>
public static class ClientErrorKeys
{
	/// <summary>The raw response text, present when the body was read but was not valid JSON.</summary>
	public const string TextPayload = "textPayload";

	/// <summary>The parsed <see cref="JsonElement"/>, present when the body was valid JSON but did not fit the requested type.</summary>
	public const string JsonPayload = "jsonPayload";

	/// <summary>The announced Content-Type media type.</summary>
	public const string ContentType = "contentType";

	/// <summary>The announced Content-Length, when the body was refused before being read.</summary>
	public const string ContentLength = "contentLength";
}

/// <summary>
/// Reads union responses out of HTTP messages. The client trusts only what the message says about
/// itself: the media type says the body shape, the naming parameter says how it was written and the
/// status code says whether it went well. There are no format flags: a Fuxion server describes every
/// response and a non-Fuxion server is read as a vanilla API.
/// </summary>
public static class HttpResponseMessageExtensions
{
	// Web defaults: any HTTP server serializing the envelope (ASP.NET Core included) uses them, and the
	// envelope property names are resolved through the naming policy at read time.
	static readonly JsonSerializerOptions defaultJsonOptions = new(JsonSerializerDefaults.Web);

	static string? ContentType(HttpResponseMessage me) => me.Content?.Headers.ContentType?.ToString();

	// The body announces the naming policy it was written with; the caller options are cloned (cached) with it.
	static JsonSerializerOptions EffectiveJsonOptions(HttpResponseMessage me, JsonSerializerOptions? jsonOptions)
		=> ResponseNaming.Apply(jsonOptions ?? defaultJsonOptions, ResponseNaming.GetParameter(ContentType(me)));

	static InvalidOperationException CreateDeserializationError(string? message, Exception? innerException = null)
		=> new(message ?? "The HTTP response body could not be deserialized.", innerException);

	// Cancellation is always observed before starting the read, regardless of target framework: the
	// BCL overload is used where available, but an already-cancelled token must not depend on whether
	// that overload happens to check it for content that is already buffered.
	static async Task<string> ReadBodyAsync(HttpResponseMessage me, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (me.Content is null) return string.Empty;
#if NET5_0_OR_GREATER
		return await me.Content.ReadAsStringAsync(ct);
#else
		ct.ThrowIfCancellationRequested();
		return await me.Content.ReadAsStringAsync();
#endif
	}

	// Try, and keep what could not be understood: the raw text when it is not JSON, the JsonElement when it
	// is JSON but does not fit the type. Mirrors the pre-union parser (StringContent / JsonContent / JsonError).
	static bool TryDeserialize<T>(string body, JsonSerializerOptions jsonOptions, string? contentType, out T value, out Error failure) where T : notnull
	{
		JsonElement element;
		try
		{
			using var document = JsonDocument.Parse(body);
			element = document.RootElement.Clone();
		}
		catch (JsonException ex)
		{
			value = default!;
			failure = Error.InternalServerError("The response body is not JSON.", exception: ex);
			failure.Extensions[ClientErrorKeys.TextPayload] = body;
			failure.Extensions[ClientErrorKeys.ContentType] = MediaTypeOf(contentType);
			return false;
		}

		// Error cannot be the success type of a Response<T>, so a bare native error is read through its own path.
		Error deserializationFailure;
		if (typeof(T) == typeof(Error))
		{
			if (body.Fx.Json.TryDeserializeError(out var nativeError, out deserializationFailure, options: jsonOptions))
			{
				value = (T)(object)nativeError;
				failure = default;
				return true;
			}
		}
		else
		{
			var desRes = body.Fx.Json.Deserialize<T>(options: jsonOptions);
			if (desRes.TryGetValue(out T? desValue))
			{
				value = desValue;
				failure = default;
				return true;
			}
			desRes.TryGetValue(out deserializationFailure);
		}

		var hint = string.Equals(ResponseNaming.GetParameter(contentType), ResponseNaming.Custom, StringComparison.OrdinalIgnoreCase)
			? " The server announced a custom naming policy; pass JsonSerializerOptions with the same policy."
			: string.Empty;
		value = default!;
		failure = Error.InternalServerError($"The JSON body could not be read as '{typeof(T).GetSignature()}'.{hint}", exception: deserializationFailure.Exception);
		failure.Extensions[ClientErrorKeys.JsonPayload] = element;
		failure.Extensions[ClientErrorKeys.ContentType] = MediaTypeOf(contentType);
		return false;
	}

	static string? MediaTypeOf(string? contentType) => ResponseMediaTypes.TryParse(contentType, out var parsed) ? parsed.MediaType : contentType;

	// RFC 9457. Some servers omit "status": the HTTP status is the authoritative fallback.
	static Error ReadProblem(string body, JsonSerializerOptions jsonOptions, HttpStatusCode status, string? contentType)
	{
		if (!TryDeserialize<ResponseProblemDetails>(body, jsonOptions, contentType, out var problem, out var failure))
			return failure;

		var error = problem.ToError(jsonOptions);
		return error.Type is null ? error with { Type = status } : error;
	}

	// A typed business error inside problem+json travels in the same errorPayload extension member
	// that Error.Payload uses, so it is recovered through the same Error machinery.
	static TError ReadTypedError<TError>(Error problemError, JsonSerializerOptions jsonOptions)
		where TError : notnull
	{
		if (problemError.TryGetPayloadAs<TError>(out var typed, jsonOptions))
			return typed;

		throw CreateDeserializationError(
			$"The problem details body does not carry an '{ProblemDetailsExtensionKeys.ErrorPayload}' extension member deserializable as '{typeof(TError).GetSignature()}'.");
	}

	// An error body this client does not recognize (a non-Fuxion server, HTML, plain text...). Nothing is
	// lost: the HTTP status becomes the error type and the raw body stays reachable as the payload.
	static Error ReadForeignError(HttpResponseMessage me, string body)
	{
		object? payload = null;
		if (!string.IsNullOrWhiteSpace(body))
		{
			try
			{
				using var document = JsonDocument.Parse(body);
				payload = document.RootElement.Clone();
			}
			catch (JsonException)
			{
				payload = body;
			}
		}

		return new Error
		{
			Type = me.StatusCode,
			Message = string.IsNullOrWhiteSpace(me.ReasonPhrase) ? $"The response status code is '{(int)me.StatusCode}'." : me.ReasonPhrase,
			Payload = payload
		};
	}

	// JSON-ish media types. An absent Content-Type is treated as JSON so servers that announce nothing
	// (legacy) keep working; anything else that is not JSON is a binary body.
	static bool IsJsonContentType(string? contentType)
		=> !ResponseMediaTypes.TryParse(contentType, out _) || ResponseMediaTypes.IsJson(contentType);

	// A text media type is worth trying: the body is read and parsed as JSON even though the server
	// did not announce it as such (a legacy or misconfigured server serializing JSON as text/plain).
	static bool IsTextContentType(string? contentType)
		=> ResponseMediaTypes.TryParse(contentType, out var parsed) && (parsed.MediaType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ?? false);

	// Neither JSON nor text: refused without reading, so a large binary body never gets buffered into a string.
	static Error UnreadableBinary(HttpResponseMessage me, Type target)
	{
		var error = Error.InternalServerError($"The response body is '{me.Content?.Headers.ContentType?.MediaType}', not JSON nor text, and '{target.GetSignature()}' cannot be read from it.");
		error.Extensions[ClientErrorKeys.ContentType] = me.Content?.Headers.ContentType?.MediaType;
		error.Extensions[ClientErrorKeys.ContentLength] = me.Content?.Headers.ContentLength;
		return error;
	}

	static async Task<Stream> ReadStreamAsync(HttpResponseMessage me, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
#if NET5_0_OR_GREATER
		return await me.Content.ReadAsStreamAsync(ct);
#else
		ct.ThrowIfCancellationRequested();
		return await me.Content.ReadAsStreamAsync();
#endif
	}

	// A binary success: the body is handed over as a stream (never buffered here), described by the
	// standard headers. The returned stream owns the message, so disposing it releases the connection.
	// Not every requested type can be produced from an HTTP body (an arbitrary Stream subtype cannot be
	// synthesized), so the outcome is returned rather than thrown: each caller overload decides how a
	// protocol level failure is reported (Error for the one-generic overload, an exception for the other).
	static async Task<(bool Success, TSuccess Value, string? FailureMessage)> TryReadBinaryAsync<TSuccess>(HttpResponseMessage me, CancellationToken ct)
		where TSuccess : notnull
	{
		ct.ThrowIfCancellationRequested();
		if (typeof(TSuccess) == typeof(byte[]))
		{
#if NET5_0_OR_GREATER
			var bytes = await me.Content.ReadAsByteArrayAsync(ct);
#else
			ct.ThrowIfCancellationRequested();
			var bytes = await me.Content.ReadAsByteArrayAsync();
#endif
			return (true, (TSuccess)(object)bytes, null);
		}

		if (typeof(TSuccess) == typeof(MemoryStream))
		{
			var source = await ReadStreamAsync(me, ct);
			var memory = new MemoryStream();
#if NET5_0_OR_GREATER
			await source.CopyToAsync(memory, ct);
#else
			ct.ThrowIfCancellationRequested();
			await source.CopyToAsync(memory);
#endif
			memory.Position = 0;
			me.Dispose();
			return (true, (TSuccess)(object)memory, null);
		}

		// Stream itself and FileContent (backed by the response stream) are supported below; any other
		// concrete Stream subtype (e.g. a custom Stream, or FileStream) cannot be conjured out of thin air.
		if (typeof(TSuccess) != typeof(Stream) && typeof(Stream).IsAssignableFrom(typeof(TSuccess)))
			return (false, default!, $"'{typeof(TSuccess).GetSignature()}' cannot be produced from an HTTP body; request Stream, MemoryStream, byte[] or FileContent instead.");

		var stream = new HttpResponseStream(await ReadStreamAsync(me, ct), me);
		if (typeof(TSuccess) == typeof(FileContent))
		{
			var headers = me.Content.Headers;
			var disposition = headers.ContentDisposition;
			var file = new FileContent(stream, headers.ContentType?.MediaType, (disposition?.FileNameStar ?? disposition?.FileName)?.Trim('"'))
			{
				Length = headers.ContentLength,
				LastModified = headers.LastModified,
				ETag = me.Headers.ETag?.ToString(),
				EnableRangeProcessing = me.Headers.AcceptRanges.Any(r => string.Equals(r, "bytes", StringComparison.OrdinalIgnoreCase))
			};
			return (true, (TSuccess)(object)file, null);
		}

		return (true, (TSuccess)(object)stream, null);
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

	// What both AsResponseAsync overloads read the same way: everything except the steps that depend on the error
	// type (envelope type, problem details, native errors and foreign error bodies). Each overload then decides
	// whether a failure is returned as an Error or thrown.
	enum ReadStep { NoContent, Value, Failure, Body }

	readonly struct ClientRead<TSuccess>
		where TSuccess : notnull
	{
		ClientRead(ReadStep step, TSuccess value, Error failure, string body)
		{
			Step = step;
			Value = value;
			Failure = failure;
			Body = body;
		}
		public ReadStep Step { get; }
		public TSuccess Value { get; }
		public Error Failure { get; }
		public string Body { get; }
		public static ClientRead<TSuccess> NoContent => new(ReadStep.NoContent, default!, default, string.Empty);
		public static ClientRead<TSuccess> Of(TSuccess value) => new(ReadStep.Value, value, default, string.Empty);
		public static ClientRead<TSuccess> Fail(Error failure) => new(ReadStep.Failure, default!, failure, string.Empty);
		public static ClientRead<TSuccess> WithBody(string body) => new(ReadStep.Body, default!, default, body);
	}

	// 204, Unit and binary payloads are resolved before the body is touched; otherwise the body is read as text.
	static async Task<ClientRead<TSuccess>> ReadHeadAsync<TSuccess>(HttpResponseMessage me, string? contentType, CancellationToken ct)
		where TSuccess : notnull
	{
		if (me.StatusCode == HttpStatusCode.NoContent)
			return ClientRead<TSuccess>.NoContent;

		if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.UnitJson))
			return TryGetUnitPayload<TSuccess>(out var unit)
				? ClientRead<TSuccess>.Of(unit)
				: ClientRead<TSuccess>.Fail(Error.InternalServerError($"The response is a Unit but '{typeof(TSuccess).GetSignature()}' was expected."));

		// Decide by type and media type BEFORE touching the body: binary payloads are never read as text.
		if (me.IsSuccessStatusCode && !ResponseMediaTypes.IsFuxion(contentType))
		{
			if (BinaryPayload.IsBinaryType(typeof(TSuccess)))
			{
				var (binarySuccess, binaryPayload, binaryFailureMessage) = await TryReadBinaryAsync<TSuccess>(me, ct);
				return binarySuccess ? ClientRead<TSuccess>.Of(binaryPayload) : ClientRead<TSuccess>.Fail(Error.InternalServerError(binaryFailureMessage));
			}
			// Unit consumes no content: a legacy server may answer 200 with an empty text/plain body.
			if (typeof(TSuccess) != typeof(Unit) && !IsJsonContentType(contentType) && !IsTextContentType(contentType))
				return ClientRead<TSuccess>.Fail(UnreadableBinary(me, typeof(TSuccess)));
		}

		return ClientRead<TSuccess>.WithBody(await ReadBodyAsync(me, ct));
	}

	// The last step of a successful response: an empty body is only valid for Unit, anything else is the payload.
	static ClientRead<TSuccess> ReadPayload<TSuccess>(HttpResponseMessage me, string body, JsonSerializerOptions jsonOptions, string? contentType)
		where TSuccess : notnull
	{
		if (string.IsNullOrWhiteSpace(body))
			return TryGetUnitPayload<TSuccess>(out var legacyUnit)
				? ClientRead<TSuccess>.Of(legacyUnit)
				: ClientRead<TSuccess>.Fail(Error.InternalServerError($"The response status code is '{(int)me.StatusCode}' and the body is empty."));

		return TryDeserialize<TSuccess>(body, jsonOptions, contentType, out var payload, out var payloadFailure)
			? ClientRead<TSuccess>.Of(payload)
			: ClientRead<TSuccess>.Fail(payloadFailure);
	}

	static ResponseMaybe<TSuccess> AsMaybe<TSuccess>(ClientRead<TSuccess> read)
		where TSuccess : notnull
		=> read.Step switch
		{
			ReadStep.NoContent => None.Value,
			ReadStep.Value => read.Value,
			_ => read.Failure,
		};

	static ResponseMaybe<TSuccess, TError> AsMaybeOrThrow<TSuccess, TError>(ClientRead<TSuccess> read)
		where TSuccess : notnull
		where TError : notnull
		=> read.Step switch
		{
			ReadStep.NoContent => None.Value,
			ReadStep.Value => read.Value,
			_ => throw CreateDeserializationError(read.Failure.Message, read.Failure.Exception),
		};

	extension(HttpResponseMessage me)
	{
		/// <summary>Reads the HTTP response as a <see cref="ResponseMaybe{TSuccess}"/>.</summary>
		/// <remarks>
		/// This overload never throws for a protocol level failure: an empty, foreign or undeserializable
		/// body is returned as an <see cref="Error"/> inside the response, because the error shape is known.
		/// The overload that also takes a custom error type cannot do this and throws instead.
		/// </remarks>
		public async Task<ResponseMaybe<TSuccess>> AsResponseAsync<TSuccess>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
		{
			var contentType = ContentType(me);
			var head = await ReadHeadAsync<TSuccess>(me, contentType, ct);
			if (head.Step != ReadStep.Body)
				return AsMaybe(head);

			var body = head.Body;
			var currentJsonOptions = EffectiveJsonOptions(me, jsonOptions);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ResponseJson))
			{
				if (TryDeserialize<ResponseMaybe<TSuccess>>(body, currentJsonOptions, contentType, out var envelope, out var envelopeFailure))
					return envelope;
				return envelopeFailure;
			}

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ProblemJson))
				return ReadProblem(body, currentJsonOptions, me.StatusCode, contentType);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ErrorJson))
			{
				if (TryDeserialize<Error>(body, currentJsonOptions, contentType, out var nativeError, out var nativeFailure))
					return nativeError;
				return nativeFailure;
			}

			if (!me.IsSuccessStatusCode)
				return ReadForeignError(me, body);

			return AsMaybe(ReadPayload<TSuccess>(me, body, currentJsonOptions, contentType));
		}

		/// <summary>Reads the HTTP response as a <see cref="ResponseMaybe{TSuccess, TError}"/>.</summary>
		/// <remarks>
		/// Unlike the overload that only takes a success type, this one throws an
		/// <see cref="InvalidOperationException"/> when the body is empty or cannot be deserialized.
		/// A value of <typeparamref name="TError"/> cannot be synthesized for an arbitrary type, so a
		/// protocol level failure has no representation inside the returned response.
		/// <typeparamref name="TError"/> cannot be <see cref="Error"/> (the union types forbid it by
		/// design); use the single-generic overload for native errors.
		/// </remarks>
		/// <exception cref="InvalidOperationException">The response body is empty or cannot be deserialized.</exception>
		public async Task<ResponseMaybe<TSuccess, TError>> AsResponseAsync<TSuccess, TError>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			where TError : notnull
		{
			var contentType = ContentType(me);
			var head = await ReadHeadAsync<TSuccess>(me, contentType, ct);
			if (head.Step != ReadStep.Body)
				return AsMaybeOrThrow<TSuccess, TError>(head);

			var body = head.Body;
			var currentJsonOptions = EffectiveJsonOptions(me, jsonOptions);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ResponseJson))
			{
				if (TryDeserialize<ResponseMaybe<TSuccess, TError>>(body, currentJsonOptions, contentType, out var envelope, out var envelopeFailure))
					return envelope;
				throw CreateDeserializationError(envelopeFailure.Message, envelopeFailure.Exception);
			}

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ProblemJson))
			{
				var problemError = ReadProblem(body, currentJsonOptions, me.StatusCode, contentType);
				return ReadTypedError<TError>(problemError, currentJsonOptions);
			}

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ErrorJson))
				throw CreateDeserializationError($"The response carries a native Error but '{typeof(TError).GetSignature()}' was expected.");

			if (!me.IsSuccessStatusCode)
			{
				if (string.IsNullOrWhiteSpace(body))
					throw CreateDeserializationError($"The response status code is '{(int)me.StatusCode}' and the body is empty.");
				if (TryDeserialize<TError>(body, currentJsonOptions, contentType, out var typed, out var typedFailure))
					return typed;
				throw CreateDeserializationError(typedFailure.Message ?? $"The error body could not be deserialized as '{typeof(TError).GetSignature()}'.", typedFailure.Exception);
			}

			return AsMaybeOrThrow<TSuccess, TError>(ReadPayload<TSuccess>(me, body, currentJsonOptions, contentType));
		}
	}

	extension(Task<HttpResponseMessage> me)
	{
		public async Task<ResponseMaybe<TSuccess>> AsResponseAsync<TSuccess>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			=> await (await me).AsResponseAsync<TSuccess>(jsonOptions, ct);

		public async Task<ResponseMaybe<TSuccess, TError>> AsResponseAsync<TSuccess, TError>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			where TError : notnull
			=> await (await me).AsResponseAsync<TSuccess, TError>(jsonOptions, ct);
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
