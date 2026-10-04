using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Test.AspNetCore.Service;
using Test.Responses.Shared.Fixtures;
using Xunit;
using static Test.Responses.Shared.WireRequests;

namespace Test.AspNetCore;

// The base wire contract shared with Web API 2 lives in Test.Responses.Shared.WireContractTests.
// What stays here does not have a Web API 2 (or, for "unset", a controller)
// counterpart: options-driven native-error mode, Vary across several routes, the controller-attribute
// cascade demo, the "unset" fixture (minimal-only), and a snake_case-server typed-error recovery check.
public class AspNetCoreWireExtrasTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<AspNetCoreWireExtrasTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	HttpClient CreateClient(ResponseOptions? options = null)
	{
		if (options is null) return factory.CreateClient();
		return factory.CreateClient(options);
	}

	[Theory(DisplayName = "A native error uses its vendor media type with the naming parameter")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task NativeError_UsesVendorMediaType(string prefix)
	{
		var res = await CreateClient(new() { SerializeErrorAsProblemDetails = false }).GetAsync($"{prefix}/response/error-message", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.GetParameter(res.Content.Headers.ContentType?.ToString()));
	}

	[Theory(DisplayName = "Mapped responses vary by Accept so shared caches never mix shapes")]
	[InlineData("minimal/response/payload")]
	[InlineData("controller/response/payload")]
	[InlineData("minimal/response/none")]
	[InlineData("minimal/response/error-message")]
	public async Task MappedResponses_VaryByAccept(string url)
	{
		var res = await CreateClient().GetAsync(url, TestContext.Current.CancellationToken);
		Assert.Contains("Accept", res.Headers.Vary);
	}

	[Fact(DisplayName = "Controller and action attributes cascade")]
	public async Task Attributes_Cascade()
	{
		var cli = CreateClient();
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("attribute-test/payload", TestContext.Current.CancellationToken)).Content.Headers.ContentType?.MediaType);
		Assert.Equal("application/json", (await cli.GetAsync("attribute-test/payload-bare", TestContext.Current.CancellationToken)).Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "An uninitialized response follows Accept like any other error")]
	public async Task Unset_FollowsAccept()
	{
		var res = await CreateClient().SendAsync(Get("minimal/response/unset", "application/vnd.fuxion.error+json, application/json;q=0.9"), TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "A typed error from a snake_case server is still recoverable through problem+json")]
	public async Task TypedError_FromSnakeCaseServer_IsRecoverable()
	{
		var cli = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
			s.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower)))
			.CreateClient();

		var res = await cli.GetAsync("minimal/response/typed-error", TestContext.Current.CancellationToken);

		var raw = await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		IsTrue(JsonNode.Parse(raw)!.AsObject().ContainsKey("errorPayload"));

		var response = await cli.GetAsync("minimal/response/typed-error", TestContext.Current.CancellationToken).AsResponseAsync<string, TestBusinessError>(ct: TestContext.Current.CancellationToken);

		IsTrue(response.TryGetValue(out TestBusinessError? error));
		Assert.Equal(TestBusinessError.Default, error);
	}

	[Theory(DisplayName = "Extensions dropped by a response written without the envelope are reported as a warning")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task DroppedExtensions_LogAWarning(string prefix)
	{
		var logs = new CapturingLoggerProvider();
		var cli = factory.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(logs))).CreateClient();

		await cli.GetAsync($"{prefix}/response/payload", TestContext.Current.CancellationToken);
		Assert.DoesNotContain(logs.Entries, e => e.Category == "Fuxion.AspNetCore.Responses");

		var res = await cli.GetAsync($"{prefix}/response/payload-extended", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		var warning = Assert.Single(logs.Entries, e => e.Category == "Fuxion.AspNetCore.Responses");
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Contains("trace", warning.Message);
	}
}

file sealed class CapturingLoggerProvider : ILoggerProvider
{
	public ConcurrentQueue<(string Category, LogLevel Level, string Message)> Entries { get; } = new();

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

	public void Dispose() { }

	sealed class CapturingLogger(CapturingLoggerProvider owner, string category) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> owner.Entries.Enqueue((category, logLevel, formatter(state, exception)));
	}
}
