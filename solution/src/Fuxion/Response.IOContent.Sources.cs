using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Where an IOContent comes from. It is opened only when the content is written or read; whoever opens it owns (and
// disposes) the stream.
public interface IContentSource
{
	Task<Stream> OpenAsync(CancellationToken cancellationToken = default);
}

// A source that can open just a part of the content, so a range request to a remote object (S3, Blob...) does not
// download all of it. The core sources do not need it: their streams are seekable, and the adapters serve ranges of
// a seekable stream through the framework (with conditional requests and If-Range).
public interface IRangeContentSource : IContentSource
{
	// The bytes from 'from' to 'to', both inclusive, like "Range: bytes=from-to".
	Task<Stream> OpenRangeAsync(long from, long to, CancellationToken cancellationToken = default);
}

// An in-memory array: every open is a new read-only stream over it.
public sealed class BytesContentSource(byte[] bytes) : IContentSource
{
	readonly byte[] bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

	public Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
}

// An already open stream: it can be handed out only once.
public sealed class StreamContentSource(Stream stream) : IContentSource, IDisposable
{
	readonly Stream stream = stream ?? throw new ArgumentNullException(nameof(stream));
	int opened;

	public Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
		=> Interlocked.Exchange(ref opened, 1) == 0
			? Task.FromResult(stream)
			: throw new InvalidOperationException("The content comes from an already open stream, which can be opened only once.");

	public void Dispose() => stream.Dispose();
}

// A file of the file system, opened for asynchronous reading and shared with other readers.
public sealed class FileContentSource(string path) : IContentSource
{
	readonly string path = path ?? throw new ArgumentNullException(nameof(path));

	public Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true));
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
