using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore;

public class FormsTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<FormsTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	[Theory(DisplayName = "Every union form, sync or wrapped in Task/ValueTask, maps its success to a plain payload")]
	[InlineData("response")] [InlineData("response-task")] [InlineData("response-valuetask")]
	[InlineData("typed")] [InlineData("typed-task")] [InlineData("typed-valuetask")]
	[InlineData("maybe")] [InlineData("maybe-task")] [InlineData("maybe-valuetask")]
	[InlineData("maybe-typed")] [InlineData("maybe-typed-task")] [InlineData("maybe-typed-valuetask")]
	public async Task AllForms_Success(string route)
	{
		var res = await factory.CreateClient().GetAsync($"minimal/forms/{route}", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		Assert.Equal("123", await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
		Assert.Contains("Accept", res.Headers.Vary);
	}

	[Fact(DisplayName = "None and a typed error survive the Task/ValueTask wrappers")]
	public async Task Wrapped_NoneAndTypedError()
	{
		var cli = factory.CreateClient();
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync("minimal/forms/maybe-typed-none-valuetask", TestContext.Current.CancellationToken)).StatusCode);
		var error = await cli.GetAsync("minimal/forms/typed-error-task", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
		Assert.Equal("business", (string?)JsonNode.Parse(await error.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["errorPayload"]);
	}
}
