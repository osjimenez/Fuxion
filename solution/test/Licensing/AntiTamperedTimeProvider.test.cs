using System;
using System.Linq;
using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Licensing.Test;

public class AntiTamperedTimeProviderTest(ITestOutputHelper output) : BaseTest<AntiTamperedTimeProviderTest>(output)
{
	string[] WebServersAddresses { get; } = {
		"http://www.google.com", "http://www.google.es",
		//"http://www.ooooooo-youtube.com",
		"http://www.microsoft.com", "http://www.yahoo.com", "http://www.amazon.com", "http://www.facebook.com", "http://www.twitter.com"
	};
	[Fact(DisplayName = "AntiTamperedTimeProvider - CheckConsistency")]
	public void AntiTamperedTimeProvider_CheckConsistency()
	{
		var atp = new AverageTimeProvider {
			Logger = Logger, MaxFailsPerTry = 1, RandomizedProvidersPerTry = WebServersAddresses.Length
		};
		foreach (var pro in WebServersAddresses.Select(address => new InternetTimeProvider {
				ServerAddress = address, ServerType = InternetTimeServerType.Web, Timeout = TimeSpan.FromSeconds(5)
			}))
			atp.AddProvider(pro);
		new AntiTamperedTimeProvider(atp, new(new MemoryStoredTimeProvider().Transform(s => {
			s.SaveUtcTime(DateTime.UtcNow);
			return s;
		})) {
			Logger = Logger
		}).CheckConsistency(Output);
	}
}