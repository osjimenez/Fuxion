using System;
using System.Collections.Generic;

namespace Fuxion.Collections.Generic;

/// <summary>
/// Provides a generic implementation of <see cref="EqualityComparer{T}"/> that uses custom equality and hash code functions.
/// This class allows creating equality comparers on-the-fly using lambda expressions or delegates.
/// </summary>
/// <typeparam name="T">The type of objects to compare for equality.</typeparam>
/// <param name="equalsFunction">
/// The function that defines the equality comparison logic. 
/// Should return true if the two objects are considered equal; otherwise, false.
/// </param>
/// <param name="getHashCodeFunction">
/// The function that generates hash codes for objects.
/// Must return consistent hash codes for objects that are considered equal by <paramref name="equalsFunction"/>.
/// </param>
/// <remarks>
/// <para>
/// This class is useful when you need a custom equality comparer without having to create a dedicated class.
/// It follows the same contract as <see cref="IEqualityComparer{T}"/> interface.
/// </para>
/// <para><strong>Important:</strong> The hash code function must satisfy the following rules:</para>
/// <list type="bullet">
/// <item><description>If two objects are equal according to <paramref name="equalsFunction"/>, they must return the same hash code</description></item>
/// <item><description>The hash code for an object must remain consistent during its lifetime</description></item>
/// <item><description>Null values should be handled appropriately</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Create a case-insensitive string comparer
/// var caseInsensitiveComparer = new GenericEqualityComparer&lt;string&gt;(
///     equalsFunction: (x, y) => 
///     {
///         if (x == null &amp;&amp; y == null) return true;
///         if (x == null || y == null) return false;
///         return x.Equals(y, StringComparison.OrdinalIgnoreCase);
///     },
///     getHashCodeFunction: obj => 
///         obj?.ToUpperInvariant().GetHashCode() ?? 0
/// );
/// 
/// var dict = new Dictionary&lt;string, int&gt;(caseInsensitiveComparer);
/// dict["Hello"] = 1;
/// dict["HELLO"] = 2; // Replaces the previous value
/// // dict.Count == 1
/// 
/// // Custom equality for complex objects
/// public class Person
/// {
///     public string FirstName { get; set; }
///     public string LastName { get; set; }
///     public int Age { get; set; }
/// }
/// 
/// // Compare by name only, ignoring age
/// var nameComparer = new GenericEqualityComparer&lt;Person&gt;(
///     equalsFunction: (p1, p2) =>
///     {
///         if (p1 == null &amp;&amp; p2 == null) return true;
///         if (p1 == null || p2 == null) return false;
///         return p1.FirstName == p2.FirstName &amp;&amp; p1.LastName == p2.LastName;
///     },
///     getHashCodeFunction: p => 
///         p == null ? 0 : HashCode.Combine(p.FirstName, p.LastName)
/// );
/// 
/// var people = new HashSet&lt;Person&gt;(nameComparer);
/// people.Add(new Person { FirstName = "John", LastName = "Doe", Age = 30 });
/// people.Add(new Person { FirstName = "John", LastName = "Doe", Age = 40 }); // Not added (same name)
/// // people.Count == 1
/// </code>
/// </example>
public class GenericEqualityComparer<T>(Func<T?, T?, bool> equalsFunction, Func<T, int> getHashCodeFunction)
	: EqualityComparer<T>
{
	/// <summary>
	/// Determines whether two objects of type <typeparamref name="T"/> are equal.
	/// </summary>
	/// <param name="x">The first object to compare.</param>
	/// <param name="y">The second object to compare.</param>
	/// <returns>true if the specified objects are equal; otherwise, false.</returns>
	/// <remarks>
	/// This method invokes the equality function provided to the constructor.
	/// </remarks>
	public override bool Equals(T? x, T? y) => equalsFunction.Invoke(x, y);
	
	/// <summary>
	/// Returns a hash code for the specified object.
	/// </summary>
	/// <param name="obj">The object for which to get a hash code.</param>
	/// <returns>A hash code for the specified object.</returns>
	/// <remarks>
	/// This method invokes the hash code function provided to the constructor.
	/// The returned hash code must be consistent with the equality logic defined in <see cref="Equals(T?, T?)"/>.
	/// </remarks>
	public override int GetHashCode(T obj) => getHashCodeFunction.Invoke(obj);
}