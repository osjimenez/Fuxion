using System;
using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts nullable objects to boolean values based on whether they are null or not.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide simple
///       null-checking conversion from any nullable object to a boolean value. It's particularly useful for
///       controlling UI element states based on whether a value exists or not.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Simple null check:</strong> Returns one value for null, opposite for non-null
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable null value:</strong> Set what boolean to return for null objects
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic inversion:</strong> Non-null always returns the opposite of <see cref="NullValue"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Partial two-way conversion:</strong> Can convert back to null, but not to original object
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Works with any object:</strong> Source type is <see cref="object"/> so it accepts anything
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Conversion logic:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Object State</term>
///          <description>Returned Value</description>
///       </listheader>
///       <item>
///          <term>null</term>
///          <description><see cref="NullValue"/> (default: <c>false</c>)</description>
///       </item>
///       <item>
///          <term>not null</term>
///          <description>!<see cref="NullValue"/> (default: <c>true</c>)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Enable/disable controls based on whether a property has a value</description>
///       </item>
///       <item>
///          <description>Show/hide validation messages when value is missing</description>
///       </item>
///       <item>
///          <description>Invert null checks (set NullValue to true to reverse logic)</description>
///       </item>
///       <item>
///          <description>Control button states based on data availability</description>
///       </item>
///       <item>
///          <description>Implement "has value" or "is empty" checks in bindings</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Enable button only when object is not null:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullToBooleanConverter x:Key="NullToBoolConverter"/>
/// </Window.Resources>
/// 
/// <!-- Enabled when SelectedItem is not null -->
/// <Button Content="Process" 
///         IsEnabled="{Binding SelectedItem, Converter={StaticResource NullToBoolConverter}}"/>
/// ]]></code>
///    <strong>Show validation message when value is null:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Set NullValue to true to invert logic -->
///     <data:NullToBooleanConverter x:Key="InvertedConverter" NullValue="True"/>
///     <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Visible when Name is null (because NullValue=true) -->
/// <TextBlock Text="Name is required" 
///            Foreground="Red"
///            Visibility="{Binding Name, 
///                                Converter={StaticResource InvertedConverter},
///                                Converter={StaticResource BoolToVisConverter}}"/>
/// ]]></code>
///    <strong>Multiple controls depending on null state:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullToBooleanConverter x:Key="HasValueConverter"/>
///     <data:NullToBooleanConverter x:Key="IsNullConverter" NullValue="True"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Enabled when document is loaded (not null) -->
///     <Button Content="Save" 
///             IsEnabled="{Binding CurrentDocument, Converter={StaticResource HasValueConverter}}"/>
///     
///     <!-- Enabled when document is not loaded (null) -->
///     <Button Content="New Document" 
///             IsEnabled="{Binding CurrentDocument, Converter={StaticResource IsNullConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new NullToBooleanConverter
/// {
///     NullValue = false  // Default
/// };
/// 
/// object obj1 = new object();
/// bool result1 = converter.Convert(obj1, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: true (not null)
/// 
/// object obj2 = null;
/// bool result2 = converter.Convert(obj2, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: false (null)
/// 
/// // Inverted logic
/// converter.NullValue = true;
/// bool result3 = converter.Convert(obj2, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: true (null, but NullValue=true)
/// 
/// bool result4 = converter.Convert(obj1, CultureInfo.CurrentCulture);
/// Console.WriteLine(result4);  // Output: false (not null, inverted)
/// </code>
///    <strong>Form validation scenario:</strong>
///    <code>
/// public class FormViewModel : INotifyPropertyChanged
/// {
///     private string _name;
///     public string Name
///     {
///         get => _name;
///         set
///         {
///             _name = value;
///             OnPropertyChanged();
///         }
///     }
///     
///     private Address _address;
///     public Address Address
///     {
///         get => _address;
///         set
///         {
///             _address = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullToBooleanConverter x:Key="HasValueConverter"/>
///     <data:NullToBooleanConverter x:Key="IsNullConverter" NullValue="True"/>
///     <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Show warning when address is null -->
///     <Border Background="Yellow" 
///             Padding="5"
///             Visibility="{Binding Address, 
///                                 Converter={StaticResource IsNullConverter},
///                                 Converter={StaticResource BoolToVisConverter}}">
///         <TextBlock Text="⚠ Please add an address" FontWeight="Bold"/>
///     </Border>
///     
///     <!-- Address details panel: visible when address is not null -->
///     <GroupBox Header="Address Details"
///               Visibility="{Binding Address, 
///                                   Converter={StaticResource HasValueConverter},
///                                   Converter={StaticResource BoolToVisConverter}}">
///         <StackPanel>
///             <TextBlock Text="{Binding Address.Street}"/>
///             <TextBlock Text="{Binding Address.City}"/>
///         </StackPanel>
///     </GroupBox>
/// </StackPanel>
/// ]]></code>
///    <strong>Multi-binding with other converters:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullToBooleanConverter x:Key="NullConverter"/>
///     <data:BooleanToBooleanMultiConverter x:Key="AndConverter" Mode="AllTrue"/>
/// </Window.Resources>
/// 
/// <!-- Enable only when both objects are not null -->
/// <Button Content="Compare">
///     <Button.IsEnabled>
///         <MultiBinding Converter="{StaticResource AndConverter}">
///             <Binding Path="Object1" Converter="{StaticResource NullConverter}"/>
///             <Binding Path="Object2" Converter="{StaticResource NullConverter}"/>
///         </MultiBinding>
///     </Button.IsEnabled>
/// </Button>
/// ]]></code>
///    <strong>ConvertBack usage (limited):</strong>
///    <code>
/// var converter = new NullToBooleanConverter { NullValue = false };
/// 
/// // Convert back to null works when result matches NullValue
/// object result1 = converter.ConvertBack(false, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1 == null);  // Output: true
/// 
/// // Convert back to non-null throws exception (cannot recreate original object)
/// try
/// {
///     object result2 = converter.ConvertBack(true, CultureInfo.CurrentCulture);
/// }
/// catch (NotSupportedException ex)
/// {
///     Console.WriteLine(ex.Message);  // Output: The value 'True' is not supported...
/// }
/// </code>
/// </example>
public sealed class NullToBooleanConverter : GenericConverter<object?, bool>
{
	/// <summary>
	///    Gets or sets the boolean value to return when the source object is null.
	/// </summary>
	/// <value>
	///    The boolean value for null objects. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       When the source is null, this value is returned. When the source is not null,
	///       the opposite of this value is returned.
	///    </para>
	///    <para>
	///       <strong>Common configurations:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <c>false</c> (default): Returns false for null, true for not null - "has value" logic
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <c>true</c>: Returns true for null, false for not null - "is null" or "is empty" logic
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public bool NullValue { get; set; }

	/// <summary>
	///    Converts a nullable object to a boolean value based on whether it is null.
	/// </summary>
	/// <param name="source">The object to evaluate. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <see cref="NullValue"/> if <paramref name="source"/> is <c>null</c>;
	///    otherwise, the opposite of <see cref="NullValue"/> (using the <c>!</c> operator).
	/// </returns>
	/// <remarks>
	///    <para>
	///       This is a simple null check conversion with automatic inversion for non-null values.
	///       The logic is: <c>source == null ? NullValue : !NullValue</c>
	///    </para>
	///    <para>
	///       <strong>Examples with default NullValue (false):</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>null → false</description></item>
	///       <item><description>new object() → true</description></item>
	///       <item><description>"text" → true</description></item>
	///       <item><description>0 → true</description></item>
	///    </list>
	///    <para>
	///       <strong>Examples with NullValue = true:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>null → true</description></item>
	///       <item><description>new object() → false</description></item>
	///       <item><description>"text" → false</description></item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullToBooleanConverter { NullValue = false };
	/// 
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));           // false
	/// Console.WriteLine(converter.Convert(new object(), CultureInfo.CurrentCulture));   // true
	/// Console.WriteLine(converter.Convert("text", CultureInfo.CurrentCulture));         // true
	/// Console.WriteLine(converter.Convert(123, CultureInfo.CurrentCulture));            // true
	/// 
	/// converter.NullValue = true;  // Invert logic
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));           // true
	/// Console.WriteLine(converter.Convert(new object(), CultureInfo.CurrentCulture));   // false
	/// </code>
	/// </example>
	public override bool Convert(object? source, CultureInfo culture) => source == null ? NullValue : !NullValue;

	/// <summary>
	///    Converts a boolean value back to an object, with limited support.
	/// </summary>
	/// <param name="result">The boolean value to convert back.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <c>null</c> if <paramref name="result"/> equals <see cref="NullValue"/>.
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when <paramref name="result"/> does not equal <see cref="NullValue"/>.
	///    The converter cannot recreate the original non-null object.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This converter only supports converting back to <c>null</c>. It cannot recreate
	///       the original non-null object because that information is lost during forward conversion.
	///    </para>
	///    <para>
	///       <strong>Supported conversion:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If <paramref name="result"/> equals <see cref="NullValue"/>, returns <c>null</c>
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Unsupported conversion:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If <paramref name="result"/> does not equal <see cref="NullValue"/>,
	///             throws <see cref="NotSupportedException"/> because the original object cannot be reconstructed
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       This limitation means the converter is primarily useful for one-way bindings or scenarios
	///       where you only need to detect and convert null values back.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullToBooleanConverter { NullValue = false };
	/// 
	/// // This works - converting back to null
	/// object result1 = converter.ConvertBack(false, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1 == null);  // Output: true
	/// 
	/// // This throws NotSupportedException - cannot recreate original object
	/// try
	/// {
	///     object result2 = converter.ConvertBack(true, CultureInfo.CurrentCulture);
	/// }
	/// catch (NotSupportedException ex)
	/// {
	///     Console.WriteLine(ex.Message);
	///     // Output: The value 'True' is not supported for 'ConvertBack' method
	/// }
	/// 
	/// // With inverted logic (NullValue = true)
	/// converter.NullValue = true;
	/// object result3 = converter.ConvertBack(true, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3 == null);  // Output: true
	/// </code>
	/// </example>
	public override object? ConvertBack(bool result, CultureInfo culture)
	{
		if (result == NullValue) return null;
		throw new NotSupportedException($"The value '{result}' is not supported for 'ConvertBack' method");
	}
}