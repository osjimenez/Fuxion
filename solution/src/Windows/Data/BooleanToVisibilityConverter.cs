using System;
using System.Globalization;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts boolean values to <see cref="Visibility"/> enumeration values
///    for controlling WPF element visibility.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide flexible
///       boolean-to-visibility conversion with configurable output values. It's one of the most commonly
///       used converters in WPF applications for controlling element visibility based on boolean conditions.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Configurable visibility values:</strong> Customize what visibility state to use for true/false
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Supports both <see cref="Convert"/> and <see cref="ConvertBack"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Standard defaults:</strong> True → Visible, False → Collapsed (most common scenario)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible configuration:</strong> Can use Visible, Collapsed, or Hidden for any mapping
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Visibility enumeration values:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Value</term>
///          <description>Behavior</description>
///       </listheader>
///       <item>
///          <term><see cref="Visibility.Visible"/></term>
///          <description>Element is visible and occupies space in the layout</description>
///       </item>
///       <item>
///          <term><see cref="Visibility.Collapsed"/></term>
///          <description>Element is invisible and does NOT occupy space (layout collapses around it)</description>
///       </item>
///       <item>
///          <term><see cref="Visibility.Hidden"/></term>
///          <description>Element is invisible but STILL occupies space (leaves a gap)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Show/hide UI elements based on boolean flags</description>
///       </item>
///       <item>
///          <description>Display validation messages when errors exist</description>
///       </item>
///       <item>
///          <description>Show loading indicators during operations</description>
///       </item>
///       <item>
///          <description>Conditionally display optional UI sections</description>
///       </item>
///       <item>
///          <description>Toggle between different views or panels</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage with default settings (true=Visible, false=Collapsed):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Show TextBlock when HasErrors is true -->
/// <TextBlock Text="Validation errors found!" 
///            Foreground="Red"
///            Visibility="{Binding HasErrors, Converter={StaticResource BoolToVisConverter}}"/>
/// ]]></code>
///    <strong>Inverted logic (true=Collapsed, false=Visible):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="InvertedConverter"
///                                        TrueValue="Collapsed"
///                                        FalseValue="Visible"/>
/// </Window.Resources>
/// 
/// <!-- Hide element when IsProcessing is true -->
/// <Button Content="Submit" 
///         Visibility="{Binding IsProcessing, Converter={StaticResource InvertedConverter}}"/>
/// ]]></code>
///    <strong>Using Hidden instead of Collapsed (preserves layout space):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="HiddenConverter"
///                                        TrueValue="Visible"
///                                        FalseValue="Hidden"/>
/// </Window.Resources>
/// 
/// <!-- Element becomes invisible but keeps its space in layout -->
/// <TextBox Visibility="{Binding ShowField, Converter={StaticResource HiddenConverter}}"/>
/// ]]></code>
///    <strong>Show loading indicator during operation:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Content -->
///     <ContentControl Content="{Binding MainContent}"/>
///     
///     <!-- Loading overlay -->
///     <Border Background="#80000000" 
///             Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisConverter}}">
///         <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
///             <ProgressBar IsIndeterminate="True" Width="200"/>
///             <TextBlock Text="Loading..." Foreground="White" Margin="0,10,0,0"/>
///         </StackPanel>
///     </Border>
/// </Grid>
/// ]]></code>
///    <strong>Conditional panel display:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Show advanced options panel when checkbox is checked -->
/// <CheckBox x:Name="ShowAdvanced" Content="Show Advanced Options"/>
/// <StackPanel Visibility="{Binding IsChecked, 
///                                  ElementName=ShowAdvanced, 
///                                  Converter={StaticResource BoolToVisConverter}}">
///     <TextBlock Text="Advanced Settings"/>
///     <TextBox/>
///     <ComboBox/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new BooleanToVisibilityConverter
/// {
///     TrueValue = Visibility.Visible,
///     FalseValue = Visibility.Collapsed
/// };
/// 
/// bool hasErrors = true;
/// Visibility result = converter.Convert(hasErrors, CultureInfo.CurrentCulture);
/// Console.WriteLine(result);  // Output: Visible
/// 
/// bool backResult = converter.ConvertBack(result, CultureInfo.CurrentCulture);
/// Console.WriteLine(backResult);  // Output: true
/// </code>
///    <strong>Two-way binding scenario:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Visibility property drives IsVisible property and vice versa -->
/// <Border x:Name="MyBorder" 
///         Visibility="{Binding IsVisible, 
///                              Converter={StaticResource BoolToVisConverter}, 
///                              Mode=TwoWay}">
///     <TextBlock Text="Toggleable content"/>
/// </Border>
/// ]]></code>
///    <strong>Multiple elements with different visibility logic:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:BooleanToVisibilityConverter x:Key="ShowWhenTrue"/>
///     <data:BooleanToVisibilityConverter x:Key="HideWhenTrue"
///                                        TrueValue="Collapsed"
///                                        FalseValue="Visible"/>
/// </Window.Resources>
/// 
/// <!-- Show error message when HasError=true -->
/// <TextBlock Text="Error occurred!" 
///            Foreground="Red"
///            Visibility="{Binding HasError, Converter={StaticResource ShowWhenTrue}}"/>
/// 
/// <!-- Show success message when HasError=false -->
/// <TextBlock Text="Success!" 
///            Foreground="Green"
///            Visibility="{Binding HasError, Converter={StaticResource HideWhenTrue}}"/>
/// ]]></code>
///    <strong>Custom configuration for specific scenarios:</strong>
///    <code>
/// // Hide element when condition is true (keeps layout space)
/// var hideWithSpaceConverter = new BooleanToVisibilityConverter
/// {
///     TrueValue = Visibility.Hidden,
///     FalseValue = Visibility.Visible
/// };
/// 
/// // Show only when true, otherwise collapse
/// var standardConverter = new BooleanToVisibilityConverter
/// {
///     TrueValue = Visibility.Visible,
///     FalseValue = Visibility.Collapsed
/// };
/// </code>
/// </example>
public class BooleanToVisibilityConverter : GenericConverter<bool, Visibility>
{
	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source boolean is <c>true</c>.
	/// </summary>
	/// <value>
	///    The visibility state for a true value. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property allows you to customize the visibility behavior for true values. Common configurations:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="Visibility.Visible"/> (default): Element is visible when true
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Collapsed"/>: Element is hidden and collapsed when true (inverted logic)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Hidden"/>: Element is hidden but preserves space when true
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public Visibility TrueValue { get; set; } = Visibility.Visible;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source boolean is <c>false</c>.
	/// </summary>
	/// <value>
	///    The visibility state for a false value. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property allows you to customize the visibility behavior for false values. Common configurations:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="Visibility.Collapsed"/> (default): Element is hidden and doesn't occupy space when false
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Visible"/>: Element is visible when false (inverted logic)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Hidden"/>: Element is hidden but preserves layout space when false
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public Visibility FalseValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Converts a boolean value to a <see cref="Visibility"/> enumeration value.
	/// </summary>
	/// <param name="source">The boolean value to convert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <see cref="TrueValue"/> if <paramref name="source"/> is <c>true</c>;
	///    otherwise, <see cref="FalseValue"/>.
	/// </returns>
	/// <remarks>
	///    This method performs a simple conditional mapping based on the source boolean value.
	///    The returned visibility value is determined by the <see cref="TrueValue"/> and
	///    <see cref="FalseValue"/> properties.
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new BooleanToVisibilityConverter();
	/// 
	/// Visibility result1 = converter.Convert(true, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: Visible
	/// 
	/// Visibility result2 = converter.Convert(false, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: Collapsed
	/// </code>
	/// </example>
	public override Visibility Convert(bool source, CultureInfo culture) => source ? TrueValue : FalseValue;

	/// <summary>
	///    Converts a <see cref="Visibility"/> value back to a boolean.
	/// </summary>
	/// <param name="result">The <see cref="Visibility"/> value to convert back.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <c>true</c> if <paramref name="result"/> equals <see cref="TrueValue"/>;
	///    <c>false</c> if <paramref name="result"/> equals <see cref="FalseValue"/>.
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when <paramref name="result"/> is neither <see cref="TrueValue"/> nor <see cref="FalseValue"/>.
	///    This can occur if the visibility value doesn't match either configured value.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method enables two-way data binding by converting visibility values back to booleans.
	///       The conversion is based on matching the input value against the configured
	///       <see cref="TrueValue"/> and <see cref="FalseValue"/> properties.
	///    </para>
	///    <para>
	///       <strong>Important:</strong> If using custom visibility configurations, ensure that the
	///       visibility value being converted back is either <see cref="TrueValue"/> or <see cref="FalseValue"/>,
	///       otherwise a <see cref="NotSupportedException"/> will be thrown.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new BooleanToVisibilityConverter();
	/// 
	/// bool result1 = converter.ConvertBack(Visibility.Visible, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: true
	/// 
	/// bool result2 = converter.ConvertBack(Visibility.Collapsed, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: false
	/// 
	/// // This would throw NotSupportedException with default configuration
	/// // since Hidden is neither TrueValue nor FalseValue
	/// try
	/// {
	///     converter.ConvertBack(Visibility.Hidden, CultureInfo.CurrentCulture);
	/// }
	/// catch (NotSupportedException ex)
	/// {
	///     Console.WriteLine(ex.Message);
	/// }
	/// </code>
	/// </example>
	public override bool ConvertBack(Visibility result, CultureInfo culture)
	{
		if (result == TrueValue) return true;
		if (result == FalseValue) return false;
		throw new NotSupportedException($"The value '{result}' is not supported for '{nameof(ConvertBack)}' method");
	}
}