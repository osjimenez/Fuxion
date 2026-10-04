using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Fuxion.AspNet;

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
