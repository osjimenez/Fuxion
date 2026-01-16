using System;
using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts <see cref="Enum"/> values (including null) to nullable <see cref="NamedEnumValue"/>
///    instances for display in WPF UI controls.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide automatic conversion
///       between enum values and their nullable <see cref="NamedEnumValue"/> wrappers, with explicit support for
///       null enum references. It's similar to <see cref="EnumToNamedEnumValueConverter"/> but handles nullable
///       scenarios where the enum reference itself can be null.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Null enum support:</strong> Handles null enum references, returning null NamedEnumValue
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic name extraction:</strong> Converts non-null enums to NamedEnumValue with Display attribute support
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Supports both forward and backward conversion with null handling
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>WPF binding friendly:</strong> Works seamlessly with nullable enum properties in ViewModels
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type safety:</strong> Constructor configures nullable value type handling appropriately
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>How it works:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>
///             <see cref="Convert"/>: If enum is null, returns null; otherwise wraps it in <see cref="NamedEnumValue"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="ConvertBack"/>: If NamedEnumValue is null, returns null; otherwise extracts the enum
///          </description>
///       </item>
///       <item>
///          <description>
///             The <see cref="NamedEnumValue"/> automatically reads the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Important note:</strong> The constructor calls the base with <c>false</c> to configure nullable
///       value type handling, which affects how the base class treats nullable types during conversion.
///    </para>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Optional enum selections in ComboBox controls where "no selection" is valid</description>
///       </item>
///       <item>
///          <description>Nullable enum properties in ViewModels that need display names</description>
///       </item>
///       <item>
///          <description>Forms where enum fields are optional</description>
///       </item>
///       <item>
///          <description>Filter or search criteria with optional enum values</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Define an enum:</strong>
///    <code>
/// public enum Priority
/// {
///     [Display(Name = "Low Priority")]
///     Low,
///     
///     [Display(Name = "Medium Priority")]
///     Medium,
///     
///     [Display(Name = "High Priority")]
///     High
/// }
/// </code>
///    <strong>ViewModel with nullable enum property:</strong>
///    <code>
/// public class TaskViewModel : INotifyPropertyChanged
/// {
///     private Priority? _priority;
///     
///     // Nullable enum - can be null, Low, Medium, or High
///     public Priority? Priority
///     {
///         get => _priority;
///         set
///         {
///             _priority = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <strong>Basic usage in XAML with nullable ComboBox:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableEnumToNamedEnumValueConverter x:Key="NullableEnumConverter"/>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumToListConverter"/>
/// </Window.Resources>
/// 
/// <!-- ComboBox that allows no selection (null) -->
/// <ComboBox ItemsSource="{Binding Converter={StaticResource EnumToListConverter},
///                                 ConverterParameter={x:Type local:Priority}}"
///           SelectedItem="{Binding Priority, Converter={StaticResource NullableEnumConverter}}"
///           DisplayMemberPath="Name">
///     <!-- Empty selection represents null -->
/// </ComboBox>
/// ]]></code>
///    <strong>Optional filter with nullable enum:</strong>
///    <code>
/// public class FilterViewModel : INotifyPropertyChanged
/// {
///     // null = no filter, otherwise filter by priority
///     public Priority? PriorityFilter { get; set; }
///     
///     public IEnumerable&lt;Task&gt; FilteredTasks
///     {
///         get
///         {
///             var tasks = AllTasks;
///             if (PriorityFilter.HasValue)
///                 tasks = tasks.Where(t => t.Priority == PriorityFilter.Value);
///             return tasks;
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableEnumToNamedEnumValueConverter x:Key="NullableConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="Filter by Priority (optional):"/>
///     <ComboBox ItemsSource="{Binding PriorityOptions}"
///               SelectedItem="{Binding PriorityFilter, 
///                                     Converter={StaticResource NullableConverter}}"
///               DisplayMemberPath="Name">
///         <ComboBox.ItemContainerStyle>
///             <Style TargetType="ComboBoxItem">
///                 <Setter Property="Content" Value="{binding Name}"/>
///             </Style>
///         </ComboBox.ItemContainerStyle>
///     </ComboBox>
///     
///     <TextBlock Text="{Binding FilteredTasks.Count, StringFormat='Found {0} tasks'}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new NullableEnumToNamedEnumValueConverter();
/// 
/// // Non-null enum
/// Priority priority = Priority.High;
/// NamedEnumValue? result1 = converter.Convert(priority, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1?.Name);  // Output: "High Priority"
/// 
/// // Null enum
/// Priority? nullPriority = null;
/// NamedEnumValue? result2 = converter.Convert(nullPriority, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2?.Name ?? "null");  // Output: "null"
/// 
/// // Convert back
/// Priority backPriority = converter.ConvertBack(result1, CultureInfo.CurrentCulture);
/// Console.WriteLine(backPriority);  // Output: High
/// 
/// Priority? backNull = converter.ConvertBack(null, CultureInfo.CurrentCulture);
/// Console.WriteLine(backNull.HasValue);  // Output: false
/// </code>
///    <strong>Form with optional enum fields:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableEnumToNamedEnumValueConverter x:Key="NullableConverter"/>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumListConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.RowDefinitions>
///         <RowDefinition Height="Auto"/>
///         <RowDefinition Height="Auto"/>
///         <RowDefinition Height="Auto"/>
///     </Grid.RowDefinitions>
///     
///     <!-- Required field -->
///     <Label Grid.Row="0" Content="Status (required):"/>
///     <ComboBox Grid.Row="0" Grid.Column="1"
///               ItemsSource="{Binding Converter={StaticResource EnumListConverter},
///                                     ConverterParameter={x:Type local:Status}}"
///               SelectedItem="{Binding Status}"
///               DisplayMemberPath="Name"/>
///     
///     <!-- Optional field - allows null -->
///     <Label Grid.Row="1" Content="Priority (optional):"/>
///     <ComboBox Grid.Row="1" Grid.Column="1"
///               ItemsSource="{Binding Converter={StaticResource EnumListConverter},
///                                     ConverterParameter={x:Type local:Priority}}"
///               SelectedItem="{Binding Priority, 
///                                     Converter={StaticResource NullableConverter}}"
///               DisplayMemberPath="Name">
///         <!-- This allows deselection back to null -->
///     </ComboBox>
///     
///     <!-- Optional field - allows null -->
///     <Label Grid.Row="2" Content="Category (optional):"/>
///     <ComboBox Grid.Row="2" Grid.Column="1"
///               ItemsSource="{Binding Converter={StaticResource EnumListConverter},
///                                     ConverterParameter={x:Type local:Category}}"
///               SelectedItem="{Binding Category, 
///                                     Converter={StaticResource NullableConverter}}"
///               DisplayMemberPath="Name"/>
/// </Grid>
/// ]]></code>
///    <strong>Display nullable enum value as text:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableEnumToNamedEnumValueConverter x:Key="NullableConverter"/>
///     <data:NamedEnumValueToStringConverter x:Key="ToStringConverter"/>
/// </Window.Resources>
/// 
/// <TextBlock>
///     <Run Text="Priority: "/>
///     <Run Text="{Binding Priority, 
///                        Converter={StaticResource NullableConverter},
///                        Converter={StaticResource ToStringConverter},
///                        TargetNullValue='Not Set'}"
///          FontWeight="Bold"/>
/// </TextBlock>
/// ]]></code>
///    <strong>Search/filter criteria with nullable enums:</strong>
///    <code>
/// public class SearchCriteria : INotifyPropertyChanged
/// {
///     public Priority? MinPriority { get; set; }
///     public Priority? MaxPriority { get; set; }
///     public Status? StatusFilter { get; set; }
///     
///     public bool HasFilters => MinPriority.HasValue || 
///                               MaxPriority.HasValue || 
///                               StatusFilter.HasValue;
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:NullableEnumToNamedEnumValueConverter x:Key="NullableConverter"/>
/// </Window.Resources>
/// 
/// <GroupBox Header="Search Filters">
///     <StackPanel>
///         <Label Content="Minimum Priority:"/>
///         <ComboBox ItemsSource="{Binding PriorityOptions}"
///                   SelectedItem="{Binding MinPriority, 
///                                         Converter={StaticResource NullableConverter}}"
///                   DisplayMemberPath="Name"/>
///         
///         <Label Content="Status:"/>
///         <ComboBox ItemsSource="{Binding StatusOptions}"
///                   SelectedItem="{Binding StatusFilter, 
///                                         Converter={StaticResource NullableConverter}}"
///                   DisplayMemberPath="Name"/>
///         
///         <Button Content="Clear Filters" 
///                 Command="{Binding ClearFiltersCommand}"
///                 IsEnabled="{Binding HasFilters}"/>
///     </StackPanel>
/// </GroupBox>
/// ]]></code>
/// </example>
public class NullableEnumToNamedEnumValueConverter : GenericConverter<Enum, NamedEnumValue?>
{
	/// <summary>
	///    Initializes a new instance of the <see cref="NullableEnumToNamedEnumValueConverter"/> class.
	/// </summary>
	/// <remarks>
	///    The constructor calls the base class constructor with <c>false</c> to configure nullable value type
	///    handling. This ensures that nullable value types are not treated as nullable reference types during
	///    the conversion process.
	/// </remarks>
	public NullableEnumToNamedEnumValueConverter() : base(false) { }

	/// <summary>
	///    Converts an <see cref="Enum"/> value (which can be null) to a nullable <see cref="NamedEnumValue"/> instance.
	/// </summary>
	/// <param name="source">The enum value to convert. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A <see cref="NamedEnumValue"/> instance that wraps the source enum value and provides access to its
	///    display name, or <c>null</c> if <paramref name="source"/> is <c>null</c>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       When the source is not null, the returned <see cref="NamedEnumValue"/> automatically extracts
	///       the display name from the enum value's <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>
	///       if present. If no DisplayAttribute is found, it uses the enum value's name.
	///    </para>
	///    <para>
	///       When the source is null, this method returns <c>null</c> (<c>default</c>), allowing nullable enum
	///       scenarios in WPF bindings.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullableEnumToNamedEnumValueConverter();
	/// 
	/// Priority priority = Priority.High;
	/// NamedEnumValue? result1 = converter.Convert(priority, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1?.Name);  // Output: "High Priority"
	/// 
	/// Priority? nullPriority = null;
	/// NamedEnumValue? result2 = converter.Convert(nullPriority, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2 == null);  // Output: true
	/// </code>
	/// </example>
	public override NamedEnumValue? Convert(Enum source, CultureInfo culture) => source == null ? default : new NamedEnumValue(source);

	/// <summary>
	///    Converts a nullable <see cref="NamedEnumValue"/> back to its underlying <see cref="Enum"/> value.
	/// </summary>
	/// <param name="result">The <see cref="NamedEnumValue"/> to convert back. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    The original <see cref="Enum"/> value wrapped by the <see cref="NamedEnumValue"/>,
	///    or <c>null</c> if <paramref name="result"/> is <c>null</c>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method enables two-way data binding by converting <see cref="NamedEnumValue"/> instances
	///       back to enum values, with full support for null values in both directions.
	///    </para>
	///    <para>
	///       When <paramref name="result"/> is null, the method returns <c>null!</c> (null-forgiving operator),
	///       which allows the null to propagate through the nullable enum type system.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new NullableEnumToNamedEnumValueConverter();
	/// 
	/// Priority original = Priority.High;
	/// NamedEnumValue? named = converter.Convert(original, CultureInfo.CurrentCulture);
	/// Priority restored = converter.ConvertBack(named, CultureInfo.CurrentCulture);
	/// 
	/// Console.WriteLine(original == restored);  // Output: true
	/// 
	/// // Null round-trip
	/// NamedEnumValue? nullNamed = null;
	/// Enum? restoredNull = converter.ConvertBack(nullNamed, CultureInfo.CurrentCulture);
	/// Console.WriteLine(restoredNull == null);  // Output: true
	/// </code>
	///    <strong>Two-way binding with nullable enum:</strong>
	///    <code>
	/// // In ViewModel
	/// private Priority? _priority;
	/// public Priority? Priority
	/// {
	///     get => _priority;
	///     set
	///     {
	///         _priority = value;
	///         OnPropertyChanged();
	///         Console.WriteLine($"Priority changed to: {value?.ToString() ?? "null"}");
	///     }
	/// }
	/// 
	/// // When user selects "High Priority" from ComboBox:
	/// // 1. NamedEnumValue with Name="High Priority" is selected
	/// // 2. Converter.ConvertBack extracts Priority.High
	/// // 3. Priority property setter is called with Priority.High
	/// // 4. Console output: "Priority changed to: High"
	/// 
	/// // When user clears selection:
	/// // 1. null is selected
	/// // 2. Converter.ConvertBack returns null
	/// // 3. Priority property setter is called with null
	/// // 4. Console output: "Priority changed to: null"
	/// </code>
	/// </example>
	public override Enum ConvertBack(NamedEnumValue? result, CultureInfo culture) => result ?? null!;
}