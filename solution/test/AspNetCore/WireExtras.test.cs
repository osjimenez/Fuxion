using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;
using Test.Responses.Shared.Fixtures;
using Xunit;
using static Test.Responses.Shared.WireRequests;

namespace Test.AspNetCore;

// The base wire contract shared with Web API 2 lives in Test.Responses.Shared.WireContractTests.
// What stays here does not have a Web API 2 (or, for "unset", a controller)
// counterpart: options-driven native-error mode, Vary across several routes, the controller-attribute
// cascade demo, the "unset" fixture (minimal-only), and a snake_case-server typed-error recovery check.
public class AspNetCoreWireExtrasTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<AspNetCoreWireExtrasTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	HttpClient CreateClient(ResponseOptions? options = null)
	{
		if (options is null) return factory.CreateClient();
		return factory.CreateClient(options);
	}

	[Theory(DisplayName = "A native error uses its vendor media type with the naming parameter")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task NativeError_UsesVendorMediaType(string prefix)
	{
		var res = await CreateClient(new() { SerializeErrorAsProblemDetails = false }).GetAsync($"{prefix}/response/error-message");

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
	}

	[Theory(DisplayName = "Mapped responses vary by Accept so shared caches never mix shapes")]
	[InlineData("minimal/response/payload")]
	[InlineData("controller/response/payload")]
	[InlineData("minimal/response/none")]
	[InlineData("minimal/response/error-message")]
	public async Task MappedResponses_VaryByAccept(string url)
	{
		var res = await CreateClient().GetAsync(url);
		Assert.Contains("Accept", res.Headers.Vary);
	}

	[Fact(DisplayName = "Controller and action attributes cascade")]
	public async Task Attributes_Cascade()
	{
		var cli = CreateClient();
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("attribute-test/payload")).Content.Headers.ContentType?.MediaType);
		Assert.Equal("application/json", (await cli.GetAsync("attribute-test/payload-bare")).Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "An uninitialized response follows Accept like any other error")]
	public async Task Unset_FollowsAccept()
	{
		var res = await CreateClient().SendAsync(Get("minimal/response/unset", "application/vnd.fuxion.error+json, application/json;q=0.9"));

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "A typed error from a snake_case server is still recoverable through problem+json")]
	public async Task TypedError_FromSnakeCaseServer_IsRecoverable()
	{
		var cli = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
			s.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower)))
			.CreateClient();

		var res = await cli.GetAsync("minimal/response/typed-error");

		var raw = await res.Content.ReadAsStringAsync();
		IsTrue(JsonNode.Parse(raw)!.AsObject().ContainsKey("errorPayload"));

		var response = await cli.GetAsync("minimal/response/typed-error").AsResponseAsync<string, TestBusinessError>();

		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}
}
