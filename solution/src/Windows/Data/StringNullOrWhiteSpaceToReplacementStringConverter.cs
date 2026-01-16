using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that replaces null, empty, or whitespace strings with configurable replacement strings.
/// </summary>
/// <remarks>
///    <para>
///       This sealed converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide string
///       normalization by replacing null references, empty strings, and whitespace-only strings with
///       meaningful replacement values. It's particularly useful for displaying user-friendly text instead
///       of blank spaces or null values in UI elements.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Three-state detection:</strong> Distinguishes between null, empty (""), and whitespace-only strings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable replacements:</strong> Separate replacement strings for each state
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>String-to-string conversion:</strong> Input and output are both strings, no type conversion needed
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Only supports forward conversion (no ConvertBack implementation)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Preserves valid strings:</strong> Non-whitespace strings pass through unchanged
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Conversion logic:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Input State</term>
///          <description>Returned Value</description>
///       </listheader>
///       <item>
///          <term>null</term>
///          <description><see cref="NullValue"/> (default: "null")</description>
///       </item>
///       <item>
///          <term>"" (empty string)</term>
///          <description><see cref="EmptyValue"/> (default: "empty")</description>
///       </item>
///       <item>
///          <term>Whitespace only (e.g., "   ", "\t", "\n")</term>
///          <description><see cref="WhiteSpaceValue"/> (default: "empty")</description>
///       </item>
///       <item>
///          <term>Valid string (non-whitespace content)</term>
///          <description>Original string unchanged</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display placeholders for null or empty user inputs</description>
///       </item>
///       <item>
///          <description>Show meaningful text instead of blank spaces in data grids or lists</description>
///       </item>
///       <item>
///          <description>Normalize optional string fields for display purposes</description>
///       </item>
///       <item>
///          <description>Debugging or logging where empty values need to be identified</description>
///       </item>
///       <item>
///          <description>Data validation feedback showing why a field is invalid</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Display placeholder for empty names:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToReplacementStringConverter x:Key="StringReplacementConverter"
///                                                              NullValue="(not set)"
///                                                              EmptyValue="(empty)"
///                                                              WhiteSpaceValue="(whitespace)"/>
/// </Window.Resources>
/// 
/// <!-- Shows replacement text when Name is null, empty, or whitespace -->
/// <TextBlock Text="{Binding Name, Converter={StaticResource StringReplacementConverter}}"/>
/// ]]></code>
///    <strong>Data grid with null/empty indicators:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToReplacementStringConverter x:Key="CellConverter"
///                                                              NullValue="[NULL]"
///                                                              EmptyValue="[EMPTY]"
///                                                              WhiteSpaceValue="[SPACES]"/>
/// </Window.Resources>
/// 
/// <DataGrid ItemsSource="{Binding Users}">
///     <DataGrid.Columns>
///         <DataGridTextColumn Header="Name" 
///                            Binding="{Binding Name, 
///                                             Converter={StaticResource CellConverter}}"/>
///         <DataGridTextColumn Header="Email" 
///                            Binding="{Binding Email, 
///                                             Converter={StaticResource CellConverter}}"/>
///     </DataGrid.Columns>
/// </DataGrid>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new StringNullOrWhiteSpaceToReplacementStringConverter
/// {
///     NullValue = "(not set)",
///     EmptyValue = "(empty)",
///     WhiteSpaceValue = "(whitespace)"
/// };
/// 
/// string result1 = converter.Convert(null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: "(not set)"
/// 
/// string result2 = converter.Convert("", CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: "(empty)"
/// 
/// string result3 = converter.Convert("   ", CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: "(whitespace)"
/// 
/// string result4 = converter.Convert("\t\n  ", CultureInfo.CurrentCulture);
/// Console.WriteLine(result4);  // Output: "(whitespace)"
/// 
/// string result5 = converter.Convert("John Doe", CultureInfo.CurrentCulture);
/// Console.WriteLine(result5);  // Output: "John Doe"
/// </code>
///    <strong>Form validation display:</strong>
///    <code>
/// public class UserViewModel : INotifyPropertyChanged
/// {
///     private string _firstName;
///     public string FirstName
///     {
///         get => _firstName;
///         set
///         {
///             _firstName = value;
///             OnPropertyChanged();
///         }
///     }
///     
///     private string _lastName;
///     public string LastName
///     {
///         get => _lastName;
///         set
///         {
///             _lastName = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToReplacementStringConverter x:Key="ValidationConverter"
///                                                              NullValue="⚠ Required"
///                                                              EmptyValue="⚠ Required"
///                                                              WhiteSpaceValue="⚠ Cannot be spaces"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="First Name:"/>
///     <TextBox Text="{Binding FirstName, UpdateSourceTrigger=PropertyChanged}"/>
///     <TextBlock Text="{Binding FirstName, 
///                              Converter={StaticResource ValidationConverter}}"
///                Foreground="Red"/>
///     
///     <Label Content="Last Name:"/>
///     <TextBox Text="{Binding LastName, UpdateSourceTrigger=PropertyChanged}"/>
///     <TextBlock Text="{Binding LastName, 
///                              Converter={StaticResource ValidationConverter}}"
///                Foreground="Red"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Debug view with different replacements:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToReplacementStringConverter x:Key="DebugConverter"
///                                                              NullValue="&lt;null&gt;"
///                                                              EmptyValue="&lt;empty&gt;"
///                                                              WhiteSpaceValue="&lt;whitespace&gt;"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding LogEntries}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <StackPanel Orientation="Horizontal">
///                 <TextBlock Text="{Binding Timestamp, StringFormat='[{0:HH:mm:ss}]'}" 
///                           Margin="0,0,10,0"/>
///                 <TextBlock Text="{Binding Message, 
///                                          Converter={StaticResource DebugConverter}}"/>
///             </StackPanel>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
///    <strong>Optional fields with custom placeholders:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToReplacementStringConverter x:Key="OptionalConverter"
///                                                              NullValue="(Optional)"
///                                                              EmptyValue="(Optional)"
///                                                              WhiteSpaceValue="(Invalid)"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.ColumnDefinitions>
///         <ColumnDefinition Width="Auto"/>
///         <ColumnDefinition Width="*"/>
///     </Grid.ColumnDefinitions>
///     <Grid.RowDefinitions>
///         <RowDefinition Height="Auto"/>
///         <RowDefinition Height="Auto"/>
///         <RowDefinition Height="Auto"/>
///     </Grid.RowDefinitions>
///     
///     <!-- Required field -->
///     <Label Grid.Row="0" Grid.Column="0" Content="Name:"/>
///     <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding Name}"/>
///     
///     <!-- Optional field with placeholder -->
///     <Label Grid.Row="1" Grid.Column="0" Content="Middle Name:"/>
///     <TextBlock Grid.Row="1" Grid.Column="1" 
///               Text="{Binding MiddleName, 
///                             Converter={StaticResource OptionalConverter}}"
///               Foreground="Gray"/>
///     
///     <!-- Optional field with placeholder -->
///     <Label Grid.Row="2" Grid.Column="0" Content="Nickname:"/>
///     <TextBlock Grid.Row="2" Grid.Column="1" 
///               Text="{Binding Nickname, 
///                             Converter={StaticResource OptionalConverter}}"
///               Foreground="Gray"/>
/// </Grid>
/// ]]></code>
///    <strong>Combining with other converters:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringNullOrWhiteSpaceToReplacementStringConverter x:Key="StringReplacement"
///                                                              NullValue="(none)"
///                                                              EmptyValue="(none)"
///                                                              WhiteSpaceValue="(invalid)"/>
///     <data:StringToUpperConverter x:Key="ToUpper"/>
///     <data:PipeConverter x:Key="ReplaceThenUpper">
///         <data:StringNullOrWhiteSpaceToReplacementStringConverter NullValue="(none)"
///                                                                  EmptyValue="(none)"
///                                                                  WhiteSpaceValue="(invalid)"/>
///         <data:StringToUpperConverter/>
///     </data:PipeConverter>
/// </Window.Resources>
/// 
/// <!-- Replaces empty values then converts to uppercase -->
/// <TextBlock Text="{Binding Category, Converter={StaticResource ReplaceThenUpper}}"/>
/// ]]></code>
/// </example>
public class StringNullOrWhiteSpaceToReplacementStringConverter : GenericConverter<string, string>
{
	/// <summary>
	///    Gets or sets the string to return when the source string is <c>null</c>.
	/// </summary>
	/// <value>
	///    The replacement string for null values. Default is <c>"null"</c>.
	/// </value>
	/// <remarks>
	///    This value is returned when the source is a null reference.
	///    Common configurations include "(not set)", "(none)", "[NULL]", or "&lt;null&gt;".
	/// </remarks>
	public string NullValue { get; set; } = "null";

	/// <summary>
	///    Gets or sets the string to return when the source string contains only whitespace characters.
	/// </summary>
	/// <value>
	///    The replacement string for whitespace-only values. Default is <c>"empty"</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This value is returned when the source string consists entirely of whitespace characters
	///       (spaces, tabs, newlines, etc.) as determined by <see cref="string.IsNullOrWhiteSpace"/>.
	///    </para>
	///    <para>
	///       Common configurations include "(whitespace)", "(spaces)", "[SPACES]", or "&lt;whitespace&gt;".
	///    </para>
	/// </remarks>
	public string WhiteSpaceValue { get; set; } = "empty";

	/// <summary>
	///    Gets or sets the string to return when the source string is empty (<c>""</c>).
	/// </summary>
	/// <value>
	///    The replacement string for empty strings. Default is <c>"empty"</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This value is returned when the source is an empty string (zero length) but not null.
	///       The check for empty string is performed before the whitespace check.
	///    </para>
	///    <para>
	///       Common configurations include "(empty)", "(none)", "[EMPTY]", or "&lt;empty&gt;".
	///    </para>
	/// </remarks>
	public string EmptyValue { get; set; } = "empty";

	/// <summary>
	///    Converts a string to a replacement string if it is null, empty, or whitespace-only.
	/// </summary>
	/// <param name="source">The string to evaluate. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    One of the following values based on the source string state:
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
	///          <description>The original <paramref name="source"/> string if it contains non-whitespace content</description>
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
	///          <description>Otherwise, return the original source string unchanged</description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Note:</strong> The empty string check is performed before the whitespace check,
	///       so <see cref="EmptyValue"/> takes precedence over <see cref="WhiteSpaceValue"/> for <c>""</c>.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new StringNullOrWhiteSpaceToReplacementStringConverter
	/// {
	///     NullValue = "(not set)",
	///     EmptyValue = "(empty)",
	///     WhiteSpaceValue = "(whitespace)"
	/// };
	/// 
	/// Console.WriteLine(converter.Convert(null, CultureInfo.CurrentCulture));      // Output: "(not set)"
	/// Console.WriteLine(converter.Convert("", CultureInfo.CurrentCulture));        // Output: "(empty)"
	/// Console.WriteLine(converter.Convert("   ", CultureInfo.CurrentCulture));     // Output: "(whitespace)"
	/// Console.WriteLine(converter.Convert("\t\n", CultureInfo.CurrentCulture));    // Output: "(whitespace)"
	/// Console.WriteLine(converter.Convert("Hello", CultureInfo.CurrentCulture));   // Output: "Hello"
	/// Console.WriteLine(converter.Convert(" Hi ", CultureInfo.CurrentCulture));    // Output: " Hi "
	/// </code>
	/// </example>
	public override string Convert(string source, CultureInfo culture)
	{
		if (source == null) return NullValue;
		if (source == "") return EmptyValue;
		return string.IsNullOrWhiteSpace(source) ? WhiteSpaceValue : source;
	}
}