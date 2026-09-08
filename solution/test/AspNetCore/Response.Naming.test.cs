using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.AspNetCore;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore;

// The echo/snake/kebab/dictionary/malformed/400 contract shared with Web API 2 lives in
// Test.Responses.Shared.RequestNamingTests. What stays here is minimal-API-only
// (raw group, 413, chunked bodies), controller-only structural checks, or a genuine per-host divergence
// ("Snake_WithoutParameter_IsNotBound": an [ApiController]'s implicit required-member validation turns a
// null FirstName into a 400, unlike here on the minimal side or on Web API 2).
public class RequestNamingTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<RequestNamingTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	static StringContent Body(string json, string? naming)
	{
		var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
		if (naming is not null)
			content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, naming));
		return content;
	}

	[Fact(DisplayName = "Without the parameter a snake_case body is not understood: the parameter is what does it")]
	public async Task Snake_WithoutParameter_IsNotBound()
	{
		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", Body("""{"first_name":"Ada","age":36}""", null));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Null((string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Fact(DisplayName = "A camelCase body with naming=camel is left untouched")]
	public async Task Camel_PassesThrough()
	{
		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", Body("""{"firstName":"Ada","age":36}""", ResponseNaming.Camel));
		Assert.Equal("Ada", (string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);
	}

	[Fact(DisplayName = "An endpoint without a declared body type receives the body untouched")]
	public async Task Minimal_RawEndpoint_Untouched()
	{
		var res = await factory.CreateClient().PostAsync("minimal/naming/raw", Body("""{"first_name":"Ada"}""", ResponseNaming.Snake));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.True((bool?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["hasSnake"]);
	}

	[Fact(DisplayName = "A snake body over the configured limit is a 413")]
	public async Task Minimal_OverLimit_Is413()
	{
		var padding = new string('a', 70 * 1024);
		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", Body($$"""{"first_name":"{{padding}}","age":36}""", ResponseNaming.Snake));

		Assert.Equal(HttpStatusCode.RequestEntityTooLarge, res.StatusCode);
	}

	[Fact(DisplayName = "A snake body without the naming parameter is never buffered nor limited")]
	public async Task Minimal_NoParameter_NotLimited()
	{
		var padding = new string('a', 70 * 1024);
		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", Body($$"""{"first_name":"{{padding}}","age":36}""", null));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
	}

	[Fact(DisplayName = "MVC registers exactly one System.Text.Json input formatter, and it is the per-request one")]
	public void Mvc_RegistersOurFormatterOnly()
	{
		var mvcOptions = factory.Services.GetRequiredService<IOptions<MvcOptions>>().Value;
		var jsonFormatters = mvcOptions.InputFormatters.OfType<SystemTextJsonInputFormatter>().ToList();

		Assert.Single(jsonFormatters);
		Assert.IsType<ResponseNamingInputFormatter>(jsonFormatters[0]);
	}

	[Fact(DisplayName = "A chunked (unknown length) snake body over the limit is still a 413")]
	public async Task Minimal_ChunkedOverLimit_Is413()
	{
		var padding = new string('a', 70 * 1024);
		var json = $$"""{"first_name":"{{padding}}","age":36}""";
		var content = new ChunkedContent(System.Text.Encoding.UTF8.GetBytes(json));
		content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, ResponseNaming.Snake));

		var res = await factory.CreateClient().PostAsync("minimal/naming/echo", content);

		Assert.Equal(HttpStatusCode.RequestEntityTooLarge, res.StatusCode);
	}

	[Fact(DisplayName = "A zero-length chunked body is handled the same with or without a naming parameter")]
	public async Task Controller_EmptyChunkedBody_MatchesStockBehavior()
	{
		static ChunkedContent EmptyContent(string? naming)
		{
			var content = new ChunkedContent([]);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			if (naming is not null)
				content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, naming));
			return content;
		}

		// The naming-aware path (our formatter) and the stock path (no naming parameter, base formatter) must
		// treat a genuinely empty chunked body (no Content-Length at all, not just Content-Length: 0) the same
		// way - here, both reject it with 400, since TestNamingPayload is a required [FromBody] parameter
		// (TreatEmptyInputAsDefaultValue is false for it).
		// Known divergence NOT exercised by this test: for an OPTIONAL body (TreatEmptyInputAsDefaultValue
		// true), the two paths would disagree on HOW they report the same empty body - the stock formatter
		// still surfaces a JsonException-based model error even though the body is legitimately absent, while
		// ours returns InputFormatterResult.NoValue() cleanly, letting the parameter default instead of failing.
		// That difference is invisible here because a required body already fails before it could matter.
		var withNaming = await factory.CreateClient().PostAsync("controller/naming/echo", EmptyContent(ResponseNaming.Snake));
		var withoutNaming = await factory.CreateClient().PostAsync("controller/naming/echo", EmptyContent(null));

		Assert.Equal(HttpStatusCode.BadRequest, withNaming.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, withoutNaming.StatusCode);
		Assert.Equal(withoutNaming.StatusCode, withNaming.StatusCode);
	}

	[Fact(DisplayName = "A controller action still binds a snake body correctly when controllers are mapped under a UseResponses() convention, and RequestNamingEndpoint skips it")]
	public async Task Controller_MappedUnderUseResponses_StillBindsSnake_AndIsSkippedByTheEndpointWrapper()
	{
		var res = await factory.CreateClient().PostAsync("controller/naming/consumes", Body("""{"first_name":"Ada","age":36}""", ResponseNaming.Snake));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("Ada", (string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);

		// RequestNamingApplied is internal (visible here via InternalsVisibleTo): a controller endpoint must
		// carry none of it, proving MVC's own formatter (not the minimal-API RequestDelegate wrapper) is what
		// bound the body above.
		var dataSources = factory.Services.GetServices<EndpointDataSource>();
		var endpoint = dataSources.SelectMany(s => s.Endpoints)
			.OfType<RouteEndpoint>()
			.Single(e => e.RoutePattern.RawText!.Contains("controller/naming/consumes", StringComparison.OrdinalIgnoreCase));
		Assert.Empty(endpoint.Metadata.OfType<RequestNamingApplied>());
	}

	[Fact(DisplayName = "PeekedByteStream replays its peeked byte, and a zero-length read does not consume it")]
	public async Task PeekedByteStream_ReplaysPeekedByte()
	{
		using var inner = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("BC"));
		var stream = new PeekedByteStream(inner, (byte)'A');

		// A zero-length read must not mark the peeked byte as served, or it would be silently dropped.
		var zeroLength = await stream.ReadAsync(Array.Empty<byte>(), 0, 0, default);
		Assert.Equal(0, zeroLength);

		using var buffer = new MemoryStream();
		var chunk = new byte[16];
		int read;
		while ((read = await stream.ReadAsync(chunk, 0, chunk.Length, default)) > 0)
			buffer.Write(chunk, 0, read);

		Assert.Equal("ABC", System.Text.Encoding.ASCII.GetString(buffer.ToArray()));
	}

	[Fact(DisplayName = "A nested group calling UseResponses() again does not double-wrap the naming support")]
	public async Task NestedGroup_NotDoubleWrapped()
	{
		// Functional side: the endpoint is reached through two UseResponses() call sites (the root group in
		// Program.cs and "special"'s own), and still binds a snake body correctly - not corrupted by a second pass.
		var res = await factory.CreateClient().PostAsync("minimal/special/naming-echo", Body("""{"first_name":"Ada","age":36}""", ResponseNaming.Snake));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal("Ada", (string?)JsonNode.Parse(await res.Content.ReadAsStringAsync())!["firstName"]);

		// Structural side: RequestNamingApplied is internal (visible here via InternalsVisibleTo), so the
		// endpoint's own metadata can be inspected directly to prove the wrapper was installed exactly once.
		var dataSources = factory.Services.GetServices<EndpointDataSource>();
		var endpoint = dataSources.SelectMany(s => s.Endpoints)
			.OfType<RouteEndpoint>()
			.Single(e => e.RoutePattern.RawText!.Contains("special/naming-echo", StringComparison.OrdinalIgnoreCase));
		Assert.Single(endpoint.Metadata.OfType<RequestNamingApplied>());
	}
}

/// <summary>An HttpContent whose length is unknowable up front, forcing a chunked request with no Content-Length -
/// so the minimal APIs naming support must discover the 413 while bounded-copying the body, not from the header.</summary>
file sealed class ChunkedContent(byte[] bytes) : HttpContent
{
	protected override bool TryComputeLength(out long length)
	{
		length = 0;
		return false;
	}

	protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
		=> stream.WriteAsync(bytes, 0, bytes.Length);
}
