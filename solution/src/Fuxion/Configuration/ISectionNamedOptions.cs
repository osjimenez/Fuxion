namespace Fuxion.Configuration;

/// <summary>
/// Defines a contract for options types that expose the configuration section name they are bound to.
/// </summary>
/// <remarks>
/// Implement this interface on options classes when the section name should be discoverable from the type itself,
/// allowing registration and binding conventions to resolve the target configuration section without duplicating
/// the section identifier elsewhere.
/// </remarks>
public interface ISectionNamedOptions
{
	/// <summary>
	/// Gets the name of the configuration section associated with the current options type.
	/// </summary>
	/// <value>
	/// The section name used to locate the corresponding configuration values.
	/// </value>
	string SectionName { get; }
}
