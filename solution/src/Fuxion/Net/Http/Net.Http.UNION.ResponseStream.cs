namespace Fuxion.Union.Net.Http;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The response body stream handed to the caller. Disposing it also disposes the
/// <see cref="HttpResponseMessage"/> that owns it, so a consumer can hold only the stream.
/// </summary>
sealed class HttpResponseStream(Stream inner, HttpResponseMessage message) : Stream
{
	public override bool CanRead => inner.CanRead;
	public override bool CanSeek => inner.CanSeek;
	public override bool CanWrite => false;
	public override long Length => inner.Length;
	public override long Position { get => inner.Position; set => inner.Position = value; }
	public override void Flush() => inner.Flush();
	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
	public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			inner.Dispose();
			message.Dispose();
		}
		base.Dispose(disposing);
	}
}
