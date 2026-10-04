using System;
using System.Net.Http.Headers;
using System.Text;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Media types of the union response wire contract. The body format is always announced through
/// Content-Type, never through a custom header, because Content-Type is the one header no
/// intermediary strips.
/// </summary>
public static class ResponseMediaTypes
{
	/// <summary>Bare payload. Never carries parameters: it must stay byte-identical to a vanilla API.</summary>
	public const string Json = "application/json";

	public const string TextJson = "text/json";

	/// <summary>RFC 9457 ProblemDetails. Standard, so no parameters either.</summary>
	public const string ProblemJson = "application/problem+json";

	/// <summary>The full response envelope (vendor tree, RFC 6838; "+json" suffix, RFC 6839).</summary>
	public const string ResponseJson = "application/vnd.fuxion.response+json";

	/// <summary>A bare native <see cref="Error"/>. Distinct shape from a payload, so it gets its own type.</summary>
	public const string ErrorJson = "application/vnd.fuxion.error+json";

	/// <summary>
	/// A <see cref="Unit"/> result: a 200 with a two-byte body so no intermediary can normalize it
	/// into a 204 and confuse it with <see cref="None"/>.
	/// </summary>
	public const string UnitJson = "application/vnd.fuxion.unit+json";

	/// <summary>Media type parameter carrying the JSON naming policy used to write the body.</summary>
	public const string NamingParameter = "naming";

	const string VendorPrefix = "application/vnd.fuxion.";

	/// <summary>Whether <paramref name="contentType"/> has the given media type, ignoring parameters and case.</summary>
	public static bool Is(string? contentType, string mediaType)
		=> TryParse(contentType, out var parsed)
			&& string.Equals(parsed.MediaType, mediaType, StringComparison.OrdinalIgnoreCase);

	/// <summary>Whether <paramref name="contentType"/> belongs to the Fuxion vendor tree.</summary>
	public static bool IsFuxion(string? contentType)
		=> TryParse(contentType, out var parsed)
			&& parsed.MediaType is { } media
			&& media.StartsWith(VendorPrefix, StringComparison.OrdinalIgnoreCase);

	// The one answer to "is this body JSON?" for the client and both adapters: application/json, text/json
	// and any structured +json type (RFC 6839), which covers problem+json and the vendor types.
	public static bool IsJson(string? contentType)
		=> TryParse(contentType, out var parsed)
			&& parsed.MediaType is { } media
			&& (media.Equals(Json, StringComparison.OrdinalIgnoreCase)
				|| media.Equals(TextJson, StringComparison.OrdinalIgnoreCase)
				|| media.EndsWith("+json", StringComparison.OrdinalIgnoreCase));

	// The encodings a JSON request body may declare in its charset parameter, the same as ASP.NET Core's own JSON
	// input: UTF-8 (also when there is no charset) and UTF-16 little endian. Anything else is refused with a 415
	// by both adapters. RFC 8259 asks for UTF-8 between systems, so the rest is legacy that is not worth guessing.
	public static bool TryGetRequestEncoding(string? charset, out Encoding encoding)
	{
		switch (charset?.Trim('"').Trim().ToLowerInvariant())
		{
			case null or "" or "utf-8" or "utf8":
				encoding = Encoding.UTF8;
				return true;
			case "utf-16" or "utf-16le" or "unicode":
				encoding = Encoding.Unicode;
				return true;
			default:
				encoding = null!;
				return false;
		}
	}

	internal static bool TryParse(string? contentType, out MediaTypeHeaderValue parsed)
	{
		parsed = null!;
		return !string.IsNullOrWhiteSpace(contentType) && MediaTypeHeaderValue.TryParse(contentType, out parsed!);
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
