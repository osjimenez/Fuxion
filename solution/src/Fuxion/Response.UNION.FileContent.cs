namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.IO;

/// <summary>
/// A binary payload that describes itself: the stream plus the HTTP metadata a file needs on the wire.
/// A file is the purest self-describing HTTP message, so nothing Fuxion is added to it: the media type
/// is the file's own, the name travels in Content-Disposition, and caching/ranges use the standard
/// headers. It never travels inside the response envelope.
/// </summary>
public sealed class FileContent : IDisposable
{
	public FileContent(Stream stream, string? contentType = null, string? fileName = null)
	{
		Stream = stream ?? throw new ArgumentNullException(nameof(stream));
		ContentType = string.IsNullOrWhiteSpace(contentType) ? BinaryPayload.DefaultContentType : contentType!;
		FileName = fileName;
		Length = stream.CanSeek ? stream.Length : null;
	}

	/// <summary>
	/// The content. Consumed (and disposed) by whoever writes or reads the response. Must be positioned
	/// at the start: the server writes from the current position while the length comes from the stream.
	/// </summary>
	public Stream Stream { get; }

	/// <summary>The file's own media type; <c>application/octet-stream</c> when unknown.</summary>
	public string ContentType { get; init; }

	/// <summary>
	/// Suggested download name (Content-Disposition). Null for an inline body without a name.
	/// On the client this is the server's suggestion: sanitize it before using it as a file-system path.
	/// </summary>
	public string? FileName { get; init; }

	/// <summary>
	/// Content length when known; taken from a seekable stream by default. On the server the framework
	/// derives Content-Length and Range support from the stream itself (seekable streams only): this
	/// value is NOT sent. On the client it is what Content-Length announced.
	/// </summary>
	public long? Length { get; init; }

	public DateTimeOffset? LastModified { get; init; }

	/// <summary>Entity tag including its quotes, e.g. <c>"v1"</c>.</summary>
	public string? ETag { get; init; }

	/// <summary>
	/// Whether the server should honour Range requests (206) for this content. Range requests can only
	/// be served when the stream is seekable; otherwise the body is written in full.
	/// </summary>
	public bool EnableRangeProcessing { get; init; }

	/// <summary>Wraps an in-memory array. Bytes are already materialized, so this is never a streaming case.</summary>
	public static FileContent FromBytes(byte[] bytes, string? contentType = null, string? fileName = null)
		=> new(new MemoryStream(bytes ?? throw new ArgumentNullException(nameof(bytes)), writable: false), contentType, fileName);

	public void Dispose() => Stream.Dispose();
}

/// <summary>Which success types travel as a bare binary body instead of JSON.</summary>
public static class BinaryPayload
{
	public const string DefaultContentType = "application/octet-stream";

	public static bool IsBinaryType(Type type)
		=> type == typeof(FileContent) || type == typeof(byte[]) || typeof(Stream).IsAssignableFrom(type);
}
