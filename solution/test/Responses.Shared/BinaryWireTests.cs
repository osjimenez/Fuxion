namespace Test.Responses.Shared;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

/// <summary>
/// A file is the purest self-describing HTTP message: the wire carries the file's own media type,
/// Content-Disposition, ETag, Last-Modified and ranges. Nothing Fuxion is added, and the envelope never applies.
/// </summary>
public abstract class BinaryWireTests : BaseTest<BinaryWireTests>
{
	/// <summary>The host this test matrix runs against.</summary>
	protected IWireHost Host { get; }

	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected BinaryWireTests(ITestOutputHelper output, IWireHost host) : base(output)
	{
		Host = host;
		Output.WriteLine($"Host: {host.GetType().Name}");
	}

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
		var res = await Host.CreateClient().GetAsync(Host.Route("binary/file"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Empty(res.Content.Headers.ContentType!.Parameters.Where(p => p.Name == ResponseMediaTypes.NamingParameter));
		Assert.Equal("attachment", res.Content.Headers.ContentDisposition?.DispositionType);
		Assert.Contains(TestFile.Name, res.Content.Headers.ContentDisposition?.FileName ?? res.Content.Headers.ContentDisposition?.FileNameStar ?? "");
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentLength);
		Assert.Equal(TestFile.ETag, res.Headers.ETag?.Tag);
		Assert.Equal(TestFile.LastModified, res.Content.Headers.LastModified);
		Assert.Contains("bytes", res.Headers.AcceptRanges);
		IsTrue(!res.Headers.Vary.Any());
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Theory(DisplayName = "A bare Stream or byte[] is an octet-stream body")]
	[InlineData("stream")]
	[InlineData("bytes")]
	public async Task StreamAndBytes_AreOctetStream(string logical)
	{
		var res = await Host.CreateClient().GetAsync(Host.Route($"binary/{logical}"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(BinaryPayload.DefaultContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Null(res.Content.Headers.ContentDisposition);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Fact(DisplayName = "Range requests are honoured with a 206")]
	public async Task Range_IsHonoured()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route("binary/file"), range: "bytes=0-3"));
		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync());
		Assert.Equal(0, res.Content.Headers.ContentRange?.From);
		Assert.Equal(3, res.Content.Headers.ContentRange?.To);
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentRange?.Length);
	}

	[Fact(DisplayName = "A bare stream honours Range by default")]
	public async Task Stream_HonoursRangeByDefault()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route("binary/stream"), range: "bytes=0-3"));
		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync());
	}

	[Fact(DisplayName = "Asking for the envelope does not change a binary success")]
	public async Task Envelope_IsIgnored()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route("binary/file"), accept: "application/vnd.fuxion.response+json, application/json;q=0.9"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}

	[Fact(DisplayName = "None and errors of a binary endpoint follow the normal wire rules")]
	public async Task NoneAndError_AreNormal()
	{
		var cli = Host.CreateClient();
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync(Host.Route("binary/none"))).StatusCode);

		var error = await cli.GetAsync(Host.Route("binary/error"));
		Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);

		var nativeError = await cli.SendAsync(Get(Host.Route("binary/error"), accept: "application/vnd.fuxion.error+json, application/json;q=0.9"));
		Assert.Equal(ResponseMediaTypes.ErrorJson, nativeError.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "A non-seekable stream is served in full, without ranges")]
	public async Task NonSeekableStream_NoRanges()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route("binary/chunked"), range: "bytes=0-3"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		IsTrue(!res.Headers.AcceptRanges.Any());
		Assert.Null(res.Content.Headers.ContentRange);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync());
	}
}
