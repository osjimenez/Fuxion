using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fuxion;

namespace Test.Responses.Shared.Fixtures;

/// <summary>
/// A remote-like source (think S3 or Blob): its streams are never seekable, so a range can only be served by asking the
/// source for just that range. A whole open returns every byte, a range open only the requested ones, so a server
/// that answers a range from the whole stream sends the wrong body.
/// </summary>
public sealed class RangeSource(byte[] bytes) : IRangeContentSource
{
	/// <summary>Opens every byte, forward-only.</summary>
	public Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult<Stream>(new NonSeekableStream(bytes));

	/// <summary>Opens the bytes from <paramref name="from"/> to <paramref name="to"/> (inclusive), forward-only.</summary>
	public Task<Stream> OpenRangeAsync(long from, long to, CancellationToken cancellationToken = default)
		=> Task.FromResult<Stream>(new NonSeekableStream(bytes.Skip((int)from).Take((int)(to - from + 1)).ToArray()));

	/// <summary>The test file behind a <see cref="RangeSource"/>, with its length and metadata.</summary>
	public static IOContent Content() => new(new RangeSource(TestFile.Bytes), TestFile.ContentType, TestFile.Name, TestFile.Bytes.Length)
	{
		LastModified = TestFile.LastModified,
		ETag = TestFile.ETag
	};
}
