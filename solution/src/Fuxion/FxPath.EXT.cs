using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fuxion;

	/// <summary>
	/// Provides extension methods for path manipulation and comparison using the Fuxion fluent API pattern.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This static class contains extension methods that enable fluent-style path operations through the <c>.Fx.Path</c> syntax.
	/// The extensions provide:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Path comparison:</strong> Check if a path is a subpath of another with OS-aware case sensitivity</description></item>
	/// <item><description><strong>Path combination:</strong> Fluent path concatenation using the <c>/</c> operator with automatic separator normalization</description></item>
	/// <item><description><strong>Separator normalization:</strong> Automatic handling of mixed path separators ('/' and '\')</description></item>
	/// <item><description><strong>OS-aware behavior:</strong> Automatic case-sensitive/insensitive comparison based on the operating system</description></item>
	/// </list>
	/// <para>
	/// <strong>Fluent API pattern:</strong>
	/// </para>
	/// <para>
	/// The extensions use the Fuxion extension pattern: <c>path.Fx.Path.Operation()</c> where:
	/// </para>
	/// <list type="bullet">
	/// <item><description><c>.Fx</c> - Entry point to Fuxion extensions (from <see cref="FuxionExtensions{T}"/>)</description></item>
	/// <item><description><c>.Path</c> - Accesses the path operations wrapper</description></item>
	/// <item><description><c>.Operation()</c> - Performs path operations like <see cref="IsSubPathOf"/></description></item>
	/// </list>
	/// <para>
	/// <strong>Cross-platform considerations:</strong>
	/// </para>
	/// <para>
	/// The path operations automatically adapt to the underlying operating system:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Windows:</strong> Case-insensitive comparisons by default, supports both '\' and '/' separators</description></item>
	/// <item><description><strong>Unix/Linux:</strong> Case-sensitive comparisons by default, uses '/' as the standard separator</description></item>
	/// <item><description><strong>macOS:</strong> Case-insensitive by default (configurable at filesystem level), uses '/' separator</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <strong>Basic path comparison:</strong>
	/// <code>
	/// // Check if a path is a subpath (Windows)
	/// bool isSubPath = @"c:\projects\myapp\bin\file.dll".Fx.Path.IsSubPathOf(@"c:\projects\myapp");
	/// // Result: true
	/// 
	/// // Unix path comparison
	/// bool isUnixSubPath = "/home/user/documents/file.txt".Fx.Path.IsSubPathOf("/home/user");
	/// // Result: true
	/// </code>
	/// 
	/// <strong>Path combination with operator:</strong>
	/// <code>
	/// var basePath = @"c:\projects".Fx.Path;
	/// var subPath = @"myapp\src".Fx.Path;
	/// var combined = basePath / subPath;
	/// // Result on Windows: "c:\projects\myapp\src"
	/// 
	/// // Handles mixed separators
	/// var mixedBase = "c:/projects".Fx.Path;
	/// var mixedSub = @"myapp\bin".Fx.Path;
	/// var normalized = mixedBase / mixedSub;
	/// // Result on Windows: "c:\projects\myapp\bin"
	/// </code>
	/// 
	/// <strong>OS-aware comparison:</strong>
	/// <code>
	/// // On Windows (case-insensitive by default)
	/// "C:\\FOO\\bar".Fx.Path.IsSubPathOf("c:\\foo");  // true
	/// 
	/// // On Linux (case-sensitive by default)
	/// "/home/USER/file".Fx.Path.IsSubPathOf("/home/user");  // false
	/// 
	/// // Override default with explicit comparison
	/// "C:\\FOO\\bar".Fx.Path.IsSubPathOf("c:\\foo", StringComparison.Ordinal);  // false
	/// </code>
	/// </example>
public static class PathStaticExtensions
{
	extension(FuxionExtensions<string?> me)
	{
		/// <summary>
		/// Provides path-related extension operations for this value.
		/// </summary>
		/// <value>
		/// A <see cref="PathExtensions"/> instance wrapping the current string value for path operations.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property serves as the entry point to path-specific operations in the Fuxion fluent API.
		/// It creates a <see cref="PathExtensions"/> wrapper around the string value, enabling methods like
		/// <see cref="IsSubPathOf"/> and the <c>/</c> operator for path combination.
		/// </para>
		/// <para>
		/// The method is marked with <see cref="MethodImplOptions.AggressiveInlining"/> for optimal performance,
		/// as it's a simple wrapper creation that's called frequently in path operations.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// string path = @"c:\projects\myapp\file.txt";
		/// 
		/// // Access path operations
		/// var pathOps = path.Fx.Path;
		/// 
		/// // Use in fluent chain
		/// bool isSubPath = path.Fx.Path.IsSubPathOf(@"c:\projects");
		/// 
		/// // Combine paths
		/// var combined = @"c:\base".Fx.Path / "subfolder".Fx.Path;
		/// </code>
		/// </example>
		public PathExtensions Path
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension(PathExtensions me)
	{
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
		/// <remarks>
		/// <para>
		/// This method performs several normalization steps to ensure accurate path comparison:
		/// </para>
		/// <list type="number">
		/// <item><description><strong>Null/whitespace check:</strong> Returns <c>false</c> if either path is null, empty, or whitespace</description></item>
		/// <item><description><strong>OS detection:</strong> Determines the default comparison type based on <see cref="OperatingSystem"/>.IsWindows (.NET 5+) or <see cref="RuntimeInformation.IsOSPlatform"/> (.NET Standard/Framework)</description></item>
		/// <item><description><strong>Separator normalization:</strong> Replaces both '\' and '/' with <see cref="Path.DirectorySeparatorChar"/></description></item>
		/// <item><description><strong>Trailing separator:</strong> Ensures both paths end with a directory separator to prevent partial folder name matches</description></item>
		/// <item><description><strong>String comparison:</strong> Uses <see cref="string.StartsWith(string, StringComparison)"/> with the determined comparison type</description></item>
		/// </list>
		/// <para>
		/// <strong>Exact folder name matching:</strong>
		/// </para>
		/// <para>
		/// The method ensures trailing separators on both paths to prevent false positives from partial folder name matches.
		/// For example, <c>"c:\foobar\file.txt"</c> will NOT match <c>"c:\foo"</c> because <c>"c:\foobar\"</c> does not start
		/// with <c>"c:\foo\"</c>.
		/// </para>
		/// <para>
		/// <strong>Platform-specific default behavior:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description><strong>Windows:</strong> Uses <see cref="StringComparison.OrdinalIgnoreCase"/> (case-insensitive) because NTFS/FAT32 are case-insensitive</description></item>
		/// <item><description><strong>Linux/Unix:</strong> Uses <see cref="StringComparison.Ordinal"/> (case-sensitive) because ext4/XFS are case-sensitive</description></item>
		/// <item><description><strong>macOS:</strong> Uses <see cref="StringComparison.Ordinal"/> by default, though HFS+/APFS can be configured for case-insensitivity</description></item>
		/// </list>
		/// <para>
		/// <strong>Mixed separator handling:</strong>
		/// </para>
		/// <para>
		/// The method normalizes both forward slashes ('/') and backslashes ('\') to the system's native
		/// <see cref="Path.DirectorySeparatorChar"/>, allowing cross-platform path strings to be compared correctly.
		/// </para>
		/// </remarks>
		/// <example>
		/// <strong>Basic usage on Windows:</strong>
		/// <code>
		/// // Standard subpath check
		/// bool result1 = @"c:\projects\myapp\bin\file.dll".Fx.Path.IsSubPathOf(@"c:\projects");
		/// // Result: true
		/// 
		/// // Partial folder name (not a match)
		/// bool result2 = @"c:\foobar\file.txt".Fx.Path.IsSubPathOf(@"c:\foo");
		/// // Result: false (prevents false positive - "foobar" is not a subfolder of "foo")
		/// 
		/// // Case-insensitive (Windows default)
		/// bool result3 = @"C:\PROJECTS\MyApp\file.txt".Fx.Path.IsSubPathOf(@"c:\projects");
		/// // Result: true
		/// </code>
		/// 
		/// <strong>Unix/Linux usage:</strong>
		/// <code>
		/// // Case-sensitive comparison (Unix default)
		/// bool result1 = "/home/user/documents/file.txt".Fx.Path.IsSubPathOf("/home/user");
		/// // Result: true
		/// 
		/// bool result2 = "/home/USER/documents/file.txt".Fx.Path.IsSubPathOf("/home/user");
		/// // Result: false (case mismatch on Unix)
		/// </code>
		/// 
		/// <strong>Mixed separators:</strong>
		/// <code>
		/// // Windows - handles forward slashes
		/// bool result1 = "c:/projects/myapp/file.txt".Fx.Path.IsSubPathOf(@"c:\projects");
		/// // Result: true (separators are normalized)
		/// 
		/// // Mix of both separator types
		/// bool result2 = @"c:\projects/myapp\bin/file.dll".Fx.Path.IsSubPathOf("c:/projects");
		/// // Result: true (all separators normalized)
		/// </code>
		/// 
		/// <strong>Explicit comparison type:</strong>
		/// <code>
		/// // Force case-sensitive comparison on Windows
		/// bool result1 = @"C:\PROJECTS\file.txt".Fx.Path.IsSubPathOf(
		///     @"c:\projects", 
		///     StringComparison.Ordinal
		/// );
		/// // Result: false (case mismatch with Ordinal comparison)
		/// 
		/// // Force case-insensitive comparison on Unix
		/// bool result2 = "/home/USER/file.txt".Fx.Path.IsSubPathOf(
		///     "/home/user",
		///     StringComparison.OrdinalIgnoreCase
		/// );
		/// // Result: true (case-insensitive override)
		/// </code>
		/// 
		/// <strong>Edge cases:</strong>
		/// <code>
		/// // Null or empty paths
		/// bool result1 = ((string?)null).Fx.Path.IsSubPathOf(@"c:\base");
		/// // Result: false
		/// 
		/// bool result2 = @"c:\path\file.txt".Fx.Path.IsSubPathOf("");
		/// // Result: false
		/// 
		/// // Whitespace-only
		/// bool result3 = "   ".Fx.Path.IsSubPathOf(@"c:\base");
		/// // Result: false
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
		/// <remarks>
		/// <para>
		/// This method uses conditional compilation to provide optimal OS detection based on the target framework:
		/// </para>
		/// <list type="bullet">
		/// <item><description><strong>.NET 5+:</strong> Uses <see cref="OperatingSystem"/>.IsWindows for efficient, inline OS detection</description></item>
		/// <item><description><strong>.NET Standard 2.0/.NET Framework:</strong> Uses <see cref="RuntimeInformation.IsOSPlatform"/> with <see cref="OSPlatform.Windows"/></description></item>
		/// </list>
		/// <para>
		/// <strong>Platform defaults:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description><strong>Windows (NTFS, FAT32):</strong> Returns <see cref="StringComparison.OrdinalIgnoreCase"/> because Windows filesystems are case-insensitive (though they preserve case)</description></item>
		/// <item><description><strong>Linux (ext4, XFS, btrfs):</strong> Returns <see cref="StringComparison.Ordinal"/> because Linux filesystems are case-sensitive</description></item>
		/// <item><description><strong>macOS (APFS, HFS+):</strong> Returns <see cref="StringComparison.Ordinal"/> by default, though APFS can be formatted as case-insensitive</description></item>
		/// </list>
		/// <para>
		/// <strong>Design note:</strong> This method is private because it's an implementation detail of path comparison.
		/// External callers should use the <c>comparisonType</c> parameter in <see cref="IsSubPathOf"/> if they need
		/// to override the default behavior.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Internal usage in IsSubPathOf
		/// private bool IsSubPathOfInternal(string basePath)
		/// {
		///     var comparison = GetDefaultPathComparison();
		///     // On Windows: comparison == StringComparison.OrdinalIgnoreCase
		///     // On Linux: comparison == StringComparison.Ordinal
		///     
		///     return normalizedPath.StartsWith(normalizedBasePath, comparison);
		/// }
		/// </code>
		/// </example>
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
		/// <remarks>
		/// <para>
		/// This operator provides fluent path combination syntax using the division operator (<c>/</c>), which is
		/// intuitive for path operations. The operator implementation uses pattern matching to handle all combinations
		/// of null and non-null values:
		/// </para>
		/// <list type="table">
		/// <listheader>
		/// <term>s1</term>
		/// <term>s2</term>
		/// <description>Result</description>
		/// </listheader>
		/// <item>
		/// <term><c>null</c></term>
		/// <term><c>null</c></term>
		/// <description><c>string.Empty</c></description>
		/// </item>
		/// <item>
		/// <term><c>null</c></term>
		/// <term>non-null</term>
		/// <description><c>s2</c> with normalized separators</description>
		/// </item>
		/// <item>
		/// <term>non-null</term>
		/// <term><c>null</c></term>
		/// <description><c>s1</c> as-is (no modification)</description>
		/// </item>
		/// <item>
		/// <term>non-null</term>
		/// <term>non-null</term>
		/// <description><see cref="Path.Combine(string, string)"/> result with normalized separators and trimmed trailing separator</description>
		/// </item>
		/// </list>
		/// <para>
		/// <strong>Normalization process:</strong>
		/// </para>
		/// <para>
		/// The operator performs the following normalizations on the combined path:
		/// </para>
		/// <list type="number">
		/// <item><description><strong>Path combination:</strong> Uses <see cref="Path.Combine(string[])"/> for correct platform-specific path joining</description></item>
		/// <item><description><strong>Backslash replacement:</strong> Replaces '\' with <see cref="Path.DirectorySeparatorChar"/></description></item>
		/// <item><description><strong>Forward slash replacement:</strong> Replaces '/' with <see cref="Path.DirectorySeparatorChar"/></description></item>
		/// <item><description><strong>Trailing separator removal:</strong> Trims the final directory separator using <see cref="string.TrimEnd(char[])"/></description></item>
		/// </list>
		/// <para>
		/// <strong>Why use <c>/</c> operator for paths?</strong>
		/// </para>
		/// <para>
		/// The division operator is chosen for its visual similarity to Unix path separators and its availability
		/// as a binary operator in C#. It provides a fluent, readable syntax for building paths without explicit
		/// method calls.
		/// </para>
		/// <para>
		/// <strong>Implicit conversions:</strong>
		/// </para>
		/// <para>
		/// <see cref="PathExtensions"/> provides implicit conversions to and from <see cref="string"/>, allowing
		/// the result to be used directly as a string or assigned back to a string variable.
		/// </para>
		/// </remarks>
		/// <example>
		/// <strong>Basic path combination:</strong>
		/// <code>
		/// var path1 = @"c:\projects".Fx.Path;
		/// var path2 = "myapp".Fx.Path;
		/// var combined = path1 / path2;
		/// // Result on Windows: "c:\projects\myapp"
		/// 
		/// // Multiple segments
		/// var basePath = @"c:\projects".Fx.Path;
		/// var subPath1 = "myapp".Fx.Path;
		/// var subPath2 = "src".Fx.Path;
		/// var subPath3 = "Program.cs".Fx.Path;
		/// var fullPath = basePath / subPath1 / subPath2 / subPath3;
		/// // Result on Windows: "c:\projects\myapp\src\Program.cs"
		/// </code>
		/// 
		/// <strong>Mixed separator handling:</strong>
		/// <code>
		/// // Input has forward slashes (Unix-style)
		/// var unixStyle = "home/user/documents".Fx.Path;
		/// var file = "file.txt".Fx.Path;
		/// var result = unixStyle / file;
		/// // Result on Windows: "home\user\documents\file.txt"
		/// // Result on Linux: "home/user/documents/file.txt"
		/// 
		/// // Input has backslashes (Windows-style)
		/// var windowsStyle = @"c:\temp\data".Fx.Path;
		/// var subdir = "output".Fx.Path;
		/// var normalized = windowsStyle / subdir;
		/// // Result on Windows: "c:\temp\data\output"
		/// // Result on Linux: "c:/temp/data/output" (separators normalized)
		/// 
		/// // Mixed separators in input
		/// var mixed1 = "c:/projects".Fx.Path;
		/// var mixed2 = @"myapp\bin".Fx.Path;
		/// var combined = mixed1 / mixed2;
		/// // Result on Windows: "c:\projects\myapp\bin"
		/// </code>
		/// 
		/// <strong>Null handling:</strong>
		/// <code>
		/// var path = @"c:\base".Fx.Path;
		/// var nullPath = ((string?)null).Fx.Path;
		/// 
		/// // Left side null
		/// var result1 = nullPath / path;
		/// // Result: "c:\base" (with normalized separators)
		/// 
		/// // Right side null
		/// var result2 = path / nullPath;
		/// // Result: "c:\base"
		/// 
		/// // Both null
		/// var result3 = nullPath / nullPath;
		/// // Result: "" (empty string)
		/// </code>
		/// 
		/// <strong>Chaining multiple paths:</strong>
		/// <code>
		/// // Build deep path hierarchy fluently
		/// var projectRoot = @"c:\dev".Fx.Path;
		/// var solution = "MySolution".Fx.Path;
		/// var project = "MyProject".Fx.Path;
		/// var folder = "Controllers".Fx.Path;
		/// var file = "HomeController.cs".Fx.Path;
		/// 
		/// var fullPath = projectRoot / solution / project / folder / file;
		/// // Result: "c:\dev\MySolution\MyProject\Controllers\HomeController.cs"
		/// 
		/// // Can mix with string literals
		/// string finalPath = projectRoot / solution / "src" / project;
		/// // Implicit conversion to string
		/// </code>
		/// 
		/// <strong>Cross-platform paths:</strong>
		/// <code>
		/// // Same code works on different platforms
		/// var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Fx.Path;
		/// var documents = "Documents".Fx.Path;
		/// var projectFolder = "MyProject".Fx.Path;
		/// 
		/// var projectPath = userHome / documents / projectFolder;
		/// // Windows result: "C:\Users\Username\Documents\MyProject"
		/// // Linux result: "/home/username/Documents/MyProject"
		/// // macOS result: "/Users/username/Documents/MyProject"
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

/// <summary>
/// Wrapper class for path extension operations on string values.
/// </summary>
/// <param name="me">The string value representing the path to wrap for path-specific operations.</param>
/// <remarks>
/// <para>
/// This class extends <see cref="Extensions{T}"/> to provide a type-safe wrapper for path operations on strings.
/// It's accessed through the <c>.Fx.Path</c> fluent API pattern and provides:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Path comparison:</strong> 'IsSubPathOf' method for subpath checking</description></item>
/// <item><description><strong>Path combination:</strong> <c>/</c> operator for fluent path concatenation</description></item>
/// <item><description><strong>Implicit conversions:</strong> Seamless conversion between <see cref="PathExtensions"/> and <see cref="string"/></description></item>
/// </list>
/// <para>
/// <strong>Design pattern:</strong> This class uses the primary constructor syntax (C# 12+) to create an immutable
/// wrapper around a string value. The base class <see cref="Extensions{T}"/> stores the value and provides it to
/// extension methods defined in <see cref="PathStaticExtensions"/>.
/// </para>
/// <para>
/// <strong>Implicit conversions:</strong>
/// </para>
/// <para>
/// The class provides bidirectional implicit conversion operators:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="op_Implicit(PathExtensions)"/> - Convert <see cref="PathExtensions"/> to <see cref="string"/></description></item>
/// <item><description><see cref="op_Implicit(string)"/> - Convert <see cref="string"/> to <see cref="PathExtensions"/></description></item>
/// </list>
/// <para>
/// These conversions allow <see cref="PathExtensions"/> instances to be used interchangeably with strings in most contexts,
/// including assignments, method parameters, and return values.
/// </para>
/// <para>
/// <strong>Null handling:</strong> The wrapper accepts <c>null</c> string values. Path operations like 'IsSubPathOf'/>
/// handle null values gracefully by returning <c>false</c>, and the <c>/</c> operator follows specific rules for
/// null operands (see operator documentation).
/// </para>
/// <para>
/// <strong>Usage:</strong> Instances of this class are typically created automatically by the <c>.Fx.Path</c> property
/// in the fluent API. Direct instantiation is rare but supported.
/// </para>
/// </remarks>
/// <example>
/// <strong>Typical usage through fluent API:</strong>
/// <code>
/// // Path comparison
/// string path = @"c:\projects\myapp\file.txt";
/// bool isSubPath = path.Fx.Path.IsSubPathOf(@"c:\projects");
/// // Behind the scenes:
/// // 1. path.Fx returns FuxionExtensions&lt;string&gt;
/// // 2. .Path returns new PathExtensions(path)
/// // 3. .IsSubPathOf() is an extension method on PathExtensions
/// 
/// // Path combination
/// var basePath = @"c:\base".Fx.Path;
/// var subPath = "subfolder".Fx.Path;
/// var combined = basePath / subPath;
/// // Result: "c:\base\subfolder"
/// </code>
/// 
/// <strong>Implicit conversions:</strong>
/// <code>
/// // String to PathExtensions (implicit)
/// PathExtensions pathExt = @"c:\projects\myapp";
/// 
/// // PathExtensions to string (implicit)
/// string pathString = pathExt;
/// 
/// // Direct assignment after operator
/// string result = @"c:\base".Fx.Path / "sub".Fx.Path;
/// // PathExtensions result is implicitly converted to string
/// 
/// // Use in method expecting string
/// void ProcessPath(string path) { /* ... */ }
/// ProcessPath(@"c:\temp".Fx.Path / "file.txt".Fx.Path);
/// // PathExtensions is implicitly converted to string parameter
/// </code>
/// 
/// <strong>Direct instantiation (rarely needed):</strong>
/// <code>
/// // Create wrapper directly
/// var wrapper = new PathExtensions(@"c:\mypath");
/// 
/// // Use extension methods
/// bool isSubPath = wrapper.IsSubPathOf(@"c:\")
/// 
/// // Combine with operator
/// var combined = wrapper / new PathExtensions("subfolder");
/// 
/// // Convert back to string
/// string finalPath = combined;
/// </code>
/// 
/// <strong>Null value handling:</strong>
/// <code>
/// // Wrapper accepts null
/// PathExtensions nullPath = new PathExtensions(null);
/// 
/// // Operations handle null gracefully
/// bool result = nullPath.IsSubPathOf(@"c:\base");  // false
/// 
/// // Operator handles null
/// var combined1 = nullPath / @"c:\path".Fx.Path;  // "c:\path"
/// var combined2 = @"c:\path".Fx.Path / nullPath;  // "c:\path"
/// 
/// // Implicit conversion of null
/// string? nullString = null;
/// PathExtensions wrapper = nullString;  // wrapper.Value is null
/// </code>
/// </example>
public class PathExtensions(string? me) : Extensions<string?>(me)
{
	/// <summary>
	/// Implicitly converts a <see cref="PathExtensions"/> instance to a <see cref="string"/>.
	/// </summary>
	/// <param name="str">The <see cref="PathExtensions"/> instance to convert.</param>
	/// <returns>
	/// The underlying string value of the <see cref="PathExtensions"/> instance, which may be <c>null</c>.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This implicit conversion operator allows <see cref="PathExtensions"/> instances to be used anywhere a
	/// <see cref="string"/> is expected without explicit casting. This provides seamless integration with existing
	/// code that works with string paths.
	/// </para>
	/// <para>
	/// <strong>Null handling:</strong> If the <see cref="PathExtensions"/> instance wraps a <c>null</c> value,
	/// the conversion returns <c>null</c>.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var pathExt = @"c:\projects\myapp".Fx.Path / "file.txt".Fx.Path;
	/// 
	/// // Implicit conversion in assignment
	/// string pathString = pathExt;
	/// 
	/// // Implicit conversion in method call
	/// void SaveFile(string path) { /* ... */ }
	/// SaveFile(pathExt);  // PathExtensions implicitly converts to string
	/// 
	/// // Implicit conversion in string interpolation
	/// Console.WriteLine($"Path: {pathExt}");
	/// 
	/// // Works with null values
	/// PathExtensions nullPath = new PathExtensions(null);
	/// string? nullString = nullPath;  // nullString is null
	/// </code>
	/// </example>
	public static implicit operator string?(PathExtensions str)
	{
		return str.Value;
	}

	/// <summary>
	/// Implicitly converts a <see cref="string"/> to a <see cref="PathExtensions"/> instance.
	/// </summary>
	/// <param name="str">The string value to wrap in a <see cref="PathExtensions"/> instance.</param>
	/// <returns>
	/// A new <see cref="PathExtensions"/> instance wrapping the specified string value.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This implicit conversion operator allows strings to be used directly in path operations without explicitly
	/// creating <see cref="PathExtensions"/> instances. This simplifies path combination syntax when mixing
	/// strings with <see cref="PathExtensions"/> instances.
	/// </para>
	/// <para>
	/// <strong>Null handling:</strong> The conversion accepts <c>null</c> strings and creates a <see cref="PathExtensions"/>
	/// instance that wraps <c>null</c>.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Direct assignment from string
	/// PathExtensions pathExt = @"c:\projects";
	/// 
	/// // Use in path combination without explicit wrapping
	/// var basePath = @"c:\base".Fx.Path;
	/// PathExtensions subPath = "subfolder";  // Implicit conversion
	/// var combined = basePath / subPath;
	/// 
	/// // Mix strings and PathExtensions in operators
	/// var path1 = @"c:\projects".Fx.Path;
	/// var result = path1 / "myapp" / "bin" / "Debug";
	/// // Each string literal is implicitly converted to PathExtensions for the operator
	/// 
	/// // Works with null
	/// string? nullString = null;
	/// PathExtensions nullPath = nullString;  // Implicit conversion of null
	/// </code>
	/// </example>
	public static implicit operator PathExtensions(string str)
	{
		return new(str);
	}
}
