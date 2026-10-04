using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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

namespace Test.AspNetCore;

// A typed business error (TError that is not Fuxion's Error) must never be lost on the wire.
// SerializeErrorAsProblemDetails governs ALL errors uniformly: when it is on, the typed error
// travels inside the standard problem+json shape, carried in the same errorPayload extension
// member that Error.Payload already uses.
public class TypedErrorTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<TypedErrorTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	private (HttpClient client, JsonSerializerOptions jsonOptions) CreateClient(ResponseOptions options)
	{
		return (factory.CreateClient(options), new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});
	}

	// The default options (problem+json without the full envelope) are covered by the shared WireContractTests:
	// what stays here is that problem+json also wins when the full envelope is asked for, as with the native Error.
	[Theory(DisplayName = "A typed error travels as problem+json carrying it in the errorPayload extension, even when the full envelope is on")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task TypedError_TravelsAsProblemDetailsWithErrorPayload(string prefix)
	{
		// Defaults: SerializeErrorAsProblemDetails = true.
		var (cli, _) = CreateClient(new() { SerializeFullResponses = true });

		var res = await cli.GetAsync($"{prefix}/response/typed-error", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);

		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		PrintVariable(body.ToJsonString(), false);

		Assert.Equal("Business error", (string?)body["title"]);
		Assert.Null(body["type"]); // implicit about:blank, without leaking the CLR type
		Assert.Equal(409, (int?)body["status"]); // TestBusinessError declares Conflict through IHttpStatusError

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

		var response = await cli.GetAsync($"{prefix}/response/typed-error", TestContext.Current.CancellationToken).AsResponseAsync<string, TestBusinessError>(jsonOptions, TestContext.Current.CancellationToken);

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

		var res = await cli.GetAsync($"{prefix}/response/typed-error", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);

		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		PrintVariable(body.ToJsonString(), false);

		// The body is the bare TError, not the envelope.
		Assert.Null(body["isSuccess"]);
		Assert.Equal(TestBusinessError.Default.Code, (string?)body["code"]);
		Assert.Equal(TestBusinessError.Default.Reason, (string?)body["reason"]);

		var response = await res.AsResponseAsync<string, TestBusinessError>(jsonOptions, TestContext.Current.CancellationToken);
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

		var res = await cli.GetAsync($"{prefix}/response/typed-error", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ResponseJson, res.Content.Headers.ContentType?.MediaType);

		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
		PrintVariable(body.ToJsonString(), false);
		Assert.False((bool?)body["isSuccess"]);

		var response = await res.AsResponseAsync<string, TestBusinessError>(jsonOptions, TestContext.Current.CancellationToken);
		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}
}
