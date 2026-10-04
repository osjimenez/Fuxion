using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;

namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// A Web API 2 formatter backed by System.Text.Json, so request binding and (depending on which types
/// the instance is constructed to read) some or all non-union responses use the same options as the
/// union wire contract (naming policy, converters, omission of undefined members).
/// </summary>
/// <remarks>
/// Request bodies are read in the encoding their <c>charset</c> declares: UTF-8 (also without a charset) or UTF-16,
/// like ASP.NET Core; any other charset is a 415.
/// </remarks>
public sealed class SystemTextJsonMediaTypeFormatter : MediaTypeFormatter
{
	readonly Func<Type, bool>? canRead;
	readonly bool writeAll;

	/// <param name="options">The base options every read goes through, before a per-request <c>naming</c> override.</param>
	/// <param name="canRead">Which types this formatter binds for both reading and (unless a plain action's declared
	/// return type is itself the union wire, which never reaches a formatter) writing; <see langword="null"/> means
	/// every type.</param>
	/// <param name="writeAll">
	/// Whether this formatter also writes every type, regardless of <paramref name="canRead"/>. Used to reproduce
	/// the old whole-app behaviour; a union response never reaches here either way, since it is written directly
	/// by the wire writer, bypassing every formatter.
	/// </param>
	public SystemTextJsonMediaTypeFormatter(JsonSerializerOptions options, Func<Type, bool>? canRead = null, bool writeAll = false)
	{
		Options = options ?? throw new ArgumentNullException(nameof(options));
		this.canRead = canRead;
		this.writeAll = writeAll;
		SupportedMediaTypes.Add(new MediaTypeHeaderValue(ResponseMediaTypes.Json));
		SupportedMediaTypes.Add(new MediaTypeHeaderValue(ResponseMediaTypes.TextJson));
		SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true));
	}

	/// <summary>The base options every read and write goes through, before a per-request <c>naming</c> override.</summary>
	public JsonSerializerOptions Options { get; }

	public override bool CanReadType(Type type) => canRead?.Invoke(type) ?? true;
	public override bool CanWriteType(Type type) => writeAll || (canRead?.Invoke(type) ?? false);

	public override async Task<object?> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
	{
		var options = OptionsFor(content);
		var encoding = EncodingFor(content);
		var contentLength = content?.Headers.ContentLength;
		if (contentLength == 0)
			return GetDefaultValueForType(type);
		// System.Text.Json reads UTF-8 only: a UTF-16 body is decoded to text first (it is legacy, not worth streaming).
		if (!encoding.Equals(Encoding.UTF8))
		{
			string text;
			using (var reader = new StreamReader(readStream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
				text = await reader.ReadToEndAsync().ConfigureAwait(false);
			if (text.Length == 0)
				return GetDefaultValueForType(type);
			return await DeserializeOrLogAsync(new MemoryStream(Encoding.UTF8.GetBytes(text)), type, options, formatterLogger).ConfigureAwait(false);
		}
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
			return await DeserializeOrLogAsync(buffer, type, options, formatterLogger).ConfigureAwait(false);
		}
		return await DeserializeOrLogAsync(readStream, type, options, formatterLogger).ConfigureAwait(false);
	}

	// BaseJsonMediaTypeFormatter's own behaviour: a malformed body is a model-binding error (reported through
	// IFormatterLogger, which feeds ModelState), not an unhandled 500 - unless there is no logger to report it
	// to, in which case the exception still escapes.
	async Task<object?> DeserializeOrLogAsync(Stream stream, Type type, JsonSerializerOptions options, IFormatterLogger formatterLogger)
	{
		try { return await JsonSerializer.DeserializeAsync(stream, type, options).ConfigureAwait(false); }
		catch (JsonException ex) when (formatterLogger is not null)
		{
			formatterLogger.LogError(string.Empty, ex);
			return GetDefaultValueForType(type);
		}
	}

	public override Task WriteToStreamAsync(Type type, object? value, Stream writeStream, HttpContent content, TransportContext transportContext)
		=> JsonSerializer.SerializeAsync(writeStream, value, type, Options);

	// The Web defaults (camelCase) used to write the problem+json body for a bad 'naming' parameter,
	// matching what AspNetCore's Results.Problem(...) writes for the same case.
	static readonly JsonSerializerOptions ProblemOptions = ResponseNaming.Apply(new JsonSerializerOptions(JsonSerializerDefaults.Web), ResponseNaming.Camel);

	// The encoding the request declares in its charset (UTF-8 when it declares none). One it cannot read is a 415,
	// like ASP.NET Core: the charset is part of the media type the server does not support.
	static Encoding EncodingFor(HttpContent? content)
	{
		var charset = content?.Headers.ContentType?.CharSet;
		if (ResponseMediaTypes.TryGetRequestEncoding(charset, out var encoding)) return encoding;
		var problem = new ResponseProblemDetails
		{
			Status = (int)HttpStatusCode.UnsupportedMediaType,
			Title = "Unsupported Media Type",
			Detail = $"Unsupported charset '{charset}' on the request Content-Type: only UTF-8 and UTF-16 are read."
		};
		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType)
		{
			Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(problem, ProblemOptions)) { Headers = { ContentType = MediaTypeHeaderValue.Parse(ResponseMediaTypes.ProblemJson) } }
		});
	}

	// The request declares its naming on its own Content-Type (spec §3): pick the matching options per request.
	JsonSerializerOptions OptionsFor(HttpContent? content)
	{
		var contentType = content?.Headers.ContentType;
		if (contentType is null) return Options;
		var values = contentType.Parameters
			.Where(p => string.Equals(p.Name, ResponseMediaTypes.NamingParameter, StringComparison.OrdinalIgnoreCase))
			.Select(p => p.Value?.Trim('"'))
			.ToList();
		if (values.Count == 0) return Options;
		if (values.Count > 1 || !ResponseNaming.IsSupported(values[0]))
		{
			var detail = $"Unsupported or duplicated '{ResponseMediaTypes.NamingParameter}' parameter on the request Content-Type: '{string.Join(", ", values)}'.";
			var problem = new ResponseProblemDetails { Status = (int)HttpStatusCode.BadRequest, Title = "Bad Request", Detail = detail };
			throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				// Without charset, like every other body the adapter writes (byte-level parity with AspNetCore).
				Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(problem, ProblemOptions)) { Headers = { ContentType = MediaTypeHeaderValue.Parse(ResponseMediaTypes.ProblemJson) } }
			});
		}
		return ResponseNaming.Apply(Options, values[0]);
	}
}
