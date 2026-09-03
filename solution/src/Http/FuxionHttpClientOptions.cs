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
	/// The Accept header to send. Without preferences it is plain JSON, so a Fuxion client is
	/// indistinguishable from any other; with preferences the vendor types come first and plain JSON
	/// stays as a fallback so a server that does not know them never answers 406.
	/// </summary>
	public string BuildAccept()
	{
		if (!PreferEnvelope && !PreferNativeErrors)
			return ResponseMediaTypes.Json;

		var parts = new List<string>(3);
		if (PreferEnvelope) parts.Add(ResponseMediaTypes.ResponseJson);
		if (PreferNativeErrors) parts.Add(ResponseMediaTypes.ErrorJson);
		parts.Add($"{ResponseMediaTypes.Json};q=0.9");
		return string.Join(", ", parts);
	}
}
