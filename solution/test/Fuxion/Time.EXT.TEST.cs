using System;
using Fuxion;
using Fuxion.Xunit;
using Test.Fuxion;
using Xunit;

namespace Test.Fuxion;

public class TimeExtensionsTest(ITestOutputHelper output) : BaseTest<TimeExtensionsTest>(output)
{
	[Fact]
	public void Epoch_Positive()
	{
		var dt = new DateTime(2000, 1, 1);
		var res = dt.Fx.Time.ToEpochSeconds();
		Assert.True(res.IsSuccess);
		Assert.True(res.SuccessOrThrow() > 0);
		PrintVariable(res.SuccessOrThrow());
	}

	[Fact]
	public void Epoch_Negative()
	{
		var dt = new DateTime(1900, 1, 1);
		var res = dt.Fx.Time.ToEpochSeconds();
		Assert.True(res.IsSuccess);
		Assert.True(res.SuccessOrThrow() < 0);
		PrintVariable(res.SuccessOrThrow());
	}

	[Fact]
	public void Epoch_Error()
	{
		var dt = new DateTime(1900, 1, 1);
		var res = dt.Fx.Time.ToEpochSeconds(true);
		Assert.True(res.IsError);
		PrintVariable(res);
	}

	[Fact]
	public void Epoch_Milliseconds()
	{
		var dt = new DateTime(2000, 1, 1);
		var resSeconds = dt.Fx.Time.ToEpochSeconds();
		var resMilliseconds = dt.Fx.Time.ToEpochMilliseconds();
		Assert.Equal(resSeconds.SuccessOrThrow() * 1000, resMilliseconds.SuccessOrThrow());
	}
}