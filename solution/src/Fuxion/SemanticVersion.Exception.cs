namespace Fuxion;

/// <summary>
/// Represents errors that occur during semantic version parsing or validation.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when a string cannot be parsed as a valid semantic version
/// according to the Semantic Versioning 2.0.0 specification. It inherits from <see cref="FuxionException"/>
/// to maintain consistency with Fuxion's exception hierarchy.
/// </para>
/// <para>
/// Common causes for this exception:
/// </para>
/// <list type="bullet">
/// <item><description>Invalid version format (e.g., missing components like "1.0" instead of "1.0.0")</description></item>
/// <item><description>Non-numeric major, minor, or patch components (e.g., "v1.0.0" with 'v' prefix)</description></item>
/// <item><description>Leading zeros in numeric components (e.g., "01.0.0" instead of "1.0.0")</description></item>
/// <item><description>Empty or invalid pre-release identifiers (e.g., "1.0.0-" with trailing hyphen)</description></item>
/// <item><description>Empty or invalid build metadata identifiers (e.g., "1.0.0+" with trailing plus)</description></item>
/// <item><description>Invalid characters in identifiers (only [0-9A-Za-z-] allowed)</description></item>
/// </list>
/// <para>
/// <strong>Best practice:</strong> Use <see cref="SemanticVersion.TryParse"/> instead of the constructor
/// when parsing user input or external data to avoid throwing exceptions for validation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // This will throw SemanticVersionException
/// try
/// {
///     var version = new SemanticVersion("v1.0.0"); // 'v' prefix not allowed
/// }
/// catch (SemanticVersionException ex)
/// {
///     Console.WriteLine($"Invalid version: {ex.Message}");
///     // Output: Invalid version: String 'v1.0.0' isn't a valid semantic version pattern
/// }
/// 
/// // Better approach using TryParse
/// if (!SemanticVersion.TryParse("v1.0.0", out var version))
/// {
///     Console.WriteLine("Invalid version format");
/// }
/// 
/// // Other invalid formats that throw this exception:
/// // new SemanticVersion("1.0");           // Missing patch version
/// // new SemanticVersion("01.0.0");        // Leading zero
/// // new SemanticVersion("1.0.0-");        // Empty prerelease
/// // new SemanticVersion("1.0.0+");        // Empty build metadata
/// // new SemanticVersion("1.0.0-alpha@");  // Invalid character '@'
/// </code>
/// </example>
/// <seealso cref="SemanticVersion"/>
/// <seealso cref="SemanticVersion.TryParse"/>
/// <seealso cref="FuxionException"/>
public class SemanticVersionException(string message) : FuxionException(message);