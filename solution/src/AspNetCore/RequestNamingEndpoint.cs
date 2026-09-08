namespace Fuxion.AspNetCore;

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

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
		// MVC controller actions already have their own per-request naming support (ResponseNamingInputFormatter,
		// registered via AddResponses()); wrapping their RequestDelegate here too would transcode the body twice.
		// This lets app.MapControllers().UseResponses() apply the response-side wire contract (envelope, Accept)
		// to controllers without double-transcoding request bodies.
		if (builder.Metadata.OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>().Any()) return;

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
		var request = context.Request;
		if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) || !IsJson(contentType)) return true;

		var values = contentType.Parameters
			.Where(p => p.Name.Equals(ResponseMediaTypes.NamingParameter, StringComparison.OrdinalIgnoreCase))
			.Select(p => p.Value.Value?.Trim('"')).ToList();
		if (values.Count == 0) return true;
		if (values.Count > 1 || !ResponseNaming.IsSupported(values[0]))
		{
			if (!context.Response.HasStarted)
				await Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Unsupported or duplicated '{ResponseMediaTypes.NamingParameter}' parameter: '{string.Join(", ", values)}'.").ExecuteAsync(context);
			return false;
		}
		if (!ResponseNaming.NeedsSeparatorTranscoding(values[0])) return true; // camel/pascal bind as they are

		var max = context.RequestServices.GetService<IOptions<ResponseOptions>>()?.Value.RequestNamingMaxBodySize ?? new ResponseOptions().RequestNamingMaxBodySize;
		if (request.ContentLength is { } declared && declared > max)
		{
			if (!context.Response.HasStarted)
				await Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, detail: $"Request body exceeds the configured limit of {max} bytes.").ExecuteAsync(context);
			return false;
		}

		using var buffer = new MemoryStream();
		await CopyBoundedAsync(request.Body, buffer, max, context);
		if (buffer.Length > max)
		{
			if (!context.Response.HasStarted)
				await Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, detail: $"Request body exceeds the configured limit of {max} bytes.").ExecuteAsync(context);
			return false;
		}
		if (buffer.Length == 0) return true;

		var jsonOptions = context.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
		byte[] bytes;
		try { bytes = JsonNamingTranscoder.Transcode(new ReadOnlySpan<byte>(buffer.GetBuffer(), 0, (int)buffer.Length), bodyType, jsonOptions); }
		catch (JsonException) { bytes = buffer.ToArray(); } // malformed: let the binder fail it the usual way (400)

		request.Body = new MemoryStream(bytes);
		request.ContentLength = bytes.Length;
		return true;
	}

	static async Task CopyBoundedAsync(Stream source, MemoryStream target, long max, HttpContext context)
	{
		var chunk = new byte[16 * 1024];
		int read;
		while ((read = await source.ReadAsync(chunk, 0, chunk.Length, context.RequestAborted)) > 0)
		{
			target.Write(chunk, 0, read);
			if (target.Length > max) return; // one byte over is enough to decide
		}
	}

	static bool IsJson(MediaTypeHeaderValue contentType)
		=> contentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
			|| contentType.MediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
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
