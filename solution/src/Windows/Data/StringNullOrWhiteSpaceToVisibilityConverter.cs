using System.Globalization;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts strings to <see cref="Visibility"/> values based on whether they are
///    null, empty, whitespace-only, or contain actual content.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide direct string-to-visibility
///       conversion with four distinct states: null, empty string, whitespace-only, and valid content. It's particularly
///       useful for controlling UI element visibility based on string content without needing intermediate boolean
///       conversions or multiple converters.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Four-state detection:</strong> Distinguishes between null, empty (""), whitespace-only, and valid strings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Independent configuration:</strong> Set different visibility for each of the four states
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Direct visibility control:</strong> No need for boolean intermediaries or chaining converters
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Optimized for forward conversion (no ConvertBack implementation)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Common defaults:</strong> Collapses empty/null/whitespace, shows valid content by default
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Conversion logic:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Input State</term>
///          <description>Returned Visibility</description>
///       </listheader>
///       <item>
///          <term>null</term>
///          <description><see cref="NullValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>"" (empty string)</term>
///          <description><see cref="EmptyValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>Whitespace only (e.g., "   ", "\t", "\n")</term>
///          <description><see cref="WhiteSpaceValue"/> (default: <see cref="Visibility.Collapsed"/>)</description>
///       </item>
///       <item>
///          <term>Valid string (non-whitespace content)</term>
///          <description><see cref="OtherValue"/> (default: <see cref="Visibility.Visible"/>)</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Show/hide UI elements based on whether text fields have content</description>
///       </item>
///       <item>
///          <description>Display validation messages only when fields are empty or invalid</description>
///       </item>
///       <item>
///          <description>Control visibility of detail panels based on data availability</description>
///       </item>
///       <item>
///          <description>Toggle between empty state placeholders and actual content</description>
///       </item>
///       <item>
///          <description>Show/hide search results based on query text</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Show element only when text has content:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="StringToVisConverter"/>
/// </Window.Resources>
/// 
/// <!-- Visible only when SearchText has actual content -->
/// <Button Content="Search" 
///         Visibility="{Binding SearchText, Converter={StaticResource StringToVisConverter}}"/>
/// ]]></code>
///    <strong>Show validation message when field is empty:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Invert logic: Visible when empty, Collapsed when has content -->
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="EmptyConverter"
///                                                       NullValue="Visible"
///                                                       EmptyValue="Visible"
///                                                       WhiteSpaceValue="Visible"
///                                                       OtherValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <TextBox Text="{Binding UserName, UpdateSourceTrigger=PropertyChanged}"/>
///     
///     <!-- Visible when UserName is empty -->
///     <TextBlock Text="⚠ Username is required" 
///                Foreground="Red"
///                Visibility="{Binding UserName, Converter={StaticResource EmptyConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Toggle between empty state and content:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="HasContentConverter"/>
///     
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="EmptyStateConverter"
///                                                       NullValue="Visible"
///                                                       EmptyValue="Visible"
///                                                       WhiteSpaceValue="Visible"
///                                                       OtherValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Content view: visible when Description has content -->
///     <ScrollViewer Visibility="{Binding Description, Converter={StaticResource HasContentConverter}}">
///         <TextBlock Text="{Binding Description}" TextWrapping="Wrap"/>
///     </ScrollViewer>
///     
///     <!-- Empty state: visible when Description is empty -->
///     <StackPanel HorizontalAlignment="Center" 
///                 VerticalAlignment="Center"
///                 Visibility="{Binding Description, Converter={StaticResource EmptyStateConverter}}">
///         <TextBlock Text="No description available" 
///                    FontSize="16" 
///                    Foreground="Gray"/>
///         <Button Content="Add Description" Margin="0,10,0,0"/>
///     </StackPanel>
/// </Grid>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new StringNullOrWhiteSpaceToVisibilityConverter
/// {
///     NullValue = Visibility.Collapsed,      // Default
///     EmptyValue = Visibility.Collapsed,     // Default
///     WhiteSpaceValue = Visibility.Collapsed,// Default
///     OtherValue = Visibility.Visible        // Default
/// };
/// 
/// Visibility result1 = converter.Convert(null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Collapsed
/// 
/// Visibility result2 = converter.Convert("", CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Collapsed
/// 
/// Visibility result3 = converter.Convert("   ", CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Collapsed
/// 
/// Visibility result4 = converter.Convert("\t\n  ", CultureInfo.CurrentCulture);
/// Console.WriteLine(result4);  // Output: Collapsed
/// 
/// Visibility result5 = converter.Convert("Hello", CultureInfo.CurrentCulture);
/// Console.WriteLine(result5);  // Output: Visible
/// </code>
///    <strong>Different visibility for each state:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="CustomConverter"
///                                                       NullValue="Hidden"
///                                                       EmptyValue="Collapsed"
///                                                       WhiteSpaceValue="Collapsed"
///                                                       OtherValue="Visible"/>
/// </Window.Resources>
/// 
/// <!-- Hidden when null (preserves space), Collapsed when empty/whitespace, Visible when has content -->
/// <Border Visibility="{Binding StatusMessage, Converter={StaticResource CustomConverter}}">
///     <TextBlock Text="{Binding StatusMessage}"/>
/// </Border>
/// ]]></code>
///    <strong>Search results visibility:</strong>
///    <code>
/// public class SearchViewModel : INotifyPropertyChanged
/// {
///     private string _searchQuery;
///     public string SearchQuery
///     {
///         get => _searchQuery;
///         set
///         {
///             _searchQuery = value;
///             OnPropertyChanged();
///             PerformSearch();
///         }
///     }
///     
///     private ObservableCollection&lt;string&gt; _results = new();
///     public ObservableCollection&lt;string&gt; Results => _results;
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="HasQueryConverter"/>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="EmptyQueryConverter"
///                                                       NullValue="Visible"
///                                                       EmptyValue="Visible"
///                                                       WhiteSpaceValue="Collapsed"
///                                                       OtherValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <TextBox Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}" 
///              Watermark="Enter search query..."/>
///     
///     <!-- Search button: enabled when query has content -->
///     <Button Content="Search" 
///             Visibility="{Binding SearchQuery, Converter={StaticResource HasQueryConverter}}"/>
///     
///     <!-- Results list: visible when query has content -->
///     <ListBox ItemsSource="{Binding Results}"
///              Visibility="{Binding SearchQuery, Converter={StaticResource HasQueryConverter}}"/>
///     
///     <!-- Prompt message: visible when query is empty -->
///     <TextBlock Text="Enter a search query to begin" 
///                Foreground="Gray"
///                Visibility="{Binding SearchQuery, Converter={StaticResource EmptyQueryConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Form validation with specific states:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="ValidConverter"
///                                                       NullValue="Collapsed"
///                                                       EmptyValue="Collapsed"
///                                                       WhiteSpaceValue="Collapsed"
///                                                       OtherValue="Visible"/>
///     
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="EmptyOrNullConverter"
///                                                       NullValue="Visible"
///                                                       EmptyValue="Visible"
///                                                       WhiteSpaceValue="Collapsed"
///                                                       OtherValue="Collapsed"/>
///     
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="WhitespaceOnlyConverter"
///                                                       NullValue="Collapsed"
///                                                       EmptyValue="Collapsed"
///                                                       WhiteSpaceValue="Visible"
///                                                       OtherValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="Name:"/>
///     <TextBox x:Name="NameTextBox" Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"/>
///     
///     <!-- Valid indicator: green checkmark -->
///     <TextBlock Text="✓ Valid" 
///                Foreground="Green"
///                Visibility="{Binding Name, Converter={StaticResource ValidConverter}}"/>
///     
///     <!-- Empty/null error -->
///     <TextBlock Text="⚠ Name is required" 
///                Foreground="Red"
///                Visibility="{Binding Name, Converter={StaticResource EmptyOrNullConverter}}"/>
///     
///     <!-- Whitespace-only error -->
///     <TextBlock Text="⚠ Name cannot be only spaces" 
///                Foreground="Orange"
///                Visibility="{Binding Name, Converter={StaticResource WhitespaceOnlyConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Detail panel with status message:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToVisibilityConverter x:Key="StringVisConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.RowDefinitions>
///         <RowDefinition Height="*"/>
///         <RowDefinition Height="Auto"/>
///     </Grid.RowDefinitions>
///     
///     <!-- Main content -->
///     <ContentControl Grid.Row="0" Content="{Binding CurrentView}"/>
///     
///     <!-- Status bar: only visible when StatusMessage has content -->
///     <Border Grid.Row="1" 
///             Background="LightYellow" 
///             Padding="10,5"
///             Visibility="{Binding StatusMessage, Converter={StaticResource StringVisConverter}}">
///         <TextBlock Text="{Binding StatusMessage}"/>
///     </Border>
/// </Grid>
/// ]]></code>
/// </example>
public class StringNullOrWhiteSpaceToVisibilityConverter : GenericConverter<string, Visibility>
{
	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source string is <c>null</c>.
	/// </summary>
	/// <value>
	///    The visibility state for null values. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Collapsed"/> (doesn't occupy space),
	///    <see cref="Visibility.Visible"/> (inverted logic), or <see cref="Visibility.Hidden"/> (preserves layout space).
	/// </remarks>
	public Visibility NullValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source string contains only whitespace characters.
	/// </summary>
	/// <value>
	///    The visibility state for whitespace-only values. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This value is returned when the source string consists entirely of whitespace characters
	///       (spaces, tabs, newlines, etc.) as determined by <see cref="string.IsNullOrWhiteSpace"/>.
	///    </para>
	///    <para>
	///       Common configurations include <see cref="Visibility.Collapsed"/> for hiding invalid input,
	///       <see cref="Visibility.Visible"/> for showing specific validation messages, or
	///       <see cref="Visibility.Hidden"/> to preserve layout.
	///    </para>
	/// </remarks>
	public Visibility WhiteSpaceValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source string is empty (<c>""</c>).
	/// </summary>
	/// <value>
	///    The visibility state for empty strings. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This value is returned when the source is an empty string (zero length) but not null.
	///       The check for empty string is performed before the whitespace check.
	///    </para>
	///    <para>
	///       Common configurations include <see cref="Visibility.Collapsed"/> to hide elements,
	///       <see cref="Visibility.Visible"/> for inverted logic (showing "required" messages), or
	///       <see cref="Visibility.Hidden"/> to preserve space.
	///    </para>
	/// </remarks>
	public Visibility EmptyValue { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the source string contains actual content
	///    (non-whitespace characters).
	/// </summary>
	/// <value>
	///    The visibility state for valid strings with content. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This value is returned when the source string is not null, not empty, and contains at least
	///       one non-whitespace character.
	///    </para>
	///    <para>
	///       Common configurations include <see cref="Visibility.Visible"/> (default behavior),
	///       <see cref="Visibility.Collapsed"/> for inverted logic (hiding when content exists), or
	///       <see cref="Visibility.Hidden"/> for special layout scenarios.
	///    </para>
	/// </remarks>
	public Visibility OtherValue { get; set; } = Visibility.Visible;

	/// <summary>
	///    Converts a string to a <see cref="Visibility"/> value based on whether it is null, empty, whitespace-only,
	///    or contains actual content.
	/// </summary>
	/// <param name="source">The string to evaluate. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    One of the following <see cref="Visibility"/> values based on the source string state:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="NullValue"/> if <paramref name="source"/> is <c>null</c></description>
	///       </item>
	///       <item>
	///          <description><see cref="EmptyValue"/> if <paramref name="source"/> is <c>""</c> (empty string)</description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="WhiteSpaceValue"/> if <paramref name="source"/> contains only whitespace
	///             (as determined by <see cref="string.IsNullOrWhiteSpace"/>)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="OtherValue"/> if <paramref name="source"/> contains non-whitespace content
	///          </description>
	///       </item>
	///    </list>
	/// </returns>
	/// <remarks>
	///    <para>
	///       The conversion logic checks conditions in the following order:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>If source is <c>null</c>, return <see cref="NullValue"/></description>
	///       </item>
	///       <item>
	///          <description>If source is <c>""</c> (empty string), return <see cref="EmptyValue"/></description>
	///       </item>
	///       <item>
	///          <description>
	///             If source contains only whitespace (checked via <see cref="string.IsNullOrWhiteSpace"/>),
	///             return <see cref="WhiteSpaceValue"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>Otherwise, return <see cref="OtherValue"/></description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Note:</strong> The empty string check is performed before the whitespace check,
	///       so <see cref="EmptyValue"/> takes precedence over <see cref="WhiteSpaceValue"/> for <c>""</c>.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new StringNullOrWhiteSpaceToVisibilityConverter();  // Default values
	/// 
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));      // Output: Collapsed
	/// Console.WriteLine(converter.Convert("", CultureInfo.CurrentCulture));        // Output: Collapsed
	/// Console.WriteLine(converter.Convert("   ", CultureInfo.CurrentCulture));     // Output: Collapsed
	/// Console.WriteLine(converter.Convert("\t\n", CultureInfo.CurrentCulture));    // Output: Collapsed
	/// Console.WriteLine(converter.Convert("Hello", CultureInfo.CurrentCulture));   // Output: Visible
	/// Console.WriteLine(converter.Convert(" Hi ", CultureInfo.CurrentCulture));    // Output: Visible
	/// 
	/// // Inverted logic
	/// converter.OtherValue = Visibility.Collapsed;
	/// converter.NullValue = Visibility.Visible;
	/// converter.EmptyValue = Visibility.Visible;
	/// converter.WhiteSpaceValue = Visibility.Visible;
	/// 
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));      // Output: Visible
	/// Console.WriteLine(converter.Convert("Hello", CultureInfo.CurrentCulture));   // Output: Collapsed
	/// </code>
	/// </example>
	public override Visibility Convert(string source, CultureInfo culture)
	{
		if (source == null) return NullValue;
		if (source == "") return EmptyValue;
		return string.IsNullOrWhiteSpace(source) ? WhiteSpaceValue : OtherValue;
	}
}