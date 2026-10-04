using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Fuxion.AspNetCore;

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

		// Inner scopes override outer ones and unset values are inherited.
		return global.Merge(endpoint.Metadata.OfType<ResponsesAttribute>().Select(l => l.ToLayer()));
	}

	// The scope options plus what the client asked for in Accept.
	public static ResponseOptions ResolveEffective(HttpContext httpContext)
		=> ResponseAccept.Apply(Resolve(httpContext), httpContext.Request.Headers.Accept.ToString());
}
