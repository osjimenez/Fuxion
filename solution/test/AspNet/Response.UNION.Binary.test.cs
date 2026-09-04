using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion.Union;
using Fuxion.Xunit;
using Test.AspNet.Service;
using Xunit;

namespace Test.AspNet.Union;

public class BinaryWireTest(ITestOutputHelper output) : BaseTest<BinaryWireTest>(output)
{
	static HttpRequestMessage Get(string url, string? accept = null, string? range = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
		return request;
	}

	[Fact(DisplayName = "A file travels with its own media type, name, ETag and Last-Modified, without Vary")]
	public async Task File_TravelsWithItsOwnHeaders()
	{
		var res = await AspNetHost.Create().GetAsync("binary/file");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal("attachment", res.Content.Headers.ContentDisposition?.DispositionType);
		Assert.Contains(TestFile.Name, res.Content.Headers.ContentDisposition?.FileName ?? "");
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentLength);
		Assert.Equal(TestFile.ETag, res.Headers.ETag?.Tag);
		Assert.Equal(TestFile.LastModified, res.Content.Headers.LastModified);
		Assert.Contains("bytes", res.Headers.AcceptRanges);
		IsTrue(!res.Headers.Vary.Any());
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Theory(DisplayName = "A bare Stream or byte[] is an octet-stream body")]
	[InlineData("binary/stream")]
	[InlineData("binary/bytes")]
	public async Task StreamAndBytes_AreOctetStream(string url)
	{
		var res = await AspNetHost.Create().GetAsync(url);
		Assert.Equal(BinaryPayload.DefaultContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Fact(DisplayName = "Range requests are honoured with a 206")]
	public async Task Range_IsHonoured()
	{
		var res = await AspNetHost.Create().SendAsync(Get("binary/file", range: "bytes=0-3"));
		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync());
		Assert.Equal(0, res.Content.Headers.ContentRange?.From);
		Assert.Equal(3, res.Content.Headers.ContentRange?.To);
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentRange?.Length);
	}

	[Fact(DisplayName = "An out-of-bounds range is a 416")]
	public async Task Range_OutOfBounds_Is416()
	{
		var res = await AspNetHost.Create().SendAsync(Get("binary/file", range: "bytes=999-1000"));
		Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, res.StatusCode);
	}

	[Fact(DisplayName = "Asking for the envelope does not change a binary success")]
	public async Task Envelope_IsIgnored()
	{
		var res = await AspNetHost.Create().SendAsync(Get("binary/file", accept: "application/vnd.fuxion.response+json"));
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "None and errors of a binary endpoint follow the normal wire rules")]
	public async Task NoneAndError_AreNormal()
	{
		var cli = AspNetHost.Create();
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync("binary/none")).StatusCode);
		var error = await cli.GetAsync("binary/error");
		Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);
	}
}
