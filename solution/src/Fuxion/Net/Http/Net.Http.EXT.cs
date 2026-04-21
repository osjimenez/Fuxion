using Fuxion.Collections.Generic;
using Fuxion.Text.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Reflection;

namespace Fuxion.Net.Http;

/// <summary>
/// Provides extension methods for converting <see cref="HttpResponseMessage"/> to Fuxion <see cref="Response"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// This class enables seamless integration between HTTP client operations and Fuxion's Response pattern,
/// providing rich error handling, payload extraction, and RFC 7807 Problem Details support.
/// </para>
/// <para>
/// Key features:
/// </para>
/// <list type="bullet">
/// <item><description>Automatic HTTP status code to ErrorType mapping</description></item>
/// <item><description>RFC 7807 Problem Details deserialization</description></item>
/// <item><description>JSON payload extraction with type safety</description></item>
/// <item><description>Stream and byte array support for file downloads</description></item>
/// <item><description>Rich extension data (status codes, content types, headers)</description></item>
/// </list>
/// </remarks>
public static class Extensions
{
	private const string InnerProblemKey = "inner-problem";
	private const string StatusCodeKey = "status-code";
	private const string PayloadKey = "payload";
	private const string ReasonPhraseKey = "reason-phrase";
	private const string ExceptionKey = "exception";
	private const string JsonContentKey = "json-content";
	private const string JsonErrorKey = "json-error";
	private const string StringContentKey = "string-content";
	private const string ContentLengthKey = "content-length";
	private const string ContentTypeKey = "content-type";
	private const string FileNameKey = "file-name";
	extension(ResponseExtensionsDictionary me)
	{
		/// <summary>
      /// Gets or sets the RFC 7807 problem details extracted from the HTTP response content.
		/// </summary>
     /// <value>
		/// A <see cref="Undefinable{T}"/> containing the inner <see cref="ResponseProblemDetails"/> value,
		/// or <see cref="Undefinable{T}.Undefined"/> when no problem details were captured.
		/// </value>
		/// <remarks>
		/// This property maps to the <c>"inner-problem"</c> extension entry and is typically populated when
		/// the response content type is <c>application/problem+json</c>.
		/// </remarks>
		public Undefinable<ResponseProblemDetails> InnerProblem
		{
			get
				=> me.TryGetValue(InnerProblemKey, out var val)
					? val switch
					{
						Undefinable<ResponseProblemDetails> und => und,
						ResponseProblemDetails res => res,
						_ => Undefinable<ResponseProblemDetails>.Undefined
					}
					: Undefinable<ResponseProblemDetails>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(InnerProblemKey);
				else
					me[InnerProblemKey] = value;
			}
		}
		
		/// <summary>
      /// Gets or sets the HTTP status code associated with the response.
		/// </summary>
     /// <value>
		/// A <see cref="Undefinable{T}"/> containing the numeric HTTP status code,
		/// or <see cref="Undefinable{T}.Undefined"/> when it is not available.
		/// </value>
		public Undefinable<int> StatusCode
		{
			get
				=> me.TryGetValue(StatusCodeKey, out var val)
					? val switch
					{
						Undefinable<int> und => und,
						int res => res,
						_ => Undefinable<int>.Undefined
					}
					: Undefinable<int>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(StatusCodeKey);
				else
					me[StatusCodeKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the deserialized payload extracted from the HTTP response.
		/// </summary>
     /// <value>
		/// A <see cref="Undefinable{T}"/> containing the payload object,
		/// or <see cref="Undefinable{T}.Undefined"/> when no payload was extracted.
		/// </value>
		/// <remarks>
		/// This property provides typed access to the <c>"payload"</c> extension entry.
		/// It can contain any deserialized object captured during response processing.
		/// </remarks>
		public Undefinable<object> Payload
		{
			get
				=> me.TryGetValue(PayloadKey, out var val)
					? val switch
					{
						Undefinable<object> und => und,
						not null => val,
						_ => Undefinable<object>.Undefined
					}
					: Undefinable<object>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(PayloadKey);
				else
					me[PayloadKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the HTTP reason phrase associated with the response.
		/// </summary>
      /// <value>
		/// A <see cref="Undefinable{T}"/> containing the reason phrase,
		/// or <see cref="Undefinable{T}.Undefined"/> when it is not available.
		/// </value>
		public Undefinable<string> ReasonPhrase
		{
			get
				=> me.TryGetValue(ReasonPhraseKey, out var val)
					? val switch
					{
						Undefinable<string> und => und,
						string res => res,
						_ => Undefinable<string>.Undefined
					}
					: Undefinable<string>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(ReasonPhraseKey);
				else
					me[ReasonPhraseKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the serialized exception details produced while processing or deserializing the HTTP response.
		/// </summary>
    /// <value>
		/// A <see cref="Undefinable{T}"/> containing a <see cref="JsonElement"/> with exception information,
		/// or <see cref="Undefinable{T}.Undefined"/> when no exception details are present.
		/// </value>
		public Undefinable<JsonElement> Exception
		{
			get
				=> me.TryGetValue(ExceptionKey, out var val)
					? val switch
					{
						Undefinable<JsonElement> und => und,
						JsonElement res => res,
						_ => Undefinable<JsonElement>.Undefined
					}
					: Undefinable<JsonElement>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(ExceptionKey);
				else
					me[ExceptionKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the JSON content extracted from the HTTP response body.
		/// </summary>
     /// <value>
		/// A <see cref="Undefinable{T}"/> containing the parsed JSON content as a <see cref="JsonElement"/>,
		/// or <see cref="Undefinable{T}.Undefined"/> when the body is not valid JSON or no JSON content was captured.
		/// </value>
		public Undefinable<JsonElement> JsonContent
		{
			get
				=> me.TryGetValue(JsonContentKey, out var val)
					? val switch
					{
						Undefinable<JsonElement> und => und,
						JsonElement res => res,
						_ => Undefinable<JsonElement>.Undefined
					}
					: Undefinable<JsonElement>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(JsonContentKey);
				else
					me[JsonContentKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the JSON error content generated while attempting to deserialize the HTTP response body.
		/// </summary>
    /// <value>
		/// A <see cref="Undefinable{T}"/> containing the serialized deserialization error as a <see cref="JsonElement"/>,
		/// or <see cref="Undefinable{T}.Undefined"/> when no JSON error information is available.
		/// </value>
		public Undefinable<JsonElement> JsonError
		{
			get
				=> me.TryGetValue(JsonErrorKey, out var val)
					? val switch
					{
						Undefinable<JsonElement> und => und,
						JsonElement res => res,
						_ => Undefinable<JsonElement>.Undefined
					}
					: Undefinable<JsonElement>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(JsonErrorKey);
				else
					me[JsonErrorKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the string content extracted from the HTTP response body.
		/// </summary>
     /// <value>
		/// A <see cref="Undefinable{T}"/> containing the response body as plain text,
		/// or <see cref="Undefinable{T}.Undefined"/> when no string content was captured.
		/// </value>
		/// <remarks>
		/// This property is typically used when the response body is not valid JSON or when the JSON payload itself is a string value.
		/// </remarks>
		public Undefinable<string> StringContent
		{
			get
				=> me.TryGetValue(StringContentKey, out var val)
					? val switch
					{
						Undefinable<string> und => und,
						string res => res,
						_ => Undefinable<string>.Undefined
					}
					: Undefinable<string>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(StringContentKey);
				else
					me[StringContentKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the content length reported by the HTTP response.
		/// </summary>
    /// <value>
		/// A <see cref="Undefinable{T}"/> containing the content length in bytes,
		/// or <see cref="Undefinable{T}.Undefined"/> when the header is not present.
		/// </value>
		public Undefinable<long> ContentLength
		{
			get
				=> me.TryGetValue(ContentLengthKey, out var val)
					? val switch
					{
						Undefinable<long> und => und,
						long res => res,
						_ => Undefinable<long>.Undefined
					}
					: Undefinable<long>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(ContentLengthKey);
				else
					me[ContentLengthKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the media type reported by the HTTP response content.
		/// </summary>
    /// <value>
		/// A <see cref="Undefinable{T}"/> containing the content type media value,
		/// or <see cref="Undefinable{T}.Undefined"/> when the header is not present.
		/// </value>
		public Undefinable<string> ContentType
		{
			get
				=> me.TryGetValue(ContentTypeKey, out var val)
					? val switch
					{
						Undefinable<string> und => und,
						string res => res,
						_ => Undefinable<string>.Undefined
					}
					: Undefinable<string>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(ContentTypeKey);
				else
					me[ContentTypeKey] = value;
			}
		}

		/// <summary>
      /// Gets or sets the file name reported by the HTTP content disposition header.
		/// </summary>
    /// <value>
		/// A <see cref="Undefinable{T}"/> containing the file name,
		/// or <see cref="Undefinable{T}.Undefined"/> when the response does not provide one.
		/// </value>
		/// <remarks>
		/// This property is mainly useful for download scenarios where the server includes a suggested file name.
		/// </remarks>
		public Undefinable<string> FileName
		{
			get
				=> me.TryGetValue(FileNameKey, out var val)
					? val switch
					{
						Undefinable<string> und => und,
						string res => res,
						_ => Undefinable<string>.Undefined
					}
					: Undefinable<string>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(FileNameKey);
				else
					me[FileNameKey] = value;
			}
		}
	}

	// Internal helper that performs the heavy lifting
	static async Task<(ResponseExtensionsDictionary Extensions, ResponseProblemDetails? Problem, object? DeserializedBody, Exception? DeserializationException)> DoAsResponse(
		HttpResponseMessage res,
		Type? deserializationType = null,
		JsonSerializerOptions? jsonOptions = null,
		CancellationToken ct = default)
	{
		ResponseExtensionsDictionary extensions = new()
		{
			StatusCode = (int)res.StatusCode,
			ReasonPhrase = res.ReasonPhrase is null ? Undefinable<string>.Undefined : res.ReasonPhrase
		};
		ResponseProblemDetails? problem = null;
		object? deserializedBody = null;
		Exception? deserializationException = null;

		if (deserializationType is not null && typeof(Stream).IsAssignableFrom(deserializationType))
		{
			extensions.ContentLength = res.Content.Headers.ContentLength is null // INFO Cannot use ?? because we need implicit conversion from long to Undefinable<long>
				? Undefinable<long>.Undefined
				: res.Content.Headers.ContentLength.Value;
			extensions.ContentType = res.Content.Headers.ContentType?.MediaType is null // INFO Cannot use ?? because we need implicit conversion from string to Undefinable<string>
				? Undefinable<string>.Undefined
				: res.Content.Headers.ContentType.MediaType;
			extensions.FileName = res.Content.Headers.ContentDisposition?.FileName is null // INFO Cannot use ?? because we need implicit conversion from string to Undefinable<string>
				? Undefinable<string>.Undefined
				: res.Content.Headers.ContentDisposition.FileName;
			return (extensions, problem, await res.Content.ReadAsStreamAsync(
#if !STANDARD_OR_OLD_FRAMEWORKS
				ct
#endif
			), deserializationException);
		}

		if (deserializationType is not null && typeof(byte[]).IsAssignableFrom(deserializationType))
		{
			extensions.ContentLength = res.Content.Headers.ContentLength is null // INFO Cannot use ?? because we need implicit conversion from long to Undefinable<long>
				? Undefinable<long>.Undefined
				: res.Content.Headers.ContentLength.Value;
			extensions.ContentType = res.Content.Headers.ContentType?.MediaType is null // INFO Cannot use ?? because we need implicit conversion from string to Undefinable<string>
				? Undefinable<string>.Undefined
				: res.Content.Headers.ContentType.MediaType;
			extensions.FileName = res.Content.Headers.ContentDisposition?.FileName is null // INFO Cannot use ?? because we need implicit conversion from string to Undefinable<string>
				? Undefinable<string>.Undefined
				: res.Content.Headers.ContentDisposition.FileName;
			return (extensions, problem, await res.Content.ReadAsByteArrayAsync(
#if !STANDARD_OR_OLD_FRAMEWORKS
				ct
#endif
			), deserializationException);
		}

		var strContent = await res.Content.ReadAsStringAsync(
#if !STANDARD_OR_OLD_FRAMEWORKS
			ct
#endif
		);


		if (strContent.IsNeitherNullNorWhiteSpace())
		{

			if (res.Content.Headers.ContentType?.MediaType == "application/problem+json")
			{
				var deserializationProblemRes = strContent.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions);
				if (deserializationProblemRes.IsSuccess)
				{
					problem = deserializationProblemRes.Payload;
					extensions.InnerProblem = problem;
				}
			}
			if (problem is null)
			{
				try
				{
					var element = JsonElement.Parse(strContent);
					if (element.ValueKind == JsonValueKind.String)
						extensions.StringContent = element.GetString() == null
							? Undefinable<string>.Undefined
							: element.ToString();
					else
						extensions.JsonContent = element;
				}
				catch 
				{
					extensions.StringContent = strContent;
				}
				if (deserializationType is not null)
				{
					var deserializationRes = strContent.Fx.Json.Deserialize(deserializationType, options: jsonOptions);
					if (deserializationRes.IsSuccess)
						deserializedBody = deserializationRes.Payload;
					else
					{
						if (deserializationRes.Exception is not null)
						{
							deserializationException = deserializationRes.Exception;
							var jsonErrorExceptionSerializationRes = deserializationRes.Exception.Fx.Json.SerializeToElement(options: jsonOptions);
							if (jsonErrorExceptionSerializationRes.IsSuccess)
								extensions.JsonError = jsonErrorExceptionSerializationRes.Payload;
						}
					}
				}
			}
		}
		return (extensions, problem, deserializedBody, deserializationException);
	}

	// Core wrappers to produce Response objects from an HttpResponseMessage
	static async Task<Response> AsResponseFromMessageAsync(HttpResponseMessage res, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
	{
		var (extensions, problem, _, exception) = await DoAsResponse(res, null, jsonOptions, ct);

		if (res.IsSuccessStatusCode)
			if (extensions.Any(e => e.Key == StringContentKey))
				return Response.Get.SuccessMessage(extensions.First(e => e.Key == StringContentKey)
					.Value?.ToString() ?? string.Empty, extensions.ToEnumerable());
			else
				return Response.Get.Success(extensions.ToEnumerable());

		var errorType = HttpStatusCodeToErrorType(res.StatusCode);

		return Response.Get
			.ErrorMessage(
				problem?.Detail
				?? extensions.FirstOrDefault(e => e.Key == StringContentKey).Value?.ToString()
				?? $"The response status code is '{(int)res.StatusCode}' and the reason phrase is '{res.ReasonPhrase}'.",
				type: errorType,
				extensions: extensions.ToEnumerable(),
				exception: exception);
	}

	static async Task<Response<TPayload>> AsResponseFromMessageAsync<TPayload>(HttpResponseMessage res, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
	{
		var (extensions, problem, deserializedBody, exception) = await DoAsResponse(res, typeof(TPayload), jsonOptions, ct);

		if (res.IsSuccessStatusCode)
		{
			if (deserializedBody is TPayload payload)
				return Response.Get.SuccessPayload(payload, extensions: extensions.ToEnumerable());
			if (extensions.JsonContent.IsDefined)
				return Response.Get
					.InvalidData($"The content of the response isn't '{typeof(TPayload).GetSignature()}' type.",
						extensions: extensions.ToEnumerable(),
						exception: exception)
					.AsPayload<TPayload>();
			return Response.Get
				.InvalidData("The content of the response isn't a valid json.",
					extensions: extensions.ToEnumerable(),
					exception: exception)
				.AsPayload<TPayload>();
		}
		var errorType = HttpStatusCodeToErrorType(res.StatusCode);

		return Response.Get
			.ErrorMessage(
				problem?.Detail
				?? extensions.FirstOrDefault(e => e.Key == StringContentKey).Value?.ToString()
				?? $"The response status code is '{(int)res.StatusCode}' and the reason phrase is '{res.ReasonPhrase}'.",
				type: errorType,
				extensions: extensions.ToEnumerable(),
				exception: exception)
			.AsPayload<TPayload>();
	}

	/// <summary>
	/// Maps HTTP status codes to Fuxion ErrorType values.
	/// </summary>
	/// <param name="statusCode">The HTTP status code to map.</param>
	/// <returns>The corresponding <see cref="ErrorType"/>.</returns>
	/// <remarks>
	/// This method provides comprehensive mapping for all standard HTTP 4xx and 5xx status codes
	/// based on their semantic meaning. The mappings follow RFC 7231 and related HTTP specifications.
	/// </remarks>
	static ErrorType HttpStatusCodeToErrorType(HttpStatusCode statusCode)
		=> statusCode switch
		{
			// 400 - Indicates that the request could not be understood by the server. BadRequest is sent when no other error is applicable, or if the exact error is unknown or does not have its own error code.
			HttpStatusCode.BadRequest => ErrorType.InvalidData,
			// 401 - Indicates that the requested resource requires authentication. The WWW-Authenticate header contains the details of how to perform the authentication.
			HttpStatusCode.Unauthorized => ErrorType.PermissionDenied,
			// 402 - Is reserved for future use.
			HttpStatusCode.PaymentRequired => ErrorType.PermissionDenied,
			// 403 - Indicates that the server refuses to fulfill the request.
			HttpStatusCode.Forbidden => ErrorType.PermissionDenied,
			// 404 - Indicates that the requested resource does not exist on the server.
			HttpStatusCode.NotFound => ErrorType.NotFound,
			// 405 - Indicates that the request method (POST or GET) is not allowed on the requested resource.
			HttpStatusCode.MethodNotAllowed => ErrorType.Unavailable,
			// 406 - Indicates that the client has indicated with Accept headers that it will not accept any of the available representations of the resource.
			HttpStatusCode.NotAcceptable => ErrorType.InvalidData,
			// 407 - Indicates that the requested proxy requires authentication. The Proxy-authenticate header contains the details of how to perform the authentication.
			HttpStatusCode.ProxyAuthenticationRequired => ErrorType.PermissionDenied,
			// 408 - Indicates that the client did not send a request within the time the server was expecting the request.
			HttpStatusCode.RequestTimeout => ErrorType.Timeout,
			// 409 - Indicates that the request could not be carried out because of a conflict on the server.
			HttpStatusCode.Conflict => ErrorType.Conflict,
			// 410 - Indicates that the requested resource is no longer available.
			HttpStatusCode.Gone => ErrorType.Unavailable,
			// 411 - Indicates that the required Content-length header is missing.
			HttpStatusCode.LengthRequired => ErrorType.InvalidData,
			// 412 - Indicates that a condition set for this request failed, and the request cannot be carried out. Conditions are set with conditional request headers like If-Match, If-None-Match, or If-Unmodified-Since.
			HttpStatusCode.PreconditionFailed => ErrorType.InvalidData,
			// 413 - Indicates that the request is too large for the server to process.
			HttpStatusCode.RequestEntityTooLarge => ErrorType.InvalidData,
			// 414 - Indicates that the URI is too long.
			HttpStatusCode.RequestUriTooLong => ErrorType.NotSupported,
			// 415 - Indicates that the request is an unsupported type.
			HttpStatusCode.UnsupportedMediaType => ErrorType.NotSupported,
			// 416 - Indicates that the range of data requested from the resource cannot be returned, either because the beginning of the range is before the beginning of the resource, or the end of the range is after the end of the resource.
			HttpStatusCode.RequestedRangeNotSatisfiable => ErrorType.InvalidData,
			// 417 - Indicates that an expectation given in an Expect header could not be met by the server.
			HttpStatusCode.ExpectationFailed => ErrorType.InvalidData,
#if !STANDARD_OR_OLD_FRAMEWORKS
			// 421 - Indicates that the request was directed at a server that is not able to produce a response.
			HttpStatusCode.MisdirectedRequest => ErrorType.Unavailable,
			// 422 - Indicates that the request was well-formed but was unable to be followed due to semantic errors.
			// UnprocessableContent is a synonym for UnprocessableEntity.
			//HttpStatusCode.UnprocessableContent => ErrorType.Unavailable,
			// 422 - Indicates that the request was well-formed but was unable to be followed due to semantic errors.
			// UnprocessableEntity is a synonym for UnprocessableContent.
			HttpStatusCode.UnprocessableEntity => ErrorType.Unavailable,
			// 423 - Indicates that the source or destination resource is locked.
			HttpStatusCode.Locked => ErrorType.Unavailable,
			// 424 - Indicates that the method couldn't be performed on the resource because the requested action depended on another action and that action failed.
			HttpStatusCode.FailedDependency => ErrorType.Conflict,
#endif
			// 426 - Indicates that the client should switch to a different protocol such as TLS/1.0.
			HttpStatusCode.UpgradeRequired => ErrorType.NotSupported,
#if !STANDARD_OR_OLD_FRAMEWORKS
			// 428 - Indicates that the server requires the request to be conditional.
			HttpStatusCode.PreconditionRequired => ErrorType.InvalidData,
			// 429 - Indicates that the user has sent too many requests in a given amount of time.
			HttpStatusCode.TooManyRequests => ErrorType.Unavailable,
			// 431 - Indicates that the server is unwilling to process the request because its header fields (either an individual header field or all the header fields collectively) are too large.
			HttpStatusCode.RequestHeaderFieldsTooLarge => ErrorType.Unavailable,
			// 451 - Indicates that the server is denying access to the resource as a consequence of a legal demand.
			HttpStatusCode.UnavailableForLegalReasons => ErrorType.PermissionDenied,
#endif
			// 500 - Indicates that a generic error has occurred on the server.
			HttpStatusCode.InternalServerError => ErrorType.Critical,
			// 501 - Indicates that the server does not support the requested function.
			HttpStatusCode.NotImplemented => ErrorType.Unavailable,
			// 502 - Indicates that an intermediate proxy server received a bad response from another proxy or the origin server.
			HttpStatusCode.BadGateway => ErrorType.Unavailable,
			// 503 - Indicates that the server is temporarily unavailable, usually due to high load or maintenance.
			HttpStatusCode.ServiceUnavailable => ErrorType.Unavailable,
			// 504 - Indicates that an intermediate proxy server timed out while waiting for a response from another proxy or the origin server.
			HttpStatusCode.GatewayTimeout => ErrorType.Timeout,
			// 505 - Indicates that the requested HTTP version is not supported by the server.
			HttpStatusCode.HttpVersionNotSupported => ErrorType.NotSupported,
#if !STANDARD_OR_OLD_FRAMEWORKS
			// 506 - Indicates that the chosen variant resource is configured to engage in transparent content negotiation itself and, therefore, isn't a proper endpoint in the negotiation process.
			HttpStatusCode.VariantAlsoNegotiates => ErrorType.NotSupported,
			// 507 - Indicates that the server is unable to store the representation needed to complete the request.
			HttpStatusCode.InsufficientStorage => ErrorType.Unavailable,
			// 508 - Indicates that the server terminated an operation because it encountered an infinite loop while processing a WebDAV request with "Depth: infinity". This status code is meant for backward compatibility with clients not aware of the 208 status code <see cref="F:System.Net.HttpStatusCode.AlreadyReported" /> appearing in multistatus response bodies.
			HttpStatusCode.LoopDetected => ErrorType.Critical,
			// 510 - Indicates that further extensions to the request are required for the server to fulfill it.
			HttpStatusCode.NotExtended => ErrorType.NotSupported,
			// 511 - Indicates that the client needs to authenticate to gain network access; it's intended for use by intercepting proxies used to control access to the network.
			HttpStatusCode.NetworkAuthenticationRequired => ErrorType.PermissionDenied,
#endif
			var _ => ErrorType.Critical
		};

	/// <summary>
	/// Extension methods for Task&lt;HttpResponseMessage&gt; to convert to Response objects.
	/// </summary>
	extension(Task<HttpResponseMessage> me)
	{
		/// <summary>
		/// Converts an HTTP response to a Fuxion Response object asynchronously.
		/// </summary>
		/// <param name="jsonOptions">Optional JSON serialization options.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A Response object containing success/error information and extensions with HTTP metadata.</returns>
		/// <remarks>
		/// This overload doesn't deserialize the response body into a typed payload.
		/// Use the generic overload if you need typed payload extraction.
		/// </remarks>
		/// <example>
		/// <code>
		/// var response = await httpClient.GetAsync("/api/endpoint").AsResponseAsync();
		/// if (response.IsSuccess)
		/// {
		///     // Access status code
		///     var statusCode = response.Extensions[Extensions.StatusCodeKey];
		/// }
		/// </code>
		/// </example>
		public async Task<Response> AsResponseAsync(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			=> await Extensions.AsResponseFromMessageAsync(await me, jsonOptions, ct);

		/// <summary>
		/// Converts an HTTP response to a Fuxion Response&lt;TPayload&gt; object asynchronously with automatic JSON deserialization.
		/// </summary>
		/// <typeparam name="TPayload">The type to deserialize the response body into. Can be Stream or byte[] for binary content.</typeparam>
		/// <param name="jsonOptions">Optional JSON serialization options.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A Response&lt;TPayload&gt; object containing the deserialized payload on success or error information.</returns>
		/// <remarks>
		/// <para>Special handling for specific payload types:</para>
		/// <list type="bullet">
		/// <item><description>Stream: Returns the response stream directly (useful for file downloads)</description></item>
		/// <item><description>byte[]: Returns the response bytes directly</description></item>
		/// <item><description>Other types: Attempts JSON deserialization</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// // JSON payload
		/// var response = await httpClient.GetAsync("/api/users/1")
		///     .AsResponseAsync&lt;User&gt;();
		/// if (response.IsSuccess)
		/// {
		///     var user = response.Payload;
		///     Console.WriteLine(user.Name);
		/// }
		/// 
		/// // File download
		/// var fileResponse = await httpClient.GetAsync("/api/files/download")
		///     .AsResponseAsync&lt;Stream&gt;();
		/// if (fileResponse.IsSuccess)
		/// {
		///     await using var stream = fileResponse.Payload;
		///     // Save stream to file...
		/// }
		/// </code>
		/// </example>
		public async Task<Response<TPayload>> AsResponseAsync<TPayload>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			=> await Extensions.AsResponseFromMessageAsync<TPayload>(await me, jsonOptions, ct);
	}

	/// <summary>
	/// Extension methods for HttpResponseMessage to convert to Response objects.
	/// </summary>
	extension(HttpResponseMessage res)
	{
		/// <summary>
		/// Converts this HTTP response to a Fuxion Response object asynchronously.
		/// </summary>
		/// <param name="jsonOptions">Optional JSON serialization options.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A Response object containing success/error information and extensions with HTTP metadata.</returns>
		/// <example>
		/// <code>
		/// HttpResponseMessage httpResponse = await httpClient.GetAsync("/api/endpoint");
		/// var response = await httpResponse.AsResponseAsync();
		/// </code>
		/// </example>
		public async Task<Response> AsResponseAsync(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			=> await Extensions.AsResponseFromMessageAsync(res, jsonOptions, ct);

		/// <summary>
		/// Converts this HTTP response to a Fuxion Response&lt;TPayload&gt; object asynchronously with automatic JSON deserialization.
		/// </summary>
		/// <typeparam name="TPayload">The type to deserialize the response body into.</typeparam>
		/// <param name="jsonOptions">Optional JSON serialization options.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A Response&lt;TPayload&gt; object containing the deserialized payload on success or error information.</returns>
		/// <example>
		/// <code>
		/// HttpResponseMessage httpResponse = await httpClient.PostAsync("/api/users", content);
		/// var response = await httpResponse.AsResponseAsync&lt;User&gt;();
		/// </code>
		/// </example>
		public async Task<Response<TPayload>> AsResponseAsync<TPayload>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
			=> await Extensions.AsResponseFromMessageAsync<TPayload>(res, jsonOptions, ct);
	}

	/// <summary>
	/// Extension methods for ResponseProblemDetails to extract typed payloads.
	/// </summary>
	extension(ResponseProblemDetails problem)
	{
		/// <summary>
		/// Attempts to extract and deserialize a typed payload from Problem Details extensions.
		/// </summary>
		/// <typeparam name="TPayload">The type to deserialize the payload into.</typeparam>
		/// <param name="payload">When this method returns true, contains the deserialized payload; otherwise, default.</param>
		/// <param name="jsonOptions">Optional JSON serialization options.</param>
		/// <returns>true if a payload was successfully extracted and deserialized; otherwise, false.</returns>
		/// <remarks>
		/// This method looks for the <see cref="PayloadKey"/> in the Problem Details extensions
		/// and attempts to deserialize it as a JsonElement to the specified type.
		/// </remarks>
		/// <example>
		/// <code>
		/// if (response.TryGetProblemDetails(out var problem))
		/// {
		///     if (problem.TryGetPayload&lt;ValidationErrors&gt;(out var errors))
		///     {
		///         foreach (var error in errors.Errors)
		///         {
		///             Console.WriteLine($"Field {error.Field}: {error.Message}");
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		public bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload, JsonSerializerOptions? jsonOptions = null)
		{
			if (problem.Extensions.TryGetValue(PayloadKey, out var obj) && obj is JsonElement jsonElement)
			{
				try
				{
					payload = jsonElement.Deserialize<TPayload>(jsonOptions);
					if (payload is not null) return true;
				}
				catch
				{
					// ignored
				}
			}
			payload = default;
			return false;
		}
		/// <summary>
		/// PEND DOC
		/// </summary>
		/// <typeparam name="TPayload"></typeparam>
		/// <param name="jsonOptions"></param>
		/// <returns></returns>
		public TPayload? PayloadOrDefault<TPayload>(JsonSerializerOptions? jsonOptions = null)
			=> problem.TryGetPayload<TPayload>(out var payload, jsonOptions) ? payload : default;

		/// <summary>
		/// PEND DOC
		/// </summary>
		/// <typeparam name="TPayload"></typeparam>
		/// <param name="fallback"></param>
		/// <param name="jsonOptions"></param>
		/// <returns></returns>
		public TPayload PayloadOrFallback<TPayload>(Func<ResponseProblemDetails, TPayload> fallback, JsonSerializerOptions? jsonOptions = null) 
			=> problem.TryGetPayload<TPayload>(out var payload, jsonOptions) ? payload : fallback(problem);
	}
}