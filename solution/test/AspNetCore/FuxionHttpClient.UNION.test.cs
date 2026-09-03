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

	/// <summary>A handler whose body counts the bytes pulled through it, to observe buffering.</summary>
	sealed class CountingHandler : HttpMessageHandler
	{
		public CountingStream? LastBody { get; private set; }
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			LastBody = new CountingStream(new byte[64 * 1024]);
			var message = new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StreamContent(LastBody) };
			message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
			return Task.FromResult(message);
		}
	}

	sealed class CountingStream(byte[] data) : System.IO.Stream
	{
		readonly System.IO.MemoryStream inner = new(data, writable: false);
		public long BytesRead { get; private set; }
		public override int Read(byte[] buffer, int offset, int count) { var n = inner.Read(buffer, offset, count); BytesRead += n; return n; }
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	[Fact(DisplayName = "A binary request is sent with ResponseHeadersRead so the body is not buffered by HttpClient")]
	public async Task Binary_UsesResponseHeadersRead()
	{
		var handler = new CountingHandler();
		var client = BuildClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

		var response = await client.GetAsync<System.IO.Stream>("files/big");

		IsTrue(response.TryGetValue(out System.IO.Stream? stream));
		Assert.Equal(0, handler.LastBody!.BytesRead); // HttpClient did not pre-read the body
		using var memory = new System.IO.MemoryStream();
		await stream!.CopyToAsync(memory);
		Assert.Equal(64 * 1024, memory.Length);
	}

	[Fact(DisplayName = "A JSON request keeps the default completion (the body is small and buffered as before)")]
	public async Task Json_KeepsContentRead()
	{
		var client = BuildClient(factory.CreateClient());
		var response = await client.GetAsync<TestPayload>("minimal/response/payload");
		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.Equal("test", payload!.Name);
	}
}
