namespace Fuxion.Union;

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
