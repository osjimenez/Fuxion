namespace Fuxion.AspNetCore;

using Fuxion.Union;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Resolved response options used by the mapper.
/// </summary>
public sealed record ResponseOptions : ResponseSerializerOptions
{
	public bool StrictNone { get; set; } = false;

	/// <summary>
	/// Applies the metadata layers on top of these options, in order, so that
	/// inner scopes override outer ones and unset values are inherited.
	/// </summary>
	public ResponseOptions Merge(ResponseOptionsAttribute[] layers)
	{
		var result = this with { };

		foreach (var layer in layers)
		{
			if (layer.SerializeFullResponses.HasValue)
				result.SerializeFullResponses = layer.SerializeFullResponses.Value;
			if (layer.SerializeErrorAsProblemDetails.HasValue)
				result.SerializeErrorAsProblemDetails = layer.SerializeErrorAsProblemDetails.Value;
			if (layer.StrictNone.HasValue)
				result.StrictNone = layer.StrictNone.Value;
		}

		return result;
	}
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

		// Walk endpoint metadata in order and collect the response option layers declared
		// either through UseResponses on groups/endpoints or as attributes on controllers/actions.
		var layers = endpoint.Metadata.OfType<ResponseOptionsAttribute>().ToArray();

		return global.Merge(layers);
	}
}
