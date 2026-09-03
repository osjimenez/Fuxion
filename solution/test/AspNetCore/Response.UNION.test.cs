#define XUNIT_NULLABLE

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.AspNetCore;
using Fuxion.Text.Json;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

public class ResponseTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResponseTest>(output), IClassFixture<WebApplicationFactory<Program>>
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
	private async Task CallEndpoint<TValue>(
		string prefix,
		ResponseOptions options,
		string path,
		HttpStatusCode expectedStatus,
		string? expectedErrorMessage = null,
		Action<TValue, JsonSerializerOptions>? assertValue = null)
		where TValue : notnull
	{
		var (cli, jsonOptions, res) = await GetMessage(prefix, options, path, expectedStatus);

		if(typeof(TValue) == typeof(Unit) || typeof(TValue) == typeof(None) || typeof(TValue) == typeof(Error))
			DoUnitResponse(await res.AsResponseAsync<Unit>(jsonOptions));
		else
			DoValueResponse(await res.AsResponseAsync<TValue>(jsonOptions));
		return;

		void DoUnitResponse(ResponseMaybe<Unit> response){
			if (response.TryGetValue(out Error error))
			{
				Assert.Equal(expectedErrorMessage, error.Message);
				PrintVariable(error.Message);
				if (assertValue is not null && error is TValue value)
					assertValue(value, jsonOptions);
			}
			else
			{
				IsTrue(response is TValue);
				// Unit y None son semanticamente distintos y deben viajar como tales
				if (typeof(TValue) == typeof(Unit))
				{
					IsTrue(response.TryGetValue(out Unit _));
					IsTrue(response is not None);
				}
				else if (typeof(TValue) == typeof(None))
				{
					IsTrue(response.TryGetValue(out None _));
					IsTrue(response is not Unit);
				}

				if (assertValue is not null && response.TryGetValue(out Unit unit) && unit is TValue value)
					assertValue(value, jsonOptions);
			}
		}
		void DoValueResponse(ResponseMaybe<TValue> response)
		{
			if (response.TryGetValue(out Error error))
			{
				Assert.Equal(expectedErrorMessage, error.Message);
				PrintVariable(error.Message);
				if (assertValue is not null && error is TValue value)
					assertValue(value, jsonOptions);
			}
			else
			{
				IsTrue(response is TValue);
				if (assertValue is not null && response.TryGetValue(out TValue? success) && success is TValue value)
					assertValue(value, jsonOptions);
			}
		}
	}

	private async Task CallService(string prefix, ResponseOptions options)
	{
		// UNIT: siempre 200 con cuerpo vacio, para no confundirse con None
		await CallEndpoint<Unit>(prefix, options, "unit", HttpStatusCode.OK);
		// NONE: 204 salvo que el envelope completo pueda transportarlo
		await CallEndpoint<None>(prefix, options, "none",
			!options.StrictNone && options.SerializeFullResponses
				? HttpStatusCode.OK
				: HttpStatusCode.NoContent);
		// STRING
		await CallEndpoint<string>(prefix, options, "string", HttpStatusCode.OK, assertValue: (value, _) => { Assert.Equal("test", value); });
		// PAYLOAD
		//await CallEndpoint<TestPayload, TestPayload>(prefix, mode, "payload", HttpStatusCode.OK, assertValue: (value, _) =>
		await CallEndpoint<TestPayload>(prefix, options, "payload", HttpStatusCode.OK, assertValue: (value, _) =>
		{
			Assert.Equal("test", value.Name);
			Assert.Equal(123, value.Age);
		});
		// ERROR - MESSAGE
		await CallEndpoint<Error>(prefix, options, "error-message", HttpStatusCode.InternalServerError, "test", (value, _) => { Assert.Equal("test", value.Message); });
		// ERROR - TYPE
		//await CallEndpoint<Unit, Error>(prefix, mode, "error-type", HttpStatusCode.NotImplemented, null, (value, _) =>
		await CallEndpoint<Error>(prefix, options, "error-type", HttpStatusCode.NotImplemented, null, (value, _) =>
		{
			PrintVariable(value.IsNotImplemented);
			PrintVariable(value.Type is HttpStatusCode);
			PrintVariable(value.Type);
			IsTrue(value.Type is HttpStatusCode.NotImplemented);
		});
		// ERROR - PAYLOAD
		await CallEndpoint<Error>(prefix, options, "error-payload", HttpStatusCode.InternalServerError, null, (value, jsonOptions) =>
		{
			var payload = value.GetPayloadAs<TestPayload>(jsonOptions);
			Assert.NotNull(payload);
			Assert.Equal(TestPayload.Default.Name, payload.Name);
			Assert.Equal(TestPayload.Default.Age, payload.Age);
		});
		// ERROR - EXCEPTION
		await CallEndpoint<Error>(prefix, options, "error-exception", HttpStatusCode.InternalServerError, null, (value, _) =>
		{
			// PEND ver que hacemos con las excepciones ...
			IsTrue(value.Exception is RemoteException);
			if (value.Exception is RemoteException rex) Assert.Equal(nameof(NotImplementedException), rex.RemoteType);
		});
	}

	//[Theory]
	//[InlineData(true, true, false)]
	//public async Task ToApiActionResult(bool fullResponse, bool errorAsProblem, bool strictNone)
	//{
	//	await CallService("controller/response/", ResponseHttpMode.Response);
	//	await CallService("controller/result/", ResponseHttpMode.Result);
	//}
	[Theory(DisplayName = "ToApiResult")]
	[InlineData("minimal", true, true, false)]
	[InlineData("minimal", true, false, false)]
	[InlineData("minimal", false, true, false)]
	[InlineData("minimal", false, false, false)]
	[InlineData("minimal", true, true, true)]
	[InlineData("minimal", true, false, true)]
	[InlineData("minimal", false, true, true)]
	[InlineData("minimal", false, false, true)]

	[InlineData("controller", true, true, false)]
	[InlineData("controller", true, false, false)]
	[InlineData("controller", false, true, false)]
	[InlineData("controller", false, false, false)]
	[InlineData("controller", true, true, true)]
	[InlineData("controller", true, false, true)]
	[InlineData("controller", false, true, true)]
	[InlineData("controller", false, false, true)]
	public async Task ToApiResult(string prefix, bool fullResponse,bool errorAsProblem,bool strictNone)
	{
		ResponseOptions options = new()
		{
			SerializeFullResponses = fullResponse,
			SerializeErrorAsProblemDetails = errorAsProblem,
			StrictNone = strictNone
		};
		await CallService(prefix + "/response/", options);
		await CallService(prefix + "/result/", options);
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

	[Fact(DisplayName = "A problem+json error is parsed even if options did not expect it")]
	public async Task ProblemDetailsBody_ParsedWhenOptionsDisabled()
	{
		// El servidor responde ProblemDetails aunque el cliente no lo esperaba: el media type
		// y la cabecera lo anuncian, asi que el error debe leerse igualmente.
		var message = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		{
			Content = new StringContent(
				"""{"status":500,"title":"Internal server error","detail":"boom"}""",
				System.Text.Encoding.UTF8,
				"application/problem+json")
		};
		var response = await message.AsResponseAsync<TestPayload>();
		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal("boom", error.Message);
	}

	[Theory(DisplayName = "A server that announces nothing still resolves Unit and None by status")]
	[InlineData(HttpStatusCode.NoContent, false)]
	[InlineData(HttpStatusCode.OK, true)]
	public async Task LegacyServer_WithoutMediaTypes_ResolvesUnitAndNoneByStatus(HttpStatusCode status, bool expectsUnit)
	{
		// Sin cabecera solo queda el status: 204 es None y cualquier otro exito sin cuerpo es Unit.
		var message = new HttpResponseMessage(status)
		{
			Content = new StringContent("")
		};

		var response = await message.AsResponseAsync<Unit>();
		if (expectsUnit)
		{
			IsTrue(response.TryGetValue(out Unit _));
			IsTrue(response is not None);
		}
		else
		{
			IsTrue(response.TryGetValue(out None _));
			IsTrue(response is not Unit);
		}
	}

	[Fact(DisplayName = "Subgroup override wins over global options")]
	public async Task SubgroupOverride_ForcesFullResponseEnvelope()
	{
		// Las opciones globales piden payload en crudo, pero el subgrupo fuerza el envelope completo.
		var currentFactory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Configure<ResponseOptions>(o =>
		{
			o.SerializeFullResponses = false;
			o.SerializeErrorAsProblemDetails = true;
			o.StrictNone = false;
		})));

		var cli = currentFactory.CreateClient();
		var res = await cli.GetAsync("minimal/special/payload");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);

		var body = await res.Content.ReadAsStringAsync();
		PrintVariable(JsonNode.Parse(body)?.ToJsonString(JsonSerializerOptions.Formatted), false);

		// El envelope completo expone el payload anidado, no en la raiz.
		var json = Assert.IsType<JsonObject>(JsonNode.Parse(body));
		IsTrue(json.ContainsKey("payload"));
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

	private async Task DoToResponse(string prefix)
	{
		var cli = factory.CreateClient();
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
			//PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		// SUCCESS
		//{
		//	var res = await cli.GetAsync($"{prefix}test-empty-success")
		//		.AsResponseAsync();
		//	PrintVariable(res.Fx.Json.Serialize(true).Payload);
		//	Assert.True(res.IsSuccess);
		//	Assert.Equal(204, res.Extensions.StatusCode.Value);
		//}
		//{
		//	var res = await cli.GetAsync($"{prefix}test-message-success")
		//		.AsResponseAsync();
		//	PrintVariable(res.Fx.Json.Serialize(true).Payload);
		//	Assert.True(res.IsSuccess);
		//	Assert.Equal(200, res.Extensions.StatusCode.Value);
		//	Assert.Equal("Success message", res.Message);
		//}
		//{
		//	var res = await cli.GetAsync($"{prefix}test-payload-success")
		//		.AsResponseAsync<TestPayload>(jsonOptions);
		//	PrintVariable(res.Fx.Json.Serialize(true).Payload);
		//	Assert.True(res.IsSuccess);
		//	Assert.Equal(200, res.Extensions.StatusCode.Value);
		//	Assert.Equal("Test name", res.Payload?.FirstName);
		//	Assert.Equal(123, res.Payload?.Age);
		//}

		//// ERROR
		//{
		//	var res = await cli.GetAsync($"{prefix}test-message-error")
		//		.AsResponseAsync(jsonOptions);
		//	Assert.False(res.IsSuccess);
		//	Assert.Equal(500, res.Extensions.StatusCode.Value);
		//	Assert.NotNull(res.Extensions.InnerProblem);
		//	Assert.True(res.Extensions.InnerProblem.IsDefined);
		//	Assert.Equal(500, res.Extensions.InnerProblem.Value.Status);
		//	Assert.Equal("Error message", res.Extensions.InnerProblem.Value.Detail);
		//}
		//{
		//	var res = await cli.GetAsync($"{prefix}test-payload-error")
		//		.AsResponseAsync(jsonOptions);
		//	Assert.False(res.IsSuccess);
		//	Assert.Equal(500, res.Extensions.StatusCode.Value);
		//	Assert.NotNull(res.Extensions.InnerProblem);
		//	Assert.True(res.Extensions.InnerProblem.IsDefined);
		//	Assert.Equal(500, res.Extensions.InnerProblem.Value.Status);
		//	Assert.Equal("Error message", res.Extensions.InnerProblem.Value.Detail);
		//	Assert.True(res.Extensions.InnerProblem.Value.TryGetPayload<TestPayload>(out var payload, jsonOptions));
		//	Assert.Equal("Test name", payload.FirstName);
		//	Assert.Equal(123, payload.Age);
		//}

		//// BAD REQUEST
		//{
		//	var res = await cli.GetAsync($"{prefix}test-message-bad-request")
		//		.AsResponseAsync<TestPayload>(jsonOptions);
		//	Assert.False(res.IsSuccess);
		//	Assert.Equal(400, res.Extensions.StatusCode.Value);
		//	Assert.NotNull(res.Extensions.InnerProblem);
		//	Assert.True(res.Extensions.InnerProblem.IsDefined);
		//	Assert.Equal(400, res.Extensions.InnerProblem.Value.Status);
		//	Assert.Equal("Error message", res.Extensions.InnerProblem.Value.Detail);
		//}
		//{
		//	var res = await cli.GetAsync($"{prefix}test-payload-bad-request")
		//		.AsResponseAsync<TestPayload>(jsonOptions);
		//	Assert.False(res.IsSuccess);
		//	Assert.Equal(400, res.Extensions.StatusCode.Value);
		//	Assert.NotNull(res.Extensions.InnerProblem);
		//	Assert.True(res.Extensions.InnerProblem.IsDefined);
		//	Assert.Equal(400, res.Extensions.InnerProblem.Value.Status);
		//	Assert.Equal("Error message", res.Extensions.InnerProblem.Value.Detail);
		//}

		//// EXCEPTION
		//{
		//	var res = await cli.GetAsync($"{prefix}test-message-exception")
		//		.AsResponseAsync(jsonOptions);

		//	PrintVariable(res.Fx.Json.Serialize(true).Payload);
		//	Assert.False(res.IsSuccess);
		//	Assert.Equal(500, res.Extensions.StatusCode.Value);
		//	Assert.NotNull(res.Extensions.InnerProblem);
		//	Assert.True(res.Extensions.InnerProblem.IsDefined);
		//	Assert.Equal(500, res.Extensions.InnerProblem.Value.Status);
		//	Assert.Equal("NotImplementedException: Not implemented", res.Extensions.InnerProblem.Value.Detail);
		//}
	}

	[Fact]
	public async Task ToResponseAsync()
	{
		await DoToResponse("endpoint-");
		await DoToResponse("controller/");
	}

	[Theory(DisplayName = "An undefined member is omitted from the endpoint payload")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task UndefinedMember_IsOmittedFromPayload(string prefix)
	{
		var cli = factory.CreateClient();

		var res = await cli.GetAsync($"{prefix}/undefinable/partial");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);

		var body = await res.Content.ReadAsStringAsync();
		PrintVariable(body);

		// The raw body is asserted on purpose: deserializing would yield an undefined value both when the
		// property is omitted and when the marker object is used, so it could not tell them apart.
		var json = JsonNode.Parse(body)!.AsObject();
		Assert.False(json.ContainsKey("name"), "The undefined member must be absent from the payload");
		Assert.DoesNotContain(global::Fuxion.Union.UndefinableConverterFactory.UndefinedMarkerPropertyName, body);
		Assert.True(json.ContainsKey("age"));
		Assert.Equal(123, (int)json["age"]!);
	}

	[Theory(DisplayName = "An absent member is bound as undefined")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task AbsentMember_IsBoundAsUndefined(string prefix)
	{
		var cli = factory.CreateClient();

		var res = await cli.PostAsync(
			$"{prefix}/undefinable/echo",
			new StringContent("""{ "age": 7 }""", System.Text.Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);

		var body = await res.Content.ReadAsStringAsync();
		PrintVariable(body);

		var json = JsonNode.Parse(body)!.AsObject();
		Assert.False((bool)json["nameDefined"]!, "An absent member must be bound as undefined");
		Assert.True((bool)json["ageDefined"]!);
	}

	[Fact(DisplayName = "An already cancelled token stops the body read")]
	public async Task CancellationToken_IsHonoured()
	{
		// El cuerpo no se llega a leer: el token ya esta cancelado antes de empezar.
		var message = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(
				"""{"name":"test","age":1}""",
				System.Text.Encoding.UTF8,
				"application/json")
		};

		using var cts = new System.Threading.CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			async () => await message.AsResponseAsync<TestPayload>(ct: cts.Token));
	}

	[Fact(DisplayName = "The envelope media type is detected even if the options did not expect it")]
	public async Task FullResponseBody_ParsedWhenOptionsDisabled()
	{
		// Simetrico a ProblemDetails: el servidor anuncia el sobre con su media type, asi que
		// debe leerse como sobre aunque el cliente no lo esperase.
		var message = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(
				"""{"isSuccess":true,"isNone":false,"payload":{"name":"envelope","age":42}}""",
				System.Text.Encoding.UTF8,
				ResponseMediaTypes.ResponseJson)
		};

		// Sin jsonOptions explicitas: el default del cliente debe alinearse con el del servidor.
		var response = await message.AsResponseAsync<TestPayload>();

		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.NotNull(payload);
		Assert.Equal("envelope", payload!.Name);
	}

	[Fact(DisplayName = "A camelCase envelope from the server is read with the client default options")]
	public async Task CamelCaseEnvelope_IsReadWithDefaultOptions()
	{
		// El servidor (ASP.NET Core) siempre serializa el sobre con JsonSerializerDefaults.Web,
		// asi que el default del cliente debe resolver los nombres en camelCase sin configuracion.
		var message = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(
				"""{"isSuccess":true,"isNone":false,"payload":{"name":"web","age":7}}""",
				System.Text.Encoding.UTF8,
				ResponseMediaTypes.ResponseJson)
		};

		var response = await message.AsResponseAsync<TestPayload>();

		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.NotNull(payload);
		Assert.Equal("web", payload!.Name);
		Assert.Equal(7, payload.Age);
	}

	[Fact(DisplayName = "A malformed body is reported as an error instead of corrupting the payload")]
	public async Task MalformedBody_IsReportedAsError()
	{
		// JSON truncado: no puede deserializarse y el overload de un genérico nunca lanza.
		var message = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(
				"""{"name":"broken",""",
				System.Text.Encoding.UTF8,
				"application/json")
		};

		var response = await message.AsResponseAsync<TestPayload>();

		IsTrue(response.IsError);
		IsTrue(!response.TryGetValue(out TestPayload? _));
	}

	[Fact(DisplayName = "A custom error type is deserialized on a failed status code")]
	public async Task CustomErrorType_IsDeserialized()
	{
		// Con un TError propio el error viaja como ese tipo, no como Error.
		var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
		{
			Content = new StringContent(
				"""{"code":"invalid_name","reason":"too short"}""",
				System.Text.Encoding.UTF8,
				"application/json")
		};

		var response = await message.AsResponseAsync<TestPayload, CustomError>(
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		IsTrue(response.TryGetValue(out CustomError? customError));
		Assert.NotNull(customError);
		Assert.Equal("invalid_name", customError!.Code);
		Assert.Equal("too short", customError.Reason);
	}

	public record CustomError(string Code, string Reason);
}