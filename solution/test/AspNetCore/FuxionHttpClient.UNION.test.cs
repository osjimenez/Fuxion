using Fuxion.Http;
using Fuxion.Union;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

// Fuxion.Http is the ergonomics layer: it puts the Accept on the wire and applies the JSON options once.
// There is no contract negotiation: every response describes itself.
public class FuxionHttpClientTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: BaseTest<FuxionHttpClientTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	static FuxionHttpClient BuildClient(HttpClient inner, Action<FuxionHttpClientOptions>? configure = null)
	{
		var options = new FuxionHttpClientOptions();
		configure?.Invoke(options);
		return new FuxionHttpClient(inner, options);
	}

	[Fact(DisplayName = "The client reads a plain payload with its default options")]
	public async Task ReadsPlainPayload()
	{
		var response = await BuildClient(factory.CreateClient()).GetAsync<TestPatchPayload>("minimal/undefinable/partial");

		IsTrue(response.TryGetValue(out TestPatchPayload? payload));
		Assert.Equal(123, payload!.Age.Value);
	}

	[Fact(DisplayName = "Without preferences the Accept is plain JSON, indistinguishable from a vanilla client")]
	public void DefaultAccept_IsPlainJson()
		=> Assert.Equal("application/json", new FuxionHttpClientOptions().BuildAccept());

	[Fact(DisplayName = "Preferences add the vendor types first and always keep a JSON fallback")]
	public void Accept_WithPreferences_HasFallback()
	{
		var accept = new FuxionHttpClientOptions { PreferEnvelope = true, PreferNativeErrors = true }.BuildAccept();

		Assert.StartsWith(ResponseMediaTypes.ResponseJson, accept);
		Assert.Contains(ResponseMediaTypes.ErrorJson, accept);
		Assert.EndsWith("application/json;q=0.9", accept);
	}

	[Fact(DisplayName = "Preferring the envelope makes the server answer with it, end to end")]
	public async Task PreferEnvelope_GetsTheEnvelope()
	{
		var client = BuildClient(factory.CreateClient(), o => o.PreferEnvelope = true);

		var request = new HttpRequestMessage(HttpMethod.Get, "minimal/response/payload");
		var response = await client.SendAsync<TestPayload>(request);

		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.Equal("test", payload!.Name);
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ResponseJson);
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.Json && a.Quality == 0.9);
	}

	[Fact(DisplayName = "An Accept already set on the request is respected")]
	public async Task ExplicitAccept_IsRespected()
	{
		var client = BuildClient(factory.CreateClient(), o => o.PreferEnvelope = true);
		var request = new HttpRequestMessage(HttpMethod.Get, "minimal/response/payload");
		request.Headers.TryAddWithoutValidation("Accept", "application/json");

		await client.SendAsync<TestPayload>(request);

		Assert.Equal("application/json", request.Headers.Accept.ToString());
	}

	[Fact(DisplayName = "The JSON options are applied to every read")]
	public async Task JsonOptions_AreApplied()
	{
		// A caller policy that does not match the server can still read the envelope, because the
		// naming parameter of the response overrides it for that body; the payload uses the same policy.
		var client = BuildClient(factory.CreateClient(), o =>
		{
			o.JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
			o.PreferEnvelope = true;
		});

		var response = await client.GetAsync<TestPayload>("minimal/response/payload");

		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.Equal("test", payload!.Name);
	}

	[Fact(DisplayName = "The DI registration wires the options into the typed client")]
	public void Registration_WiresOptions()
	{
		var services = new ServiceCollection();
		services.AddFuxionHttpClient(c => c.BaseAddress = new Uri("http://localhost/"), o => o.PreferNativeErrors = true);
		var provider = services.BuildServiceProvider();

		var client = provider.GetRequiredService<FuxionHttpClient>();

		IsTrue(client.Options.PreferNativeErrors);
		Assert.Equal(new Uri("http://localhost/"), client.HttpClient.BaseAddress);
	}
}
