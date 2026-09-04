using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.AspNet;
using Fuxion.Xunit;
using Test.AspNet.Service;
using Xunit;

namespace Test.AspNet.Union;

// Web API 2 serializes with Newtonsoft by default; UseResponses installs a System.Text.Json formatter so
// requests and non-union responses follow the same JSON rules as the union wire (incl. Undefinable omission).
public class UndefinableTest(ITestOutputHelper output) : BaseTest<UndefinableTest>(output)
{
	[Fact(DisplayName = "An undefined member is omitted from a plain (non-union) response")]
	public async Task UndefinedMember_IsOmitted()
	{
		var res = await AspNetHost.Create().GetAsync("undefinable/partial");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!.AsObject();
		Assert.False(json.ContainsKey("name"));
		Assert.Equal(123, (int?)json["age"]);
	}

	[Fact(DisplayName = "An absent member is bound as undefined")]
	public async Task AbsentMember_IsBoundAsUndefined()
	{
		var res = await AspNetHost.Create().PostAsync("undefinable/echo", new StringContent("""{ "age": 7 }""", System.Text.Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!.AsObject();
		Assert.False((bool)json["nameDefined"]!);
		Assert.True((bool)json["ageDefined"]!);
	}

	// The classic Newtonsoft formatter treats an empty body with no Content-Length header (chunked or
	// custom content) as a default value, not as malformed JSON. The formatter must match that, without
	// starting to swallow genuinely malformed bodies.
	[Fact(DisplayName = "An empty body without Content-Length binds to the default value")]
	public async Task EmptyBodyWithoutContentLength_BindsToDefault()
	{
		var formatter = new SystemTextJsonMediaTypeFormatter(new JsonSerializerOptions(JsonSerializerDefaults.Web));
		using var content = new LengthlessContent();
		Assert.Null(content.Headers.ContentLength);
		var result = await formatter.ReadFromStreamAsync(typeof(TestPatchPayload), new MemoryStream(), content, null!);
		Assert.Null(result);
	}

	[Fact(DisplayName = "A malformed non-empty body without Content-Length still throws without a formatter logger")]
	public async Task MalformedBodyWithoutContentLength_StillThrows()
	{
		var formatter = new SystemTextJsonMediaTypeFormatter(new JsonSerializerOptions(JsonSerializerDefaults.Web));
		using var content = new LengthlessContent();
		using var body = new MemoryStream(Encoding.UTF8.GetBytes("{ not json"));
		await Assert.ThrowsAnyAsync<JsonException>(() => formatter.ReadFromStreamAsync(typeof(TestPatchPayload), body, content, null!));
	}

	/// <summary>HttpContent whose length cannot be computed up front, e.g. chunked transfer encoding.</summary>
	sealed class LengthlessContent : HttpContent
	{
		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return false;
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => Task.CompletedTask;
	}

	// The union wire itself always serializes with System.Text.Json, whichever formatter handles the
	// rest of the app - so opting out of the formatter replacement must not affect it.
	[Fact(DisplayName = "Keeping the Newtonsoft formatter is possible")]
	public async Task ReplaceJsonFormatter_CanBeOptedOut()
	{
		var client = AspNetHost.Create(replaceJsonFormatter: false);

		// Newtonsoft still answers non-union requests; its body shape is not asserted on here, only that
		// the endpoint still works.
		var partial = await client.GetAsync("undefinable/partial");
		Assert.Equal(HttpStatusCode.OK, partial.StatusCode);

		// The union wire is unaffected: still plain camelCase application/json.
		var payload = await client.GetAsync("response/payload");
		Assert.Equal(HttpStatusCode.OK, payload.StatusCode);
		Assert.Equal("application/json", payload.Content.Headers.ContentType?.MediaType);
		var body = JsonNode.Parse(await payload.Content.ReadAsStringAsync())!;
		Assert.Equal("test", (string?)body["name"]);
	}
}
