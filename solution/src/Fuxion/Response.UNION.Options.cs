namespace Fuxion.Union;

using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Server-side defaults for how union responses are written to the wire. They only apply to
/// clients that do not ask for a specific shape through the Accept header.
/// </summary>
public sealed record ResponseOptions
{
	/// <summary>Write the full response envelope instead of the bare payload.</summary>
	public bool SerializeFullResponses { get; set; } = false;

	/// <summary>Write errors as RFC 9457 ProblemDetails instead of the native Error shape.</summary>
	public bool SerializeErrorAsProblemDetails { get; set; } = true;

	/// <summary>When the envelope is in use, force None to be a body-less 204 instead of an envelope.</summary>
	public bool StrictNone { get; set; } = false;
}

/// <summary>
/// A partial layer of options declared by a scope (a route group, a controller, an action). Null means
/// "not set here, inherit from the outer scope", so layers can be merged from outside in.
/// </summary>
public sealed record ResponseOptionsLayer
{
	public bool? SerializeFullResponses { get; init; }
	public bool? SerializeErrorAsProblemDetails { get; init; }
	public bool? StrictNone { get; init; }
}

public static class ResponseOptionsMerge
{
	/// <summary>Applies the layers in order on a copy of <paramref name="options"/>; unset values are inherited.</summary>
	public static ResponseOptions Merge(this ResponseOptions options, IEnumerable<ResponseOptionsLayer> layers)
	{
		var result = options with { };
		foreach (var layer in layers)
		{
			if (layer.SerializeFullResponses.HasValue) result.SerializeFullResponses = layer.SerializeFullResponses.Value;
			if (layer.SerializeErrorAsProblemDetails.HasValue) result.SerializeErrorAsProblemDetails = layer.SerializeErrorAsProblemDetails.Value;
			if (layer.StrictNone.HasValue) result.StrictNone = layer.StrictNone.Value;
		}
		return result;
	}
}

#pragma warning restore CS1591
