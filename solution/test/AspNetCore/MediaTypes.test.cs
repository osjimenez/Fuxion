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

public class MediaTypesTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<MediaTypesTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	private async Task<(HttpClient client, JsonSerializerOptions jsonOptions, HttpResponseMessage message)> GetMessage(
		string prefix,
		ResponseOptions options,
		string path,
		HttpStatusCode expectedStatus)
	{
		var cli = factory.CreateClient(options);
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		var url = $"{prefix}{path}";
		PrintVariable($"""

			----- {url}
			 - fullResponse({options.SerializeFullResponses})
			 - errorAsProblemDetails({options.SerializeErrorAsProblemDetails})
			 - strictNone({options.StrictNone})
		""", false);
		var res = await cli.GetAsync(url);
		PrintVariable(res.StatusCode);
		Assert.Equal(expectedStatus, res.StatusCode);

		var body = await res.Content.ReadAsStringAsync();
		if (body.IsNeitherNullNorWhiteSpace())
			PrintVariable(JsonNode.Parse(body)?.ToJsonString(JsonSerializerOptions.Formatted), false);

		return (cli, jsonOptions, res);
	}

	[Theory(DisplayName = "The server emits the standard problem+json media type")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task ErrorResponse_UsesProblemJsonMediaType(string prefix)
	{
		// Without this, the client detecting ProblemDetails by media type would be dead code.
		ResponseOptions options = new()
		{
			SerializeFullResponses = false,
			SerializeErrorAsProblemDetails = true,
			StrictNone = false
		};

		foreach (var group in new[] { "/response/", "/result/" })
		{
			var (_, _, res) = await GetMessage(prefix + group, options, "error-message", HttpStatusCode.InternalServerError);
			Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
		}
	}

	[Theory(DisplayName = "The full envelope is advertised with its vendor media type")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task FullResponse_UsesVendorMediaType(string prefix)
	{
		ResponseOptions options = new()
		{
			SerializeFullResponses = true,
			SerializeErrorAsProblemDetails = true,
			StrictNone = false
		};

		foreach (var group in new[] { "/response/", "/result/" })
		{
			var (_, _, envelope) = await GetMessage(prefix + group, options, "payload", HttpStatusCode.OK);
			Assert.Equal(ResponseMediaTypes.ResponseJson, envelope.Content.Headers.ContentType?.MediaType);
		}

		// A raw payload must not be announced as an envelope.
		ResponseOptions raw = options with { SerializeFullResponses = false };
		var (_, _, bare) = await GetMessage(prefix + "/result/", raw, "payload", HttpStatusCode.OK);
		Assert.Equal("application/json", bare.Content.Headers.ContentType?.MediaType);
	}

	[Theory]
	[InlineData("controller/result/error-type", "errorType")]
	[InlineData("minimal/result/error-type", "errorType")]
	[InlineData("controller/result/error-payload", "errorPayload")]
	[InlineData("minimal/result/error-payload", "errorPayload")]
	public async Task ResultMode_ProblemDetailsExtensions_UseAspNetJsonNamingPolicy(string url, string extensionName)
	{
		var cli = factory.CreateClient();
		var res = await cli.GetAsync(url, TestContext.Current.CancellationToken);
		var body = await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

		PrintVariable(url, false);
		PrintVariable(JsonNode.Parse(body)?.ToJsonString(JsonSerializerOptions.Formatted), false);
		var json = Assert.IsType<JsonObject>(JsonNode.Parse(body));
		Assert.True(json.ContainsKey(extensionName));
		Assert.False(json.ContainsKey($"{char.ToUpperInvariant(extensionName[0])}{extensionName[1..]}"));
	}
}
