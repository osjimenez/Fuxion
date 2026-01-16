using System;

namespace Fuxion;

/// <summary>
/// Provides extension methods for types that implement <see cref="IComparable"/>.
/// </summary>
public static class ComparableExtensions
{
	/// <summary>
	/// Extension methods for checking if a comparable value falls within a specified range.
	/// </summary>
	extension<TComparable>(TComparable me) where TComparable : IComparable
	{
		/// <summary>
		/// Determines whether the value is between the specified minimum and maximum values (inclusive on both ends).
		/// </summary>
		/// <param name="minimum">The minimum value of the range (inclusive).</param>
		/// <param name="maximum">The maximum value of the range (inclusive).</param>
		/// <returns>true if the value is within the range [minimum, maximum]; otherwise, false.</returns>
		/// <example>
		/// <code>
		/// int value = 5;
		/// bool result = value.IsBetween(1, 10); // true
		/// 
		/// var date = new DateTime(2024, 6, 15);
		/// bool inRange = date.IsBetween(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)); // true
		/// </code>
		/// </example>
		public bool IsBetween(TComparable minimum, TComparable maximum) => me.IsBetween(false, minimum, false, maximum);
		
		/// <summary>
		/// Determines whether the value is between the specified minimum and maximum values,
		/// with configurable exclusivity for the minimum bound.
		/// </summary>
		/// <param name="minimumExclusive">If true, the minimum value is excluded from the range; otherwise, it is included.</param>
		/// <param name="minimum">The minimum value of the range.</param>
		/// <param name="maximum">The maximum value of the range (inclusive).</param>
		/// <returns>true if the value is within the specified range; otherwise, false.</returns>
		/// <example>
		/// <code>
		/// int value = 10;
		/// bool result1 = value.IsBetween(false, 10, 20); // true (inclusive minimum)
		/// bool result2 = value.IsBetween(true, 10, 20);  // false (exclusive minimum)
		/// </code>
		/// </example>
		public bool IsBetween(bool minimumExclusive, TComparable minimum, TComparable maximum) => me.IsBetween(minimumExclusive, minimum, false, maximum);
		
		/// <summary>
		/// Determines whether the value is between the specified minimum and maximum values,
		/// with configurable exclusivity for the maximum bound.
		/// </summary>
		/// <param name="minimum">The minimum value of the range (inclusive).</param>
		/// <param name="maximumExclusive">If true, the maximum value is excluded from the range; otherwise, it is included.</param>
		/// <param name="maximum">The maximum value of the range.</param>
		/// <returns>true if the value is within the specified range; otherwise, false.</returns>
		/// <example>
		/// <code>
		/// int value = 20;
		/// bool result1 = value.IsBetween(10, false, 20); // true (inclusive maximum)
		/// bool result2 = value.IsBetween(10, true, 20);  // false (exclusive maximum)
		/// </code>
		/// </example>
		public bool IsBetween(TComparable minimum, bool maximumExclusive, TComparable maximum) => me.IsBetween(false, minimum, maximumExclusive, maximum);
		
		/// <summary>
		/// Determines whether the value is between the specified minimum and maximum values,
		/// with configurable exclusivity for both bounds.
		/// </summary>
		/// <param name="minimumExclusive">If true, the minimum value is excluded from the range; otherwise, it is included.</param>
		/// <param name="minimum">The minimum value of the range.</param>
		/// <param name="maximumExclusive">If true, the maximum value is excluded from the range; otherwise, it is included.</param>
		/// <param name="maximum">The maximum value of the range.</param>
		/// <returns>true if the value is within the specified range; otherwise, false.</returns>
		/// <remarks>
		/// <para>This method uses <see cref="IComparable.CompareTo"/> to perform comparisons.</para>
		/// <para>The method evaluates:</para>
		/// <list type="bullet">
		/// <item><description>If value &lt; minimum: returns false</description></item>
		/// <item><description>If value == minimum and minimumExclusive is true: returns false</description></item>
		/// <item><description>If value &gt; maximum: returns false</description></item>
		/// <item><description>If value == maximum and maximumExclusive is true: returns false</description></item>
		/// <item><description>Otherwise: returns true</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Testing range boundaries with integers
		/// int value = 15;
		/// 
		/// // Inclusive on both ends: [10, 20]
		/// value.IsBetween(false, 10, false, 20); // true
		/// 
		/// // Exclusive on both ends: (10, 20)
		/// value.IsBetween(true, 10, true, 20); // true
		/// 
		/// // Edge cases
		/// 10.IsBetween(false, 10, false, 20); // true (inclusive min)
		/// 10.IsBetween(true, 10, false, 20);  // false (exclusive min)
		/// 20.IsBetween(false, 10, false, 20); // true (inclusive max)
		/// 20.IsBetween(false, 10, true, 20);  // false (exclusive max)
		/// 
		/// // Real-world example: checking business hours
		/// var now = DateTime.Now.TimeOfDay;
		/// var openTime = new TimeSpan(9, 0, 0);  // 9:00 AM
		/// var closeTime = new TimeSpan(17, 0, 0); // 5:00 PM
		/// bool isOpen = now.IsBetween(false, openTime, true, closeTime); // [9:00, 17:00)
		/// 
		/// // Temperature range check
		/// double temp = 22.5;
		/// bool comfortable = temp.IsBetween(false, 18.0, false, 25.0); // [18°C, 25°C]
		/// </code>
		/// </example>
		public bool IsBetween(bool minimumExclusive, TComparable minimum, bool maximumExclusive, TComparable maximum)
		{
			var minRes = me.CompareTo(minimum);
			if (minRes < 0) return false;
			if (minimumExclusive && minRes == 0) return false;

			var maxRes = me.CompareTo(maximum);
			if (maxRes > 0) return false;
			if (maximumExclusive && maxRes == 0) return false;

			return true;
		}
	}
}