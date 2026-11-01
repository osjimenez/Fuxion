using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Fuxion.Types;

/// <summary>
/// The identity of a type: its key, an <c>https</c> URI that ends with its major version, or a construction of a generic
/// type, its key followed by its type arguments between brackets.
/// </summary>
/// <remarks>
/// <para>
/// A key has one canonical form, <c>https://&lt;host&gt;/&lt;segment&gt;[/&lt;segment&gt;…]/v&lt;major&gt;</c>: the
/// host in lowercase ASCII (an international name, in punycode), without port or user; at least one segment before the
/// version, each in kebab-case ASCII (<c>[a-z0-9]+(-[a-z0-9]+)*</c>); the last segment, the major version
/// (<c>v0</c>, <c>v1</c>…, without leading zeros); and no query, fragment, trailing slash or percent-encoding. A
/// construction is a key followed by its arguments, in order, between brackets and separated by commas without spaces;
/// each argument is a key or a construction:
/// <c>https://meta.fuxion.nexus/system/dictionary/v1[https://meta.fuxion.nexus/system/string/v1,https://meta.fuxion.nexus/erp/invoice/v1]</c>.
/// </para>
/// <para>
/// Whatever is not in that form is rejected, never normalized, so the same key cannot be written in two ways. Two
/// <see cref="UriKey"/> are equal when their texts are, character by character. Only the major version is in the key:
/// the exact revision of a type, its base and its interfaces are in its <see cref="TypeDefinition"/>.
/// </para>
/// <para>In JSON, a <see cref="UriKey"/> is a string with its canonical form.</para>
/// </remarks>
[JsonConverter(typeof(UriKeyJsonConverter))]
public sealed class UriKey : IEquatable<UriKey>
{
	/// <summary>The most levels a construction nests (as many as JSON by default): a text received cannot exhaust the stack.</summary>
	public const int MaxDepth = 64;

	UriKey(string text, string keyText, uint major, UriKey[] arguments, int depth)
	{
		this.text = text;
		this.keyText = keyText;
		Major = major;
		this.arguments = arguments;
		Depth = depth;
	}

	readonly string text;
	readonly string keyText;
	readonly UriKey[] arguments;
	UriKey? key;
	string? fingerprint;

	/// <summary>The key of the type, without arguments: this same <see cref="UriKey"/> when it is not a construction.</summary>
	public UriKey Key => key ??= arguments.Length == 0 ? this : new(keyText, keyText, Major, [], 0);

	/// <summary>The type arguments of a construction, in order; none for a key.</summary>
	public IReadOnlyList<UriKey> Arguments => arguments;

	/// <summary>Whether it is a construction of a generic type, with its arguments, and not a key.</summary>
	public bool IsConstruction => arguments.Length > 0;

	/// <summary>The major version of the type, the last segment of its key.</summary>
	public uint Major { get; }

	/// <summary>The lineage of the type, the conceptual family that crosses its major versions: its key without the major version.</summary>
	public string Lineage => keyText.Substring(0, keyText.LastIndexOf('/'));

	/// <summary>
	/// The fingerprint of its canonical form: the SHA-256 of its UTF-8, truncated to its first 128 bits, in base64url without
	/// padding (22 characters). A compact form for parties that share a registry; what is signed is the canonical form.
	/// </summary>
	public string Fingerprint => fingerprint ??= ComputeFingerprint(text);

	// The levels it nests: 0 for a key
	internal int Depth { get; }

	/// <summary>Reads a <see cref="UriKey"/> in its canonical form.</summary>
	/// <param name="text">A key or a construction, in its canonical form.</param>
	/// <returns>The <see cref="UriKey"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	/// <exception cref="FormatException"><paramref name="text"/> is not in the canonical form; the message says why.</exception>
	public static UriKey Parse(string text)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));
		return TryParse(text, out var key, out var error) ? key : throw new FormatException(error);
	}

	/// <summary>Reads a <see cref="UriKey"/> in its canonical form, if it is.</summary>
	/// <param name="text">A key or a construction, in its canonical form.</param>
	/// <param name="key">The <see cref="UriKey"/>, when it is in the canonical form.</param>
	/// <returns>Whether <paramref name="text"/> is in the canonical form.</returns>
	public static bool TryParse([NotNullWhen(true)] string? text, [NotNullWhen(true)] out UriKey? key) => TryParse(text, out key, out _);

	internal static bool TryParse([NotNullWhen(true)] string? text, [NotNullWhen(true)] out UriKey? key, [NotNullWhen(false)] out string? error)
	{
		key = null;
		if (string.IsNullOrEmpty(text))
		{
			error = "A UriKey cannot be empty.";
			return false;
		}
		var position = 0;
		if (!TryParse(text!, ref position, 0, out var parsed, out var reason))
		{
			error = $"'{text}' is not a UriKey in its canonical form: {reason}.";
			return false;
		}
		if (position != text!.Length)
		{
			error = $"'{text}' is not a UriKey in its canonical form: '{text[position]}' at position {position} was not expected.";
			return false;
		}
		key = parsed;
		error = null;
		return true;
	}

	// key [ "[" uriKey *( "," uriKey ) "]" ], from position
	static bool TryParse(string text, ref int position, int level, [NotNullWhen(true)] out UriKey? key, [NotNullWhen(false)] out string? error)
	{
		key = null;
		var start = position;
		while (position < text.Length && text[position] is not ('[' or ',' or ']')) position++;
		if (position == start)
		{
			error = $"a key is missing at position {start}";
			return false;
		}
		if (!UriKeyGrammar.TryValidateKey(text, start, position, out var major, out var keyError))
		{
			error = $"{keyError} ('{text.Substring(start, position - start)}')";
			return false;
		}
		var keyEnd = position;
		if (position == text.Length || text[position] != '[')
		{
			var keyText = text.Substring(start, keyEnd - start);
			key = new(keyText, keyText, major, [], 0);
			error = null;
			return true;
		}
		if (level + 1 > MaxDepth)
		{
			error = $"a construction nests {MaxDepth} levels at most";
			return false;
		}
		position++;
		List<UriKey> arguments = [];
		var depth = 0;
		while (true)
		{
			if (!TryParse(text, ref position, level + 1, out var argument, out error)) return false;
			arguments.Add(argument);
			depth = Math.Max(depth, argument.Depth);
			if (position == text.Length)
			{
				error = "a construction closes its arguments with ']'";
				return false;
			}
			var separator = text[position++];
			if (separator == ']') break;
			if (separator != ',')
			{
				error = $"'{separator}' at position {position - 1} was not expected";
				return false;
			}
		}
		key = new(text.Substring(start, position - start), text.Substring(start, keyEnd - start), major, arguments.ToArray(), depth + 1);
		error = null;
		return true;
	}

	/// <summary>The construction of this generic type with its type arguments, in order.</summary>
	/// <param name="arguments">The type arguments: keys or constructions.</param>
	/// <returns>The construction, <c>key[argument,…]</c>.</returns>
	/// <exception cref="InvalidOperationException">This <see cref="UriKey"/> is a construction already, not a key.</exception>
	/// <exception cref="ArgumentException">There are no arguments, or the construction would nest more than <see cref="MaxDepth"/> levels.</exception>
	/// <remarks>Whether the type takes that many arguments is for its definition to say (<see cref="TypeRegistry.IsValid"/>).</remarks>
	public UriKey Construct(params UriKey[] arguments)
	{
		if (arguments is null) throw new ArgumentNullException(nameof(arguments));
		if (IsConstruction) throw new InvalidOperationException($"'{this}' is a construction already: construct from its key, '{keyText}'.");
		if (arguments.Length == 0) throw new ArgumentException("A construction has at least one type argument.", nameof(arguments));
		var builder = new StringBuilder(keyText).Append('[');
		var depth = 0;
		for (var i = 0; i < arguments.Length; i++)
		{
			var argument = arguments[i] ?? throw new ArgumentException($"The type argument {i} is null.", nameof(arguments));
			if (i > 0) builder.Append(',');
			builder.Append(argument.text);
			depth = Math.Max(depth, argument.Depth);
		}
		if (depth + 1 > MaxDepth) throw new ArgumentException($"A construction nests {MaxDepth} levels at most.", nameof(arguments));
		return new(builder.Append(']').ToString(), keyText, Major, (UriKey[])arguments.Clone(), depth + 1);
	}

	static string ComputeFingerprint(string canonical)
	{
		using var sha = SHA256.Create();
		var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
		return Convert.ToBase64String(hash, 0, 16).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	/// <summary>Its canonical form.</summary>
	/// <returns>The key, or the construction, as text.</returns>
	public override string ToString() => text;

	/// <summary>Whether it is the same key or construction: the same text, character by character.</summary>
	/// <param name="other">The other <see cref="UriKey"/>.</param>
	/// <returns>Whether both texts are equal.</returns>
	public bool Equals(UriKey? other) => other is not null && string.Equals(text, other.text, StringComparison.Ordinal);

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is UriKey other && Equals(other);

	/// <inheritdoc/>
	public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(text);

	/// <summary>Whether two <see cref="UriKey"/> are the same key or construction.</summary>
	/// <param name="left">One.</param>
	/// <param name="right">The other.</param>
	/// <returns>Whether both texts are equal, or both are <see langword="null"/>.</returns>
	public static bool operator ==(UriKey? left, UriKey? right) => left is null ? right is null : left.Equals(right);

	/// <summary>Whether two <see cref="UriKey"/> are different keys or constructions.</summary>
	/// <param name="left">One.</param>
	/// <param name="right">The other.</param>
	/// <returns>Whether their texts differ.</returns>
	public static bool operator !=(UriKey? left, UriKey? right) => !(left == right);
}
