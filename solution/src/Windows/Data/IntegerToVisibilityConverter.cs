using System.Globalization;
using System.Linq;
using System.Windows;
using Fuxion.Collections.Generic;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts an integer value to a <see cref="Visibility"/> value based on
///    configurable comma-separated lists of values for each visibility state.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide flexible
///       integer-to-visibility conversion where you can define which integer values map to
///       <see cref="Visibility.Visible"/>, <see cref="Visibility.Collapsed"/>, or <see cref="Visibility.Hidden"/>.
///       It's particularly useful for controlling UI element visibility based on enumeration values, status codes,
///       or other integer-based states.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Declarative mapping:</strong> Define visibility mappings using comma-separated strings in XAML
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multiple value support:</strong> Map multiple integers to each visibility state
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Fallback value:</strong> Configurable default visibility for unmapped values
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible configuration:</strong> Set different values for Visible, Collapsed, and Hidden states
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Parse resilience:</strong> Invalid values in comma-separated lists are automatically ignored
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>How it works:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>
///             Checks if source value is in <see cref="VisibleValuesCommaSeparated"/> list ? returns <see cref="Visibility.Visible"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             Checks if source value is in <see cref="CollapsedValuesCommaSeparated"/> list ? returns <see cref="Visibility.Collapsed"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             Checks if source value is in <see cref="HiddenValuesCommaSeparated"/> list ? returns <see cref="Visibility.Hidden"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             If not found in any list, returns <see cref="NonDeclaredValue"/>
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Control visibility based on enum numeric values</description>
///       </item>
///       <item>
///          <description>Show/hide elements based on status codes</description>
///       </item>
///       <item>
///          <description>Display different content for different state values</description>
///       </item>
///       <item>
///          <description>Create complex visibility logic without code-behind</description>
///       </item>
///       <item>
///          <description>Map multiple similar states to the same visibility</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage with enum values:</strong>
///    <code>
/// public enum OrderStatus
/// {
///     Pending = 0,
///     Approved = 1,
///     InProgress = 2,
///     Completed = 3,
///     Cancelled = 4
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Show for Pending and Approved -->
///     <data:IntegerToVisibilityConverter x:Key="PendingApprovedConverter"
///                                        VisibleValuesCommaSeparated="0,1"
///                                        CollapsedValuesCommaSeparated="2,3,4"/>
/// </Window.Resources>
/// 
/// <!-- Visible only when status is 0 (Pending) or 1 (Approved) -->
/// <TextBlock Text="Waiting for processing..." 
///            Visibility="{Binding StatusValue, Converter={StaticResource PendingApprovedConverter}}"/>
/// ]]></code>
///    <strong>Show different elements for different status groups:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Show for active statuses -->
///     <data:IntegerToVisibilityConverter x:Key="ActiveConverter"
///                                        VisibleValuesCommaSeparated="0,1,2"
///                                        CollapsedValuesCommaSeparated="3,4"/>
///     
///     <!-- Show for completed statuses -->
///     <data:IntegerToVisibilityConverter x:Key="CompletedConverter"
///                                        VisibleValuesCommaSeparated="3"
///                                        CollapsedValuesCommaSeparated="0,1,2,4"/>
///     
///     <!-- Show for cancelled status -->
///     <data:IntegerToVisibilityConverter x:Key="CancelledConverter"
///                                        VisibleValuesCommaSeparated="4"
///                                        CollapsedValuesCommaSeparated="0,1,2,3"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Active status message -->
///     <Border Background="LightBlue" 
///             Visibility="{Binding StatusValue, Converter={StaticResource ActiveConverter}}">
///         <TextBlock Text="Order in progress"/>
///     </Border>
///     
///     <!-- Completed status message -->
///     <Border Background="LightGreen" 
///             Visibility="{Binding StatusValue, Converter={StaticResource CompletedConverter}}">
///         <TextBlock Text="Order completed"/>
///     </Border>
///     
///     <!-- Cancelled status message -->
///     <Border Background="LightCoral" 
///             Visibility="{Binding StatusValue, Converter={StaticResource CancelledConverter}}">
///         <TextBlock Text="Order cancelled"/>
///     </Border>
/// </StackPanel>
/// ]]></code>
///    <strong>Using Hidden for layout preservation:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Hide (not collapse) for certain values -->
///     <data:IntegerToVisibilityConverter x:Key="StatusConverter"
///                                        VisibleValuesCommaSeparated="1,2,3"
///                                        HiddenValuesCommaSeparated="0"
///                                        CollapsedValuesCommaSeparated="4"/>
/// </Window.Resources>
/// 
/// <!-- Visible: 1,2,3 | Hidden (preserves space): 0 | Collapsed: 4 -->
/// <TextBlock Text="Processing..." 
///            Visibility="{Binding Status, Converter={StaticResource StatusConverter}}"/>
/// ]]></code>
///    <strong>With default value for unmapped integers:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Only define Visible values, all others use NonDeclaredValue -->
///     <data:IntegerToVisibilityConverter x:Key="ImportantStatusConverter"
///                                        VisibleValuesCommaSeparated="1,2,3"
///                                        NonDeclaredValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <!-- Visible for 1,2,3; Collapsed for any other value -->
/// <TextBlock Text="Important status" 
///            Visibility="{Binding StatusCode, Converter={StaticResource ImportantStatusConverter}}"/>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new IntegerToVisibilityConverter
/// {
///     VisibleValuesCommaSeparated = "1,2,3",
///     CollapsedValuesCommaSeparated = "0,4",
///     NonDeclaredValue = Visibility.Hidden
/// };
/// 
/// Visibility result1 = converter.Convert(1, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Visible
/// 
/// Visibility result2 = converter.Convert(0, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Collapsed
/// 
/// Visibility result3 = converter.Convert(99, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Hidden (NonDeclaredValue)
/// </code>
///    <strong>Complex status dashboard:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:IntegerToVisibilityConverter x:Key="ErrorsConverter"
///                                        VisibleValuesCommaSeparated="1,2,3,4,5,6,7,8,9"
///                                        CollapsedValuesCommaSeparated="0"/>
///     
///     <data:IntegerToVisibilityConverter x:Key="SuccessConverter"
///                                        VisibleValuesCommaSeparated="0"
///                                        CollapsedValuesCommaSeparated="1,2,3,4,5,6,7,8,9"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Error indicator: visible when ErrorCount > 0 -->
///     <Border Background="Red" 
///             Visibility="{Binding ErrorCount, Converter={StaticResource ErrorsConverter}}">
///         <StackPanel>
///             <TextBlock Text="?" FontSize="24" Foreground="White"/>
///             <TextBlock Text="{Binding ErrorCount, StringFormat='{}{0} errors'}" 
///                        Foreground="White"/>
///         </StackPanel>
///     </Border>
///     
///     <!-- Success indicator: visible when ErrorCount = 0 -->
///     <Border Background="Green" 
///             Visibility="{Binding ErrorCount, Converter={StaticResource SuccessConverter}}">
///         <TextBlock Text="? All systems operational" 
///                    FontSize="16" 
///                    Foreground="White"/>
///     </Border>
/// </Grid>
/// ]]></code>
///    <strong>Priority-based visibility:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- High priority: 1,2 -->
///     <data:IntegerToVisibilityConverter x:Key="HighPriorityConverter"
///                                        VisibleValuesCommaSeparated="1,2"
///                                        NonDeclaredValue="Collapsed"/>
///     
///     <!-- Medium priority: 3,4,5 -->
///     <data:IntegerToVisibilityConverter x:Key="MediumPriorityConverter"
///                                        VisibleValuesCommaSeparated="3,4,5"
///                                        NonDeclaredValue="Collapsed"/>
///     
///     <!-- Low priority: 6,7,8,9 -->
///     <data:IntegerToVisibilityConverter x:Key="LowPriorityConverter"
///                                        VisibleValuesCommaSeparated="6,7,8,9"
///                                        NonDeclaredValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <ItemsControl ItemsSource="{Binding Tasks}">
///     <ItemsControl.ItemTemplate>
///         <DataTemplate>
///             <Border>
///                 <!-- High priority badge -->
///                 <Border Background="Red" 
///                         CornerRadius="3" 
///                         Padding="5,2"
///                         Visibility="{Binding Priority, Converter={StaticResource HighPriorityConverter}}">
///                     <TextBlock Text="HIGH" Foreground="White" FontWeight="Bold"/>
///                 </Border>
///                 
///                 <!-- Medium priority badge -->
///                 <Border Background="Orange" 
///                         CornerRadius="3" 
///                         Padding="5,2"
///                         Visibility="{Binding Priority, Converter={StaticResource MediumPriorityConverter}}">
///                     <TextBlock Text="MEDIUM" Foreground="White"/>
///                 </Border>
///                 
///                 <!-- Low priority badge -->
///                 <Border Background="Gray" 
///                         CornerRadius="3" 
///                         Padding="5,2"
///                         Visibility="{Binding Priority, Converter={StaticResource LowPriorityConverter}}">
///                     <TextBlock Text="LOW" Foreground="White"/>
///                 </Border>
///             </Border>
///         </DataTemplate>
///     </ItemsControl.ItemTemplate>
/// </ItemsControl>
/// ]]></code>
/// </example>
public class IntegerToVisibilityConverter : GenericConverter<int, Visibility>
{
	/// <summary>
	///    Gets or sets a comma-separated list of integer values that should return <see cref="Visibility.Visible"/>.
	/// </summary>
	/// <value>
	///    A comma-separated string of integer values (e.g., "1,2,3"). Default is <c>null</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       Values in this list that cannot be parsed as integers are automatically ignored.
	///       If this property is <c>null</c> or empty, no values will map to <see cref="Visibility.Visible"/>
	///       through this property.
	///    </para>
	///    <para>
	///       <strong>Example values:</strong> "0", "1,2,3", "10,20,30,40"
	///    </para>
	/// </remarks>
	public string? VisibleValuesCommaSeparated { get; set; }

	/// <summary>
	///    Gets or sets a comma-separated list of integer values that should return <see cref="Visibility.Collapsed"/>.
	/// </summary>
	/// <value>
	///    A comma-separated string of integer values (e.g., "4,5,6"). Default is <c>null</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       Values in this list that cannot be parsed as integers are automatically ignored.
	///       If this property is <c>null</c> or empty, no values will map to <see cref="Visibility.Collapsed"/>
	///       through this property.
	///    </para>
	///    <para>
	///       Elements with <see cref="Visibility.Collapsed"/> are hidden and do not occupy layout space.
	///    </para>
	/// </remarks>
	public string? CollapsedValuesCommaSeparated { get; set; }

	/// <summary>
	///    Gets or sets a comma-separated list of integer values that should return <see cref="Visibility.Hidden"/>.
	/// </summary>
	/// <value>
	///    A comma-separated string of integer values (e.g., "7,8,9"). Default is <c>null</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       Values in this list that cannot be parsed as integers are automatically ignored.
	///       If this property is <c>null</c> or empty, no values will map to <see cref="Visibility.Hidden"/>
	///       through this property.
	///    </para>
	///    <para>
	///       Elements with <see cref="Visibility.Hidden"/> are invisible but still occupy layout space,
	///       unlike <see cref="Visibility.Collapsed"/>.
	///    </para>
	/// </remarks>
	public string? HiddenValuesCommaSeparated { get; set; }

	/// <summary>
	///    Gets or sets the default <see cref="Visibility"/> value to return when the source integer
	///    is not found in any of the configured lists.
	/// </summary>
	/// <value>
	///    The fallback visibility value. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    This value is returned when the source integer doesn't match any value in
	///    <see cref="VisibleValuesCommaSeparated"/>, <see cref="CollapsedValuesCommaSeparated"/>,
	///    or <see cref="HiddenValuesCommaSeparated"/>.
	/// </remarks>
	public Visibility NonDeclaredValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Converts an integer value to a <see cref="Visibility"/> value based on the configured
	///    comma-separated lists.
	/// </summary>
	/// <param name="source">The integer value to convert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A <see cref="Visibility"/> value determined by checking the source value against the configured lists:
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="Visibility.Visible"/> if found in <see cref="VisibleValuesCommaSeparated"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Collapsed"/> if found in <see cref="CollapsedValuesCommaSeparated"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="Visibility.Hidden"/> if found in <see cref="HiddenValuesCommaSeparated"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="NonDeclaredValue"/> if not found in any list
	///          </description>
	///       </item>
	///    </list>
	/// </returns>
	/// <remarks>
	///    <para>
	///       The conversion process:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             Splits each comma-separated property by comma
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Attempts to parse each value as an integer (invalid values are ignored)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Checks if source matches any parsed value in priority order: Visible ? Collapsed ? Hidden
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Returns the corresponding visibility or <see cref="NonDeclaredValue"/> if no match
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Performance note:</strong> The string parsing happens on every conversion call.
	///       For high-frequency scenarios, consider caching the parsed values or using a different approach.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new IntegerToVisibilityConverter
	/// {
	///     VisibleValuesCommaSeparated = "1,2,3",
	///     CollapsedValuesCommaSeparated = "4,5",
	///     HiddenValuesCommaSeparated = "6",
	///     NonDeclaredValue = Visibility.Collapsed
	/// };
	/// 
	/// Console.WriteLine(converter.Convert(1, CultureInfo.CurrentCulture));  // Output: Visible
	/// Console.WriteLine(converter.Convert(4, CultureInfo.CurrentCulture));  // Output: Collapsed
	/// Console.WriteLine(converter.Convert(6, CultureInfo.CurrentCulture));  // Output: Hidden
	/// Console.WriteLine(converter.Convert(99, CultureInfo.CurrentCulture)); // Output: Collapsed (NonDeclaredValue)
	/// </code>
	/// </example>
	public override Visibility Convert(int source, CultureInfo culture) =>
		VisibleValuesCommaSeparated?.Split(',').Select(v => {
			if (int.TryParse(v, out var res)) return res;
			return (int?)null;
		}).WhereNotNull().Contains(source) ?? false ? Visibility.Visible :
		CollapsedValuesCommaSeparated?.Split(',').Select(v => {
			if (int.TryParse(v, out var res)) return res;
			return (int?)null;
		}).WhereNotNull().Contains(source) ?? false ? Visibility.Collapsed :
		HiddenValuesCommaSeparated?.Split(',').Select(v => {
			if (int.TryParse(v, out var res)) return res;
			return (int?)null;
		}).WhereNotNull().Contains(source) ?? false ? Visibility.Hidden : NonDeclaredValue;
}