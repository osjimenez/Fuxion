using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.AspNet;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.AspNet.Union;

public class RequestNamingTest(ITestOutputHelper output) : BaseTest<RequestNamingTest>(output)
{
	static StringContent Body(string json, string? naming, string mediaType = "application/json")
	{
		var content = new StringContent(json, System.Text.Encoding.UTF8, mediaType);
		if (naming is not null)
			content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, naming));
		return content;
	}

	/// <summary>Innermost handler that just captures the (possibly replaced) request and answers 200.</summary>
	sealed class CapturingHandler : DelegatingHandler
	{
		public HttpRequestMessage? Captured { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Captured = request;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
		}
	}

	[Theory(DisplayName = "A snake_case or kebab-case body is bound when its Content-Type declares the naming")]
	[InlineData("""{"first_name":"Ada","age":36}""", "snake")]
	[InlineData("""{"first-name":"Ada","age":36}""", "kebab")]
	public async Task SeparatedNaming_IsBound(string json, string naming)
	{
		var res = await AspNetHost.Create().PostAsync("naming/echo", Body(json, naming));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("Ada", (string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Fact(DisplayName = "Without the parameter a snake_case body is not understood")]
	public async Task Snake_WithoutParameter_IsNotBound()
	{
		var res = await AspNetHost.Create().PostAsync("naming/echo", Body("""{"first_name":"Ada","age":36}""", null));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Null((string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Fact(DisplayName = "A non-JSON body declaring a naming parameter is never touched")]
	public async Task NonJson_IsNeverTouched()
	{
		var res = await AspNetHost.Create().PostAsync("naming/echo", Body("first_name=Ada", "snake", "text/plain"));
		Assert.Equal(HttpStatusCode.UnsupportedMediaType, res.StatusCode);
	}

	// The classic Web API 2 outcome for a malformed JSON body: model binding fails and the action never
	// runs, rather than an unhandled 500 (the deserialization exception is now reported through
	// IFormatterLogger, which feeds ModelState, exactly like BaseJsonMediaTypeFormatter always did).
	[Fact(DisplayName = "A malformed JSON body fails model binding like the framework always did")]
	public async Task MalformedBody_FailsModelBinding()
	{
		var res = await AspNetHost.Create().PostAsync("naming/validate", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact(DisplayName = "Transcoding preserves other content headers and recomputes Content-Length for the shorter, transcoded body")]
	public async Task Transcoding_PreservesHeadersAndRecomputesContentLength()
	{
		var inner = new CapturingHandler();
		var handler = new RequestNamingHandler { InnerHandler = inner };
		using var invoker = new HttpMessageInvoker(handler);

		var content = Body("""{"first_name":"Ada","age":36}""", "snake"); // "first_name" (10) -> "firstName" (9): strictly shorter
		content.Headers.ContentLanguage.Add("es");
		var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/naming/echo") { Content = content };

		await invoker.SendAsync(request, CancellationToken.None);

		Assert.NotNull(inner.Captured?.Content);
		var transcodedBytes = await inner.Captured!.Content!.ReadAsByteArrayAsync();
		Assert.Equal(transcodedBytes.Length, inner.Captured.Content.Headers.ContentLength);
		Assert.Contains("es", inner.Captured.Content.Headers.ContentLanguage);
	}
}
