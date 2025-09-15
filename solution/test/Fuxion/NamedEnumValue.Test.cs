﻿using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Test;

public class NamedEnumValueTest : BaseTest<NamedEnumValueTest>
{
	public NamedEnumValueTest(ITestOutputHelper output) : base(output) { }
	[Fact(DisplayName = "NamedEnumValue - Equality and comparisons")]
	public void First()
	{
		var nev = new NamedEnumValue(TestEnum.One);
		Assert.False(nev.Equals(null!));
		Assert.False(nev == null!);
		Assert.False(null! == nev);
		Assert.True(nev != null!);
		Assert.True(null! != nev);
		Assert.True(nev.Equals(TestEnum.One));
		Assert.True(nev == TestEnum.One);
		Assert.True(TestEnum.One == nev);
		Assert.False(nev != TestEnum.One);
		Assert.False(TestEnum.One != nev);
		Assert.False(nev.Equals(TestEnum.Two));
		Assert.False(nev == TestEnum.Two);
		Assert.False(TestEnum.Two == nev);
		Assert.True(nev != TestEnum.Two);
		Assert.True(TestEnum.Two != nev);
	}
}

public enum TestEnum
{
	One,
	Two
}