#define XUNIT_NULLABLE

using Fuxion;
using Fuxion.AspNetCore;
using Fuxion.Reflection;
using Fuxion.Text.Json;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

public class ResponseTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResponseTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
   async Task<(HttpClient client, JsonSerializerOptions jsonOptions, HttpResponseMessage message)> GetMessage(string prefix, ResponseHttpMode mode, string path, HttpStatusCode expectedStatus)
   {
      var cli = factory.CreateClient();
      var jsonOptions = new JsonSerializerOptions
      {
         PropertyNameCaseInsensitive = true
         //PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      var url = $"{prefix}{path}";
      PrintVariable($" - {mode} - {url}", false);
      var res = await cli.GetAsync(url);
      PrintVariable(res.StatusCode);
      Assert.Equal(expectedStatus, res.StatusCode);

      var body = await res.Content.ReadAsStringAsync();
      if (body.IsNeitherNullNorWhiteSpace())
         PrintVariable(JsonNode.Parse(body)?.ToJsonString(JsonSerializerOptions.Formatted), false);

      return (cli, jsonOptions, res);
   }
   async Task CallEndpoint<TSuccess, TValue>(string prefix, ResponseHttpMode mode, string path, HttpStatusCode expectedStatus, Action<TValue, JsonSerializerOptions>? assertValue = null)
      where TSuccess : notnull
   {
      (var cli, var jsonOptions, var res) = await GetMessage(prefix, mode, path, expectedStatus);

      var response = await res.AsResponseAsync<TSuccess>(mode: mode, jsonOptions: jsonOptions);
      IsTrue(response is not null);
      if (response is Error error)
         PrintVariable(error.Message);
      IsTrue(response is TValue);
      if (assertValue is not null && response is TValue value)
         assertValue(value, jsonOptions);
   }
   async Task CallEndpoint<TSuccess, TError, TValue>(string prefix, ResponseHttpMode mode, string path, HttpStatusCode expectedStatus, Action<TValue, JsonSerializerOptions>? assertValue = null)
      where TSuccess : notnull
      where TError : notnull
   {
      (var cli, var jsonOptions, var res) = await GetMessage(prefix, mode, path, expectedStatus);

      var response = await res.AsResponseAsync<TSuccess, TError>(mode: mode, jsonOptions: jsonOptions);
      IsTrue(response is not null);
      if (response is Error error)
         PrintVariable(error.Message);
      IsTrue(response is TValue);
      if (assertValue is not null && response is TValue value)
         assertValue(value, jsonOptions);
   }
   async Task CallService(string prefix, ResponseHttpMode mode)
	{
      //// UNIT
      //await CallEndpoint<Unit, Unit>(prefix, mode, "unit", HttpStatusCode.OK);
      //// NONE
      //await CallEndpoint<Unit, None>(prefix, mode, "none", HttpStatusCode.NoContent);
      //// STRING
      //await CallEndpoint<string, string>(prefix, mode, "string", HttpStatusCode.OK, (value, jsonOptions) =>
      //{
      //   Assert.Equal("test", value);
      //});
      //// PAYLOAD
      //await CallEndpoint<TestPayload, TestPayload>(prefix, mode, "payload", HttpStatusCode.OK, (value, jsonOptions) =>
      //{
      //   Assert.Equal("test", value.Name);
      //   Assert.Equal(123, value.Age);
      //});
      //// ERROR - MESSAGE
      //await CallEndpoint<Unit, Error>(prefix, mode, "error-message", HttpStatusCode.InternalServerError, (value, jsonOptions) =>
      //{
      //   Assert.Equal("test", value.Message);
      //});
      // ERROR - TYPE
      await CallEndpoint<Unit, Error>(prefix, mode, "error-type", HttpStatusCode.NotImplemented, (value, jsonOptions) =>
      {
         PrintVariable(value.IsNotImplemented);
         PrintVariable(value.Type is HttpStatusCode);
         PrintVariable(value.Type);
         IsTrue(value.Type is HttpStatusCode status && status == HttpStatusCode.NotImplemented);
      });
      // ERROR - PAYLOAD
      await CallEndpoint<Unit, Error>(prefix, mode, "error-payload", HttpStatusCode.InternalServerError, (value, jsonOptions) =>
      {
         var payload = value.GetPayloadAs<TestPayload>(jsonOptions);
         Assert.NotNull(payload);
         Assert.Equal(TestPayload.Default.Name, payload.Name);
         Assert.Equal(TestPayload.Default.Age, payload.Age);
      });
      // ERROR - EXCEPTION
      await CallEndpoint<Unit, Error>(prefix, mode, "error-exception", HttpStatusCode.InternalServerError, (value, jsonOptions) =>
      {
         // PEND ver que hacemos con las excepciones ...
         IsTrue(value.Exception is RemoteException);
         if(value.Exception is RemoteException rex)
         {
            Assert.Equal(nameof(NotImplementedException), rex.RemoteType);
         }
      });
   }
	[Fact]
	public async Task ToApiActionResult()
	{
		await CallService("controller/response/", ResponseHttpMode.Response);
      await CallService("controller/result/", ResponseHttpMode.Result);
   }
	[Fact]
	public async Task ToApiResult()
	{
		//await CallService("minimal/response/", ResponseHttpMode.Response);
      await CallService("minimal/result/", ResponseHttpMode.Result);
   }

	async Task DoToResponse(string prefix)
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