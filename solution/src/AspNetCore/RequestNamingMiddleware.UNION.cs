namespace Fuxion.AspNetCore;

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Fuxion.Union;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Honours the <c>naming</c> parameter of the request Content-Type: a body written in snake_case or
/// kebab-case is rewritten so the (case-insensitive) binders match the CLR property names. camelCase
/// and PascalCase already bind, so they pass through untouched.
/// </summary>
sealed class RequestNamingMiddleware(RequestDelegate next)
{
	public async Task InvokeAsync(HttpContext context)
	{
		var naming = ResponseNaming.GetParameter(context.Request.ContentType);
		if (NeedsTranscoding(naming) && IsJson(context.Request.ContentType) && context.Request.ContentLength != 0)
		{
			using var buffer = new MemoryStream();
			await context.Request.Body.CopyToAsync(buffer, context.RequestAborted);
			if (buffer.Length > 0)
			{
				try
				{
					var transcoded = JsonNamingTranscoder.Transcode(new ReadOnlySpan<byte>(buffer.GetBuffer(), 0, (int)buffer.Length));
					context.Request.Body = new MemoryStream(transcoded);
					context.Request.ContentLength = transcoded.Length;
				}
				catch (JsonException)
				{
					// Malformed input: leave it untouched so the framework's own binding fails it the
					// usual way (400), instead of surfacing the transcoder's exception as a 500.
					context.Request.Body = new MemoryStream(buffer.ToArray());
				}
			}
		}

		await next(context);
	}

	static bool NeedsTranscoding(string? naming)
		=> string.Equals(naming, ResponseNaming.Snake, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(naming, ResponseNaming.Kebab, StringComparison.OrdinalIgnoreCase);

	// The naming parameter is JSON-specific: a multipart/form-data or text/plain body that happens to
	// carry it must never be treated as JSON to transcode.
	static bool IsJson(string? contentType)
	{
		if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed) || parsed.MediaType is not { } mediaType)
			return false;

		return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
			|| mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
	}
}

/// <summary>Inserts <see cref="RequestNamingMiddleware"/> at the start of the pipeline from <c>AddResponses</c>, so consumers configure nothing.</summary>
sealed class ResponseNamingStartupFilter : IStartupFilter
{
	public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
		=> app =>
		{
			app.UseMiddleware<RequestNamingMiddleware>();
			next(app);
		};
}
