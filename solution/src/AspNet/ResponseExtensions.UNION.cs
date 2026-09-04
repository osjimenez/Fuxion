namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using Fuxion.Union;

/// <summary>
/// Maps union return values of Web API 2 actions to the wire contract. Only actions whose declared
/// return type is a union shape are touched; everything else keeps the framework's behaviour.
/// </summary>
public sealed class ResponseActionFilter : ActionFilterAttribute
{
	public override void OnActionExecuted(HttpActionExecutedContext context)
	{
		if (context.Exception is not null || context.Response is null) return;
		// ReflectedHttpActionDescriptor.ReturnType is null for void and non-generic Task actions: there is
		// no declared union shape to check, so this is the same as "not supported", not a null to dereference.
		if (context.ActionContext.ActionDescriptor.ReturnType is not { } declared || !ResponseWireMapper.IsSupportedDeclaredResponseType(declared)) return;

		var original = context.Response;
		if (!original.TryGetContentValue<object>(out var value)) return;

		var request = context.Request;
		if (!ResponseWireMapper.TryMap(value, ResponseOptionsResolver.Resolve(request), out var mapping)) return;

		// Build the replacement before disposing the original: Materialize/Write can still read from
		// the extracted value, and the original's ObjectContent must not be dropped without disposal.
		var replacement = ResponseHttpWriter.Write(request, mapping, ResponseOptionsResolver.ResolveJsonOptions(request));
		original.Dispose();
		context.Response = replacement;
	}
}

/// <summary>Writes a wire mapping as an <see cref="HttpResponseMessage"/> with System.Text.Json.</summary>
static class ResponseHttpWriter
{
	public static HttpResponseMessage Write(HttpRequestMessage request, ResponseWireMapping mapping, JsonSerializerOptions jsonOptions)
	{
		if (mapping.Shape == ResponseWireShape.Binary)
			return WriteBinary(request, (FileContent)mapping.Value!);

		var response = request.CreateResponse((HttpStatusCode)mapping.StatusCode);
		// The shape depends on Accept, so shared caches must key on it.
		response.Headers.Vary.Add("Accept");

		if (!mapping.HasBody) return response;

		var (contentType, body, serializerOptions) = mapping.Materialize(jsonOptions);
		var bytes = JsonSerializer.SerializeToUtf8Bytes(body, body!.GetType(), serializerOptions ?? jsonOptions);
		// Written verbatim, like the ASP.NET Core adapter: application/json (and the Fuxion media types)
		// never carry a charset parameter.
		var mediaType = MediaTypeHeaderValue.Parse(contentType!);
		response.Content = new ByteArrayContent(bytes) { Headers = { ContentType = mediaType } };
		return response;
	}

	// A file is the purest self-describing message: its own media type, Content-Disposition, ETag,
	// Last-Modified and ranges. No Vary: its shape does not depend on Accept.
	static HttpResponseMessage WriteBinary(HttpRequestMessage request, FileContent file)
	{
		var mediaType = MediaTypeHeaderValue.Parse(file.ContentType);
		var response = request.CreateResponse(HttpStatusCode.OK);

		if (file.EnableRangeProcessing && file.Stream.CanSeek && request.Headers.Range is not null)
		{
			try
			{
				response.Content = new ByteRangeStreamContent(file.Stream, request.Headers.Range, mediaType);
				response.StatusCode = HttpStatusCode.PartialContent;
			}
			catch (InvalidByteRangeException invalidRange)
			{
				// No content ever took ownership of the stream: the wrapper's constructor failed before assignment.
				file.Stream.Dispose();
				return request.CreateErrorResponse(invalidRange);
			}
		}
		else
		{
			response.Content = new StreamContent(file.Stream);
			response.Content.Headers.ContentType = mediaType;
		}

		if (file.FileName is not null)
		{
			var disposition = new ContentDispositionHeaderValue("attachment");
			// FileName is the ASCII-only legacy form; FileNameStar (RFC 5987) is the fallback for non-ASCII
			// names and is always set, matching ASP.NET Core's Results.File behaviour.
			try { disposition.FileName = file.FileName; }
			catch (FormatException) { }
			disposition.FileNameStar = file.FileName;
			response.Content.Headers.ContentDisposition = disposition;
		}
		if (file.LastModified is not null)
			response.Content.Headers.LastModified = file.LastModified;
		if (!string.IsNullOrWhiteSpace(file.ETag) && EntityTagHeaderValue.TryParse(file.ETag, out var etag))
			response.Headers.ETag = etag;
		if (file.EnableRangeProcessing)
			response.Headers.AcceptRanges.Add("bytes");

		return response;
	}
}

public static class ResponseHttpActionResultExtensions
{
	extension(IResponse me)
	{
		/// <summary>Deferred: options (scope + Accept) are resolved from the request when the result executes.</summary>
		public IHttpActionResult ToHttpActionResult(HttpRequestMessage request) => new WireHttpActionResult(request, me, null);

		/// <summary>Explicit options; Accept is not consulted.</summary>
		public IHttpActionResult ToHttpActionResult(HttpRequestMessage request, ResponseOptions options) => new WireHttpActionResult(request, me, options);
	}
}

/// <summary>
/// Web API 2 offers no context outside the request (unlike ASP.NET Core's HttpContext), so the request
/// travels explicitly and options are resolved lazily when the framework executes the result.
/// </summary>
sealed class WireHttpActionResult(HttpRequestMessage request, IResponse response, ResponseOptions? explicitOptions) : IHttpActionResult
{
	public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
	{
		var options = explicitOptions ?? ResponseOptionsResolver.Resolve(request);
		if (!ResponseWireMapper.TryMap(response, options, out var mapping))
			throw new NotSupportedException($"The union response value of type '{response.Value?.GetType().FullName ?? "null"}' is not supported.");
		return Task.FromResult(ResponseHttpWriter.Write(request, mapping, ResponseOptionsResolver.ResolveJsonOptions(request)));
	}
}
