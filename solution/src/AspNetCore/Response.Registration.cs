using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fuxion.AspNetCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseBuilderExtensions
{
	public static MvcOptions UseResponses(this MvcOptions options)
	{
		options.Filters.Add<ResponseActionFilter>();
		return options;
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
	/// purpose because they already get per-request naming support from <see cref="RequestNamingInputFormatter"/>
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
	/// per-request naming support from <see cref="RequestNamingInputFormatter"/>, registered independently by
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

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
