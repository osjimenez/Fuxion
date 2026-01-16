using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts a <see cref="NamedEnumValue"/> to its string representation.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide simple
///       conversion from <see cref="NamedEnumValue"/> instances to their display string. It calls
///       the <see cref="NamedEnumValue.ToString"/> method, which returns the <see cref="NamedEnumValue.Name"/>
///       property value (the display name from <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>
///       or the enum value name if no attribute is present).
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Simple conversion:</strong> Converts NamedEnumValue directly to display string
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Display attribute aware:</strong> Uses DisplayAttribute.Name when available
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Only supports forward conversion (no ConvertBack)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Minimal overhead:</strong> Direct delegation to ToString method
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display enum names in TextBlocks or Labels</description>
///       </item>
///       <item>
///          <description>Show selected enum value as text</description>
///       </item>
///       <item>
///          <description>Format enum values for tooltips or status bars</description>
///       </item>
///       <item>
///          <description>Convert enum values to strings for logging or display</description>
///       </item>
///    </list>
///    <para>
///       <strong>Related converters:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <see cref="EnumToNamedEnumValueConverter"/>: Converts Enum to NamedEnumValue
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="EnumTypeToNamedEnumValueListConverter"/>: Gets all NamedEnumValue instances for an enum type
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Display selected enum value as text:</strong>
///    <code>
/// public enum Priority
/// {
///     [Display(Name = "Low Priority")]
///     Low,
///     
///     [Display(Name = "High Priority")]
///     High
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
///     <data:NamedEnumValueToStringConverter x:Key="NamedToStringConverter"/>
/// </Window.Resources>
/// 
/// <!-- Convert enum to NamedEnumValue, then to string -->
/// <TextBlock Text="{Binding SelectedPriority, 
///                          Converter={StaticResource EnumToNamedConverter},
///                          Converter={StaticResource NamedToStringConverter}}"/>
/// ]]></code>
///    <strong>Show selected item from ComboBox as text:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NamedEnumValueToStringConverter x:Key="NamedToStringConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- ComboBox with NamedEnumValue items -->
///     <ComboBox x:Name="PriorityCombo" 
///               ItemsSource="{Binding PriorityOptions}"
///               DisplayMemberPath="Name"/>
///     
///     <!-- Display selected item as text -->
///     <TextBlock Text="{Binding SelectedItem, 
///                              ElementName=PriorityCombo,
///                              Converter={StaticResource NamedToStringConverter}}"
///                FontWeight="Bold"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Tooltip with enum display name:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
///     <data:NamedEnumValueToStringConverter x:Key="NamedToStringConverter"/>
/// </Window.Resources>
/// 
/// <Button Content="Save">
///     <Button.ToolTip>
///         <StackPanel>
///             <TextBlock Text="Current Status:"/>
///             <TextBlock Text="{Binding Status, 
///                                      Converter={StaticResource EnumToNamedConverter},
///                                      Converter={StaticResource NamedToStringConverter}}"
///                        FontWeight="Bold"/>
///         </StackPanel>
///     </Button.ToolTip>
/// </Button>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new NamedEnumValueToStringConverter();
/// 
/// var priority = Priority.High;
/// var namedValue = new NamedEnumValue(priority);
/// 
/// string displayText = converter.Convert(namedValue, CultureInfo.CurrentCulture);
/// Console.WriteLine(displayText);  // Output: "High Priority"
/// </code>
///    <strong>Status bar display:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
///     <data:NamedEnumValueToStringConverter x:Key="NamedToStringConverter"/>
/// </Window.Resources>
/// 
/// <StatusBar>
///     <StatusBarItem>
///         <TextBlock>
///             <Run Text="Status: "/>
///             <Run Text="{Binding CurrentStatus, 
///                                Converter={StaticResource EnumToNamedConverter},
///                                Converter={StaticResource NamedToStringConverter}}"
///                  FontWeight="Bold"/>
///         </TextBlock>
///     </StatusBarItem>
/// </StatusBar>
/// ]]></code>
///    <strong>Combining with PipeConverter (if available):</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:PipeConverter x:Key="EnumToStringPipe">
///         <data:EnumToNamedEnumValueConverter/>
///         <data:NamedEnumValueToStringConverter/>
///     </data:PipeConverter>
/// </Window.Resources>
/// 
/// <!-- Single converter that does both conversions -->
/// <TextBlock Text="{Binding Status, Converter={StaticResource EnumToStringPipe}}"/>
/// ]]></code>
///    <strong>List of enum values as strings:</strong>
///    <code>
/// // ViewModel
/// public class ViewModel
/// {
///     public ObservableCollection&lt;NamedEnumValue&gt; StatusOptions { get; } = new();
///     
///     public ViewModel()
///     {
///         foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
///         {
///             StatusOptions.Add(new NamedEnumValue(status));
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- Display as text items -->
/// <Window.Resources>
///     <data:NamedEnumValueToStringConverter x:Key="ToStringConverter"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding StatusOptions}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <TextBlock Text="{Binding Converter={StaticResource ToStringConverter}}"/>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
///    <strong>Note on usage:</strong>
///    <para>
///       In most cases, you don't need this converter when using <see cref="NamedEnumValue"/> in XAML,
///       because WPF automatically calls <see cref="object.ToString"/> for text display. This converter
///       is useful when you need explicit converter syntax or when chaining with other converters.
///    </para>
/// </example>
public class NamedEnumValueToStringConverter : GenericConverter<NamedEnumValue, string>
{
	/// <summary>
	///    Converts a <see cref="NamedEnumValue"/> to its string representation.
	/// </summary>
	/// <param name="source">The <see cref="NamedEnumValue"/> to convert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    The display name of the enum value, obtained by calling <see cref="NamedEnumValue.ToString"/>.
	///    This returns the value from <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Name"/>
	///    if present, otherwise the enum value's name.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method simply delegates to <see cref="NamedEnumValue.ToString"/>, which returns
	///       the <see cref="NamedEnumValue.Name"/> property. The Name property contains:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             The value from <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Name"/>
	///             if the enum value has this attribute
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             The enum value's identifier name if no DisplayAttribute is present
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       The <paramref name="culture"/> parameter is not used because <see cref="NamedEnumValue.ToString"/>
	///       doesn't perform culture-specific formatting.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NamedEnumValueToStringConverter();
	/// 
	/// // With DisplayAttribute
	/// var priority = Priority.High;  // [Display(Name = "High Priority")]
	/// var namedValue = new NamedEnumValue(priority);
	/// string result = converter.Convert(namedValue, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result);  // Output: "High Priority"
	/// 
	/// // Without DisplayAttribute
	/// var status = Status.InProgress;  // No DisplayAttribute
	/// var namedStatus = new NamedEnumValue(status);
	/// string result2 = converter.Convert(namedStatus, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Output: "InProgress"
	/// </code>
	/// </example>
	public override string Convert(NamedEnumValue source, CultureInfo culture) => source.ToString();
}