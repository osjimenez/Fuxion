using System;
using System.Globalization;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts nullable objects to <see cref="Visibility"/> values based on whether they are null or not.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide simple
///       null-checking conversion from any nullable object to a <see cref="Visibility"/> value. It's particularly
///       useful for controlling UI element visibility directly based on whether a value exists or not, without
///       needing to chain with <see cref="BooleanToVisibilityConverter"/>.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Direct visibility control:</strong> Returns visibility directly without boolean intermediary
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable null/not-null values:</strong> Customize visibility for both states independently
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Simple null check:</strong> No complex logic, just null vs. not-null
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
///          <description>Returned Visibility</description>
///       </listheader>
///       <item>
///          <term>null</term>
///          <description><see cref="NullValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>not null</term>
///          <description><see cref="NotNullValue"/> (default: <see cref="Visibility.Visible"/>)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Show/hide panels based on whether data is loaded</description>
///       </item>
///       <item>
///          <description>Display "no data" messages when values are null</description>
///       </item>
///       <item>
///          <description>Control visibility of detail views based on selection</description>
///       </item>
///       <item>
///          <description>Show/hide optional content sections</description>
///       </item>
///       <item>
///          <description>Toggle between empty state and content views</description>
///       </item>
///    </list>
///    <para>
///       <strong>Example usages:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Show content only when data is loaded:</strong>
///             <code><![CDATA[
/// <Window.Resources>
///     <data:NullToVisibilityConverter x:Key="NullToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Visible when CurrentDocument is not null -->
/// <Border Visibility="{Binding CurrentDocument, Converter={StaticResource NullToVisConverter}}">
///     <StackPanel>
///         <TextBlock Text="{Binding CurrentDocument.Title}"/>
///         <TextBlock Text="{Binding CurrentDocument.Content}"/>
///     </StackPanel>
/// </Border>
/// ]]></code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Show "no selection" message when null:</strong>
///             <code><![CDATA[
/// <Window.Resources>
///     <!-- Invert logic: Visible when null, Collapsed when not null -->
///     <data:NullToVisibilityConverter x:Key="InvertedConverter"
///                                     NullValue="Visible"
///                                     NotNullValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Empty state: visible when SelectedItem is null -->
///     <TextBlock Text="No item selected" 
///                Foreground="Gray"
///                Visibility="{Binding SelectedItem, Converter={StaticResource InvertedConverter}}"/>
/// </Grid>
/// ]]></code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Toggle between empty state and content view:</strong>
///             <code><![CDATA[
/// <Window.Resources>
///     <data:NullToVisibilityConverter x:Key="HasDataConverter"
///                                     NullValue="Collapsed"
///                                     NotNullValue="Visible"/>
///     
///     <data:NullToVisibilityConverter x:Key="NoDataConverter"
///                                     NullValue="Visible"
///                                     NotNullValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Content view: visible when data exists -->
///     <ScrollViewer Visibility="{Binding CurrentUser, Converter={StaticResource HasDataConverter}}">
///         <StackPanel>
///             <TextBlock Text="{Binding CurrentUser.Name}"/>
///             <TextBlock Text="{Binding CurrentUser.Email}"/>
///         </StackPanel>
///     </ScrollViewer>
///     
///     <!-- Empty state: visible when no data -->
///     <StackPanel HorizontalAlignment="Center" 
///                 VerticalAlignment="Center"
///                 Visibility="{Binding CurrentUser, Converter={StaticResource NoDataConverter}}">
///         <TextBlock Text="No user logged in" FontSize="18" Foreground="Gray"/>
///         <Button Content="Login" Margin="0,10,0,0"/>
///     </StackPanel>
/// </Grid>
/// ]]></code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Usage in code-behind:</strong>
///             <code>
/// var converter = new NullToVisibilityConverter
/// {
///     NullValue = Visibility.Collapsed,      // Default
///     NotNullValue = Visibility.Visible      // Default
/// };
/// 
/// object obj1 = new object();
/// Visibility result1 = converter.Convert(obj1, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Visible (not null)
/// 
/// object obj2 = null;
/// Visibility result2 = converter.Convert(obj2, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Collapsed (null)
/// 
/// // Inverted logic
/// converter.NullValue = Visibility.Visible;
/// converter.NotNullValue = Visibility.Collapsed;
/// 
/// Visibility result3 = converter.Convert(obj2, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Visible (null, but NullValue=Visible)
/// 
/// Visibility result4 = converter.Convert(obj1, CultureInfo.CurrentCulture);
/// Console.WriteLine(result4);  // Output: Collapsed (not null, but NotNullValue=Collapsed)
/// </code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Master-detail scenario:</strong>
///             <code>
/// public class OrderViewModel : INotifyPropertyChanged
/// {
///     private Order _selectedOrder;
///     
///     public Order SelectedOrder
///     {
///         get => _selectedOrder;
///         set
///         {
///             _selectedOrder = value;
///             OnPropertyChanged();
///         }
///     }
///     
///     public ObservableCollection&lt;Order&gt; Orders { get; } = new();
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullToVisibilityConverter x:Key="NullToVisConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.ColumnDefinitions>
///         <ColumnDefinition Width="*"/>
///         <ColumnDefinition Width="2*"/>
///     </Grid.ColumnDefinitions>
///     
///     <!-- Master list -->
///     <ListBox Grid.Column="0" 
///              ItemsSource="{Binding Orders}"
///              SelectedItem="{Binding SelectedOrder}"/>
///     
///     <!-- Detail view: only visible when order is selected -->
///     <Border Grid.Column="1" 
///             BorderBrush="Gray" 
///             BorderThickness="1"
///             Visibility="{Binding SelectedOrder, Converter={StaticResource NullToVisConverter}}">
///         <StackPanel Margin="10">
///             <TextBlock Text="Order Details" FontSize="18" FontWeight="Bold"/>
///             <TextBlock Text="{Binding SelectedOrder.Id, StringFormat='Order #: {0}'}"/>
///             <TextBlock Text="{Binding SelectedOrder.CustomerName}"/>
///             <TextBlock Text="{Binding SelectedOrder.Total, StringFormat='Total: {0:C}'}"/>
///         </StackPanel>
///     </Border>
/// </Grid>
/// ]]></code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Using Hidden to preserve layout space:</strong>
///             <code><![CDATA[
/// <Window.Resources>
///     <data:NullToVisibilityConverter x:Key="PreserveSpaceConverter"
///                                     NullValue="Hidden"
///                                     NotNullValue="Visible"/>
/// </Window.Resources>
/// 
/// <!-- Control is invisible but preserves space when ProgressInfo is null -->
/// <ProgressBar Value="{Binding ProgressInfo.Percentage}"
///              Visibility="{Binding ProgressInfo, 
///                                  Converter={StaticResource PreserveSpaceConverter}}"/>
/// ]]></code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multiple states with different converters:</strong>
///             <code><![CDATA[
/// <Window.Resources>
///     <data:NullToVisibilityConverter x:Key="ShowWhenLoaded"/>
///     <data:NullToVisibilityConverter x:Key="ShowWhenEmpty"
///                                     NullValue="Visible"
///                                     NotNullValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Loading indicator: shown during load (when data is null) -->
///     <ProgressBar IsIndeterminate="True"
///                  Visibility="{Binding LoadedData, 
///                                      Converter={StaticResource ShowWhenEmpty}}"/>
///     
///     <!-- Data view: shown when loaded (not null) -->
///     <ItemsControl ItemsSource="{Binding LoadedData.Items}"
///                   Visibility="{Binding LoadedData, 
///                                       Converter={StaticResource ShowWhenLoaded}}"/>
/// </StackPanel>
/// ]]></code>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>ConvertBack usage (limited):</strong>
///             <code>
/// var converter = new NullToVisibilityConverter
/// {
///     NullValue = Visibility.Collapsed,
///     NotNullValue = Visibility.Visible
/// };
/// 
/// // Convert back to null works when result matches NullValue
/// object result1 = converter.ConvertBack(Visibility.Collapsed, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1 == null);  // Output: true
/// 
/// // Convert back to non-null throws exception (cannot recreate original object)
/// try
/// {
///     object result2 = converter.ConvertBack(Visibility.Visible, CultureInfo.CurrentCulture);
/// }
/// catch (NotSupportedException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // Output: The value 'Visible' is not supported for 'ConvertBack' method
/// }
/// </code>
///          </description>
///       </item>
///    </list>
/// </remarks>
public class NullToVisibilityConverter : GenericConverter<object?, Visibility>
{
	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source object is null.
	/// </summary>
	/// <value>
	///    The visibility state for null objects. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       Common configurations:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="Visibility.Collapsed"/> (default): Element is hidden and doesn't occupy space when null
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Visible"/>: Element is visible when null (inverted logic)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Hidden"/>: Element is invisible but preserves layout space when null
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public Visibility NullValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source object is not null.
	/// </summary>
	/// <value>
	///    The visibility state for non-null objects. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       Common configurations:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="Visibility.Visible"/> (default): Element is visible when not null
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Collapsed"/>: Element is hidden when not null (inverted logic)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Hidden"/>: Element is invisible but preserves space when not null
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public Visibility NotNullValue { get; set; } = Visibility.Visible;

	/// <summary>
	///    Converts a nullable object to a <see cref="Visibility"/> value based on whether it is null.
	/// </summary>
	/// <param name="source">The object to evaluate. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    <see cref="NullValue"/> if <paramref name="source"/> is <c>null</c>;
	///    otherwise, <see cref="NotNullValue"/>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This is a simple null check conversion with direct visibility mapping.
	///       The logic is: <c>source == null ? NullValue : NotNullValue</c>
	///    </para>
	///    <para>
	///       <strong>Examples with default values:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>null → Collapsed</description></item>
	///       <item><description>new object() → Visible</description></item>
	///       <item><description>"text" → Visible</description></item>
	///       <item><description>0 → Visible</description></item>
	///    </list>
	///    <para>
	///       <strong>Examples with inverted configuration (NullValue=Visible, NotNullValue=Collapsed):</strong>
	///    </para>
	///    <list type="bullet">
	///       <item><description>null → Visible</description></item>
	///       <item><description>new object() → Collapsed</description></item>
	///       <item><description>"text" → Collapsed</description></item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullToVisibilityConverter();  // Default values
	/// 
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));           // Collapsed
	/// Console.WriteLine(converter.Convert(new object(), CultureInfo.CurrentCulture));   // Visible
	/// Console.WriteLine(converter.Convert("text", CultureInfo.CurrentCulture));         // Visible
	/// Console.WriteLine(converter.Convert(123, CultureInfo.CurrentCulture));            // Visible
	/// 
	/// // Inverted logic
	/// converter.NullValue = Visibility.Visible;
	/// converter.NotNullValue = Visibility.Collapsed;
	/// 
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));           // Visible
	/// Console.WriteLine(converter.Convert(new object(), CultureInfo.CurrentCulture));   // Collapsed
	/// </code>
	/// </example>
	public override Visibility Convert(object? source, CultureInfo culture) => source == null ? NullValue : NotNullValue;

	/// <summary>
	///    Converts a <see cref="Visibility"/> value back to an object, with limited support.
	/// </summary>
	/// <param name="result">The <see cref="Visibility"/> value to convert back.</param>
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
	/// var converter = new NullToVisibilityConverter
	/// {
	///     NullValue = Visibility.Collapsed,
	///     NotNullValue = Visibility.Visible
	/// };
	/// 
	/// // This works - converting back to null
	/// object result1 = converter.ConvertBack(Visibility.Collapsed, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1 == null);  // Output: true
	/// 
	/// // This throws NotSupportedException - cannot recreate original object
	/// try
	/// {
	///     object result2 = converter.ConvertBack(Visibility.Visible, CultureInfo.CurrentCulture);
	/// }
	/// catch (NotSupportedException ex)
	/// {
	///     Console.WriteLine(ex.Message);
	///     // Output: The value 'Visible' is not supported for 'ConvertBack' method
	/// }
	/// 
	/// // With inverted logic
	/// converter.NullValue = Visibility.Visible;
	/// converter.NotNullValue = Visibility.Collapsed;
	/// 
	/// object result3 = converter.ConvertBack(Visibility.Visible, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3 == null);  // Output: true
	/// </code>
	/// </example>
	public override object? ConvertBack(Visibility result, CultureInfo culture)
	{
		if (result == NullValue) return null;
		throw new NotSupportedException($"The value '{result}' is not supported for 'ConvertBack' method");
	}
}