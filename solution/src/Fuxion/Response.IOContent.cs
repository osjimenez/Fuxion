using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// A binary payload that describes itself: the stream plus the HTTP metadata a file needs on the wire.
/// A file is the purest self-describing HTTP message, so nothing Fuxion is added to it: the media type
/// is the file's own, the name travels in Content-Disposition, and caching/ranges use the standard
/// headers. It never travels inside the response envelope.
/// </summary>
// What the content is (this descriptor) is kept apart from where it comes from (its IContentSource): the source is
// opened only when the content is written or read, so a remote object never has to be loaded beforehand.
public sealed class IOContent : IDisposable
{
	public IOContent(IContentSource source, string? contentType = null, string? fileName = null, long? length = null)
	{
		Source = source ?? throw new ArgumentNullException(nameof(source));
		ContentType = string.IsNullOrWhiteSpace(contentType) ? BinaryPayload.DefaultContentType : contentType!;
		FileName = fileName;
		Length = length;
		EnableRangeProcessing = source is IRangeContentSource;
	}

	// An already open stream (what the content always was before its sources): it can be opened only once.
	public IOContent(Stream stream, string? contentType = null, string? fileName = null)
		: this(new StreamContentSource(stream), contentType, fileName, stream.CanSeek ? stream.Length : null)
		=> EnableRangeProcessing = stream.CanSeek;

	public IContentSource Source { get; }

	/// <summary>The file's own media type; <c>application/octet-stream</c> when unknown.</summary>
	public string ContentType { get; init; }

	/// <summary>
	/// Suggested download name (Content-Disposition). Null for an inline body without a name.
	/// On the client this is the server's suggestion: sanitize it before using it as a file-system path.
	/// </summary>
	public string? FileName { get; init; }

	/// <summary>
	/// Content length when known; taken from a seekable stream by default. On the server it is sent as
	/// Content-Length when the stream cannot tell it (not seekable). On the client it is what Content-Length announced.
	/// </summary>
	public long? Length { get; init; }

	public DateTimeOffset? LastModified { get; init; }

	/// <summary>Entity tag including its quotes, e.g. <c>"v1"</c>.</summary>
	public string? ETag { get; init; }

	/// <summary>
	/// Whether the server should honour Range requests (206) for this content. Defaults to the stream's
	/// seekability; range requests need a known length and random access.
	/// </summary>
	public bool EnableRangeProcessing { get; init; }

	// Opens the whole content. Whoever opens it owns (and disposes) the stream.
	public Task<Stream> OpenAsync(CancellationToken cancellationToken = default) => Source.OpenAsync(cancellationToken);

	/// <summary>Wraps an in-memory array. Bytes are already materialized, so this is never a streaming case.</summary>
	public static IOContent FromBytes(byte[] bytes, string? contentType = null, string? fileName = null)
		=> new(new BytesContentSource(bytes), contentType, fileName, bytes.Length) { EnableRangeProcessing = true };

	public static IOContent FromStream(Stream stream, string? contentType = null, string? fileName = null)
		=> new(stream, contentType, fileName);

	// Length and last modification come from the file system now; a missing file fails when it is opened, not here.
	public static IOContent FromFile(string path, string? contentType = null, string? fileName = null)
	{
		var info = new FileInfo(path ?? throw new ArgumentNullException(nameof(path)));
		return new(new FileContentSource(path), contentType, fileName ?? info.Name, info.Exists ? info.Length : null)
		{
			LastModified = info.Exists ? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero) : null,
			EnableRangeProcessing = true
		};
	}

	public void Dispose() => (Source as IDisposable)?.Dispose();
}

/// <summary>Which success types travel as a bare binary body instead of JSON.</summary>
public static class BinaryPayload
{
	public const string DefaultContentType = "application/octet-stream";

	public static bool IsBinaryType(Type type)
		=> type == typeof(IOContent) || type == typeof(byte[]) || typeof(Stream).IsAssignableFrom(type);
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
