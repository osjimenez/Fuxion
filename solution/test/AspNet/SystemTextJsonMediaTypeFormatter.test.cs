using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.Http;
using Fuxion.AspNet;
using Fuxion.Xunit;
using Test.Responses.Shared.Fixtures;
using Xunit;

namespace Test.AspNet;

// Web API 2 serializes with Newtonsoft by default; UseResponses installs a System.Text.Json formatter so
// requests and non-union responses follow the same JSON rules as the union wire (incl. Undefinable omission).
// The omission/binding contract itself lives in Test.Responses.Shared.UndefinableTests.
// Everything below is Web API 2 specific: the formatter used directly, and opting out of the replacement.
public class SystemTextJsonMediaTypeFormatterTest(ITestOutputHelper output) : BaseTest<SystemTextJsonMediaTypeFormatterTest>(output)
{
	// The classic Newtonsoft formatter treats an empty body with no Content-Length header (chunked or
	// custom content) as a default value, not as malformed JSON. The formatter must match that, without
	// starting to swallow genuinely malformed bodies.
	[Fact(DisplayName = "An empty body without Content-Length binds to the default value")]
	public async Task EmptyBodyWithoutContentLength_BindsToDefault()
	{
		var formatter = new SystemTextJsonMediaTypeFormatter(new JsonSerializerOptions(JsonSerializerDefaults.Web));
		using var content = new ChunkedContent([]);
		Assert.Null(content.Headers.ContentLength);
		var result = await formatter.ReadFromStreamAsync(typeof(TestPatchPayload), new MemoryStream(), content, null!);
		Assert.Null(result);
	}

	[Fact(DisplayName = "A malformed non-empty body without Content-Length still throws without a formatter logger")]
	public async Task MalformedBodyWithoutContentLength_StillThrows()
	{
		var formatter = new SystemTextJsonMediaTypeFormatter(new JsonSerializerOptions(JsonSerializerDefaults.Web));
		using var content = new ChunkedContent([]);
		using var body = new MemoryStream(Encoding.UTF8.GetBytes("{ not json"));
		await Assert.ThrowsAnyAsync<JsonException>(() => formatter.ReadFromStreamAsync(typeof(TestPatchPayload), body, content, null!));
	}

	/// <summary>HttpContent whose length cannot be computed up front, e.g. chunked transfer encoding.</summary>
	// The union wire itself always serializes with System.Text.Json, whichever formatter handles the
	// rest of the app - so opting out of the formatter replacement must not affect it.
	[Fact(DisplayName = "Keeping the Newtonsoft formatter is possible")]
	public async Task ReplaceJsonFormatter_CanBeOptedOut()
	{
		var client = AspNetHost.Create(scope: JsonFormatterScope.None);

		// Newtonsoft still answers non-union requests; its body shape is not asserted on here, only that
		// the endpoint still works.
		var partial = await client.GetAsync("undefinable/partial", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, partial.StatusCode);

		// The union wire is unaffected: still plain camelCase application/json.
		var payload = await client.GetAsync("response/payload", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, payload.StatusCode);
		Assert.Equal("application/json", payload.Content.Headers.ContentType?.MediaType);
		var body = JsonNode.Parse(await payload.Content.ReadAsStringAsync())!;
		Assert.Equal("test", (string?)body["name"]);
	}

	[Fact(DisplayName = "Existing plain endpoints keep Newtonsoft and PascalCase after UseResponses")]
	public async Task PlainEndpoint_KeepsNewtonsoft()
	{
		var res = await AspNetHost.Create().GetAsync("plain/poco", TestContext.Current.CancellationToken);
		var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!.AsObject();
		Assert.True(json.ContainsKey("Name"));
		Assert.False(json.ContainsKey("name"));
	}

	[Fact(DisplayName = "The Newtonsoft formatter stays registered by default and is gone with scope All")]
	public void JsonFormatter_Presence()
	{
		var keep = new HttpConfiguration(); keep.UseResponses();
		Assert.NotNull(keep.Formatters.JsonFormatter);
		var all = new HttpConfiguration(); all.UseResponses(scope: JsonFormatterScope.All);
		Assert.Null(all.Formatters.JsonFormatter);
	}

	[Fact(DisplayName = "Scope All moves plain endpoints to System.Text.Json (camelCase)")]
	public async Task ScopeAll_PlainEndpoint_IsCamel()
	{
		var res = await AspNetHost.Create(scope: JsonFormatterScope.All).GetAsync("plain/poco", TestContext.Current.CancellationToken);
		Assert.True(JsonNode.Parse(await res.Content.ReadAsStringAsync())!.AsObject().ContainsKey("name"));
	}

	// Web API 2 builds a route-not-found HttpError itself, before any formatter (Newtonsoft or ours) is ever
	// consulted for a union or plain type: it always writes its own PascalCase keys, whichever formatter is
	// installed. UseResponses() must not change that.
	[Fact(DisplayName = "Framework HttpError bodies keep their PascalCase keys under the default scope")]
	public async Task FrameworkHttpError_KeepsPascalCase()
	{
		var res = await AspNetHost.Create().GetAsync("plain/does-not-exist", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
		var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!.AsObject();
		Assert.True(json.ContainsKey("Message"));
		Assert.False(json.ContainsKey("message"));
	}
}
