using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Http;
using Fuxion.Xunit;
using Test.Responses.Shared.Fixtures;
using Xunit;

namespace Test.AspNet;

// The AsResponseAsync/FuxionHttpClient contract shared with ASP.NET Core lives in
// Test.Responses.Shared.ClientTests. What stays here is specific to the deferred
// ToHttpActionResult extension (Web API 2 only) and a second, redundant-on-purpose proof that the envelope
// Accept FuxionHttpClient adds is exactly what makes the server answer with the envelope.
public class ClientAgainstAspNetTest(ITestOutputHelper output) : BaseTest<ClientAgainstAspNetTest>(output)
{
	[Fact(DisplayName = "ToHttpActionResult is deferred: it honours the scope options and the Accept header")]
	public async Task ToHttpActionResult_IsDeferred()
	{
		var cli = AspNetHost.Create();
		var plain = await cli.GetAsync("result/payload", TestContext.Current.CancellationToken);
		Assert.Equal("application/json", plain.Content.Headers.ContentType?.MediaType);

		var request = new HttpRequestMessage(HttpMethod.Get, "result/payload");
		request.Headers.TryAddWithoutValidation("Accept", "application/vnd.fuxion.response+json");
		var envelope = await cli.SendAsync(request, TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ResponseJson, envelope.Content.Headers.ContentType?.MediaType);

		var error = await cli.GetAsync("result/error", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);

		var explicitOptions = await cli.GetAsync("result/explicit", TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ResponseJson, explicitOptions.Content.Headers.ContentType?.MediaType);
	}

	// FuxionHttpClient.SendAsync only returns the union, not the raw HttpResponseMessage, so the media
	// type the server answered with is proved separately: the exact Accept the client just added (read
	// back from the sent request above) is replayed through the plain HttpClient.
	[Fact(DisplayName = "The envelope Accept added by FuxionHttpClient makes Web API 2 answer with the envelope")]
	public async Task FuxionHttpClient_Accept_ServerAnswersWithEnvelope()
	{
		var options = new FuxionHttpClientOptions { PreferEnvelope = true };
		var client = new FuxionHttpClient(AspNetHost.Create(), options);

		var sent = new HttpRequestMessage(HttpMethod.Get, "response/payload");
		await client.SendAsync<TestPayload>(sent, TestContext.Current.CancellationToken);

		var raw = new HttpRequestMessage(HttpMethod.Get, "response/payload");
		foreach (var accept in sent.Headers.Accept)
			raw.Headers.Accept.Add(accept);

		var response = await client.HttpClient.SendAsync(raw, TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ResponseJson, response.Content.Headers.ContentType?.MediaType);
	}
}
