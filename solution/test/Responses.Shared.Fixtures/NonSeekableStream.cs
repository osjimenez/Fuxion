using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Responses.Shared.Fixtures;

/// <summary>A read-only, forward-only stream over a byte array: no Length, no Seek. Servers must send it chunked, without ranges.</summary>
public sealed class NonSeekableStream(byte[] bytes) : Stream
{
	readonly MemoryStream inner = new(bytes, writable: false);
	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();
	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
	public override void Flush() { }
	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
