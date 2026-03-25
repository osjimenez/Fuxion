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
	/// <summary>Key for accessing inner Problem Details from Response extensions.</summary>
	public const string InnerProblemKey = "inner-problem";
	
	/// <summary>Key for accessing JSON content as JsonElement from Response extensions.</summary>
	public const string JsonContentKey = "json-content";
	
	/// <summary>Key for accessing JSON deserialization errors from Response extensions.</summary>
	public const string JsonErrorKey = "json-error";
	
	/// <summary>Key for accessing string content from Response extensions.</summary>
	public const string StringContentKey = "string-content";
	
	/// <summary>Key for accessing payloads from Problem Details extensions.</summary>
	public const string PayloadKey = "payload";
	
	/// <summary>Key for accessing exceptions from Response extensions.</summary>
	public const string ExceptionKey = "exception";
	
	/// <summary>Key for accessing HTTP status codes from Response extensions.</summary>
	public const string StatusCodeKey = "status-code";
	
	/// <summary>Key for accessing reason phrases from Response extensions.</summary>
	public const string ReasonPhraseKey = "reason-phrase";
	
	/// <summary>Key for accessing content length headers from Response extensions.</summary>
	public const string ContentLengthKey = "content-length";
	
	/// <summary>Key for accessing content type headers from Response extensions.</summary>
	public const string ContentTypeKey = "content-type";
	
	/// <summary>Key for accessing file names from Content-Disposition headers.</summary>
	public const string FileNameKey = "file-name";

	// Internal helper that performs the heavy lifting
	static async Task<(List<(string, object?)> Extensions, ResponseProblemDetails? Problem, object? DeserializedBody, Exception? DeserializationException)> DoAsResponse(
		HttpResponseMessage res,
		Type? deserializationType = null,
		JsonSerializerOptions? jsonOptions = null,
		CancellationToken ct = default)
	{
		List<(string, object?)> extensions =
		[
			(StatusCodeKey, (int)res.StatusCode),
			(ReasonPhraseKey, res.ReasonPhrase)
		];
		ResponseProblemDetails? problem = null;
		object? deserializedBody = null;
		Exception? deserializationException = null;

		if (deserializationType is not null && typeof(Stream).IsAssignableFrom(deserializationType))
		{
			extensions.Add((ContentLengthKey, res.Content.Headers.ContentLength));
			extensions.Add((ContentTypeKey, res.Content.Headers.ContentType?.MediaType));
			extensions.Add((FileNameKey, res.Content.Headers.ContentDisposition?.FileName));
			return (extensions, problem, await res.Content.ReadAsStreamAsync(
#if !STANDARD_OR_OLD_FRAMEWORKS
				ct
#endif
			), deserializationException);
		}

		if (deserializationType is not null && typeof(byte[]).IsAssignableFrom(deserializationType))
		{
			extensions.Add((ContentLengthKey, res.Content.Headers.ContentLength));
			extensions.Add((ContentTypeKey, res.Content.Headers.ContentType?.MediaType));
			extensions.Add((FileNameKey, res.Content.Headers.ContentDisposition?.FileName));
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


		if (!strContent.IsNullOrEmpty())
		{

			if (res.Content.Headers.ContentType?.MediaType == "application/problem+json")
			{
				var problemResponse = strContent.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions);
				if (problemResponse.IsSuccess)
				{
					problem = problemResponse.Payload;
					extensions.Add((InnerProblemKey, problem));
				}
			}
			if (problem is null)
			{
				var ele = strContent.Fx.Json.SerializeToElement();
				if (ele.IsError)
					extensions.Add((StringContentKey, strContent));
				extensions.Add(ele.Payload.ValueKind == JsonValueKind.String
					? (StringContentKey, ele.Payload)
					: (JsonContentKey, ele.Payload));
				if (deserializationType is not null)
				{
					var deserializationResponse = strContent.Fx.Json.Deserialize(deserializationType, options: jsonOptions);
					if (deserializationResponse.IsSuccess)
						deserializedBody = deserializationResponse.Payload;
					else
					{
						if (deserializationResponse.Exception is not null)
						{
							deserializationException = deserializationResponse.Exception;
							var tt = deserializationResponse.Exception.Fx.Json.SerializeToElement(options: jsonOptions);
							if (tt.IsSuccess)
								extensions.Add((JsonErrorKey, tt.Payload));
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
			if (extensions.Any(e => e.Item1 == StringContentKey))
				return Response.Get.SuccessMessage(extensions.First(e => e.Item1 == StringContentKey)
					.Item2?.ToString() ?? string.Empty, extensions);
			else
				return Response.Get.Success(extensions);

		var errorType = HttpStatusCodeToErrorType(res.StatusCode);

		return Response.Get
			.ErrorMessage(
				problem?.Detail
				?? extensions.FirstOrDefault(e => e.Item1 == StringContentKey).Item2?.ToString()
				?? $"The response status code is '{(int)res.StatusCode}' and the reason phrase is '{res.ReasonPhrase}'.",
				type: errorType,
				extensions: extensions,
				exception: exception);
	}

	static async Task<Response<TPayload>> AsResponseFromMessageAsync<TPayload>(HttpResponseMessage res, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
	{
		var (extensions, problem, deserializedBody, exception) = await DoAsResponse(res, typeof(TPayload), jsonOptions, ct);

		if (res.IsSuccessStatusCode)
		{
			if (deserializedBody is TPayload payload)
				return Response.Get.SuccessPayload(payload, extensions: extensions);
			if (extensions.Any(e => e.Item1 == JsonContentKey))
				return Response.Get
					.InvalidData($"The content of the response isn't '{typeof(TPayload).GetSignature()}' type.",
						extensions: extensions,
						exception: exception)
					.AsPayload<TPayload>();
			return Response.Get
				.InvalidData("The content of the response isn't a valid json.",
					extensions: extensions,
					exception: exception)
				.AsPayload<TPayload>();
		}
		var errorType = HttpStatusCodeToErrorType(res.StatusCode);

		return Response.Get
			.ErrorMessage(
				problem?.Detail
				?? extensions.FirstOrDefault(e => e.Item1 == StringContentKey).Item2?.ToString()
				?? $"The response status code is '{(int)res.StatusCode}' and the reason phrase is '{res.ReasonPhrase}'.",
				type: errorType,
				extensions: extensions,
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
	/// Extension methods for IResponse to extract Problem Details.
	/// </summary>
	extension(Response me)
	{
		/// <summary>
		/// Attempts to extract RFC 7807 Problem Details from the response extensions.
		/// </summary>
		/// <param name="problem">When this method returns true, contains the Problem Details; otherwise, null.</param>
		/// <returns>true if Problem Details were found; otherwise, false.</returns>
		/// <remarks>
		/// This method looks for the <see cref="InnerProblemKey"/> in the response extensions.
		/// Problem Details are automatically extracted when the response Content-Type is "application/problem+json".
		/// </remarks>
		/// <example>
		/// <code>
		/// var response = await httpClient.GetAsync("/api/endpoint").AsResponseAsync();
		/// if (!response.IsSuccess &amp;&amp; response.TryGetProblemDetails(out var problem))
		/// {
		///     Console.WriteLine($"Problem Type: {problem.Type}");
		///     Console.WriteLine($"Problem Title: {problem.Title}");
		///     Console.WriteLine($"Problem Detail: {problem.Detail}");
		/// }
		/// </code>
		/// </example>
		public bool TryGetProblemDetails([NotNullWhen(true)] out ResponseProblemDetails? problem)
		{
			if (me.Extensions.TryGetValue(InnerProblemKey, out var obj) && obj is ResponseProblemDetails res)
			{
				problem = res;
				return true;
			}
			problem = null;
			return false;
		}
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
	}
}