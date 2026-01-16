using System;
using System.Collections.Generic;
using System.Linq;

#if STANDARD_OR_OLD_FRAMEWORKS

namespace Fuxion
{
	using System.Runtime.CompilerServices;

   /// <summary>
   /// Provides extension methods that backport modern .NET functionality to older target frameworks.
   /// </summary>
   /// <remarks>
   /// <para>
   /// This class is conditionally compiled only for .NET Standard 2.0 and .NET Framework 4.7.2 (when STANDARD_OR_OLD_FRAMEWORKS is defined).
   /// It provides polyfills for methods that exist natively in modern .NET versions but are missing in older frameworks.
   /// </para>
   /// <para>
   /// <strong>Included polyfills:</strong>
   /// </para>
   /// <list type="bullet">
   /// <item><description>string.EndsWith(char) and <see cref="string.StartsWith(string)"/> - Added in .NET Core 2.1</description></item>
   /// <item><description>Enumerable.SkipLast - Added in .NET Core 2.0</description></item>
   /// <item><description>Enumerable.MaxBy - Added in .NET 6.0</description></item>
   /// </list>
   /// </remarks>
   public static class SystemExtensions
	{
		/// <summary>
		/// Determines whether the end of this string instance matches the specified character.
		/// </summary>
		/// <param name="me">The string to check.</param>
		/// <param name="c">The character to compare to the character at the end of this instance.</param>
		/// <returns><c>true</c> if <paramref name="c"/> matches the end of this instance; otherwise, <c>false</c>.</returns>
		/// <remarks>
		/// This is a polyfill for string.EndsWith(char) which was added in .NET Core 2.1.
		/// The implementation converts the character to a string and uses the existing <see cref="string.EndsWith(string)"/> overload.
		/// </remarks>
		/// <example>
		/// <code>
		/// string path = "file.txt";
		/// bool isText = path.EndsWith('t'); // true
		/// </code>
		/// </example>
		public static bool EndsWith(this string me, char c) => me.EndsWith(c.ToString());
		
		/// <summary>
		/// Determines whether the beginning of this string instance matches the specified character.
		/// </summary>
		/// <param name="me">The string to check.</param>
		/// <param name="c">The character to compare to the character at the beginning of this instance.</param>
		/// <returns><c>true</c> if <paramref name="c"/> matches the beginning of this instance; otherwise, <c>false</c>.</returns>
		/// <remarks>
		/// This is a polyfill for <see cref="string.StartsWith(string)"/> which was added in .NET Core 2.1.
		/// The implementation converts the character to a string and uses the existing <see cref="string.StartsWith(string)"/> overload.
		/// </remarks>
		/// <example>
		/// <code>
		/// string path = "/home/user";
		/// bool isAbsolute = path.StartsWith('/'); // true
		/// </code>
		/// </example>
		public static bool StartsWith(this string me, char c) => me.StartsWith(c.ToString());

		/// <summary>
		/// Bypasses a specified number of elements at the end of a sequence and returns the remaining elements.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The sequence to return elements from.</param>
		/// <param name="count">The number of elements to omit from the end of the returned sequence.</param>
		/// <returns>
		/// A sequence that contains the elements from <paramref name="source"/> minus <paramref name="count"/> elements from the end.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This is a polyfill for Enumerable.SkipLast which was added in .NET Core 2.0.
		/// The implementation uses Enumerable.Take with a calculated count: <c>source.Count() - count</c>.
		/// </para>
		/// <para>
		/// <strong>Performance consideration:</strong> This implementation calls Enumerable.Count which
		/// enumerates the entire sequence. For large or lazily-evaluated sequences, this may have performance implications.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var numbers = new[] { 1, 2, 3, 4, 5 };
		/// var result = numbers.SkipLast(2); // { 1, 2, 3 }
		/// </code>
		/// </example>
		public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count) => source.Take(source.Count() - count);
		
		/// <summary>
		/// Returns the element in a sequence that has the maximum value according to a specified key selector function.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
		/// <param name="source">A sequence of values to determine the maximum element of.</param>
		/// <param name="keySelector">A function to extract the key for each element.</param>
		/// <returns>
		/// The element in the sequence with the maximum key value, or <c>default(TSource)</c> if the sequence is empty.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This is a polyfill for Enumerable.MaxBy which was added in .NET 6.0.
		/// The implementation uses Enumerable.OrderByDescending followed by
		/// <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
		/// </para>
		/// <para>
		/// <strong>Performance consideration:</strong> This implementation performs a full sort of the sequence,
		/// which is less efficient than the native .NET 6+ implementation that uses a single pass algorithm.
		/// For performance-critical code with large sequences, consider alternative approaches.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var people = new[]
		/// {
		///     new { Name = "Alice", Age = 30 },
		///     new { Name = "Bob", Age = 25 },
		///     new { Name = "Charlie", Age = 35 }
		/// };
		/// 
		/// var oldest = people.MaxBy(p => p.Age); // Charlie (Age = 35)
		/// </code>
		/// </example>
		public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
			=> source.OrderByDescending(keySelector)
				.FirstOrDefault();
	}
	
	/// <summary>
	/// Provides a polyfill for <see cref="HashCode"/> which was added in .NET Core 2.1 / .NET Standard 2.1.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This struct provides a simple implementation of hash code combination for older frameworks.
	/// The native <see cref="HashCode"/> in modern .NET uses more sophisticated algorithms
	/// and provides better collision resistance.
	/// </para>
	/// <para>
	/// <strong>Algorithm:</strong> Uses the standard hash combining formula: <c>hash = hash * 31 + item.GetHashCode()</c>
	/// starting with a seed value of 17. This is wrapped in an <c>unchecked</c> block to allow integer overflow.
	/// </para>
	/// <para>
	/// <strong>Limitation:</strong> Unlike the native <see cref="HashCode"/>, this polyfill doesn't provide
	/// the full API (Add methods, ToHashCode, etc.). It only provides the static <see cref="Combine"/> method.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// public class Person
	/// {
	///     public string FirstName { get; set; }
	///     public string LastName { get; set; }
	///     public int Age { get; set; }
	///     
	///     public override int GetHashCode()
	///     {
	///         return HashCode.Combine(FirstName, LastName, Age);
	///     }
	/// }
	/// </code>
	/// </example>
	public struct HashCode
	{
		/// <summary>
		/// Combines multiple hash codes into a single hash code.
		/// </summary>
		/// <param name="values">The objects whose hash codes are to be combined.</param>
		/// <returns>A combined hash code for the specified values.</returns>
		/// <remarks>
		/// <para>
		/// This method iterates through all provided values, calling <see cref="object.GetHashCode()"/> on each,
		/// and combines them using the formula: <c>hash = hash * 31 + value.GetHashCode()</c>.
		/// </para>
		/// <para>
		/// <strong>Important:</strong> Null values are not handled specially and will throw a <see cref="NullReferenceException"/>.
		/// Ensure all values are non-null before calling this method, or handle nulls in your calling code.
		/// </para>
		/// <para>
		/// <strong>Algorithm notes:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Initial seed: 17 (a prime number)</description></item>
		/// <item><description>Multiplier: 31 (a prime number chosen for good distribution)</description></item>
		/// <item><description>Uses <c>unchecked</c> to allow overflow (deliberate for hash functions)</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Combining multiple values
		/// int hash1 = HashCode.Combine("Alice", 30, DateTime.Now);
		/// 
		/// // Use in GetHashCode override
		/// public override int GetHashCode()
		/// {
		///     return HashCode.Combine(FirstName ?? "", LastName ?? "", Age);
		/// }
		/// </code>
		/// </example>
		public static int Combine(params object[] values)
		{
			unchecked
			{
				var hash = 17;
				foreach (var value in values)
				{
					hash = hash * 31 + value.GetHashCode();
				}
				return hash;
			}
		}
	}

	// https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Index.cs
	// https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Range.cs

	/// <summary>
	/// Represents a type that can be used to index a collection either from the start or the end.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is a polyfill for <see cref="System.Index"/> which was added in C# 8.0 / .NET Core 3.0 / .NET Standard 2.1.
	/// The <see cref="Index"/> struct enables the C# 8.0 index syntax (<c>^</c> operator) for indexing from the end of collections.
	/// </para>
	/// <para>
	/// <strong>Index syntax support:</strong>
	/// </para>
	/// <code>
	/// int[] array = { 1, 2, 3, 4, 5 };
	/// int lastElement = array[^1];  // 5 (last element)
	/// int secondLast = array[^2];   // 4 (second from end)
	/// int firstElement = array[0];  // 1 (from start)
	/// </code>
	/// <para>
	/// <strong>Internal representation:</strong> Uses a single <c>int</c> field where negative values indicate "from end" indexing.
	/// The bitwise complement operator (<c>~</c>) is used to encode/decode the value.
	/// </para>
	/// <para>
	/// <strong>Source:</strong> This implementation is based on the official .NET runtime source code from GitHub.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Creating indices
	/// var first = Index.FromStart(0);      // First element
	/// var last = Index.FromEnd(1);         // Last element
	/// var secondLast = Index.FromEnd(2);   // Second from end
	/// 
	/// // Using with arrays
	/// int[] numbers = { 10, 20, 30, 40, 50 };
	/// int value = numbers[^1]; // 50
	/// 
	/// // Implicit conversion from int
	/// Index idx = 2; // Same as Index.FromStart(2)
	/// 
	/// // Getting offset
	/// int offset = last.GetOffset(numbers.Length); // 4
	/// </code>
	/// </example>
	public readonly struct Index : IEquatable<Index>
	{
		private readonly int _value;

		/// <summary>
		/// Constructs an <see cref="Index"/> using a value and indicating if the index is from the start or from the end.
		/// </summary>
		/// <param name="value">The index value. Must be zero or a positive number.</param>
		/// <param name="fromEnd">
		/// <c>true</c> if the index is from the end; <c>false</c> if from the start. Default is <c>false</c>.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="value"/> is negative.
		/// </exception>
		/// <remarks>
		/// <para>
		/// When constructed from the end (<paramref name="fromEnd"/> = <c>true</c>):
		/// </para>
		/// <list type="bullet">
		/// <item><description>Index value 1 means the last element</description></item>
		/// <item><description>Index value 0 means beyond the last element (invalid for most operations)</description></item>
		/// </list>
		/// <para>
		/// The internal representation uses the bitwise complement (<c>~</c>) of the value when <paramref name="fromEnd"/> is <c>true</c>.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var fromStart = new Index(2, fromEnd: false); // Third element
		/// var fromEnd = new Index(1, fromEnd: true);    // Last element
		/// 
		/// // Equivalent using factory methods
		/// var start2 = Index.FromStart(2);
		/// var end1 = Index.FromEnd(1);
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Index(int value, bool fromEnd = false)
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
			}

			if (fromEnd)
				_value = ~value;
			else
				_value = value;
		}

		// The following private constructors mainly created for perf reason to avoid the checks
		private Index(int value)
		{
			_value = value;
		}

		/// <summary>
		/// Gets an <see cref="Index"/> pointing at the first element.
		/// </summary>
		/// <value>An <see cref="Index"/> equivalent to <c>new Index(0)</c> or <c>^0</c> from start.</value>
		/// <remarks>
		/// This is equivalent to the index <c>0</c> in array access.
		/// </remarks>
		public static Index Start => new Index(0);

		/// <summary>
		/// Gets an <see cref="Index"/> pointing beyond the last element.
		/// </summary>
		/// <value>An <see cref="Index"/> equivalent to <c>new Index(0, fromEnd: true)</c> or <c>^0</c>.</value>
		/// <remarks>
		/// This index points to a position one past the last element, similar to <c>array.Length</c>.
		/// It's primarily used with <see cref="Range"/> for slicing operations.
		/// </remarks>
		public static Index End => new Index(~0);

		/// <summary>
		/// Creates an <see cref="Index"/> from the start at the position indicated by the value.
		/// </summary>
		/// <param name="value">The index value from the start. Must be non-negative.</param>
		/// <returns>An <see cref="Index"/> representing the specified position from the start.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="value"/> is negative.
		/// </exception>
		/// <remarks>
		/// This method is equivalent to using the implicit conversion from <c>int</c> to <see cref="Index"/>.
		/// </remarks>
		/// <example>
		/// <code>
		/// var third = Index.FromStart(2); // Third element (zero-based)
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Index FromStart(int value)
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
			}

			return new Index(value);
		}

		/// <summary>
		/// Creates an <see cref="Index"/> from the end at the position indicated by the value.
		/// </summary>
		/// <param name="value">The index value from the end. Must be non-negative.</param>
		/// <returns>An <see cref="Index"/> representing the specified position from the end.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="value"/> is negative.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method creates indices that work with the <c>^</c> operator in C# 8.0+:
		/// </para>
		/// <list type="bullet">
		/// <item><description><c>FromEnd(1)</c> is equivalent to <c>^1</c> (last element)</description></item>
		/// <item><description><c>FromEnd(2)</c> is equivalent to <c>^2</c> (second from end)</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var last = Index.FromEnd(1);    // Last element
		/// var secondLast = Index.FromEnd(2); // Second from end
		/// 
		/// // Equivalent syntax in C# 8.0+
		/// var lastAlt = ^1;
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Index FromEnd(int value)
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
			}

			return new Index(~value);
		}

		/// <summary>
		/// Gets the index value.
		/// </summary>
		/// <value>The absolute index value, regardless of whether it's from the start or end.</value>
		/// <remarks>
		/// For indices from the end, this returns the positive value without the collection length.
		/// Use <see cref="GetOffset"/> to get the actual array index given a collection length.
		/// </remarks>
		/// <example>
		/// <code>
		/// var start = new Index(5);
		/// Console.WriteLine(start.Value);     // 5
		/// Console.WriteLine(start.IsFromEnd); // false
		/// 
		/// var end = new Index(3, fromEnd: true);
		/// Console.WriteLine(end.Value);       // 3
		/// Console.WriteLine(end.IsFromEnd);   // true
		/// </code>
		/// </example>
		public int Value
		{
			get
			{
				if (_value < 0)
				{
					return ~_value;
				} else
				{
					return _value;
				}
			}
		}

		/// <summary>
		/// Indicates whether the index is from the start or the end.
		/// </summary>
		/// <value><c>true</c> if the index counts from the end; otherwise, <c>false</c>.</value>
		/// <remarks>
		/// This property is determined by checking if the internal <c>_value</c> is negative.
		/// </remarks>
		public bool IsFromEnd => _value < 0;

		/// <summary>
		/// Calculates the offset from the start using the given collection length.
		/// </summary>
		/// <param name="length">The length of the collection that the <see cref="Index"/> will be used with. Must be a positive value.</param>
		/// <returns>The zero-based offset from the start of the collection.</returns>
		/// <remarks>
		/// <para>
		/// For performance reasons, this method doesn't validate:
		/// </para>
		/// <list type="bullet">
		/// <item><description>The input <paramref name="length"/> parameter against negative values</description></item>
		/// <item><description>The returned offset against negative values</description></item>
		/// <item><description>Whether the returned offset is greater than the input length</description></item>
		/// </list>
		/// <para>
		/// It's expected that <see cref="Index"/> will be used with collections which always have non-negative length/count.
		/// If the returned offset is negative or out of range and used to index a collection, it will throw an appropriate
		/// exception from the collection itself.
		/// </para>
		/// <para>
		/// <strong>Calculation for from-end indices:</strong>
		/// </para>
		/// <para>
		/// The formula <c>offset = length - (~value)</c> is equivalent to <c>offset = length + value + 1</c>,
		/// which gives the correct offset for indices counting from the end.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var array = new[] { 10, 20, 30, 40, 50 }; // Length = 5
		/// 
		/// var start = Index.FromStart(2);
		/// int offset1 = start.GetOffset(array.Length); // 2
		/// 
		/// var end = Index.FromEnd(2);
		/// int offset2 = end.GetOffset(array.Length);   // 3 (5 - 2 = 3)
		/// 
		/// Console.WriteLine(array[offset1]); // 30
		/// Console.WriteLine(array[offset2]); // 40
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetOffset(int length)
		{
			var offset = _value;
			if (IsFromEnd)
			{
				// offset = length - (~value)
				// offset = length + (~(~value) + 1)
				// offset = length + value + 1

				offset += length + 1;
			}
			return offset;
		}

		/// <summary>
		/// Indicates whether the current <see cref="Index"/> object is equal to another object of the same type.
		/// </summary>
		/// <param name="value">An object to compare with this object.</param>
		/// <returns><c>true</c> if the current object is equal to the <paramref name="value"/> parameter; otherwise, <c>false</c>.</returns>
		public override bool Equals(object? value) => value is Index && _value == ((Index)value)._value;

		/// <summary>
		/// Indicates whether the current <see cref="Index"/> object is equal to another <see cref="Index"/> object.
		/// </summary>
		/// <param name="other">An <see cref="Index"/> to compare with this object.</param>
		/// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
		public bool Equals(Index other) => _value == other._value;

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public override int GetHashCode() => _value;

		/// <summary>
		/// Converts an integer number to an <see cref="Index"/>.
		/// </summary>
		/// <param name="value">The integer value to convert.</param>
		/// <returns>An <see cref="Index"/> representing the specified position from the start.</returns>
		/// <remarks>
		/// This implicit conversion allows natural syntax like <c>Index idx = 5;</c>.
		/// </remarks>
		public static implicit operator Index(int value) => FromStart(value);

		/// <summary>
		/// Converts the value of the current <see cref="Index"/> object to its equivalent string representation.
		/// </summary>
		/// <returns>
		/// The string representation of the index. From-end indices are prefixed with <c>^</c>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Examples:
		/// </para>
		/// <list type="bullet">
		/// <item><description>From start: <c>"5"</c></description></item>
		/// <item><description>From end: <c>"^3"</c></description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var start = Index.FromStart(5);
		/// Console.WriteLine(start.ToString()); // "5"
		/// 
		/// var end = Index.FromEnd(3);
		/// Console.WriteLine(end.ToString());   // "^3"
		/// </code>
		/// </example>
		public override string ToString()
		{
			if (IsFromEnd)
				return "^" + ((uint)Value).ToString();

			return ((uint)Value).ToString();
		}
	}

	/// <summary>
	/// Represents a range that has start and end indices.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is a polyfill for <see cref="System.Range"/> which was added in C# 8.0 / .NET Core 3.0 / .NET Standard 2.1.
	/// The <see cref="Range"/> struct enables the C# 8.0 range syntax (<c>..</c> operator) for slicing collections.
	/// </para>
	/// <para>
	/// <strong>Range syntax support:</strong>
	/// </para>
	/// <code>
	/// int[] array = { 1, 2, 3, 4, 5 };
	/// int[] subArray1 = array[0..2];   // { 1, 2 } - from index 0 to 2 (exclusive)
	/// int[] subArray2 = array[1..^0];  // { 2, 3, 4, 5 } - from index 1 to end
	/// int[] subArray3 = array[^3..^1]; // { 3, 4 } - last 3 to last 1 (exclusive)
	/// </code>
	/// <para>
	/// <strong>Important:</strong> The <see cref="End"/> index is exclusive, meaning it points to the position
	/// after the last element to include in the range.
	/// </para>
	/// <para>
	/// <strong>Source:</strong> This implementation is based on the official .NET runtime source code from GitHub.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Creating ranges
	/// var allRange = Range.All;                          // 0..^0 (entire collection)
	/// var fromThird = Range.StartAt(Index.FromStart(2)); // 2..^0 (from third to end)
	/// var upToThird = Range.EndAt(Index.FromStart(2));   // 0..2 (from start to third)
	/// 
	/// // Using with arrays
	/// int[] numbers = { 10, 20, 30, 40, 50 };
	/// var slice = numbers[1..4]; // { 20, 30, 40 }
	/// 
	/// // Getting offset and length
	/// var range = new Range(Index.FromStart(1), Index.FromEnd(1));
	/// var (offset, length) = range.GetOffsetAndLength(numbers.Length);
	/// // offset = 1, length = 3 (elements at indices 1, 2, 3)
	/// </code>
	/// </example>
	public readonly struct Range : IEquatable<Range>
	{
		/// <summary>
		/// Gets the inclusive start index of the <see cref="Range"/>.
		/// </summary>
		/// <value>An <see cref="Index"/> representing where the range begins.</value>
		/// <remarks>
		/// This index is inclusive, meaning the element at this position is included in the range.
		/// </remarks>
		public Index Start { get; }

		/// <summary>
		/// Gets the exclusive end index of the <see cref="Range"/>.
		/// </summary>
		/// <value>An <see cref="Index"/> representing where the range ends.</value>
		/// <remarks>
		/// This index is exclusive, meaning the element at this position is NOT included in the range.
		/// It points to the position immediately after the last element in the range.
		/// </remarks>
		public Index End { get; }

		/// <summary>
		/// Constructs a <see cref="Range"/> object using the start and end indices.
		/// </summary>
		/// <param name="start">The inclusive start index of the range.</param>
		/// <param name="end">The exclusive end index of the range.</param>
		/// <remarks>
		/// The <paramref name="end"/> index is exclusive. For example, <c>new Range(1, 4)</c>
		/// includes elements at indices 1, 2, and 3, but not 4.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Create a range from index 1 to 4 (exclusive)
		/// var range = new Range(Index.FromStart(1), Index.FromStart(4));
		/// 
		/// // Using C# 8.0 range syntax
		/// var rangeAlt = 1..4; // Equivalent
		/// 
		/// // Range from second element to second-to-last
		/// var middleRange = new Range(Index.FromStart(1), Index.FromEnd(1));
		/// // or: 1..^1
		/// </code>
		/// </example>
		public Range(Index start, Index end)
		{
			Start = start;
			End = end;
		}

		/// <summary>
		/// Indicates whether the current <see cref="Range"/> object is equal to another object of the same type.
		/// </summary>
		/// <param name="value">An object to compare with this object.</param>
		/// <returns><c>true</c> if the current object is equal to the <paramref name="value"/> parameter; otherwise, <c>false</c>.</returns>
		public override bool Equals(object? value) =>
			 value is Range r &&
			 r.Start.Equals(Start) &&
			 r.End.Equals(End);

		/// <summary>
		/// Indicates whether the current <see cref="Range"/> object is equal to another <see cref="Range"/> object.
		/// </summary>
		/// <param name="other">A <see cref="Range"/> to compare with this object.</param>
		/// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
		public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		/// <remarks>
		/// Uses a simple combining formula: <c>Start.GetHashCode() * 31 + End.GetHashCode()</c>.
		/// </remarks>
		public override int GetHashCode()
		{
			return Start.GetHashCode() * 31 + End.GetHashCode();
		}

		/// <summary>
		/// Converts the value of the current <see cref="Range"/> object to its equivalent string representation.
		/// </summary>
		/// <returns>
		/// The string representation of the range in the format <c>"start..end"</c>.
		/// </returns>
		/// <example>
		/// <code>
		/// var range1 = new Range(Index.FromStart(1), Index.FromStart(4));
		/// Console.WriteLine(range1.ToString()); // "1..4"
		/// 
		/// var range2 = new Range(Index.FromStart(0), Index.FromEnd(1));
		/// Console.WriteLine(range2.ToString()); // "0..^1"
		/// 
		/// var range3 = Range.All;
		/// Console.WriteLine(range3.ToString()); // "0..^0"
		/// </code>
		/// </example>
		public override string ToString()
		{
			return Start + ".." + End;
		}

		/// <summary>
		/// Creates a <see cref="Range"/> object starting from the specified start index to the end of the collection.
		/// </summary>
		/// <param name="start">The inclusive start index.</param>
		/// <returns>A <see cref="Range"/> from <paramref name="start"/> to the end.</returns>
		/// <remarks>
		/// This is equivalent to the range syntax <c>start..^0</c> or <c>start..</c>.
		/// </remarks>
		/// <example>
		/// <code>
		/// var range = Range.StartAt(Index.FromStart(2));
		/// // Equivalent to: 2..^0 or 2..
		/// 
		/// int[] array = { 1, 2, 3, 4, 5 };
		/// var result = array[range]; // { 3, 4, 5 }
		/// </code>
		/// </example>
		public static Range StartAt(Index start) => new Range(start, Index.End);

		/// <summary>
		/// Creates a <see cref="Range"/> object starting from the first element to the specified end index.
		/// </summary>
		/// <param name="end">The exclusive end index.</param>
		/// <returns>A <see cref="Range"/> from the start to <paramref name="end"/>.</returns>
		/// <remarks>
		/// This is equivalent to the range syntax <c>0..end</c> or <c>..end</c>.
		/// </remarks>
		/// <example>
		/// <code>
		/// var range = Range.EndAt(Index.FromStart(3));
		/// // Equivalent to: 0..3 or ..3
		/// 
		/// int[] array = { 1, 2, 3, 4, 5 };
		/// var result = array[range]; // { 1, 2, 3 }
		/// </code>
		/// </example>
		public static Range EndAt(Index end) => new Range(Index.Start, end);

		/// <summary>
		/// Gets a <see cref="Range"/> object representing the entire collection.
		/// </summary>
		/// <value>A <see cref="Range"/> from the start to the end.</value>
		/// <remarks>
		/// This is equivalent to the range syntax <c>0..^0</c> or <c>..</c> (entire range).
		/// </remarks>
		/// <example>
		/// <code>
		/// var range = Range.All;
		/// // Equivalent to: .. or 0..^0
		/// 
		/// int[] array = { 1, 2, 3, 4, 5 };
		/// var result = array[range]; // { 1, 2, 3, 4, 5 } (entire array)
		/// </code>
		/// </example>
		public static Range All => new Range(Index.Start, Index.End);

		/// <summary>
		/// Calculates the start offset and length of the range using a collection length.
		/// </summary>
		/// <param name="length">The length of the collection that the range will be used with. Must be a positive value.</param>
		/// <returns>
		/// A tuple containing:
		/// <list type="bullet">
		/// <item><description><c>Offset</c>: The zero-based starting position in the collection</description></item>
		/// <item><description><c>Length</c>: The number of elements in the range</description></item>
		/// </list>
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when:
		/// <list type="bullet">
		/// <item><description>The calculated end position is greater than <paramref name="length"/></description></item>
		/// <item><description>The calculated start position is greater than the calculated end position</description></item>
		/// </list>
		/// </exception>
		/// <remarks>
		/// <para>
		/// For performance reasons, this method doesn't validate the input <paramref name="length"/> parameter against negative values.
		/// It's expected that <see cref="Range"/> will be used with collections which always have non-negative length/count.
		/// </para>
		/// <para>
		/// The method validates that the range is within bounds using unsigned integer comparison for efficiency.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// int[] array = { 10, 20, 30, 40, 50 }; // Length = 5
		/// 
		/// var range1 = new Range(Index.FromStart(1), Index.FromStart(4));
		/// var (offset1, length1) = range1.GetOffsetAndLength(array.Length);
		/// // offset1 = 1, length1 = 3 (elements: 20, 30, 40)
		/// 
		/// var range2 = new Range(Index.FromStart(2), Index.FromEnd(1));
		/// var (offset2, length2) = range2.GetOffsetAndLength(array.Length);
		/// // offset2 = 2, length2 = 2 (elements: 30, 40)
		/// 
		/// // Use to slice manually
		/// var slice = new int[length1];
		/// Array.Copy(array, offset1, slice, 0, length1);
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public (int Offset, int Length) GetOffsetAndLength(int length)
		{
			int start;
			var startIndex = Start;
			if (startIndex.IsFromEnd)
				start = length - startIndex.Value;
			else
				start = startIndex.Value;

			int end;
			var endIndex = End;
			if (endIndex.IsFromEnd)
				end = length - endIndex.Value;
			else
				end = endIndex.Value;

			if ((uint)end > (uint)length || (uint)start > (uint)end)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			return (start, end - start);
		}
	}
}

namespace Fuxion.Runtime.CompilerServices
{
	/// <summary>
	/// Provides runtime helper methods for compiler-generated code, specifically for range operations.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class provides polyfills for methods in <see cref="System.Runtime.CompilerServices.RuntimeHelpers"/>
	/// that were added in newer .NET versions. These methods are used by the C# compiler to support features
	/// like range indexing with arrays.
	/// </para>
	/// <para>
	/// <strong>Note:</strong> This is in the <c>Fuxion.Runtime.CompilerServices</c> namespace to avoid conflicts
	/// with the actual <see cref="System.Runtime.CompilerServices.RuntimeHelpers"/> in newer frameworks.
	/// </para>
	/// </remarks>
	public static class RuntimeHelpers
	{
		/// <summary>
		/// Slices the specified array using the specified range.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="array">The array to slice.</param>
		/// <param name="range">The range that specifies which portion of the array to return.</param>
		/// <returns>
		/// A new array containing the elements specified by the <paramref name="range"/>.
		/// Returns <see cref="Array.Empty{T}"/> if the range specifies an empty slice.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="array"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the <paramref name="range"/> is out of bounds for the <paramref name="array"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method is used by the C# compiler to support range slicing syntax like <c>array[1..4]</c>.
		/// The method creates a new array and copies the specified elements from the source array.
		/// </para>
		/// <para>
		/// <strong>Type handling:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>If <c>T</c> is a value type or the array is exactly of type <c>T[]</c>, creates a <c>T[]</c> directly</description></item>
		/// <item><description>If the array is actually a <c>U[]</c> where <c>U:T</c> (covariance), creates an array of the actual runtime type</description></item>
		/// </list>
		/// <para>
		/// <strong>Performance optimization:</strong> Returns <see cref="Array.Empty{T}"/> for empty ranges
		/// to avoid unnecessary allocations.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// int[] array = { 1, 2, 3, 4, 5 };
		/// 
		/// // Using C# 8.0 range syntax (compiler calls GetSubArray internally)
		/// int[] slice1 = array[1..4]; // { 2, 3, 4 }
		/// 
		/// // Manual usage
		/// var range = new Range(Index.FromStart(1), Index.FromEnd(1));
		/// int[] slice2 = RuntimeHelpers.GetSubArray(array, range); // { 2, 3, 4 }
		/// 
		/// // Empty range
		/// int[] empty = array[3..3]; // { } (empty array)
		/// 
		/// // Covariant array example
		/// object[] objects = new string[] { "a", "b", "c" };
		/// object[] objectSlice = RuntimeHelpers.GetSubArray(objects, 0..2); // { "a", "b" }
		/// // Result is string[], not object[]
		/// </code>
		/// </example>
		public static T[] GetSubArray<T>(T[] array, Range range)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			(int offset, int length) = range.GetOffsetAndLength(array.Length);

			if (default(T) != null || typeof(T[]) == array.GetType())
			{
				// We know the type of the array to be exactly T[].

				if (length == 0)
				{
					return Array.Empty<T>();
				}

				var dest = new T[length];
				Array.Copy(array, offset, dest, 0, length);
				return dest;
			} else
			{
				// The array is actually a U[] where U:T.
				var dest = (T[])Array.CreateInstance(array.GetType().GetElementType(), length);
				Array.Copy(array, offset, dest, 0, length);
				return dest;
			}
		}
	}
}

#endif