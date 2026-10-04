#if OLD_FRAMEWORKS
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.Xunit;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// The CancellationToken overloads that .NET Framework lacks, so a test can pass TestContext.Current.CancellationToken
// (xUnit1051) the same way on every target framework. Where .NET Framework has no cancellable version, the token is
// checked once before starting.
public static class CancellationPolyfills
{
	public static Task<string> ReadToEndAsync(this TextReader reader, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return reader.ReadToEndAsync();
	}

	public static Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken)
		=> source.CopyToAsync(destination, 81920, cancellationToken);

	public static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.ReadAsStreamAsync();
	}

	public static Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.ReadAsStringAsync();
	}

	public static Task<byte[]> ReadAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.ReadAsByteArrayAsync();
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#endif
