using Fuxion;
using static Test.Responses.Shared.WireRequests;

namespace Test.Responses.Shared;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Xunit;

/// <summary>
/// The client declares how it wrote the request body in its own Content-Type; the server adapts per request.
/// </summary>
public abstract class RequestNamingTests : WireTestBase<RequestNamingTests>
{
	/// <summary>
	/// Whether a duplicated naming parameter may surface as a 415 on this host, instead of always a 400.
	/// Only Web API 2's own <c>MediaTypeHeaderValue</c> parser can reject the malformed Content-Type before
	/// the request reaches the naming support; ASP.NET Core hosts always answer 400.
	/// </summary>
	protected virtual bool DuplicatedNamingMayBe415 => false;

	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected RequestNamingTests(ITestOutputHelper output, IWireHost host) : base(output, host) { }

	[Theory(DisplayName = "A snake_case or kebab-case body is bound when its Content-Type declares the naming")]
	[InlineData("""{"first_name":"Ada","age":36}""", "snake", "Ada", 36)]
	[InlineData("""{"first-name":"Grace","age":45}""", "kebab", "Grace", 45)]
	public async Task SeparatedNaming_IsBound(string json, string naming, string expectedFirstName, int expectedAge)
	{
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingEcho), Body(json, naming), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal(expectedFirstName, (string?)body["firstName"]);
		Assert.Equal(expectedAge, (int?)body["age"]);
	}

	// Not shared: without the naming parameter, a snake_case body binds its non-nullable FirstName as null,
	// which [ApiController]'s implicit required-member validation (nullable reference types) turns into a
	// 400 for a controller - a real per-host divergence, not something Route() can paper over - while
	// Web API 2 and minimal APIs both let it through as 200 with FirstName null. Each host keeps its own
	// version of this test instead.

	[Fact(DisplayName = "A non-JSON body declaring a naming parameter is never touched")]
	public async Task NonJson_IsNeverTouched()
	{
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingEcho), Body("first_name=Ada", "snake", "text/plain"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.UnsupportedMediaType, res.StatusCode);
	}

	[Fact(DisplayName = "A malformed body declaring a naming parameter fails like any malformed body: 400, not 500")]
	public async Task Malformed_IsBadRequest()
	{
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingMalformed), Body("""{"first_name":"Ada",""", "snake"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact(DisplayName = "Dictionary keys are never renamed when a naming is declared")]
	public async Task Dictionary_KeysUntouched()
	{
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingDictionary), Body("""{"first_name":"Ada","tags":{"my_tag":1,"other-tag":2}}""", "snake"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var json = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal("Ada", (string?)json["name"]);
		Assert.Equal(new[] { "my_tag", "other-tag" }, json["keys"]!.AsArray().Select(n => (string?)n).ToArray());
	}

	// Every host answers with problem+json (Web API 2: ResponseProblemDetails; MVC: ValidationProblemDetails;
	// minimal APIs: Results.Problem); only the wording of "detail" is host-specific, so it is not asserted here.
	[Theory(DisplayName = "An unsupported naming parameter is a 400 problem+json")]
	[InlineData("custom")]
	[InlineData("whatever")]
	public async Task UnsupportedNaming_Is400(string naming)
	{
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingEcho), Body("""{"first_name":"Ada","age":36}""", naming), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(400, (int?)JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["status"]);
	}

	[Fact(DisplayName = "A duplicated naming parameter is rejected, whether by the framework's own parsing or by the naming support")]
	public async Task DuplicatedNaming_IsRejected()
	{
		var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("""{"first_name":"Ada","age":36}"""));
		content.Headers.TryAddWithoutValidation("Content-Type", "application/json; naming=snake; naming=kebab");

		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingEcho), content, TestContext.Current.CancellationToken);
		Output.WriteLine($"Observed status for a duplicated naming parameter: {(int)res.StatusCode} {res.StatusCode}.");
		if (DuplicatedNamingMayBe415)
			Assert.True(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType,
				$"Expected 400 or 415, got {(int)res.StatusCode} {res.StatusCode}.");
		else
			Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}
	// RFC 8259 asks for UTF-8 between systems, but a client may still send another encoding and declare it in the
	// charset parameter (legacy clients, UTF-16 from some .NET stacks). Every host must decode it, not assume UTF-8.
	[Theory(DisplayName = "A UTF-16 body declared by its charset is read like a UTF-8 one")]
	[InlineData(null, """{"firstName":"Ñandú 漢字","age":36}""")]
	[InlineData("snake", """{"first_name":"Ñandú 漢字","age":36}""")]
	public async Task Utf16Body_IsDecoded(string? naming, string json)
	{
		var content = new ByteArrayContent(Encoding.Unicode.GetBytes(json));
		content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-16" + (naming is null ? "" : $"; naming={naming}"));
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingEcho), content, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		Assert.Equal("Ñandú 漢字", (string?)body["firstName"]);
		Assert.Equal(36, (int?)body["age"]);
	}

	[Fact(DisplayName = "A body in an unsupported charset is a 415")]
	public async Task UnsupportedCharset_Is415()
	{
		var content = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"firstName":"Ada","age":36}"""));
		content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=shift_jis");
		var res = await Host.CreateClient().PostAsync(Host.Route(Routes.NamingEcho), content, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.UnsupportedMediaType, res.StatusCode);
	}
}
