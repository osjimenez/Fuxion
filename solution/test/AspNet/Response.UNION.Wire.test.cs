using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.Http;
using Fuxion.AspNet;
using Fuxion.Union;
using Fuxion.Xunit;
using Test.AspNet.Service;
using Xunit;

namespace Test.AspNet.Union;

// Same wire as ASP.NET Core, produced by Web API 2 through the shared core mapper.
public class WireContractTest(ITestOutputHelper output) : BaseTest<WireContractTest>(output)
{
	static HttpRequestMessage Get(string url, string? accept = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		return request;
	}

	[Fact(DisplayName = "Unit is a 200 with an empty object body and its own media type")]
	public async Task Unit_IsEmptyObjectWithMediaType()
	{
		var res = await AspNetHost.Create().GetAsync("response/unit");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.UnitJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		Assert.Equal("{}", await res.Content.ReadAsStringAsync());
	}

	[Fact(DisplayName = "None is a body-less 204")]
	public async Task None_Is204()
	{
		var res = await AspNetHost.Create().GetAsync("response/none");
		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
	}

	[Fact(DisplayName = "A bare payload is plain camelCase application/json with Vary: Accept")]
	public async Task Payload_IsPlainJson()
	{
		var res = await AspNetHost.Create().GetAsync("response/payload");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		// application/json never carries parameters (not the naming one, not even a charset).
		Assert.Empty(res.Content.Headers.ContentType!.Parameters);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal("test", (string?)body["name"]);
		Assert.Contains("Accept", res.Headers.Vary);
	}

	[Fact(DisplayName = "Errors are problem+json in camel with the status from Error.Type")]
	public async Task Error_IsProblemJson()
	{
		var res = await AspNetHost.Create().GetAsync("response/error-type");
		Assert.Equal(HttpStatusCode.NotImplemented, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal(501, (int?)body["status"]);
	}

	[Fact(DisplayName = "A typed error travels as problem+json with the errorPayload extension")]
	public async Task TypedError_IsProblemWithPayload()
	{
		var res = await AspNetHost.Create().GetAsync("response/typed-error");
		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal("Business error", (string?)body["title"]);
		Assert.Equal("stock", (string?)body["errorPayload"]!["code"]);
	}

	[Fact(DisplayName = "Asking for the envelope through Accept wins over the defaults")]
	public async Task Accept_Envelope_Wins()
	{
		var res = await AspNetHost.Create().SendAsync(Get("response/payload", "application/vnd.fuxion.response+json, application/json;q=0.9"));
		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);
		Assert.True((bool?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["isSuccess"]);
	}

	[Fact(DisplayName = "Asking for native errors through Accept turns problem details off")]
	public async Task Accept_NativeError_Wins()
	{
		var res = await AspNetHost.Create().SendAsync(Get("response/error-message", "application/vnd.fuxion.error+json, application/json;q=0.9"));
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
	}

	[Theory(DisplayName = "A wildcard or plain JSON Accept keeps the defaults")]
	[InlineData("*/*")]
	[InlineData("application/json")]
	public async Task Accept_Vanilla_KeepsDefaults(string accept)
	{
		var res = await AspNetHost.Create().SendAsync(Get("response/payload", accept));
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		// Proves the union path actually ran (not just that the framework's own default happens to match).
		Assert.Contains("Accept", res.Headers.Vary);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal("test", (string?)body["name"]);
	}

	[Fact(DisplayName = "Global options are the defaults for clients that ask nothing")]
	public async Task GlobalOptions_AreDefaults()
	{
		var res = await AspNetHost.Create(o => o.SerializeFullResponses = true).GetAsync("response/payload");
		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "The scope cascade is global, then controller attribute, then action attribute")]
	public async Task Attributes_Cascade()
	{
		var cli = AspNetHost.Create();
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("special/payload")).Content.Headers.ContentType?.MediaType);
		Assert.Equal("application/json", (await cli.GetAsync("special/payload-bare")).Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "Fuxion media types announce the naming policy the server uses")]
	public async Task NamingParameter_FollowsServerPolicy()
	{
		var snake = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
		var res = await AspNetHost.Create(jsonOptions: snake).SendAsync(Get("response/payload", "application/vnd.fuxion.response+json"));
		Assert.Equal(ResponseNaming.Snake, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		Assert.True((bool?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["is_success"]);
	}

	[Fact(DisplayName = "An uninitialized response is a 500 problem")]
	public async Task Unset_Is500()
	{
		var res = await AspNetHost.Create().GetAsync("response/unset");
		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "Endpoints that do not declare a union type are never touched")]
	public async Task NonUnion_IsUntouched()
	{
		var res = await AspNetHost.Create().GetAsync("plain/list");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		IsTrue(!res.Headers.Vary.Any());
		Assert.Equal("""["a","b"]""", await res.Content.ReadAsStringAsync());
	}

	// ReflectedHttpActionDescriptor.ReturnType is null for void and non-generic Task actions: the filter
	// must not crash on a null declared return type and must leave the framework's own 204 alone.
	[Theory(DisplayName = "void and Task actions keep the framework's 204")]
	[InlineData("plain/void")]
	[InlineData("plain/task")]
	public async Task VoidAndTask_Keep204(string url)
	{
		var res = await AspNetHost.Create().GetAsync(url);
		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
	}

	[Fact(DisplayName = "An async union action is mapped like a sync one")]
	public async Task AsyncAction_IsMappedLikeSync()
	{
		var res = await AspNetHost.Create().GetAsync("response/async-payload");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		Assert.Contains("Accept", res.Headers.Vary);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal("test", (string?)body["name"]);
	}

	[Fact(DisplayName = "UseResponses can only be applied once")]
	public void UseResponses_CanOnlyBeAppliedOnce()
	{
		using var config = new HttpConfiguration();
		config.UseResponses();
		Assert.Throws<InvalidOperationException>(() => config.UseResponses());
	}
}
