using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A simple value converter that inverts (negates) boolean values.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide
///       boolean inversion functionality. It's one of the most commonly used converters in WPF
///       applications for inverting boolean properties in XAML bindings.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Simple inversion:</strong> Converts <c>true</c> to <c>false</c> and vice versa
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional:</strong> Supports both <see cref="Convert"/> and <see cref="ConvertBack"/> operations
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>No configuration needed:</strong> Works out-of-the-box without additional properties
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Lightweight:</strong> Minimal overhead for boolean logic operations
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Disable a button when a checkbox is checked</description>
///       </item>
///       <item>
///          <description>Show/hide elements based on inverted boolean conditions</description>
///       </item>
///       <item>
///          <description>Enable controls when validation fails (IsValid = false)</description>
///       </item>
///       <item>
///          <description>Toggle states with inverted logic</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Disable button when checkbox is checked:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToNegateBooleanConverter x:Key="InvertConverter"/>
/// </Window.Resources>
/// 
/// <CheckBox x:Name="LockCheckBox" Content="Lock"/>
/// <Button IsEnabled="{Binding IsChecked, ElementName=LockCheckBox, Converter={StaticResource InvertConverter}}">
///     Edit
/// </Button>
/// ]]></code>
///    <strong>Two-way binding with inversion:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToNegateBooleanConverter x:Key="InvertConverter"/>
/// </Window.Resources>
/// 
/// <!-- ViewModel has IsDisabled property, but we want to bind to an IsEnabled checkbox -->
/// <CheckBox Content="Enabled" 
///           IsChecked="{Binding IsDisabled, 
///                               Converter={StaticResource InvertConverter}, 
///                               Mode=TwoWay}"/>
/// ]]></code>
///    <strong>Hide element when condition is true:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToNegateBooleanConverter x:Key="InvertConverter"/>
///     <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Using PipeConverter to chain converters -->
/// <TextBlock Text="Hidden when processing" 
///            Visibility="{Binding IsProcessing, 
///                                 Converter={StaticResource InvertConverter}}"/>
/// ]]></code>
///    <strong>Enable save button only when form has NO errors:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToNegateBooleanConverter x:Key="InvertConverter"/>
/// </Window.Resources>
/// 
/// <Button Content="Save" 
///         IsEnabled="{Binding HasErrors, Converter={StaticResource InvertConverter}}"/>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new BooleanToNegateBooleanConverter();
/// 
/// bool isEnabled = true;
/// bool result = converter.Convert(isEnabled, CultureInfo.CurrentCulture);
/// Console.WriteLine(result);  // Output: false
/// 
/// bool backResult = converter.ConvertBack(result, CultureInfo.CurrentCulture);
/// Console.WriteLine(backResult);  // Output: true
/// </code>
///    <strong>Multiple inversions (not recommended, but possible):</strong>
///    <code><![CDATA[
/// <!-- Double inversion results in original value -->
/// <CheckBox>
///     <CheckBox.IsEnabled>
///         <MultiBinding Converter="{StaticResource PipeConverter}">
///             <MultiBinding.Converters>
///                 <data:BooleanToNegateBooleanConverter/>
///                 <data:BooleanToNegateBooleanConverter/>
///             </MultiBinding.Converters>
///             <Binding Path="OriginalValue"/>
///         </MultiBinding>
///     </CheckBox.IsEnabled>
/// </CheckBox>
/// ]]></code>
///    <strong>Combining with MultiBinding:</strong>
///    <code><![CDATA[
/// <Button>
///     <Button.IsEnabled>
///         <MultiBinding Converter="{StaticResource AndConverter}">
///             <!-- Enable button only when NOT processing AND has permission -->
///             <Binding Path="IsProcessing" Converter="{StaticResource InvertConverter}"/>
///             <Binding Path="HasPermission"/>
///         </MultiBinding>
///     </Button.IsEnabled>
/// </Button>
/// ]]></code>
/// </example>
public class BooleanToNegateBooleanConverter : GenericConverter<bool, bool>
{
	/// <summary>
	///    Converts a boolean value to its logical inverse.
	/// </summary>
	/// <param name="source">The boolean value to invert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <c>false</c> if <paramref name="source"/> is <c>true</c>; otherwise, <c>true</c>.
	/// </returns>
	/// <remarks>
	///    This method performs a simple boolean NOT operation (<c>!source</c>).
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new BooleanToNegateBooleanConverter();
	/// 
	/// bool result1 = converter.Convert(true, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: false
	/// 
	/// bool result2 = converter.Convert(false, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: true
	/// </code>
	/// </example>
	public override bool Convert(bool source, CultureInfo culture) => !source;

	/// <summary>
	///    Converts a boolean value back to its logical inverse.
	/// </summary>
	/// <param name="result">The boolean value to invert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <c>false</c> if <paramref name="result"/> is <c>true</c>; otherwise, <c>true</c>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method performs the same boolean NOT operation as <see cref="Convert"/>,
	///       since boolean inversion is its own inverse operation (involutory function).
	///       This makes the converter fully bidirectional for two-way data binding scenarios.
	///    </para>
	///    <para>
	///       Mathematical property: <c>NOT(NOT(x)) = x</c> for any boolean <c>x</c>.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new BooleanToNegateBooleanConverter();
	/// 
	/// bool original = true;
	/// bool inverted = converter.Convert(original, CultureInfo.CurrentCulture);
	/// bool restored = converter.ConvertBack(inverted, CultureInfo.CurrentCulture);
	/// 
	/// Console.WriteLine(original);   // Output: true
	/// Console.WriteLine(inverted);   // Output: false
	/// Console.WriteLine(restored);   // Output: true
	/// Console.WriteLine(original == restored);  // Output: true
	/// </code>
	/// </example>
	public override bool ConvertBack(bool result, CultureInfo culture) => !result;
}