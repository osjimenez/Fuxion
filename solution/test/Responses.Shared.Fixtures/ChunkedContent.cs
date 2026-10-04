using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Test.Responses.Shared.Fixtures;

/// <summary>Request content whose length cannot be computed, so it travels without Content-Length (chunked).</summary>
public sealed class ChunkedContent(byte[] bytes) : HttpContent
{
	protected override bool TryComputeLength(out long length)
	{
		length = 0;
		return false;
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
		=> stream.WriteAsync(bytes, 0, bytes.Length);
}
