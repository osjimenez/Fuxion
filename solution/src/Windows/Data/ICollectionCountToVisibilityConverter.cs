using System.Collections;
using System.Globalization;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts an <see cref="ICollection"/> to a <see cref="Visibility"/> value based on its item count.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide automatic
///       conversion from collection instances to <see cref="Visibility"/> values based on whether the collection is empty,
///       has items, or is null. It's particularly useful for controlling UI element visibility based on collection state
///       without needing additional boolean converters.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Count-based visibility:</strong> Returns different visibility values for empty vs. non-empty collections
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Configurable visibility for null collections
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable outputs:</strong> Customize visibility for zero, non-zero, and null states
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Any collection type:</strong> Works with any type implementing <see cref="ICollection"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Direct visibility control:</strong> No need to chain with BooleanToVisibilityConverter
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
///          <description><see cref="NullValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>Count == 0</term>
///          <description><see cref="ZeroValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>Count > 0</term>
///          <description><see cref="NotZeroValue"/> (default: <see cref="Visibility.Visible"/>)</description>
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
///          <description>Display content panels only when data is available</description>
///       </item>
///       <item>
///          <description>Toggle visibility of action buttons based on selection</description>
///       </item>
///       <item>
///          <description>Show/hide loading indicators or empty state messages</description>
///       </item>
///       <item>
///          <description>Control visibility of data grids or lists with dynamic content</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Show message when collection is empty:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToVisibilityConverter x:Key="EmptyCollectionConverter"
///                                                 ZeroValue="Visible"
///                                                 NotZeroValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <ListBox ItemsSource="{Binding Items}"/>
///     
///     <!-- Visible when collection is empty -->
///     <TextBlock Text="No items available" 
///                Foreground="Gray"
///                Visibility="{Binding Items, Converter={StaticResource EmptyCollectionConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Show content only when collection has items:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToVisibilityConverter x:Key="HasItemsConverter"/>
/// </Window.Resources>
/// 
/// <!-- Visible only when collection has items -->
/// <Border BorderBrush="Gray" 
///         BorderThickness="1" 
///         Visibility="{Binding Products, Converter={StaticResource HasItemsConverter}}">
///     <ItemsControl ItemsSource="{Binding Products}"/>
/// </Border>
/// ]]></code>
///    <strong>Toggle between empty state and data view:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToVisibilityConverter x:Key="HasItemsConverter"
///                                                 ZeroValue="Collapsed"
///                                                 NotZeroValue="Visible"/>
///     <data:ICollectionCountToVisibilityConverter x:Key="EmptyConverter"
///                                                 ZeroValue="Visible"
///                                                 NotZeroValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Data view: visible when has items -->
///     <DataGrid ItemsSource="{Binding Orders}" 
///               Visibility="{Binding Orders, Converter={StaticResource HasItemsConverter}}"/>
///     
///     <!-- Empty state: visible when no items -->
///     <StackPanel HorizontalAlignment="Center" 
///                 VerticalAlignment="Center"
///                 Visibility="{Binding Orders, Converter={StaticResource EmptyConverter}}">
///         <TextBlock Text="No orders found" FontSize="16" Foreground="Gray"/>
///         <Button Content="Create New Order" Margin="0,10,0,0"/>
///     </StackPanel>
/// </Grid>
/// ]]></code>
///    <strong>Control button visibility based on selection:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToVisibilityConverter x:Key="HasSelectionConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <ListBox x:Name="ItemsList" SelectionMode="Multiple"/>
///     
///     <!-- Buttons visible only when items are selected -->
///     <StackPanel Orientation="Horizontal" 
///                 Visibility="{Binding SelectedItems, 
///                                     ElementName=ItemsList, 
///                                     Converter={StaticResource HasSelectionConverter}}">
///         <Button Content="Delete Selected"/>
///         <Button Content="Export Selected"/>
///     </StackPanel>
/// </StackPanel>
/// ]]></code>
///    <strong>Handle null collections with Hidden instead of Collapsed:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToVisibilityConverter x:Key="CollectionConverter"
///                                                 ZeroValue="Collapsed"
///                                                 NotZeroValue="Visible"
///                                                 NullValue="Hidden"/>
/// </Window.Resources>
/// 
/// <!-- Preserves layout space when collection is null -->
/// <Border Visibility="{Binding Items, Converter={StaticResource CollectionConverter}}">
///     <ItemsControl ItemsSource="{Binding Items}"/>
/// </Border>
/// ]]></code>
///    <strong>Multiple scenarios with different configurations:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Show when has items -->
///     <data:ICollectionCountToVisibilityConverter x:Key="ShowWhenHasItems"
///                                                 ZeroValue="Collapsed"
///                                                 NotZeroValue="Visible"/>
///     
///     <!-- Show when empty -->
///     <data:ICollectionCountToVisibilityConverter x:Key="ShowWhenEmpty"
///                                                 ZeroValue="Visible"
///                                                 NotZeroValue="Collapsed"/>
///     
///     <!-- Hide when empty (preserves space) -->
///     <data:ICollectionCountToVisibilityConverter x:Key="HideWhenEmpty"
///                                                 ZeroValue="Hidden"
///                                                 NotZeroValue="Visible"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Count badge: visible when has items -->
///     <Border Background="Red" 
///             CornerRadius="10"
///             Visibility="{Binding Notifications, Converter={StaticResource ShowWhenHasItems}}">
///         <TextBlock Text="{Binding Notifications.Count}" Foreground="White"/>
///     </Border>
///     
///     <!-- Empty message: visible when empty -->
///     <TextBlock Text="No notifications" 
///                Visibility="{Binding Notifications, Converter={StaticResource ShowWhenEmpty}}"/>
///     
///     <!-- List: hidden but preserves space when empty -->
///     <ListBox ItemsSource="{Binding Notifications}"
///              Visibility="{Binding Notifications, Converter={StaticResource HideWhenEmpty}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new ICollectionCountToVisibilityConverter
/// {
///     ZeroValue = Visibility.Collapsed,
///     NotZeroValue = Visibility.Visible,
///     NullValue = Visibility.Collapsed
/// };
/// 
/// var emptyList = new List&lt;string&gt;();
/// Visibility result1 = converter.Convert(emptyList, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Collapsed
/// 
/// emptyList.Add("item");
/// Visibility result2 = converter.Convert(emptyList, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Visible
/// 
/// Visibility result3 = converter.Convert(null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Collapsed
/// </code>
///    <strong>With ObservableCollection for dynamic UI updates:</strong>
///    <code>
/// // ViewModel
/// public class ShoppingCartViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;Product&gt; CartItems { get; } = new();
///     
///     public void AddProduct(Product product)
///     {
///         CartItems.Add(product);
///         // UI elements bound with the converter will automatically update
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- XAML - visibility updates automatically as CartItems changes -->
/// <StackPanel>
///     <!-- Cart content: visible when has items -->
///     <ItemsControl ItemsSource="{Binding CartItems}"
///                   Visibility="{Binding CartItems, 
///                                       Converter={StaticResource ShowWhenHasItems}}"/>
///     
///     <!-- Empty cart message: visible when no items -->
///     <TextBlock Text="Your cart is empty"
///                Visibility="{Binding CartItems, 
///                                    Converter={StaticResource ShowWhenEmpty}}"/>
///     
///     <!-- Checkout button: visible when has items -->
///     <Button Content="Checkout"
///             Visibility="{Binding CartItems, 
///                                 Converter={StaticResource ShowWhenHasItems}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Complex layout with loading states:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ICollectionCountToVisibilityConverter x:Key="HasDataConverter"/>
///     <data:ICollectionCountToVisibilityConverter x:Key="NoDataConverter"
///                                                 ZeroValue="Visible"
///                                                 NotZeroValue="Collapsed"
///                                                 NullValue="Collapsed"/>
///     <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Loading indicator -->
///     <ProgressBar IsIndeterminate="True"
///                  Visibility="{Binding IsLoading, 
///                                      Converter={StaticResource BoolToVisConverter}}"/>
///     
///     <!-- Data view: visible when loaded and has data -->
///     <ScrollViewer Visibility="{Binding Items, 
///                                       Converter={StaticResource HasDataConverter}}">
///         <ItemsControl ItemsSource="{Binding Items}"/>
///     </ScrollViewer>
///     
///     <!-- Empty state: visible when loaded but no data -->
///     <StackPanel HorizontalAlignment="Center" 
///                 VerticalAlignment="Center"
///                 Visibility="{Binding Items, 
///                                     Converter={StaticResource NoDataConverter}}">
///         <TextBlock Text="No data available" FontSize="18"/>
///         <Button Content="Refresh" Command="{Binding RefreshCommand}" Margin="0,10,0,0"/>
///     </StackPanel>
/// </Grid>
/// ]]></code>
/// </example>
public sealed class ICollectionCountToVisibilityConverter : GenericConverter<ICollection, Visibility>
{
	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the collection count is zero.
	/// </summary>
	/// <value>
	///    The visibility state for empty collections. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    This value is returned when the collection is not null but has zero items (Count == 0).
	///    Common configurations:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="Visibility.Collapsed"/>: Element is hidden and doesn't occupy space</description>
	///       </item>
	///       <item>
	///          <description><see cref="Visibility.Visible"/>: Element is visible (inverted logic for empty state messages)</description>
	///       </item>
	///       <item>
	///          <description><see cref="Visibility.Hidden"/>: Element is invisible but preserves layout space</description>
	///       </item>
	///    </list>
	/// </remarks>
	public Visibility ZeroValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the collection count is greater than zero.
	/// </summary>
	/// <value>
	///    The visibility state for non-empty collections. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    This value is returned when the collection is not null and has one or more items (Count > 0).
	///    Common configurations:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="Visibility.Visible"/>: Element is visible when collection has items</description>
	///       </item>
	///       <item>
	///          <description><see cref="Visibility.Collapsed"/>: Element is hidden when collection has items (inverted logic)</description>
	///       </item>
	///       <item>
	///          <description><see cref="Visibility.Hidden"/>: Element is invisible but preserves space</description>
	///       </item>
	///    </list>
	/// </remarks>
	public Visibility NotZeroValue { get; set; } = Visibility.Visible;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the collection is null.
	/// </summary>
	/// <value>
	///    The visibility state for null collections. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    This value is returned when the source collection reference is null.
	///    Typically set to <see cref="Visibility.Collapsed"/> to treat null as equivalent to empty.
	/// </remarks>
	public Visibility NullValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Converts an <see cref="ICollection"/> to a <see cref="Visibility"/> value based on its item count.
	/// </summary>
	/// <param name="source">The collection to evaluate. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A <see cref="Visibility"/> value determined by the collection state:
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
	/// var converter = new ICollectionCountToVisibilityConverter();
	/// 
	/// var list = new List&lt;int&gt; { 1, 2, 3 };
	/// Visibility result1 = converter.Convert(list, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Output: Visible
	/// 
	/// list.Clear();
	/// Visibility result2 = converter.Convert(list, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: Collapsed
	/// 
	/// Visibility result3 = converter.Convert(null, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3);  // Output: Collapsed
	/// </code>
	/// </example>
	public override Visibility Convert(ICollection source, CultureInfo culture)
	{
		if (source == null) return NullValue;
		if (source.Count == 0) return ZeroValue;
		return NotZeroValue;
	}
}