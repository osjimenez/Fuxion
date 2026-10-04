using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore;

public class ScopesTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ScopesTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	[Fact(DisplayName = "Subgroup override wins over global options")]
	public async Task SubgroupOverride_ForcesFullResponseEnvelope()
	{
		// The global options ask for the raw payload, but the subgroup forces the full envelope.
		var cli = factory.CreateClient(new ResponseOptions { SerializeFullResponses = false, SerializeErrorAsProblemDetails = true, StrictNone = false });
		var res = await cli.GetAsync("minimal/special/payload");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);

		var body = await res.Content.ReadAsStringAsync();
		PrintVariable(JsonNode.Parse(body)?.ToJsonString(JsonSerializerOptions.Formatted), false);

		// El envelope completo expone el payload anidado, no en la raiz.
		var json = Assert.IsType<JsonObject>(JsonNode.Parse(body));
		IsTrue(json.ContainsKey("payload"));
	}

	[Fact(DisplayName = "Three scope levels merge partially: the child overrides one flag and inherits the rest")]
	public async Task ThreeLevels_MergePartially()
	{
		var cli = factory.CreateClient();
		// global: full=false, strictNone=false; sub: full=true; deep: strictNone=true (inherits full=true)
		Assert.Equal("application/json", (await cli.GetAsync("minimal/response/payload")).Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("sub/payload")).Content.Headers.ContentType?.MediaType);
		var subNone = await cli.GetAsync("sub/none");
		Assert.Equal(HttpStatusCode.OK, subNone.StatusCode); // full=true, strictNone=false → envelope
		Assert.Equal(ResponseMediaTypes.ResponseJson, subNone.Content.Headers.ContentType?.MediaType);
		var subNoneBody = Assert.IsType<JsonObject>(JsonNode.Parse(await subNone.Content.ReadAsStringAsync()));
		IsTrue((bool?)subNoneBody["isNone"]);
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("sub/deep/payload")).Content.Headers.ContentType?.MediaType); // inherited
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync("sub/deep/none")).StatusCode); // strictNone=true wins over inherited full=true
	}

	[Fact(DisplayName = "An override on a single endpoint applies to it alone")]
	public async Task EndpointOverride_IsLocal()
	{
		var cli = factory.CreateClient();
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("minimal/response/payload-enveloped")).Content.Headers.ContentType?.MediaType);
		Assert.Equal("application/json", (await cli.GetAsync("minimal/response/payload")).Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "A bare Error or None returned from a lambda is mapped like its response wrapper")]
	public async Task BareErrorAndNone_AreMapped()
	{
		var cli = factory.CreateClient();
		var error = await cli.GetAsync("minimal/result/bare-error");
		Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync("minimal/result/bare-none")).StatusCode);
		var plain = await cli.GetAsync("minimal/result/bare-value");
		Assert.Equal("123", await plain.Content.ReadAsStringAsync());
		IsFalse(plain.Headers.Vary.Any());
	}

	[Fact(DisplayName = "Neighbouring non-union endpoints are untouched by UseResponses")]
	public async Task NonUnionNeighbours_AreUntouched()
	{
		// Pins the observable contract - non-union values are never mapped, and no Vary is added - independently
		// of which layer (the endpoint filter, the naming wrapper...) is responsible for enforcing it.
		var cli = factory.CreateClient();
		var list = await cli.GetAsync("minimal/plain/list");
		Assert.Equal("""["a","b"]""", await list.Content.ReadAsStringAsync());
		IsFalse(list.Headers.Vary.Any());
		var ok = await cli.GetAsync("minimal/plain/ok");
		Assert.Equal("plain", (string?)JsonNode.Parse(await ok.Content.ReadAsStringAsync())!["name"]);
		IsFalse(ok.Headers.Vary.Any());
	}

	// "Accept: application/json" is inert by design (it never selects the envelope), so it cannot prove that
	// explicit options ignore Accept. The plain rows send an Accept that *would* select the envelope if
	// consulted, and assert the endpoint still answers with the bare media type its explicit options demand.
	[Theory(DisplayName = "ToResult/ToActionResult with explicit options ignore the scope and the Accept header, whether forcing the envelope or the plain shape")]
	[InlineData("minimal/result/explicit-envelope", "application/json", ResponseMediaTypes.ResponseJson)]
	[InlineData("controller/result/explicit-envelope", "application/json", ResponseMediaTypes.ResponseJson)]
	[InlineData("minimal/result/explicit-plain", "application/vnd.fuxion.response+json, application/json;q=0.9", "application/json")]
	[InlineData("controller/result/explicit-plain", "application/vnd.fuxion.response+json, application/json;q=0.9", "application/json")]
	public async Task ExplicitOptions_IgnoreScopeAndAccept(string url, string accept, string expectedMediaType)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.TryAddWithoutValidation("Accept", accept);
		var res = await factory.CreateClient().SendAsync(request);
		Assert.Equal(expectedMediaType, res.Content.Headers.ContentType?.MediaType);
		// Only the envelope shape has an "isSuccess" wrapper to inspect; the plain shape is the bare payload.
		if (expectedMediaType == ResponseMediaTypes.ResponseJson)
			IsTrue((bool?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["isSuccess"]);
	}
}
