namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// A Web API 2 formatter backed by System.Text.Json, so request binding and non-union responses use the
/// same options as the union wire contract (naming policy, converters, omission of undefined members).
/// </summary>
/// <remarks>
/// Request bodies are always decoded as UTF-8: <see cref="System.Text.Json.JsonSerializer"/> reads UTF-8
/// (or UTF-8 with a BOM) only, so a request declaring a different <c>charset</c> parameter is not honoured.
/// </remarks>
public sealed class SystemTextJsonMediaTypeFormatter : MediaTypeFormatter
{
	public SystemTextJsonMediaTypeFormatter(JsonSerializerOptions options)
	{
		Options = options ?? throw new ArgumentNullException(nameof(options));
		SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
		SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
		SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	/// <summary>The options every read and write goes through.</summary>
	public JsonSerializerOptions Options { get; }

	public override bool CanReadType(Type type) => true;
	public override bool CanWriteType(Type type) => true;

	public override async Task<object?> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
	{
		var contentLength = content?.Headers.ContentLength;
		if (contentLength == 0)
			return GetDefaultValueForType(type);
		if (contentLength is null)
		{
			// Chunked or otherwise length-less content: Web API's own formatters buffer a non-seekable
			// body too, so do the same here to tell an empty body (no Content-Length header at all)
			// apart from a malformed one before handing it to the deserializer.
			var buffer = new MemoryStream();
			await readStream.CopyToAsync(buffer).ConfigureAwait(false);
			if (buffer.Length == 0)
				return GetDefaultValueForType(type);
			buffer.Position = 0;
			try { return await JsonSerializer.DeserializeAsync(buffer, type, Options).ConfigureAwait(false); }
			catch (JsonException ex) when (formatterLogger is not null)
			{
				// BaseJsonMediaTypeFormatter's own behaviour: a malformed body is a model-binding error
				// (reported through IFormatterLogger, which feeds ModelState), not an unhandled 500 -
				// unless there is no logger to report it to, in which case the exception still escapes.
				formatterLogger.LogError(string.Empty, ex);
				return GetDefaultValueForType(type);
			}
		}
		try { return await JsonSerializer.DeserializeAsync(readStream, type, Options).ConfigureAwait(false); }
		catch (JsonException ex) when (formatterLogger is not null)
		{
			formatterLogger.LogError(string.Empty, ex);
			return GetDefaultValueForType(type);
		}
	}

	public override Task WriteToStreamAsync(Type type, object? value, Stream writeStream, HttpContent content, TransportContext transportContext)
		=> JsonSerializer.SerializeAsync(writeStream, value, type, Options);
}
