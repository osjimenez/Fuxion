using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

namespace Fuxion.Collections.Generic;

/// <summary>
/// Provides extension methods for working with collections and sequences.
/// </summary>
public static class CollectionsExtensions
{
	/// <summary>
	/// Extension methods for filtering nullable reference types from sequences.
	/// </summary>
	extension<T>(IEnumerable<T?> me) where T : class
	{
		/// <summary>
		/// Filters out null values from a sequence of nullable reference types.
		/// </summary>
		/// <returns>A sequence containing only non-null values.</returns>
		/// <example>
		/// <code>
		/// IEnumerable&lt;string?&gt; items = new[] { "hello", null, "world", null };
		/// var nonNull = items.WhereNotNull(); // Returns: ["hello", "world"]
		/// </code>
		/// </example>
		public IEnumerable<T> WhereNotNull()
			=> me.Where(i => i is not null).Select(i => i!);
	}
	
	/// <summary>
	/// Extension methods for filtering strings from sequences.
	/// </summary>
	extension(IEnumerable<string?> me)
	{
		/// <summary>
		/// Filters out null, empty, or whitespace-only strings from a sequence.
		/// </summary>
		/// <returns>A sequence containing only strings that have meaningful content.</returns>
		/// <example>
		/// <code>
		/// var strings = new[] { "hello", null, "  ", "world", "" };
		/// var valid = strings.WhereNeitherNullNorWhiteSpace(); // Returns: ["hello", "world"]
		/// </code>
		/// </example>
		public IEnumerable<string> WhereNeitherNullNorWhiteSpace()
			=> me.Where(i => i.IsNeitherNullNorWhiteSpace()).Select(i => i!);
	}
	
	/// <summary>
	/// Extension methods for filtering nullable value types from sequences.
	/// </summary>
	extension<T>(IEnumerable<T?> me) where T : struct
	{
		/// <summary>
		/// Filters out null values from a sequence of nullable value types.
		/// </summary>
		/// <returns>A sequence containing only values that have a value (not null).</returns>
		/// <example>
		/// <code>
		/// IEnumerable&lt;int?&gt; numbers = new int?[] { 1, null, 2, null, 3 };
		/// var nonNull = numbers.WhereNotNull(); // Returns: [1, 2, 3]
		/// </code>
		/// </example>
		public IEnumerable<T> WhereNotNull()
			=> me.Where(i => i.HasValue).Select(i => i!.Value);

		/// <summary>
		/// Filters out null values and default values from a sequence of nullable value types.
		/// </summary>
		/// <returns>A sequence containing only values that are neither null nor the default value for the type.</returns>
		/// <example>
		/// <code>
		/// IEnumerable&lt;int?&gt; numbers = new int?[] { 1, null, 0, 2, 0, 3 };
		/// var filtered = numbers.WhereNeitherNullNorDefault(); // Returns: [1, 2, 3]
		/// </code>
		/// </example>
		public IEnumerable<T> WhereNeitherNullNorDefault()
			=> me.Where(i => i.HasValue && !i.Value.Equals(default(T))).Select(i => i!.Value);
	}

	/// <summary>
	/// Extension methods for filtering nullable reference types from queryable sequences.
	/// </summary>
	extension<T>(IQueryable<T?> me) where T : class
	{
		/// <summary>
		/// Filters out null values from a queryable sequence of nullable reference types.
		/// This method is expression-tree compatible for use with LINQ providers like Entity Framework.
		/// </summary>
		/// <returns>A queryable sequence containing only non-null values.</returns>
		public IQueryable<T> WhereNotNull()
			=> me.Where(i => i != null).Select(i => i!);
	}
	
	/// <summary>
	/// Extension methods for filtering strings from queryable sequences.
	/// </summary>
	extension(IQueryable<string?> me)
	{
		/// <summary>
		/// Filters out null, empty, or whitespace-only strings from a queryable sequence.
		/// This method is expression-tree compatible for use with LINQ providers like Entity Framework.
		/// </summary>
		/// <returns>A queryable sequence containing only strings that have meaningful content.</returns>
		public IQueryable<string> WhereNeitherNullNorWhiteSpace()
			=> me.Where(i => i.IsNeitherNullNorWhiteSpace()).Select(i => i!);
	}

	/// <summary>
	/// Extension methods for filtering nullable value types from queryable sequences.
	/// </summary>
	extension<T>(IQueryable<T?> me) where T : struct
	{
		/// <summary>
		/// Filters out null values from a queryable sequence of nullable value types.
		/// This method is expression-tree compatible for use with LINQ providers like Entity Framework.
		/// </summary>
		/// <returns>A queryable sequence containing only values that have a value (not null).</returns>
		public IQueryable<T> WhereNotNull()
			=> me.Where(i => i.HasValue).Select(i => i!.Value);

		/// <summary>
		/// Filters out null values and default values from a queryable sequence of nullable value types.
		/// This method is expression-tree compatible for use with LINQ providers like Entity Framework.
		/// </summary>
		/// <returns>A queryable sequence containing only values that are neither null nor the default value for the type.</returns>
		public IQueryable<T> WhereNeitherNullNorDefault()
			=> me.Where(i => i.HasValue && !i.Value.Equals(default(T))).Select(i => i!.Value);
	}

	/// <summary>
	/// Takes a specified number of random elements from a sequence without repetition.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="me">The sequence to take elements from.</param>
	/// <param name="count">The number of random elements to take.</param>
	/// <returns>A list containing the randomly selected elements.</returns>
	/// <exception cref="Exception">Thrown when <paramref name="count"/> is greater than the number of elements in the sequence.</exception>
	/// <remarks>
	/// Uses cryptographically secure random number generation on modern frameworks (.NET 6+),
	/// and falls back to GUID-seeded random for older frameworks.
	/// </remarks>
	/// <example>
	/// <code>
	/// var numbers = Enumerable.Range(1, 100);
	/// var randomFive = numbers.TakeRandomly(5); // Returns 5 random numbers without repetition
	/// </code>
	/// </example>
	public static List<T> TakeRandomly<T>(this IEnumerable<T> me, int count)
	{
		var list = me.ToList();
		// TODO Implements canRepeat behavior
		//if (!canRepeat && count > list.Count) throw new("'count' cannot be higher than number of elements if 'canRepeat' is false");
		if (count > list.Count) throw new("'count' cannot be higher than number of elements");
		var used = new List<int>();
		List<T> res = new();
		for (var i = 0; i < count; i++)
		{
#if STANDARD_OR_OLD_FRAMEWORKS
			var ran = new Random(Guid.NewGuid()
				.GetHashCode());
			var actual = ran.Next(0, list.Count);
			while (used.Contains(actual)) actual = ran.Next(0, list.Count);
#else
			var actual = RandomNumberGenerator.GetInt32(0, list.Count);
			while (used.Contains(actual)) actual = RandomNumberGenerator.GetInt32(0, list.Count);
#endif
			used.Add(actual);
			res.Add(list[actual]);
		}
		return res;
	}
	
	/// <summary>
	/// Determines whether a sequence is null or contains no elements.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="me">The sequence to check.</param>
	/// <returns>true if the sequence is null or empty; otherwise, false.</returns>
	/// <example>
	/// <code>
	/// List&lt;int&gt;? nullList = null;
	/// var emptyList = new List&lt;int&gt;();
	/// var filledList = new List&lt;int&gt; { 1, 2, 3 };
	/// 
	/// nullList.IsNullOrEmpty();   // true
	/// emptyList.IsNullOrEmpty();  // true
	/// filledList.IsNullOrEmpty(); // false
	/// </code>
	/// </example>
	public static bool IsNullOrEmpty<T>([NotNullWhen(false)]this IEnumerable<T>? me) => me == null || !me.Any();

	/// <summary>
	/// Determines whether a sequence is neither null nor empty.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="me">The sequence to check.</param>
	/// <returns>true if the sequence is not null and contains at least one element; otherwise, false.</returns>
	/// <example>
	/// <code>
	/// List&lt;int&gt;? nullList = null;
	/// var emptyList = new List&lt;int&gt;();
	/// var filledList = new List&lt;int&gt; { 1, 2, 3 };
	/// 
	/// nullList.IsNeitherNullNorEmpty();   // false
	/// emptyList.IsNeitherNullNorEmpty();  // false
	/// filledList.IsNeitherNullNorEmpty(); // true
	/// </code>
	/// </example>
	public static bool IsNeitherNullNorEmpty<T>([NotNullWhen(true)] this IEnumerable<T>? me) => me != null && me.Any();

	/// <summary>
	/// Removes statistical outliers from a sequence of integers using the interquartile range (IQR) method.
	/// </summary>
	/// <param name="list">The sequence of integers to process.</param>
	/// <param name="outputConsole">Optional action to output diagnostic information during calculation.</param>
	/// <returns>A sequence with outliers removed.</returns>
	/// <remarks>
	/// This is a convenience method that converts integers to longs, removes outliers, and converts back.
	/// See <see href="http://www.ehow.com/how_5201412_calculate-outliers.html">outlier calculation reference</see>.
	/// </remarks>
	public static IEnumerable<int> RemoveOutliers(this IEnumerable<int> list, Action<string>? outputConsole = null)
		=> list.Select(i => (long)i)
			.RemoveOutliers(outputConsole: outputConsole)
			.Select(i => (int)i);
	
	/// <summary>
	/// Removes statistical outliers from a sequence of dates using the interquartile range (IQR) method.
	/// </summary>
	/// <param name="list">The sequence of dates to process.</param>
	/// <param name="outputConsole">Optional action to output diagnostic information during calculation.</param>
	/// <returns>A sequence with outliers removed.</returns>
	/// <remarks>
	/// This method converts dates to ticks (long), removes outliers, and converts back to dates.
	/// </remarks>
	public static IEnumerable<DateTime> RemoveOutliers(this IEnumerable<DateTime> list, Action<string>? outputConsole = null)
		=> list.Select(i => i.Ticks)
			.RemoveOutliers(outputConsole: outputConsole)
			.Select(t => new DateTime(t));
	
	/// <summary>
	/// Removes statistical outliers from a sequence of long integers using the interquartile range (IQR) method.
	/// </summary>
	/// <param name="me">The sequence of long integers to process.</param>
	/// <param name="interquartileOutlierValueRangeFactor">The multiplier for the IQR to determine outlier boundaries. Default is 1.5 (mild outliers).</param>
	/// <param name="outputConsole">Optional action to output diagnostic information during calculation.</param>
	/// <returns>A sequence with outliers removed.</returns>
	/// <remarks>
	/// <para>The method uses the following algorithm:</para>
	/// <list type="number">
	/// <item><description>Sorts data in ascending order</description></item>
	/// <item><description>Calculates quartiles (Q1, Q2, Q3, Q4)</description></item>
	/// <item><description>Computes the interquartile range: IQ = Q3 - Q1</description></item>
	/// <item><description>Determines outlier boundaries: Q1 - (IQ × factor) and Q3 + (IQ × factor)</description></item>
	/// <item><description>Removes values outside these boundaries</description></item>
	/// </list>
	/// <para>See <see href="http://www.ehow.com/how_5201412_calculate-outliers.html">outlier calculation reference</see>.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var data = new long[] { 1, 2, 3, 4, 5, 100 }; // 100 is an outlier
	/// var cleaned = data.RemoveOutliers();
	/// // Result will exclude 100
	/// </code>
	/// </example>
	public static IEnumerable<long> RemoveOutliers(this IEnumerable<long> me, double interquartileOutlierValueRangeFactor = 1.5, Action<string>? outputConsole = null)
	{
		if (!me.Any()) return me;
		// Sort data in ascending
		var l = me.OrderBy(_ => _)
			.ToList();
		// Calculate median
		double median;
		if (l.Count % 2 == 0) // if even number of elements, average two in the middle
			median = l.Skip(l.Count / 2 - 1)
				.Take(2)
				.Average();
		else // if odd number of elements, take center
			median = l.Skip(l.Count / 2)
				.First();
		// Find the upper quartile Q2
		// http://estadisticapasoapaso.blogspot.com.es/2011/09/los-cuartiles.html
		// Qk = k (N/4)
		// q1 = 1 (N/4)
		// q2 = 2 (N/4)
		var getQuartileFunction = new Func<int, double>(q =>
		{
			outputConsole?.Invoke("Calculating Q" + q);
			var exactPosition = q * ((double)l.Count / 4);
			outputConsole?.Invoke($"   {nameof(exactPosition)} = {exactPosition}");
			var integerPosition = (int)exactPosition - 1;
			if (integerPosition < 0) integerPosition = 0;
			outputConsole?.Invoke($"   {nameof(integerPosition)} = {integerPosition}");
			var restPosition = exactPosition % 1;
			if (restPosition > 0 && integerPosition + 1 == l.Count) restPosition = 0;
			outputConsole?.Invoke($"   {nameof(restPosition)} = {restPosition}");
			var result = (double)l[integerPosition];
			outputConsole?.Invoke($"   {nameof(result)} (before rest) = {(long)result}");
			if (restPosition > 0) result += restPosition * (l[integerPosition + 1] - l[integerPosition]);
			outputConsole?.Invoke($"   {nameof(result)} = {(long)result}");
			return result;
		});
		double firstQuartilePossition = 1 * (l.Count / 4);
		var q1 = getQuartileFunction(1);
		var q2 = getQuartileFunction(2);
		var q3 = getQuartileFunction(3);
		var q4 = getQuartileFunction(4);
		var iq = q3 - q1;
		var mildOutlierRange = iq * interquartileOutlierValueRangeFactor;
		var upperMildOutlierValue = q3 + mildOutlierRange;
		var lowerMildOutlierValue = q1 - mildOutlierRange;
		//var extremeOutlierRange = iq * 3;
		//var upperExtremeOutlierValue = q3 + extremeOutlierRange;
		//var lowerExtremeOutlierValue = q1 - extremeOutlierRange;
		outputConsole?.Invoke("Original values:");
		foreach (var i in l) outputConsole?.Invoke("  - " + i);
		outputConsole?.Invoke("");
		outputConsole?.Invoke("Q1 => " + (long)q1);
		outputConsole?.Invoke("Q2 => " + (long)q2);
		outputConsole?.Invoke("Q3 => " + (long)q3);
		outputConsole?.Invoke("Q4 => " + (long)q4);
		outputConsole?.Invoke("");
		outputConsole?.Invoke("Interquartile range: " + iq);
		outputConsole?.Invoke("interquartileOutlierValueRangeFactor: " + interquartileOutlierValueRangeFactor);
		outputConsole?.Invoke("Mild outlier range: " + mildOutlierRange);
		//outputConsole?.Invoke("Extreme outlier range: " + extremeOutlierRange);
		outputConsole?.Invoke("Upper mild outlier limit: " + (long)upperMildOutlierValue);
		outputConsole?.Invoke("Lower mild outlier limit: " + (long)lowerMildOutlierValue);
		//outputConsole?.Invoke("Upper extreme outlier limit: " + (long)upperExtremeOutlierValue);
		//outputConsole?.Invoke("Lower extreme outlier limit: " + (long)lowerExtremeOutlierValue);
		outputConsole?.Invoke("");
		var res = l.Where(v => v <= upperMildOutlierValue && v >= lowerMildOutlierValue)
			.ToList();
		var outliers = l.Where(v => v > upperMildOutlierValue || v < lowerMildOutlierValue)
			.ToList();
		outputConsole?.Invoke("Outliers:");
		foreach (var i in outliers) outputConsole?.Invoke("  - " + i);
		outputConsole?.Invoke("");
		outputConsole?.Invoke("Result values:");
		foreach (var i in res) outputConsole?.Invoke("  - " + i);
		return res;
	}
	
	/// <summary>
	/// Distributes a total number of items across multiple categories based on percentage values.
	/// </summary>
	/// <param name="percentages">The percentage values for each category. Must sum to 100.</param>
	/// <param name="amountOfItems">The total number of items to distribute.</param>
	/// <returns>
	/// A response containing a sequence of tuples with the percentage, rounded quantity, and exact quantity for each category.
	/// Returns an error response if validation fails.
	/// </returns>
	/// <remarks>
	/// <para>The method ensures that:</para>
	/// <list type="bullet">
	/// <item><description>Percentages sum to exactly 100</description></item>
	/// <item><description>Number of categories doesn't exceed total items</description></item>
	/// <item><description>No category receives zero items (minimum 1)</description></item>
	/// <item><description>The sum of rounded quantities equals the total items</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// var percentages = new[] { 30.0, 50.0, 20.0 };
	/// var result = percentages.DistributeAsPercentages(100);
	/// // Result: [(30%, 30, 30.0), (50%, 50, 50.0), (20%, 20, 20.0)]
	/// </code>
	/// </example>
	public static IResponse<IEnumerable<(double Percentage, int Rounded, double Exact)>> DistributeAsPercentages(this IEnumerable<double> percentages, int amountOfItems)
	{
		var count = percentages.Count();
		if (count > amountOfItems)
			return ResponseExt.Get.InvalidData($"{nameof(percentages)}.Count ({count}) must be less than {nameof(amountOfItems)} ({amountOfItems})")
				.AsPayload<IEnumerable<(double Percentage, int Rounded, double Exact)>>();
		var sum = percentages.Sum();
		if (sum != 100)
			return ResponseExt.Get.InvalidData($"Percentages must sum 100, but sum {sum}")
				.AsPayload<IEnumerable<(double Percentage, int Rounded, double Exact)>>();
		var ordered = percentages.OrderBy(x => x);
		var quantities = ordered.Select(value => new
		{
			Percentage = value,
			Rounded = (int)System.Math.Floor(amountOfItems * (value / 100d)),
			Exact = amountOfItems * (value / 100d)
		})
			.ToList();
		quantities = quantities.Select(x => x with
		{
			Rounded = x.Rounded == 0 ? 1 : x.Rounded
		})
			.ToList();
		while (quantities.Sum(x => x.Rounded) > amountOfItems)
		{
			var quantity = quantities.MaxBy(_ => _.Rounded);
			if (quantity is null)
				return ResponseExt.Get.Critical($"{nameof(quantity)} cannot be null")
					.AsPayload<IEnumerable<(double Percentage, int Rounded, double Exact)>>();
			var index = quantities.IndexOf(quantity);
			quantities.Remove(quantity);
			quantities.Insert(index, quantity with
			{
				Rounded = quantity.Rounded - 1
			});
		}
		return ResponseExt.Get.SuccessPayload(quantities.Select(x => (x.Percentage, x.Rounded, x.Exact)));
	}
	
	/// <summary>
	/// Distributes a total number of items across labeled categories based on percentage values.
	/// </summary>
	/// <param name="percentages">A list of tuples containing labels and their corresponding percentage values. Percentages must sum to 100.</param>
	/// <param name="amountOfItems">The total number of items to distribute.</param>
	/// <returns>
	/// A response containing a dictionary mapping each label to its percentage, rounded quantity, and exact quantity.
	/// Returns an error response if validation fails.
	/// </returns>
	/// <remarks>
	/// <para>This overload provides the same functionality as <see cref="DistributeAsPercentages(IEnumerable{double}, int)"/> 
	/// but with labeled categories for easier identification of results.</para>
	/// <para>The method adjusts quantities both upward and downward to ensure the sum matches the total items exactly.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var categories = new List&lt;(string, double)&gt;
	/// {
	///     ("Small", 30.0),
	///     ("Medium", 50.0),
	///     ("Large", 20.0)
	/// };
	/// var result = categories.DistributeAsPercentages(100);
	/// // Result: { "Small": (30%, 30, 30.0), "Medium": (50%, 50, 50.0), "Large": (20%, 20, 20.0) }
	/// </code>
	/// </example>
	public static IResponse<Dictionary<string, (double Percentage, int Rounded, double Exact)>> DistributeAsPercentages(this IList<(string Label, double Percentage)> percentages, int amountOfItems)
	{
		if (percentages.Count > amountOfItems)
			return ResponseExt.Get.InvalidData($"{nameof(percentages)}.Count ({percentages.Count}) must be less than {nameof(amountOfItems)} ({amountOfItems})")
				.AsPayload<Dictionary<string, (double Percentage, int Rounded, double Exact)>>();
		if (percentages.Sum(x => x.Percentage) != 100d)
			return ResponseExt.Get.InvalidData($"Percentages must sum 100, but sum {percentages.Sum(x => x.Percentage)}")
				.AsPayload<Dictionary<string, (double Percentage, int Rounded, double Exact)>>();
		var ordered = percentages.OrderBy(x => x.Percentage);
		var quantities = ordered.Select(value => new
		{
			value.Label,
			value.Percentage,
			Rounded = (int)System.Math.Floor(amountOfItems * (value.Percentage / 100d)),
			Exact = amountOfItems * (value.Percentage / 100d)
		})
			.ToList();
		quantities = quantities.Select(x => x with
		{
			Rounded = x.Rounded == 0 ? 1 : x.Rounded
		})
			.ToList();
		while (quantities.Sum(x => x.Rounded) > amountOfItems)
		{
			var quantity = quantities.OrderByDescending(x => x.Rounded)
				.MaxBy(y => y.Exact);
			if (quantity is null)
				return ResponseExt.Get.Critical($"{nameof(quantity)} cannot be null")
					.AsPayload<Dictionary<string, (double Percentage, int Rounded, double Exact)>>();
			var index = quantities.IndexOf(quantity);
			quantities.Remove(quantity);
			quantities.Insert(index, quantity with
			{
				Rounded = quantity.Rounded - 1
			});
		}

		while (quantities.Sum(x => x.Rounded) < amountOfItems)
		{
			var quantity = quantities.OrderBy(x => x.Rounded)
				.MaxBy(y => y.Exact);
			if (quantity is null)
				return ResponseExt.Get.Critical($"{nameof(quantity)} cannot be null")
					.AsPayload<Dictionary<string, (double Percentage, int Rounded, double Exact)>>();
			var index = quantities.IndexOf(quantity);
			quantities.Remove(quantity);
			quantities.Insert(index, quantity with
			{
				Rounded = quantity.Rounded + 1
			});
		}

		return ResponseExt.Get.SuccessPayload(quantities.Select(x => (x.Label, x.Percentage, x.Rounded, x.Exact))
			.ToDictionary(x => x.Label, x => (x.Percentage, x.Rounded, x.Exact)));
	}
}