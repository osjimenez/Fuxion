namespace Fuxion.Union;

using System;
using System.Net.Http.Headers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Applies the client's Accept header on top of the scope defaults. Only an Accept that explicitly
/// names a Fuxion media type (or problem+json) changes the shape: "*/*", "application/json" or no
/// Accept at all keep the defaults, so a client that knows nothing about Fuxion sees a vanilla API.
/// </summary>
public static class ResponseAccept
{
	public static ResponseOptions Apply(ResponseOptions defaults, string? accept)
	{
		if (string.IsNullOrWhiteSpace(accept)) return defaults;

		bool envelope = false, nativeError = false, problem = false;
		string? naming = null;
		foreach (var item in accept!.Split(','))
		{
			if (!MediaTypeWithQualityHeaderValue.TryParse(item.Trim(), out var parsed)) continue;
			if (parsed.Quality is 0) continue;

			var isFuxion = ResponseMediaTypes.IsFuxion(parsed.MediaType);
			if (string.Equals(parsed.MediaType, ResponseMediaTypes.ResponseJson, StringComparison.OrdinalIgnoreCase)) envelope = true;
			else if (string.Equals(parsed.MediaType, ResponseMediaTypes.ErrorJson, StringComparison.OrdinalIgnoreCase)) nativeError = true;
			else if (string.Equals(parsed.MediaType, ResponseMediaTypes.ProblemJson, StringComparison.OrdinalIgnoreCase)) problem = true;

			// The naming parameter is only meaningful on Fuxion types: application/json never carries parameters.
			if (isFuxion && naming is null)
				foreach (var parameter in parsed.Parameters)
					if (string.Equals(parameter.Name, ResponseMediaTypes.NamingParameter, StringComparison.OrdinalIgnoreCase)
						&& ResponseNaming.IsSupported(parameter.Value?.Trim('"')))
						naming = parameter.Value!.Trim('"').ToLowerInvariant();
		}

		if (!envelope && !nativeError && !problem && naming is null) return defaults;

		var result = defaults with { };
		if (envelope) result.SerializeFullResponses = true;
		if (nativeError) result.SerializeErrorAsProblemDetails = false;
		else if (problem) result.SerializeErrorAsProblemDetails = true;
		if (naming is not null) result.Naming = naming;
		return result;
	}
}
