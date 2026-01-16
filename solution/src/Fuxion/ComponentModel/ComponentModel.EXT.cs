using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Fuxion.ComponentModel;

/// <summary>
/// Provides extension methods for <see cref="PropertyDescriptor"/> to enhance property metadata handling.
/// </summary>
public static class PropertyDescriptorExtensions
{
	/// <summary>
	/// Gets the display name for a property, with priority given to the <see cref="DisplayAttribute"/> if present.
	/// </summary>
	/// <param name="me">The property descriptor to get the display name from.</param>
	/// <returns>
	/// The display name from the <see cref="DisplayAttribute"/> if present; 
	/// otherwise, the value from <see cref="PropertyDescriptor"/>.DisplayName.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method follows a fallback strategy for determining the display name:
	/// </para>
	/// <list type="number">
	/// <item><description>First, checks for a <see cref="DisplayAttribute"/> and uses its Name property</description></item>
	/// <item><description>If no <see cref="DisplayAttribute"/> is found, returns the default PropertyDescriptor.DisplayName</description></item>
	/// </list>
	/// <para>
	/// This is particularly useful when working with data-driven UI scenarios where property metadata
	/// is used for labels, headers, or other display purposes, and you want to support both 
	/// <see cref="DisplayAttribute"/> (from System.ComponentModel.DataAnnotations) and 
	/// <see cref="DisplayNameAttribute"/> (from System.ComponentModel).
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// public class Person
	/// {
	///     [Display(Name = "Full Name")]
	///     public string Name { get; set; }
	///     
	///     [DisplayName("Years Old")]
	///     public int Age { get; set; }
	///     
	///     public string Email { get; set; }
	/// }
	/// 
	/// // Using PropertyDescriptor
	/// var properties = TypeDescriptor.GetProperties(typeof(Person));
	/// 
	/// var nameDescriptor = properties["Name"];
	/// var displayName1 = nameDescriptor.GetDisplayName(); // Returns: "Full Name" (from DisplayAttribute)
	/// 
	/// var ageDescriptor = properties["Age"];
	/// var displayName2 = ageDescriptor.GetDisplayName(); // Returns: "Years Old" (from DisplayNameAttribute)
	/// 
	/// var emailDescriptor = properties["Email"];
	/// var displayName3 = emailDescriptor.GetDisplayName(); // Returns: "Email" (default PropertyDescriptor.DisplayName)
	/// 
	/// // Real-world usage: generating UI labels
	/// foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(typeof(Person)))
	/// {
	///     var label = prop.GetDisplayName();
	///     Console.WriteLine($"Label for {prop.Name}: {label}");
	/// }
	/// </code>
	/// </example>
	public static string GetDisplayName(this PropertyDescriptor me)
	{
		var att = me.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
		return att?.GetName() ?? me.DisplayName;
	}
}