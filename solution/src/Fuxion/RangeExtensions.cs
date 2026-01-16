using System;

namespace Fuxion;

/// <summary>
/// Provides extension methods for <see cref="Range"/> and <see cref="int"/> to enable foreach enumeration.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable elegant range-based iteration using C# foreach syntax, making numeric loops
/// more expressive and concise.
/// </para>
/// <para>
/// Supports two patterns:
/// </para>
/// <list type="bullet">
/// <item><description>Range syntax: <c>foreach (var i in 0..10)</c></description></item>
/// <item><description>Integer syntax: <c>foreach (var i in 10)</c> (iterates from 0 to 10)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Using Range syntax
/// foreach (var i in 0..5)
/// {
///     Console.WriteLine(i); // Prints: 0, 1, 2, 3, 4, 5
/// }
/// 
/// // Using integer syntax
/// foreach (var i in 5)
/// {
///     Console.WriteLine(i); // Prints: 0, 1, 2, 3, 4, 5
/// }
/// 
/// // Equivalent to traditional for loop
/// for (var i = 0; i &lt;= 10; i++)
/// {
///     Console.WriteLine(i);
/// }
/// // Can be written as:
/// foreach (var i in 0..10)
/// {
///     Console.WriteLine(i);
/// }
/// </code>
/// </example>
public static class RangeExtensions
{
	/// <summary>
	/// Extension methods for Range to enable foreach enumeration.
	/// </summary>
	extension(Range me)
	{
		/// <summary>
		/// Gets an enumerator that iterates through the range.
		/// </summary>
		/// <returns>A <see cref="CustomIntEnumerator"/> for the range.</returns>
		/// <remarks>
		/// This method enables using Range in foreach loops. The range must have a defined end value (not from-end syntax).
		/// </remarks>
		/// <example>
		/// <code>
		/// // Iterate from 0 to 10 (inclusive)
		/// foreach (var i in 0..10)
		/// {
		///     Console.WriteLine(i);
		/// }
		/// 
		/// // Iterate from 5 to 15 (inclusive)
		/// foreach (var i in 5..15)
		/// {
		///     Console.WriteLine(i);
		/// }
		/// </code>
		/// </example>
		/// <exception cref="NotSupportedException">Thrown when the range uses from-end syntax (^).</exception>
		public CustomIntEnumerator GetEnumerator() => new(me);
	}
	
	/// <summary>
	/// Extension methods for int to enable foreach enumeration from 0 to the number.
	/// </summary>
	extension(int number)
	{
		/// <summary>
		/// Gets an enumerator that iterates from 0 to the specified number (inclusive).
		/// </summary>
		/// <returns>A <see cref="CustomIntEnumerator"/> for the range 0 to number.</returns>
		/// <remarks>
		/// This provides a shorthand for iterating from 0 to a specific count.
		/// <c>foreach (var i in 10)</c> is equivalent to <c>foreach (var i in 0..10)</c>.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Iterate from 0 to 5 (inclusive) - 6 iterations total
		/// foreach (var i in 5)
		/// {
		///     Console.WriteLine(i); // Prints: 0, 1, 2, 3, 4, 5
		/// }
		/// 
		/// // Create an array with indices
		/// var indices = new List&lt;int&gt;();
		/// foreach (var i in 10)
		/// {
		///     indices.Add(i);
		/// }
		/// // indices contains: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
		/// </code>
		/// </example>
		/// <exception cref="ArgumentException">Thrown when number is less than or equal to 0.</exception>
		public CustomIntEnumerator GetEnumerator()
			=> number <= 0 ? throw new ArgumentException($"{nameof(number)} must be a positive value greater than 0", nameof(number)) : new(new(0, number));
	}
	
	/// <summary>
	/// Custom enumerator for iterating through integer ranges.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This struct implements the enumerator pattern required for foreach loops without implementing
	/// <see cref="System.Collections.IEnumerator"/>. This is a performance optimization as the compiler
	/// can use duck typing to recognize the pattern.
	/// </para>
	/// <para>
	/// The enumerator is inclusive of both start and end values.
	/// </para>
	/// </remarks>
	public struct CustomIntEnumerator
	{
		private readonly int _end;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomIntEnumerator"/> struct.
		/// </summary>
		/// <param name="range">The range to enumerate.</param>
		/// <exception cref="NotSupportedException">Thrown when the range end uses from-end syntax (^).</exception>
		/// <remarks>
		/// The Current value is initialized to one less than the start value, so the first MoveNext() call
		/// will set it to the start value.
		/// </remarks>
		public CustomIntEnumerator(Range range)
		{
			if (range.End.IsFromEnd) throw new NotSupportedException("You must specify an end for the range");
			Current = range.Start.Value - 1;
			_end = range.End.Value;
		}
		
		/// <summary>
		/// Gets the current element in the enumeration.
		/// </summary>
		/// <value>The current integer value in the range.</value>
		public int Current { get; private set; }
		
		/// <summary>
		/// Advances the enumerator to the next element of the range.
		/// </summary>
		/// <returns>
		/// true if the enumerator was successfully advanced to the next element; 
		/// false if the enumerator has passed the end of the range.
		/// </returns>
		/// <remarks>
		/// This method increments <see cref="Current"/> and returns true as long as the value
		/// doesn't exceed the end of the range (inclusive).
		/// </remarks>
		public bool MoveNext()
		{
			Current++;
			return Current <= _end;
		}
	}
}