namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
using Fuxion;

/// <summary>
/// A Web API 2 formatter backed by System.Text.Json, so request binding and (depending on which types
/// the instance is constructed to read) some or all non-union responses use the same options as the
/// union wire contract (naming policy, converters, omission of undefined members).
/// </summary>
/// <remarks>
/// Request bodies are always decoded as UTF-8: <see cref="System.Text.Json.JsonSerializer"/> reads UTF-8
/// (or UTF-8 with a BOM) only, so a request declaring a different <c>charset</c> parameter is not honoured.
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
		SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
		SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
		SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	/// <summary>The base options every read and write goes through, before a per-request <c>naming</c> override.</summary>
	public JsonSerializerOptions Options { get; }

	public override bool CanReadType(Type type) => canRead?.Invoke(type) ?? true;
	public override bool CanWriteType(Type type) => writeAll || (canRead?.Invoke(type) ?? false);

	public override async Task<object?> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
	{
		var options = OptionsFor(content);
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
			try { return await JsonSerializer.DeserializeAsync(buffer, type, options).ConfigureAwait(false); }
			catch (JsonException ex) when (formatterLogger is not null)
			{
				// BaseJsonMediaTypeFormatter's own behaviour: a malformed body is a model-binding error
				// (reported through IFormatterLogger, which feeds ModelState), not an unhandled 500 -
				// unless there is no logger to report it to, in which case the exception still escapes.
				formatterLogger.LogError(string.Empty, ex);
				return GetDefaultValueForType(type);
			}
		}
		try { return await JsonSerializer.DeserializeAsync(readStream, type, options).ConfigureAwait(false); }
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
				Content = new StringContent(JsonSerializer.Serialize(problem, ProblemOptions), Encoding.UTF8, ResponseMediaTypes.ProblemJson)
			});
		}
		return ResponseNaming.Apply(Options, values[0]);
	}
}
