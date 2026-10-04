using System;
using System.Collections.Generic;
using System.Net;

namespace Fuxion;

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

	/// <summary>
	/// Naming the client asked for through Accept on a Fuxion media type; null = the server's own policy.
	/// Never set by configuration; set by ResponseAccept.
	/// </summary>
	public string? Naming { get; internal set; }

	/// <summary>
	/// Status for a business error value the service does not own (or wants to override). Return null for
	/// "no opinion": the value's own <see cref="IHttpStatusError"/> is used next, then 500.
	/// </summary>
	public Func<object, HttpStatusCode?>? BusinessErrorStatus { get; set; }

	/// <summary>
	/// Upper bound for a request body rewritten by the minimal APIs naming support; larger bodies get 413.
	/// Bodies without a naming parameter are never buffered.
	/// </summary>
	public long RequestNamingMaxBodySize { get; set; } = 1024 * 1024;
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
