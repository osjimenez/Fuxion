using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.AspNet;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.AspNet.Union;

// The echo/snake/kebab/dictionary/400/malformed contract shared with ASP.NET Core lives in
// Test.Responses.Shared.RequestNamingTests. What stays here is Web API 2 specific:
// "Snake_WithoutParameter_IsNotBound" genuinely diverges from an [ApiController] (its implicit required-member
// validation turns a null FirstName into a 400 there, unlike here or under minimal APIs), and the plain-POCO
// Newtonsoft opt-out only makes sense for this host's dual-formatter design.
public class RequestNamingTest(ITestOutputHelper output) : BaseTest<RequestNamingTest>(output)
{
	static StringContent Body(string json, string? naming, string mediaType = "application/json")
	{
		var content = new StringContent(json, System.Text.Encoding.UTF8, mediaType);
		if (naming is not null)
			content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, naming));
		return content;
	}

	[Fact(DisplayName = "Without the parameter a snake_case body is not understood")]
	public async Task Snake_WithoutParameter_IsNotBound()
	{
		var res = await AspNetHost.Create().PostAsync("naming/echo", Body("""{"first_name":"Ada","age":36}""", null));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Null((string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	// PlainTwoWords is a plain (non-Fuxion) type, never opted into System.Text.Json by AspNetHost: under the
	// default scope it is bound by Newtonsoft, which knows nothing about the naming parameter and ignores it,
	// rather than transcoding the body. Under scope All every type goes through the formatter that does honour it.
	[Fact(DisplayName = "A plain POCO body keeps Newtonsoft and ignores the naming parameter under the default scope")]
	public async Task PlainPoco_IgnoresNamingUnderDefaultScope()
	{
		var defaultScope = await AspNetHost.Create().PostAsync("plain/two-words", Body("""{"first_name":"Ada"}""", "snake"));
		Assert.Equal(HttpStatusCode.OK, defaultScope.StatusCode);
		Assert.Null((string?)JsonNode.Parse(await defaultScope.Content.ReadAsStringAsync())!["firstName"]);

		var allScope = await AspNetHost.Create(scope: JsonFormatterScope.All).PostAsync("plain/two-words", Body("""{"first_name":"Ada"}""", "snake"));
		Assert.Equal(HttpStatusCode.OK, allScope.StatusCode);
		Assert.Equal("Ada", (string?)JsonNode.Parse(await allScope.Content.ReadAsStringAsync())!["firstName"]);
	}

	// Web API 2 specific: unlike the shared 400-only assertion (Test.Responses.Shared.RequestNamingTests -
	// an ASP.NET Core controller's own automatic model validation answers a different, ValidationProblemDetails
	// shape for the same case), SystemTextJsonMediaTypeFormatter builds a plain RFC 9457 ResponseProblemDetails
	// body itself, matching what AspNetCore's minimal-API RequestNamingEndpoint writes for an unsupported or
	// duplicated 'naming' parameter.
	[Fact(DisplayName = "An unsupported naming parameter is a problem+json 400 whose detail mentions naming")]
	public async Task UnsupportedNaming_Is400Problem()
	{
		var res = await AspNetHost.Create().PostAsync("naming/echo", Body("""{"first_name":"Ada","age":36}""", "whatever"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Contains(ResponseMediaTypes.NamingParameter, (string?)body["detail"], System.StringComparison.OrdinalIgnoreCase);
	}
}
