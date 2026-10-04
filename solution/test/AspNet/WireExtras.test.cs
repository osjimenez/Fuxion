using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Tracing;
using Fuxion;
using Fuxion.AspNet;
using Fuxion.Xunit;
using Xunit;
using static Test.Responses.Shared.WireRequests;

namespace Test.AspNet;

// Web API 2 specifics that have no ASP.NET Core equivalent to share through Test.Responses.Shared:
// the scope-attribute cascade, void/Task actions, async union actions and UseResponses' single-use guard.
// The rest of the wire contract lives in Test.Responses.Shared.WireContractTests.
public class WebApi2WireExtrasTest(ITestOutputHelper output) : BaseTest<WebApi2WireExtrasTest>(output)
{
	[Fact(DisplayName = "The scope cascade is global, then controller attribute, then action attribute")]
	public async Task Attributes_Cascade()
	{
		var cli = AspNetHost.Create();
		Assert.Equal(ResponseMediaTypes.ResponseJson, (await cli.GetAsync("attribute-test/payload", TestContext.Current.CancellationToken)).Content.Headers.ContentType?.MediaType);
		Assert.Equal("application/json", (await cli.GetAsync("attribute-test/payload-bare", TestContext.Current.CancellationToken)).Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "An uninitialized response is a 500 problem")]
	public async Task Unset_Is500()
	{
		var res = await AspNetHost.Create().GetAsync("response/unset", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "An uninitialized response follows Accept like any other error")]
	public async Task Unset_FollowsAccept()
	{
		var res = await AspNetHost.Create().SendAsync(Get("response/unset", "application/vnd.fuxion.error+json, application/json;q=0.9"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
		Assert.Equal(ResponseMediaTypes.ErrorJson, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "Endpoints that do not declare a union type are never touched")]
	public async Task NonUnion_IsUntouched()
	{
		var res = await AspNetHost.Create().GetAsync("plain/list", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		IsFalse(res.Headers.Vary.Any());
		Assert.Equal("""["a","b"]""", await res.Content.ReadAsStringAsync());
	}

	// ReflectedHttpActionDescriptor.ReturnType is null for void and non-generic Task actions: the filter
	// must not crash on a null declared return type and must leave the framework's own 204 alone.
	[Theory(DisplayName = "void and Task actions keep the framework's 204")]
	[InlineData("plain/void")]
	[InlineData("plain/task")]
	public async Task VoidAndTask_Keep204(string url)
	{
		var res = await AspNetHost.Create().GetAsync(url, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
	}

	[Fact(DisplayName = "An async union action is mapped like a sync one")]
	public async Task AsyncAction_IsMappedLikeSync()
	{
		var res = await AspNetHost.Create().GetAsync("response/async-payload", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		Assert.Contains("Accept", res.Headers.Vary);
		var body = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
		Assert.Equal("test", (string?)body["name"]);
	}

	[Fact(DisplayName = "UseResponses can only be applied once")]
	public void UseResponses_CanOnlyBeAppliedOnce()
	{
		using var config = new HttpConfiguration();
		config.UseResponses();
		Assert.Throws<InvalidOperationException>(() => config.UseResponses());
	}

	[Fact(DisplayName = "Extensions dropped by a response written without the envelope are traced as a warning")]
	public async Task DroppedExtensions_TraceAWarning()
	{
		var traces = new CapturingTraceWriter();
		var cli = AspNetHost.Create(traceWriter: traces);

		await cli.GetAsync("response/payload", TestContext.Current.CancellationToken);
		Assert.DoesNotContain(traces.Records, r => r.Category == "Fuxion.Responses");

		var res = await cli.GetAsync("response/payload-extended", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
		var warning = Assert.Single(traces.Records, r => r.Category == "Fuxion.Responses");
		Assert.Equal(TraceLevel.Warn, warning.Level);
		Assert.Contains("trace", warning.Message);
	}
}

file sealed class CapturingTraceWriter : ITraceWriter
{
	public ConcurrentQueue<TraceRecord> Records { get; } = new();

	public void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
	{
		var record = new TraceRecord(request, category, level);
		traceAction(record);
		Records.Enqueue(record);
	}
}
