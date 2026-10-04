using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Fuxion.AspNetCore;

// Writes an IOContent. A seekable stream goes to the framework (Results.File or FileStreamResult), which already serves
// ranges, conditional requests and If-Range. This only adds what the framework cannot do with a stream that is not
// seekable: send a length known beforehand, and serve a single range by asking the source for just that range.
static class IOContentWriter
{
	public static async Task WriteAsync(HttpContext http, IOContent file, Func<Stream, bool, Task> frameworkWrite)
	{
		var cancellationToken = http.RequestAborted;
		if (file.EnableRangeProcessing && file.Source is IRangeContentSource ranges && file.Length is { } length && TryGetSingleRange(http.Request, file, length, out var range))
		{
			if (range is { } satisfiable)
				await WriteRangeAsync(http.Response, file, ranges, satisfiable.From, satisfiable.To, length, cancellationToken);
			else
				WriteUnsatisfiable(http.Response, length);
			return;
		}

		var stream = await file.OpenAsync(cancellationToken);
		if (!stream.CanSeek)
		{
			// The framework leaves Content-Length out for a stream that cannot tell its length.
			if (file.Length is { } known)
				http.Response.ContentLength = known;
			if (file.EnableRangeProcessing && file.Source is IRangeContentSource)
				http.Response.Headers.AcceptRanges = "bytes";
		}
		await frameworkWrite(stream, file.EnableRangeProcessing && stream.CanSeek);
	}

	// True when the request asks for exactly one byte range (and If-Range, if sent, still matches); the range is null
	// when it cannot be satisfied. Anything else (no Range, several ranges, another unit) is answered in full.
	static bool TryGetSingleRange(HttpRequest request, IOContent file, long length, out (long From, long To)? range)
	{
		range = null;
		var header = request.Headers.Range.ToString();
		if (string.IsNullOrEmpty(header)
			|| !RangeHeaderValue.TryParse(header, out var parsed)
			|| !string.Equals(parsed.Unit.Value, "bytes", StringComparison.OrdinalIgnoreCase)
			|| parsed.Ranges.Count != 1)
			return false;
		var ifRange = request.Headers.IfRange.ToString();
		if (!string.IsNullOrEmpty(ifRange) && !string.Equals(ifRange, file.ETag, StringComparison.Ordinal))
			return false;

		var item = parsed.Ranges.Single();
		if (item.From is { } from)
		{
			if (from < length)
				range = (from, Math.Min(item.To ?? length - 1, length - 1));
		}
		else if (item.To is > 0 and var suffix)
			// "bytes=-N": the last N bytes.
			range = (Math.Max(0, length - suffix), length - 1);
		return true;
	}

	static async Task WriteRangeAsync(HttpResponse response, IOContent file, IRangeContentSource source, long from, long to, long length, CancellationToken cancellationToken)
	{
		response.StatusCode = StatusCodes.Status206PartialContent;
		WriteFileHeaders(response, file);
		response.Headers.ContentRange = new ContentRangeHeaderValue(from, to, length).ToString();
		response.ContentLength = to - from + 1;
		using var stream = await source.OpenRangeAsync(from, to, cancellationToken);
		await stream.CopyToAsync(response.Body, cancellationToken);
	}

	// Like the framework: an empty 416 with the length the client can ask for.
	static void WriteUnsatisfiable(HttpResponse response, long length)
	{
		response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
		response.Headers.ContentRange = new ContentRangeHeaderValue(length).ToString();
		response.ContentLength = 0;
	}

	// The headers the framework writes for a file.
	static void WriteFileHeaders(HttpResponse response, IOContent file)
	{
		response.ContentType = file.ContentType;
		response.Headers.AcceptRanges = "bytes";
		if (file.FileName is not null)
		{
			var disposition = new ContentDispositionHeaderValue("attachment");
			disposition.SetHttpFileName(file.FileName);
			response.Headers.ContentDisposition = disposition.ToString();
		}
		if (file.LastModified is { } lastModified)
			response.Headers.LastModified = HeaderUtilities.FormatDate(lastModified);
		if (ResponseHttpAdapter.ParseETag(file.ETag) is { } etag)
			response.Headers.ETag = etag.ToString();
	}
}
