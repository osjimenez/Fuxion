using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// The <c>naming</c> media type parameter. Its absence means camelCase (the Web default), the same
/// way the absence of charset in application/json means UTF-8 by RFC 8259.
/// </summary>
public static class ResponseNaming
{
	public const string Camel = "camel";
	public const string Pascal = "pascal";
	public const string Snake = "snake";
	public const string Kebab = "kebab";
	public const string SnakeUpper = "snake-upper";
	public const string KebabUpper = "kebab-upper";

	/// <summary>A policy the wire cannot name: the client must supply matching options and can infer nothing.</summary>
	public const string Custom = "custom";

	static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<string, JsonSerializerOptions>> cache = new();

	/// <summary>Maps a policy to its parameter value. Never null: an unknown policy is announced as <see cref="Custom"/>.</summary>
	public static string FromPolicy(JsonNamingPolicy? policy)
	{
		if (policy is null) return Pascal;
		if (ReferenceEquals(policy, JsonNamingPolicy.CamelCase)) return Camel;
		if (ReferenceEquals(policy, JsonNamingPolicy.SnakeCaseLower)) return Snake;
		if (ReferenceEquals(policy, JsonNamingPolicy.SnakeCaseUpper)) return SnakeUpper;
		if (ReferenceEquals(policy, JsonNamingPolicy.KebabCaseLower)) return Kebab;
		if (ReferenceEquals(policy, JsonNamingPolicy.KebabCaseUpper)) return KebabUpper;
		return Custom;
	}

	/// <summary>Maps a parameter value to its policy. Pascal maps to a null policy; <see cref="Custom"/> is not resolvable.</summary>
	public static bool TryGetPolicy(string naming, out JsonNamingPolicy? policy)
	{
		switch (naming?.ToLowerInvariant())
		{
			case Camel: policy = JsonNamingPolicy.CamelCase; return true;
			case Pascal: policy = null; return true;
			case Snake: policy = JsonNamingPolicy.SnakeCaseLower; return true;
			case SnakeUpper: policy = JsonNamingPolicy.SnakeCaseUpper; return true;
			case Kebab: policy = JsonNamingPolicy.KebabCaseLower; return true;
			case KebabUpper: policy = JsonNamingPolicy.KebabCaseUpper; return true;
			default: policy = null; return false;
		}
	}

	/// <summary>Whether the value is one of the six namings a request or a response can be read with.</summary>
	public static bool IsSupported(string? naming) => naming is not null && TryGetPolicy(naming, out _);

	/// <summary>snake/kebab (lower or upper) bodies need their separators removed before a camel/pascal binder can match them.</summary>
	public static bool NeedsSeparatorTranscoding(string? naming)
	{
		if (naming is null) return false;
		var n = naming.ToLowerInvariant();
		return n is Snake or SnakeUpper or Kebab or KebabUpper;
	}

	/// <summary>Reads the <c>naming</c> parameter of a content type, or null when absent.</summary>
	public static string? GetParameter(string? contentType)
	{
		if (!ResponseMediaTypes.TryParse(contentType, out var parsed)) return null;
		foreach (var parameter in parsed.Parameters)
			if (string.Equals(parameter.Name, ResponseMediaTypes.NamingParameter, StringComparison.OrdinalIgnoreCase))
				return parameter.Value?.Trim('"');
		return null;
	}

	/// <summary>Appends the <c>naming</c> parameter for <paramref name="policy"/> to a Fuxion media type.</summary>
	public static string WithNaming(string mediaType, JsonNamingPolicy? policy)
		=> $"{mediaType}; {ResponseMediaTypes.NamingParameter}={FromPolicy(policy)}";

	/// <summary>
	/// Returns options that read with the announced naming policy. The source is never mutated;
	/// a clone per (source, naming) pair is cached because creating options per call destroys the
	/// System.Text.Json metadata cache.
	/// </summary>
	public static JsonSerializerOptions Apply(JsonSerializerOptions source, string? naming)
	{
		if (naming is null || !TryGetPolicy(naming, out var policy)) return source;
		if (ReferenceEquals(source.PropertyNamingPolicy, policy)) return source;

		var perSource = cache.GetValue(source, static _ => new ConcurrentDictionary<string, JsonSerializerOptions>(StringComparer.OrdinalIgnoreCase));
		return perSource.GetOrAdd(naming, _ => new JsonSerializerOptions(source) { PropertyNamingPolicy = policy });
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
