using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

// A file is the purest self-describing HTTP message: the wire carries the file's own media type,
// Content-Disposition, ETag, Last-Modified and ranges. Nothing Fuxion is added, and the envelope never applies.
public class BinaryWireTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<BinaryWireTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	static HttpRequestMessage Get(string url, string? accept = null, string? range = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
		return request;
	}

	[Theory(DisplayName = "A file travels with its own media type, name, ETag and Last-Modified, and no Fuxion parameter")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task File_TravelsWithItsOwnHeaders(string prefix)
	{
		var res = await factory.CreateClient().GetAsync($"{prefix}/binary/file");

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Empty(res.Content.Headers.ContentType!.Parameters.Where(p => p.Name == ResponseMediaTypes.NamingParameter));
		Assert.Equal("attachment", res.Content.Headers.ContentDisposition?.DispositionType);
		Assert.Contains(TestFile.Name, res.Content.Headers.ContentDisposition?.FileName ?? res.Content.Headers.ContentDisposition?.FileNameStar ?? "");
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentLength);
		Assert.Equal(TestFile.ETag, res.Headers.ETag?.Tag);
		Assert.Equal(TestFile.LastModified, res.Content.Headers.LastModified);
		Assert.Contains("bytes", res.Headers.AcceptRanges);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
		IsTrue(!res.Headers.Vary.Any()); // the shape does not depend on Accept
	}

	[Theory(DisplayName = "A bare Stream or byte[] is an octet-stream body without a name")]
	[InlineData("minimal/binary/stream")]
	[InlineData("controller/binary/stream")]
	[InlineData("minimal/binary/bytes")]
	[InlineData("controller/binary/bytes")]
	public async Task StreamAndBytes_AreOctetStream(string url)
	{
		var res = await factory.CreateClient().GetAsync(url);

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(BinaryPayload.DefaultContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Null(res.Content.Headers.ContentDisposition);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Theory(DisplayName = "Range requests are honoured with a 206")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Range_IsHonoured(string prefix)
	{
		var res = await factory.CreateClient().SendAsync(Get($"{prefix}/binary/file", range: "bytes=0-3"));

		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync());
		Assert.Equal(0, res.Content.Headers.ContentRange?.From);
		Assert.Equal(3, res.Content.Headers.ContentRange?.To);
	}

	[Theory(DisplayName = "Asking for the envelope does not change a binary success")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Envelope_IsIgnoredForBinarySuccess(string prefix)
	{
		var res = await factory.CreateClient().SendAsync(Get($"{prefix}/binary/file", accept: "application/vnd.fuxion.response+json, application/json;q=0.9"));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Theory(DisplayName = "None and errors of a binary endpoint follow the normal wire rules")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task NoneAndError_AreNormal(string prefix)
	{
		var none = await factory.CreateClient().GetAsync($"{prefix}/binary/none");
		Assert.Equal(HttpStatusCode.NoContent, none.StatusCode);

		var error = await factory.CreateClient().GetAsync($"{prefix}/binary/error");
		Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);

		var nativeError = await factory.CreateClient().SendAsync(Get($"{prefix}/binary/error", accept: "application/vnd.fuxion.error+json, application/json;q=0.9"));
		Assert.Equal(ResponseMediaTypes.ErrorJson, nativeError.Content.Headers.ContentType?.MediaType);
	}
}

public class BinaryClientTest(ITestOutputHelper output, WebApplicationFactory<Program> factory) : BaseTest<BinaryClientTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	[Theory(DisplayName = "The client rebuilds FileContent from a real server response and streams the body")]
	[InlineData("minimal")]
	[InlineData("controller")]
	public async Task Client_ReadsFileContent(string prefix)
	{
		var response = await factory.CreateClient().GetAsync($"{prefix}/binary/file", HttpCompletionOption.ResponseHeadersRead).AsResponseAsync<FileContent>();

		IsTrue(response.TryGetValue(out FileContent? file));
		Assert.Equal(TestFile.ContentType, file!.ContentType);
		Assert.Equal(TestFile.Name, file.FileName);
		Assert.Equal(TestFile.Bytes.Length, file.Length);
		Assert.Equal(TestFile.ETag, file.ETag);
		Assert.Equal(TestFile.LastModified, file.LastModified);
		using var memory = new System.IO.MemoryStream();
		await file.Stream.CopyToAsync(memory);
		Assert.Equal(TestFile.Bytes, memory.ToArray());
	}

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
