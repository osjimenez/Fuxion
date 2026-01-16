using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts an enum <see cref="Type"/> to a list of <see cref="NamedEnumValue"/> instances
///    representing all values of that enum type.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult,TParameter}"/> to provide automatic
///       generation of enum value lists for use in WPF UI controls. It's particularly useful for dynamically
///       populating ComboBox, ListBox, or other item controls with all possible values of an enum type,
///       with optional alphabetical sorting by display name.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Dynamic enum value extraction:</strong> Automatically gets all values from any enum type
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Display name support:</strong> Each value is wrapped in <see cref="NamedEnumValue"/> with DisplayAttribute support
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Optional sorting:</strong> Can alphabetically sort values by their display names
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type-based conversion:</strong> Uses the enum type as converter parameter
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>XAML friendly:</strong> Works seamlessly with ObjectDataProvider and Binding ConverterParameter
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>How it works:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>Receives an enum <see cref="Type"/> as the converter parameter</description>
///       </item>
///       <item>
///          <description>Uses <see cref="Enum.GetValues"/> to extract all enum values</description>
///       </item>
///       <item>
///          <description>Wraps each value in a <see cref="NamedEnumValue"/> for display name support</description>
///       </item>
///       <item>
///          <description>Optionally sorts the list alphabetically by <see cref="NamedEnumValue.Name"/></description>
///       </item>
///       <item>
///          <description>Returns the list ready for data binding</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Populate ComboBox with all values of an enum type defined in XAML</description>
///       </item>
///       <item>
///          <description>Create reusable enum selection controls without code-behind</description>
///       </item>
///       <item>
///          <description>Display sorted enum options for better user experience</description>
///       </item>
///       <item>
///          <description>Build dynamic forms where enum types are determined at runtime</description>
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
///     High,
///     
///     [Display(Name = "Critical")]
///     Critical
/// }
/// </code>
///    <strong>Basic usage in XAML with ComboBox:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumToListConverter"/>
/// </Window.Resources>
/// 
/// <ComboBox ItemsSource="{Binding Converter={StaticResource EnumToListConverter}, 
///                                 ConverterParameter={x:Type local:Priority}}"
///           SelectedItem="{Binding SelectedPriority}"
///           DisplayMemberPath="Name"/>
/// ]]></code>
///    <strong>With alphabetical sorting:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="SortedEnumConverter" 
///                                                 IsAlphabeticallyOrdered="True"/>
/// </Window.Resources>
/// 
/// <!-- Values will be displayed alphabetically: Critical, High Priority, Low Priority, Medium Priority -->
/// <ComboBox ItemsSource="{Binding Converter={StaticResource SortedEnumConverter}, 
///                                 ConverterParameter={x:Type local:Priority}}"
///           DisplayMemberPath="Name"/>
/// ]]></code>
///    <strong>Using ObjectDataProvider for static binding:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumToListConverter"/>
///     
///     <ObjectDataProvider x:Key="PriorityList"
///                         MethodName="GetValues"
///                         ObjectType="{x:Type sys:Enum}">
///         <ObjectDataProvider.MethodParameters>
///             <x:Type TypeName="local:Priority"/>
///         </ObjectDataProvider.MethodParameters>
///     </ObjectDataProvider>
/// </Window.Resources>
/// 
/// <ComboBox ItemsSource="{Binding Source={StaticResource PriorityList}, 
///                                 Converter={StaticResource EnumToListConverter}}"
///           DisplayMemberPath="Name"/>
/// ]]></code>
///    <strong>Multiple enum ComboBoxes in a form:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="Priority:"/>
///     <ComboBox ItemsSource="{Binding Converter={StaticResource EnumConverter},
///                                     ConverterParameter={x:Type local:Priority}}"
///               SelectedItem="{Binding TaskPriority}"
///               DisplayMemberPath="Name"/>
///     
///     <Label Content="Status:"/>
///     <ComboBox ItemsSource="{Binding Converter={StaticResource EnumConverter},
///                                     ConverterParameter={x:Type local:TaskStatus}}"
///               SelectedItem="{Binding TaskStatus}"
///               DisplayMemberPath="Name"/>
///     
///     <Label Content="Category:"/>
///     <ComboBox ItemsSource="{Binding Converter={StaticResource EnumConverter},
///                                     ConverterParameter={x:Type local:Category}}"
///               SelectedItem="{Binding TaskCategory}"
///               DisplayMemberPath="Name"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new EnumTypeToNamedEnumValueListConverter
/// {
///     IsAlphabeticallyOrdered = true
/// };
/// 
/// // Convert using the enum type as parameter
/// List&lt;NamedEnumValue&gt; priorityList = converter.Convert(
///     null, 
///     typeof(Priority), 
///     CultureInfo.CurrentCulture);
/// 
/// // Display the list
/// foreach (var item in priorityList)
/// {
///     Console.WriteLine($"{item.Name} = {item.Value}");
/// }
/// // Output (alphabetically sorted):
/// // Critical = Critical
/// // High Priority = High
/// // Low Priority = Low
/// // Medium Priority = Medium
/// </code>
///    <strong>Creating a reusable UserControl for enum selection:</strong>
///    <code><![CDATA[
/// <!-- EnumComboBox.xaml -->
/// <UserControl x:Class="MyApp.Controls.EnumComboBox"
///              xmlns:data="clr-namespace:Fuxion.Windows.Data">
///     <UserControl.Resources>
///         <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumConverter"
///                                                     IsAlphabeticallyOrdered="True"/>
///     </UserControl.Resources>
///     
///     <ComboBox x:Name="ComboBox"
///               ItemsSource="{Binding Converter={StaticResource EnumConverter},
///                                     ConverterParameter={Binding EnumType, RelativeSource={RelativeSource AncestorType=UserControl}}}"
///               DisplayMemberPath="Name"/>
/// </UserControl>
/// ]]></code>
///    <code>
/// // EnumComboBox.xaml.cs
/// public partial class EnumComboBox : UserControl
/// {
///     public static readonly DependencyProperty EnumTypeProperty =
///         DependencyProperty.Register(
///             nameof(EnumType),
///             typeof(Type),
///             typeof(EnumComboBox));
///     
///     public Type EnumType
///     {
///         get => (Type)GetValue(EnumTypeProperty);
///         set => SetValue(EnumTypeProperty, value);
///     }
///     
///     public EnumComboBox()
///     {
///         InitializeComponent();
///     }
/// }
/// </code>
///    <strong>Using the reusable control:</strong>
///    <code><![CDATA[
/// <controls:EnumComboBox EnumType="{x:Type local:Priority}"
///                        SelectedItem="{Binding SelectedPriority}"/>
/// ]]></code>
///    <strong>Dynamic enum selection based on ViewModel property:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumTypeToNamedEnumValueListConverter x:Key="EnumConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- First ComboBox to select which enum to display -->
///     <ComboBox ItemsSource="{Binding AvailableEnumTypes}"
///               SelectedItem="{Binding SelectedEnumType}"/>
///     
///     <!-- Second ComboBox shows values of the selected enum type -->
///     <ComboBox ItemsSource="{Binding Converter={StaticResource EnumConverter},
///                                     ConverterParameter={Binding SelectedEnumType}}"
///               DisplayMemberPath="Name"/>
/// </StackPanel>
/// ]]></code>
/// </example>
public class EnumTypeToNamedEnumValueListConverter : GenericConverter<object, List<NamedEnumValue>, Type>
{
	/// <summary>
	///    Gets or sets a value indicating whether the resulting list should be sorted alphabetically by display name.
	/// </summary>
	/// <value>
	///    <c>true</c> to sort the enum values alphabetically by their <see cref="NamedEnumValue.Name"/> property;
	///    otherwise, <c>false</c> to maintain the declaration order. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       When set to <c>true</c>, the enum values are sorted alphabetically based on their display names
	///       (from <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> if present, otherwise
	///       the enum value name). This can improve user experience by presenting options in a predictable order.
	///    </para>
	///    <para>
	///       When <c>false</c> (default), the values are returned in their declaration order as defined in the enum type.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code><![CDATA[
	/// <!-- Unsorted (declaration order) -->
	/// <data:EnumTypeToNamedEnumValueListConverter x:Key="UnsortedConverter"
	///                                             IsAlphabeticallyOrdered="False"/>
	/// 
	/// <!-- Sorted alphabetically -->
	/// <data:EnumTypeToNamedEnumValueListConverter x:Key="SortedConverter"
	///                                             IsAlphabeticallyOrdered="True"/>
	/// ]]></code>
	/// </example>
	public bool IsAlphabeticallyOrdered { get; set; }

	/// <summary>
	///    Converts an enum <see cref="Type"/> to a list of <see cref="NamedEnumValue"/> instances.
	/// </summary>
	/// <param name="_">
	///    The source value (not used). This parameter is ignored as the conversion is based solely
	///    on the <paramref name="enumType"/> parameter.
	/// </param>
	/// <param name="enumType">
	///    The enum <see cref="Type"/> to extract values from. Must be a valid enum type.
	/// </param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult,TParameter}"/> interface.
	/// </param>
	/// <returns>
	///    A <see cref="List{NamedEnumValue}"/> containing all values from the specified enum type,
	///    each wrapped in a <see cref="NamedEnumValue"/> for display purposes. The list is sorted
	///    alphabetically if <see cref="IsAlphabeticallyOrdered"/> is <c>true</c>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method performs the following operations:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Calls <see cref="Enum.GetValues"/> to retrieve all enum values</description>
	///       </item>
	///       <item>
	///          <description>Casts values to <see cref="Enum"/> type</description>
	///       </item>
	///       <item>
	///          <description>Wraps each value in a <see cref="NamedEnumValue"/> instance</description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="IsAlphabeticallyOrdered"/> is true, sorts by <see cref="NamedEnumValue.Name"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>Returns the resulting list</description>
	///       </item>
	///    </list>
	///    <para>
	///       The <paramref name="_"/> parameter is not used because the conversion is entirely based on
	///       the enum type provided via <paramref name="enumType"/>. This design allows the converter
	///       to work with any binding source, as only the ConverterParameter matters.
	///    </para>
	/// </remarks>
	/// <exception cref="ArgumentException">
	///    Thrown by <see cref="Enum.GetValues"/> if <paramref name="enumType"/> is not a valid enum type.
	/// </exception>
	/// <example>
	///    <code>
	/// var converter = new EnumTypeToNamedEnumValueListConverter
	/// {
	///     IsAlphabeticallyOrdered = false
	/// };
	/// 
	/// // Get unsorted list
	/// var unsortedList = converter.Convert(null, typeof(Priority), CultureInfo.CurrentCulture);
	/// // Returns: [Low Priority, Medium Priority, High Priority, Critical]
	/// 
	/// converter.IsAlphabeticallyOrdered = true;
	/// var sortedList = converter.Convert(null, typeof(Priority), CultureInfo.CurrentCulture);
	/// // Returns: [Critical, High Priority, Low Priority, Medium Priority]
	/// </code>
	/// </example>
	public override List<NamedEnumValue> Convert(object _, Type enumType, CultureInfo culture)
	{
		var res = Enum.GetValues(enumType).Cast<Enum>().Select(e => new NamedEnumValue(e)).ToList();
		if (IsAlphabeticallyOrdered) res = res.OrderBy(e => e.Name).ToList();
		return res;
	}
}