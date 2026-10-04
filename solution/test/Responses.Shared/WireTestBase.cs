using Fuxion.Xunit;
using Xunit;

namespace Test.Responses.Shared;

/// <summary>What every shared wire family needs: the host it runs against, logged once per run.</summary>
public abstract class WireTestBase<TSelf> : BaseTest<TSelf>
	where TSelf : WireTestBase<TSelf>
{
	/// <summary>The host this test matrix runs against.</summary>
	protected IWireHost Host { get; }

	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected WireTestBase(ITestOutputHelper output, IWireHost host) : base(output)
	{
		Host = host;
		Output.WriteLine($"Host: {host.GetType().Name}");
	}
}
