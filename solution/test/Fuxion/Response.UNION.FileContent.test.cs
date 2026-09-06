using System.IO;
using System.Text;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class FileContentTest(ITestOutputHelper output) : BaseTest<FileContentTest>(output)
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
		var bytes = Encoding.UTF8.GetBytes("hello");
		using var file = new FileContent(new MemoryStream(bytes));

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
		IsTrue(new FileContent(new MemoryStream(new byte[3])).EnableRangeProcessing);
		IsTrue(!new FileContent(new NonSeekableStream()).EnableRangeProcessing);
		IsTrue(!new FileContent(new MemoryStream(new byte[3])) { EnableRangeProcessing = false }.EnableRangeProcessing);
	}

	[Fact(DisplayName = "Metadata is carried by the value itself")]
	public void Metadata()
	{
		using var file = new FileContent(new MemoryStream(new byte[3]), "application/pdf", "doc.pdf") { ETag = "\"v1\"", EnableRangeProcessing = true };

		Assert.Equal("application/pdf", file.ContentType);
		Assert.Equal("doc.pdf", file.FileName);
		Assert.Equal("\"v1\"", file.ETag);
		IsTrue(file.EnableRangeProcessing);
	}

	[Fact(DisplayName = "FromBytes wraps the array without copying its meaning")]
	public void FromBytes()
	{
		var bytes = Encoding.UTF8.GetBytes("payload");
		using var file = FileContent.FromBytes(bytes, "text/plain", "a.txt");

		Assert.Equal(bytes.Length, file.Length);
		Assert.Equal("text/plain", file.ContentType);
		Assert.Equal("a.txt", file.FileName);
		using var reader = new StreamReader(file.Stream);
		Assert.Equal("payload", reader.ReadToEnd());
	}

	[Fact(DisplayName = "Disposing the file content disposes its stream")]
	public void Dispose_DisposesStream()
	{
		var stream = new MemoryStream(new byte[1]);
		new FileContent(stream).Dispose();

		Assert.Throws<System.ObjectDisposedException>(() => stream.ReadByte());
	}

	[Theory(DisplayName = "Only FileContent, byte[] and streams are binary payload types")]
	[InlineData(typeof(FileContent), true)]
	[InlineData(typeof(byte[]), true)]
	[InlineData(typeof(Stream), true)]
	[InlineData(typeof(MemoryStream), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(Unit), false)]
	[InlineData(typeof(int[]), false)]
	public void IsBinaryType(System.Type type, bool expected)
		=> Assert.Equal(expected, BinaryPayload.IsBinaryType(type));
}
