using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    Defines the logical operation modes for combining multiple boolean values.
/// </summary>
/// <remarks>
///    <para>
///       This enumeration is used by multi-value converters such as <see cref="BooleanToVisibilityMultiConverter"/>
///       and <see cref="BooleanToBooleanMultiConverter"/> to determine how to combine multiple boolean inputs
///       into a single output value.
///    </para>
///    <para>
///       <strong>Logical operations:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Mode</term>
///          <description>Logic Description</description>
///       </listheader>
///       <item>
///          <term><see cref="AllTrue"/></term>
///          <description>Logical AND - Returns true only when all values are true</description>
///       </item>
///       <item>
///          <term><see cref="AnyTrue"/></term>
///          <description>Logical OR - Returns true when at least one value is true</description>
///       </item>
///       <item>
///          <term><see cref="AllFalse"/></term>
///          <description>Logical NOR - Returns true only when all values are false</description>
///       </item>
///       <item>
///          <term><see cref="AnyFalse"/></term>
///          <description>Logical NAND - Returns true when at least one value is false</description>
///       </item>
///    </list>
/// </remarks>
public enum BooleanMultiConverterMode
{
	/// <summary>
	///    All values must be true for the result to be true (logical AND).
	/// </summary>
	/// <remarks>
	///    <para>
	///       This mode returns true only when every input value is true.
	///       If any value is false, the result is false.
	///    </para>
	///    <para>
	///       <strong>Truth table example:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>[true, true, true] → true</description></item>
	///       <item><description>[true, false, true] → false</description></item>
	///       <item><description>[false, false, false] → false</description></item>
	///    </list>
	///    <para>
	///       <strong>Common use cases:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>Enable a button only when all form fields are valid</description></item>
	///       <item><description>Show content only when all permissions are granted</description></item>
	///       <item><description>Display message only when all conditions are met</description></item>
	///    </list>
	/// </remarks>
	AllTrue,

	/// <summary>
	///    At least one value must be true for the result to be true (logical OR).
	/// </summary>
	/// <remarks>
	///    <para>
	///       This mode returns true when any input value is true.
	///       The result is false only when all values are false.
	///    </para>
	///    <para>
	///       <strong>Truth table example:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>[true, false, false] → true</description></item>
	///       <item><description>[false, true, false] → true</description></item>
	///       <item><description>[false, false, false] → false</description></item>
	///    </list>
	///    <para>
	///       <strong>Common use cases:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>Show error message if any field has an error</description></item>
	///       <item><description>Display warning if any validation fails</description></item>
	///       <item><description>Enable feature if user has any of several permissions</description></item>
	///    </list>
	/// </remarks>
	AnyTrue,

	/// <summary>
	///    All values must be false for the result to be true (logical NOR).
	/// </summary>
	/// <remarks>
	///    <para>
	///       This mode returns true only when every input value is false.
	///       If any value is true, the result is false.
	///    </para>
	///    <para>
	///       <strong>Truth table example:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>[false, false, false] → true</description></item>
	///       <item><description>[false, true, false] → false</description></item>
	///       <item><description>[true, true, true] → false</description></item>
	///    </list>
	///    <para>
	///       <strong>Common use cases:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>Show message when no options are selected</description></item>
	///       <item><description>Display placeholder when all checkboxes are unchecked</description></item>
	///       <item><description>Enable action when no operations are in progress</description></item>
	///    </list>
	/// </remarks>
	AllFalse,

	/// <summary>
	///    At least one value must be false for the result to be true (logical NAND).
	/// </summary>
	/// <remarks>
	///    <para>
	///       This mode returns true when any input value is false.
	///       The result is false only when all values are true.
	///    </para>
	///    <para>
	///       <strong>Truth table example:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>[false, true, true] → true</description></item>
	///       <item><description>[true, false, true] → true</description></item>
	///       <item><description>[true, true, true] → false</description></item>
	///    </list>
	///    <para>
	///       <strong>Common use cases:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>Show warning if any field is invalid</description></item>
	///       <item><description>Display message if any requirement is not met</description></item>
	///       <item><description>Enable fallback when any condition fails</description></item>
	///    </list>
	/// </remarks>
	AnyFalse
}

/// <summary>
///    A multi-value converter that combines multiple boolean values into a <see cref="Visibility"/> value
///    based on specified logical conditions.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericMultiConverter{TSource,TResult}"/> to provide
///       logical operations on multiple boolean bindings for controlling WPF element visibility.
///       It's particularly useful when element visibility depends on multiple boolean conditions.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Multiple logical modes:</strong> Supports AllTrue, AnyTrue, AllFalse, and AnyFalse operations
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable visibility values:</strong> Customize return values for true/false results
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null/empty handling:</strong> Separate visibility values for null and empty source arrays
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multi-binding support:</strong> Works with any number of boolean bindings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible visibility control:</strong> Use Visible, Collapsed, or Hidden for any mapping
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Logical modes:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Mode</term>
///          <description>Behavior</description>
///       </listheader>
///       <item>
///          <term><see cref="BooleanMultiConverterMode.AllTrue"/></term>
///          <description>Returns <see cref="TrueValue"/> only if all values are true (logical AND)</description>
///       </item>
///       <item>
///          <term><see cref="BooleanMultiConverterMode.AnyTrue"/></term>
///          <description>Returns <see cref="TrueValue"/> if at least one value is true (logical OR)</description>
///       </item>
///       <item>
///          <term><see cref="BooleanMultiConverterMode.AllFalse"/></term>
///          <description>Returns <see cref="TrueValue"/> only if all values are false (logical NOR)</description>
///       </item>
///       <item>
///          <term><see cref="BooleanMultiConverterMode.AnyFalse"/></term>
///          <description>Returns <see cref="TrueValue"/> if at least one value is false (logical NAND)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Show element only when all conditions are met</description>
///       </item>
///       <item>
///          <description>Display error panel when any validation fails</description>
///       </item>
///       <item>
///          <description>Hide content when all items are deselected</description>
///       </item>
///       <item>
///          <description>Show loading overlay when any operation is in progress</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Show element when all conditions are true (AllTrue mode):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityMultiConverter x:Key="AllTrueConverter" 
///                                             Mode="AllTrue"/>
/// </Window.Resources>
/// 
/// <!-- Show submit button only when all fields are valid -->
/// <Button Content="Submit">
///     <Button.Visibility>
///         <MultiBinding Converter="{StaticResource AllTrueConverter}">
///             <Binding Path="IsNameValid"/>
///             <Binding Path="IsEmailValid"/>
///             <Binding Path="IsPhoneValid"/>
///         </MultiBinding>
///     </Button.Visibility>
/// </Button>
/// ]]></code>
///    <strong>Show error panel if any field has errors (AnyTrue mode):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityMultiConverter x:Key="AnyTrueConverter" 
///                                             Mode="AnyTrue"/>
/// </Window.Resources>
/// 
/// <!-- Show error panel if any field has errors -->
/// <Border Background="LightCoral" 
///         BorderBrush="Red" 
///         BorderThickness="1" 
///         Padding="10">
///     <Border.Visibility>
///         <MultiBinding Converter="{StaticResource AnyTrueConverter}">
///             <Binding Path="HasNameError"/>
///             <Binding Path="HasEmailError"/>
///             <Binding Path="HasPhoneError"/>
///         </MultiBinding>
///     </Border.Visibility>
///     <TextBlock Text="Please fix the validation errors" Foreground="DarkRed"/>
/// </Border>
/// ]]></code>
///    <strong>Show message when no items are selected (AllFalse mode):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityMultiConverter x:Key="AllFalseConverter" 
///                                             Mode="AllFalse"/>
/// </Window.Resources>
/// 
/// <!-- Show message when no checkboxes are selected -->
/// <TextBlock Text="Please select at least one option" 
///            Foreground="Gray">
///     <TextBlock.Visibility>
///         <MultiBinding Converter="{StaticResource AllFalseConverter}">
///             <Binding Path="Option1Selected"/>
///             <Binding Path="Option2Selected"/>
///             <Binding Path="Option3Selected"/>
///         </MultiBinding>
///     </TextBlock.Visibility>
/// </TextBlock>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new BooleanToVisibilityMultiConverter
/// {
///     Mode = BooleanMultiConverterMode.AllTrue,
///     TrueValue = Visibility.Visible,
///     FalseValue = Visibility.Collapsed
/// };
/// 
/// bool[] conditions = { isValid, isEnabled, hasPermission };
/// Visibility result = converter.Convert(conditions, CultureInfo.CurrentCulture);
/// 
/// if (result == Visibility.Visible)
/// {
///     // All conditions are true
///     ShowElement();
/// }
/// </code>
/// </example>
public sealed class BooleanToVisibilityMultiConverter : GenericMultiConverter<bool, Visibility>
{
	/// <summary>
	///    Gets or sets the logical mode for combining boolean values.
	/// </summary>
	/// <value>
	///    A <see cref="BooleanMultiConverterMode"/> value that determines how boolean values are combined.
	///    Default is <see cref="BooleanMultiConverterMode.AllTrue"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       The mode determines the logical operation applied to the source boolean values:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AllTrue"/>: Logical AND - all values must be true
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AnyTrue"/>: Logical OR - at least one value must be true
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AllFalse"/>: Logical NOR - all values must be false
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AnyFalse"/>: Logical NAND - at least one value must be false
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public BooleanMultiConverterMode Mode { get; set; } = BooleanMultiConverterMode.AllTrue;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the logical condition evaluates to true.
	/// </summary>
	/// <value>
	///    The visibility state to return for a true result. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Visible"/>, <see cref="Visibility.Collapsed"/>,
	///    or <see cref="Visibility.Hidden"/>.
	/// </remarks>
	public Visibility TrueValue { get; set; } = Visibility.Visible;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the logical condition evaluates to false.
	/// </summary>
	/// <value>
	///    The visibility state to return for a false result. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Collapsed"/> (doesn't occupy space)
	///    or <see cref="Visibility.Hidden"/> (preserves layout space).
	/// </remarks>
	public Visibility FalseValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source array is empty.
	/// </summary>
	/// <value>
	///    The visibility state to return when the source array has no elements. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    This value is used when the conversion receives an empty array (length = 0).
	///    This is different from receiving <c>null</c>, which uses <see cref="NullValue"/>.
	/// </remarks>
	public Visibility EmptyValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source is null.
	/// </summary>
	/// <value>
	///    The visibility state to return when the source is <c>null</c>. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    This value is used when the conversion receives a <c>null</c> reference instead of an array.
	///    This is different from receiving an empty array, which uses <see cref="EmptyValue"/>.
	/// </remarks>
	public Visibility NullValue { get; set; } = Visibility.Collapsed;

	//public bool AllowNullValues { get; set; }
	//public bool NullValue { get; set; }

	/// <summary>
	///    Converts an array of boolean values to a <see cref="Visibility"/> value based on the configured <see cref="Mode"/>.
	/// </summary>
	/// <param name="source">
	///    An array of boolean values to evaluate. Can be <c>null</c> or empty.
	/// </param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the boolean logic
	///    but is required by the <see cref="GenericMultiConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A <see cref="Visibility"/> value determined by applying the <see cref="Mode"/> logic to the source values
	///    and mapping the result through <see cref="TrueValue"/> or <see cref="FalseValue"/>.
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when the <see cref="Mode"/> property has an unsupported value.
	/// </exception>
	/// <remarks>
	///    <para>
	///       <strong>Special cases:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>If <paramref name="source"/> is <c>null</c>, returns <see cref="NullValue"/></description>
	///       </item>
	///       <item>
	///          <description>If <paramref name="source"/> is empty, returns <see cref="EmptyValue"/></description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Mode behavior:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AllTrue"/>: Returns <see cref="FalseValue"/> if any value is false,
	///             otherwise <see cref="TrueValue"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AnyTrue"/>: Returns <see cref="TrueValue"/> if any value is true,
	///             otherwise <see cref="FalseValue"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AllFalse"/>: Returns <see cref="FalseValue"/> if any value is true,
	///             otherwise <see cref="TrueValue"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="BooleanMultiConverterMode.AnyFalse"/>: Returns <see cref="TrueValue"/> if any value is false,
	///             otherwise <see cref="FalseValue"/>
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public override Visibility Convert(bool[] source, CultureInfo culture)
	{
		if (source == null) return NullValue;
		if (!source.Any()) return EmptyValue;
		switch (Mode)
		{
			case BooleanMultiConverterMode.AllTrue:  return source.Any(v => !v) ? FalseValue : TrueValue;
			case BooleanMultiConverterMode.AnyTrue:  return source.Any(v => v) ? TrueValue : FalseValue;
			case BooleanMultiConverterMode.AllFalse: return source.Any(v => v) ? FalseValue : TrueValue;
			case BooleanMultiConverterMode.AnyFalse: return source.Any(v => !v) ? TrueValue : FalseValue;
			default:                                 throw new NotSupportedException($"The value of Mode '{Mode}' is not supported");
		}
	}
}