using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fuxion;

public static class PathStaticExtensions
{
	extension(FuxionExtensions<string?> me)
	{
		/// <summary>
		/// Provides path-related extension operations for this value.
		/// </summary>
		public PathExtensions Path
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension(PathExtensions me)
	{
		//public string _ => "";

		/// <summary>
		/// Determines whether the underlying path is a sub-path of the specified base directory path.
		/// The comparison normalizes path separators ('/' and '\').
		/// Only matches if the base directory folder name is matched exactly.
		/// </summary>
		/// <param name="baseDirPath">The base directory path to compare against.</param>
		/// <param name="comparisonType">
		/// The string comparison type to use. When <c>null</c>, defaults to <see cref="StringComparison.OrdinalIgnoreCase"/> 
		/// on Windows and <see cref="StringComparison.Ordinal"/> on Unix-based systems (case-sensitive file systems).
		/// </param>
		/// <returns>
		/// <c>true</c> when the underlying path starts with <paramref name="baseDirPath"/> and both paths are valid;
		/// <c>false</c> when the underlying path doesn't start with <paramref name="baseDirPath"/>,
		/// or when either the underlying value or <paramref name="baseDirPath"/> is <c>null</c>, empty, or only whitespace.
		/// </returns>
		/// <example>
		/// <code>
		/// // On Windows (case-insensitive by default):
		/// "c:\\foobar\\file.txt".Fx.Path.IsSubPathOf("c:\\foo")      // returns false (exact folder name match required)
		/// "c:\\foo\\bar\\file.txt".Fx.Path.IsSubPathOf("c:\\foo")    // returns true
		/// "c:\\FOO\\bar\\file.txt".Fx.Path.IsSubPathOf("c:\\foo")    // returns true (case-insensitive on Windows)
		/// "c:/foo/bar/file.txt".Fx.Path.IsSubPathOf("c:\\foo")       // returns true (handles mixed separators)
		/// 
		/// // On Unix (case-sensitive by default):
		/// "/home/user/file.txt".Fx.Path.IsSubPathOf("/home/user")    // returns true
		/// "/home/USER/file.txt".Fx.Path.IsSubPathOf("/home/user")    // returns false (case-sensitive on Unix)
		/// 
		/// // Explicit comparison type:
		/// "c:\\FOO\\file.txt".Fx.Path.IsSubPathOf("c:\\foo", StringComparison.Ordinal)  // returns false (case-sensitive)
		/// </code>
		/// </example>
		public bool IsSubPathOf(string baseDirPath, StringComparison? comparisonType = null)
		{
			if (me.Value.IsNullOrWhiteSpace())
				return false;
			if (baseDirPath.IsNullOrWhiteSpace())
				return false;

			// Default comparison based on OS
			var comparison = comparisonType ?? GetDefaultPathComparison();

			var normalizedPath = me.Value
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar)
				.EnsureEndsWith(Path.DirectorySeparatorChar.ToString());
			var normalizedBaseDirPath = baseDirPath
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar)
				.EnsureEndsWith(Path.DirectorySeparatorChar.ToString());

			return normalizedPath.StartsWith(normalizedBaseDirPath, comparison);
		}

		/// <summary>
		/// Gets the default string comparison type for path operations based on the current operating system.
		/// Returns <see cref="StringComparison.OrdinalIgnoreCase"/> on Windows (case-insensitive file system)
		/// and <see cref="StringComparison.Ordinal"/> on Unix-based systems (case-sensitive file systems).
		/// </summary>
		/// <returns>
		/// The default <see cref="StringComparison"/> for the current operating system.
		/// </returns>
		private static StringComparison GetDefaultPathComparison()
		{
#if NET5_0_OR_GREATER
			return OperatingSystem.IsWindows() 
				? StringComparison.OrdinalIgnoreCase 
				: StringComparison.Ordinal;
#else
			return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
#endif
		}

		/// <summary>
		/// Combines two path values using the system's directory separator character.
		/// Normalizes path separators ('/' and '\') to the system's <see cref="Path.DirectorySeparatorChar"/>
		/// and handles <c>null</c> values gracefully.
		/// </summary>
		/// <param name="s1">The first path segment (left operand).</param>
		/// <param name="s2">The second path segment (right operand).</param>
		/// <returns>
		/// The combined path with normalized separators. When both values are <c>null</c>, returns an empty string.
		/// When one value is <c>null</c>, returns the non-null value with normalized separators.
		/// When both values are non-null, uses <see cref="Path.Combine(string, string)"/> and normalizes the result.
		/// </returns>
		/// <example>
		/// <code>
		/// var path1 = "c:\\folder".Fx.Path;
		/// var path2 = "subfolder\\file.txt".Fx.Path;
		/// var combined = path1 / path2;  // returns "c:\folder\subfolder\file.txt" (on Windows)
		/// 
		/// var unixPath1 = "/home/user".Fx.Path;
		/// var unixPath2 = "documents/file.txt".Fx.Path;
		/// var unixCombined = unixPath1 / unixPath2;  // returns "/home/user/documents/file.txt" (on Unix)
		/// </code>
		/// </example>
		public static string operator /(PathExtensions s1, PathExtensions s2)
		{
			return (s1.Value, s2.Value) switch
			{
				(null, null) => string.Empty,
				(null, not null) => s2.Value.Replace('\\', Path.DirectorySeparatorChar)
					.Replace('/', Path.DirectorySeparatorChar),
				(not null, null) => s1.Value,
				(not null, not null) => Path.Combine(s1.Value, s2.Value)
					.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)
					.TrimEnd(Path.DirectorySeparatorChar)
			};
		}
	}
}

public class PathExtensions(string? me) : Extensions<string?>(me)
{
	public static implicit operator string?(PathExtensions str)
	{
		return str.Value;
	}

	public static implicit operator PathExtensions(string str)
	{
		return new(str);
	}
}
