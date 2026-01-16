using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fuxion;

/// <summary>
/// Represents a semantic version following the Semantic Versioning 2.0.0 specification.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a complete implementation of Semantic Versioning (SemVer) as defined in the
/// <see href="https://semver.org/#semantic-versioning-specification-semver">Semantic Versioning Specification</see>.
/// </para>
/// <para>
/// <strong>Version format:</strong> MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA]
/// </para>
/// <list type="bullet">
/// <item><description><strong>MAJOR:</strong> Incremented for incompatible API changes</description></item>
/// <item><description><strong>MINOR:</strong> Incremented for backwards-compatible functionality additions</description></item>
/// <item><description><strong>PATCH:</strong> Incremented for backwards-compatible bug fixes</description></item>
/// <item><description><strong>PRERELEASE:</strong> Optional dot-separated identifiers (e.g., alpha.1, beta.2, rc.1)</description></item>
/// <item><description><strong>BUILDMETADATA:</strong> Optional dot-separated identifiers for build information</description></item>
/// </list>
/// <para>
/// <strong>Precedence rules (comparison):</strong>
/// </para>
/// <list type="number">
/// <item><description>Versions are compared by MAJOR, MINOR, and PATCH in order</description></item>
/// <item><description>Pre-release versions have lower precedence than normal versions (1.0.0-alpha &lt; 1.0.0)</description></item>
/// <item><description>Pre-release identifiers are compared lexicographically (numeric identifiers compared as integers)</description></item>
/// <item><description>Build metadata is ignored for precedence determination</description></item>
/// </list>
/// <para>
/// The class uses the official SemVer regex from the specification (adapted for C# by removing 'P' prefixes from named groups).
/// Framework compatibility: Uses GeneratedRegexAttribute on .NET 7+ for performance, falls back to traditional Regex on older frameworks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating versions
/// var v1 = new SemanticVersion("1.0.0");
/// var v2 = new SemanticVersion("1.2.3-alpha.1+build.123");
/// var v3 = new SemanticVersion("2.0.0-rc.1");
/// 
/// // Parsing with validation
/// if (SemanticVersion.TryParse("1.0.0-beta", out var version))
/// {
///     Console.WriteLine($"Valid version: {version}");
/// }
/// 
/// // Accessing components
/// Console.WriteLine($"Major: {v2.Major}");           // 1
/// Console.WriteLine($"Minor: {v2.Minor}");           // 2
/// Console.WriteLine($"Patch: {v2.Patch}");           // 3
/// Console.WriteLine($"PreRelease: {v2.PreRelease}"); // alpha.1
/// Console.WriteLine($"Build: {v2.BuildMetadata}");   // build.123
/// 
/// // Comparison
/// bool isNewer = v3 > v1;          // true
/// bool isSame = v1 == v2;          // false
/// int comparison = v1.CompareTo(v2); // -1 (v1 is older)
/// 
/// // Pre-release ordering
/// var alpha = new SemanticVersion("1.0.0-alpha");
/// var beta = new SemanticVersion("1.0.0-beta");
/// var release = new SemanticVersion("1.0.0");
/// // alpha &lt; beta &lt; release
/// 
/// // Implicit conversions
/// SemanticVersion version = "1.0.0";     // string to SemanticVersion
/// string versionString = version;         // SemanticVersion to string
/// </code>
/// </example>
public partial class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SemanticVersion"/> class from a version string.
	/// </summary>
	/// <param name="semanticVersion">
	/// A version string conforming to the Semantic Versioning 2.0.0 specification.
	/// Format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA]
	/// </param>
	/// <exception cref="SemanticVersionException">
	/// Thrown when <paramref name="semanticVersion"/> does not conform to the SemVer specification.
	/// </exception>
	/// <remarks>
	/// <para>
	/// The version string is parsed using the official SemVer regex pattern. All components are validated:
	/// </para>
	/// <list type="bullet">
	/// <item><description>MAJOR, MINOR, PATCH must be non-negative integers without leading zeros (except "0" itself)</description></item>
	/// <item><description>PRERELEASE identifiers must contain only [0-9A-Za-z-] and cannot be empty between dots</description></item>
	/// <item><description>BUILDMETADATA identifiers must contain only [0-9A-Za-z-] and cannot be empty between dots</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Valid versions
	/// var v1 = new SemanticVersion("1.0.0");
	/// var v2 = new SemanticVersion("0.0.1");
	/// var v3 = new SemanticVersion("1.2.3-alpha");
	/// var v4 = new SemanticVersion("1.2.3-alpha.1");
	/// var v5 = new SemanticVersion("1.2.3+build.123");
	/// var v6 = new SemanticVersion("1.2.3-beta.2+sha.5114f85");
	/// 
	/// // Invalid versions (throw SemanticVersionException)
	/// // var invalid1 = new SemanticVersion("v1.0.0");        // No 'v' prefix allowed
	/// // var invalid2 = new SemanticVersion("1.0");           // Must have all three components
	/// // var invalid3 = new SemanticVersion("01.0.0");        // No leading zeros
	/// // var invalid4 = new SemanticVersion("1.0.0-");        // Empty prerelease identifier
	/// </code>
	/// </example>
	public SemanticVersion(string semanticVersion)
	{
		this.semanticVersion = semanticVersion;
		var m = SemanticVersionRegex().Match(semanticVersion);
		if (!m.Success) throw new SemanticVersionException($"String '{semanticVersion}' isn't a valid semantic version pattern");
		var major = m.Groups["major"];
		var minor = m.Groups["minor"];
		var patch = m.Groups["patch"];
		var preRelease = m.Groups["prerelease"];
		var buildMetadata = m.Groups["buildmetadata"];
		Major = uint.Parse(major.Value);
		Minor = uint.Parse(minor.Value);
		Patch = uint.Parse(patch.Value);
		PreRelease = preRelease.Value.IsNullOrWhiteSpace()
			? new([])
			: new(preRelease.Value.Split('.')
				.Select(i => new SemanticVersionIdentifier(i))
				.ToArray());
		BuildMetadata = buildMetadata.Value.IsNullOrWhiteSpace()
			? new([])
			: new(buildMetadata.Value.Split('.')
				.Select(i => new SemanticVersionIdentifier(i))
				.ToArray());
	}
	readonly string semanticVersion;
	
	/// <summary>
	/// Gets the major version number.
	/// </summary>
	/// <value>A non-negative integer representing incompatible API changes.</value>
	/// <remarks>
	/// <para>
	/// According to SemVer specification:
	/// </para>
	/// <list type="bullet">
	/// <item><description>MUST be incremented if any backwards incompatible changes are introduced</description></item>
	/// <item><description>MINOR and PATCH MUST be reset to 0 when MAJOR is incremented</description></item>
	/// <item><description>MAJOR version zero (0.y.z) is for initial development; anything may change</description></item>
	/// <item><description>Version 1.0.0 defines the public API</description></item>
	/// </list>
	/// </remarks>
	public uint Major { get; } // Must be non negative integer
	
	/// <summary>
	/// Gets the minor version number.
	/// </summary>
	/// <value>A non-negative integer representing backwards-compatible functionality additions.</value>
	/// <remarks>
	/// <para>
	/// According to SemVer specification:
	/// </para>
	/// <list type="bullet">
	/// <item><description>MUST be incremented if new, backwards-compatible functionality is introduced</description></item>
	/// <item><description>MUST be incremented if any public API functionality is marked as deprecated</description></item>
	/// <item><description>MAY be incremented if substantial new functionality or improvements are introduced within private code</description></item>
	/// <item><description>PATCH MUST be reset to 0 when MINOR is incremented</description></item>
	/// </list>
	/// </remarks>
	public uint Minor { get; } // Must be non negative integer
	
	/// <summary>
	/// Gets the patch version number.
	/// </summary>
	/// <value>A non-negative integer representing backwards-compatible bug fixes.</value>
	/// <remarks>
	/// <para>
	/// According to SemVer specification:
	/// </para>
	/// <list type="bullet">
	/// <item><description>MUST be incremented if only backwards-compatible bug fixes are introduced</description></item>
	/// <item><description>A bug fix is defined as an internal change that fixes incorrect behavior</description></item>
	/// </list>
	/// </remarks>
	public uint Patch { get; } // Must be non negative integer
	
	/// <summary>
	/// Gets the pre-release version identifiers.
	/// </summary>
	/// <value>
	/// A collection of dot-separated identifiers, or empty if no pre-release version is specified.
	/// </value>
	/// <remarks>
	/// <para>
	/// Pre-release versions indicate that the version is unstable and might not satisfy the intended compatibility requirements.
	/// </para>
	/// <para>
	/// Identifier rules:
	/// </para>
	/// <list type="bullet">
	/// <item><description>MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-]</description></item>
	/// <item><description>MUST NOT be empty</description></item>
	/// <item><description>Numeric identifiers MUST NOT include leading zeroes</description></item>
	/// </list>
	/// <para>
	/// Common pre-release identifiers: alpha, beta, rc (release candidate), followed by numeric versions (e.g., alpha.1, beta.2, rc.1).
	/// </para>
	/// <para>
	/// Precedence: When comparing versions, pre-release versions have lower precedence than the associated normal version.
	/// Example: 1.0.0-alpha &lt; 1.0.0
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var alpha = new SemanticVersion("1.0.0-alpha");
	/// var beta = new SemanticVersion("1.0.0-beta.2");
	/// var rc = new SemanticVersion("1.0.0-rc.1");
	/// var release = new SemanticVersion("1.0.0");
	/// 
	/// // Ordering: alpha &lt; beta.2 &lt; rc.1 &lt; release
	/// Console.WriteLine(alpha.PreRelease.Count);      // 1: ["alpha"]
	/// Console.WriteLine(beta.PreRelease.Count);       // 2: ["beta", "2"]
	/// Console.WriteLine(release.PreRelease.Count);    // 0: []
	/// </code>
	/// </example>
	public SemanticVersionIdentifierCollection PreRelease { get; } // A series of dot separated identifiers. Identifiers MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-]
	
	/// <summary>
	/// Gets the build metadata identifiers.
	/// </summary>
	/// <value>
	/// A collection of dot-separated identifiers for build metadata, or empty if no build metadata is specified.
	/// </value>
	/// <remarks>
	/// <para>
	/// Build metadata MAY be denoted by appending a plus sign and a series of dot separated identifiers
	/// immediately following the patch or pre-release version.
	/// </para>
	/// <para>
	/// Identifier rules:
	/// </para>
	/// <list type="bullet">
	/// <item><description>MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-]</description></item>
	/// <item><description>MUST NOT be empty</description></item>
	/// </list>
	/// <para>
	/// <strong>Important:</strong> Build metadata SHOULD be ignored when determining version precedence.
	/// Thus, two versions that differ only in build metadata have the same precedence.
	/// Examples: 1.0.0+build.1 and 1.0.0+build.2 are considered equal for comparison purposes.
	/// </para>
	/// <para>
	/// Common use cases: build numbers, commit SHA, timestamps (e.g., +20130313144700, +sha.5114f85, +build.123).
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var v1 = new SemanticVersion("1.0.0+build.1");
	/// var v2 = new SemanticVersion("1.0.0+build.2");
	/// var v3 = new SemanticVersion("1.0.0+sha.5114f85");
	/// 
	/// Console.WriteLine(v1.BuildMetadata.Count);  // 1: ["build.1"] (stored as single identifier)
	/// Console.WriteLine(v3.BuildMetadata.Count);  // 1: ["sha.5114f85"]
	/// 
	/// // Build metadata ignored in comparison
	/// Console.WriteLine(v1 == v2);  // true (same precedence)
	/// </code>
	/// </example>
	public SemanticVersionIdentifierCollection BuildMetadata { get; } // A series of dot separated identifiers. Identifiers MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-]
	
	/// <summary>
	/// Compares this instance with a specified object and returns an indication of their relative values.
	/// </summary>
	/// <param name="obj">An object to compare, or null.</param>
	/// <returns>
	/// A signed integer that indicates the relative order:
	/// Less than zero if this instance precedes <paramref name="obj"/>, 
	/// zero if they have the same precedence,
	/// greater than zero if this instance follows <paramref name="obj"/> or <paramref name="obj"/> is null.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not null and not a <see cref="SemanticVersion"/>.</exception>
	public int CompareTo(object? obj)
	{
		if (obj is null) return 1;
		var other = obj as SemanticVersion ?? throw new ArgumentException($"Type must be '{nameof(SemanticVersion)}'", "obj");
		return CompareTo(other);
	}
	
	/// <summary>
	/// Compares this instance with another <see cref="SemanticVersion"/> and returns an indication of their relative precedence.
	/// </summary>
	/// <param name="other">A <see cref="SemanticVersion"/> to compare with this instance, or null.</param>
	/// <returns>
	/// A signed integer that indicates the relative precedence according to SemVer specification:
	/// Less than zero if this instance precedes <paramref name="other"/>, 
	/// zero if they have the same precedence,
	/// greater than zero if this instance follows <paramref name="other"/> or <paramref name="other"/> is null.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Precedence is determined by the first difference when comparing:
	/// </para>
	/// <list type="number">
	/// <item><description>MAJOR versions (higher is greater)</description></item>
	/// <item><description>MINOR versions (higher is greater)</description></item>
	/// <item><description>PATCH versions (higher is greater)</description></item>
	/// <item><description>Pre-release versions (absence is greater than presence, then lexicographic comparison)</description></item>
	/// </list>
	/// <para>
	/// Build metadata is explicitly ignored during comparison per SemVer specification.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var v1 = new SemanticVersion("1.0.0");
	/// var v2 = new SemanticVersion("1.0.0-alpha");
	/// var v3 = new SemanticVersion("1.0.1");
	/// var v4 = new SemanticVersion("1.1.0");
	/// var v5 = new SemanticVersion("2.0.0");
	/// 
	/// // Pre-release &lt; release
	/// Console.WriteLine(v2.CompareTo(v1));  // -1 (alpha &lt; release)
	/// 
	/// // Patch comparison
	/// Console.WriteLine(v1.CompareTo(v3));  // -1 (1.0.0 &lt; 1.0.1)
	/// 
	/// // Minor comparison
	/// Console.WriteLine(v3.CompareTo(v4));  // -1 (1.0.1 &lt; 1.1.0)
	/// 
	/// // Major comparison
	/// Console.WriteLine(v4.CompareTo(v5));  // -1 (1.1.0 &lt; 2.0.0)
	/// </code>
	/// </example>
	public int CompareTo(SemanticVersion? other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		if (Major > other.Major) return 1;
		if (other.Major > Major) return -1;
		if (Minor > other.Minor) return 1;
		if (other.Minor > Minor) return -1;
		if (Patch > other.Patch) return 1;
		if (other.Patch > Patch) return -1;
		return (PreRelease.Count, other.PreRelease.Count) switch
		{
			(0, > 0) => 1,
			(> 0, 0) => -1,
			var _ => PreRelease.CompareTo(other.PreRelease)
		};
	}
	
	/// <summary>
	/// Determines whether this instance and another <see cref="SemanticVersion"/> have the same value.
	/// </summary>
	/// <param name="other">The <see cref="SemanticVersion"/> to compare with this instance.</param>
	/// <returns>
	/// true if the Major, Minor, Patch, and PreRelease components of <paramref name="other"/> 
	/// are equal to this instance; otherwise, false.
	/// </returns>
	/// <remarks>
	/// <para>
	/// <strong>Important:</strong> Build metadata is explicitly ignored during equality comparison per SemVer specification.
	/// Two versions differing only in build metadata are considered equal.
	/// </para>
	/// <para>
	/// Example: 1.0.0+build.1 == 1.0.0+build.2 returns true.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var v1 = new SemanticVersion("1.0.0-alpha");
	/// var v2 = new SemanticVersion("1.0.0-alpha");
	/// var v3 = new SemanticVersion("1.0.0-alpha+build.1");
	/// var v4 = new SemanticVersion("1.0.0-alpha+build.2");
	/// var v5 = new SemanticVersion("1.0.0-beta");
	/// 
	/// Console.WriteLine(v1.Equals(v2));  // true (identical)
	/// Console.WriteLine(v3.Equals(v4));  // true (build metadata ignored)
	/// Console.WriteLine(v1.Equals(v5));  // false (different prerelease)
	/// </code>
	/// </example>
	public bool Equals(SemanticVersion? other) => other is not null && Major.Equals(other.Major) && Minor.Equals(other.Minor) && Patch.Equals(other.Patch) && PreRelease.Equals(other.PreRelease);
	
	// Official regex from https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
	// Official regex must be adapted to work in c# by removing 'P' before each group name
	// You can try with this regex on https://regex101.com/r/P3smVG/1
	const string RegexPattern = @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
#if STANDARD_OR_OLD_FRAMEWORKS
	internal static Regex SemanticVersionRegex() => new(RegexPattern);
#else
	/// <summary>
	/// Gets the compiled regex pattern for validating and parsing semantic version strings.
	/// </summary>
	/// <returns>A <see cref="Regex"/> instance for matching SemVer strings.</returns>
	/// <remarks>
	/// <para>
	/// This method uses different implementations based on the target framework:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>.NET 7+:</strong> Uses <see cref="GeneratedRegexAttribute"/> for source-generated, high-performance regex</description></item>
	/// <item><description><strong>Older frameworks:</strong> Creates a new Regex instance on each call (cached by the runtime)</description></item>
	/// </list>
	/// <para>
	/// The regex pattern is the official SemVer regex from the specification, adapted for C#
	/// by removing 'P' prefixes from named groups.
	/// </para>
	/// <para>
	/// Test the pattern: <see href="https://regex101.com/r/P3smVG/1"/>
	/// </para>
	/// </remarks>
	[GeneratedRegex(RegexPattern)]
	internal static partial Regex SemanticVersionRegex();
#endif
	
	/// <summary>
	/// Returns the string representation of this semantic version.
	/// </summary>
	/// <returns>
	/// The original version string used to create this instance, preserving the exact format.
	/// </returns>
	/// <remarks>
	/// The returned string is the same string that was passed to the constructor or TryParse method,
	/// ensuring format preservation for round-tripping.
	/// </remarks>
	public override string ToString() => semanticVersion;
	
	/// <summary>
	/// Attempts to parse a string into a <see cref="SemanticVersion"/>.
	/// </summary>
	/// <param name="semanticVersion">The string to parse.</param>
	/// <param name="result">
	/// When this method returns, contains the parsed <see cref="SemanticVersion"/> if parsing succeeded,
	/// or null if parsing failed.
	/// </param>
	/// <returns>true if <paramref name="semanticVersion"/> was successfully parsed; otherwise, false.</returns>
	/// <remarks>
	/// This method provides a safe way to parse version strings without throwing exceptions.
	/// Use this instead of the constructor when you need to validate user input or handle invalid versions gracefully.
	/// </remarks>
	/// <example>
	/// <code>
	/// // Safe parsing
	/// if (SemanticVersion.TryParse("1.0.0-beta", out var version))
	/// {
	///     Console.WriteLine($"Valid version: {version}");
	/// }
	/// else
	/// {
	///     Console.WriteLine("Invalid version format");
	/// }
	/// 
	/// // Validating user input
	/// string userInput = GetUserInput();
	/// if (!SemanticVersion.TryParse(userInput, out var parsedVersion))
	/// {
	///     Console.WriteLine("Please enter a valid semantic version (e.g., 1.0.0)");
	///     return;
	/// }
	/// 
	/// // Continue with validated version
	/// ProcessVersion(parsedVersion);
	/// </code>
	/// </example>
	public static bool TryParse(string semanticVersion, [MaybeNullWhen(returnValue: false)] out SemanticVersion result)
	{
		if (SemanticVersionRegex().IsMatch(semanticVersion))
		{
			result = new(semanticVersion);
			return true;
		}
		result = null;
		return false;
	}
	
	/// <summary>
	/// Converts a <see cref="SemanticVersion"/> to its string representation.
	/// </summary>
	/// <param name="version">The semantic version to convert.</param>
	/// <returns>The string representation of the version.</returns>
	public static implicit operator string(SemanticVersion version)=> version.ToString();
	
	/// <summary>
	/// Converts a string to a <see cref="SemanticVersion"/>.
	/// </summary>
	/// <param name="version">The version string to convert.</param>
	/// <returns>A new <see cref="SemanticVersion"/> instance.</returns>
	/// <exception cref="SemanticVersionException">Thrown if the string is not a valid semantic version.</exception>
	/// <remarks>
	/// This operator enables implicit conversion from strings to SemanticVersion:
	/// <code>SemanticVersion version = "1.0.0";</code>
	/// </remarks>
	public static implicit operator SemanticVersion(string version) => new(version);
	
	/// <summary>
	/// Determines whether two <see cref="SemanticVersion"/> objects have the same value.
	/// </summary>
	/// <param name="version1">The first version to compare.</param>
	/// <param name="version2">The second version to compare.</param>
	/// <returns>true if the versions are equal; otherwise, false.</returns>
	/// <remarks>
	/// Build metadata is ignored in comparison. Two versions differing only in build metadata are considered equal.
	/// </remarks>
	public static bool operator ==(SemanticVersion? version1, SemanticVersion? version2)
	{
		if (version1 is null) return version2 is null;
		return version1.Equals(version2);
	}
	
	/// <summary>
	/// Determines whether two <see cref="SemanticVersion"/> objects have different values.
	/// </summary>
	/// <param name="version1">The first version to compare.</param>
	/// <param name="version2">The second version to compare.</param>
	/// <returns>true if the versions are not equal; otherwise, false.</returns>
	public static bool operator !=(SemanticVersion version1, SemanticVersion version2) => !(version1 == version2);
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersion"/> is less than another.
	/// </summary>
	/// <param name="version1">The first version to compare.</param>
	/// <param name="version2">The second version to compare.</param>
	/// <returns>true if <paramref name="version1"/> precedes <paramref name="version2"/>; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="version1"/> is null.</exception>
	/// <remarks>
	/// Comparison follows SemVer precedence rules. Pre-release versions have lower precedence than release versions.
	/// </remarks>
	public static bool operator <(SemanticVersion version1, SemanticVersion version2)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		if (version1 is null) throw new ArgumentException(nameof(version1));
#else
		ArgumentNullException.ThrowIfNull(version1);
#endif
		return version1.CompareTo(version2) < 0;
	}
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersion"/> is less than or equal to another.
	/// </summary>
	/// <param name="version1">The first version to compare.</param>
	/// <param name="version2">The second version to compare.</param>
	/// <returns>true if <paramref name="version1"/> precedes or equals <paramref name="version2"/>; otherwise, false.</returns>
	public static bool operator <=(SemanticVersion version1, SemanticVersion version2) => version1 == version2 || version1 < version2;
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersion"/> is greater than another.
	/// </summary>
	/// <param name="version1">The first version to compare.</param>
	/// <param name="version2">The second version to compare.</param>
	/// <returns>true if <paramref name="version1"/> follows <paramref name="version2"/>; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="version1"/> is null.</exception>
	public static bool operator >(SemanticVersion version1, SemanticVersion version2)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		if (version1 is null) throw new ArgumentException(nameof(version1));
#else
		ArgumentNullException.ThrowIfNull(version1);
#endif
		return version2 < version1;
	}
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersion"/> is greater than or equal to another.
	/// </summary>
	/// <param name="version1">The first version to compare.</param>
	/// <param name="version2">The second version to compare.</param>
	/// <returns>true if <paramref name="version1"/> follows or equals <paramref name="version2"/>; otherwise, false.</returns>
	public static bool operator >=(SemanticVersion version1, SemanticVersion version2) => version1 == version2 || version1 > version2;
	
	/// <summary>
	/// Determines whether the specified object is equal to the current <see cref="SemanticVersion"/>.
	/// </summary>
	/// <param name="obj">The object to compare with the current version.</param>
	/// <returns>true if the specified object is a <see cref="SemanticVersion"/> and is equal to this instance; otherwise, false.</returns>
	public override bool Equals(object? obj)
	{
		var collection = obj as SemanticVersion;
		return collection is not null && Equals(collection);
	}
	
	/// <summary>
	/// Returns the hash code for this <see cref="SemanticVersion"/>.
	/// </summary>
	/// <returns>A 32-bit signed integer hash code.</returns>
	/// <remarks>
	/// The hash code is computed from Major, Minor, Patch, and PreRelease components.
	/// Build metadata is excluded to maintain consistency with equality comparison.
	/// </remarks>
	public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);
}