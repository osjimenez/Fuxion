using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.AspNetCore;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

// The wire describes itself: media types (with the naming parameter) say what the body is, and the
// client asks for a shape through Accept. No custom header is involved anywhere.
public class WireContractTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<WireContractTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	HttpClient CreateClient(ResponseOptions? options = null)
	{
		if (options is null) return factory.CreateClient();
		return factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Configure<ResponseOptions>(o =>
		{
			o.SerializeFullResponses = options.SerializeFullResponses;
			o.SerializeErrorAsProblemDetails = options.SerializeErrorAsProblemDetails;
			o.StrictNone = options.StrictNone;
		}))).CreateClient();
	}

	static HttpRequestMessage Get(string url, string? accept = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		return request;
	}

	[Theory(DisplayName = "Unit is a 200 with an empty object body and its own media type")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Unit_IsEmptyObjectWithMediaType(string prefix)
	{
		var res = await CreateClient().GetAsync($"{prefix}/response/unit");

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.UnitJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		Assert.Equal("{}", await res.Content.ReadAsStringAsync());
		IsTrue(!res.Headers.Contains("fuxion-response-kind"));
	}

	[Theory(DisplayName = "None is a body-less 204 without any custom header")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task None_Is204(string prefix)
	{
		var res = await CreateClient().GetAsync($"{prefix}/response/none");

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		IsTrue(!res.Headers.Contains("fuxion-response-kind"));
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

	[Theory(DisplayName = "A bare payload is plain application/json without parameters")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Payload_IsPlainJson(string prefix)
	{
		var res = await CreateClient().GetAsync($"{prefix}/response/payload");

		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		Assert.Empty(res.Content.Headers.ContentType!.Parameters.Where(p => p.Name == ResponseMediaTypes.NamingParameter));
	}

	[Theory(DisplayName = "Asking for the envelope through Accept wins over the scope defaults")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Accept_Envelope_WinsOverDefaults(string prefix)
	{
		var res = await CreateClient().SendAsync(Get($"{prefix}/response/payload", "application/vnd.fuxion.response+json, application/json;q=0.9"));

		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.True((bool?)body["isSuccess"]);
	}

	[Theory(DisplayName = "Asking for native errors through Accept turns problem details off for that request")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Accept_NativeError_WinsOverDefaults(string prefix)
	{
		var res = await CreateClient().SendAsync(Get($"{prefix}/response/error-message", "application/vnd.fuxion.error+json, application/json;q=0.9"));

		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Theory(DisplayName = "A wildcard or plain JSON Accept keeps the scope defaults")]
	[InlineData("*/*")]
	[InlineData("application/json")]
	public async Task Accept_Vanilla_KeepsDefaults(string accept)
	{
		var res = await CreateClient().SendAsync(Get("minimal/response/payload", accept));
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);

		var error = await CreateClient().SendAsync(Get("minimal/response/error-message", accept));
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);
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

	[Fact(DisplayName = "Fuxion media types announce the naming policy the server actually uses")]
	public async Task NamingParameter_FollowsServerPolicy()
	{
		var cli = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
			s.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower)))
			.CreateClient();

		var res = await cli.SendAsync(Get("minimal/response/payload", "application/vnd.fuxion.response+json"));

		Assert.Equal(ResponseNaming.Snake, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.True((bool?)body["is_success"]);
	}

	[Fact(DisplayName = "A typed error from a snake_case server is still recoverable through problem+json")]
	public async Task TypedError_FromSnakeCaseServer_IsRecoverable()
	{
		var cli = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
			s.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower)))
			.CreateClient();

		var res = await cli.GetAsync("minimal/response/typed-error");

		var raw = await res.Content.ReadAsStringAsync();
		Assert.Contains("\"errorPayload\"", raw);

		var response = await cli.GetAsync("minimal/response/typed-error").AsResponseAsync<string, TestBusinessError>();

		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}
}
