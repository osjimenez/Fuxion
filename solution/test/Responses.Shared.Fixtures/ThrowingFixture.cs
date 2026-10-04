using System;

namespace Test.Responses.Shared.Fixtures;

/// <summary>Throws from two frames deep, so an exception that crosses the wire carries a real stack trace.</summary>
public static class ThrowingFixture
{
	public static void Throw() => Level1();
	static void Level1() => Level2();
	static void Level2() => throw new NotImplementedException("Not implemented");
}
