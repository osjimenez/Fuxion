using System;

namespace Fuxion;

/// <summary>
/// Represents a single identifier within a semantic version's pre-release or build metadata component.
/// </summary>
/// <remarks>
/// <para>
/// According to the Semantic Versioning 2.0.0 specification, pre-release versions and build metadata
/// consist of dot-separated identifiers. This class represents one such identifier and implements
/// the comparison rules defined in the SemVer specification.
/// </para>
/// <para>
/// <strong>Identifier rules (per SemVer spec):</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Identifiers MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-]</description></item>
/// <item><description>Identifiers MUST NOT be empty</description></item>
/// <item><description>Numeric identifiers MUST NOT include leading zeroes</description></item>
/// </list>
/// <para>
/// <strong>Comparison rules (precedence):</strong>
/// </para>
/// <list type="number">
/// <item><description>Identifiers with only digits are compared numerically</description></item>
/// <item><description>Identifiers with letters or hyphens are compared lexically in ASCII sort order</description></item>
/// <item><description>Numeric identifiers always have lower precedence than non-numeric identifiers</description></item>
/// </list>
/// <para>
/// Examples: 1 &lt; 2 &lt; 10 &lt; alpha &lt; beta (numeric comparison for numbers, then alphabetic)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating identifiers
/// var numeric = new SemanticVersionIdentifier("123");
/// var alphanumeric = new SemanticVersionIdentifier("alpha");
/// var mixed = new SemanticVersionIdentifier("beta2");
/// 
/// // Checking if numeric
/// Console.WriteLine(numeric.IsNumber);      // true
/// Console.WriteLine(numeric.NumberValue);   // 123
/// Console.WriteLine(alphanumeric.IsNumber); // false
/// Console.WriteLine(alphanumeric.NumberValue); // null
/// 
/// // Comparison (SemVer rules)
/// var id1 = new SemanticVersionIdentifier("1");
/// var id2 = new SemanticVersionIdentifier("10");
/// var id3 = new SemanticVersionIdentifier("alpha");
/// 
/// Console.WriteLine(id1 &lt; id2);    // true (1 &lt; 10 numerically)
/// Console.WriteLine(id2 &lt; id3);    // true (numeric &lt; alphanumeric)
/// Console.WriteLine(id3.CompareTo(id1)); // 1 (alphanumeric > numeric)
/// 
/// // String representation
/// Console.WriteLine(numeric.ToString());  // "123"
/// Console.WriteLine(mixed.ToString());    // "beta2"
/// </code>
/// </example>
/// <seealso cref="SemanticVersion"/>
/// <seealso cref="SemanticVersionIdentifierCollection"/>
public class SemanticVersionIdentifier : IComparable, IComparable<SemanticVersionIdentifier>, IEquatable<SemanticVersionIdentifier>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SemanticVersionIdentifier"/> class.
	/// </summary>
	/// <param name="identifier">
	/// The string value of the identifier. Must comply with SemVer rules:
	/// Only [0-9A-Za-z-] characters allowed, cannot be empty.
	/// </param>
	/// <remarks>
	/// <para>
	/// The constructor automatically determines if the identifier is purely numeric by attempting
	/// to parse it as a <see cref="uint"/>. This classification affects comparison behavior:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Numeric identifiers (e.g., "0", "1", "123") are compared numerically</description></item>
	/// <item><description>Non-numeric identifiers (e.g., "alpha", "rc1", "build-123") are compared lexically</description></item>
	/// </list>
	/// <para>
	/// <strong>Note:</strong> This constructor does not validate that the identifier string conforms
	/// to SemVer rules. Validation is performed when parsing the full semantic version string.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Numeric identifiers
	/// var id1 = new SemanticVersionIdentifier("0");
	/// var id2 = new SemanticVersionIdentifier("123");
	/// Console.WriteLine(id1.IsNumber);      // true
	/// Console.WriteLine(id2.NumberValue);   // 123
	/// 
	/// // Non-numeric identifiers
	/// var id3 = new SemanticVersionIdentifier("alpha");
	/// var id4 = new SemanticVersionIdentifier("rc-1");
	/// var id5 = new SemanticVersionIdentifier("01");  // Leading zero - treated as non-numeric
	/// Console.WriteLine(id3.IsNumber);      // false
	/// Console.WriteLine(id4.IsNumber);      // false
	/// Console.WriteLine(id5.IsNumber);      // false (leading zeros not allowed in numeric identifiers)
	/// </code>
	/// </example>
	public SemanticVersionIdentifier(string identifier)
	{
		Value = identifier;
		IsNumber = uint.TryParse(identifier, out var numberValue);
		if (IsNumber) NumberValue = numberValue;
	}
	
	/// <summary>
	/// Gets the string value of this identifier.
	/// </summary>
	/// <value>The original identifier string as provided to the constructor.</value>
	/// <remarks>
	/// This property preserves the exact format of the identifier, including any leading
	/// characters that might make it non-numeric (e.g., "01" is stored as-is, not converted to "1").
	/// </remarks>
	public string Value { get; }
	
	/// <summary>
	/// Gets a value indicating whether this identifier is purely numeric.
	/// </summary>
	/// <value>
	/// true if the identifier contains only digits and can be parsed as a <see cref="uint"/>;
	/// otherwise, false.
	/// </value>
	/// <remarks>
	/// <para>
	/// An identifier is considered numeric only if:
	/// </para>
	/// <list type="bullet">
	/// <item><description>It contains only digits [0-9]</description></item>
	/// <item><description>It can be successfully parsed as an unsigned 32-bit integer</description></item>
	/// <item><description>It does not have leading zeros (per SemVer specification)</description></item>
	/// </list>
	/// <para>
	/// This distinction is crucial for comparison: numeric identifiers are compared numerically
	/// (1 &lt; 2 &lt; 10), while non-numeric identifiers are compared lexically (1 &lt; 10 &lt; 2).
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var numeric = new SemanticVersionIdentifier("123");
	/// var withLeadingZero = new SemanticVersionIdentifier("01");
	/// var alphanumeric = new SemanticVersionIdentifier("alpha1");
	/// 
	/// Console.WriteLine(numeric.IsNumber);           // true
	/// Console.WriteLine(withLeadingZero.IsNumber);   // false (leading zero)
	/// Console.WriteLine(alphanumeric.IsNumber);      // false (contains letters)
	/// </code>
	/// </example>
	public bool IsNumber { get; }
	
	/// <summary>
	/// Gets the numeric value of this identifier if it is numeric; otherwise, null.
	/// </summary>
	/// <value>
	/// A <see cref="uint"/> representing the numeric value if <see cref="IsNumber"/> is true;
	/// otherwise, null.
	/// </value>
	/// <remarks>
	/// <para>
	/// This property is only populated when the identifier can be successfully parsed as a
	/// non-negative integer without leading zeros. It is used for efficient numeric comparison
	/// of numeric identifiers during precedence determination.
	/// </para>
	/// <para>
	/// For non-numeric identifiers, this property is null and comparison is performed lexically
	/// using the <see cref="Value"/> property instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var numeric = new SemanticVersionIdentifier("42");
	/// var alphanumeric = new SemanticVersionIdentifier("beta");
	/// 
	/// Console.WriteLine(numeric.NumberValue.HasValue);     // true
	/// Console.WriteLine(numeric.NumberValue.Value);        // 42
	/// Console.WriteLine(alphanumeric.NumberValue.HasValue); // false
	/// Console.WriteLine(alphanumeric.NumberValue);         // null
	/// </code>
	/// </example>
	public uint? NumberValue { get; }
	
	/// <summary>
	/// Returns the string representation of this identifier.
	/// </summary>
	/// <returns>The <see cref="Value"/> of this identifier.</returns>
	/// <remarks>
	/// This method simply returns the original identifier string, preserving its exact format.
	/// </remarks>
	public override string ToString() => Value;
	
	/// <summary>
	/// Determines whether two <see cref="SemanticVersionIdentifier"/> objects are equal.
	/// </summary>
	/// <param name="identifier1">The first identifier to compare.</param>
	/// <param name="identifier2">The second identifier to compare.</param>
	/// <returns>true if the identifiers have the same value; otherwise, false.</returns>
	/// <remarks>
	/// Equality is determined by comparing the <see cref="Value"/> strings.
	/// Numeric vs. non-numeric classification does not affect equality.
	/// </remarks>
	public static bool operator ==(SemanticVersionIdentifier? identifier1, SemanticVersionIdentifier? identifier2)
	{
		if (identifier1 is null) return identifier2 is null;
		return identifier1.Equals(identifier2);
	}
	
	/// <summary>
	/// Determines whether two <see cref="SemanticVersionIdentifier"/> objects are not equal.
	/// </summary>
	/// <param name="identifier1">The first identifier to compare.</param>
	/// <param name="identifier2">The second identifier to compare.</param>
	/// <returns>true if the identifiers have different values; otherwise, false.</returns>
	public static bool operator !=(SemanticVersionIdentifier identifier1, SemanticVersionIdentifier identifier2) => !(identifier1 == identifier2);
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifier"/> is less than another
	/// according to SemVer precedence rules.
	/// </summary>
	/// <param name="identifier1">The first identifier to compare.</param>
	/// <param name="identifier2">The second identifier to compare.</param>
	/// <returns>true if <paramref name="identifier1"/> has lower precedence than <paramref name="identifier2"/>; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="identifier1"/> is null.</exception>
	/// <remarks>
	/// <para>
	/// Comparison follows SemVer precedence rules:
	/// </para>
	/// <list type="number">
	/// <item><description>Numeric identifiers are compared as integers (1 &lt; 2 &lt; 10)</description></item>
	/// <item><description>Alphanumeric identifiers are compared lexically (ASCII sort order)</description></item>
	/// <item><description>Numeric identifiers always have lower precedence than alphanumeric (10 &lt; alpha)</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// var id1 = new SemanticVersionIdentifier("1");
	/// var id2 = new SemanticVersionIdentifier("10");
	/// var id3 = new SemanticVersionIdentifier("alpha");
	/// var id4 = new SemanticVersionIdentifier("beta");
	/// 
	/// Console.WriteLine(id1 &lt; id2);    // true (1 &lt; 10 numerically)
	/// Console.WriteLine(id2 &lt; id3);    // true (numeric &lt; alphanumeric)
	/// Console.WriteLine(id3 &lt; id4);    // true ("alpha" &lt; "beta" lexically)
	/// </code>
	/// </example>
	public static bool operator < (SemanticVersionIdentifier identifier1, SemanticVersionIdentifier identifier2)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		if(identifier1 is null) throw new ArgumentException(nameof(identifier1));
#else
		ArgumentNullException.ThrowIfNull(identifier1);
#endif
		return identifier1.CompareTo(identifier2) < 0;
	}
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifier"/> is less than or equal to another.
	/// </summary>
	/// <param name="identifier1">The first identifier to compare.</param>
	/// <param name="identifier2">The second identifier to compare.</param>
	/// <returns>true if <paramref name="identifier1"/> has lower or equal precedence compared to <paramref name="identifier2"/>; otherwise, false.</returns>
	public static bool operator <=(SemanticVersionIdentifier identifier1, SemanticVersionIdentifier identifier2) => identifier1 == identifier2 || identifier1 < identifier2;
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifier"/> is greater than another
	/// according to SemVer precedence rules.
	/// </summary>
	/// <param name="identifier1">The first identifier to compare.</param>
	/// <param name="identifier2">The second identifier to compare.</param>
	/// <returns>true if <paramref name="identifier1"/> has higher precedence than <paramref name="identifier2"/>; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="identifier1"/> is null.</exception>
	public static bool operator > (SemanticVersionIdentifier identifier1, SemanticVersionIdentifier identifier2)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		if(identifier1 is null) throw new ArgumentException(nameof(identifier1));
#else
		ArgumentNullException.ThrowIfNull(identifier1);
#endif
		return identifier2 < identifier1;
	}
	
	/// <summary>
	/// Determines whether one <see cref="SemanticVersionIdentifier"/> is greater than or equal to another.
	/// </summary>
	/// <param name="identifier1">The first identifier to compare.</param>
	/// <param name="identifier2">The second identifier to compare.</param>
	/// <returns>true if <paramref name="identifier1"/> has higher or equal precedence compared to <paramref name="identifier2"/>; otherwise, false.</returns>
	public static bool operator >=(SemanticVersionIdentifier identifier1, SemanticVersionIdentifier identifier2) => identifier1 == identifier2 || identifier1 > identifier2;
	
	/// <summary>
	/// Compares this identifier with another and returns an indication of their relative precedence.
	/// </summary>
	/// <param name="other">The identifier to compare with this instance.</param>
	/// <returns>
	/// A signed integer indicating the relative precedence:
	/// Less than zero if this identifier has lower precedence,
	/// zero if they have the same precedence,
	/// greater than zero if this identifier has higher precedence.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method implements the SemVer precedence rules for identifiers:
	/// </para>
	/// <list type="number">
	/// <item><description>Both numeric: Compare as integers (1 &lt; 2 &lt; 10)</description></item>
	/// <item><description>This numeric, other not: This has lower precedence (-1)</description></item>
	/// <item><description>This not numeric, other is: This has higher precedence (1)</description></item>
	/// <item><description>Both non-numeric: Compare lexically using ordinal (case-sensitive) comparison</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// var numeric1 = new SemanticVersionIdentifier("5");
	/// var numeric2 = new SemanticVersionIdentifier("10");
	/// var alpha = new SemanticVersionIdentifier("alpha");
	/// var beta = new SemanticVersionIdentifier("beta");
	/// 
	/// Console.WriteLine(numeric1.CompareTo(numeric2));  // -1 (5 &lt; 10)
	/// Console.WriteLine(numeric2.CompareTo(alpha));     // -1 (numeric &lt; alphanumeric)
	/// Console.WriteLine(alpha.CompareTo(beta));         // -1 ("alpha" &lt; "beta")
	/// Console.WriteLine(beta.CompareTo(beta));          // 0 (equal)
	/// Console.WriteLine(beta.CompareTo(alpha));         // 1 ("beta" > "alpha")
	/// </code>
	/// </example>
	public int CompareTo(SemanticVersionIdentifier? other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		return (IsNumber, other.IsNumber) switch
		{
			(true, true) => Nullable.Compare(NumberValue, other.NumberValue),
			(true, false) => -1,
			(false, true) => 1,
			var _ => string.Compare(Value, other.Value, StringComparison.Ordinal)
		};
	}
	
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
	/// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not null and not a <see cref="SemanticVersionIdentifier"/>.</exception>
	public int CompareTo(object? obj)
	{
		if (obj is null)
			return 1;
		var other = obj as SemanticVersionIdentifier
			?? throw new ArgumentException($"Type must be '{nameof(SemanticVersionIdentifier)}'", "obj");
		return CompareTo(other);
	}
	
	/// <summary>
	/// Determines whether this identifier is equal to another.
	/// </summary>
	/// <param name="other">The <see cref="SemanticVersionIdentifier"/> to compare with this instance.</param>
	/// <returns>true if the <see cref="Value"/> strings are equal; otherwise, false.</returns>
	/// <remarks>
	/// Equality is based solely on string comparison of the <see cref="Value"/> property.
	/// The numeric classification (<see cref="IsNumber"/>) does not affect equality.
	/// </remarks>
	/// <example>
	/// <code>
	/// var id1 = new SemanticVersionIdentifier("123");
	/// var id2 = new SemanticVersionIdentifier("123");
	/// var id3 = new SemanticVersionIdentifier("alpha");
	/// 
	/// Console.WriteLine(id1.Equals(id2));  // true (same value)
	/// Console.WriteLine(id1.Equals(id3));  // false (different value)
	/// </code>
	/// </example>
	public bool Equals(SemanticVersionIdentifier? other) => other is not null && string.Equals(Value, other.Value);
	
	/// <summary>
	/// Determines whether the specified object is equal to the current <see cref="SemanticVersionIdentifier"/>.
	/// </summary>
	/// <param name="obj">The object to compare with the current identifier.</param>
	/// <returns>true if the specified object is a <see cref="SemanticVersionIdentifier"/> and is equal to this instance; otherwise, false.</returns>
	public override bool Equals(object? obj)
	{
		var identifier = obj as SemanticVersionIdentifier;
		return identifier is not null && Equals(identifier);
	}
	
	/// <summary>
	/// Returns the hash code for this <see cref="SemanticVersionIdentifier"/>.
	/// </summary>
	/// <returns>A 32-bit signed integer hash code.</returns>
	/// <remarks>
	/// The hash code is computed from the <see cref="Value"/> string to ensure that
	/// equal identifiers have the same hash code.
	/// </remarks>
	public override int GetHashCode() => Value.GetHashCode();
}