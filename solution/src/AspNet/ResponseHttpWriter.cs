using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Tracing;

namespace Fuxion.AspNet;

/// <summary>Writes a wire mapping as an <see cref="HttpResponseMessage"/> with System.Text.Json.</summary>
static class ResponseHttpWriter
{
	// Asynchronous because a binary content is opened from its source only now, when it is written.
	public static async Task<HttpResponseMessage> WriteAsync(HttpRequestMessage request, ResponseWireMapping mapping, JsonSerializerOptions jsonOptions, CancellationToken cancellationToken)
	{
		WarnDroppedExtensions(request, mapping);

		if (mapping.Shape == ResponseWireShape.Binary)
			return await WriteBinaryAsync(request, (IOContent)mapping.Value!, cancellationToken);

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

	// The response extensions only travel inside the envelope: when the chosen shape drops them, say so (RF-23).
	static void WarnDroppedExtensions(HttpRequestMessage request, ResponseWireMapping mapping)
	{
		if (mapping.DroppedExtensions.Count == 0)
			return;
		request.GetConfiguration()?.Services.GetTraceWriter()?.Warn(request, TraceCategory,
			"The response to {0} {1} was written as {2}, which has no place for its extensions, so they were dropped: {3}. Only the envelope ({4}) carries them.",
			request.Method, request.RequestUri?.AbsolutePath, mapping.Shape, string.Join(", ", mapping.DroppedExtensions), ResponseMediaTypes.ResponseJson);
	}

	public const string TraceCategory = "Fuxion.Responses";

	// A file is the purest self-describing message: its own media type, Content-Disposition, ETag,
	// Last-Modified and ranges. No Vary: its shape does not depend on Accept.
	static async Task<HttpResponseMessage> WriteBinaryAsync(HttpRequestMessage request, IOContent file, CancellationToken cancellationToken)
	{
		var mediaType = MediaTypeHeaderValue.Parse(file.ContentType);
		var response = request.CreateResponse(HttpStatusCode.OK);

		// A source that opens ranges by itself (a remote object) serves a single range without opening the whole
		// content: its streams are not seekable, so ByteRangeStreamContent could not cut them.
		if (file.EnableRangeProcessing && file.Source is IRangeContentSource ranges && file.Length is { } length && TryGetSingleRange(request, file, length, out var range))
		{
			if (range is not { } satisfiable)
				return request.CreateErrorResponse(new InvalidByteRangeException(new ContentRangeHeaderValue(length)));
			response.StatusCode = HttpStatusCode.PartialContent;
			response.Content = new StreamContent(await ranges.OpenRangeAsync(satisfiable.From, satisfiable.To, cancellationToken));
			response.Content.Headers.ContentType = mediaType;
			response.Content.Headers.ContentRange = new ContentRangeHeaderValue(satisfiable.From, satisfiable.To, length);
			response.Content.Headers.ContentLength = satisfiable.To - satisfiable.From + 1;
			return WithFileHeaders(response, file);
		}

		var stream = await file.OpenAsync(cancellationToken);
		if (file.EnableRangeProcessing && stream.CanSeek && request.Headers.Range is not null)
		{
			try
			{
				response.Content = new ByteRangeStreamContent(stream, request.Headers.Range, mediaType);
				response.StatusCode = HttpStatusCode.PartialContent;
			}
			catch (InvalidByteRangeException invalidRange)
			{
				// No content ever took ownership of the stream: the wrapper's constructor failed before assignment.
				stream.Dispose();
				return request.CreateErrorResponse(invalidRange);
			}
		}
		else
		{
			response.Content = new StreamContent(stream);
			response.Content.Headers.ContentType = mediaType;
			// StreamContent can only tell the length of a seekable stream.
			if (!stream.CanSeek && file.Length is { } known)
				response.Content.Headers.ContentLength = known;
		}

		return WithFileHeaders(response, file);
	}

	// True when the request asks for exactly one byte range (and If-Range, if sent, still matches); the range is null
	// when it cannot be satisfied. Anything else (no Range, several ranges, another unit) is answered in full.
	static bool TryGetSingleRange(HttpRequestMessage request, IOContent file, long length, out (long From, long To)? range)
	{
		range = null;
		if (request.Headers.Range is not { } header
			|| !string.Equals(header.Unit, "bytes", StringComparison.OrdinalIgnoreCase)
			|| header.Ranges.Count != 1)
			return false;
		if (request.Headers.IfRange is { } ifRange && (ifRange.EntityTag is null || ifRange.EntityTag.ToString() != file.ETag))
			return false;

		var item = header.Ranges.Single();
		if (item.From is { } from)
		{
			if (from < length)
				range = (from, Math.Min(item.To ?? length - 1, length - 1));
		}
		else if (item.To is > 0 and { } suffix)
			// "bytes=-N": the last N bytes.
			range = (Math.Max(0, length - suffix), length - 1);
		return true;
	}

	static HttpResponseMessage WithFileHeaders(HttpResponseMessage response, IOContent file)
	{
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
