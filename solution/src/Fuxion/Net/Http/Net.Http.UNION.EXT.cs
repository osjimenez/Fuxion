using Fuxion.Reflection;
using Fuxion.Text.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.Union.Net.Http;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Reads union responses out of HTTP messages. The client trusts only what the message says about
/// itself: the media type says the body shape, the naming parameter says how it was written and the
/// status code says whether it went well. There are no format flags: a Fuxion server describes every
/// response and a non-Fuxion server is read as a vanilla API.
/// </summary>
public static class ResponseExtensions
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

	// The token is honoured where the target framework exposes an overload that accepts it. On the
	// older frameworks the read cannot be cancelled, so cancellation is observed before starting it.
	static async Task<string> ReadBodyAsync(HttpResponseMessage me, CancellationToken ct)
	{
		if (me.Content is null) return string.Empty;
#if NET5_0_OR_GREATER
		return await me.Content.ReadAsStringAsync(ct);
#else
		ct.ThrowIfCancellationRequested();
		return await me.Content.ReadAsStringAsync();
#endif
	}

	static bool TryDeserialize<T>(string body, JsonSerializerOptions jsonOptions, out T value, out Error failure)
	{
		var deserialization = body.Fx.Json.Deserialize<T>(options: jsonOptions);
		if (deserialization.IsSuccess)
		{
			value = deserialization.Payload;
			failure = default;
			return true;
		}

		value = default!;
		failure = Error.Critical(deserialization.Message, exception: deserialization.Exception);
		return false;
	}

	// RFC 9457. Some servers omit "status": the HTTP status is the authoritative fallback.
	static Error ReadProblem(string body, JsonSerializerOptions jsonOptions, HttpStatusCode status)
	{
		if (!TryDeserialize<ResponseProblemDetails>(body, jsonOptions, out var problem, out var failure))
			return failure;

		var error = ErrorProblemDetailsConverter.ToError(problem, jsonOptions);
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
			$"The problem details body does not carry an '{ErrorProblemDetailsConverter.ErrorPayloadExtensionName}' extension member deserializable as '{typeof(TError).GetSignature()}'.");
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
	{
		if (!ResponseMediaTypes.TryParse(contentType, out var parsed)) return true;
		var media = parsed.MediaType ?? string.Empty;
		return media.Equals("application/json", StringComparison.OrdinalIgnoreCase)
			|| media.Equals("text/json", StringComparison.OrdinalIgnoreCase)
			|| media.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
			|| ResponseMediaTypes.IsFuxion(contentType);
	}

	static async Task<Stream> ReadStreamAsync(HttpResponseMessage me, CancellationToken ct)
	{
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
			if (me.StatusCode == HttpStatusCode.NoContent)
				return None.Value;

			var contentType = ContentType(me);
			var currentJsonOptions = EffectiveJsonOptions(me, jsonOptions);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.UnitJson))
				return TryGetUnitPayload<TSuccess>(out var unit) ? unit : Error.Critical($"The response is a Unit but '{typeof(TSuccess).GetSignature()}' was expected.");

			// Decide by type and media type BEFORE touching the body: binary payloads are never read as text.
			if (me.IsSuccessStatusCode && !ResponseMediaTypes.IsFuxion(contentType))
			{
				if (BinaryPayload.IsBinaryType(typeof(TSuccess)))
				{
					var (binarySuccess, binaryPayload, binaryFailureMessage) = await TryReadBinaryAsync<TSuccess>(me, ct);
					return binarySuccess ? binaryPayload : Error.Critical(binaryFailureMessage);
				}
				// Unit consumes no content: a legacy server may answer 200 with an empty text/plain body.
				if (typeof(TSuccess) != typeof(Unit) && !IsJsonContentType(contentType))
					return Error.Critical($"The response body is '{me.Content?.Headers.ContentType?.MediaType}', not JSON, and '{typeof(TSuccess).GetSignature()}' cannot be read from it.");
			}

			var body = await ReadBodyAsync(me, ct);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ResponseJson))
			{
				if (TryDeserialize<ResponseMaybe<TSuccess>>(body, currentJsonOptions, out var envelope, out var envelopeFailure))
					return envelope;
				return envelopeFailure;
			}

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ProblemJson))
				return ReadProblem(body, currentJsonOptions, me.StatusCode);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ErrorJson))
			{
				if (TryDeserialize<Error>(body, currentJsonOptions, out var nativeError, out var nativeFailure))
					return nativeError;
				return nativeFailure;
			}

			if (!me.IsSuccessStatusCode)
				return ReadForeignError(me, body);

			if (string.IsNullOrWhiteSpace(body))
				return TryGetUnitPayload<TSuccess>(out var legacyUnit) ? legacyUnit : Error.Critical($"The response status code is '{(int)me.StatusCode}' and the body is empty.");

			if (TryDeserialize<TSuccess>(body, currentJsonOptions, out var payload, out var payloadFailure))
				return payload;
			return payloadFailure;
		}

		/// <summary>Reads the HTTP response as a <see cref="ResponseMaybe{TSuccess, TError}"/>.</summary>
		/// <remarks>
		/// Unlike the overload that only takes a success type, this one throws an
		/// <see cref="InvalidOperationException"/> when the body is empty or cannot be deserialized.
		/// A value of <typeparamref name="TError"/> cannot be synthesized for an arbitrary type, so a
		/// protocol level failure has no representation inside the returned response.
		/// </remarks>
		/// <exception cref="InvalidOperationException">The response body is empty or cannot be deserialized.</exception>
		public async Task<ResponseMaybe<TSuccess, TError>> AsResponseAsync<TSuccess, TError>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			where TSuccess : notnull
			where TError : notnull
		{
			if (me.StatusCode == HttpStatusCode.NoContent)
				return None.Value;

			var contentType = ContentType(me);
			var currentJsonOptions = EffectiveJsonOptions(me, jsonOptions);
			var errorIsNative = typeof(TError) == typeof(Error);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.UnitJson))
				return TryGetUnitPayload<TSuccess>(out var unit) ? unit : throw CreateDeserializationError($"The response is a Unit but '{typeof(TSuccess).GetSignature()}' was expected.");

			// Decide by type and media type BEFORE touching the body: binary payloads are never read as text.
			if (me.IsSuccessStatusCode && !ResponseMediaTypes.IsFuxion(contentType))
			{
				if (BinaryPayload.IsBinaryType(typeof(TSuccess)))
				{
					var (binarySuccess, binaryPayload, binaryFailureMessage) = await TryReadBinaryAsync<TSuccess>(me, ct);
					return binarySuccess ? binaryPayload : throw CreateDeserializationError(binaryFailureMessage);
				}
				// Unit consumes no content: a legacy server may answer 200 with an empty text/plain body.
				if (typeof(TSuccess) != typeof(Unit) && !IsJsonContentType(contentType))
					throw CreateDeserializationError($"The response body is '{me.Content?.Headers.ContentType?.MediaType}', not JSON, and '{typeof(TSuccess).GetSignature()}' cannot be read from it.");
			}

			var body = await ReadBodyAsync(me, ct);

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ResponseJson))
			{
				if (TryDeserialize<ResponseMaybe<TSuccess, TError>>(body, currentJsonOptions, out var envelope, out var envelopeFailure))
					return envelope;
				throw CreateDeserializationError(envelopeFailure.Message, envelopeFailure.Exception);
			}

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ProblemJson))
			{
				var problemError = ReadProblem(body, currentJsonOptions, me.StatusCode);
				return errorIsNative ? (TError)(object)problemError : ReadTypedError<TError>(problemError, currentJsonOptions);
			}

			if (ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ErrorJson))
			{
				if (!errorIsNative)
					throw CreateDeserializationError($"The response carries a native Error but '{typeof(TError).GetSignature()}' was expected.");
				if (TryDeserialize<Error>(body, currentJsonOptions, out var nativeError, out var nativeFailure))
					return (TError)(object)nativeError;
				throw CreateDeserializationError(nativeFailure.Message, nativeFailure.Exception);
			}

			if (!me.IsSuccessStatusCode)
			{
				if (errorIsNative)
					return (TError)(object)ReadForeignError(me, body);
				if (string.IsNullOrWhiteSpace(body))
					throw CreateDeserializationError($"The response status code is '{(int)me.StatusCode}' and the body is empty.");
				if (TryDeserialize<TError>(body, currentJsonOptions, out var typed, out var typedFailure))
					return typed;
				throw CreateDeserializationError(typedFailure.Message ?? $"The error body could not be deserialized as '{typeof(TError).GetSignature()}'.", typedFailure.Exception);
			}

			if (string.IsNullOrWhiteSpace(body))
				return TryGetUnitPayload<TSuccess>(out var legacyUnit) ? legacyUnit : throw CreateDeserializationError($"The response status code is '{(int)me.StatusCode}' and the body is empty.");

			if (TryDeserialize<TSuccess>(body, currentJsonOptions, out var payload, out var payloadFailure))
				return payload;
			throw CreateDeserializationError(payloadFailure.Message, payloadFailure.Exception);
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
