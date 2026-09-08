#define XUNIT_NULLABLE

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Net.Http;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;
using Test.Responses.Shared.Fixtures;
using Xunit;

namespace Test.AspNetCore;

public class ResponseMatrixTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResponseMatrixTest>(output), IClassFixture<WebApplicationFactory<Program>>
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
}
