#define XUNIT_NULLABLE

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

public class ResponseMediaTypesE2ETest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResponseMediaTypesE2ETest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	private async Task<(HttpClient client, JsonSerializerOptions jsonOptions, HttpResponseMessage message)> GetMessage(
		string prefix,
		ResponseOptions options,
		string path,
		HttpStatusCode expectedStatus)
	{
		// Primero re-inyecto las opciones de Responses creando una factoria ad-hoc
		var currentFactory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Configure<ResponseOptions>(o =>
		{
			o.SerializeFullResponses = options.SerializeFullResponses;
			o.SerializeErrorAsProblemDetails = options.SerializeErrorAsProblemDetails;
			o.StrictNone = options.StrictNone;
		})));

		var cli = currentFactory.CreateClient();
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
			//PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
		// Sin esto, la deteccion de ProblemDetails por media type en el cliente seria letra muerta.
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

		// Un payload en crudo no debe anunciarse como envelope.
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
		var res = await cli.GetAsync(url);
		var body = await res.Content.ReadAsStringAsync();

		PrintVariable(url, false);
		PrintVariable(JsonNode.Parse(body)?.ToJsonString(JsonSerializerOptions.Formatted), false);
		var json = Assert.IsType<JsonObject>(JsonNode.Parse(body));
		Assert.True(json.ContainsKey(extensionName));
		Assert.False(json.ContainsKey($"{char.ToUpperInvariant(extensionName[0])}{extensionName[1..]}"));
	}
}
