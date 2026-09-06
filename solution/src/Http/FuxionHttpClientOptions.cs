namespace Fuxion.Http;

using Fuxion.Union;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// What a <see cref="FuxionHttpClient"/> asks for and how it reads. Declared once at registration, so
/// no call site repeats the JSON options or the Accept header.
/// </summary>
public sealed class FuxionHttpClientOptions
{
	/// <summary>Options used to read bodies. Web defaults match what an ASP.NET Core server writes.</summary>
	public JsonSerializerOptions JsonOptions { get; set; } = new(JsonSerializerDefaults.Web);

	/// <summary>Ask the server for the full response envelope instead of the bare payload.</summary>
	public bool PreferEnvelope { get; set; }

	/// <summary>Ask the server for native Fuxion errors instead of problem+json.</summary>
	public bool PreferNativeErrors { get; set; }

	/// <summary>
	/// Naming policy to ask for on Fuxion media types (one of the ResponseNaming values); null = the server's own.
	/// </summary>
	/// <remarks>
	/// The server only ever stamps a requested naming on shapes that announce it through the vnd media
	/// type's <c>naming</c> parameter - the envelope, a native error, and the Unit type - never on a bare
	/// payload (<c>application/json</c>) or a typed business error's raw value, since those shapes carry no
	/// parameter for a client to notice the transcoding by. Setting <see cref="PreferNaming"/> alone,
	/// without <see cref="PreferEnvelope"/>, therefore has no visible effect on a plain payload response: it
	/// keeps arriving under the server's own policy. To get the requested naming applied to every response,
	/// set <see cref="PreferEnvelope"/> to <see langword="true"/> as well, so the payload always travels
	/// nested inside the (naming-announcing) envelope.
	/// </remarks>
	public string? PreferNaming { get; set; }

	/// <summary>
	/// The Accept header to send. Without preferences it is plain JSON, so a Fuxion client is
	/// indistinguishable from any other; with preferences the vendor types come first and plain JSON
	/// stays as a fallback so a server that does not know them never answers 406.
	/// </summary>
	public string BuildAccept()
	{
		var naming = PreferNaming is { } n && ResponseNaming.IsSupported(n) ? $"; {ResponseMediaTypes.NamingParameter}={n.ToLowerInvariant()}" : string.Empty;
		if (!PreferEnvelope && !PreferNativeErrors && naming.Length == 0)
			return ResponseMediaTypes.Json;

		var parts = new List<string>(3);
		if (PreferEnvelope) parts.Add(ResponseMediaTypes.ResponseJson + naming);
		if (PreferNativeErrors) parts.Add(ResponseMediaTypes.ErrorJson + naming);
		// A naming preference needs a Fuxion type to ride on; the unit type changes no shape, so it is the neutral carrier.
		if (parts.Count == 0) parts.Add(ResponseMediaTypes.UnitJson + naming);
		parts.Add($"{ResponseMediaTypes.Json};q=0.9");
		return string.Join(", ", parts);
	}
}
