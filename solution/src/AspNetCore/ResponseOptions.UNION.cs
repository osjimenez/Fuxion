namespace Fuxion.AspNetCore;

using Fuxion.Union;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseOptionsAttributeMerge
{
	/// <summary>
	/// Applies the metadata layers on top of these options, in order, so that
	/// inner scopes override outer ones and unset values are inherited.
	/// </summary>
	public static ResponseOptions Merge(this ResponseOptions options, ResponseOptionsAttribute[] layers)
		=> options.Merge(layers.Select(l => l.ToLayer()));
}

#pragma warning restore CS1591

static class ResponseOptionsResolver
{
	public static ResponseOptions Resolve(HttpContext httpContext)
	{
		if (httpContext is null)
			throw new ArgumentNullException(nameof(httpContext));

		var services = httpContext.RequestServices;
		var global = services?.GetService<IOptions<ResponseOptions>>()?.Value ?? new ResponseOptions();

		var endpoint = httpContext.GetEndpoint();
		if (endpoint is null)
			return global;

		var layers = endpoint.Metadata.OfType<ResponseOptionsAttribute>().ToArray();
		return global.Merge(layers);
	}
}
