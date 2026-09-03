namespace Test.AspNetCore.Service;

using System;
using System.IO;
using System.Text;
using Fuxion.Union;

/// <summary>A small deterministic file for the binary wire tests.</summary>
public static class TestFile
{
	public static readonly byte[] Bytes = Encoding.UTF8.GetBytes("Fuxion binary payload 0123456789");
	public const string ContentType = "application/pdf";
	public const string Name = "test.pdf";
	public static readonly DateTimeOffset LastModified = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
	public const string ETag = "\"v1\"";

	public static FileContent Create() => new(new MemoryStream(Bytes, writable: false), ContentType, Name)
	{
		LastModified = LastModified,
		ETag = ETag,
		EnableRangeProcessing = true
	};
}
