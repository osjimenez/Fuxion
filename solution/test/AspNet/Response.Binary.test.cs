using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Xunit;

namespace Test.AspNet;

// Everything shared with ASP.NET Core lives in Test.Responses.Shared.BinaryWireTests.
// This 416 case has no counterpart there yet (AspNetCore's own binary suite does not exercise it either).
public class BinaryWireTest(ITestOutputHelper output) : BaseTest<BinaryWireTest>(output)
{
	static HttpRequestMessage Get(string url, string? accept = null, string? range = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
		return request;
	}

	[Fact(DisplayName = "An out-of-bounds range is a 416")]
	public async Task Range_OutOfBounds_Is416()
	{
		var res = await AspNetHost.Create().SendAsync(Get("binary/file", range: "bytes=999-1000"));
		Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, res.StatusCode);
	}
}
