using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.AspNetCore.Service;
using Test.Responses.Shared.Fixtures;
using Xunit;

namespace Test.AspNetCore;

// The base binary wire contract shared with Web API 2 lives in Test.Responses.Shared.BinaryWireTests.
// If-None-Match/304 has no Web API 2 counterpart yet, so it stays here.
public class BinaryWireTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<BinaryWireTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	static HttpRequestMessage Get(string url, string? accept = null, string? range = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
		return request;
	}

	[Fact(DisplayName = "A matching If-None-Match yields 304")]
	public async Task IfNoneMatch_Yields304()
	{
		var request = Get("minimal/binary/file");
		request.Headers.TryAddWithoutValidation("If-None-Match", "\"v1\"");
		var res = await factory.CreateClient().SendAsync(request);

		Assert.Equal(HttpStatusCode.NotModified, res.StatusCode);
		Assert.Empty(await res.Content.ReadAsByteArrayAsync());
	}

	[Fact(DisplayName = "A 304 from a conditional GET is read by the client as an error typed NotModified")]
	public async Task IfNoneMatch_ClientReads304()
	{
		var request = Get("minimal/binary/file");
		request.Headers.TryAddWithoutValidation("If-None-Match", TestFile.ETag);
		var response = await factory.CreateClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead).AsResponseAsync<FileContent>();
		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.NotModified, error.Type);
	}
}

// AsResponseAsync<FileContent> is covered for both prefixes by Test.Responses.Shared.ClientTests.Client_ReadsFile;
// stream/byte[]/none/error through the client have no shared coverage yet, so they stay here.
public class BinaryClientTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<BinaryClientTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	[Theory(DisplayName = "Stream and byte[] round-trip through the client")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Client_ReadsStreamAndBytes(string prefix)
	{
		var streamResponse = await factory.CreateClient().GetAsync($"{prefix}/binary/stream").AsResponseAsync<System.IO.Stream>();
		IsTrue(streamResponse.TryGetValue(out System.IO.Stream? stream));
		using var memory = new System.IO.MemoryStream();
		await stream!.CopyToAsync(memory);
		Assert.Equal(TestFile.Bytes, memory.ToArray());

		var bytesResponse = await factory.CreateClient().GetAsync($"{prefix}/binary/bytes").AsResponseAsync<byte[]>();
		IsTrue(bytesResponse.TryGetValue(out byte[]? bytes));
		Assert.Equal(TestFile.Bytes, bytes);
	}

	[Theory(DisplayName = "None and errors of a binary endpoint are read normally by the client")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Client_NoneAndError(string prefix)
	{
		var none = await factory.CreateClient().GetAsync($"{prefix}/binary/none").AsResponseAsync<FileContent>();
		IsTrue(none.IsNone);

		var error = await factory.CreateClient().GetAsync($"{prefix}/binary/error").AsResponseAsync<FileContent>();
		IsTrue(error.TryGetValue(out Error e));
		Assert.Equal(HttpStatusCode.NotFound, e.Type);
	}
}
