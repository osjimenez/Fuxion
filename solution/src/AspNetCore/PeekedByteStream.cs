using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.AspNetCore;

/// <summary>Replays one already-read byte in front of an inner stream, so a stream can be probed one byte deep
/// (to tell "no data at all" from "some data") without losing that byte for the real read that follows.
/// Never disposes the inner stream: that one is owned by the request, not by this wrapper.</summary>
/// <remarks>
/// Only the async read overloads are implemented: <see cref="JsonSerializer.DeserializeAsync{TValue}(Stream, System.Text.Json.JsonSerializerOptions?, System.Threading.CancellationToken)"/>
/// (the only consumer of this stream) never calls a synchronous <c>Read</c>, and this host disallows
/// synchronous request-body reads anyway, so the synchronous overloads throw <see cref="NotSupportedException"/>
/// instead of silently blocking on the inner stream (sync-over-async) to serve one.
/// </remarks>
sealed class PeekedByteStream(Stream inner, byte firstByte) : Stream
{
	// Only set once the peeked byte has actually been written into a caller's buffer - a zero-length read
	// (an empty buffer, or count 0) writes nothing, so it must return 0 without marking the byte as served,
	// or it would be silently dropped: the next, real read would go straight to the inner stream and skip it.
	bool served;

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();
	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

	public override void Flush() { }

	public override int Read(byte[] buffer, int offset, int count)
		=> throw new NotSupportedException("Synchronous reads are not supported by this stream; only the async overloads are.");

	public override int Read(Span<byte> buffer)
		=> throw new NotSupportedException("Synchronous reads are not supported by this stream; only the async overloads are.");

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!served)
		{
			if (count == 0) return 0; // nothing written: still unserved, try again on the next call
			buffer[offset] = firstByte;
			served = true;
			return 1;
		}
		return await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (!served)
		{
			if (buffer.IsEmpty) return 0; // nothing written: still unserved, try again on the next call
			buffer.Span[0] = firstByte;
			served = true;
			return 1;
		}
		return await inner.ReadAsync(buffer, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing) { } // the inner stream (Request.Body) is not ours to dispose
}
