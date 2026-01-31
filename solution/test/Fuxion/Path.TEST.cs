using Fuxion.Xunit;
using Fuxion;
using System.Runtime.InteropServices;
using Xunit;
using System;

namespace Test.Fuxion;

public class StringTest(ITestOutputHelper output) : BaseTest<StringTest>(output)
{
	[Fact]
	public void IsSubPathOf()
	{
		var root = @"c:\foo\bar";
		var relative = @"c:/fOo/Bar\file.txt";
#if NET5_0_OR_GREATER
		PrintVariable(OperatingSystem.IsWindows());
#else
		PrintVariable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
#endif
		Assert.True(relative.Fx.Path.IsSubPathOf(root));
	}

	[Fact]
	public void Operator()
	{
		var t1 = @"c:\one".Fx.Path / "two/three".Fx.Path / "four".Fx.Path;
		PrintVariable(t1);
		Assert.Equal(@"c:\one\two\three\four", t1);

		var t2 = @"c:\one".Fx.Path / "two/three".Fx.Path / "four/".Fx.Path;
		PrintVariable(t2);
		Assert.Equal(@"c:\one\two\three\four", t2);

		var t3 = @"\\server.domain.local\one".Fx.Path / "two/three".Fx.Path / "four".Fx.Path;
		PrintVariable(t3);
		Assert.Equal(@"\\server.domain.local\one\two\three\four", t3);
	}
}
