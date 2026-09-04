using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Http;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Fuxion.Xunit;
using Test.AspNet.Service;
using Xunit;

namespace Test.AspNet.Union;

// The Fuxion client reads a Web API 2 server exactly like an ASP.NET Core one: same wire, same code.
public class ClientAgainstAspNetTest(ITestOutputHelper output) : BaseTest<ClientAgainstAspNetTest>(output)
{
	[Fact(DisplayName = "ToHttpActionResult is deferred: it honours the scope options and the Accept header")]
	public async Task ToHttpActionResult_IsDeferred()
	{
		var cli = AspNetHost.Create();
		var plain = await cli.GetAsync("result/payload");
		Assert.Equal("application/json", plain.Content.Headers.ContentType?.MediaType);

		var request = new HttpRequestMessage(HttpMethod.Get, "result/payload");
		request.Headers.TryAddWithoutValidation("Accept", "application/vnd.fuxion.response+json");
		var envelope = await cli.SendAsync(request);
		Assert.Equal(ResponseMediaTypes.ResponseJson, envelope.Content.Headers.ContentType?.MediaType);

		var error = await cli.GetAsync("result/error");
		Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);

		var explicitOptions = await cli.GetAsync("result/explicit");
		Assert.Equal(ResponseMediaTypes.ResponseJson, explicitOptions.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "AsResponseAsync reads payload, error and typed error from a Web API 2 server")]
	public async Task AsResponseAsync_ReadsEverything()
	{
		var cli = AspNetHost.Create();

		var payload = await cli.GetAsync("response/payload").AsResponseAsync<TestPayload>();
		IsTrue(payload.TryGetValue(out TestPayload? p));
		Assert.Equal(TestPayload.Default, p);

		var error = await cli.GetAsync("response/error-type").AsResponseAsync<TestPayload>();
		IsTrue(error.TryGetValue(out Error e));
		Assert.Equal(HttpStatusCode.NotImplemented, e.Type);

		var typed = await cli.GetAsync("response/typed-error").AsResponseAsync<string, TestBusinessError>();
		IsTrue(typed.TryGetValue(out TestBusinessError? business));
		Assert.Equal(TestBusinessError.Default, business);

		IsTrue((await cli.GetAsync("response/unit").AsResponseAsync<Unit>()).TryGetValue(out Unit _));
		IsTrue((await cli.GetAsync("response/none").AsResponseAsync<Unit>()).IsNone);
	}

	[Fact(DisplayName = "The client streams a file from a Web API 2 server")]
	public async Task AsResponseAsync_ReadsFile()
	{
		var response = await AspNetHost.Create().GetAsync("binary/file", HttpCompletionOption.ResponseHeadersRead).AsResponseAsync<FileContent>();
		IsTrue(response.TryGetValue(out FileContent? file));
		Assert.Equal(TestFile.Name, file!.FileName);
		Assert.Equal(TestFile.ContentType, file.ContentType);
		using var memory = new MemoryStream();
		await file.Stream.CopyToAsync(memory);
		Assert.Equal(TestFile.Bytes, memory.ToArray());
	}

	[Fact(DisplayName = "FuxionHttpClient adds the envelope Accept and Web API 2 answers with the envelope")]
	public async Task FuxionHttpClient_AddsEnvelopeAccept()
	{
		var options = new FuxionHttpClientOptions { PreferEnvelope = true };
		var client = new FuxionHttpClient(AspNetHost.Create(), options);

		var request = new HttpRequestMessage(HttpMethod.Get, "response/payload");
		var response = await client.SendAsync<TestPayload>(request);

		// The request is mutated in place by the client before it is sent, so the header survives the send.
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ResponseJson);
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.Json && a.Quality == 0.9);
		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.Equal(TestPayload.Default, payload);
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
		await client.SendAsync<TestPayload>(sent);

		var raw = new HttpRequestMessage(HttpMethod.Get, "response/payload");
		foreach (var accept in sent.Headers.Accept)
			raw.Headers.Accept.Add(accept);

		var response = await client.HttpClient.SendAsync(raw);
		Assert.Equal(ResponseMediaTypes.ResponseJson, response.Content.Headers.ContentType?.MediaType);
	}
}
