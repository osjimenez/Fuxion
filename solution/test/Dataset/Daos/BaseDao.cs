using System;

namespace Test.Dataset.Daos;

public abstract class BaseDao
{
	public required DateTime UpdatedAtUtc { get; set; }
}
