using System;

namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Partial response options for a controller or an action. Null properties inherit from the outer
/// scope (global, then controller, then action).
/// </summary>
// Nullable<T> is not a legal attribute parameter type (CS0655), so a tri-state "not set / true / false"
// property cannot be typed as bool? here; each value is tracked with a shadow "has a value" flag instead,
// and only ToLayer() exposes the nullable semantics documented for a response options layer.
// Keep in sync with Fuxion.AspNetCore.ResponseOptionsAttribute: ResponseOptionsAttributeShape (Test.Responses.Shared) pins both.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResponseOptionsAttribute : Attribute
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

	public ResponseOptionsLayer ToLayer() => new()
	{
		SerializeFullResponses = hasSerializeFullResponses ? serializeFullResponses : null,
		SerializeErrorAsProblemDetails = hasSerializeErrorAsProblemDetails ? serializeErrorAsProblemDetails : null,
		StrictNone = hasStrictNone ? strictNone : null
	};
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
