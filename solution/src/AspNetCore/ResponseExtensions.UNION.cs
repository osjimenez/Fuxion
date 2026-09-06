namespace Fuxion.AspNetCore;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fuxion.Union;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
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
		/// <summary>Deferred: the effective options (scope + Accept) are resolved when the result executes.</summary>
		public IResult ToResult() => new DeferredResponseResult(me);

		/// <summary>Explicit options, for callers without an HTTP context. Accept is not consulted.</summary>
		public IResult ToResult(ResponseOptions options)
			=> ResponseWireMapper.TryMap(me, options, out var mapping)
				? new WireResult(mapping)
				: throw new NotSupportedException($"The union response value of type '{me.Value?.GetType().FullName ?? "null"}' is not supported.");

		public IActionResult ToActionResult() => new DeferredResponseActionResult(me);

		public IActionResult ToActionResult(ResponseOptions options)
			=> ResponseWireMapper.TryMap(me, options, out var mapping)
				? new WireActionResult(mapping)
				: throw new NotSupportedException($"The union response value of type '{me.Value?.GetType().FullName ?? "null"}' is not supported.");
	}

	sealed class DeferredResponseResult(IResponse response) : IResult
	{
		public Task ExecuteAsync(HttpContext httpContext)
		{
			if (!ResponseHttpAdapter.TryMap(httpContext, response, out var mapping))
				throw new NotSupportedException("The union response value is not supported by the current response mapping.");
			return new WireResult(mapping).ExecuteAsync(httpContext);
		}
	}

	sealed class DeferredResponseActionResult(IResponse response) : IActionResult
	{
		public Task ExecuteResultAsync(ActionContext context)
		{
			if (!ResponseHttpAdapter.TryMap(context.HttpContext, response, out var mapping))
				throw new NotSupportedException("The union response value is not supported by the current response mapping.");
			return new WireActionResult(mapping).ExecuteResultAsync(context);
		}
	}

	/// <summary>
	/// Enables the union response wire contract on this endpoint convention builder: mapped union return values
	/// are turned into the appropriate HTTP response (envelope, native/problem error, Unit...), and a request
	/// body's own per-request <c>naming</c> Content-Type parameter (spec §3) is honored by wrapping the
	/// endpoint's <c>RequestDelegate</c> (<see cref="RequestNamingEndpoint.Apply"/>).
	/// </summary>
	/// <remarks>
	/// This call is for minimal-API endpoints. A controller action reached through this builder (e.g.
	/// <c>app.MapControllers().UseResponses()</c>) gets nothing from it: MVC ignores endpoint filter factories,
	/// so the response-side contract for controllers comes from <c>AddControllers(o =&gt; o.UseResponses())</c>
	/// (<see cref="UseResponses(MvcOptions)"/>), and the request-naming wrapper skips controller endpoints on
	/// purpose because they already get per-request naming support from <see cref="ResponseNamingInputFormatter"/>
	/// (registered by <c>AddResponses</c>); wrapping them too would transcode the request body twice.
	/// </remarks>
	public static TBuilder UseResponses<TBuilder>(this TBuilder builder)
		where TBuilder : IEndpointConventionBuilder
	{
		builder.AddEndpointFilterFactory(ResponseEndpointFilterFactory.Create);
		// Route handler RequestDelegates are set after normal conventions run, so only a Finally convention
		// can see (and wrap) the final delegate to apply the per-request naming support (spec D7).
		builder.Finally(RequestNamingEndpoint.Apply);
		return builder;
	}

	/// <summary>
	/// Same as the parameterless <see cref="UseResponses{TBuilder}(TBuilder)"/>, but also attaches a
	/// <see cref="ResponseOptionsAttribute"/> built by <paramref name="configure"/>, overriding the response
	/// options (scope + error shape) for every endpoint reached through this builder.
	/// </summary>
	/// <param name="builder">The endpoint convention builder (a route group, a mapped endpoint...) to enable the wire contract on.</param>
	/// <param name="configure">Sets the per-endpoint (or per-group) response options override.</param>
	/// <remarks>
	/// As with the parameterless overload, a controller action reached through this builder is skipped by the
	/// request-naming wrapper (<see cref="RequestNamingEndpoint.Apply"/>): MVC controllers already get
	/// per-request naming support from <see cref="ResponseNamingInputFormatter"/>, registered independently by
	/// <c>AddResponses</c>. Only minimal-API endpoints have their <c>RequestDelegate</c> wrapped by this call.
	/// </remarks>
	public static TBuilder UseResponses<TBuilder>(this TBuilder builder, Action<ResponseOptionsAttribute> configure)
		where TBuilder : IEndpointConventionBuilder
	{
		var meta = new ResponseOptionsAttribute();
		configure?.Invoke(meta);
		builder.WithMetadata(meta);
		builder.AddEndpointFilterFactory(ResponseEndpointFilterFactory.Create);
		// Same as the parameterless overload: a nested group that calls this overload directly (instead of
		// only inheriting its parent's) must still get the naming wrapper. RequestNamingEndpoint.Apply is
		// idempotent, so an endpoint reached by both this and an ancestor's UseResponses() is only wrapped once.
		builder.Finally(RequestNamingEndpoint.Apply);
		return builder;
	}
}

/// <summary>Shortcuts to map the <see cref="Unit"/> value to an HTTP response.</summary>
public static class UnitResponseExtensions
{
	extension(Unit)
	{
		/// <summary>An <see cref="IResult"/> representing an operation that completed without a result.</summary>
		public static IResult Result => ((IResponse)(Response<Unit>)Unit.Value).ToResult();

		/// <summary>An <see cref="IActionResult"/> representing an operation that completed without a result.</summary>
		public static IActionResult ActionResult => ((IResponse)(Response<Unit>)Unit.Value).ToActionResult();
	}
}

/// <summary>Shortcuts to map the <see cref="None"/> value to an HTTP response.</summary>
public static class NoneResponseExtensions
{
	extension(None)
	{
		/// <summary>An <see cref="IResult"/> representing the absence of a result.</summary>
		public static IResult Result => ((IResponse)(ResponseMaybe<Unit>)None.Value).ToResult();

		/// <summary>An <see cref="IActionResult"/> representing the absence of a result.</summary>
		public static IActionResult ActionResult => ((IResponse)(ResponseMaybe<Unit>)None.Value).ToActionResult();
	}
}

static class ResponseEndpointFilterFactory
{
	public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
	{
		// Build-time gate: endpoints that did not declare a union return type are never touched.
		if (!ResponseWireMapper.IsSupportedDeclaredResponseType(context.MethodInfo.ReturnType))
			return next;

		return async invocationContext =>
		{
			var value = await next(invocationContext);
			return ResponseHttpAdapter.TryMap(invocationContext.HttpContext, value, out var mapping)
				? new WireResult(mapping)
				: value;
		};
	}
}

sealed class ResponseActionFilter : IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context) { }

	public void OnActionExecuted(ActionExecutedContext context)
	{
		if (context.ActionDescriptor is not ControllerActionDescriptor action) return;
		if (!ResponseWireMapper.IsSupportedDeclaredResponseType(action.MethodInfo.ReturnType)) return;
		if (context.Result is not ObjectResult result) return;
		if (!ResponseHttpAdapter.TryMap(context.HttpContext, result.Value, out var mapping)) return;

		context.Result = new WireActionResult(mapping);
	}
}

/// <summary>
/// The ASP.NET Core side of the wire contract: resolves the effective options (scope defaults + the
/// request's Accept), delegates the decision to <see cref="ResponseWireMapper"/> and writes the result.
/// </summary>
static class ResponseHttpAdapter
{
	public static ResponseOptions ResolveEffectiveOptions(HttpContext httpContext)
		=> ResponseAccept.Apply(ResponseOptionsResolver.Resolve(httpContext), httpContext.Request.Headers.Accept.ToString());

	public static bool TryMap(HttpContext httpContext, object? value, out ResponseWireMapping mapping)
	{
		// Already materialized results are never touched.
		if (value is IResult || value is IActionResult)
		{
			mapping = null!;
			return false;
		}

		return ResponseWireMapper.TryMap(value, ResolveEffectiveOptions(httpContext), out mapping);
	}

	// The shape depends on Accept, so shared caches must key on it. StringValues.Contains is an exact
	// element match, so a pre-existing "Vary: Accept, Origin" needs a comma-separated-value comparison
	// (case-insensitively) instead, or Accept would be appended a second time.
	public static void MarkVaryByAccept(HttpResponse response)
	{
		var existing = response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
		foreach (var value in existing)
			if (string.Equals(value, HeaderNames.Accept, StringComparison.OrdinalIgnoreCase))
				return;

		response.Headers.Append(HeaderNames.Vary, HeaderNames.Accept);
	}

	public static JsonSerializerOptions ResolveHttpJsonOptions(HttpContext httpContext)
		=> httpContext.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions
			?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

	public static JsonSerializerOptions ResolveMvcJsonOptions(HttpContext httpContext)
		=> httpContext.RequestServices.GetService<IOptions<MvcJsonOptions>>()?.Value.JsonSerializerOptions
			?? httpContext.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions
			?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

	public static EntityTagHeaderValue? ParseETag(string? etag)
		=> !string.IsNullOrWhiteSpace(etag) && EntityTagHeaderValue.TryParse(etag, out var parsed) ? parsed : null;
}

sealed class WireResult(ResponseWireMapping mapping) : IResult
{
	public Task ExecuteAsync(HttpContext httpContext)
	{
		if (mapping.Shape == ResponseWireShape.Binary)
			return BinaryResult((FileContent)mapping.Value!).ExecuteAsync(httpContext);

		ResponseHttpAdapter.MarkVaryByAccept(httpContext.Response);

		if (!mapping.HasBody)
			return Results.StatusCode(mapping.StatusCode).ExecuteAsync(httpContext);

		var jsonOptions = ResponseHttpAdapter.ResolveHttpJsonOptions(httpContext);
		var (contentType, body, serializerOptions) = mapping.Materialize(jsonOptions);
		return Results.Json(body, serializerOptions ?? jsonOptions, contentType, mapping.StatusCode).ExecuteAsync(httpContext);
	}

	// The framework writes the stream (and disposes it), sets Content-Disposition, Content-Length,
	// ETag/Last-Modified and serves Range requests. No Vary: a file's shape does not depend on Accept.
	static IResult BinaryResult(FileContent file)
		=> Results.File(file.Stream, file.ContentType, file.FileName, file.LastModified, ResponseHttpAdapter.ParseETag(file.ETag), file.EnableRangeProcessing);
}

sealed class WireActionResult(ResponseWireMapping mapping) : IActionResult
{
	public Task ExecuteResultAsync(ActionContext context)
	{
		var http = context.HttpContext;

		if (mapping.Shape == ResponseWireShape.Binary)
		{
			var file = (FileContent)mapping.Value!;
			return new FileStreamResult(file.Stream, file.ContentType)
			{
				FileDownloadName = file.FileName,
				LastModified = file.LastModified,
				EntityTag = ResponseHttpAdapter.ParseETag(file.ETag),
				EnableRangeProcessing = file.EnableRangeProcessing
			}.ExecuteResultAsync(context);
		}

		ResponseHttpAdapter.MarkVaryByAccept(http.Response);
		http.Response.StatusCode = mapping.StatusCode;

		if (!mapping.HasBody)
			return Task.CompletedTask;

		var jsonOptions = ResponseHttpAdapter.ResolveMvcJsonOptions(http);
		var (contentType, body, serializerOptions) = mapping.Materialize(jsonOptions);
		return http.Response.WriteAsJsonAsync(body, body!.GetType(), serializerOptions ?? jsonOptions, contentType, http.RequestAborted);
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
