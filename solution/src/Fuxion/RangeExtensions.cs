using System;

namespace Fuxion;

public static class RangeExtensions
{
	extension(Range me)
	{
		public CustomIntEnumerator GetEnumerator() => new(me);
	}
	extension(int number)
	{
		public CustomIntEnumerator GetEnumerator()
			=> number <= 0 ? throw new ArgumentException($"{nameof(number)} must be a positive value greater than 0", nameof(number)) : new(new(0, number));
	}
	public struct CustomIntEnumerator
	{
		private readonly int _end;
		public CustomIntEnumerator(Range range)
		{
			if (range.End.IsFromEnd) throw new NotSupportedException("You must specify an end for the range");
			Current = range.Start.Value - 1;
			_end = range.End.Value;
		}
		public int Current { get; private set; }
		public bool MoveNext()
		{
			Current++;
			return Current <= _end;
		}
	}
}