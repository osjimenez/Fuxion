using Fuxion;
using Test.Responses.Shared.Fixtures;
using static Test.Responses.Shared.WireRequests;

namespace Test.Responses.Shared;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Xunit;

/// <summary>
/// A file is the purest self-describing HTTP message: the wire carries the file's own media type,
/// Content-Disposition, ETag, Last-Modified and ranges. Nothing Fuxion is added, and the envelope never applies.
/// </summary>
public abstract class BinaryWireTests : WireTestBase<BinaryWireTests>
{
	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected BinaryWireTests(ITestOutputHelper output, IWireHost host) : base(output, host) { }

	[Fact(DisplayName = "A file travels with its own media type, name, ETag and Last-Modified, without Vary")]
	public async Task File_TravelsWithItsOwnHeaders()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.BinaryFile), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.DoesNotContain(res.Content.Headers.ContentType!.Parameters, p => p.Name == ResponseMediaTypes.NamingParameter);
		Assert.Equal("attachment", res.Content.Headers.ContentDisposition?.DispositionType);
		Assert.Contains(TestFile.Name, res.Content.Headers.ContentDisposition?.FileName ?? res.Content.Headers.ContentDisposition?.FileNameStar ?? "");
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentLength);
		Assert.Equal(TestFile.ETag, res.Headers.ETag?.Tag);
		Assert.Equal(TestFile.LastModified, res.Content.Headers.LastModified);
		Assert.Contains("bytes", res.Headers.AcceptRanges);
		IsFalse(res.Headers.Vary.Any());
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Theory(DisplayName = "A bare Stream or byte[] is an octet-stream body")]
	[InlineData("stream")]
	[InlineData("bytes")]
	public async Task StreamAndBytes_AreOctetStream(string logical)
	{
		var res = await Host.CreateClient().GetAsync(Host.Route($"binary/{logical}"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(BinaryPayload.DefaultContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Null(res.Content.Headers.ContentDisposition);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "Range requests are honoured with a 206")]
	public async Task Range_IsHonoured()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryFile), range: "bytes=0-3"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
		Assert.Equal(0, res.Content.Headers.ContentRange?.From);
		Assert.Equal(3, res.Content.Headers.ContentRange?.To);
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentRange?.Length);
	}

	// Only the status is shared: Web API 2 answers the 416 with an HttpError body, ASP.NET Core with an empty one
	// and Content-Range: bytes */length (a known delta, see ideas.md).
	[Fact(DisplayName = "An out-of-bounds range is a 416")]
	public async Task Range_OutOfBounds_Is416()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryFile), range: "bytes=999-1000"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, res.StatusCode);
	}

	[Fact(DisplayName = "A bare stream honours Range by default")]
	public async Task Stream_HonoursRangeByDefault()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryStream), range: "bytes=0-3"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "Asking for the envelope does not change a binary success")]
	public async Task Envelope_IsIgnored()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryFile), accept: "application/vnd.fuxion.response+json, application/json;q=0.9"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.ContentType, res.Content.Headers.ContentType?.MediaType);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "None and errors of a binary endpoint follow the normal wire rules")]
	public async Task NoneAndError_AreNormal()
	{
		var cli = Host.CreateClient();
		Assert.Equal(HttpStatusCode.NoContent, (await cli.GetAsync(Host.Route(Routes.BinaryNone), TestContext.Current.CancellationToken)).StatusCode);

		var error = await cli.GetAsync(Host.Route(Routes.BinaryError), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
		Assert.Equal(ResponseMediaTypes.ProblemJson, error.Content.Headers.ContentType?.MediaType);

		var nativeError = await cli.SendAsync(Get(Host.Route(Routes.BinaryError), accept: "application/vnd.fuxion.error+json, application/json;q=0.9"), TestContext.Current.CancellationToken);
		Assert.Equal(ResponseMediaTypes.ErrorJson, nativeError.Content.Headers.ContentType?.MediaType);
	}

	[Fact(DisplayName = "A non-seekable stream is served in full, without ranges")]
	public async Task NonSeekableStream_NoRanges()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryChunked), range: "bytes=0-3"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		IsFalse(res.Headers.AcceptRanges.Any());
		Assert.Null(res.Content.Headers.ContentRange);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "A non-seekable stream with a known length is sent with Content-Length")]
	public async Task SizedStream_SendsContentLength()
	{
		// Headers only: a buffered read would compute Content-Length on the client and hide a missing header.
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.BinarySizedStream), HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentLength);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "A source that opens ranges is served in full with its length and announces ranges")]
	public async Task RangeSource_Full()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route(Routes.BinaryRangeSource), HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentLength);
		Assert.Contains("bytes", res.Headers.AcceptRanges);
		Assert.Equal(TestFile.ETag, res.Headers.ETag?.Tag);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "A range of a source that opens ranges is a 206 with just those bytes, opened from the source")]
	public async Task RangeSource_Range()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryRangeSource), range: "bytes=2-5"), HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.PartialContent, res.StatusCode);
		Assert.Equal(2, res.Content.Headers.ContentRange?.From);
		Assert.Equal(5, res.Content.Headers.ContentRange?.To);
		Assert.Equal(TestFile.Bytes.Length, res.Content.Headers.ContentRange?.Length);
		Assert.Equal(4, res.Content.Headers.ContentLength);
		Assert.Equal(TestFile.Bytes.Skip(2).Take(4).ToArray(), await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}

	[Fact(DisplayName = "An out-of-bounds range of a source that opens ranges is a 416")]
	public async Task RangeSource_OutOfBounds_Is416()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryRangeSource), range: "bytes=999-1000"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, res.StatusCode);
	}

	[Fact(DisplayName = "Several ranges of a source that opens ranges are served as the whole content")]
	public async Task RangeSource_SeveralRanges_AreTheWholeContent()
	{
		var res = await Host.CreateClient().SendAsync(Get(Host.Route(Routes.BinaryRangeSource), range: "bytes=0-1,4-5"), TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(TestFile.Bytes, await res.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
	}
}
