namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Union;

/// <summary>
/// Honours the <c>naming</c> parameter of a JSON request Content-Type: a snake_case or kebab-case body is
/// rewritten so the (case-insensitive) binder matches the CLR property names. A malformed body is left
/// untouched so the framework fails the way it always does.
/// </summary>
public sealed class RequestNamingHandler : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var contentType = request.Content?.Headers.ContentType;
		var naming = ResponseNaming.GetParameter(contentType?.ToString());
		// Parity with the AspNetCore middleware: an empty body needs no transcoding, so it is never buffered.
		if (request.Content is not null && contentType is not null && request.Content.Headers.ContentLength != 0 && NeedsTranscoding(naming) && IsJson(contentType))
		{
			var original = request.Content;
			var originalBytes = await original.ReadAsByteArrayAsync().ConfigureAwait(false);
			if (originalBytes.Length > 0)
			{
				byte[] bytes;
				try { bytes = JsonNamingTranscoder.Transcode(originalBytes); }
				catch (JsonException) { bytes = originalBytes; }

				var replaced = new ByteArrayContent(bytes);
				// Preserve the other content headers (Content-Encoding, Content-Language...): Content-Length must come
				// from the new (transcoded) body, not the original's, and Content-Type is set explicitly right below.
				foreach (var header in original.Headers)
					if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)
						&& !string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
						replaced.Headers.TryAddWithoutValidation(header.Key, header.Value);
				replaced.Headers.ContentType = (MediaTypeHeaderValue)((ICloneable)contentType).Clone();

				request.Content = replaced;
				original.Dispose();
			}
		}

		return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
	}

	static bool NeedsTranscoding(string? naming)
		=> string.Equals(naming, ResponseNaming.Snake, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(naming, ResponseNaming.Kebab, StringComparison.OrdinalIgnoreCase);

	static bool IsJson(MediaTypeHeaderValue contentType)
		=> string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)
			|| (contentType.MediaType?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ?? false);
}
