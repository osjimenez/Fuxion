using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Http;
using Fuxion.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Test.Http;

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
	record TwoWords(string FirstName);
	record BusinessError(string Code);

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
		await client.GetAsync<Person>("people/1", TestContext.Current.CancellationToken);
		Assert.Contains(handler.Last!.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ResponseJson);

		var request = new HttpRequestMessage(HttpMethod.Get, "people/1");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
		await client.SendAsync<Person>(request, TestContext.Current.CancellationToken);
		Assert.Single(handler.Last!.Headers.Accept);
		Assert.Equal("text/plain", handler.Last.Headers.Accept.Single().MediaType);
	}

	[Fact(DisplayName = "GetAsync reads a plain payload through AsResponseAsync")]
	public async Task GetAsync_ReadsPayload()
	{
		var (client, _) = Create();
		var response = await client.GetAsync<Person>("people/1", TestContext.Current.CancellationToken);
		IsTrue(response.TryGetValue(out Person? p));
		Assert.Equal("test", p!.Name);
	}

	[Fact(DisplayName = "The JSON options are applied to every read, and a naming announced by the response overrides them for that body")]
	public async Task JsonOptions_AreApplied()
	{
		// The caller reads with snake_case, the server answers a camelCase envelope that says so in its media type.
		var (client, _) = Create(
			new FuxionHttpClientOptions { JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }, PreferEnvelope = true },
			_ =>
			{
				var message = Json("""{"isSuccess":true,"payload":{"firstName":"Ada"}}""");
				message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"{ResponseMediaTypes.ResponseJson}; naming={ResponseNaming.Camel}");
				return message;
			});

		var response = await client.GetAsync<TwoWords>("people/1", TestContext.Current.CancellationToken);

		IsTrue(response.TryGetValue(out TwoWords? payload));
		Assert.Equal("Ada", payload!.FirstName);
	}

	/// <summary>A stream that counts the bytes pulled through it, to observe buffering.</summary>
	sealed class CountingStream(byte[] data) : Stream
	{
		readonly MemoryStream inner = new(data, writable: false);
		public long BytesRead { get; private set; }
		public override int Read(byte[] buffer, int offset, int count) { var n = inner.Read(buffer, offset, count); BytesRead += n; return n; }
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	[Fact(DisplayName = "A binary request is sent with ResponseHeadersRead so the body is not buffered by HttpClient")]
	public async Task Binary_UsesResponseHeadersRead()
	{
		var body = new CountingStream(new byte[64 * 1024]);
		var (client, _) = Create(respond: _ =>
		{
			var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
			message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
			return message;
		});

		var response = await client.GetAsync<Stream>("files/big", TestContext.Current.CancellationToken);

		IsTrue(response.TryGetValue(out Stream? stream));
		Assert.Equal(0, body.BytesRead); // HttpClient did not pre-read the body
		using var memory = new MemoryStream();
		await stream!.CopyToAsync(memory, TestContext.Current.CancellationToken);
		Assert.Equal(64 * 1024, memory.Length);
	}

	[Fact(DisplayName = "GetAsync<TSuccess, TError> sends the Accept and recovers the typed error from a problem+json errorPayload")]
	public async Task GetAsync_TypedError_RecoversErrorPayload()
	{
		var (client, handler) = Create(new FuxionHttpClientOptions { PreferEnvelope = true }, _ =>
		{
			var message = new HttpResponseMessage(HttpStatusCode.Conflict)
			{
				Content = new StringContent("""{"title":"Business error","status":409,"errorPayload":{"code":"stock"}}""", Encoding.UTF8)
			};
			message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ResponseMediaTypes.ProblemJson);
			return message;
		});

		var response = await client.GetAsync<Person, BusinessError>("people/1", TestContext.Current.CancellationToken);

		Assert.Contains(handler.Last!.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ResponseJson);
		IsTrue(response.TryGetValue(out BusinessError? error));
		Assert.Equal("stock", error!.Code);
	}

	[Fact(DisplayName = "GetAsync<Stream, TError> also uses ResponseHeadersRead, so the body is not buffered by HttpClient")]
	public async Task Binary_TypedError_UsesResponseHeadersRead()
	{
		var body = new CountingStream(new byte[64 * 1024]);
		var (client, _) = Create(respond: _ =>
		{
			var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
			message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
			return message;
		});

		var response = await client.GetAsync<Stream, BusinessError>("files/big", TestContext.Current.CancellationToken);

		IsTrue(response.TryGetValue(out Stream? stream));
		Assert.Equal(0, body.BytesRead);
		using var memory = new MemoryStream();
		await stream!.CopyToAsync(memory, TestContext.Current.CancellationToken);
		Assert.Equal(64 * 1024, memory.Length);
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
