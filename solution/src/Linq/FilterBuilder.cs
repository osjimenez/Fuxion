using System;

namespace Fuxion.Linq;

public static class FilterBuilder
{
	public static FilterEntityBuilder<TEntity> For<TEntity>(string singularKey, string pluralKey)
	{
		if(singularKey.Contains(" "))
			throw new ArgumentException(@"Singular name cannot contain spaces.", nameof(singularKey));
		if(pluralKey.Contains(" "))
			throw new ArgumentException(@"Plural name cannot contain spaces.", nameof(pluralKey));

		return new(singularKey, pluralKey);
	}
}