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

// What only an end-to-end run covers: the result/ group (ToResult, ToActionResult, Unit.Result, None.ActionResult) on both
// hosts, and an Error carrying an exception crossing the wire. Every flag combination of the mapping itself is pinned by
// ResponseWireMapperTest, and the response/ shapes by the shared WireContractTests.
public class ResultExtensionsTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResultExtensionsTest>(output), IClassFixture<WebApplicationFactory<Program>>
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
		// UNIT: always 200 with an empty body, so it is never mistaken for None
		await CallEndpoint<Unit>(prefix, options, "unit", HttpStatusCode.OK);
		// NONE: 204 unless the full envelope can carry it
		await CallEndpoint<None>(prefix, options, "none",
			!options.StrictNone && options.SerializeFullResponses
				? HttpStatusCode.OK
				: HttpStatusCode.NoContent);
		// STRING
		await CallEndpoint<string>(prefix, options, "string", HttpStatusCode.OK, assertValue: (value, _) => { Assert.Equal("test", value); });
		// PAYLOAD
		await CallEndpoint<TestPayload>(prefix, options, "payload", HttpStatusCode.OK, assertValue: (value, _) =>
		{
			Assert.Equal("test", value.Name);
			Assert.Equal(123, value.Age);
		});
		// ERROR - MESSAGE
		await CallEndpoint<Error>(prefix, options, "error-message", HttpStatusCode.InternalServerError, "test", (value, _) => { Assert.Equal("test", value.Message); });
		// ERROR - TYPE
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
		await CallErrorException(prefix, options);
	}

	Task CallErrorException(string prefix, ResponseOptions options)
		=> CallEndpoint<Error>(prefix, options, "error-exception", HttpStatusCode.InternalServerError, null, (value, _) =>
		{
			var remote = Assert.IsType<RemoteException>(value.Exception);
			Assert.Equal(nameof(NotImplementedException), remote.RemoteType);
		});

	public static TheoryData<string, bool, bool, bool> Hosts() => new()
	{
		// Defaults, then a combination where every flag differs from them.
		{ "minimal", false, true, false },
		{ "minimal", true, false, true },
		{ "controller", false, true, false },
		{ "controller", true, false, true },
	};

	[Theory(DisplayName = "ToResult, ToActionResult, Unit.Result and None.ActionResult produce the same wire as a returned union")]
	[MemberData(nameof(Hosts))]
	public async Task ResultExtensions_MatchTheReturnedUnionWire(string prefix, bool fullResponses, bool errorAsProblem, bool strictNone)
		=> await CallService(prefix + "/result/", new() { SerializeFullResponses = fullResponses, SerializeErrorAsProblemDetails = errorAsProblem, StrictNone = strictNone });

	[Theory(DisplayName = "An error carrying an exception travels as a RemoteException")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task ErrorException_TravelsAsRemoteException(string prefix)
		=> await CallErrorException(prefix + "/response/", new ResponseOptions());
}
