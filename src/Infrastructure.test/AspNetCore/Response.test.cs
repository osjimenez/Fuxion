using System.Net;
using System.Text.Json;
using Fuxion.AspNetCore.Service;
using Fuxion.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Fuxion.AspNetCore.Test;

public class ResponseTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResponseTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	async Task DoToApi(string prefix)
	{
		var cli = factory.CreateClient();
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
			//PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		// SUCCESS
		{
			var res = await cli.GetAsync($"{prefix}test-empty-success");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-message-success");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.OK, res.StatusCode);
			Assert.Equal("Success message", await res.Content.ReadAsStringAsync());
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-success");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.OK, res.StatusCode);
			var str = await res.Content.ReadAsStringAsync();
			var payload = str.DeserializeFromJson<TestPayload>(options: jsonOptions);
			Assert.Equal("Test name", payload?.FirstName);
			Assert.Equal(123, payload?.Age);
		}

		// ERROR
		{
			var res = await cli.GetAsync($"{prefix}test-message-error");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
			var str = await res.Content.ReadAsStringAsync();
			var problem = str.DeserializeFromJson<ProblemDetails>(options: jsonOptions);
			Assert.Equal("Error message", problem?.Detail);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-error");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
			var str = await res.Content.ReadAsStringAsync();
			var problem = str.DeserializeFromJson<ProblemDetails>(options: jsonOptions);
			Assert.Equal("Error message", problem?.Detail);
			var payload = ((JsonElement)problem?.Extensions["payload"]!).Deserialize<TestPayload>(jsonOptions);
			Assert.Equal("Test name", payload?.FirstName);
			Assert.Equal(123, payload?.Age);
		}

		// BAD REQUEST
		{
			var res = await cli.GetAsync($"{prefix}test-message-bad-request");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
			var str = await res.Content.ReadAsStringAsync();
			var problem = str.DeserializeFromJson<ProblemDetails>(options: jsonOptions);
			Assert.Equal("Error message", problem?.Detail);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-bad-request");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
			var str = await res.Content.ReadAsStringAsync();
			var problem = str.DeserializeFromJson<ProblemDetails>(options: jsonOptions);
			Assert.Equal("Error message", problem?.Detail);
			var payload = ((JsonElement)problem?.Extensions["payload"]!).Deserialize<TestPayload>(jsonOptions);
			Assert.Equal("Test name", payload?.FirstName);
			Assert.Equal(123, payload?.Age);
		}

		// EXCEPTION
		{
			var res = await cli.GetAsync($"{prefix}test-message-exception");
			PrintVariable(res.StatusCode);
			Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
			var str = await res.Content.ReadAsStringAsync();
			PrintVariable(str);
			var problem = str.DeserializeFromJson<ProblemDetails>(options: jsonOptions);
			var exception = (JsonElement)problem?.Extensions["exception"]!;
			Assert.Equal("Not implemented", exception.GetProperty("Message").GetString());
		}
	}
	[Fact]
	public async Task ToApiActionResult() => await DoToApi("controller/");
	[Fact]
	public async Task ToApiResult() => await DoToApi("endpoint-");

	async Task DoToResponse(string prefix)
	{
		var cli = factory.CreateClient();
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
			//PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		// SUCCESS
		{
			var res = await cli.GetAsync($"{prefix}test-empty-success")
				.AsResponseAsync();
			Assert.True(res.IsSuccess);
			Assert.Equal(204, res.Extensions["status-code"]);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-message-success")
				.AsResponseAsync();
			Assert.True(res.IsSuccess);
			Assert.Equal(200, res.Extensions["status-code"]);
			Assert.Equal("Success message", res.Message);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-success")
				.AsResponseAsync<TestPayload>(jsonOptions);
			Assert.True(res.IsSuccess);
			Assert.Equal(200, res.Extensions["status-code"]);
			Assert.Equal("Test name", res.Payload?.FirstName);
			Assert.Equal(123, res.Payload?.Age);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-success")
				.AsResponseAsync<DateTime>(jsonOptions);
			Assert.False(res.IsSuccess);
			Assert.Equal(default, res.Payload);
			Assert.Equal(ErrorType.InvalidData, res.ErrorType);
			Assert.Equal(200, res.Extensions["status-code"]);
			Assert.True(res.Extensions.ContainsKey("json-content"));
		}

		// ERROR
		{
			var res = await cli.GetAsync($"{prefix}test-message-error")
				.AsResponseAsync(jsonOptions);
			Assert.False(res.IsSuccess);
			Assert.Equal(500, res.Extensions["status-code"]);
			Assert.True(res.TryGetProblemDetails(out var problem));
			Assert.Equal("Error message", problem.Detail);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-error")
				.AsResponseAsync(jsonOptions);
			Assert.False(res.IsSuccess);
			Assert.Equal(500, res.Extensions["status-code"]);
			Assert.True(res.TryGetProblemDetails(out var problem));
			Assert.Equal("Error message", problem.Detail);
			Assert.True(problem.TryGetPayload<TestPayload>(out var payload, jsonOptions));
			Assert.Equal("Test name", payload.FirstName);
			Assert.Equal(123, payload.Age);
		}

		// BAD REQUEST
		{
			var res = await cli.GetAsync($"{prefix}test-message-bad-request")
				.AsResponseAsync<TestPayload>(jsonOptions);
			Assert.False(res.IsSuccess);
			Assert.Equal(400, res.Extensions["status-code"]);
			Assert.True(res.TryGetProblemDetails(out var problem));
			Assert.Equal("Error message", problem.Detail);
		}
		{
			var res = await cli.GetAsync($"{prefix}test-payload-bad-request")
				.AsResponseAsync<TestPayload>(jsonOptions);
			Assert.False(res.IsSuccess);
			Assert.Equal(400, res.Extensions["status-code"]);
			Assert.True(res.TryGetProblemDetails(out var problem));
			Assert.Equal("Error message", problem.Detail);
		}

		// EXCEPTION
		{
			var res = await cli.GetAsync($"{prefix}test-message-exception")
				.AsResponseAsync(jsonOptions);
			Assert.False(res.IsSuccess);
			Assert.Equal(500, res.Extensions["status-code"]);
			Assert.True(res.TryGetProblemDetails(out var problem));
			Assert.Equal("NotImplementedException: Not implemented", problem.Detail);
		}
	}
	[Fact]
	public async Task ToResponseAsync()
	{
		await DoToResponse("endpoint-");
		await DoToResponse("controller/");
	}
}