using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web.Http;
using System.Web.Http.Results;
using Fuxion.Text.Json.Serialization;
using static System.Net.WebRequestMethods;
using static Fuxion.Net.Http.Extensions;

namespace Fuxion.AspNet;

public static class ResponseExtensions
{
	public static bool IncludeException { get; set; } = true;
	public static bool UseNewtonsoft { get; set; } = false;
	public static JsonSerializerOptions? JsonSerializerOptions { get; set; }
	public static IHttpActionResult ToApiResult<TPayload>(this Response<TPayload> me)
	{
		if (me.IsSuccess)
			return HttpActionResultFactory.Success(me.Payload, me.Message);

		var extensions = me.Extensions.ToDictionary(e => e.Key, e => e.Value);
		extensions.Remove(StatusCodeKey);
		extensions.Remove(ReasonPhraseKey);
		if (me.Payload is not null)
		{
			extensions[PayloadKey] = me.Payload;
		}
		if (IncludeException && me.Exception is not null)
		{
			var jsonOptions = JsonSerializerOptions is null
				? new() { Converters = { new ExceptionConverter() } }
				: JsonSerializerOptions.Transform(o =>
				{
					var res = new JsonSerializerOptions(o);
					res.Converters.Add(new ExceptionConverter());
					return res;
				});
			extensions[ExceptionKey] = UseNewtonsoft
				? Newtonsoft.Json.Linq.JObject.Parse(me.Exception.SerializeToJson(true))
				: JsonSerializer.SerializeToElement(me.Exception, options: jsonOptions);
		}

		return me.ErrorType switch
		{
			ErrorType.NotFound
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.NotFound, "Not found", extensions),
			ErrorType.PermissionDenied
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.Forbidden, "Forbidden", extensions),
			ErrorType.InvalidData
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.BadRequest, "Bad request", extensions),
			ErrorType.Conflict
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.Conflict, "Conflict", extensions),
			ErrorType.Critical
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.InternalServerError, "Internal server error", extensions),
			ErrorType.NotSupported
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.NotImplemented, "Not implemented", extensions),
			ErrorType.Unavailable
				=> HttpActionResultFactory.Problem(me.Message, HttpStatusCode.ServiceUnavailable, "Service unavailable", extensions),
			var _ => HttpActionResultFactory.Problem(me.Message, HttpStatusCode.InternalServerError, "Internal server error", extensions)
		};
	}
}
file class HttpActionResultFactory(HttpStatusCode status, object? payload = null, string? message = null, bool isProblem = false) : IHttpActionResult
{
	public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		=> payload is null
			? message is null
				? Task.FromResult(new HttpResponseMessage(status))
				: Task.FromResult(new HttpResponseMessage(status)
				{
					Content = new StringContent(message)
				})
			: Task.FromResult(new HttpResponseMessage(status)
			{
				Content = ResponseExtensions.UseNewtonsoft
					? new ObjectContent(payload.GetType(), payload, new JsonMediaTypeFormatter())
					: new StringContent(payload.SerializeToJson(
							true, ResponseExtensions.JsonSerializerOptions != null
								? new(ResponseExtensions.JsonSerializerOptions)
								: null),
						Encoding.UTF8,
						isProblem ? "application/problem+json" : "application/json"),
			});
	public static IHttpActionResult Success(object? payload, string? message)
		=> payload is null
			? message is null
				? new HttpActionResultFactory(HttpStatusCode.NoContent, payload)
				: new HttpActionResultFactory(HttpStatusCode.OK, null, message)
			: new HttpActionResultFactory(HttpStatusCode.OK, payload);
	public static IHttpActionResult Problem(string? detail, HttpStatusCode statusCode, string title, Dictionary<string, object?>? extensions)
		=> new HttpActionResultFactory(statusCode, new ResponseProblemDetails
		{
			Type = GetTypeFromStatusCode(statusCode),
			Status = (int)statusCode,
			Title = title,
			Detail = detail,
			Extensions = extensions ?? new(StringComparer.Ordinal)
		}, isProblem: true);
	static string GetTypeFromStatusCode(HttpStatusCode status)
	{
		return status switch
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
	}
}