using Fuxion.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fuxion;

/// <summary>
/// Provides time-related extension methods for date/time types and numeric types.
/// </summary>
/// <remarks>
/// This class extends the Fuxion extension system to provide convenient time operations including:
/// <list type="bullet">
/// <item><description>EPOCH time conversions</description></item>
/// <item><description>TimeSpan creation from numeric values</description></item>
/// <item><description>Date/time averaging</description></item>
/// <item><description>Human-readable time formatting</description></item>
/// </list>
/// </remarks>
public static class TimeExtensions
{
	private static readonly DateTime EpochStartTime = new(1970, 1, 1);

	/// <summary>
	/// Extension methods for accessing time operations on DateTime through the Fx.Time property.
	/// </summary>
	extension(FuxionExtensions<DateTime> me)
	{
		/// <summary>
		/// Provides time-related extension operations for this <see cref="DateTime" /> value.
		/// </summary>
		/// <value>A <see cref="TimeExtensions{T}"/> instance for performing time operations on the DateTime value.</value>
		/// <example>
		/// <code>
		/// var date = DateTime.Now;
		/// var epochSeconds = date.Fx.Time.ToEpochSeconds();
		/// </code>
		/// </example>
		public TimeExtensions<DateTime> Time
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	/// <summary>
	/// Extension methods for accessing time operations on TimeSpan through the Fx.Time property.
	/// </summary>
	extension(FuxionExtensions<TimeSpan> me)
	{
		/// <summary>
		/// Provides time-related extension operations for this <see cref="TimeSpan" /> value.
		/// </summary>
		/// <value>A <see cref="TimeExtensions{T}"/> instance for performing time operations on the TimeSpan value.</value>
		/// <example>
		/// <code>
		/// var duration = TimeSpan.FromHours(2.5);
		/// var readable = duration.Fx.Time.ToTimeString();
		/// </code>
		/// </example>
		public TimeExtensions<TimeSpan> Time
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	/// <summary>
	/// Extension methods for DateTime providing direct access to EPOCH start time.
	/// </summary>
	extension(DateTime me)
	{
		/// <summary>
		/// Gets the EPOCH start time (January 1, 1970, 00:00:00 UTC).
		/// </summary>
		/// <value>A DateTime representing 1970-01-01 00:00:00.</value>
		/// <example>
		/// <code>
		/// DateTime epoch = DateTime.Now.EpochStartTime; // 1970-01-01 00:00:00
		/// </code>
		/// </example>
		public static DateTime EpochStartTime => TimeExtensions.EpochStartTime;
	}
	
	/// <summary>
	/// Extension methods for collections of DateTime values.
	/// </summary>
	extension(IEnumerable<DateTime> me)
	{
		/// <summary>
		/// Calculates the average of a sequence of DateTime values.
		/// </summary>
		/// <returns>A DateTime representing the average of all values in the sequence.</returns>
		/// <remarks>
		/// The average is calculated using the Ticks property of each DateTime.
		/// </remarks>
		/// <example>
		/// <code>
		/// var dates = new[] 
		/// {
		///     new DateTime(2024, 1, 1),
		///     new DateTime(2024, 1, 15),
		///     new DateTime(2024, 1, 31)
		/// };
		/// var average = dates.Average(); // Approximately 2024-01-16
		/// </code>
		/// </example>
		public DateTime Average() => new((long)me.Average(dt => dt.Ticks));
	}
	
	/// <summary>
	/// Extension methods for collections of DateTimeOffset values.
	/// </summary>
	extension(IEnumerable<DateTimeOffset> me)
	{
		/// <summary>
		/// Calculates the average of a sequence of DateTimeOffset values.
		/// </summary>
		/// <returns>A DateTime representing the average of all UTC values in the sequence.</returns>
		/// <remarks>
		/// The average is calculated using the UtcTicks property of each DateTimeOffset.
		/// The result is a DateTime (not DateTimeOffset).
		/// </remarks>
		/// <example>
		/// <code>
		/// var offsets = new[] 
		/// {
		///     new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
		///     new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero)
		/// };
		/// var average = offsets.Average(); // Approximately 2024-01-16
		/// </code>
		/// </example>
		public DateTime Average() => new((long)me.Average(dto => dto.UtcTicks));
	}
	
	/// <summary>
	/// Extension methods for collections of TimeSpan values.
	/// </summary>
	extension(IEnumerable<TimeSpan> me)
	{
		/// <summary>
		/// Calculates the average of a sequence of TimeSpan values.
		/// </summary>
		/// <returns>A TimeSpan representing the average of all values in the sequence.</returns>
		/// <example>
		/// <code>
		/// var durations = new[] 
		/// {
		///     TimeSpan.FromHours(1),
		///     TimeSpan.FromHours(3),
		///     TimeSpan.FromHours(2)
		/// };
		/// var average = durations.Average(); // 2 hours
		/// </code>
		/// </example>
		public TimeSpan Average() => new((long)me.Average(dto => dto.Ticks));
	}

	/// <summary>
	/// Extension methods for int values to create TimeSpan instances.
	/// </summary>
	extension(int me)
	{
		/// <summary>Gets a TimeSpan representing the specified number of ticks.</summary>
		/// <example><code>int value = 10000; var ts = value.Ticks;</code></example>
		public TimeSpan Ticks => TimeSpan.FromTicks(me);
		
		/// <summary>Gets a TimeSpan representing the specified number of milliseconds.</summary>
		/// <example><code>int value = 500; var ts = value.Milliseconds; // 0.5 seconds</code></example>
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		
		/// <summary>Gets a TimeSpan representing the specified number of seconds.</summary>
		/// <example><code>int value = 30; var ts = value.Seconds; // 30 seconds</code></example>
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		
		/// <summary>Gets a TimeSpan representing the specified number of minutes.</summary>
		/// <example><code>int value = 15; var ts = value.Minutes; // 15 minutes</code></example>
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		
		/// <summary>Gets a TimeSpan representing the specified number of hours.</summary>
		/// <example><code>int value = 2; var ts = value.Hours; // 2 hours</code></example>
		public TimeSpan Hours => TimeSpan.FromHours(me);
		
		/// <summary>Gets a TimeSpan representing the specified number of days.</summary>
		/// <example><code>int value = 7; var ts = value.Days; // 1 week</code></example>
		public TimeSpan Days => TimeSpan.FromDays(me);
	}

	/// <summary>
	/// Extension methods for uint values to create TimeSpan instances.
	/// </summary>
	extension(uint me)
	{
		/// <summary>Gets a TimeSpan representing the specified number of ticks.</summary>
		public TimeSpan Ticks => TimeSpan.FromTicks(me);
		/// <summary>Gets a TimeSpan representing the specified number of milliseconds.</summary>
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of seconds.</summary>
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of minutes.</summary>
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		/// <summary>Gets a TimeSpan representing the specified number of hours.</summary>
		public TimeSpan Hours => TimeSpan.FromHours(me);
		/// <summary>Gets a TimeSpan representing the specified number of days.</summary>
		public TimeSpan Days => TimeSpan.FromDays(me);
	}

	/// <summary>
	/// Extension methods for long values to create TimeSpan instances.
	/// </summary>
	extension(long me)
	{
		/// <summary>Gets a TimeSpan representing the specified number of ticks.</summary>
		public TimeSpan Ticks => TimeSpan.FromTicks(me);
		/// <summary>Gets a TimeSpan representing the specified number of milliseconds.</summary>
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of seconds.</summary>
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of minutes.</summary>
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		/// <summary>Gets a TimeSpan representing the specified number of hours.</summary>
		public TimeSpan Hours => TimeSpan.FromHours(me);
		/// <summary>Gets a TimeSpan representing the specified number of days.</summary>
		public TimeSpan Days => TimeSpan.FromDays(me);
	}
	
	/// <summary>
	/// Extension methods for ulong values to create TimeSpan instances.
	/// </summary>
	/// <remarks>Note: Ticks property is not available for ulong as it may overflow long.</remarks>
	extension(ulong me)
	{
		/// <summary>Gets a TimeSpan representing the specified number of milliseconds.</summary>
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of seconds.</summary>
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of minutes.</summary>
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		/// <summary>Gets a TimeSpan representing the specified number of hours.</summary>
		public TimeSpan Hours => TimeSpan.FromHours(me);
		/// <summary>Gets a TimeSpan representing the specified number of days.</summary>
		public TimeSpan Days => TimeSpan.FromDays(me);
	}
	
	/// <summary>
	/// Extension methods for double values to create TimeSpan instances.
	/// </summary>
	extension(double me)
	{
		/// <summary>Gets a TimeSpan representing the specified number of milliseconds.</summary>
		/// <example><code>double value = 1500.5; var ts = value.Milliseconds; // 1.5005 seconds</code></example>
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of seconds.</summary>
		/// <example><code>double value = 90.5; var ts = value.Seconds; // 1 minute 30.5 seconds</code></example>
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		/// <summary>Gets a TimeSpan representing the specified number of minutes.</summary>
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		/// <summary>Gets a TimeSpan representing the specified number of hours.</summary>
		/// <example><code>double value = 2.5; var ts = value.Hours; // 2 hours 30 minutes</code></example>
		public TimeSpan Hours => TimeSpan.FromHours(me);
		/// <summary>Gets a TimeSpan representing the specified number of days.</summary>
		public TimeSpan Days => TimeSpan.FromDays(me);
	}
	
	/// <summary>
	/// Extension methods for long values wrapped in TimeExtensions for EPOCH conversions.
	/// </summary>
	extension(TimeExtensions<long> me)
	{
		/// <summary>
		/// Converts EPOCH seconds to a DateTime.
		/// </summary>
		/// <returns>A DateTime representing the EPOCH time plus the specified seconds.</returns>
		/// <example>
		/// <code>
		/// long epochSeconds = 1609459200; // 2021-01-01 00:00:00
		/// DateTime date = epochSeconds.Fx.Time.ToEpochDateTimeFromSeconds();
		/// </code>
		/// </example>
		public DateTime ToEpochDateTimeFromSeconds() => EpochStartTime.AddSeconds(me.Value);
		
		/// <summary>
		/// Converts EPOCH milliseconds to a DateTime.
		/// </summary>
		/// <returns>A DateTime representing the EPOCH time plus the specified milliseconds.</returns>
		/// <example>
		/// <code>
		/// long epochMs = 1609459200000; // 2021-01-01 00:00:00
		/// DateTime date = epochMs.Fx.Time.ToEpochDateTimeFromMilliseconds();
		/// </code>
		/// </example>
		public DateTime ToEpochDateTimeFromMilliseconds() => EpochStartTime.AddMilliseconds(me.Value);
	}
	
	/// <summary>
	/// Extension methods for ulong values wrapped in TimeExtensions for EPOCH conversions.
	/// </summary>
	extension(TimeExtensions<ulong> me)
	{
		/// <summary>Converts EPOCH seconds to a DateTime.</summary>
		public DateTime ToEpochDateTimeFromSeconds() => EpochStartTime.AddSeconds(me.Value);
		/// <summary>Converts EPOCH milliseconds to a DateTime.</summary>
		public DateTime ToEpochDateTimeFromMilliseconds() => EpochStartTime.AddMilliseconds(me.Value);
	}
	
	/// <summary>
	/// Extension methods for double values wrapped in TimeExtensions for EPOCH conversions.
	/// </summary>
	extension(TimeExtensions<double> me)
	{
		/// <summary>Converts EPOCH seconds (with fractional seconds) to a DateTime.</summary>
		public DateTime ToEpochDateTimeFromSeconds() => EpochStartTime.AddSeconds(me.Value);
		/// <summary>Converts EPOCH milliseconds (with fractional milliseconds) to a DateTime.</summary>
		public DateTime ToEpochDateTimeFromMilliseconds() => EpochStartTime.AddMilliseconds(me.Value);
	}
	
	/// <summary>
	/// Extension methods for DateTime values wrapped in TimeExtensions for EPOCH conversions.
	/// </summary>
	extension(TimeExtensions<DateTime> me)
	{
		/// <summary>
		/// Converts a DateTime to EPOCH seconds.
		/// </summary>
		/// <param name="errorIfPrior1970">If true, returns an error response if the date is before 1970-01-01; otherwise, returns a negative value.</param>
		/// <returns>A Response containing the number of seconds since EPOCH (1970-01-01), or an error if the date is invalid.</returns>
		/// <example>
		/// <code>
		/// var date = new DateTime(2021, 1, 1);
		/// var result = date.Fx.Time.ToEpochSeconds();
		/// if (result.IsSuccess)
		/// {
		///     long seconds = result.Payload;
		///     Console.WriteLine($"EPOCH seconds: {seconds}");
		/// }
		/// </code>
		/// </example>
		public IResponse<long> ToEpochSeconds(bool errorIfPrior1970 = false)
			=> me.ToEpochMilliseconds(errorIfPrior1970).Match(
				r => ResponseExt.Get.SuccessPayload(r.Payload / 1000),
				r => r);

		/// <summary>
		/// Converts a DateTime to EPOCH milliseconds.
		/// </summary>
		/// <param name="errorIfPrior1970">If true, returns an error response if the date is before 1970-01-01; otherwise, returns a negative value.</param>
		/// <returns>A Response containing the number of milliseconds since EPOCH (1970-01-01), or an error if the date is invalid.</returns>
		/// <example>
		/// <code>
		/// var date = new DateTime(2021, 1, 1);
		/// var result = date.Fx.Time.ToEpochMilliseconds();
		/// if (result.IsSuccess)
		/// {
		///     long ms = result.Payload;
		///     Console.WriteLine($"EPOCH milliseconds: {ms}");
		/// }
		/// </code>
		/// </example>
		public IResponse<long> ToEpochMilliseconds(bool errorIfPrior1970 = false)
			=> (me.Value - EpochStartTime).TotalMilliseconds switch
			{
				< 0 when errorIfPrior1970 => ResponseExt.Get
					.InvalidData("DateTime cannot be prior 1/1/1970 to be converted to EPOCH date").AsPayload<long>(),
				var r => ResponseExt.Get.SuccessPayload((long)r)
			};
	}

	/// <summary>
	/// Extension methods for TimeSpan values wrapped in TimeExtensions.
	/// </summary>
	extension(TimeExtensions<TimeSpan> me)
	{
		/// <summary>
		/// Converts a TimeSpan to a human-readable string representation.
		/// </summary>
		/// <param name="numberOfElements">Maximum number of time components to include (1-5). Default is 5.</param>
		/// <param name="onlyLetters">If true, uses abbreviated letters (d, h, m, s, ms); otherwise, uses full words.</param>
		/// <returns>A formatted string representing the time span.</returns>
		/// <remarks>
		/// <para>The method formats up to 5 components in order: days, hours, minutes, seconds, milliseconds.</para>
		/// <para>Negative time spans are prefixed with "- ".</para>
		/// <para>Zero components are omitted unless all components are zero.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var duration = TimeSpan.FromSeconds(3665.5);
		/// 
		/// // Full format with all elements
		/// var full = duration.Fx.Time.ToTimeString();
		/// // "1 hour, 1 minute, 5 seconds, 500 milliseconds"
		/// 
		/// // Abbreviated format
		/// var short = duration.Fx.Time.ToTimeString(onlyLetters: true);
		/// // "1 h 1 m 5 s 500 ms"
		/// 
		/// // Limited to 2 elements
		/// var limited = duration.Fx.Time.ToTimeString(numberOfElements: 2);
		/// // "1 hour, 1 minute"
		/// 
		/// // Negative duration
		/// var negative = TimeSpan.FromMinutes(-30).Fx.Time.ToTimeString();
		/// // "- 30 minutes"
		/// </code>
		/// </example>
		public string ToTimeString(int numberOfElements = 5, bool onlyLetters = false)
		{
			var res = "";
			TimeSpan ts;
			if (me.Value.Ticks < 0)
			{
				res += "- ";
				ts = me.Value.Negate();
			}
			else
				ts = me.Value;

			var count = 0;
			if (count >= numberOfElements) return res.Trim(',', ' ');
			if (ts.Days > 0)
			{
				res +=
					$"{ts.Days} {(onlyLetters ? "d" : ts.Days > 1 ? Strings.days : Strings.day)}{(onlyLetters ? "" : ",")} ";
				count++;
			}

			if (count >= numberOfElements) return res.Trim(',', ' ');
			if (ts.Hours > 0)
			{
				res +=
					$"{ts.Hours} {(onlyLetters ? "h" : ts.Hours > 1 ? Strings.hours : Strings.hour)}{(onlyLetters ? "" : ",")} ";
				count++;
			}

			if (count >= numberOfElements) return res.Trim(',', ' ');
			if (ts.Minutes > 0)
			{
				res +=
					$"{ts.Minutes} {(onlyLetters ? "m" : ts.Minutes > 1 ? Strings.minutes : Strings.minute)}{(onlyLetters ? "" : ",")} ";
				count++;
			}

			if (count >= numberOfElements) return res.Trim(',', ' ');
			if (ts.Seconds > 0)
			{
				res +=
					$"{ts.Seconds} {(onlyLetters ? "s" : ts.Seconds > 1 ? Strings.seconds : Strings.second)}{(onlyLetters ? "" : ",")} ";
				count++;
			}

			if (count >= numberOfElements) return res.Trim(',', ' ');
			if (ts.Milliseconds > 0)
				res +=
					$"{ts.Milliseconds} {(onlyLetters ? "ms" : ts.Milliseconds > 1 ? Strings.milliseconds : Strings.millisecond)}{(onlyLetters ? "" : ",")} ";
			if (string.IsNullOrWhiteSpace(res)) res = "0";
			return res.Trim(',', ' ');
		}
	}
}

/// <summary>
/// Container for time-related extension methods accessed through the Fx.Time property.
/// </summary>
/// <typeparam name="T">The type of value being wrapped for time operations.</typeparam>
/// <param name="me">The value being wrapped.</param>
/// <remarks>
/// This class extends <see cref="Extensions{T}"/> to provide specialized time operations.
/// It's accessed through the <c>.Fx.Time</c> property chain.
/// </remarks>
public class TimeExtensions<T>(T me) : Extensions<T>(me);
