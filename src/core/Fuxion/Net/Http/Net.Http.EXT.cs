
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fuxion.Net.Http;

public static class Extensions
{
	public const string InnerProblemKey = "inner-problem";
	public const string JsonContentKey = "json-content";
	public const string StringContentKey = "string-content";
	public const string PayloadKey = "payload";
	public const string ExceptionKey = "exception";
	public const string StatusCodeKey = "status-code";
	public const string ReasonPhraseKey = "reason-phrase";
	static async Task<(
		List<(string, object?)> Extensions,
		ResponseProblemDetails? Problem,
		object? deserializedBody)> DoAsResponse(
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
		var strContent = await res.Content.ReadAsStringAsync(
#if !NETSTANDARD2_0 && !NET472
				ct
#endif
		);
		if (!strContent.IsNullOrEmpty())
		{
			if (res.Content.Headers.ContentType?.MediaType == "application/problem+json")
			{
				try
				{
					problem = strContent.DeserializeFromJson<ResponseProblemDetails>(options: jsonOptions);
					if (problem is not null) extensions.Add((InnerProblemKey, problem));
				} catch
				{
					// ignored
				}
			}
			if (problem is null)
			{
				if (deserializationType is not null)
				{
					try
					{
						deserializedBody = JsonSerializer.Deserialize(strContent, deserializationType, options: jsonOptions);
					} catch
					{
						// ignored
					}
				}
				if (deserializedBody is null)
				{
					try
					{
						var jsonContent = JsonNode.Parse(strContent);
						if (jsonContent is not null) extensions.Add((JsonContentKey, jsonContent));
					} catch
					{
						extensions.Add((StringContentKey, strContent));
					}
				}
			}
		}
		return (extensions, problem, deserializedBody);
	}
	public static async Task<Response> AsResponseAsync(this Task<HttpResponseMessage> me, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
		=> await AsResponseAsync(await me, jsonOptions, ct);
	public static async Task<Response> AsResponseAsync(this HttpResponseMessage res, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
	{
		var (extensions, problem, _) = await DoAsResponse(res, null, jsonOptions, ct);

		if (res.IsSuccessStatusCode)
			if (extensions.Any(e => e.Item1 == StringContentKey))
				return Response.Get.Success(
					message: extensions.First(e => e.Item1 == StringContentKey).Item2?.ToString(),
					extensions: extensions);
			else
				return Response.Get.Success(extensions: extensions);

		var errorType = HttpStatusCodeToErrorType(res.StatusCode);

		return Response.Get.Error(
			problem?.Detail ?? $"The response status code is '{(int)res.StatusCode}' and the reason phrase is '{res.ReasonPhrase}'.",
			type: errorType,
			extensions: extensions);
	}
	public static async Task<Response<TPayload>> AsResponseAsync<TPayload>(this Task<HttpResponseMessage> me, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
		=> await AsResponseAsync<TPayload>(await me, jsonOptions, ct);
	public static async Task<Response<TPayload>> AsResponseAsync<TPayload>(this HttpResponseMessage res, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
	{
		var (extensions, problem, deserializedBody) = await DoAsResponse(res, typeof(TPayload), jsonOptions, ct);

		if (res.IsSuccessStatusCode)
		{
			if (deserializedBody is TPayload payload)
				return Response.Get.Success(payload, extensions: extensions);
			if (extensions.Any(e => e.Item1 == JsonContentKey))
				return Response.Get.Error.InvalidData($"The content of the response isn't '{typeof(TPayload).GetSignature()}' type.", extensions: extensions);
			return Response.Get.Error.InvalidData($"The content of the response isn't a valid json.", extensions: extensions);
		}
		var errorType = HttpStatusCodeToErrorType(res.StatusCode);

		return Response.Get.Error(
			problem?.Detail ?? $"The response status code is '{(int)res.StatusCode}' and the reason phrase is '{res.ReasonPhrase}'.",
			type: errorType,
			extensions: extensions);
	}
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
#if !NETSTANDARD2_0 && !NET472
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
#if !NETSTANDARD2_0 && !NET472
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
#if !NETSTANDARD2_0 && !NET472
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
	public static bool TryGetProblemDetails<TPayload>(this Response<TPayload> me, [NotNullWhen(true)] out ResponseProblemDetails? problem)
	{
		if (me.Extensions.TryGetValue(InnerProblemKey, out var obj) && obj is ResponseProblemDetails res)
		{
			problem = res;
			return true;
		}
		problem = null;
		return false;
	}
	public static bool TryGetPayload<TPayload>(this ResponseProblemDetails problem, [NotNullWhen(true)] out TPayload? payload, JsonSerializerOptions? jsonOptions = null)
	{
		if (problem.Extensions.TryGetValue(PayloadKey, out var obj) && obj is JsonElement jsonElement)
		{
			try
			{
				payload = jsonElement.Deserialize<TPayload>(jsonOptions);
				if (payload is not null)
					return true;
			} catch
			{
				// ignored
			}
		}
		payload = default;
		return false;
	}
}