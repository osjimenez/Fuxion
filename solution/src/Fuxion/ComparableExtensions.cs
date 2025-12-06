using System;

namespace Fuxion;

public static class ComparableExtensions
{
	extension<TComparable>(TComparable me) where TComparable : IComparable
	{
		public bool IsBetween(TComparable minimum, TComparable maximum) => me.IsBetween(false, minimum, false, maximum);
		public bool IsBetween(bool minimumExclusive, TComparable minimum, TComparable maximum) => me.IsBetween(minimumExclusive, minimum, false, maximum);
		public bool IsBetween(TComparable minimum, bool maximumExclusive, TComparable maximum) => me.IsBetween(false, minimum, maximumExclusive, maximum);
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