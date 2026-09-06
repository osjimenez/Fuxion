namespace Test.Http;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Http;
using Fuxion.Union;
using Fuxion.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class FuxionHttpClientTest(ITestOutputHelper output) : BaseTest<FuxionHttpClientTest>(output)
{
	sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
	{
		public HttpRequestMessage? Last { get; private set; }
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Last = request;
			return Task.FromResult(respond(request));
		}
	}

	static HttpResponseMessage Json(string body, string contentType = "application/json")
		=> new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, contentType) };

	static (FuxionHttpClient Client, RecordingHandler Handler) Create(FuxionHttpClientOptions? options = null, Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
	{
		var handler = new RecordingHandler(respond ?? (_ => Json("""{"name":"test","age":123}""")));
		return (new FuxionHttpClient(new HttpClient(handler) { BaseAddress = new Uri("http://stub/") }, options ?? new FuxionHttpClientOptions()), handler);
	}

	record Person(string Name, int Age);

	[Theory(DisplayName = "BuildAccept expresses the preferences with a JSON fallback")]
	[InlineData(false, false, null, "application/json")]
	[InlineData(true, false, null, "application/vnd.fuxion.response+json, application/json;q=0.9")]
	[InlineData(false, true, null, "application/vnd.fuxion.error+json, application/json;q=0.9")]
	[InlineData(true, true, null, "application/vnd.fuxion.response+json, application/vnd.fuxion.error+json, application/json;q=0.9")]
	[InlineData(true, true, "snake", "application/vnd.fuxion.response+json; naming=snake, application/vnd.fuxion.error+json; naming=snake, application/json;q=0.9")]
	[InlineData(true, false, "snake", "application/vnd.fuxion.response+json; naming=snake, application/json;q=0.9")]
	[InlineData(false, false, "snake", "application/vnd.fuxion.unit+json; naming=snake, application/json;q=0.9")]
	[InlineData(false, false, "kebab", "application/vnd.fuxion.unit+json; naming=kebab, application/json;q=0.9")]
	public void BuildAccept(bool envelope, bool nativeErrors, string? naming, string expected)
		=> Assert.Equal(expected, new FuxionHttpClientOptions { PreferEnvelope = envelope, PreferNativeErrors = nativeErrors, PreferNaming = naming }.BuildAccept());

	[Fact(DisplayName = "The Accept header is added only when the request has none")]
	public async Task Accept_OnlyWhenAbsent()
	{
		var (client, handler) = Create(new FuxionHttpClientOptions { PreferEnvelope = true });
		await client.GetAsync<Person>("people/1");
		Assert.Contains(handler.Last!.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ResponseJson);

		var request = new HttpRequestMessage(HttpMethod.Get, "people/1");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
		await client.SendAsync<Person>(request);
		Assert.Single(handler.Last!.Headers.Accept);
		Assert.Equal("text/plain", handler.Last.Headers.Accept.Single().MediaType);
	}

	[Fact(DisplayName = "GetAsync reads a plain payload through AsResponseAsync")]
	public async Task GetAsync_ReadsPayload()
	{
		var (client, _) = Create();
		var response = await client.GetAsync<Person>("people/1");
		IsTrue(response.TryGetValue(out Person? p));
		Assert.Equal("test", p!.Name);
	}

	[Fact(DisplayName = "AddFuxionHttpClient registers a configured client")]
	public void AddFuxionHttpClient_Registers()
	{
		var services = new ServiceCollection();
		services.AddFuxionHttpClient(http => http.BaseAddress = new Uri("http://stub/"), o => o.PreferEnvelope = true);
		using var provider = services.BuildServiceProvider();
		var client = provider.GetRequiredService<FuxionHttpClient>();
		Assert.Equal(new Uri("http://stub/"), client.HttpClient.BaseAddress);
		IsTrue(client.Options.PreferEnvelope);
	}
}
