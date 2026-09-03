using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Union;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

// The client declares how it wrote the request in its own Content-Type; the server adapts per request.
public class RequestNamingTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<RequestNamingTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	static StringContent Body(string json, string? naming)
	{
		var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
		if (naming is not null)
			content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, naming));
		return content;
	}

	[Theory(DisplayName = "A snake_case request body is bound when its Content-Type declares naming=snake")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Snake_IsBound(string prefix)
	{
		var res = await factory.CreateClient().PostAsync($"{prefix}/naming/echo", Body("""{"first_name":"Ada","age":36}""", ResponseNaming.Snake));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal("Ada", (string?)body["firstName"]);
		Assert.Equal(36, (int?)body["age"]);
	}

	[Theory(DisplayName = "A kebab-case request body is bound when its Content-Type declares naming=kebab")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Kebab_IsBound(string prefix)
	{
		var res = await factory.CreateClient().PostAsync($"{prefix}/naming/echo", Body("""{"first-name":"Grace","age":45}""", ResponseNaming.Kebab));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("Grace", (string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Fact(DisplayName = "Without the parameter a snake_case body is not understood: the parameter is what does it")]
	public async Task Snake_WithoutParameter_IsNotBound()
	{
		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", Body("""{"first_name":"Ada","age":36}""", null));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Null((string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Fact(DisplayName = "A camelCase body with naming=camel is left untouched")]
	public async Task Camel_PassesThrough()
	{
		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", Body("""{"firstName":"Ada","age":36}""", ResponseNaming.Camel));
		Assert.Equal("Ada", (string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Theory(DisplayName = "A malformed body declaring a naming parameter fails like any malformed body: 400, not 500")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Malformed_IsBadRequest(string prefix)
	{
		var res = await factory.CreateClient().PostAsync($"{prefix}/naming/echo", Body("""{"first_name":"Ada",""", ResponseNaming.Snake));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact(DisplayName = "A non-JSON body declaring a naming parameter is never touched")]
	public async Task NonJsonBody_IsNeverTouched()
	{
		var content = new StringContent("first_name=Ada", System.Text.Encoding.UTF8, "text/plain");
		content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, ResponseNaming.Snake));

		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", content);

		// The endpoint expects JSON: reaching the framework untouched means it answers 415, not a
		// transcoder failure surfaced as something else.
		Assert.Equal(HttpStatusCode.UnsupportedMediaType, res.StatusCode);
	}
}
