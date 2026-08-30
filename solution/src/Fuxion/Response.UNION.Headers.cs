namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Wire contract used to discriminate the response shapes of the union model.
/// </summary>
/// <remarks>
/// Both <see cref="Unit"/> and <see cref="None"/> can be emitted without a body, so the status
/// code alone is not a reliable discriminator: intermediaries are allowed to normalize a body-less
/// 200 into a 204. This header makes the semantics explicit and survives such normalization.
/// It is emitted for every mapped response shape, not only the body-less ones, so a consumer can
/// rely on it uniformly instead of treating it as a special case.
/// </remarks>
public static class ResponseHeaders
{
	/// <summary>
	/// Header carrying the semantic shape of a response.
	/// </summary>
	/// <remarks>
	/// Declared in lowercase because HTTP/2 and HTTP/3 require lowercase field names on the wire,
	/// and without the discouraged "X-" prefix (RFC 6648).
	/// </remarks>
	public const string ResponseKind = "fuxion-response-kind";

	/// <summary>
	/// Value for an operation that completed successfully without producing a result.
	/// </summary>
	public const string UnitKind = "unit";

	/// <summary>
	/// Value for the explicit absence of a result.
	/// </summary>
	public const string NoneKind = "none";

	/// <summary>
	/// Value for a successful response carrying a payload.
	/// </summary>
	public const string PayloadKind = "payload";

	/// <summary>
	/// Value for a failed response carrying an error.
	/// </summary>
	public const string ErrorKind = "error";
}

/// <summary>
/// Media types used by the union response model.
/// </summary>
public static class ResponseMediaTypes
{
	/// <summary>
	/// Standard media type for a RFC 9457 ProblemDetails payload.
	/// </summary>
	public const string ProblemJson = "application/problem+json";

	/// <summary>
	/// Media type for the full response envelope.
	/// </summary>
	/// <remarks>
	/// Uses the vendor tree defined by RFC 6838, because this type is not registered with IANA,
	/// and the "+json" structured suffix of RFC 6839, so that any generic consumer can still
	/// parse the body as JSON even if it does not understand the envelope semantics.
	/// </remarks>
	public const string ResponseJson = "application/vnd.fuxion.response+json";
}
