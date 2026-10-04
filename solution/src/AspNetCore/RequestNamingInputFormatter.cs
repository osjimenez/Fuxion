using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Fuxion.AspNetCore;

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
sealed class RequestNamingInputFormatter(JsonOptions jsonOptions, ILogger<RequestNamingInputFormatter> logger) : SystemTextJsonInputFormatter(jsonOptions, logger)
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

		// InputFormatter.ReadAsync does not filter Content-Length: 0 before calling this method, so an empty
		// body is answered here without reading anything.
		if (request.ContentLength == 0)
			return context.TreatEmptyInputAsDefaultValue ? InputFormatterResult.Success(null) : InputFormatterResult.NoValue();

		// A chunked request has no Content-Length, so a single byte is probed to detect an empty body
		// structurally instead of matching a JsonException message. The probe goes through Request.Body:
		// Request.BodyReader does not rewind under TestServer, so a peek through it would lose the bytes.
		// PeekedByteStream replays the probed byte before the rest of the body.
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

sealed class RequestNamingInputFormatterSetup(IOptions<JsonOptions> jsonOptions, ILoggerFactory loggerFactory) : IPostConfigureOptions<MvcOptions>
{
	// IPostConfigureOptions always runs after every IConfigureOptions<MvcOptions>, including the framework's own
	// MvcCoreMvcOptionsSetup that populates InputFormatters - unlike IConfigureOptions, whose relative order
	// against that setup depends on whether AddResponses() or AddControllers() ran first.
	public void PostConfigure(string? name, MvcOptions options)
	{
		options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
		options.InputFormatters.Insert(0, new RequestNamingInputFormatter(jsonOptions.Value, loggerFactory.CreateLogger<RequestNamingInputFormatter>()));
	}
}
