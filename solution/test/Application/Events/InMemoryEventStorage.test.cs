using System;
using Fuxion.Xunit;

namespace Fuxion.Application.Test.Events;

public class InMemoryEventStorageTest(ITestOutputHelper output) : BaseTest<InMemoryEventStorageTest>(output)
{
	[Fact(DisplayName = "InMemoryEventStorage - Pending", Skip = "Not implemented yet")]
	public void Pending() => throw new NotImplementedException();
}