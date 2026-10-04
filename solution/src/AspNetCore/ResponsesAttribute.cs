using System;

namespace Fuxion.AspNetCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Partial response options applied as endpoint metadata. Merging preserves unset values inherited
/// from outer scopes (global options, parent groups, controller). Can be used as an attribute on
/// controllers/actions or registered directly as metadata on route groups and endpoints through
/// <c>UseResponses</c>.
/// </summary>
/// <remarks>
/// <see cref="Nullable{T}"/> is not a legal attribute parameter type (CS0655), so the tri-state
/// "not set / true / false" semantics of <see cref="ResponseOptionsLayer"/> cannot be exposed
/// directly as <c>bool?</c> properties here. Each value is tracked with a private <c>bool</c> plus
/// a shadow "has a value" flag set whenever the setter runs; only <see cref="ToLayer"/> exposes the
/// nullable view used for merging.
/// </remarks>
// Keep in sync with Fuxion.AspNet.ResponsesAttribute: ResponsesAttributeShape (Test.Responses.Shared) pins both.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResponsesAttribute : Attribute
{
	bool serializeFullResponses;
	bool hasSerializeFullResponses;
	public bool SerializeFullResponses
	{
		get => serializeFullResponses;
		set { serializeFullResponses = value; hasSerializeFullResponses = true; }
	}

	bool serializeErrorAsProblemDetails;
	bool hasSerializeErrorAsProblemDetails;
	public bool SerializeErrorAsProblemDetails
	{
		get => serializeErrorAsProblemDetails;
		set { serializeErrorAsProblemDetails = value; hasSerializeErrorAsProblemDetails = true; }
	}

	bool strictNone;
	bool hasStrictNone;
	public bool StrictNone
	{
		get => strictNone;
		set { strictNone = value; hasStrictNone = true; }
	}

	/// <summary>Converts this attribute to a core <see cref="ResponseOptionsLayer"/> so it can be merged with <c>ResponseOptionsMerge.Merge</c>.</summary>
	public ResponseOptionsLayer ToLayer() => new()
	{
		SerializeFullResponses = hasSerializeFullResponses ? serializeFullResponses : null,
		SerializeErrorAsProblemDetails = hasSerializeErrorAsProblemDetails ? serializeErrorAsProblemDetails : null,
		StrictNone = hasStrictNone ? strictNone : null
	};
}
#pragma warning restore CS1591
