using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Fuxion.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using static Fuxion.Net.Http.Extensions;

namespace Fuxion.AspNet;

public static class ResponseExtensions
{
	public static bool IncludeException { get; set; } = true;
	public static JsonSerializerOptions? JsonSerializerOptions { get; set; }

	// Core helper used by all extensions
	static IHttpActionResult ToApiResultCore(
		IResponse me,
		string? contentType,
		string? fileDownloadName,
		bool fullSerialization)
	{
		if (me.IsSuccess)
			if (me is IResponse<object?> { Payload: not null } me2)
				if (me2.Payload is Stream stream)
					return Factory.Ok(new StreamContent(stream)
					{
						Headers =
						{
							ContentDisposition = new("attachment")
							{
								FileName = fileDownloadName ?? (me.Extensions.TryGetValue(FileNameKey, out var fileNameExt) && fileNameExt is string fn ? fn : null),
							},
							ContentType = new(contentType ?? (me.Extensions.TryGetValue(ContentTypeKey, out var contentTypeExt) && contentTypeExt is string con ? con : null)),
							ContentLength = me.Extensions.TryGetValue(ContentLengthKey, out var extension) && extension is long length ? length : -1,
						}
					});
				else if (me2.Payload is byte[] bytes)
					return Factory.Ok(new ByteArrayContent(bytes)
					{
						Headers =
						{
							ContentDisposition = new("attachment")
							{
								FileName = fileDownloadName ?? (me.Extensions.TryGetValue(FileNameKey, out var fileNameExt) && fileNameExt is string fn ? fn : null)
							},
							ContentType = new(contentType ?? (me.Extensions.TryGetValue(ContentTypeKey, out var contentTypeExt) && contentTypeExt is string con ? con : null)),
							ContentLength = me.Extensions.TryGetValue(ContentLengthKey, out var extension) && extension is long length ? length : -1
						}
					});
				else
					return Factory.Ok(fullSerialization
						? new StringContent(me2.SerializeToJson(JsonSerializerOptions != null ? new(JsonSerializerOptions) : null), Encoding.UTF8, "application/json")
						: new StringContent(me2.Payload.SerializeToJson(JsonSerializerOptions != null ? new(JsonSerializerOptions) : null), Encoding.UTF8, "application/json")
					);
			else if (me.Message is not null)
				return Factory.Ok(fullSerialization
					? new StringContent(me.SerializeToJson(JsonSerializerOptions != null ? new(JsonSerializerOptions) : null), Encoding.UTF8, "application/json")
					: new StringContent(me.Message));
			else
				return Factory.NoContent();

		var extensions = me.Extensions.ToDictionary(e => e.Key, e => e.Value);
		extensions.Remove(StatusCodeKey);
		extensions.Remove(ReasonPhraseKey);

		if (me is IResponse<object?> { Payload: not null and not Stream } me3) extensions[PayloadKey] = me3.Payload;
		if (IncludeException && me.Exception is not null)
		{
			var jsonOptions = JsonSerializerOptions is null
				? new()
				{
					Converters =
					{
						new ExceptionConverter()
					}
				}
				: JsonSerializerOptions.Transform(o =>
				{
					var res = new JsonSerializerOptions(o);
					res.Converters.Add(new ExceptionConverter());
					return res;
				});
			extensions[ExceptionKey] = JsonSerializer.SerializeToElement(me.Exception, jsonOptions);
		}

		return me.ErrorType switch
		{
			ErrorType.NotFound => Factory.Problem(me.Message, HttpStatusCode.NotFound, "Not found", extensions),
			ErrorType.PermissionDenied => Factory.Problem(me.Message, HttpStatusCode.Forbidden, "Forbidden", extensions),
			ErrorType.InvalidData => Factory.Problem(me.Message, HttpStatusCode.BadRequest, "Bad request", extensions),
			ErrorType.Conflict => Factory.Problem(me.Message, HttpStatusCode.Conflict, "Conflict", extensions),
			ErrorType.Critical => Factory.Problem(me.Message, HttpStatusCode.InternalServerError, "Internal server error", extensions),
			ErrorType.NotSupported => Factory.Problem(me.Message, HttpStatusCode.NotImplemented, "Not implemented", extensions),
			ErrorType.Unavailable => Factory.Problem(me.Message, HttpStatusCode.ServiceUnavailable, "Service unavailable", extensions),
			ErrorType.Timeout => Factory.Problem(me.Message, HttpStatusCode.RequestTimeout, "Request timeout", extensions),
			var _ => Factory.Problem(me.Message, HttpStatusCode.InternalServerError, "Internal server error", extensions)
		};
	}

	// New C# 14 extension syntax blocks

	// Task<IResponse<TPayload>> receivers (general)
	extension<TPayload>(Task<IResponse<TPayload>> me)
	{
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, fullSerialization);
	}
	// Task<Response<TPayload>> receivers (general)
	extension<TPayload>(Task<Response<TPayload>> me)
	{
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, fullSerialization);
	}

	// Stream payload specializations
	extension<TPayload>(Task<IResponse<TPayload>> me) where TPayload : Stream
	{
		public async Task<IHttpActionResult> ToApiFileStreamResultAsync(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(await me, contentType, fileDownloadName, false);
	}
	extension<TPayload>(Task<Response<TPayload>> me) where TPayload : Stream
	{
		public async Task<IHttpActionResult> ToApiFileStreamResultAsync(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(await me, contentType, fileDownloadName, false);
	}
	extension<TPayload>(IResponse<TPayload> me) where TPayload : Stream
	{
		public IHttpActionResult ToApiFileStreamResult(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(me, contentType, fileDownloadName, false);
	}

	// Bytes payload specializations
	extension<TPayload>(Task<IResponse<TPayload>> me) where TPayload : IEnumerable<byte>
	{
		public async Task<IHttpActionResult> ToApiFileBytesResultAsync(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(await me, contentType, fileDownloadName, false);
	}
	extension<TPayload>(Task<Response<TPayload>> me) where TPayload : IEnumerable<byte>
	{
		public async Task<IHttpActionResult> ToApiFileBytesResultAsync(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(await me, contentType, fileDownloadName, false);
	}
	extension<TPayload>(IResponse<TPayload> me) where TPayload : IEnumerable<byte>
	{
		public IHttpActionResult ToApiFileBytesResult(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(me, contentType, fileDownloadName, false);
	}

	// Non-generic IResponse and Task wrappers
	extension(Task<IResponse> me)
	{
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, fullSerialization);
	}
	extension(Task<Response> me)
	{
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, fullSerialization);
	}
	extension(IResponse me)
	{
		public IHttpActionResult ToApiResult(bool fullSerialization = false)
			=> ToApiResultCore(me, null, null, fullSerialization);
	}
}

file class Factory(Func<CancellationToken, Task<HttpResponseMessage>> func) : IHttpActionResult
{
	public static Factory Ok(HttpContent? content = null) => Create(HttpStatusCode.OK, content);
	public static Factory NoContent() => Create(HttpStatusCode.NoContent);
	public static Factory Problem(string? detail, HttpStatusCode statusCode, string title, Dictionary<string, object?>? extensions)
		=> Create(statusCode, new StringContent(new ResponseProblemDetails
		{
			Type = GetTypeFromStatusCode(statusCode),
			Status = (int)statusCode,
			Title = title,
			Detail = detail,
			Extensions = extensions ?? new(StringComparer.Ordinal)
		}.SerializeToJson(ResponseExtensions.JsonSerializerOptions != null ? new(ResponseExtensions.JsonSerializerOptions) : null), Encoding.UTF8, "application/problem+json"));
	static Factory Create(HttpStatusCode status, HttpContent? content = null)
		=> new(_ =>
		{
			var msg = new HttpResponseMessage(status);
			if (content is not null) msg.Content = content;
			return Task.FromResult(msg);
		});
	static string GetTypeFromStatusCode(HttpStatusCode status)
		=> status switch
		{
			HttpStatusCode.Continue => "https://httpwg.org/specs/rfc9110.html#section-15.2.1", // 100
			HttpStatusCode.SwitchingProtocols => "https://httpwg.org/specs/rfc9110.html#section-15.2.2", // 101

			HttpStatusCode.OK => "https://httpwg.org/specs/rfc9110.html#section-15.3.1", // 200
			HttpStatusCode.Created => "https://httpwg.org/specs/rfc9110.html#section-15.3.2", // 201
			HttpStatusCode.Accepted => "https://httpwg.org/specs/rfc9110.html#section-15.3.3", // 202
			HttpStatusCode.NonAuthoritativeInformation => "https://httpwg.org/specs/rfc9110.html#section-15.3.4", // 203
			HttpStatusCode.NoContent => "https://httpwg.org/specs/rfc9110.html#section-15.3.5", // 204
			HttpStatusCode.ResetContent => "https://httpwg.org/specs/rfc9110.html#section-15.3.6", // 205
			HttpStatusCode.PartialContent => "https://httpwg.org/specs/rfc9110.html#section-15.3.7", // 206

			HttpStatusCode.MultipleChoices => "https://httpwg.org/specs/rfc9110.html#section-15.4.1", // 300
			HttpStatusCode.MovedPermanently => "https://httpwg.org/specs/rfc9110.html#section-15.4.2", // 301
			HttpStatusCode.Found => "https://httpwg.org/specs/rfc9110.html#section-15.4.3", // 302
			HttpStatusCode.SeeOther => "https://httpwg.org/specs/rfc9110.html#section-15.4.4", // 303
			HttpStatusCode.NotModified => "https://httpwg.org/specs/rfc9110.html#section-15.4.5", // 304
			HttpStatusCode.UseProxy => "https://httpwg.org/specs/rfc9110.html#section-15.4.6", // 305
			HttpStatusCode.Unused => "https://httpwg.org/specs/rfc9110.html#section-15.4.7", // 306
			HttpStatusCode.TemporaryRedirect => "https://httpwg.org/specs/rfc9110.html#section-15.4.8", // 307

			HttpStatusCode.BadRequest => "https://httpwg.org/specs/rfc9110.html#section-15.5.1", // 400
			HttpStatusCode.Unauthorized => "https://httpwg.org/specs/rfc9110.html#section-15.5.2", // 401
			HttpStatusCode.PaymentRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.3", // 402
			HttpStatusCode.Forbidden => "https://httpwg.org/specs/rfc9110.html#section-15.5.4", // 403
			HttpStatusCode.NotFound => "https://httpwg.org/specs/rfc9110.html#section-15.5.5", // 404
			HttpStatusCode.MethodNotAllowed => "https://httpwg.org/specs/rfc9110.html#section-15.5.6", // 405
			HttpStatusCode.NotAcceptable => "https://httpwg.org/specs/rfc9110.html#section-15.5.7", // 406
			HttpStatusCode.ProxyAuthenticationRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.8", // 407
			HttpStatusCode.RequestTimeout => "https://httpwg.org/specs/rfc9110.html#section-15.5.9", // 408
			HttpStatusCode.Conflict => "https://httpwg.org/specs/rfc9110.html#section-15.5.10", // 409
			HttpStatusCode.Gone => "https://httpwg.org/specs/rfc9110.html#section-15.5.11", // 410
			HttpStatusCode.LengthRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.12", // 411
			HttpStatusCode.PreconditionFailed => "https://httpwg.org/specs/rfc9110.html#section-15.5.13", // 412
			HttpStatusCode.RequestEntityTooLarge => "https://httpwg.org/specs/rfc9110.html#section-15.5.14", // 413
			HttpStatusCode.RequestUriTooLong => "https://httpwg.org/specs/rfc9110.html#section-15.5.15", // 414
			HttpStatusCode.UnsupportedMediaType => "https://httpwg.org/specs/rfc9110.html#section-15.5.16", // 415
			HttpStatusCode.RequestedRangeNotSatisfiable => "https://httpwg.org/specs/rfc9110.html#section-15.5.17", // 416
			HttpStatusCode.ExpectationFailed => "https://httpwg.org/specs/rfc9110.html#section-15.5.18", // 417
			HttpStatusCode.UpgradeRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.19", // 426

			HttpStatusCode.InternalServerError => "https://httpwg.org/specs/rfc9110.html#section-15.6.1", // 500
			HttpStatusCode.NotImplemented => "https://httpwg.org/specs/rfc9110.html#section-15.6.2", // 501
			HttpStatusCode.BadGateway => "https://httpwg.org/specs/rfc9110.html#section-15.6.3", // 502
			HttpStatusCode.ServiceUnavailable => "https://httpwg.org/specs/rfc9110.html#section-15.6.4", // 503
			HttpStatusCode.GatewayTimeout => "https://httpwg.org/specs/rfc9110.html#section-15.6.5", // 504
			HttpStatusCode.HttpVersionNotSupported => "https://httpwg.org/specs/rfc9110.html#section-15.6.6", // 505

			var _ => throw new NotImplementedException($"Status code '{status}' is not supported")
		};
	Task<HttpResponseMessage> IHttpActionResult.ExecuteAsync(CancellationToken cancellationToken) => func(cancellationToken);
}