namespace Fuxion.AspNetCore;

using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Partial response options applied as endpoint metadata. Properties are nullable so that
/// merging preserves unset values inherited from outer scopes (global options, parent groups,
/// controller). Can be used as an attribute on controllers/actions or registered directly as
/// metadata on route groups and endpoints through <c>UseResponses</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResponseOptionsAttribute : Attribute
{
	public bool? SerializeFullResponses { get; set; }
	public bool? SerializeErrorAsProblemDetails { get; set; }
	public bool? StrictNone { get; set; }

	public ResponseOptionsAttribute() { }

	public ResponseOptionsAttribute(bool serializeFullResponses)
		=> SerializeFullResponses = serializeFullResponses;
}
#pragma warning restore CS1591
