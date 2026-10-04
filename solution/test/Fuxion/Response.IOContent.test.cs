using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class IOContentTest(ITestOutputHelper output) : BaseTest<IOContentTest>(output)
{
	/// <summary>A minimal non-seekable stream, used to prove range processing is not defaulted on for it.</summary>
	sealed class NonSeekableStream : Stream
	{
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new System.NotSupportedException();
		public override long Position { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
		public override void Flush() { }
		public override int Read(byte[] buffer, int offset, int count) => 0;
		public override long Seek(long offset, SeekOrigin origin) => throw new System.NotSupportedException();
		public override void SetLength(long value) => throw new System.NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new System.NotSupportedException();
	}

	[Fact(DisplayName = "A file content defaults to octet-stream and takes the length from a seekable stream")]
	public void Defaults()
	{
		var bytes = "hello"u8.ToArray();
		using var file = new IOContent(new MemoryStream(bytes));

		Assert.Equal(BinaryPayload.DefaultContentType, file.ContentType);
		Assert.Null(file.FileName);
		Assert.Equal(5, file.Length);
		Assert.Null(file.LastModified);
		Assert.Null(file.ETag);
		IsTrue(file.EnableRangeProcessing);
	}

	[Fact(DisplayName = "Range processing is on by default for seekable streams and off otherwise")]
	public void RangeProcessing_DefaultsFromSeekability()
	{
		IsTrue(new IOContent(new MemoryStream(new byte[3])).EnableRangeProcessing);
		IsFalse(new IOContent(new NonSeekableStream()).EnableRangeProcessing);
		IsFalse(new IOContent(new MemoryStream(new byte[3])) { EnableRangeProcessing = false }.EnableRangeProcessing);
	}

	[Fact(DisplayName = "Metadata is carried by the value itself")]
	public void Metadata()
	{
		using var file = new IOContent(new MemoryStream(new byte[3]), "application/pdf", "doc.pdf") { ETag = "\"v1\"", EnableRangeProcessing = true };

		Assert.Equal("application/pdf", file.ContentType);
		Assert.Equal("doc.pdf", file.FileName);
		Assert.Equal("\"v1\"", file.ETag);
		IsTrue(file.EnableRangeProcessing);
	}

	[Fact(DisplayName = "FromBytes knows its length and opens a new stream from the start every time")]
	public async Task FromBytes()
	{
		var bytes = Encoding.UTF8.GetBytes("payload");
		using var file = IOContent.FromBytes(bytes, "text/plain", "a.txt");

		Assert.Equal(bytes.Length, file.Length);
		Assert.Equal("text/plain", file.ContentType);
		Assert.Equal("a.txt", file.FileName);
		IsTrue(file.EnableRangeProcessing);
		Assert.Equal("payload", await ReadAllAsync(file));
		Assert.Equal("payload", await ReadAllAsync(file));
	}

	[Fact(DisplayName = "FromStream hands out its open stream once and knows the length only when it is seekable")]
	public async Task FromStream()
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
		using var file = IOContent.FromStream(stream);

		Assert.Equal(7, file.Length);
		IsFalse(file.Source is IRangeContentSource);
		Assert.Same(stream, await file.OpenAsync());
		await Assert.ThrowsAsync<System.InvalidOperationException>(() => file.OpenAsync());
		Assert.Null(IOContent.FromStream(new NonSeekableStream()).Length);
	}

	[Fact(DisplayName = "FromFile takes length, date and name from the file and opens it for shared reading")]
	public async Task FromFile()
	{
		var path = Path.Combine(Path.GetTempPath(), $"iocontent-{System.Guid.NewGuid():N}.txt");
		File.WriteAllText(path, "payload");
		try
		{
			using var file = IOContent.FromFile(path);

			Assert.Equal(7, file.Length);
			Assert.Equal(BinaryPayload.DefaultContentType, file.ContentType);
			Assert.Equal(Path.GetFileName(path), file.FileName);
			Assert.Equal(new System.DateTimeOffset(File.GetLastWriteTimeUtc(path), System.TimeSpan.Zero), file.LastModified);
			IsTrue(file.EnableRangeProcessing);
			using (var first = await file.OpenAsync())
			using (var second = await file.OpenAsync())
				Assert.Equal("payload", await new StreamReader(second).ReadToEndAsync());
			Assert.Equal("doc.txt", IOContent.FromFile(path, fileName: "doc.txt").FileName);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact(DisplayName = "FromFile of a missing file fails when it is opened, not when it is created")]
	public async Task FromFile_Missing()
	{
		var file = IOContent.FromFile(Path.Combine(Path.GetTempPath(), $"missing-{System.Guid.NewGuid():N}.txt"));

		Assert.Null(file.Length);
		Assert.Null(file.LastModified);
		await Assert.ThrowsAsync<FileNotFoundException>(() => file.OpenAsync());
	}

	[Fact(DisplayName = "Range processing is on by default only for sources that can open ranges")]
	public void RangeProcessing_DefaultsFromTheSource()
	{
		IsFalse(new IOContent(new BytesContentSource([1])).EnableRangeProcessing);
		IsTrue(new IOContent(new RangeOnlySource()).EnableRangeProcessing);
	}

	static async Task<string> ReadAllAsync(IOContent file)
	{
		using var reader = new StreamReader(await file.OpenAsync());
		return await reader.ReadToEndAsync();
	}

	sealed class RangeOnlySource : IRangeContentSource
	{
		public Task<Stream> OpenAsync(CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
		public Task<Stream> OpenRangeAsync(long from, long to, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
	}

	[Fact(DisplayName = "Disposing the file content disposes its stream")]
	public void Dispose_DisposesStream()
	{
		var stream = new MemoryStream(new byte[1]);
		new IOContent(stream).Dispose();

		Assert.Throws<System.ObjectDisposedException>(() => stream.ReadByte());
	}

	[Theory(DisplayName = "Only IOContent, byte[] and streams are binary payload types")]
	[InlineData(typeof(IOContent), true)]
	[InlineData(typeof(byte[]), true)]
	[InlineData(typeof(Stream), true)]
	[InlineData(typeof(MemoryStream), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(Unit), false)]
	[InlineData(typeof(int[]), false)]
	public void IsBinaryType(System.Type type, bool expected)
		=> Assert.Equal(expected, BinaryPayload.IsBinaryType(type));
}
