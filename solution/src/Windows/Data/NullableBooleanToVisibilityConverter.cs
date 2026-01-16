using System;
using System.Globalization;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts nullable boolean values (<see cref="bool"/>) to <see cref="Visibility"/>
///    enumeration values for controlling WPF element visibility.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide flexible
///       nullable boolean-to-visibility conversion with configurable output values for true, false, and null states.
///       It's similar to <see cref="BooleanToVisibilityConverter"/> but adds support for three-state logic
///       where null has a distinct meaning.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Three-state support:</strong> Separate visibility values for true, false, and null
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable visibility values:</strong> Customize visibility for each state
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Supports both <see cref="Convert"/> and <see cref="ConvertBack"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Standard defaults:</strong> True → Visible, False → Collapsed, Null → Collapsed
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Treats null and not-HasValue the same way
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Conversion logic:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Boolean State</term>
///          <description>Returned Visibility</description>
///       </listheader>
///       <item>
///          <term>true</term>
///          <description><see cref="TrueValue"/> (default: <see cref="Visibility.Visible"/>)</description>
///       </item>
///       <item>
///          <term>false</term>
///          <description><see cref="FalseValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>null or not-HasValue</term>
///          <description><see cref="NullValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Control visibility based on tri-state checkboxes or nullable boolean properties</description>
///       </item>
///       <item>
///          <description>Show/hide elements based on optional configuration flags</description>
///       </item>
///       <item>
///          <description>Display different states for indeterminate conditions</description>
///       </item>
///       <item>
///          <description>Handle scenarios where "unknown" or "not set" is a valid state</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage with nullable boolean property:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableBooleanToVisibilityConverter x:Key="NullableBoolConverter"/>
/// </Window.Resources>
/// 
/// <!-- Visible when IsEnabled is true, Collapsed when false or null -->
/// <TextBlock Text="Feature enabled" 
///            Visibility="{Binding IsEnabled, Converter={StaticResource NullableBoolConverter}}"/>
/// ]]></code>
///    <strong>Using with tri-state CheckBox:</strong>
///    <code>
/// public class SettingsViewModel : INotifyPropertyChanged
/// {
///     private bool? _enableFeature;
///     public bool? EnableFeature
///     {
///         get => _enableFeature;
///         set
///         {
///             _enableFeature = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableBooleanToVisibilityConverter x:Key="BoolConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Tri-state CheckBox -->
///     <CheckBox Content="Enable Feature" 
///               IsThreeState="True" 
///               IsChecked="{Binding EnableFeature}"/>
///     
///     <!-- Show when checked (true) -->
///     <TextBlock Text="[CHECK] Feature is enabled" 
///                Foreground="Green"
///                Visibility="{Binding EnableFeature, Converter={StaticResource BoolConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Different visibility for each state:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- True=Visible, False=Hidden, Null=Collapsed -->
///     <data:NullableBooleanToVisibilityConverter x:Key="CustomConverter"
///                                                TrueValue="Visible"
///                                                FalseValue="Hidden"
///                                                NullValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <Border Visibility="{Binding OptionalSetting, Converter={StaticResource CustomConverter}}">
///     <!-- Visible when true, Hidden (preserves space) when false, Collapsed when null -->
///     <TextBlock Text="Content"/>
/// </Border>
/// ]]></code>
///    <strong>Show different messages for each state:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableBooleanToVisibilityConverter x:Key="TrueConverter"
///                                                TrueValue="Visible"
///                                                FalseValue="Collapsed"
///                                                NullValue="Collapsed"/>
///     
///     <data:NullableBooleanToVisibilityConverter x:Key="FalseConverter"
///                                                TrueValue="Collapsed"
///                                                FalseValue="Visible"
///                                                NullValue="Collapsed"/>
///     
///     <data:NullableBooleanToVisibilityConverter x:Key="NullConverter"
///                                                TrueValue="Collapsed"
///                                                FalseValue="Collapsed"
///                                                NullValue="Visible"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Show when IsApproved = true -->
///     <TextBlock Text="[APPROVED] Approved" 
///                Foreground="Green"
///                Visibility="{Binding IsApproved, Converter={StaticResource TrueConverter}}"/>
///     
///     <!-- Show when IsApproved = false -->
///     <TextBlock Text="[REJECTED] Rejected" 
///                Foreground="Red"
///                Visibility="{Binding IsApproved, Converter={StaticResource FalseConverter}}"/>
///     
///     <!-- Show when IsApproved = null -->
///     <TextBlock Text="[PENDING] Pending Review" 
///                Foreground="Orange"
///                Visibility="{Binding IsApproved, Converter={StaticResource NullConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new NullableBooleanToVisibilityConverter
/// {
///     TrueValue = Visibility.Visible,
///     FalseValue = Visibility.Collapsed,
///     NullValue = Visibility.Hidden
/// };
/// 
/// bool? value1 = true;
/// Visibility result1 = converter.Convert(value1, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Visible
/// 
/// bool? value2 = false;
/// Visibility result2 = converter.Convert(value2, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Collapsed
/// 
/// bool? value3 = null;
/// Visibility result3 = converter.Convert(value3, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Hidden
/// </code>
///    <strong>Two-way binding with tri-state checkbox:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableBooleanToVisibilityConverter x:Key="BoolConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <CheckBox IsThreeState="True" IsChecked="{Binding FeatureEnabled, Mode=TwoWay}"/>
///     
///     <!-- Visibility updates as checkbox state changes -->
///     <Border Visibility="{Binding FeatureEnabled, 
///                                  Converter={StaticResource BoolConverter}, 
///                                  Mode=TwoWay}">
///         <TextBlock Text="Feature content"/>
///     </Border>
/// </StackPanel>
/// ]]></code>
///    <strong>Optional feature with indeterminate state:</strong>
///    <code>
/// public class FeatureViewModel : INotifyPropertyChanged
/// {
///     // null = not configured, true = enabled, false = disabled
///     public bool? IsFeatureEnabled { get; set; }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableBooleanToVisibilityConverter x:Key="EnabledConverter"
///                                                TrueValue="Visible"
///                                                FalseValue="Collapsed"
///                                                NullValue="Collapsed"/>
///     
///     <data:NullableBooleanToVisibilityConverter x:Key="DisabledConverter"
///                                                TrueValue="Collapsed"
///                                                FalseValue="Visible"
///                                                NullValue="Collapsed"/>
///     
///     <data:NullableBooleanToVisibilityConverter x:Key="NotConfiguredConverter"
///                                                TrueValue="Collapsed"
///                                                FalseValue="Collapsed"
///                                                NullValue="Visible"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Show when enabled -->
///     <TextBlock Text="Feature is enabled" 
///                Visibility="{Binding IsFeatureEnabled, 
///                                    Converter={StaticResource EnabledConverter}}"/>
///     
///     <!-- Show when disabled -->
///     <TextBlock Text="Feature is disabled" 
///                Visibility="{Binding IsFeatureEnabled, 
///                                    Converter={StaticResource DisabledConverter}}"/>
///     
///     <!-- Show when not configured -->
///     <TextBlock Text="Feature not configured - please set up" 
///                Visibility="{Binding IsFeatureEnabled, 
///                                    Converter={StaticResource NotConfiguredConverter}}"/>
/// </StackPanel>
/// ]]></code>
/// </example>
public class NullableBooleanToVisibilityConverter : GenericConverter<bool?, Visibility>
{
	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source boolean is <c>true</c>.
	/// </summary>
	/// <value>
	///    The visibility state for a true value. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Visible"/>, <see cref="Visibility.Collapsed"/>,
	///    or <see cref="Visibility.Hidden"/>.
	/// </remarks>
	public Visibility TrueValue { get; set; } = Visibility.Visible;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source boolean is <c>false</c>.
	/// </summary>
	/// <value>
	///    The visibility state for a false value. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Collapsed"/> (doesn't occupy space)
	///    or <see cref="Visibility.Hidden"/> (preserves layout space).
	/// </remarks>
	public Visibility FalseValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source boolean is <c>null</c>
	///    or doesn't have a value.
	/// </summary>
	/// <value>
	///    The visibility state for a null value. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This value is returned when the source is <c>null</c> or when <c>source.HasValue</c> is false.
	///       Both conditions are treated identically.
	///    </para>
	///    <para>
	///       Typically set to <see cref="Visibility.Collapsed"/> to hide elements when the state is unknown
	///       or not set. Can be configured to <see cref="Visibility.Hidden"/> to preserve layout space,
	///       or even <see cref="Visibility.Visible"/> to show elements by default when state is unknown.
	///    </para>
	/// </remarks>
	public Visibility NullValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Converts a nullable boolean value to a <see cref="Visibility"/> enumeration value.
	/// </summary>
	/// <param name="source">The nullable boolean value to convert. Can be <c>true</c>, <c>false</c>, or <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A <see cref="Visibility"/> value determined by the source boolean state:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="NullValue"/> if <paramref name="source"/> is <c>null</c> or not HasValue</description>
	///       </item>
	///       <item>
	///          <description><see cref="TrueValue"/> if <paramref name="source"/> is <c>true</c></description>
	///       </item>
	///       <item>
	///          <description><see cref="FalseValue"/> if <paramref name="source"/> is <c>false</c></description>
	///       </item>
	///    </list>
	/// </returns>
	/// <remarks>
	///    This method performs a simple conditional mapping based on the source boolean value.
	///    The null check is performed first, treating both <c>null</c> references and nullable types
	///    without values (not HasValue) identically.
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullableBooleanToVisibilityConverter();
	/// 
	/// bool? value1 = true;
	/// Visibility result1 = converter.Convert(value1, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: Visible
	/// 
	/// bool? value2 = false;
	/// Visibility result2 = converter.Convert(value2, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: Collapsed
	/// 
	/// bool? value3 = null;
	/// Visibility result3 = converter.Convert(value3, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3);  // Output: Collapsed
	/// </code>
	/// </example>
	public override Visibility Convert(bool? source, CultureInfo culture)
	{
		if (source == null || !source.HasValue) return NullValue;
		return source.Value ? TrueValue : FalseValue;
	}

	/// <summary>
	///    Converts a <see cref="Visibility"/> value back to a nullable boolean.
	/// </summary>
	/// <param name="result">The <see cref="Visibility"/> value to convert back.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A nullable boolean value determined by matching the visibility:
	///    <list type="bullet">
	///       <item>
	///          <description><c>true</c> if <paramref name="result"/> equals <see cref="TrueValue"/></description>
	///       </item>
	///       <item>
	///          <description><c>false</c> if <paramref name="result"/> equals <see cref="FalseValue"/></description>
	///       </item>
	///       <item>
	///          <description><c>null</c> if <paramref name="result"/> equals <see cref="NullValue"/></description>
	///       </item>
	///    </list>
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when <paramref name="result"/> doesn't match any of the configured visibility values
	///    (<see cref="TrueValue"/>, <see cref="FalseValue"/>, or <see cref="NullValue"/>).
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method enables two-way data binding by converting visibility values back to nullable booleans.
	///       The conversion is based on matching the input value against the configured visibility properties.
	///    </para>
	///    <para>
	///       <strong>Important:</strong> If using custom visibility configurations, ensure that the
	///       visibility value being converted back matches one of <see cref="TrueValue"/>, <see cref="FalseValue"/>,
	///       or <see cref="NullValue"/>, otherwise a <see cref="NotSupportedException"/> will be thrown.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullableBooleanToVisibilityConverter();
	/// 
	/// bool? result1 = converter.ConvertBack(Visibility.Visible, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: true
	/// 
	/// bool? result2 = converter.ConvertBack(Visibility.Collapsed, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: false (assuming default FalseValue)
	/// 
	/// // With custom configuration where Collapsed = null
	/// converter.NullValue = Visibility.Hidden;
	/// bool? result3 = converter.ConvertBack(Visibility.Collapsed, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3);  // Output: false
	/// 
	/// bool? result4 = converter.ConvertBack(Visibility.Hidden, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result4);  // Output: null
	/// </code>
	/// </example>
	public override bool? ConvertBack(Visibility result, CultureInfo culture)
	{
		if (result == TrueValue) return true;
		if (result == FalseValue) return false;
		if (result == NullValue) return null;
		throw new NotSupportedException($"The value '{result}' is not supported for '{nameof(ConvertBack)}' method");
	}
}