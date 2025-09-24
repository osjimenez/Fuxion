using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Fuxion.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using static Fuxion.Net.Http.Extensions;

namespace Fuxion.AspNetCore;

public static class ResponseExtensions
{
	public static bool IncludeException { get; set; } = true;

	// Core helpers (non-extension) used by all extension methods
	private static IResult ToApiResultCore(
		IResponse me,
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
				else if (me2.Payload is IEnumerable<byte> bytes)
					return Results.File(bytes.ToArray(), contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag);
				else
					return Results.Ok(fullSerialization ? me2 : me2.Payload);
			else if (me.Message is not null)
				return fullSerialization ? Results.Ok(me) : Results.Content(me.Message);
			else
				return Results.NoContent();

		var extensions = me.Extensions.ToDictionary();
		extensions.Remove(StatusCodeKey);
		extensions.Remove(ReasonPhraseKey);

		if (me is IResponse<object?> { Payload: not null and not Stream } me3) extensions[PayloadKey] = me3.Payload;
		if (IncludeException && me.Exception is not null)
			extensions[ExceptionKey] = JsonSerializer.SerializeToElement(me.Exception, options: new()
			{
				Converters = { new ExceptionConverter() }
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

	private static IActionResult ToApiActionResultCore(
		IResponse me,
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
				return fullSerialization
					? new OkObjectResult(me)
					: new ContentResult { Content = me.Message, ContentType = "text/plain" };
			else
				return new NoContentResult();

		var extensions = me.Extensions.ToDictionary();
		if (me is IResponse<object?> me3 && me3.Payload is not null && me3.Payload is not Stream) extensions["payload"] = me3.Payload;
		if (IncludeException && me.Exception is not null)
			extensions["exception"] = JsonSerializer.SerializeToElement(me.Exception, options: new()
			{
				Converters = { new ExceptionConverter() }
			});

		return me.ErrorType switch
		{
			ErrorType.NotFound => new(GetProblem(me.Message, StatusCodes.Status404NotFound, "Not found", extensions)) { StatusCode = StatusCodes.Status404NotFound },
			ErrorType.PermissionDenied => new(GetProblem(me.Message, StatusCodes.Status403Forbidden, "Forbidden", extensions)) { StatusCode = StatusCodes.Status403Forbidden },
			ErrorType.InvalidData => new(GetProblem(me.Message, StatusCodes.Status400BadRequest, "Bad request", extensions)) { StatusCode = StatusCodes.Status400BadRequest },
			ErrorType.Conflict => new(GetProblem(me.Message, StatusCodes.Status409Conflict, "Conflict", extensions)) { StatusCode = StatusCodes.Status409Conflict },
			ErrorType.Critical => new(GetProblem(me.Message, StatusCodes.Status500InternalServerError, "Internal server error", extensions)) { StatusCode = StatusCodes.Status500InternalServerError },
			ErrorType.NotSupported => new(GetProblem(me.Message, StatusCodes.Status501NotImplemented, "Not implemented", extensions)) { StatusCode = StatusCodes.Status501NotImplemented },
			var _ => new ObjectResult(GetProblem(me.Message, StatusCodes.Status500InternalServerError, "Internal server error", extensions)) { StatusCode = StatusCodes.Status500InternalServerError }
		};

		ProblemDetails GetProblem(string? detail, int? status, string? title, Dictionary<string, object?>? extensions)
		{
			var res = new ProblemDetails { Detail = detail, Status = status, Title = title, Type = status is not null ? GetTypeFromInt(status.Value) : null };
			if (extensions is not null) res.Extensions = extensions;
			return res;
		}
	}

	private static string GetTypeFromInt(int status) => GetTypeFromStatusCode((HttpStatusCode)status);

	private static string GetTypeFromStatusCode(HttpStatusCode status) => status switch
	{
		HttpStatusCode.Continue => "https://tools.ietf.org/html/rfc7231#section-6.2.1",
		HttpStatusCode.SwitchingProtocols => "https://tools.ietf.org/html/rfc7231#section-6.2.2",
		HttpStatusCode.OK => "https://tools.ietf.org/html/rfc7231#section-6.3.1",
		HttpStatusCode.Created => "https://tools.ietf.org/html/rfc7231#section-6.3.2",
		HttpStatusCode.Accepted => "https://tools.ietf.org/html/rfc7231#section-6.3.3",
		HttpStatusCode.NonAuthoritativeInformation => "https://tools.ietf.org/html/rfc7231#section-6.3.4",
		HttpStatusCode.NoContent => "https://tools.ietf.org/html/rfc7231#section-6.3.5",
		HttpStatusCode.ResetContent => "https://tools.ietf.org/html/rfc7231#section-6.3.6",
		HttpStatusCode.PartialContent => "https://tools.ietf.org/html/rfc7233#section-4.1",
		HttpStatusCode.MultipleChoices => "https://tools.ietf.org/html/rfc7231#section-6.4.1",
		HttpStatusCode.MovedPermanently => "https://tools.ietf.org/html/rfc7231#section-6.4.2",
		HttpStatusCode.Found => "https://tools.ietf.org/html/rfc7231#section-6.4.3",
		HttpStatusCode.SeeOther => "https://tools.ietf.org/html/rfc7231#section-6.4.4",
		HttpStatusCode.NotModified => "https://tools.ietf.org/html/rfc7232#section-4.1",
		HttpStatusCode.UseProxy => "https://tools.ietf.org/html/rfc7231#section-6.4.5",
		HttpStatusCode.Unused => "https://tools.ietf.org/html/rfc7231#section-6.4.6",
		HttpStatusCode.TemporaryRedirect => "https://tools.ietf.org/html/rfc7231#section-6.4.7",
		HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
		HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
		HttpStatusCode.PaymentRequired => "https://tools.ietf.org/html/rfc7231#section-6.5.2",
		HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
		HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
		HttpStatusCode.MethodNotAllowed => "https://tools.ietf.org/html/rfc7231#section-6.5.5",
		HttpStatusCode.NotAcceptable => "https://tools.ietf.org/html/rfc7231#section-6.5.6",
		HttpStatusCode.ProxyAuthenticationRequired => "https://tools.ietf.org/html/rfc7235#section-3.2",
		HttpStatusCode.RequestTimeout => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
		HttpStatusCode.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
		HttpStatusCode.Gone => "https://tools.ietf.org/html/rfc7231#section-6.5.9",
		HttpStatusCode.LengthRequired => "https://tools.ietf.org/html/rfc7231#section-6.5.10",
		HttpStatusCode.PreconditionFailed => "https://tools.ietf.org/html/rfc7232#section-4.2",
		HttpStatusCode.RequestEntityTooLarge => "https://tools.ietf.org/html/rfc7231#section-6.5.11",
		HttpStatusCode.RequestUriTooLong => "https://tools.ietf.org/html/rfc7231#section-6.5.12",
		HttpStatusCode.UnsupportedMediaType => "https://tools.ietf.org/html/rfc7231#section-6.5.13",
		HttpStatusCode.RequestedRangeNotSatisfiable => "https://tools.ietf.org/html/rfc7233#section-4.4",
		HttpStatusCode.ExpectationFailed => "https://tools.ietf.org/html/rfc7231#section-6.5.14",
		HttpStatusCode.UpgradeRequired => "https://tools.ietf.org/html/rfc7231#section-6.5.15",
		HttpStatusCode.InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
		HttpStatusCode.NotImplemented => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
		HttpStatusCode.BadGateway => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
		HttpStatusCode.ServiceUnavailable => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
		HttpStatusCode.GatewayTimeout => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
		HttpStatusCode.HttpVersionNotSupported => "https://tools.ietf.org/html/rfc7231#section-6.6.6",
		var _ => throw new NotImplementedException($"Status code '{status}' is not supported")
	};

	// New C# 14 extension syntax blocks

	// Task<Response<TPayload>> receivers
	extension<TPayload>(Task<Response<TPayload>> me) where TPayload : Stream
	{
		public async Task<IResult> ToApiFileStreamResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public async Task<IResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, null, null, false, fullSerialization);

		public async Task<IActionResult> ToApiFileStreamActionResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiActionResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public async Task<IActionResult> ToApiActionResultAsync(bool fullSerialization = false)
			=> ToApiActionResultCore(await me, null, null, null, null, false, fullSerialization);
	}
	extension<TPayload>(Task<Response<TPayload>> me) where TPayload : IEnumerable<byte>
	{
		public async Task<IResult> ToApiFileBytesResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public async Task<IActionResult> ToApiFileBytesActionResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiActionResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	}

	// Task<IResponse<TPayload>> receivers (general)
	extension<TPayload>(Task<IResponse<TPayload>> me)
	{
		public async Task<IResult> ToApiResultAsync()
			=> ToApiResultCore(await me, null, null, null, null, false, false);

		public async Task<IActionResult> ToApiActionResultAsync()
			=> ToApiActionResultCore(await me, null, null, null, null, false, false);
	}
	// Task<IResponse<TPayload>> specialized for stream and bytes
	extension<TPayload>(Task<IResponse<TPayload>> me) where TPayload : Stream
	{
		public async Task<IResult> ToApiFileStreamResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public async Task<IActionResult> ToApiFileStreamActionResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiActionResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	}
	extension<TPayload>(Task<IResponse<TPayload>> me) where TPayload : IEnumerable<byte>
	{
		public async Task<IResult> ToApiFileBytesResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public async Task<IActionResult> ToApiFileBytesActionResultAsync(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiActionResultCore(await me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	}

	// Task<IResponse> and Task<Response> receivers
	extension(Task<IResponse> me)
	{
		public async Task<IResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, null, null, false, fullSerialization);

		public async Task<IActionResult> ToApiActionResultAsync(bool fullSerialization = false)
			=> ToApiActionResultCore(await me, null, null, null, null, false, fullSerialization);
	}
	extension(Task<Response> me)
	{
		public async Task<IResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, null, null, false, fullSerialization);

		public async Task<IActionResult> ToApiActionResultAsync(bool fullSerialization = false)
			=> ToApiActionResultCore(await me, null, null, null, null, false, fullSerialization);
	}

	// IResponse<TPayload> receivers
	extension<TPayload>(IResponse<TPayload> me)
	{
		public IResult ToApiResult() => ToApiResultCore(me, null, null, null, null, false, false);
		public IActionResult ToApiActionResult() => ToApiActionResultCore(me, null, null, null, null, false, false);
	}
	extension<TPayload>(IResponse<TPayload> me) where TPayload : Stream
	{
		public IResult ToApiFileStreamResult(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiResultCore(me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public IActionResult ToApiFileStreamActionResult(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiActionResultCore(me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	}
	extension<TPayload>(IResponse<TPayload> me) where TPayload : IEnumerable<byte>
	{
		public IResult ToApiFileBytesResult(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiResultCore(me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);

		public IActionResult ToApiFileBytesActionResult(
			string? contentType = null,
			string? fileDownloadName = null,
			DateTimeOffset? lastModified = null,
			EntityTagHeaderValue? entityTag = null,
			bool enableRangeProcessing = false)
			=> ToApiActionResultCore(me, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing, false);
	}

	// IResponse receivers
	extension(IResponse me)
	{
		public IResult ToApiResult(bool fullSerialization = false)
			=> ToApiResultCore(me, null, null, null, null, false, fullSerialization);

		public IActionResult ToApiActionResult(bool fullSerialization = false)
			=> ToApiActionResultCore(me, null, null, null, null, false, fullSerialization);
	}
}