using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Fuxion.Text.Json;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Fuxion.AspNetCore;

/// <summary>
/// Minimal APIs compile the body binder once per endpoint (RequestDelegateFactory bakes the JsonTypeInfo in), so a
/// request-declared naming cannot reach the binder. This wraps the endpoint's own RequestDelegate - hence it runs
/// after routing and authorization - and rewrites separator-cased keys guided by the body type the endpoint
/// declares (IAcceptsMetadata). Endpoints without a declared body type are left untouched.
/// </summary>
static class RequestNamingEndpoint
{
	public static void Apply(EndpointBuilder builder)
	{
		// MVC controller actions already have their own per-request naming support (RequestNamingInputFormatter,
		// registered via AddResponses()); wrapping their RequestDelegate here too would transcode the body twice.
		// This lets app.MapControllers().UseResponses() apply the response-side wire contract (envelope, Accept)
		// to controllers without double-transcoding request bodies.
		if (builder.Metadata.OfType<ControllerActionDescriptor>().Any()) return;

		// UseResponses() can be called at more than one nesting level for the same endpoint (a root group and
		// one of its nested groups both calling it), and Finally runs once per call. The marker makes wrapping
		// idempotent so the RequestDelegate is only ever wrapped once, however many times Apply runs.
		if (builder.Metadata.OfType<RequestNamingApplied>().Any()) return;
		builder.Metadata.Add(RequestNamingApplied.Instance);

		var bodyType = builder.Metadata.OfType<IAcceptsMetadata>().FirstOrDefault()?.RequestType;
		var inner = builder.RequestDelegate;
		if (bodyType is null || inner is null) return;

		builder.RequestDelegate = async context =>
		{
			if (await TryPrepareBodyAsync(context, bodyType))
				await inner(context);
		};
	}

	// Returns false when a response was already written (400/413).
	static async Task<bool> TryPrepareBodyAsync(HttpContext context, Type bodyType)
	{
		if (!TryReadNaming(context.Request.ContentType, out var naming, out var charset, out var invalidDetail))
			return await WriteProblemAsync(context, StatusCodes.Status400BadRequest, invalidDetail);
		if (!ResponseMediaTypes.TryGetRequestEncoding(charset, out var encoding))
			return await WriteProblemAsync(context, StatusCodes.Status415UnsupportedMediaType, $"Unsupported charset '{charset}' on the request Content-Type: only UTF-8 and UTF-16 are read.");
		if (naming is null || !ResponseNaming.NeedsSeparatorTranscoding(naming)) return true; // absent, or camel/pascal: bind as they are

		var max = context.RequestServices.GetService<IOptions<ResponseOptions>>()?.Value.RequestNamingMaxBodySize ?? new ResponseOptions().RequestNamingMaxBodySize;
		using var buffer = await ReadBoundedAsync(context, max);
		if (buffer is null)
			return await WriteProblemAsync(context, StatusCodes.Status413PayloadTooLarge, $"Request body exceeds the configured limit of {max} bytes.");
		if (buffer.Length == 0) return true;

		var jsonOptions = context.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
		// The transcoder works on UTF-8: a UTF-16 body is converted first, and the Content-Type then says so.
		var utf8 = encoding.Equals(Encoding.UTF8) ? buffer.ToArray() : Encoding.UTF8.GetBytes(encoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
		byte[] bytes;
		try { bytes = JsonNamingTranscoder.Transcode(utf8, bodyType, jsonOptions); }
		catch (JsonException) { bytes = utf8; } // malformed: let the binder fail it the usual way (400)
		if (!encoding.Equals(Encoding.UTF8) && MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var declared))
		{
			declared.Charset = "utf-8";
			context.Request.ContentType = declared.ToString();
		}

		context.Request.Body = new MemoryStream(bytes);
		context.Request.ContentLength = bytes.Length;
		return true;
	}

	// The naming and the charset a JSON request declares on its Content-Type. True with a null naming when the body is
	// not JSON or declares none; false, with the problem detail, when the parameter is unsupported or duplicated.
	static bool TryReadNaming(string? contentTypeHeader, out string? naming, out string? charset, out string invalidDetail)
	{
		naming = null;
		charset = null;
		invalidDetail = string.Empty;
		if (!MediaTypeHeaderValue.TryParse(contentTypeHeader, out var contentType) || !IsJson(contentType)) return true;
		charset = contentType.Charset.HasValue ? contentType.Charset.Value : null;

		var values = contentType.Parameters
			.Where(p => p.Name.Equals(ResponseMediaTypes.NamingParameter, StringComparison.OrdinalIgnoreCase))
			.Select(p => p.Value.Value?.Trim('"')).ToList();
		if (values.Count == 0) return true;
		if (values.Count > 1 || !ResponseNaming.IsSupported(values[0]))
		{
			invalidDetail = $"Unsupported or duplicated '{ResponseMediaTypes.NamingParameter}' parameter: '{string.Join(", ", values)}'.";
			return false;
		}
		naming = values[0];
		return true;
	}

	// The whole body, or null when it is over the limit. A declared Content-Length is checked first so an oversized
	// body is never read; a chunked one is read only until one byte over the limit, which is enough to decide.
	static async Task<MemoryStream?> ReadBoundedAsync(HttpContext context, long max)
	{
		if (context.Request.ContentLength is { } declared && declared > max) return null;

		var buffer = new MemoryStream();
		var chunk = new byte[16 * 1024];
		int read;
		while ((read = await context.Request.Body.ReadAsync(chunk, 0, chunk.Length, context.RequestAborted)) > 0)
		{
			buffer.Write(chunk, 0, read);
			if (buffer.Length > max)
			{
				buffer.Dispose();
				return null;
			}
		}
		return buffer;
	}

	static async Task<bool> WriteProblemAsync(HttpContext context, int status, string detail)
	{
		if (!context.Response.HasStarted)
			await Results.Problem(statusCode: status, detail: detail).ExecuteAsync(context);
		return false;
	}

	// Minimal APIs only bind application/json and +json bodies: text/json is answered 415 before the handler
	// runs, so there is nothing to transcode for it.
	static bool IsJson(MediaTypeHeaderValue contentType)
		=> ResponseMediaTypes.IsJson(contentType.MediaType.Value)
			&& !ResponseMediaTypes.Is(contentType.MediaType.Value, ResponseMediaTypes.TextJson);
}

/// <summary>
/// Marks an endpoint as already wrapped by <see cref="RequestNamingEndpoint.Apply"/>, so a second call (from a
/// nested group that also calls <c>UseResponses()</c>) is a no-op instead of wrapping the RequestDelegate twice.
/// Internal rather than private so a test can assert an endpoint carries exactly one instance of it.
/// </summary>
sealed class RequestNamingApplied
{
	public static readonly RequestNamingApplied Instance = new();
}
