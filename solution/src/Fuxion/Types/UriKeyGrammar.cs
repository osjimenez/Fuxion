using System.Diagnostics.CodeAnalysis;

namespace Fuxion.Types;

// The grammar of a key, in its only canonical form:
//
//   https://<host>/<segment>[/<segment>…]/v<major>
//
// - The host, in lowercase ASCII (an international name, in punycode), without port or user: labels of [a-z0-9-] that
//   neither start nor end with '-', of 63 characters at most, 253 in all.
// - At least one segment before the version, each in kebab-case ASCII: [a-z0-9]+(-[a-z0-9]+)*.
// - The last segment is the major version: v0, v1, v2…, without leading zeros.
// - No query, no fragment, no trailing slash and no percent-encoding.
//
// Whatever is not in that form is rejected, never normalized. The same file is compiled into Fuxion and into its
// generator of type definitions (Fuxion.Analyzers), which cannot depend on Fuxion: one grammar, in one place.
static class UriKeyGrammar
{
	internal const string Scheme = "https://";
	const int MaxHostLength = 253;
	const int MaxLabelLength = 63;

	// Validates the key in text[start..end): the whole of it, without arguments.
	internal static bool TryValidateKey(string text, int start, int end, out uint major, [NotNullWhen(false)] out string? error)
	{
		major = 0;
		var length = end - start;
		if (length <= 0)
		{
			error = "a key cannot be empty";
			return false;
		}
		if (length < Scheme.Length || string.CompareOrdinal(text, start, Scheme, 0, Scheme.Length) != 0)
		{
			error = "a key starts with 'https://', in lowercase";
			return false;
		}
		for (var i = start; i < end; i++)
		{
			var problem = text[i] switch
			{
				'?' => "a key has no query",
				'#' => "a key has no fragment",
				'%' => "a key has no percent-encoding",
				'@' => "a key has no user information",
				_ => null
			};
			if (problem is not null)
			{
				error = problem;
				return false;
			}
		}

		// The host, up to the first slash
		var hostStart = start + Scheme.Length;
		var hostEnd = text.IndexOf('/', hostStart, end - hostStart);
		if (hostEnd < 0)
		{
			error = "a key has at least one segment and its major version after its host";
			return false;
		}
		if (!TryValidateHost(text, hostStart, hostEnd, out error)) return false;

		// The segments, the last of them the major version
		var segmentStart = hostEnd + 1;
		var segments = 0;
		while (true)
		{
			var slash = text.IndexOf('/', segmentStart, end - segmentStart);
			var segmentEnd = slash < 0 ? end : slash;
			if (segmentEnd == segmentStart)
			{
				error = slash < 0 ? "a key has no trailing slash" : "a key has no empty segments";
				return false;
			}
			if (slash < 0)
			{
				if (segments == 0)
				{
					error = "a key has at least one segment before its major version";
					return false;
				}
				return TryParseMajor(text, segmentStart, segmentEnd, out major, out error);
			}
			if (!IsKebabCase(text, segmentStart, segmentEnd))
			{
				error = $"the segment '{text.Substring(segmentStart, segmentEnd - segmentStart)}' is not in kebab-case ASCII, [a-z0-9]+(-[a-z0-9]+)*";
				return false;
			}
			segments++;
			segmentStart = segmentEnd + 1;
		}
	}

	static bool TryValidateHost(string text, int start, int end, [NotNullWhen(false)] out string? error)
	{
		if (end == start)
		{
			error = "a key has a host";
			return false;
		}
		if (end - start > MaxHostLength)
		{
			error = $"the host of a key has {MaxHostLength} characters at most";
			return false;
		}
		var labelStart = start;
		for (var i = start; i <= end; i++)
		{
			if (i < end && text[i] != '.')
			{
				var c = text[i];
				if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-') continue;
				error = c switch
				{
					>= 'A' and <= 'Z' => "the host of a key is in lowercase",
					':' => "the host of a key has no port",
					_ => "the host of a key is ASCII letters, digits, hyphens and dots: an international name goes in punycode"
				};
				return false;
			}
			var labelLength = i - labelStart;
			if (labelLength == 0)
			{
				error = "the host of a key has no empty labels";
				return false;
			}
			if (labelLength > MaxLabelLength)
			{
				error = $"each label of the host of a key has {MaxLabelLength} characters at most";
				return false;
			}
			if (text[labelStart] == '-' || text[i - 1] == '-')
			{
				error = "a label of the host of a key neither starts nor ends with '-'";
				return false;
			}
			labelStart = i + 1;
		}
		error = null;
		return true;
	}

	static bool TryParseMajor(string text, int start, int end, out uint major, [NotNullWhen(false)] out string? error)
	{
		major = 0;
		const string expected = "the last segment of a key is its major version: v0, v1, v2…, without leading zeros";
		if (end - start < 2 || text[start] != 'v' || (text[start + 1] == '0' && end - start > 2))
		{
			error = expected;
			return false;
		}
		ulong value = 0;
		for (var i = start + 1; i < end; i++)
		{
			var c = text[i];
			if (c is < '0' or > '9')
			{
				error = expected;
				return false;
			}
			value = value * 10 + (uint)(c - '0');
			if (value > uint.MaxValue)
			{
				error = $"the major version of a key is {uint.MaxValue} at most";
				return false;
			}
		}
		major = (uint)value;
		error = null;
		return true;
	}

	// [a-z0-9]+(-[a-z0-9]+)*
	static bool IsKebabCase(string text, int start, int end)
	{
		if (text[start] == '-' || text[end - 1] == '-') return false;
		for (var i = start; i < end; i++)
		{
			var c = text[i];
			if (c is >= 'a' and <= 'z' or >= '0' and <= '9') continue;
			if (c == '-' && text[i - 1] != '-') continue;
			return false;
		}
		return true;
	}
}
