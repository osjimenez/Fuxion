using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
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

// A typed business error (TError that is not Fuxion's Error) must never be lost on the wire.
// SerializeErrorAsProblemDetails governs ALL errors uniformly: when it is on, the typed error
// travels inside the standard problem+json shape, carried in the same errorPayload extension
// member that Error.Payload already uses.
public class TypedErrorTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<TypedErrorTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	private (HttpClient client, JsonSerializerOptions jsonOptions) CreateClient(ResponseOptions options)
	{
		var currentFactory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Configure<ResponseOptions>(o =>
		{
			o.SerializeFullResponses = options.SerializeFullResponses;
			o.SerializeErrorAsProblemDetails = options.SerializeErrorAsProblemDetails;
			o.StrictNone = options.StrictNone;
		})));

		return (currentFactory.CreateClient(), new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});
	}

	[Theory(DisplayName = "A typed error travels as problem+json carrying it in the errorPayload extension")]
	[InlineData("minimal", false)]
	[InlineData("controller", false)]
	// El problem+json gana tambien cuando se pide el envelope completo, igual que con el Error nativo.
	[InlineData("minimal", true)]
	[InlineData("controller", true)]
	public async Task TypedError_TravelsAsProblemDetailsWithErrorPayload(string prefix, bool fullResponses)
	{
		// Defaults: SerializeErrorAsProblemDetails = true.
		var (cli, _) = CreateClient(new() { SerializeFullResponses = fullResponses });

		var res = await cli.GetAsync($"{prefix}/response/typed-error");

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);

		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		PrintVariable(body.ToJsonString(), false);

		Assert.Equal("Business error", (string?)body["title"]);
		Assert.Null(body["type"]); // about:blank implicito, sin filtrar el tipo CLR
		Assert.Equal(500, (int?)body["status"]);

		var payload = body["errorPayload"];
		Assert.NotNull(payload);
		Assert.Equal(TestBusinessError.Default.Code, (string?)payload!["code"]);
		Assert.Equal(TestBusinessError.Default.Reason, (string?)payload["reason"]);
	}

	[Theory(DisplayName = "The client recovers the typed error from the problem+json body")]
	[InlineData("minimal", false)]
	[InlineData("controller", false)]
	[InlineData("minimal", true)]
	[InlineData("controller", true)]
	public async Task TypedError_Client_RecoversItFromProblemBody(string prefix, bool fullResponses)
	{
		ResponseOptions options = new() { SerializeFullResponses = fullResponses };
		var (cli, jsonOptions) = CreateClient(options);

		var response = await cli.GetAsync($"{prefix}/response/typed-error").AsResponseAsync<string, TestBusinessError>(options, jsonOptions);

		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}

	[Theory(DisplayName = "With problem details disabled a typed error travels as the bare value")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task TypedError_RawMode_TravelsAsBareValue(string prefix)
	{
		ResponseOptions options = new() { SerializeErrorAsProblemDetails = false };
		var (cli, jsonOptions) = CreateClient(options);

		var res = await cli.GetAsync($"{prefix}/response/typed-error");

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseHeaders.ErrorKind, res.Headers.GetValues(ResponseHeaders.ResponseKind).Single());

		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		PrintVariable(body.ToJsonString(), false);

		// El cuerpo es el TError pelado, no el envelope.
		Assert.Null(body["isSuccess"]);
		Assert.Equal(TestBusinessError.Default.Code, (string?)body["code"]);
		Assert.Equal(TestBusinessError.Default.Reason, (string?)body["reason"]);

		var response = await res.AsResponseAsync<string, TestBusinessError>(options, jsonOptions);
		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}

	[Theory(DisplayName = "With the full envelope enabled a typed error travels inside it")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task TypedError_FullEnvelope_KeepsItInside(string prefix)
	{
		ResponseOptions options = new() { SerializeErrorAsProblemDetails = false, SerializeFullResponses = true };
		var (cli, jsonOptions) = CreateClient(options);

		var res = await cli.GetAsync($"{prefix}/response/typed-error");

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);

		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		PrintVariable(body.ToJsonString(), false);
		Assert.False((bool?)body["isSuccess"]);

		var response = await res.AsResponseAsync<string, TestBusinessError>(options, jsonOptions);
		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}

	[Fact(DisplayName = "A problem body without the errorPayload extension cannot synthesize a typed error and throws")]
	public async Task TypedError_ProblemWithoutPayload_Throws()
	{
		var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
		{
			Content = new StringContent(
				"""{"title":"Bad request","status":400}""",
				System.Text.Encoding.UTF8,
				ResponseMediaTypes.ProblemJson)
		};

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () => await message.AsResponseAsync<string, TestBusinessError>());
	}
}
