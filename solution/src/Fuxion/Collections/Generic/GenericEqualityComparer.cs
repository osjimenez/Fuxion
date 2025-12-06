using System;
using System.Collections.Generic;

namespace Fuxion.Collections.Generic;

public class GenericEqualityComparer<T>(Func<T?, T?, bool> equalsFunction, Func<T, int> getHashCodeFunction)
	: EqualityComparer<T>
{
	public override bool Equals(T? x, T? y) => equalsFunction.Invoke(x, y);
	public override int GetHashCode(T obj) => getHashCodeFunction.Invoke(obj);
}