using System;
using System.Collections.Generic;

namespace Fuxion.Collections.Generic;

/// <summary>
/// Provides a generic implementation of <see cref="Comparer{T}"/> that uses a custom comparison function.
/// This class allows creating comparers on-the-fly using lambda expressions or delegates.
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
/// <param name="comparisonFunction">
/// The function that defines the comparison logic. 
/// Should return a negative value if the first parameter is less than the second,
/// zero if they are equal, or a positive value if the first parameter is greater than the second.
/// </param>
/// <remarks>
/// This class is useful when you need a custom comparer without having to create a dedicated class.
/// It follows the same contract as <see cref="IComparer{T}.Compare"/> method.
/// </remarks>
/// <example>
/// <code>
/// // Create a comparer for strings by length (descending)
/// var lengthComparer = new GenericComparer&lt;string&gt;((x, y) => 
/// {
///     if (x == null &amp;&amp; y == null) return 0;
///     if (x == null) return 1;
///     if (y == null) return -1;
///     return y.Length.CompareTo(x.Length); // Descending
/// });
/// 
/// var words = new[] { "apple", "hi", "banana", "xyz" };
/// Array.Sort(words, lengthComparer);
/// // Result: ["banana", "apple", "xyz", "hi"]
/// 
/// // Use with LINQ OrderBy
/// var sorted = words.OrderBy(w => w, lengthComparer);
/// 
/// // Numeric comparison example
/// var evenFirstComparer = new GenericComparer&lt;int&gt;((x, y) =>
/// {
///     bool xIsEven = x % 2 == 0;
///     bool yIsEven = y % 2 == 0;
///     
///     if (xIsEven &amp;&amp; !yIsEven) return -1;
///     if (!xIsEven &amp;&amp; yIsEven) return 1;
///     return x.CompareTo(y);
/// });
/// </code>
/// </example>
public class GenericComparer<T>(Func<T?, T?, int> comparisonFunction) : Comparer<T>
{
	/// <summary>
	/// Performs a comparison of two objects of the same type and returns a value indicating their relative order.
	/// </summary>
	/// <param name="x">The first object to compare.</param>
	/// <param name="y">The second object to compare.</param>
	/// <returns>
	/// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>:
	/// <list type="bullet">
	/// <item><description>Less than zero: <paramref name="x"/> is less than <paramref name="y"/></description></item>
	/// <item><description>Zero: <paramref name="x"/> equals <paramref name="y"/></description></item>
	/// <item><description>Greater than zero: <paramref name="x"/> is greater than <paramref name="y"/></description></item>
	/// </list>
	/// </returns>
	public override int Compare(T? x, T? y) => comparisonFunction(x, y);
}