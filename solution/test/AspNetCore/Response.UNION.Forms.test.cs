#define XUNIT_NULLABLE

using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

public class ResponseFormsTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<ResponseFormsTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	[Theory(DisplayName = "Every union form, sync or wrapped in Task/ValueTask, maps its success to a plain payload")]
	[InlineData("response")] [InlineData("response-task")] [InlineData("response-valuetask")]
	[InlineData("typed")] [InlineData("typed-task")] [InlineData("typed-valuetask")]
	[InlineData("maybe")] [InlineData("maybe-task")] [InlineData("maybe-valuetask")]
	[InlineData("maybe-typed")] [InlineData("maybe-typed-task")] [InlineData("maybe-typed-valuetask")]
	public async Task AllForms_Success(string route)
	{
		var res = await factory.CreateClient().GetAsync($"minimal/forms/{route}");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		Assert.Equal("123", await res.Content.ReadAsStringAsync());
		Assert.Contains("Accept", res.Headers.Vary);
	}

	[Fact(DisplayName = "None and a typed error survive the Task/ValueTask wrappers")]
	public async Task Wrapped_NoneAndTypedError()
	{
		var cli = factory.CreateClient();
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync("minimal/forms/maybe-typed-none-valuetask")).StatusCode);
		var error = await cli.GetAsync("minimal/forms/typed-error-task");
		Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
		Assert.Equal("business", (string?)JsonNode.Parse(await error.Content.ReadAsStringAsync())!["errorPayload"]);
	}
}
