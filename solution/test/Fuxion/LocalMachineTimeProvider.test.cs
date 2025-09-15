﻿using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Test;

public class LocalMachineTimeProviderTest : BaseTest<LocalMachineTimeProviderTest>
{
	public LocalMachineTimeProviderTest(ITestOutputHelper output) : base(output) { }
	[Fact(DisplayName = "LocalMachineTimeProvider - CheckConsistency")]
	public void LocalMachineTimeProvider_CheckConsistency() => new LocalMachineTimeProvider().CheckConsistency(Output);
}