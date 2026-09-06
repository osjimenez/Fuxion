namespace Fuxion.AspNetCore;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Union;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

/// <summary>Binds JSON bodies with the naming policy the request declares on its Content-Type (spec §3), per request.</summary>
/// <remarks>
/// <see cref="SystemTextJsonInputFormatter"/> seals its <c>ReadRequestBodyAsync(InputFormatterContext, Encoding)</c>
/// override in this framework version, so this overrides the single-argument entry point one level up
/// (<see cref="TextInputFormatter"/>) instead and selects the encoding itself, exactly as the base class would.
/// The body is read straight off the request stream through <c>JsonSerializer.DeserializeAsync</c> - never
/// buffered wholesale into a string - both to mirror the stock formatter's behaviour and because reading it
/// through <c>context.ReaderFactory</c>'s <c>TextReader</c> falls back to a synchronous read under the test host,
/// which throws (synchronous request body reads are disallowed there).
/// </remarks>
sealed class ResponseNamingInputFormatter(JsonOptions jsonOptions, ILogger<ResponseNamingInputFormatter> logger) : SystemTextJsonInputFormatter(jsonOptions, logger)
{
	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
	{
		var request = context.HttpContext.Request;
		if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)) return await base.ReadRequestBodyAsync(context);
		var values = contentType.Parameters.Where(p => p.Name.Equals(ResponseMediaTypes.NamingParameter, StringComparison.OrdinalIgnoreCase)).Select(p => p.Value.Value?.Trim('"')).ToList();
		if (values.Count == 0) return await base.ReadRequestBodyAsync(context);
		if (values.Count > 1 || !ResponseNaming.IsSupported(values[0]))
		{
			context.ModelState.TryAddModelError(context.ModelName, $"Unsupported or duplicated '{ResponseMediaTypes.NamingParameter}' parameter: '{string.Join(", ", values)}'.");
			return InputFormatterResult.Failure();
		}

		var options = ResponseNaming.Apply(SerializerOptions, values[0]!);

		// Defensive fast path: InputFormatter.ReadAsync (the public entry point that calls this method) does
		// not itself special-case Content-Length: 0 before calling in - so this is not proven dead code - and
		// even if some caller's own pre-check made it redundant today, it costs nothing (no read at all) and
		// avoids relying on that caller's behaviour never changing.
		if (request.ContentLength == 0)
			return context.TreatEmptyInputAsDefaultValue ? InputFormatterResult.Success(null) : InputFormatterResult.NoValue();

		// A chunked request has no Content-Length to check above, so read a single byte to detect "nothing
		// ever arrives" structurally instead of matching a JsonException message. Peeking through
		// Request.BodyReader directly (read, then AdvanceTo(start, end) to mark examined-not-consumed) was
		// tried first, expecting Request.Body to replay the same unconsumed bytes afterwards since the two
		// facades share the same pipe - but under this host a subsequent read through Request.Body came back
		// empty regardless, turning every real body into a false "empty" read (RED: Snake_IsBound,
		// Kebab_IsBound and Controller_Dictionary_KeysUntouched all failed with 400). PeekedByteStream below
		// reads that one byte through Request.Body itself (the same API this method already uses for the
		// rest of the body) and replays it before the underlying stream, so nothing is lost either way.
		var probe = new byte[1];
		var probeRead = await request.Body.ReadAsync(probe, context.HttpContext.RequestAborted);
		if (probeRead == 0)
			return context.TreatEmptyInputAsDefaultValue ? InputFormatterResult.Success(null) : InputFormatterResult.NoValue();

		var encoding = SelectCharacterEncoding(context);
		if (encoding is null)
		{
			context.ModelState.TryAddModelError(context.ModelName, $"Unsupported charset '{contentType.Charset}'.");
			return InputFormatterResult.Failure();
		}

		// System.Text.Json only reads UTF-8: a non-UTF-8 charset is transcoded on the fly, exactly like the
		// stock formatter, instead of buffering the whole body to convert it.
		Stream body = new PeekedByteStream(request.Body, probe[0]);
		var stream = encoding.Equals(Encoding.UTF8) ? body : Encoding.CreateTranscodingStream(body, encoding, Encoding.UTF8, leaveOpen: true);
		try
		{
			// Same shape as the base implementation: a malformed body is a model-state error, not an exception.
			var model = await JsonSerializer.DeserializeAsync(stream, context.ModelType, options, context.HttpContext.RequestAborted);
			return model is null && !context.TreatEmptyInputAsDefaultValue ? InputFormatterResult.NoValue() : InputFormatterResult.Success(model);
		}
		catch (JsonException ex)
		{
			// Emptiness was already ruled out above (Content-Length and the one-byte probe), so reaching
			// here means the body actually has bytes System.Text.Json could not parse: a genuine malformed
			// body, not an empty one. Wrapped in InputFormatterException, like the stock formatter does, so
			// the message reaches ValidationProblemDetails instead of being reduced to a generic model error.
			var key = ex.Path ?? context.ModelName;
			context.ModelState.TryAddModelError(key, new InputFormatterException(ex.Message, ex), context.Metadata);
			return InputFormatterResult.Failure();
		}
		finally
		{
			if (!ReferenceEquals(stream, body)) await stream.DisposeAsync();
		}
	}
}

/// <summary>Replays one already-read byte in front of an inner stream, so a stream can be probed one byte deep
/// (to tell "no data at all" from "some data") without losing that byte for the real read that follows.
/// Never disposes the inner stream: that one is owned by the request, not by this wrapper.</summary>
/// <remarks>
/// Only the async read overloads are implemented: <see cref="JsonSerializer.DeserializeAsync{TValue}(Stream, System.Text.Json.JsonSerializerOptions?, System.Threading.CancellationToken)"/>
/// (the only consumer of this stream) never calls a synchronous <c>Read</c>, and this host disallows
/// synchronous request-body reads anyway, so the synchronous overloads throw <see cref="NotSupportedException"/>
/// instead of silently blocking on the inner stream (sync-over-async) to serve one.
/// </remarks>
internal sealed class PeekedByteStream(Stream inner, byte firstByte) : Stream
{
	// Only set once the peeked byte has actually been written into a caller's buffer - a zero-length read
	// (an empty buffer, or count 0) writes nothing, so it must return 0 without marking the byte as served,
	// or it would be silently dropped: the next, real read would go straight to the inner stream and skip it.
	bool served;

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();
	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

	public override void Flush() { }

	public override int Read(byte[] buffer, int offset, int count)
		=> throw new NotSupportedException("Synchronous reads are not supported by this stream; only the async overloads are.");

	public override int Read(Span<byte> buffer)
		=> throw new NotSupportedException("Synchronous reads are not supported by this stream; only the async overloads are.");

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!served)
		{
			if (count == 0) return 0; // nothing written: still unserved, try again on the next call
			buffer[offset] = firstByte;
			served = true;
			return 1;
		}
		return await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (!served)
		{
			if (buffer.IsEmpty) return 0; // nothing written: still unserved, try again on the next call
			buffer.Span[0] = firstByte;
			served = true;
			return 1;
		}
		return await inner.ReadAsync(buffer, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing) { } // the inner stream (Request.Body) is not ours to dispose
}

sealed class ResponseNamingInputFormatterSetup(IOptions<JsonOptions> jsonOptions, ILoggerFactory loggerFactory) : IPostConfigureOptions<MvcOptions>
{
	// IPostConfigureOptions always runs after every IConfigureOptions<MvcOptions>, including the framework's own
	// MvcCoreMvcOptionsSetup that populates InputFormatters - unlike IConfigureOptions, whose relative order
	// against that setup depends on whether AddResponses() or AddControllers() ran first.
	public void PostConfigure(string? name, MvcOptions options)
	{
		options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
		options.InputFormatters.Insert(0, new ResponseNamingInputFormatter(jsonOptions.Value, loggerFactory.CreateLogger<ResponseNamingInputFormatter>()));
	}
}
