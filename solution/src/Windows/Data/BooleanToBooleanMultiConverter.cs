using System;
using System.Globalization;
using System.Linq;

namespace Fuxion.Windows.Data;

/// <summary>
///    A multi-value converter that combines multiple boolean values into a single boolean result
///    based on specified logical conditions.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericMultiConverter{TSource,TResult}"/> to provide
///       logical operations on multiple boolean bindings. It's particularly useful in WPF scenarios
///       where UI element properties depend on multiple boolean conditions.
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
///             <strong>Configurable outputs:</strong> Customize return values for true/false results
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null/empty handling:</strong> Separate values for null and empty source arrays
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multi-binding support:</strong> Works with any number of boolean bindings
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
///          <description>Enable a button only when multiple conditions are met</description>
///       </item>
///       <item>
///          <description>Show validation message when any field has errors</description>
///       </item>
///       <item>
///          <description>Control element state based on multiple permission checks</description>
///       </item>
///       <item>
///          <description>Implement complex UI logic with declarative XAML</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Enable button when all fields are valid (AllTrue mode):</strong>
///    <code><![CDATA[
/// <Button>
///     <Button.IsEnabled>
///         <MultiBinding Converter="{StaticResource BooleanToBooleanMultiConverter}">
///             <MultiBinding.ConverterParameter>
///                 <data:BooleanToBooleanMultiConverter Mode="AllTrue"/>
///             </MultiBinding.ConverterParameter>
///             <Binding Path="IsNameValid"/>
///             <Binding Path="IsEmailValid"/>
///             <Binding Path="IsPhoneValid"/>
///         </MultiBinding>
///     </Button.IsEnabled>
/// </Button>
/// ]]></code>
///    <strong>Show error message if any field is invalid (AnyTrue mode):</strong>
///    <code><![CDATA[
/// <TextBlock Text="Please fix validation errors">
///     <TextBlock.Visibility>
///         <MultiBinding Converter="{StaticResource BooleanToVisibilityMultiConverter}">
///             <MultiBinding.Converter>
///                 <data:BooleanToVisibilityMultiConverter Mode="AnyTrue"/>
///             </MultiBinding.Converter>
///             <Binding Path="HasNameError"/>
///             <Binding Path="HasEmailError"/>
///             <Binding Path="HasPhoneError"/>
///         </MultiBinding>
///     </TextBlock.Visibility>
/// </TextBlock>
/// ]]></code>
///    <strong>Invert multiple boolean values (AllFalse mode):</strong>
///    <code><![CDATA[
/// <!-- Show message when NO checkboxes are checked -->
/// <TextBlock Text="Please select at least one option">
///     <TextBlock.Visibility>
///         <MultiBinding Converter="{StaticResource BooleanToVisibilityMultiConverter}">
///             <MultiBinding.Converter>
///                 <data:BooleanToVisibilityMultiConverter Mode="AllFalse"/>
///             </MultiBinding.Converter>
///             <Binding Path="Option1Checked"/>
///             <Binding Path="Option2Checked"/>
///             <Binding Path="Option3Checked"/>
///         </MultiBinding>
///     </TextBlock.Visibility>
/// </TextBlock>
/// ]]></code>
///    <strong>Using with custom true/false values:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToBooleanMultiConverter x:Key="InverterConverter"
///                                          Mode="AllTrue"
///                                          TrueValue="False"
///                                          FalseValue="True"/>
/// </Window.Resources>
/// 
/// <!-- Disable button when all conditions are true -->
/// <Button>
///     <Button.IsEnabled>
///         <MultiBinding Converter="{StaticResource InverterConverter}">
///             <Binding Path="IsProcessing"/>
///             <Binding Path="IsLocked"/>
///         </MultiBinding>
///     </Button.IsEnabled>
/// </Button>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new BooleanToBooleanMultiConverter
/// {
///     Mode = BooleanMultiConverterMode.AllTrue,
///     TrueValue = true,
///     FalseValue = false
/// };
/// 
/// bool[] conditions = { isValid, isEnabled, hasPermission };
/// bool result = converter.Convert(conditions, CultureInfo.CurrentCulture);
/// 
/// if (result)
/// {
///     // All conditions are true
///     ExecuteOperation();
/// }
/// </code>
///    <strong>Handling null and empty arrays:</strong>
///    <code>
/// var converter = new BooleanToBooleanMultiConverter
/// {
///     Mode = BooleanMultiConverterMode.AnyTrue,
///     NullValue = false,  // Return false if source is null
///     EmptyValue = true   // Return true if source array is empty
/// };
/// 
/// bool resultForNull = converter.Convert(null, CultureInfo.CurrentCulture);  // Returns false
/// bool resultForEmpty = converter.Convert(Array.Empty&lt;bool&gt;(), CultureInfo.CurrentCulture);  // Returns true
/// </code>
/// </example>
public sealed class BooleanToBooleanMultiConverter : GenericMultiConverter<bool, bool>
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
	///    Gets or sets the value to return when the logical condition evaluates to true.
	/// </summary>
	/// <value>
	///    The boolean value to return for a true result. Default is <c>true</c>.
	/// </value>
	/// <remarks>
	///    Set this to <c>false</c> to invert the converter's output, effectively creating
	///    a NOT operation on the logical result.
	/// </remarks>
	public bool TrueValue { get; set; } = true;

	/// <summary>
	///    Gets or sets the value to return when the logical condition evaluates to false.
	/// </summary>
	/// <value>
	///    The boolean value to return for a false result. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    Set this to <c>true</c> to invert the converter's output, effectively creating
	///    a NOT operation on the logical result.
	/// </remarks>
	public bool FalseValue { get; set; } = false;

	/// <summary>
	///    Gets or sets the value to return when the source array is empty.
	/// </summary>
	/// <value>
	///    The boolean value to return when the source array has no elements. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    This value is used when the conversion receives an empty array (length = 0).
	///    This is different from receiving <c>null</c>, which uses <see cref="NullValue"/>.
	/// </remarks>
	public bool EmptyValue { get; set; } = false;

	/// <summary>
	///    Gets or sets the value to return when the source is null.
	/// </summary>
	/// <value>
	///    The boolean value to return when the source is <c>null</c>. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    This value is used when the conversion receives a <c>null</c> reference instead of an array.
	///    This is different from receiving an empty array, which uses <see cref="EmptyValue"/>.
	/// </remarks>
	public bool NullValue { get; set; } = false;

	/// <summary>
	///    Converts an array of boolean values to a single boolean result based on the configured <see cref="Mode"/>.
	/// </summary>
	/// <param name="source">
	///    An array of boolean values to evaluate. Can be <c>null</c> or empty.
	/// </param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the boolean logic
	///    but is required by the <see cref="GenericMultiConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A boolean value determined by applying the <see cref="Mode"/> logic to the source values
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
	public override bool Convert(bool[] source, CultureInfo culture)
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