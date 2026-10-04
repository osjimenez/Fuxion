using Fuxion;
using static Test.Responses.Shared.WireRequests;

namespace Test.Responses.Shared;

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Xunit;

/// <summary>
/// The wire-contract matrix shared by every host: media types (with the naming parameter) say what the
/// body is, and the client asks for a shape through Accept. No custom header is involved anywhere.
/// Concrete per-host subclasses only provide an <see cref="IWireHost"/>.
/// </summary>
public abstract class WireContractTests : WireTestBase<WireContractTests>
{
	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected WireContractTests(ITestOutputHelper output, IWireHost host) : base(output, host) { }

	[Fact(DisplayName = "Unit is a 200 with an empty object body and its own media type")]
	public async Task Unit_IsEmptyObjectWithMediaType()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.ResponseUnit), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.UnitJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		Assert.Equal("{}", await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
		IsFalse(res.Headers.Contains("fuxion-response-kind"));
	}

	[Fact(DisplayName = "None is a body-less 204")]
	public async Task None_Is204()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.ResponseNone), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		IsFalse(res.Headers.Contains("fuxion-response-kind"));
	}

	[Fact(DisplayName = "A bare payload is plain camelCase application/json with Vary: Accept")]
	public async Task Payload_IsPlainJson()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.ResponsePayload), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		// application/json never carries parameters (not the naming one, not even a charset).
		Assert.Empty(res.Content.Headers.ContentType!.Parameters);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal("test", (string?)body["name"]);
		Assert.Contains("Accept", res.Headers.Vary);
	}

	[Fact(DisplayName = "A native error type is problem+json with the status from Error.Type")]
	public async Task Error_IsProblemJson()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.ResponseErrorType), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NotImplemented, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal(501, (int?)body["status"]);
	}

	[Fact(DisplayName = "A typed error travels as problem+json with the errorPayload extension, at its declared status")]
	public async Task TypedError_IsProblemWithPayload()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.ResponseTypedError), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal("Business error", (string?)body["title"]);
		Assert.Equal(409, (int?)body["status"]);
		Assert.Equal("stock", (string?)body["errorPayload"]!["code"]);
	}

	[Fact(DisplayName = "The service's BusinessErrorStatus classifies a foreign error that declares no status of its own")]
	public async Task TypedErrorForeign_UsesServiceOverride()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.ResponseTypedErrorForeign), TestContext.Current.CancellationToken);
		// Cast to keep this test compiling under net472, whose HttpStatusCode enum has no TooManyRequests member.
		Assert.Equal((HttpStatusCode)429, res.StatusCode);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal(429, (int?)body["status"]);
		Assert.Equal("quota", (string?)body["errorPayload"]!["code"]);
	}

	[Fact(DisplayName = "Asking for the envelope through Accept wins over the defaults")]
	public async Task Accept_Envelope_Wins()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.ResponsePayload), "application/vnd.fuxion.response+json, application/json;q=0.9"), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);
		IsTrue((bool?)JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["isSuccess"]);
	}

	[Fact(DisplayName = "Asking for native errors through Accept turns problem details off")]
	public async Task Accept_NativeError_Wins()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.ResponseErrorMessage), "application/vnd.fuxion.error+json, application/json;q=0.9"), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
	}

	[Theory(DisplayName = "A wildcard or plain JSON Accept keeps the defaults")]
	[InlineData("*/*")]
	[InlineData("application/json")]
	public async Task Accept_Vanilla_KeepsDefaults(string accept)
	{
		var cli = Host.CreateClient();

		var res = await cli.SendAsync(Get(Host.Route(Routes.ResponsePayload), accept), TestContext.Current.CancellationToken);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		// Proves the union path actually ran (not just that the framework's own default happens to match).
		Assert.Contains("Accept", res.Headers.Vary);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal("test", (string?)body["name"]);

		var error = await cli.SendAsync(Get(Host.Route(Routes.ResponseErrorMessage), accept), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "Global options are the defaults for clients that ask nothing")]
	public async Task GlobalOptions_AreDefaults()
	{
		var res = await Host.CreateClient(o => o.SerializeFullResponses = true).GetAsync(Host.Route(Routes.ResponsePayload), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "Fuxion media types announce the naming policy the server uses")]
	public async Task NamingParameter_FollowsServerPolicy()
	{
		var res = await Host.CreateClient(namingPolicy: JsonNamingPolicy.SnakeCaseLower).SendAsync(Get(Host.Route(Routes.ResponsePayload), "application/vnd.fuxion.response+json"), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseNaming.Snake, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		IsTrue((bool?)JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["is_success"]);
	}

	[Fact(DisplayName = "The client can ask for a naming policy on Fuxion types")]
	public async Task Accept_Naming_IsHonoured()
	{
		// The envelope announces its naming through the vnd media type parameter, so a multi-word payload
		// nested inside it is transcoded too - unlike a bare application/json payload, which never announces
		// (and therefore never carries) a requested naming (see ResponseWireMapper.TryMap).
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.ResponseNamingPayload), "application/vnd.fuxion.response+json; naming=snake, application/json;q=0.9"), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseNaming.Snake, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		IsTrue((bool?)body["is_success"]);
		Assert.Equal("Ada", (string?)body["payload"]!["first_name"]);
	}
}
