using Fuxion.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fuxion;

public static class TimeExtensions
{
	private static readonly DateTime EpochStartTime = new(1970, 1, 1);

	extension(FuxionExtensions<DateTime> me)
	{
		/// <summary>
		///    Provides time-related extension operations for this <see cref="DateTime" /> value.
		/// </summary>
		public TimeExtensions<DateTime> Time
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension(FuxionExtensions<TimeSpan> me)
	{
		/// <summary>
		///    Provides time-related extension operations for this <see cref="TimeSpan" /> value.
		/// </summary>
		public TimeExtensions<TimeSpan> Time
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension(DateTime me)
	{
		public static DateTime EpochStartTime => EpochStartTime;
	}
	extension(IEnumerable<DateTime> me)
	{
		public DateTime Average() => new((long)me.Average(dt => dt.Ticks));
	}
	extension(IEnumerable<DateTimeOffset> me)
	{
		public DateTime Average() => new((long)me.Average(dto => dto.UtcTicks));
	}
	extension(IEnumerable<TimeSpan> me)
	{
		public TimeSpan Average() => new((long)me.Average(dto => dto.Ticks));
	}

	extension(int me)
	{
		public TimeSpan Ticks => TimeSpan.FromTicks(me);
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		public TimeSpan Hours => TimeSpan.FromHours(me);
		public TimeSpan Days => TimeSpan.FromDays(me);
	}

	extension(uint me)
	{
		public TimeSpan Ticks => TimeSpan.FromTicks(me);
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		public TimeSpan Hours => TimeSpan.FromHours(me);
		public TimeSpan Days => TimeSpan.FromDays(me);
	}

	extension(long me)
	{
		public TimeSpan Ticks => TimeSpan.FromTicks(me);
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		public TimeSpan Hours => TimeSpan.FromHours(me);
		public TimeSpan Days => TimeSpan.FromDays(me);
	}
	extension(ulong me)
	{
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		public TimeSpan Hours => TimeSpan.FromHours(me);
		public TimeSpan Days => TimeSpan.FromDays(me);
	}
	extension(double me)
	{
		public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(me);
		public TimeSpan Seconds => TimeSpan.FromSeconds(me);
		public TimeSpan Minutes => TimeSpan.FromMinutes(me);
		public TimeSpan Hours => TimeSpan.FromHours(me);
		public TimeSpan Days => TimeSpan.FromDays(me);
	}
	extension(TimeExtensions<long> me)
	{
		public DateTime ToEpochDateTimeFromSeconds() => EpochStartTime.AddSeconds(me.Value);
		public DateTime ToEpochDateTimeFromMilliseconds() => EpochStartTime.AddMilliseconds(me.Value);
	}
	extension(TimeExtensions<ulong> me)
	{
		public DateTime ToEpochDateTimeFromSeconds() => EpochStartTime.AddSeconds(me.Value);
		public DateTime ToEpochDateTimeFromMilliseconds() => EpochStartTime.AddMilliseconds(me.Value);
	}
	extension(TimeExtensions<double> me)
	{
		public DateTime ToEpochDateTimeFromSeconds() => EpochStartTime.AddSeconds(me.Value);
		public DateTime ToEpochDateTimeFromMilliseconds() => EpochStartTime.AddMilliseconds(me.Value);
	}
	extension(TimeExtensions<DateTime> me)
	{
		public Response<long> ToEpochSeconds(bool errorIfPrior1970 = false)
			=> me.ToEpochMilliseconds(errorIfPrior1970).Match(
				r => Response.Get.SuccessPayload(r.Payload / 1000),
				r => r);

		public Response<long> ToEpochMilliseconds(bool errorIfPrior1970 = false)
			=> (me.Value - EpochStartTime).TotalMilliseconds switch
			{
				< 0 when errorIfPrior1970 => Response.Get
					.InvalidData("DateTime cannot be prior 1/1/1970 to be converted to EPOCH date").AsPayload<long>(),
				var r => (long)r
			};
	}

	extension(TimeExtensions<TimeSpan> me)
	{
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

public class TimeExtensions<T>(T me) : Extensions<T>(me);
