namespace Fuxion.AspNetCore;

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseMiddlewareExtensions
{
	public static MvcOptions UseResponses(this MvcOptions options)
	{
		options.Filters.Add<ResponseActionFilter>();
		return options;
	}

	 extension(IResponse me)
	 {
		  public IResult ToResult()
		  {
				return new DeferredResponseResult(me);
		  }

		  public IResult ToResult(ResponseOptions options)
		  {
				if (ResponseHttpMapper.TryMap(me, options, out var mapping))
					 return mapping.ToResult();

				throw new NotSupportedException($"The union response value of type '{me.Value?.GetType().FullName ?? "null"}' is not supported yet.");
		  }

		  public IActionResult ToActionResult()
		  {
				return new DeferredResponseActionResult(me);
		  }

		  public IActionResult ToActionResult(ResponseOptions options)
		  {
				if (ResponseHttpMapper.TryMap(me, options, out var mapping))
					 return mapping.ToActionResult();

				throw new NotSupportedException($"The union response value of type '{me.Value?.GetType().FullName ?? "null"}' is not supported yet.");
		  }
	 }

	 sealed class DeferredResponseResult : IResult
	 {
		  readonly IResponse _response;
		  public DeferredResponseResult(IResponse response) => _response = response;

		  public Task ExecuteAsync(HttpContext httpContext)
		  {
				var options = ResponseOptionsResolver.Resolve(httpContext);
				if (!ResponseHttpMapper.TryMap(_response, options, out var mapping))
					 throw new NotSupportedException("The union response value is not supported by the current response mapping.");

				return mapping.ToResult().ExecuteAsync(httpContext);
		  }
	 }

	 sealed class DeferredResponseActionResult : IActionResult
	 {
		  readonly IResponse _response;
		  public DeferredResponseActionResult(IResponse response) => _response = response;

		  public Task ExecuteResultAsync(ActionContext context)
		  {
				var options = ResponseOptionsResolver.Resolve(context.HttpContext);
				if (!ResponseHttpMapper.TryMap(_response, options, out var mapping))
					 throw new NotSupportedException("The union response value is not supported by the current response mapping.");

				return mapping.ToActionResult().ExecuteResultAsync(context);
		  }
	 }

	 public static TBuilder UseResponses<TBuilder>(this TBuilder builder)
		  where TBuilder : IEndpointConventionBuilder
	 {
		  builder.AddEndpointFilterFactory(ResponseEndpointFilterFactory.Create);
		  return builder;
	 }

	 public static TBuilder UseResponses<TBuilder>(this TBuilder builder, Action<ResponseOptionsAttribute> configure)
		  where TBuilder : IEndpointConventionBuilder
	 {
		  var meta = new ResponseOptionsAttribute();
		  configure?.Invoke(meta);
		  builder.WithMetadata(meta);
		  builder.AddEndpointFilterFactory(ResponseEndpointFilterFactory.Create);
		  return builder;
	 }
}

/// <summary>
/// Shortcuts to map the <see cref="Unit"/> value to an HTTP response.
/// </summary>
public static class UnitResponseExtensions
{
	 extension(Unit)
	 {
		  /// <summary>
		  /// An <see cref="IResult"/> representing an operation that completed without a result.
		  /// </summary>
		  public static IResult Result => ((IResponse)(Response<Unit>)Unit.Value).ToResult();

		  /// <summary>
		  /// An <see cref="IActionResult"/> representing an operation that completed without a result.
		  /// </summary>
		  public static IActionResult ActionResult => ((IResponse)(Response<Unit>)Unit.Value).ToActionResult();
	 }
}

/// <summary>
/// Shortcuts to map the <see cref="None"/> value to an HTTP response.
/// </summary>
public static class NoneResponseExtensions
{
	 extension(None)
	 {
		  /// <summary>
		  /// An <see cref="IResult"/> representing the absence of a result.
		  /// </summary>
		  public static IResult Result => ((IResponse)(ResponseMaybe<Unit>)None.Value).ToResult();

		  /// <summary>
		  /// An <see cref="IActionResult"/> representing the absence of a result.
		  /// </summary>
		  public static IActionResult ActionResult => ((IResponse)(ResponseMaybe<Unit>)None.Value).ToActionResult();
	 }
}

static class ResponseEndpointFilterFactory
{
	public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
	{
		  // Keep the build-time gate to avoid affecting endpoints that did not opt-in.
		  if (!ResponseHttpMapper.IsSupportedDeclaredResponseType(context.MethodInfo.ReturnType))
				return next;

		  return async invocationContext =>
		  {
				var value = await next(invocationContext);
				// Resolve options at runtime from HttpContext
				var options = ResponseOptionsResolver.Resolve(invocationContext.HttpContext);
				return ResponseHttpMapper.TryMap(value, options, out var mapping)
					 ? mapping.ToResult()
					 : value;
		  };
	}
}

sealed class ResponseActionFilter : IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context) { }

	public void OnActionExecuted(ActionExecutedContext context)
	{
		if (context.ActionDescriptor is not ControllerActionDescriptor action)
			return;

		if (!ResponseHttpMapper.IsSupportedDeclaredResponseType(action.MethodInfo.ReturnType))
			return;

		if (context.Result is not ObjectResult result)
			return;

		  var options = ResponseOptionsResolver.Resolve(context.HttpContext);
		  if (!ResponseHttpMapper.TryMap(result.Value, options, out var mapping))
				return;

		  context.Result = mapping.ToActionResult();
	}
}

	 static class ResponseHttpMapper
	 {
		  public static bool IsSupportedDeclaredResponseType(Type type)
				=> IsResponseReturnType(UnwrapTypeIfATaskWrapIt(type));

		  public static bool TryMap(object? value, ResponseOptions options, out ResponseHttpMapping mapping)
		  {
				// Do not touch already materialized results
				if (value is IResult || value is IActionResult)
				{
					 mapping = default;
					 return false;
				}

				if (value is IResponse response)
				{
					 // Error handling
					 if (response.IsError)
					 {
						  var status = StatusCodes.Status500InternalServerError;
						  if (response.Value is Error er && er.Type is HttpStatusCode s)
								status = (int)s;

						  if (options.SerializeErrorAsProblemDetails)
						  {
								if (response.Value is Error er2)
								{
									 mapping = ResponseHttpMapping.FromError(er2);
									 return true;
								}

								// A typed business error also travels as problem+json, carried in the
								// errorPayload extension member, so it is never lost on the wire.
								if (response.Value is not null)
								{
									 mapping = ResponseHttpMapping.FromTypedError(response.Value);
									 return true;
								}

								mapping = new ResponseHttpMapping(StatusCodes.Status500InternalServerError, new ResponseProblemDetails
								{
									 Status = StatusCodes.Status500InternalServerError,
									 Title = "Internal server error",
									 Detail = "The response is error but it does not contain a supported error payload."
								}, false, responseKind: ResponseHeaders.ErrorKind);
								return true;
						  }

						  // Serialize error as raw/Error or full response
						  if (options.SerializeFullResponses)
						  {
								mapping = new ResponseHttpMapping(status, value, false, responseKind: ResponseHeaders.ErrorKind);
								return true;
						  }

						  if (response.Value is Error er3)
						  {
								mapping = new ResponseHttpMapping(status, er3, false, responseKind: ResponseHeaders.ErrorKind);
								return true;
						  }

						  // Payload-only mode emits the bare error value, never the envelope.
						  mapping = new ResponseHttpMapping(status, response.Value, false, responseKind: ResponseHeaders.ErrorKind);
						  return true;
					 }

					 // Success cases
					 if (response.Value is Unit)
					 {
						  // Unit means "completed without a result": 200 with an empty body,
						  // plus a header so it stays distinguishable from None (204) on the wire.
						  if (options.SerializeFullResponses)
						  {
								mapping = new ResponseHttpMapping(StatusCodes.Status200OK, value, false, responseKind: ResponseHeaders.UnitKind);
								return true;
						  }

						  mapping = new ResponseHttpMapping(StatusCodes.Status200OK, null, true, responseKind: ResponseHeaders.UnitKind);
						  return true;
					 }

					 if (response.Value is None)
					 {
						  // None means "absence of a result": 204 unless the full envelope is requested
						  // and StrictNone allows it to carry a body.
						  if (!options.StrictNone && options.SerializeFullResponses)
						  {
								mapping = new ResponseHttpMapping(StatusCodes.Status200OK, value, false, responseKind: ResponseHeaders.NoneKind);
								return true;
						  }

						  mapping = new ResponseHttpMapping(StatusCodes.Status204NoContent, null, true, responseKind: ResponseHeaders.NoneKind);
						  return true;
					 }

					 if (response.Value is not null)
					 {
						  if (options.SerializeFullResponses)
								mapping = new ResponseHttpMapping(StatusCodes.Status200OK, value, false, responseKind: ResponseHeaders.PayloadKind);
						  else
								mapping = new ResponseHttpMapping(StatusCodes.Status200OK, response.Value, false, responseKind: ResponseHeaders.PayloadKind);

						  return true;
					 }

					 // Unset / default Response
					 mapping = new ResponseHttpMapping(StatusCodes.Status500InternalServerError, new ResponseProblemDetails
					 {
						  Status = StatusCodes.Status500InternalServerError,
						  Title = "Internal server error",
						  Detail = "The Response value is uninitialized (default)."
					 }, false, responseKind: ResponseHeaders.ErrorKind);
					 return true;
				}

				if (value is Error error)
				{
					 var status = error.Type is HttpStatusCode s ? (int)s : StatusCodes.Status500InternalServerError;
					 if (options.SerializeErrorAsProblemDetails)
					 {
						  mapping = ResponseHttpMapping.FromError(error);
						  return true;
					 }

					 mapping = new ResponseHttpMapping(status, error, false, responseKind: ResponseHeaders.ErrorKind);
					 return true;
				}

				mapping = default;
				return false;
		  }

	static Type UnwrapTypeIfATaskWrapIt(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
			return type.GetGenericArguments()[0];

		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
			return type.GetGenericArguments()[0];

		return type;
	}

	static bool IsResponseReturnType(Type type)
	{
		  if (type == typeof(Error) || type == typeof(None))
				return true;

		  if (!type.IsGenericType)
				return false;

		  var definition = type.GetGenericTypeDefinition();
		  return definition == typeof(Response<>)
				|| definition == typeof(Response<,>)
				|| definition == typeof(ResponseMaybe<>)
				|| definition == typeof(ResponseMaybe<,>);
	}
}

readonly struct ResponseHttpMapping(int statusCode, object? body, bool suppressBody, Error? error = null, string? responseKind = null, string? problemTitle = null)
{
	public int StatusCode { get; } = statusCode;
	public object? Body { get; } = body;
	public bool SuppressBody { get; } = suppressBody;
	public Error? Error { get; } = error;

	/// <summary>
	/// Overrides the ProblemDetails title derived from the status code. Used for typed business
	/// errors, whose problem shape carries a stable generic title instead of the status phrase.
	/// </summary>
	public string? ProblemTitle { get; } = problemTitle;

	/// <summary>
	/// Semantic discriminator emitted as a header so Unit and None stay distinguishable
	/// even if an intermediary normalizes a body-less 200 into a 204.
	/// </summary>
	public string? ResponseKind { get; } = responseKind;

	public static ResponseHttpMapping FromError(Error error)
		=> new(error.Type is HttpStatusCode status ? (int)status : StatusCodes.Status500InternalServerError, null, false, error, ResponseHeaders.ErrorKind);

	// A typed business error has no HttpStatusCode source, so it maps to 500 for now (a configurable
	// error-to-status mapping is a planned improvement). It travels wrapped in an Error so the whole
	// ProblemDetails pipeline is reused: the value lands in the same errorPayload extension member
	// that Error.Payload already uses. The title is a stable generic one instead of the status phrase,
	// and no type member is emitted so the CLR type never leaks to the wire.
	public static ResponseHttpMapping FromTypedError(object typedError)
		=> new(StatusCodes.Status500InternalServerError, null, false, new Error { Payload = typedError }, ResponseHeaders.ErrorKind, "Business error");

	public IResult ToResult()
		=> Error is not null
			? new ErrorProblemDetailsResult(Error.Value, ProblemTitle)
			: new ResponseKindResult(this);

	public IActionResult ToActionResult()
		=> Error is not null
			? new ErrorProblemActionResult(Error.Value, ProblemTitle)
			: new ResponseKindActionResult(this);

	internal IResult ToInnerResult()
		=> SuppressBody
			? Results.StatusCode(StatusCode)
			: Results.Json(Body, contentType: MediaType, statusCode: StatusCode);

	internal IActionResult ToInnerActionResult()
	{
		if (SuppressBody)
			return new StatusCodeResult(StatusCode);

		var result = new JsonResult(Body) { StatusCode = StatusCode };
		if (MediaType is not null)
			result.ContentType = MediaType;

		return result;
	}

	// The full envelope is advertised with its own vendor media type so a consumer can tell it
	// apart from a bare payload. Any other body keeps the default JSON content type.
	string? MediaType => Body is IResponse ? ResponseMediaTypes.ResponseJson : null;
}

sealed class ResponseKindResult(ResponseHttpMapping mapping) : IResult
{
	public Task ExecuteAsync(HttpContext httpContext)
	{
		if (mapping.ResponseKind is not null)
			httpContext.Response.Headers[ResponseHeaders.ResponseKind] = mapping.ResponseKind;

		return mapping.ToInnerResult().ExecuteAsync(httpContext);
	}
}

sealed class ResponseKindActionResult(ResponseHttpMapping mapping) : IActionResult
{
	public Task ExecuteResultAsync(ActionContext context)
	{
		if (mapping.ResponseKind is not null)
			context.HttpContext.Response.Headers[ResponseHeaders.ResponseKind] = mapping.ResponseKind;

		return mapping.ToInnerActionResult().ExecuteResultAsync(context);
	}
}

sealed class ErrorProblemDetailsResult(Error error, string? titleOverride = null) : IResult
{
	public Task ExecuteAsync(HttpContext httpContext)
	{
		var jsonOptions = ResolveHttpJsonOptions(httpContext);
		var problem = global::Fuxion.ErrorProblemDetailsConverter.ToProblemDetails(error, jsonOptions);
		if (titleOverride is not null)
			problem.Title = titleOverride;
		httpContext.Response.Headers[ResponseHeaders.ResponseKind] = ResponseHeaders.ErrorKind;
		return Results.Json(problem, jsonOptions, contentType: ResponseMediaTypes.ProblemJson, statusCode: problem.Status ?? StatusCodes.Status500InternalServerError).ExecuteAsync(httpContext);
	}

	static JsonSerializerOptions ResolveHttpJsonOptions(HttpContext httpContext)
		=> httpContext.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions
			?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
}

sealed class ErrorProblemActionResult(Error error, string? titleOverride = null) : IActionResult
{
	public Task ExecuteResultAsync(ActionContext context)
	{
		var jsonOptions = ResolveMvcJsonOptions(context.HttpContext);
		var problem = global::Fuxion.ErrorProblemDetailsConverter.ToProblemDetails(error, jsonOptions);
		if (titleOverride is not null)
			problem.Title = titleOverride;
		context.HttpContext.Response.Headers[ResponseHeaders.ResponseKind] = ResponseHeaders.ErrorKind;
		context.HttpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
		context.HttpContext.Response.ContentType = ResponseMediaTypes.ProblemJson;
		return context.HttpContext.Response.WriteAsJsonAsync(problem, jsonOptions, contentType: ResponseMediaTypes.ProblemJson);
	}

	static JsonSerializerOptions ResolveMvcJsonOptions(HttpContext httpContext)
		=> httpContext.RequestServices.GetService<IOptions<MvcJsonOptions>>()?.Value.JsonSerializerOptions
			?? httpContext.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions
			?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member