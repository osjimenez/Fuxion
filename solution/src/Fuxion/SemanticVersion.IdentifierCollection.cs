using System;
using System.Linq;

namespace Fuxion;

/// <summary>
/// Represents a collection of semantic version identifiers used in pre-release or build metadata components.
/// </summary>
/// <remarks>
/// <para>
/// According to the Semantic Versioning 2.0.0 specification, pre-release versions and build metadata
/// consist of dot-separated identifiers. This class represents such a collection and implements
/// the comparison logic defined in the SemVer specification for identifier sequences.
/// </para>
/// <para>
/// <strong>Comparison rules (precedence):</strong>
/// </para>
/// <list type="number">
/// <item><description>A larger set of pre-release fields has higher precedence than a smaller set, if all preceding identifiers are equal</description></item>
/// <item><description>Identifiers are compared left-to-right until a difference is found</description></item>
/// <item><description>Each identifier is compared according to <see cref="SemanticVersionIdentifier"/> rules (numeric &lt; alphanumeric)</description></item>
/// </list>
/// <para>
/// <strong>Examples of precedence:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>1.0.0-alpha &lt; 1.0.0-alpha.1 (shorter &lt; longer when prefix matches)</description></item>
/// <item><description>1.0.0-alpha.1 &lt; 1.0.0-alpha.beta (numeric &lt; alphanumeric)</description></item>
/// <item><description>1.0.0-alpha.beta &lt; 1.0.0-beta (lexicographic comparison)</description></item>
/// <item><description>1.0.0-beta &lt; 1.0.0-beta.2 (shorter &lt; longer)</description></item>
/// <item><description>1.0.0-beta.2 &lt; 1.0.0-beta.11 (numeric comparison: 2 &lt; 11)</description></item>
/// <item><description>1.0.0-beta.11 &lt; 1.0.0-rc.1 (lexicographic: "beta" &lt; "rc")</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Creating collections
/// var empty = new SemanticVersionIdentifierCollection(Array.Empty&lt;SemanticVersionIdentifier&gt;());
/// var alpha = new SemanticVersionIdentifierCollection(new[]
/// {
///     new SemanticVersionIdentifier("alpha")
/// });
/// var alphaOne = new SemanticVersionIdentifierCollection(new[]
/// {
///     new SemanticVersionIdentifier("alpha"),
///     new SemanticVersionIdentifier("1")
/// });
/// var beta = new SemanticVersionIdentifierCollection(new[]
/// {
///     new SemanticVersionIdentifier("beta"),
///     new SemanticVersionIdentifier("2")
/// });
/// 
/// // Accessing properties
/// Console.WriteLine(empty.Count);      // 0
/// Console.WriteLine(alpha.Count);      // 1
/// Console.WriteLine(alphaOne.Count);   // 2
/// 
/// // String representation (dot-separated)
/// Console.WriteLine(empty.ToString());      // "" (empty string)
/// Console.WriteLine(alpha.ToString());      // "alpha"
/// Console.WriteLine(alphaOne.ToString());   // "alpha.1"
/// Console.WriteLine(beta.ToString());       // "beta.2"
/// 
/// // Comparison (SemVer precedence rules)
/// Console.WriteLine(alpha &lt; alphaOne);   // true (shorter &lt; longer when prefix matches)
/// Console.WriteLine(alphaOne &lt; beta);    // true ("alpha" &lt; "beta")
/// Console.WriteLine(beta.CompareTo(alpha)); // 1 (beta > alpha)
/// 
/// // Equality
/// var alphaTwo = new SemanticVersionIdentifierCollection(new[]
/// {
///     new SemanticVersionIdentifier("alpha")
/// });
/// Console.WriteLine(alpha == alphaTwo);  // true (same identifiers)
/// </code>
/// </example>
/// <seealso cref="SemanticVersion"/>
/// <seealso cref="SemanticVersionIdentifier"/>
public class SemanticVersionIdentifierCollection(SemanticVersionIdentifier[] identifiers) : IComparable, IComparable<SemanticVersionIdentifierCollection>, IEquatable<SemanticVersionIdentifierCollection>
{
	readonly SemanticVersionIdentifier[] identifiers = identifiers;
	
	/// <summary>
	/// Gets the number of identifiers in this collection.
	/// </summary>
	/// <value>The count of <see cref="SemanticVersionIdentifier"/> objects in the collection.</value>
	/// <remarks>
	/// <para>
	/// An empty collection (count = 0) indicates no pre-release or build metadata was specified
	/// in the semantic version. For pre-release versions, an empty collection means this is a
	/// release version (not a pre-release).
	/// </para>
	/// <para>
	/// According to SemVer precedence rules, when comparing pre-release versions, a longer
	/// identifier collection has higher precedence than a shorter one if all preceding
	/// identifiers are equal. For example: 1.0.0-alpha &lt; 1.0.0-alpha.1
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var release = new SemanticVersion("1.0.0");
	/// var preRelease = new SemanticVersion("1.0.0-alpha.1.beta");
	/// 
	/// Console.WriteLine(release.PreRelease.Count);     // 0 (no pre-release)
	/// Console.WriteLine(preRelease.PreRelease.Count);  // 3 (alpha, 1, beta)
	/// </code>
	/// </example>
	public int Count => identifiers.Length;
	
	/// <summary>
	/// Returns the string representation of this identifier collection.
	/// </summary>
	/// <returns>
	/// A dot-separated string of all identifiers, or an empty string if the collection is empty.
	/// </returns>
	/// <remarks>
	/// <para>
	/// The string format follows the SemVer specification: identifiers are joined with dots,
	/// and any leading or trailing dots are trimmed. An empty collection returns an empty string.
	/// </para>
	/// <para>
	/// This method preserves the original identifier values, including their exact casing and format.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var empty = new SemanticVersionIdentifierCollection(Array.Empty&lt;SemanticVersionIdentifier&gt;());
	/// var single = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha")
	/// });
	/// var multiple = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("1"),
	///     new SemanticVersionIdentifier("beta")
	/// });
	/// 
	/// Console.WriteLine(empty.ToString());     // "" (empty)
	/// Console.WriteLine(single.ToString());    // "alpha"
	/// Console.WriteLine(multiple.ToString());  // "alpha.1.beta"
	/// </code>
	/// </example>
	public override string ToString() => identifiers.Aggregate("", (a, c) => a + "." + c, a => a.Trim('.'));
	
	/// <summary>
	/// Determines whether two <see cref="SemanticVersionIdentifierCollection"/> objects are equal.
	/// </summary>
	/// <param name="collection1">The first collection to compare.</param>
	/// <param name="collection2">The second collection to compare.</param>
	/// <returns>true if the collections contain the same identifiers in the same order; otherwise, false.</returns>
	/// <remarks>
	/// Equality is determined by comparing all identifiers in sequence using <see cref="Enumerable.SequenceEqual{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>.
	/// Both the values and order of identifiers must match for collections to be considered equal.
	/// </remarks>
	public static bool operator ==(SemanticVersionIdentifierCollection? collection1, SemanticVersionIdentifierCollection? collection2)
	{
		if (collection1 is null) return collection2 is null;
		return collection1.Equals(collection2);
	}
	
	/// <summary>
	/// Determines whether two <see cref="SemanticVersionIdentifierCollection"/> objects are not equal.
	/// </summary>
	/// <param name="collection1">The first collection to compare.</param>
	/// <param name="collection2">The second collection to compare.</param>
	/// <returns>true if the collections differ in any identifier or order; otherwise, false.</returns>
	public static bool operator !=(SemanticVersionIdentifierCollection collection1, SemanticVersionIdentifierCollection collection2) => !(collection1 == collection2);
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifierCollection"/> has lower precedence than another
	/// according to SemVer comparison rules.
	/// </summary>
	/// <param name="collection1">The first collection to compare.</param>
	/// <param name="collection2">The second collection to compare.</param>
	/// <returns>true if <paramref name="collection1"/> has lower precedence than <paramref name="collection2"/>; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="collection1"/> is null.</exception>
	/// <remarks>
	/// <para>
	/// Comparison follows SemVer precedence rules for identifier sequences:
	/// </para>
	/// <list type="number">
	/// <item><description>Compare identifiers left-to-right until a difference is found</description></item>
	/// <item><description>If one collection is a prefix of the other, the shorter has lower precedence</description></item>
	/// <item><description>Each identifier comparison follows <see cref="SemanticVersionIdentifier"/> rules</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// var alpha = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha")
	/// });
	/// var alphaOne = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("1")
	/// });
	/// var beta = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("beta")
	/// });
	/// 
	/// Console.WriteLine(alpha &lt; alphaOne);  // true (shorter &lt; longer)
	/// Console.WriteLine(alphaOne &lt; beta);   // true (alpha &lt; beta)
	/// Console.WriteLine(beta &lt; alpha);      // false
	/// </code>
	/// </example>
	public static bool operator < (SemanticVersionIdentifierCollection collection1, SemanticVersionIdentifierCollection collection2)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		if (collection1 is null) throw new ArgumentException(nameof(collection1));
#else
		ArgumentNullException.ThrowIfNull(collection1);
#endif
		return collection1.CompareTo(collection2) < 0;
	}
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifierCollection"/> has lower or equal precedence compared to another.
	/// </summary>
	/// <param name="collection1">The first collection to compare.</param>
	/// <param name="collection2">The second collection to compare.</param>
	/// <returns>true if <paramref name="collection1"/> has lower or equal precedence; otherwise, false.</returns>
	public static bool operator <=(SemanticVersionIdentifierCollection collection1, SemanticVersionIdentifierCollection collection2) => collection1 == collection2 || collection1 < collection2;
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifierCollection"/> has higher precedence than another
	/// according to SemVer comparison rules.
	/// </summary>
	/// <param name="collection1">The first collection to compare.</param>
	/// <param name="collection2">The second collection to compare.</param>
	/// <returns>true if <paramref name="collection1"/> has higher precedence than <paramref name="collection2"/>; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="collection1"/> is null.</exception>
	public static bool operator > (SemanticVersionIdentifierCollection collection1, SemanticVersionIdentifierCollection collection2)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		if (collection1 is null) throw new ArgumentException(nameof(collection1));
#else
		ArgumentNullException.ThrowIfNull(collection1);
#endif
		return collection2 < collection1;
	}
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifierCollection"/> has higher or equal precedence compared to another.
	/// </summary>
	/// <param name="collection1">The first collection to compare.</param>
	/// <param name="collection2">The second collection to compare.</param>
	/// <returns>true if <paramref name="collection1"/> has higher or equal precedence; otherwise, false.</returns>
	public static bool operator >=(SemanticVersionIdentifierCollection collection1, SemanticVersionIdentifierCollection collection2) => collection1 == collection2 || collection1 > collection2;
	
	/// <summary>
	/// Compares this collection with another and returns an indication of their relative precedence.
	/// </summary>
	/// <param name="other">The collection to compare with this instance.</param>
	/// <returns>
	/// A signed integer indicating the relative precedence:
	/// Less than zero if this collection has lower precedence,
	/// zero if they have the same precedence,
	/// greater than zero if this collection has higher precedence.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method implements the SemVer precedence rules for identifier collections:
	/// </para>
	/// <list type="number">
	/// <item><description>Iterate through identifiers left-to-right comparing each pair</description></item>
	/// <item><description>First non-equal identifier comparison determines the result</description></item>
	/// <item><description>If one collection ends first (is a prefix), it has lower precedence</description></item>
	/// <item><description>If all identifiers are equal, collections have the same precedence</description></item>
	/// </list>
	/// <para>
	/// <strong>Special case:</strong> If <paramref name="other"/> is null, this instance has higher precedence (-1 is returned
	/// as per SemVer spec where absence of pre-release has higher precedence than presence).
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var alpha = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha")
	/// });
	/// var alpha1 = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("1")
	/// });
	/// var alpha2 = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("2")
	/// });
	/// var beta = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("beta")
	/// });
	/// 
	/// Console.WriteLine(alpha.CompareTo(alpha1));   // -1 (shorter &lt; longer when prefix matches)
	/// Console.WriteLine(alpha1.CompareTo(alpha2));  // -1 (1 &lt; 2 numerically)
	/// Console.WriteLine(alpha2.CompareTo(beta));    // -1 ("alpha" &lt; "beta" lexically)
	/// Console.WriteLine(beta.CompareTo(alpha));     //  1 ("beta" > "alpha")
	/// Console.WriteLine(alpha.CompareTo(alpha));    //  0 (equal)
	/// </code>
	/// </example>
	public int CompareTo(SemanticVersionIdentifierCollection? other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return -1;
		for (var i = 0; i < System.Math.Max(identifiers.Length, other.identifiers.Length); i++)
		{
			if (identifiers.Length < i + 1) return -1;
			if (other.identifiers.Length < i + 1) return 1;
			var res = identifiers[i]
				.CompareTo(other.identifiers[i]);
			if (res != 0) return res;
		}
		return 0;
	}
	
	/// <summary>
	/// Compares this instance with a specified object and returns an indication of their relative values.
	/// </summary>
	/// <param name="value">An object to compare, or null.</param>
	/// <returns>
	/// A signed integer that indicates the relative order:
	/// Less than zero if this instance precedes <paramref name="value"/>,
	/// zero if they have the same precedence,
	/// greater than zero if this instance follows <paramref name="value"/> or <paramref name="value"/> is null.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not null and not a <see cref="SemanticVersionIdentifierCollection"/>.</exception>
	public int CompareTo(object? value)
	{
		if (value is null)
			return 1;
		var other = value as SemanticVersionIdentifierCollection
			?? throw new ArgumentException($"Type must be '{nameof(SemanticVersionIdentifierCollection)}'", nameof(value));
		return CompareTo(other);
	}
	
	/// <summary>
	/// Determines whether this collection is equal to another.
	/// </summary>
	/// <param name="other">The <see cref="SemanticVersionIdentifierCollection"/> to compare with this instance.</param>
	/// <returns>true if all identifiers are equal in value and order; otherwise, false.</returns>
	/// <remarks>
	/// Equality is determined using <see cref="Enumerable.SequenceEqual{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>,
	/// which compares each identifier in sequence. Both count and order matter for equality.
	/// </remarks>
	/// <example>
	/// <code>
	/// var collection1 = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("1")
	/// });
	/// var collection2 = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("1")
	/// });
	/// var collection3 = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("alpha"),
	///     new SemanticVersionIdentifier("2")
	/// });
	/// var collection4 = new SemanticVersionIdentifierCollection(new[]
	/// {
	///     new SemanticVersionIdentifier("1"),
	///     new SemanticVersionIdentifier("alpha")
	/// });
	/// 
	/// Console.WriteLine(collection1.Equals(collection2));  // true (same identifiers)
	/// Console.WriteLine(collection1.Equals(collection3));  // false (different value: "1" vs "2")
	/// Console.WriteLine(collection1.Equals(collection4));  // false (different order)
	/// </code>
	/// </example>
	public bool Equals(SemanticVersionIdentifierCollection? other) => other is not null && identifiers.SequenceEqual(other.identifiers);
	
	/// <summary>
	/// Determines whether the specified object is equal to the current <see cref="SemanticVersionIdentifierCollection"/>.
	/// </summary>
	/// <param name="obj">The object to compare with the current collection.</param>
	/// <returns>true if the specified object is a <see cref="SemanticVersionIdentifierCollection"/> and is equal to this instance; otherwise, false.</returns>
	public override bool Equals(object? obj)
	{
		var collection = obj as SemanticVersionIdentifierCollection;
		return collection is not null && Equals(collection);
	}
	
	/// <summary>
	/// Returns the hash code for this <see cref="SemanticVersionIdentifierCollection"/>.
	/// </summary>
	/// <returns>A 32-bit signed integer hash code.</returns>
	/// <remarks>
	/// The hash code is computed from the underlying identifier array to ensure that
	/// equal collections have the same hash code.
	/// </remarks>
	public override int GetHashCode() => identifiers.GetHashCode();
}