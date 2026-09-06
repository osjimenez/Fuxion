namespace Test.Responses.Shared;

using System;
using System.IO;
using System.Text;
using Fuxion.Union;

/// <summary>A small deterministic file for the binary wire tests.</summary>
public static class TestFile
{
	/// <summary>The file's raw bytes.</summary>
	public static readonly byte[] Bytes = Encoding.UTF8.GetBytes("Fuxion binary payload 0123456789");
	/// <summary>The file's media type.</summary>
	public const string ContentType = "application/pdf";
	/// <summary>The file's name.</summary>
	public const string Name = "test.pdf";
	/// <summary>The file's last-modified timestamp.</summary>
	public static readonly DateTimeOffset LastModified = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
	/// <summary>The file's ETag.</summary>
	public const string ETag = "\"v1\"";

	/// <summary>Builds a <see cref="FileContent"/> for <see cref="Bytes"/> with range processing enabled.</summary>
	public static FileContent Create() => new(new MemoryStream(Bytes, writable: false), ContentType, Name)
	{
		LastModified = LastModified,
		ETag = ETag,
		EnableRangeProcessing = true
	};
}
