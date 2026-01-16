using System.Collections;
using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts an <see cref="ICollection"/> to a boolean value based on its item count.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide automatic
///       conversion from collection instances to boolean values based on whether the collection is empty, has items,
///       or is null. It's particularly useful for controlling UI element visibility, enablement, or other properties
///       based on collection state.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Count-based conversion:</strong> Returns different boolean values for empty vs. non-empty collections
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Configurable behavior for null collections
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable outputs:</strong> Customize boolean values for zero, non-zero, and null states
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Any collection type:</strong> Works with any type implementing <see cref="ICollection"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>No code-behind needed:</strong> Pure XAML solution for collection state detection
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Conversion logic:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Collection State</term>
///          <description>Returned Value</description>
///       </listheader>
///       <item>
///          <term>null</term>
///          <description><see cref="NullValue"/> (default: <c>false</c>)</description>
///       </item>
///       <item>
///          <term>Count == 0</term>
///          <description><see cref="ZeroValue"/> (default: <c>false</c>)</description>
///       </item>
///       <item>
///          <term>Count > 0</term>
///          <description><see cref="NotZeroValue"/> (default: <c>true</c>)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Show/hide "No items" messages based on collection state</description>
///       </item>
///       <item>
///          <description>Enable/disable buttons when collection has items</description>
///       </item>
///       <item>
///          <description>Display different content based on whether collection is empty</description>
///       </item>
///       <item>
///          <description>Trigger validation or warnings for empty collections</description>
///       </item>
///       <item>
///          <description>Combine with other converters for complex visibility logic</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Show message when collection is empty:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToBooleanConverter x:Key="CollectionToBoolConverter"/>
///     <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <ListBox ItemsSource="{Binding Items}"/>
///     
///     <!-- Show when collection is empty -->
///     <TextBlock Text="No items available" 
///                Foreground="Gray"
///                Visibility="{Binding Items, 
///                                     Converter={StaticResource CollectionToBoolConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Enable button only when collection has items:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToBooleanConverter x:Key="HasItemsConverter"/>
/// </Window.Resources>
/// 
/// <Button Content="Process All" 
///         IsEnabled="{Binding SelectedItems, Converter={StaticResource HasItemsConverter}}"/>
/// ]]></code>
///    <strong>Inverted logic - show message when collection has items:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToBooleanConverter x:Key="InvertedConverter"
///                                              ZeroValue="True"
///                                              NotZeroValue="False"/>
///     <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Show when collection has items -->
/// <TextBlock Text="{Binding Items.Count, StringFormat='{}{0} items found'}" 
///            Visibility="{Binding Items, 
///                                 Converter={StaticResource InvertedConverter}}"/>
/// ]]></code>
///    <strong>Handle null collections differently:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToBooleanConverter x:Key="CollectionConverter"
///                                              ZeroValue="False"
///                                              NotZeroValue="True"
///                                              NullValue="False"/>
/// </Window.Resources>
/// 
/// <Button Content="Clear All" 
///         IsEnabled="{Binding Items, Converter={StaticResource CollectionConverter}}"
///         ToolTip="Enabled when collection has items"/>
/// ]]></code>
///    <strong>Multiple scenarios with different configurations:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Standard: true when has items -->
///     <data:ICollectionCountToBooleanConverter x:Key="HasItems"/>
///     
///     <!-- Inverted: true when empty -->
///     <data:ICollectionCountToBooleanConverter x:Key="IsEmpty"
///                                              ZeroValue="True"
///                                              NotZeroValue="False"/>
///     
///     <!-- Null-safe: treat null as empty -->
///     <data:ICollectionCountToBooleanConverter x:Key="HasItemsNullSafe"
///                                              NullValue="False"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Delete button: enabled when has items -->
///     <Button Content="Delete Selected" 
///             IsEnabled="{Binding SelectedItems, Converter={StaticResource HasItems}}"/>
///     
///     <!-- Empty message: visible when empty -->
///     <TextBlock Text="No items selected" 
///                Visibility="{Binding SelectedItems, 
///                                     Converter={StaticResource IsEmpty}}"/>
///     
///     <!-- Export button: enabled when has items, disabled when null -->
///     <Button Content="Export" 
///             IsEnabled="{Binding ExportableItems, 
///                                 Converter={StaticResource HasItemsNullSafe}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new ICollectionCountToBooleanConverter
/// {
///     ZeroValue = false,
///     NotZeroValue = true,
///     NullValue = false
/// };
/// 
/// var emptyList = new List&lt;string&gt;();
/// bool result1 = converter.Convert(emptyList, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: false (Count == 0)
/// 
/// emptyList.Add("item");
/// bool result2 = converter.Convert(emptyList, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: true (Count > 0)
/// 
/// bool result3 = converter.Convert(null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: false (null)
/// </code>
///    <strong>With ObservableCollection for dynamic updates:</strong>
///    <code>
/// // ViewModel
/// public class MyViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;string&gt; Items { get; } = new();
///     
///     public MyViewModel()
///     {
///         // As items are added/removed, bindings using the converter
///         // will automatically update
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- XAML - button automatically enables/disables as Items changes -->
/// <Button Content="Process" 
///         IsEnabled="{Binding Items, Converter={StaticResource HasItemsConverter}}"/>
/// ]]></code>
///    <strong>Combining with MultiBinding for complex logic:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToBooleanConverter x:Key="HasItemsConverter"/>
///     <data:BooleanToBooleanMultiConverter x:Key="AndConverter" Mode="AllTrue"/>
/// </Window.Resources>
/// 
/// <!-- Enable only when collection has items AND user has permission -->
/// <Button Content="Delete">
///     <Button.IsEnabled>
///         <MultiBinding Converter="{StaticResource AndConverter}">
///             <Binding Path="SelectedItems" Converter="{StaticResource HasItemsConverter}"/>
///             <Binding Path="CanDelete"/>
///         </MultiBinding>
///     </Button.IsEnabled>
/// </Button>
/// ]]></code>
/// </example>
public sealed class ICollectionCountToBooleanConverter : GenericConverter<ICollection, bool>
{
	/// <summary>
	///    Gets or sets the boolean value to return when the collection count is zero.
	/// </summary>
	/// <value>
	///    The boolean value for empty collections. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    This value is returned when the collection is not null but has zero items (Count == 0).
	///    Set to <c>true</c> to invert the logic for scenarios where you want to return true for empty collections.
	/// </remarks>
	public bool ZeroValue { get; set; } = false;

	/// <summary>
	///    Gets or sets the boolean value to return when the collection count is greater than zero.
	/// </summary>
	/// <value>
	///    The boolean value for non-empty collections. Default is <c>true</c>.
	/// </value>
	/// <remarks>
	///    This value is returned when the collection is not null and has one or more items (Count > 0).
	///    Set to <c>false</c> to invert the logic for scenarios where you want to return false when collection has items.
	/// </remarks>
	public bool NotZeroValue { get; set; } = true;

	/// <summary>
	///    Gets or sets the boolean value to return when the collection is null.
	/// </summary>
	/// <value>
	///    The boolean value for null collections. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    This value is returned when the source collection reference is null.
	///    Typically set to <c>false</c> to treat null as equivalent to empty.
	/// </remarks>
	public bool NullValue { get; set; } = false;

	/// <summary>
	///    Converts an <see cref="ICollection"/> to a boolean value based on its item count.
	/// </summary>
	/// <param name="source">The collection to evaluate. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A boolean value determined by the collection state:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="NullValue"/> if <paramref name="source"/> is <c>null</c></description>
	///       </item>
	///       <item>
	///          <description><see cref="ZeroValue"/> if <paramref name="source"/>.Count is 0</description>
	///       </item>
	///       <item>
	///          <description><see cref="NotZeroValue"/> if <paramref name="source"/>.Count is greater than 0</description>
	///       </item>
	///    </list>
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method checks the collection state in the following order:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>If the collection is null, returns <see cref="NullValue"/></description>
	///       </item>
	///       <item>
	///          <description>If the collection count is zero, returns <see cref="ZeroValue"/></description>
	///       </item>
	///       <item>
	///          <description>Otherwise (count > 0), returns <see cref="NotZeroValue"/></description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Performance note:</strong> This converter accesses the <see cref="ICollection.Count"/> property,
	///       which is typically an O(1) operation for most collection implementations.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new ICollectionCountToBooleanConverter();
	/// 
	/// var list = new List&lt;int&gt; { 1, 2, 3 };
	/// bool result1 = converter.Convert(list, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: true (has items)
	/// 
	/// list.Clear();
	/// bool result2 = converter.Convert(list, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: false (empty)
	/// 
	/// bool result3 = converter.Convert(null, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3);  // Output: false (null)
	/// </code>
	/// </example>
	public override bool Convert(ICollection source, CultureInfo culture)
	{
		if (source == null) return NullValue;
		if (source.Count == 0) return ZeroValue;
		return NotZeroValue;
	}
}