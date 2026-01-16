using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Fuxion.Windows;

/// <summary>
///    Represents an enumeration value with a human-readable name derived from the <see cref="DisplayAttribute"/>.
/// </summary>
/// <param name="value">The enumeration value to wrap.</param>
/// <remarks>
///    <para>
///       This readonly struct provides a wrapper around any <see cref="Enum"/> type that automatically
///       extracts and exposes a user-friendly display name. The name is obtained from the
///       <see cref="DisplayAttribute.Name"/> property if present, otherwise it falls back to
///       the enum value's string representation.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic name extraction:</strong> Reads display names from <see cref="DisplayAttribute"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Implicit conversions:</strong> Seamlessly converts between <see cref="Enum"/> and <see cref="NamedEnumValue"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Equality support:</strong> Provides operators and methods for comparing with enum values
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>WPF binding friendly:</strong> ToString() returns the display name for easy binding
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Immutable:</strong> Readonly struct ensures thread safety and prevents modification
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>WPF ComboBox or ListBox data binding with localized enum names</description>
///       </item>
///       <item>
///          <description>UI components that need to display user-friendly enum values</description>
///       </item>
///       <item>
///          <description>Data grids showing enum columns with custom display text</description>
///       </item>
///       <item>
///          <description>Forms and dialogs that present enum options to users</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Define an enum with display attributes:</strong>
///    <code>
/// public enum Status
/// {
///     [Display(Name = "Pending Approval")]
///     PendingApproval,
///     
///     [Display(Name = "Approved")]
///     Approved,
///     
///     [Display(Name = "Rejected")]
///     Rejected,
///     
///     // Without Display attribute, falls back to "Cancelled"
///     Cancelled
/// }
/// </code>
///    <strong>Use in WPF XAML binding:</strong>
///    <code><![CDATA[
/// <!-- ViewModel property -->
/// public IEnumerable<NamedEnumValue> StatusOptions { get; } = 
///     Enum.GetValues<Status>().Select(s => (NamedEnumValue)s);
/// 
/// public NamedEnumValue SelectedStatus { get; set; }
/// 
/// <!-- XAML -->
/// <ComboBox ItemsSource="{Binding StatusOptions}" 
///           SelectedItem="{Binding SelectedStatus}"
///           DisplayMemberPath="Name"/>
/// ]]></code>
///    <strong>Basic usage:</strong>
///    <code>
/// Status status = Status.PendingApproval;
/// NamedEnumValue namedValue = status; // Implicit conversion
/// 
/// Console.WriteLine(namedValue.Name);  // Output: "Pending Approval"
/// Console.WriteLine(namedValue);       // Output: "Pending Approval" (calls ToString())
/// 
/// Status backToEnum = namedValue;      // Implicit conversion back
/// </code>
///    <strong>Comparison operations:</strong>
///    <code>
/// NamedEnumValue namedValue = Status.Approved;
/// 
/// if (namedValue == Status.Approved)
/// {
///     Console.WriteLine("Status is approved");
/// }
/// 
/// if (Status.Rejected != namedValue)
/// {
///     Console.WriteLine("Status is not rejected");
/// }
/// </code>
///    <strong>Create collection for binding:</strong>
///    <code>
/// // Extension method to get all named values
/// public static IEnumerable&lt;NamedEnumValue&gt; GetNamedValues(this Type enumType)
/// {
///     if (!enumType.IsEnum)
///         throw new ArgumentException("Type must be an enum", nameof(enumType));
///         
///     return Enum.GetValues(enumType)
///         .Cast&lt;Enum&gt;()
///         .Select(e => (NamedEnumValue)e);
/// }
/// 
/// // Usage
/// var statusList = typeof(Status).GetNamedValues().ToList();
/// </code>
/// </example>
public readonly struct NamedEnumValue(Enum value) : IEquatable<Enum>
{
	/// <summary>
	///    Gets the human-readable display name for the enumeration value.
	/// </summary>
	/// <value>
	///    The name from the <see cref="DisplayAttribute"/> if present; otherwise,
	///    the result of calling <see cref="object.ToString"/> on the enum value.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property uses reflection to retrieve the <see cref="DisplayAttribute"/>
	///       from the enum field. The lookup is performed only once during construction.
	///    </para>
	///    <para>
	///       If the enum field has a <see cref="DisplayAttribute"/>, the value from
	///       <see cref="DisplayAttribute.GetName"/> is used. Otherwise, the enum's
	///       name (e.g., "PendingApproval") is used as-is.
	///    </para>
	/// </remarks>
	public string Name { get; } = value.GetType()
		.GetField(value.ToString())
		?.GetCustomAttribute<DisplayAttribute>(false)
		?.GetName() ?? value.ToString();

	/// <summary>
	///    Gets the underlying enumeration value.
	/// </summary>
	/// <value>
	///    The <see cref="Enum"/> instance that this <see cref="NamedEnumValue"/> wraps.
	/// </value>
	public Enum Value { get; } = value;

	/// <summary>
	///    Returns the display name of the enumeration value.
	/// </summary>
	/// <returns>
	///    The same value as the <see cref="Name"/> property.
	/// </returns>
	/// <remarks>
	///    This method is particularly useful for data binding scenarios in WPF where
	///    the framework automatically calls ToString() to display values in UI controls.
	/// </remarks>
	public override string ToString() => Name;

	/// <summary>
	///    Implicitly converts an <see cref="Enum"/> to a <see cref="NamedEnumValue"/>.
	/// </summary>
	/// <param name="enum">The enumeration value to convert.</param>
	/// <returns>
	///    A new <see cref="NamedEnumValue"/> instance wrapping the specified enum value.
	/// </returns>
	/// <remarks>
	///    This operator enables seamless conversion from any enum type to <see cref="NamedEnumValue"/>
	///    without requiring explicit casting.
	/// </remarks>
	/// <example>
	///    <code>
	/// Status status = Status.Approved;
	/// NamedEnumValue named = status; // Implicit conversion
	/// </code>
	/// </example>
	public static implicit operator NamedEnumValue(Enum @enum) => new(@enum);

	/// <summary>
	///    Implicitly converts a <see cref="NamedEnumValue"/> back to its underlying <see cref="Enum"/> value.
	/// </summary>
	/// <param name="value">The <see cref="NamedEnumValue"/> to convert.</param>
	/// <returns>
	///    The underlying <see cref="Enum"/> value.
	/// </returns>
	/// <remarks>
	///    This operator enables seamless conversion back to the original enum type without explicit casting.
	/// </remarks>
	/// <example>
	///    <code>
	/// NamedEnumValue named = Status.Approved;
	/// Status status = named; // Implicit conversion back
	/// </code>
	/// </example>
	public static implicit operator Enum(NamedEnumValue value) => value.Value;

	/// <summary>
	///    Determines whether an <see cref="Enum"/> value is equal to a <see cref="NamedEnumValue"/>.
	/// </summary>
	/// <param name="enum">The enumeration value to compare.</param>
	/// <param name="value">The <see cref="NamedEnumValue"/> to compare.</param>
	/// <returns>
	///    <c>true</c> if the enum value equals the underlying value of <paramref name="value"/>;
	///    otherwise, <c>false</c>.
	/// </returns>
	public static bool operator ==(Enum? @enum, NamedEnumValue value) => value.Equals(@enum);

	/// <summary>
	///    Determines whether an <see cref="Enum"/> value is not equal to a <see cref="NamedEnumValue"/>.
	/// </summary>
	/// <param name="enum">The enumeration value to compare.</param>
	/// <param name="value">The <see cref="NamedEnumValue"/> to compare.</param>
	/// <returns>
	///    <c>true</c> if the enum value does not equal the underlying value of <paramref name="value"/>;
	///    otherwise, <c>false</c>.
	/// </returns>
	public static bool operator !=(Enum? @enum, NamedEnumValue value) => !value.Equals(@enum);

	/// <summary>
	///    Determines whether a <see cref="NamedEnumValue"/> is equal to an <see cref="Enum"/> value.
	/// </summary>
	/// <param name="value">The <see cref="NamedEnumValue"/> to compare.</param>
	/// <param name="enum">The enumeration value to compare.</param>
	/// <returns>
	///    <c>true</c> if the underlying value of <paramref name="value"/> equals the enum value;
	///    otherwise, <c>false</c>.
	/// </returns>
	public static bool operator ==(NamedEnumValue value, Enum? @enum) => value.Equals(@enum);

	/// <summary>
	///    Determines whether a <see cref="NamedEnumValue"/> is not equal to an <see cref="Enum"/> value.
	/// </summary>
	/// <param name="value">The <see cref="NamedEnumValue"/> to compare.</param>
	/// <param name="enum">The enumeration value to compare.</param>
	/// <returns>
	///    <c>true</c> if the underlying value of <paramref name="value"/> does not equal the enum value;
	///    otherwise, <c>false</c>.
	/// </returns>
	public static bool operator !=(NamedEnumValue value, Enum? @enum) => !value.Equals(@enum);

	/// <summary>
	///    Determines whether the specified object is equal to this <see cref="NamedEnumValue"/>.
	/// </summary>
	/// <param name="obj">The object to compare with this instance.</param>
	/// <returns>
	///    <c>true</c> if <paramref name="obj"/> is an <see cref="Enum"/> or <see cref="NamedEnumValue"/>
	///    with the same underlying value; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	///    This method supports comparison with both <see cref="Enum"/> values and other
	///    <see cref="NamedEnumValue"/> instances.
	/// </remarks>
	public override bool Equals(object? obj) => obj is Enum @enum ? Equals(@enum) : obj is NamedEnumValue value && Equals(value.Value);

	/// <summary>
	///    Returns the hash code for this <see cref="NamedEnumValue"/>.
	/// </summary>
	/// <returns>
	///    The hash code of the underlying <see cref="Enum"/> value.
	/// </returns>
	/// <remarks>
	///    This ensures that <see cref="NamedEnumValue"/> instances can be used as dictionary keys
	///    or in hash-based collections, and that they hash consistently with their underlying enum values.
	/// </remarks>
	public override int GetHashCode() => Value.GetHashCode();

	/// <summary>
	///    Determines whether this <see cref="NamedEnumValue"/> is equal to the specified <see cref="Enum"/> value.
	/// </summary>
	/// <param name="enum">The enumeration value to compare.</param>
	/// <returns>
	///    <c>true</c> if <paramref name="enum"/> equals the underlying <see cref="Value"/>
	///    or if both are <c>null</c>; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	///    This method implements the <see cref="IEquatable{Enum}"/> interface, providing
	///    type-safe equality comparison with enum values.
	/// </remarks>
	public bool Equals(Enum? @enum) => @enum != null && @enum.Equals(Value) || @enum == null && Value == null;
}