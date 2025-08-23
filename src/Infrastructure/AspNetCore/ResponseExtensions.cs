using System.Net;
using System.Text.Json;
using Fuxion.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using static Fuxion.Net.Http.Extensions;

namespace Fuxion.AspNetCore;

public static class ResponseExtensions
{
	public static bool IncludeException { get; set; } = true;

	public static async Task<IResult> ToApiFileStreamResultAsync<TPayload>(
		this Task<IResponse<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : Stream
		=> ToApiResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IResult> ToApiFileStreamResultAsync<TPayload>(
		this Task<Response<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : Stream
		=> ToApiResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IResult> ToApiFileBytesResultAsync<TPayload>(
		this Task<IResponse<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : IEnumerable<byte>
		=> ToApiResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IResult> ToApiFileBytesResultAsync<TPayload>(
		this Task<Response<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : IEnumerable<byte>
		=> ToApiResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static IResult ToApiFileStreamResult<TPayload>(
		this IResponse<TPayload> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : Stream
		=> ToApiResult(me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static IResult ToApiFileBytesResult<TPayload>(
		this IResponse<TPayload> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : IEnumerable<byte>
		=> ToApiResult(me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IResult> ToApiResultAsync(this Task<IResponse> me, bool fullSerialization = false)
		=> ToApiResult(await me, fullSerialization);
	public static async Task<IResult> ToApiResultAsync(this Task<Response> me, bool fullSerialization = false)
		=> ToApiResult(await me, fullSerialization);
	public static async Task<IResult> ToApiResultAsync<TPayload>(this Task<IResponse<TPayload>> me)
		=> ToApiResult(await me);
	public static async Task<IResult> ToApiResultAsync<TPayload>(this Task<Response<TPayload>> me)
		=> ToApiResult(await me);
	public static IResult ToApiResult(this IResponse me, bool fullSerialization = false)
		=> ToApiResult(me, null, null, null, null, false, fullSerialization);
	public static IResult ToApiResult<TPayload>(this IResponse<TPayload> me) => ToApiResult(me, null, null, null, null, false, false);

	static IResult ToApiResult(
		this IResponse me,
		string? contentType,
		string? fileDownloadName,
		DateTimeOffset? lastModified,
		EntityTagHeaderValue? entityTag,
		bool enableRangeProcessing,
		bool fullSerialization)
	{
		if (me.IsSuccess)
			if (me is IResponse<object?> { Payload: not null } me2)
				if (me2.Payload is Stream stream)
					return Results.File(stream, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing);
				else if(me2.Payload is IEnumerable<byte> bytes)
					return Results.File(bytes.ToArray(), contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag);
				else
					return Results.Ok(fullSerialization ? me2 : me2.Payload);
			else if (me.Message is not null)
				return Results.Content(me.Message);
			else
				return Results.NoContent();

		var extensions = me.Extensions.ToDictionary();
		extensions.Remove(StatusCodeKey);
		extensions.Remove(ReasonPhraseKey);

		if (me is IResponse<object?> { Payload: not null and not Stream } me3) extensions[PayloadKey] = me3.Payload;
		if (IncludeException && me.Exception is not null)
			extensions[ExceptionKey] = JsonSerializer.SerializeToElement(me.Exception, options: new()
			{
				Converters =
				{
					new ExceptionConverter()
				}
			});

		return me.ErrorType switch
		{
			ErrorType.NotFound => Results.Problem(me.Message, statusCode: StatusCodes.Status404NotFound, title: "Not found", extensions: extensions),
			ErrorType.PermissionDenied => Results.Problem(me.Message, statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", extensions: extensions),
			ErrorType.InvalidData => Results.Problem(me.Message, statusCode: StatusCodes.Status400BadRequest, title: "Bad request", extensions: extensions),
			ErrorType.Conflict => Results.Problem(me.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict", extensions: extensions),
			ErrorType.Critical => Results.Problem(me.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error", extensions: extensions),
			ErrorType.NotSupported => Results.Problem(me.Message, statusCode: StatusCodes.Status501NotImplemented, title: "Not implemented", extensions: extensions),
			ErrorType.Unavailable => Results.Problem(me.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Service unavailable", extensions: extensions),
			ErrorType.Timeout => Results.Problem(me.Message, statusCode: StatusCodes.Status408RequestTimeout, title: "Request timeout", extensions: extensions),
			var _ => Results.Problem(me.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error", extensions: extensions)
		};
	}

	public static async Task<IActionResult> ToApiFileStreamActionResultAsync<TPayload>(
		this Task<IResponse<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : Stream
		=> ToApiActionResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IActionResult> ToApiFileStreamActionResultAsync<TPayload>(
		this Task<Response<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : Stream
		=> ToApiActionResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IActionResult> ToApiFileBytesActionResultAsync<TPayload>(
		this Task<IResponse<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : IEnumerable<byte>
		=> ToApiActionResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IActionResult> ToApiFileBytesActionResultAsync<TPayload>(
		this Task<Response<TPayload>> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : IEnumerable<byte>
		=> ToApiActionResult(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static IActionResult ToApiFileStreamActionResult<TPayload>(
		this IResponse<TPayload> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : Stream
		=> me.ToApiActionResult(contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static IActionResult ToApiFileBytesActionResult<TPayload>(
		this IResponse<TPayload> me,
		string? contentType = null,
		string? fileDownloadName = null,
		DateTimeOffset? lastModified = null,
		EntityTagHeaderValue? entityTag = null,
		bool enableRangeProcessing = false)
		where TPayload : IEnumerable<byte>
		=> me.ToApiActionResult(contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	public static async Task<IActionResult> ToApiActionResultAsync(this Task<IResponse> me, bool fullSerialization = false)
		=> ToApiActionResult(await me, fullSerialization);
	public static async Task<IActionResult> ToApiActionResultAsync(this Task<Response> me, bool fullSerialization = false)
		=> ToApiActionResult(await me, fullSerialization);
	public static async Task<IActionResult> ToApiActionResultAsync<TPayload>(this Task<IResponse<TPayload>> me)
		=> ToApiActionResult(await me);
	public static async Task<IActionResult> ToApiActionResultAsync<TPayload>(this Task<Response<TPayload>> me)
		=> ToApiActionResult(await me);
	public static IActionResult ToApiActionResult(this IResponse me, bool fullSerialization = false)
		=> me.ToApiActionResult(null, null, null, null, false, fullSerialization);
	public static IActionResult ToApiActionResult<TPayload>(this IResponse<TPayload> me)
		=> me.ToApiActionResult(null, null, null, null, false, false);

	static IActionResult ToApiActionResult(
		this IResponse me,
		string? contentType,
		string? fileDownloadName,
		DateTimeOffset? lastModified,
		EntityTagHeaderValue? entityTag,
		bool enableRangeProcessing,
		bool fullSerialization)
	{
		if (me.IsSuccess)
			if (me is IResponse<object?> me2 && me2.Payload is not null)
				if (me2.Payload is Stream stream)
					return new FileStreamResult(stream, contentType ?? string.Empty)
					{
						FileDownloadName = fileDownloadName,
						LastModified = lastModified,
						EntityTag = entityTag,
						EnableRangeProcessing = enableRangeProcessing
					};
				else
					return new OkObjectResult(fullSerialization ? me2 : me2.Payload);
			else if (me.Message is not null)
				return new ContentResult
				{
					Content = me.Message,
					ContentType = "text/plain"
				};
			else
				return new NoContentResult();

		var extensions = me.Extensions.ToDictionary();
		if (me is IResponse<object?> me3 && me3.Payload is not null && me3.Payload is not Stream) extensions["payload"] = me3.Payload;
		if (IncludeException && me.Exception is not null)
			extensions["exception"] = JsonSerializer.SerializeToElement(me.Exception, options: new()
			{
				Converters =
				{
					new ExceptionConverter()
				}
			});

		return me.ErrorType switch
		{
			ErrorType.NotFound => new(GetProblem(me.Message, StatusCodes.Status404NotFound, "Not found", extensions))
			{
				StatusCode = StatusCodes.Status404NotFound
			},
			ErrorType.PermissionDenied => new(GetProblem(me.Message, StatusCodes.Status403Forbidden, "Forbidden", extensions))
			{
				StatusCode = StatusCodes.Status403Forbidden
			},
			ErrorType.InvalidData => new(GetProblem(me.Message, StatusCodes.Status400BadRequest, "Bad request", extensions))
			{
				StatusCode = StatusCodes.Status400BadRequest
			},
			ErrorType.Conflict => new(GetProblem(me.Message, StatusCodes.Status409Conflict, "Conflict", extensions))
			{
				StatusCode = StatusCodes.Status409Conflict
			},
			ErrorType.Critical => new(GetProblem(me.Message, StatusCodes.Status500InternalServerError, "Internal server error", extensions))
			{
				StatusCode = StatusCodes.Status500InternalServerError
			},
			ErrorType.NotSupported => new(GetProblem(me.Message, StatusCodes.Status501NotImplemented, "Not implemented", extensions))
			{
				StatusCode = StatusCodes.Status501NotImplemented
			},
			var _ => new ObjectResult(GetProblem(me.Message, StatusCodes.Status500InternalServerError, "Internal server error", extensions))
			{
				StatusCode = StatusCodes.Status500InternalServerError
			}
		};
		ProblemDetails GetProblem(string? detail, int? status, string? title, Dictionary<string, object?>? extensions)
		{
			var res = new ProblemDetails
			{
				Detail = detail,
				Status = status,
				Title = title,
				Type = status is not null ? GetTypeFromInt(status.Value) : null
			};
			if (extensions is not null) res.Extensions = extensions;
			return res;
		}
	}

	static string GetTypeFromInt(int status) => GetTypeFromStatusCode((HttpStatusCode)status);
	static string GetTypeFromStatusCode(HttpStatusCode status)
		=> status switch
		{
			HttpStatusCode.Continue => "https://tools.ietf.org/html/rfc7231#section-6.2.1", // 100
			HttpStatusCode.SwitchingProtocols => "https://tools.ietf.org/html/rfc7231#section-6.2.2", // 101

			HttpStatusCode.OK => "https://tools.ietf.org/html/rfc7231#section-6.3.1", // 200
			HttpStatusCode.Created => "https://tools.ietf.org/html/rfc7231#section-6.3.2", // 201
			HttpStatusCode.Accepted => "https://tools.ietf.org/html/rfc7231#section-6.3.3", // 202
			HttpStatusCode.NonAuthoritativeInformation => "https://tools.ietf.org/html/rfc7231#section-6.3.4", // 203
			HttpStatusCode.NoContent => "https://tools.ietf.org/html/rfc7231#section-6.3.5", // 204
			HttpStatusCode.ResetContent => "https://tools.ietf.org/html/rfc7231#section-6.3.6", // 205
			HttpStatusCode.PartialContent => "https://tools.ietf.org/html/rfc7233#section-4.1", // 206

			HttpStatusCode.MultipleChoices => "https://tools.ietf.org/html/rfc7231#section-6.4.1", // 300
			HttpStatusCode.MovedPermanently => "https://tools.ietf.org/html/rfc7231#section-6.4.2", // 301
			HttpStatusCode.Found => "https://tools.ietf.org/html/rfc7231#section-6.4.3", // 302
			HttpStatusCode.SeeOther => "https://tools.ietf.org/html/rfc7231#section-6.4.4", // 303
			HttpStatusCode.NotModified => "https://tools.ietf.org/html/rfc7232#section-4.1", // 304
			HttpStatusCode.UseProxy => "https://tools.ietf.org/html/rfc7231#section-6.4.5", // 305
			HttpStatusCode.Unused => "https://tools.ietf.org/html/rfc7231#section-6.4.6", // 306
			HttpStatusCode.TemporaryRedirect => "https://tools.ietf.org/html/rfc7231#section-6.4.7", // 307

			HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1", // 400
			HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1", // 401
			HttpStatusCode.PaymentRequired => "https://tools.ietf.org/html/rfc7231#section-6.5.2", // 402
			HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3", // 403
			HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4", // 404
			HttpStatusCode.MethodNotAllowed => "https://tools.ietf.org/html/rfc7231#section-6.5.5", // 405
			HttpStatusCode.NotAcceptable => "https://tools.ietf.org/html/rfc7231#section-6.5.6", // 406
			HttpStatusCode.ProxyAuthenticationRequired => "https://tools.ietf.org/html/rfc7235#section-3.2", // 407
			HttpStatusCode.RequestTimeout => "https://tools.ietf.org/html/rfc7231#section-6.5.7", // 408
			HttpStatusCode.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8", // 409
			HttpStatusCode.Gone => "https://tools.ietf.org/html/rfc7231#section-6.5.9", // 410
			HttpStatusCode.LengthRequired => "https://tools.ietf.org/html/rfc7231#section-6.5.10", // 411
			HttpStatusCode.PreconditionFailed => "https://tools.ietf.org/html/rfc7232#section-4.2", // 412
			HttpStatusCode.RequestEntityTooLarge => "https://tools.ietf.org/html/rfc7231#section-6.5.11", // 413
			HttpStatusCode.RequestUriTooLong => "https://tools.ietf.org/html/rfc7231#section-6.5.12", //414
			HttpStatusCode.UnsupportedMediaType => "https://tools.ietf.org/html/rfc7231#section-6.5.13", //415
			HttpStatusCode.RequestedRangeNotSatisfiable => "https://tools.ietf.org/html/rfc7233#section-4.4", // 416
			HttpStatusCode.ExpectationFailed => "https://tools.ietf.org/html/rfc7231#section-6.5.14", // 417
			HttpStatusCode.UpgradeRequired => "https://tools.ietf.org/html/rfc7231#section-6.5.15", // 426

			HttpStatusCode.InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1", // 500
			HttpStatusCode.NotImplemented => "https://tools.ietf.org/html/rfc7231#section-6.6.2", // 501
			HttpStatusCode.BadGateway => "https://tools.ietf.org/html/rfc7231#section-6.6.3", // 502
			HttpStatusCode.ServiceUnavailable => "https://tools.ietf.org/html/rfc7231#section-6.6.4", // 503
			HttpStatusCode.GatewayTimeout => "https://tools.ietf.org/html/rfc7231#section-6.6.5", // 504
			HttpStatusCode.HttpVersionNotSupported => "https://tools.ietf.org/html/rfc7231#section-6.6.6", // 505
			var _ => throw new NotImplementedException($"Status code '{status}' is not supported")
		};
}