using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Net.Http;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

// Binary payloads are read as streams, not strings; the decision is taken from the type and the media
// type before any byte of the body is consumed.
public class ClientBinaryReadingTest(ITestOutputHelper output) : BaseTest<ClientBinaryReadingTest>(output)
{
	public record Payload(string Name);

	/// <summary>Counts how many bytes have been pulled through it, to prove nothing was buffered eagerly.</summary>
	sealed class CountingStream(byte[] data) : Stream
	{
		readonly MemoryStream inner = new(data, writable: false);
		public long BytesRead { get; private set; }
		public override int Read(byte[] buffer, int offset, int count) { var n = inner.Read(buffer, offset, count); BytesRead += n; return n; }
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	static (HttpResponseMessage Message, CountingStream Body) Binary(string contentType = "application/pdf", string? fileName = "a.pdf")
	{
		var body = new CountingStream(Encoding.UTF8.GetBytes("0123456789"));
		var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
		message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
		message.Content.Headers.ContentLength = 10;
		if (fileName is not null)
			message.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
		message.Headers.ETag = new EntityTagHeaderValue("\"v1\"");
		return (message, body);
	}

	[Fact(DisplayName = "A Stream is handed over without pulling a single byte")]
	public async Task Stream_IsNotBuffered()
	{
		var (message, body) = Binary();

		var response = await message.AsResponseAsync<Stream>();

		IsTrue(response.TryGetValue(out Stream? stream));
		Assert.Equal(0, body.BytesRead);
		using var reader = new StreamReader(stream!);
		Assert.Equal("0123456789", await reader.ReadToEndAsync());
		Assert.Equal(10, body.BytesRead);
	}

	[Fact(DisplayName = "FileContent is rebuilt from the standard headers")]
	public async Task FileContent_FromHeaders()
	{
		var (message, _) = Binary();

		var response = await message.AsResponseAsync<FileContent>();

		IsTrue(response.TryGetValue(out FileContent? file));
		Assert.Equal("application/pdf", file!.ContentType);
		Assert.Equal("a.pdf", file.FileName);
		Assert.Equal(10, file.Length);
		Assert.Equal("\"v1\"", file.ETag);
		using var reader = new StreamReader(file.Stream);
		Assert.Equal("0123456789", await reader.ReadToEndAsync());
	}

	[Fact(DisplayName = "byte[] materializes the whole body")]
	public async Task Bytes_AreMaterialized()
	{
		var (message, _) = Binary();
		var response = await message.AsResponseAsync<byte[]>();
		IsTrue(response.TryGetValue(out byte[]? bytes));
		Assert.Equal(Encoding.UTF8.GetBytes("0123456789"), bytes);
	}

	[Fact(DisplayName = "Disposing the returned stream disposes the HTTP response message")]
	public async Task DisposingStream_DisposesMessage()
	{
		var (message, _) = Binary();
		var response = await message.AsResponseAsync<Stream>();
		IsTrue(response.TryGetValue(out Stream? stream));

		stream!.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() => message.Content.ReadAsStreamAsync());
	}

	[Fact(DisplayName = "A non-binary type receiving a binary body fails without reading it")]
	public async Task NonBinaryType_BinaryBody_FailsWithoutReading()
	{
		var (message, body) = Binary();

		var response = await message.AsResponseAsync<Payload>();

		IsTrue(response.IsError);
		Assert.Equal(0, body.BytesRead);
	}

	[Fact(DisplayName = "A binary type without any Content-Type is still read as a stream (legacy servers)")]
	public async Task BinaryType_NoContentType_IsStream()
	{
		var body = new CountingStream(new byte[] { 1, 2, 3 });
		var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
		message.Content.Headers.ContentType = null;

		var response = await message.AsResponseAsync<FileContent>();

		IsTrue(response.TryGetValue(out FileContent? file));
		Assert.Equal(BinaryPayload.DefaultContentType, file!.ContentType);
	}

	[Fact(DisplayName = "An error on a binary endpoint is still read as an error")]
	public async Task BinaryType_ProblemError_IsError()
	{
		var message = new HttpResponseMessage(HttpStatusCode.NotFound)
		{
			Content = new StringContent("""{"title":"Not Found","status":404,"detail":"missing"}""", Encoding.UTF8, ResponseMediaTypes.ProblemJson)
		};

		var response = await message.AsResponseAsync<FileContent>();

		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.NotFound, error.Type);
		Assert.Equal("missing", error.Message);
	}

	[Fact(DisplayName = "With a custom error type, a binary body is read the same way")]
	public async Task Typed_Stream_IsNotBuffered()
	{
		var (message, body) = Binary();
		var response = await message.AsResponseAsync<Stream, Payload>();
		IsTrue(response.TryGetValue(out Stream? _));
		Assert.Equal(0, body.BytesRead);
	}

	[Fact(DisplayName = "A legacy empty 200 with a non-JSON content type still resolves to Unit")]
	public async Task LegacyEmptyBody_NonJsonContentType_ResolvesToUnit()
	{
		var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };

		var response = await message.AsResponseAsync<Unit>();

		IsTrue(response.TryGetValue(out Unit _));
	}

	/// <summary>A concrete <see cref="Stream"/> subtype the client cannot synthesize from an HTTP body.</summary>
	sealed class OtherStream : MemoryStream;

	[Fact(DisplayName = "MemoryStream is materialized from the body")]
	public async Task MemoryStream_IsMaterialized()
	{
		var (message, _) = Binary();

		var response = await message.AsResponseAsync<MemoryStream>();

		IsTrue(response.TryGetValue(out MemoryStream? ms));
		Assert.Equal(10, ms!.Length);
		using var reader = new StreamReader(ms);
		Assert.Equal("0123456789", await reader.ReadToEndAsync());
	}

	[Fact(DisplayName = "An unsupported Stream subtype is reported as an error, not an exception")]
	public async Task UnsupportedStreamSubtype_IsError_NotException()
	{
		var (message, _) = Binary();
		var response = await message.AsResponseAsync<OtherStream>();
		IsTrue(response.IsError);

		var (typedMessage, _) = Binary();
		await Assert.ThrowsAsync<InvalidOperationException>(() => typedMessage.AsResponseAsync<OtherStream, Payload>());
	}
}
