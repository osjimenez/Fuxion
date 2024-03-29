﻿using Fuxion.Identity.Helpers;

namespace Fuxion.Identity;

public class DiscriminatorEqualityComparer : IEqualityComparer<IDiscriminator>
{
	public bool Equals(IDiscriminator? x, IDiscriminator? y) => AreEquals(x, y);
	public int GetHashCode(IDiscriminator obj) => obj != null ? obj.Id?.GetHashCode() ?? 0 : 0;
	static bool AreEquals(object? obj1, object? obj2)
	{
		// If both are NULL, return TRUE
		if (Equals(obj1, null) && Equals(obj2, null)) return true;
		// If some of them is null, return FALSE
		if (Equals(obj1, null) || Equals(obj2, null)) return false;
		// If any of them are of other type, return FALSE
		if (!(obj1 is IDiscriminator) || !(obj2 is IDiscriminator)) return false;
		var dis1 = (IDiscriminator)obj1;
		var dis2 = (IDiscriminator)obj2;
		// Use 'Equals' to compare the ids
		return Comparer.AreEquals(dis1.Id, dis2.Id) && Comparer.AreEquals(dis1.TypeKey, dis2.TypeKey);
	}
}