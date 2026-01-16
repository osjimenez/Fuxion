using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Fuxion.ComponentModel.DataAnnotations;

/// <summary>
/// Validates that a collection does not contain consecutive duplicate elements using a custom comparison method.
/// </summary>
/// <remarks>
/// <para>
/// This validation attribute checks for consecutive duplicate elements in a collection by invoking a static
/// comparison method provided by the user. The comparison method must return true when two consecutive elements
/// are considered duplicates.
/// </para>
/// <para>
/// The attribute requires:
/// </para>
/// <list type="bullet">
/// <item><description>A type containing the comparison method</description></item>
/// <item><description>The name of a public static method that takes two parameters and returns bool</description></item>
/// <item><description>Both parameters must be of the same type as the collection elements</description></item>
/// </list>
/// <para>
/// Note: This validator only checks <strong>consecutive</strong> elements. It does not check for duplicates
/// across the entire collection, only between adjacent items (element[i] compared with element[i+1]).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ComparisonMethods
/// {
///     // Compare strings case-insensitively
///     public static bool AreEqualIgnoreCase(string a, string b)
///     {
///         if (a == null &amp;&amp; b == null) return true;
///         if (a == null || b == null) return false;
///         return a.Equals(b, StringComparison.OrdinalIgnoreCase);
///     }
///     
///     // Compare persons by ID
///     public static bool HaveSameId(Person a, Person b)
///     {
///         return a?.Id == b?.Id;
///     }
/// }
/// 
/// public class Playlist
/// {
///     [EnsureNoDuplicates(typeof(ComparisonMethods), nameof(ComparisonMethods.AreEqualIgnoreCase),
///         ErrorMessage = "The playlist cannot have consecutive duplicate songs")]
///     public List&lt;string&gt; Songs { get; set; } = new();
/// }
/// 
/// // Valid - no consecutive duplicates
/// var validPlaylist = new Playlist
/// {
///     Songs = new List&lt;string&gt; { "Song A", "Song B", "Song A" } // "Song A" appears twice but not consecutively
/// };
/// 
/// // Invalid - has consecutive duplicates
/// var invalidPlaylist = new Playlist
/// {
///     Songs = new List&lt;string&gt; { "Song A", "Song A", "Song B" } // "Song A" appears consecutively
/// };
/// 
/// // Usage with complex objects
/// public class Person
/// {
///     public int Id { get; set; }
///     public string Name { get; set; }
/// }
/// 
/// public class Team
/// {
///     [EnsureNoDuplicates(typeof(ComparisonMethods), nameof(ComparisonMethods.HaveSameId),
///         ErrorMessage = "Cannot have the same person appear consecutively in the team lineup")]
///     public List&lt;Person&gt; Lineup { get; set; } = new();
/// }
/// </code>
/// </example>
public class EnsureNoDuplicatesAttribute : ValidationAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EnsureNoDuplicatesAttribute"/> class.
	/// </summary>
	/// <param name="type">The type containing the static comparison method.</param>
	/// <param name="comparassionMethodName">The name of the static public method used to compare two elements.</param>
	/// <exception cref="ArgumentException">
	/// Thrown when:
	/// <list type="bullet">
	/// <item><description>The method is not found in the specified type</description></item>
	/// <item><description>The method is not public or static</description></item>
	/// <item><description>The method does not return bool</description></item>
	/// <item><description>The method does not have exactly 2 parameters</description></item>
	/// </list>
	/// </exception>
	/// <remarks>
	/// The comparison method must have the following signature:
	/// <code>
	/// public static bool MethodName(T element1, T element2)
	/// </code>
	/// where T is the type of elements in the collection being validated.
	/// </remarks>
	public EnsureNoDuplicatesAttribute(Type type, string comparassionMethodName)
	{
		this.type = type;
		method = type.GetMethod(comparassionMethodName, BindingFlags.Static | BindingFlags.Public);
		if (method != null)
		{
			if (method.ReturnType == typeof(bool))
			{
				var pars = method.GetParameters();
				if (pars.Length != 2)
					throw new ArgumentException(
						$"Method '{comparassionMethodName}' in type '{type.Name}' specified for this '{nameof(EnsureNoDuplicatesAttribute)}' must has 2 parameters. Both of them must be of type of property.");
			} else
				throw new ArgumentException($"Method '{comparassionMethodName}' in type '{type.Name}' specified for this '{nameof(EnsureNoDuplicatesAttribute)}' must return 'bool'.");
		} else
			throw new ArgumentException(
				$"Method '{comparassionMethodName}' in type '{type.Name}' specified for this '{nameof(EnsureNoDuplicatesAttribute)}' was not found. This method must be public and static.");
	}
	
	readonly MethodInfo? method;
	readonly Type type;
	string? lastDuplicateValue;
	
	/// <summary>
	/// Formats the error message to display when validation fails.
	/// </summary>
	/// <param name="name">The name of the field being validated.</param>
	/// <returns>A formatted error message string including the property name and the duplicate value found.</returns>
	/// <remarks>
	/// The error message can use format placeholders: {0} for the property name and {1} for the string representation
	/// of the duplicate element that was found.
	/// </remarks>
	public override string FormatErrorMessage(string name) => string.Format(ErrorMessageString, name, lastDuplicateValue);
	
	/// <summary>
	/// Determines whether the specified collection is valid by checking for consecutive duplicate elements.
	/// </summary>
	/// <param name="value">The collection to validate.</param>
	/// <returns>
	/// true if the collection is valid (no consecutive duplicates found or fewer than 2 elements); otherwise, false.
	/// </returns>
	/// <remarks>
	/// <para>The validation process:</para>
	/// <list type="number">
	/// <item><description>Returns false if the value is not an <see cref="IList"/></description></item>
	/// <item><description>Returns true if the list has fewer than 2 elements (no comparison needed)</description></item>
	/// <item><description>Iterates through the list comparing each element with the next one</description></item>
	/// <item><description>If the comparison method returns true for any consecutive pair, validation fails</description></item>
	/// <item><description>Stores the string representation of the duplicate element for error messaging</description></item>
	/// </list>
	/// <para>
	/// The comparison starts at index 1 and compares element[i-1] with element[i], checking only consecutive pairs.
	/// </para>
	/// </remarks>
	public override bool IsValid(object? value)
	{
		if (value is IList list)
		{
			if (list.Count < 2) return true;
			for (var i = 1; i < list.Count; i++)
				if ((bool?)method?.Invoke(null, [list[i - 1], list[i]]) ?? false)
				{
					lastDuplicateValue = list[i]?.ToString();
					return false;
				}
			return true;
		}
		return false;
	}
}