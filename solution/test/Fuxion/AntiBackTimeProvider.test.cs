using System;
using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Test;

public class AntiBackTimeProviderTest : BaseTest<AntiBackTimeProviderTest>
{
	public AntiBackTimeProviderTest(ITestOutputHelper output) : base(output) { }
	[Fact(DisplayName = "AntiBackTimeProvider - BackTimeException")]
	public void AntiBackTimeProvider_BackTimeException()
	{
		var mock = new MockTimeProvider();
		var abtp = new AntiBackTimeProvider(new MemoryStoredTimeProvider().Tap(s => s.SaveUtcTime(DateTime.UtcNow))) {
			TimeProvider = mock,
			Logger = Logger
		};
		mock.SetOffset(TimeSpan.FromDays(-1));
		Assert.Throws<BackTimeException>(() => abtp.UtcNow());
	}
	[Fact(DisplayName = "AntiBackTimeProvider - CheckConsistency")]
	public void AntiBackTimeProvider_CheckConsistency()
		=> new AntiBackTimeProvider(new MockStorageTimeProvider().Tap(s => s.SaveUtcTime(DateTime.UtcNow))).CheckConsistency(Output);
}

public class MockStorageTimeProvider : StoredTimeProvider
{
	DateTime value;
	public override DateTime GetUtcTime() => value;
	public override void SaveUtcTime(DateTime time) => value = time.ToUniversalTime();
}