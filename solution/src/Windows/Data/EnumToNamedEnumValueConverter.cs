using System;
using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts <see cref="Enum" /> values to <see cref="NamedEnumValue" /> instances
///    for display in WPF UI controls with user-friendly names.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}" /> to provide automatic conversion
///       between raw enum values and their display-friendly <see cref="NamedEnumValue" /> wrappers. It's particularly
///       useful for data binding scenarios where enum values need to be displayed with human-readable names
///       derived from <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" />.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic name extraction:</strong> Converts enums to NamedEnumValue with Display attribute support
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Supports both forward and backward conversion
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>WPF binding friendly:</strong> Seamlessly integrates with ComboBox, ListBox, and other item
///             controls
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Display attribute aware:</strong> Automatically uses DisplayAttribute.Name when available
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>No configuration needed:</strong> Works out-of-the-box with any enum type
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>How it works:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>
///             <see cref="Convert" />: Takes an <see cref="Enum" /> value and wraps it in a <see cref="NamedEnumValue" />
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="ConvertBack" />: Extracts the original <see cref="Enum" /> value from a
///             <see cref="NamedEnumValue" />
///          </description>
///       </item>
///       <item>
///          <description>
///             The <see cref="NamedEnumValue" /> automatically reads the
///             <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" />
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display enum values with localized or user-friendly names in ComboBox controls</description>
///       </item>
///       <item>
///          <description>Show enum options in ListBox with custom display text</description>
///       </item>
///       <item>
///          <description>Bind enum properties to UI controls with automatic name resolution</description>
///       </item>
///       <item>
///          <description>Create dropdown lists for enum-based selections with readable labels</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Define an enum with display attributes:</strong>
///    <code>
/// public enum OrderStatus
/// {
///     [Display(Name = "Pending Approval")]
///     PendingApproval,
///     
///     [Display(Name = "Approved")]
///     Approved,
///     
///     [Display(Name = "In Progress")]
///     InProgress,
///     
///     [Display(Name = "Completed")]
///     Completed,
///     
///     [Display(Name = "Cancelled")]
///     Cancelled
/// }
/// </code>
///    <strong>Basic usage in XAML with ComboBox:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
///     
///     <!-- Create collection of enum values in code-behind or ViewModel -->
/// </Window.Resources>
/// 
/// <!-- Bind to enum property with converter -->
/// <ComboBox ItemsSource="{Binding StatusOptions}" 
///           SelectedItem="{Binding SelectedStatus, Converter={StaticResource EnumToNamedConverter}}"
///           DisplayMemberPath="Name"/>
/// ]]></code>
///    <strong>ViewModel setup for ComboBox binding:</strong>
///    <code>
/// public class OrderViewModel : INotifyPropertyChanged
/// {
///     public OrderViewModel()
///     {
///         // Populate enum options as NamedEnumValue collection
///         StatusOptions = Enum.GetValues&lt;OrderStatus&gt;()
///             .Select(s => new NamedEnumValue(s))
///             .ToList();
///     }
///     
///     public IEnumerable&lt;NamedEnumValue&gt; StatusOptions { get; }
///     
///     private OrderStatus _selectedStatus;
///     public OrderStatus SelectedStatus
///     {
///         get => _selectedStatus;
///         set
///         {
///             _selectedStatus = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <strong>Alternative: Direct enum binding with converter:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
///     <ObjectDataProvider x:Key="StatusValues" 
///                         MethodName="GetValues" 
///                         ObjectType="{x:Type sys:Enum}">
///         <ObjectDataProvider.MethodParameters>
///             <x:Type TypeName="local:OrderStatus"/>
///         </ObjectDataProvider.MethodParameters>
///     </ObjectDataProvider>
/// </Window.Resources>
/// 
/// <ComboBox ItemsSource="{Binding Source={StaticResource StatusValues}}" 
///           SelectedItem="{Binding Status}">
///     <ComboBox.ItemTemplate>
///         <DataTemplate>
///             <TextBlock Text="{Binding Converter={StaticResource EnumToNamedConverter}}"/>
///         </DataTemplate>
///     </ComboBox.ItemTemplate>
/// </ComboBox>
/// ]]></code>
///    <strong>Usage with ListBox:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding PriorityOptions}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <StackPanel Orientation="Horizontal">
///                 <TextBlock Text="{Binding Converter={StaticResource EnumToNamedConverter}}" 
///                            FontWeight="Bold"/>
///                 <TextBlock Text=" - " Margin="5,0"/>
///                 <TextBlock Text="{Binding Description}"/>
///             </StackPanel>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new EnumToNamedEnumValueConverter();
/// 
/// OrderStatus status = OrderStatus.PendingApproval;
/// NamedEnumValue namedValue = converter.Convert(status, CultureInfo.CurrentCulture);
/// 
/// Console.WriteLine(namedValue.Name);  // Output: "Pending Approval"
/// Console.WriteLine(namedValue.Value); // Output: PendingApproval
/// 
/// // Convert back
/// OrderStatus backStatus = converter.ConvertBack(namedValue, CultureInfo.CurrentCulture);
/// Console.WriteLine(backStatus);       // Output: PendingApproval
/// </code>
///    <strong>Two-way binding scenario:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
/// </Window.Resources>
/// 
/// <!-- ComboBox selection updates the enum property and vice versa -->
/// <ComboBox ItemsSource="{Binding StatusOptions}" 
///           SelectedItem="{Binding CurrentStatus, 
///                                  Converter={StaticResource EnumToNamedConverter}, 
///                                  Mode=TwoWay}"
///           DisplayMemberPath="Name">
///     <ComboBox.ItemContainerStyle>
///         <Style TargetType="ComboBoxItem">
///             <Setter Property="ToolTip" Value="{Binding Name}"/>
///         </Style>
///     </ComboBox.ItemContainerStyle>
/// </ComboBox>
/// ]]></code>
///    <strong>Creating a reusable enum ComboBox control:</strong>
///    <code>
/// public static class EnumComboBoxHelper
/// {
///     public static IEnumerable&lt;NamedEnumValue&gt; GetEnumValues(Type enumType)
///     {
///         if (!enumType.IsEnum)
///             throw new ArgumentException("Type must be an enum", nameof(enumType));
///             
///         return Enum.GetValues(enumType)
///             .Cast&lt;Enum&gt;()
///             .Select(e => new NamedEnumValue(e))
///             .ToList();
///     }
/// }
/// 
/// // Usage in ViewModel
/// public IEnumerable&lt;NamedEnumValue&gt; StatusOptions =&gt; 
///     EnumComboBoxHelper.GetEnumValues(typeof(OrderStatus));
/// </code>
///    <strong>Multiple enum properties in a form:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:EnumToNamedEnumValueConverter x:Key="EnumToNamedConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="Status:"/>
///     <ComboBox ItemsSource="{Binding StatusOptions}" 
///               SelectedItem="{Binding Status, Converter={StaticResource EnumToNamedConverter}}"
///               DisplayMemberPath="Name"/>
///     
///     <Label Content="Priority:"/>
///     <ComboBox ItemsSource="{Binding PriorityOptions}" 
///               SelectedItem="{Binding Priority, Converter={StaticResource EnumToNamedConverter}}"
///               DisplayMemberPath="Name"/>
///     
///     <Label Content="Category:"/>
///     <ComboBox ItemsSource="{Binding CategoryOptions}" 
///               SelectedItem="{Binding Category, Converter={StaticResource EnumToNamedEnumValueConverter}}"
///               DisplayMemberPath="Name"/>
/// </StackPanel>
/// ]]></code>
/// </example>
public class EnumToNamedEnumValueConverter : GenericConverter<Enum, NamedEnumValue>
{
	/// <summary>
	///    Converts an <see cref="Enum" /> value to a <see cref="NamedEnumValue" /> instance.
	/// </summary>
	/// <param name="source">The enum value to convert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}" /> interface.
	/// </param>
	/// <returns>
	///    A new <see cref="NamedEnumValue" /> instance that wraps the source enum value
	///    and provides access to its display name.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The returned <see cref="NamedEnumValue" /> automatically extracts the display name
	///       from the enum value's <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" />
	///       if present. If no <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" /> is found,
	///       it uses the enum value's name (e.g., "PendingApproval").
	///    </para>
	///    <para>
	///       This conversion is lightweight and can be used frequently without performance concerns.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new EnumToNamedEnumValueConverter();
	/// 
	/// OrderStatus status = OrderStatus.PendingApproval;
	/// NamedEnumValue result = converter.Convert(status, CultureInfo.CurrentCulture);
	/// 
	/// Console.WriteLine(result.Name);   // Output: "Pending Approval" (from DisplayAttribute)
	/// Console.WriteLine(result.Value);  // Output: PendingApproval (original enum)
	/// Console.WriteLine(result);        // Output: "Pending Approval" (ToString returns Name)
	/// </code>
	/// </example>
	public override NamedEnumValue Convert(Enum source, CultureInfo culture)
	{
		return new(source);
	}

	/// <summary>
	///    Converts a <see cref="NamedEnumValue" /> back to its underlying <see cref="Enum" /> value.
	/// </summary>
	/// <param name="result">The <see cref="NamedEnumValue" /> to convert back.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}" /> interface.
	/// </param>
	/// <returns>
	///    The original <see cref="Enum" /> value wrapped by the <see cref="NamedEnumValue" />.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method simply extracts and returns the <see cref="NamedEnumValue.Value" /> property,
	///       which contains the original enum value. This enables two-way data binding scenarios
	///       where the UI selection needs to update the underlying enum property.
	///    </para>
	///    <para>
	///       The conversion is lossless - you can convert an enum to <see cref="NamedEnumValue" />
	///       and back to the original enum without losing any information.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new EnumToNamedEnumValueConverter();
	/// 
	/// OrderStatus original = OrderStatus.Approved;
	/// NamedEnumValue named = converter.Convert(original, CultureInfo.CurrentCulture);
	/// OrderStatus restored = converter.ConvertBack(named, CultureInfo.CurrentCulture);
	/// 
	/// Console.WriteLine(original == restored);  // Output: true
	/// </code>
	///    <strong>Two-way binding demonstration:</strong>
	///    <code>
	/// // In ViewModel
	/// private OrderStatus _status = OrderStatus.PendingApproval;
	/// public OrderStatus Status
	/// {
	///     get => _status;
	///     set
	///     {
	///         _status = value;
	///         OnPropertyChanged();
	///         Console.WriteLine($"Status changed to: {value}");
	///     }
	/// }
	/// 
	/// // When user selects "Approved" from ComboBox:
	/// // 1. NamedEnumValue with Name="Approved" is selected
	/// // 2. Converter.ConvertBack extracts OrderStatus.Approved
	/// // 3. Status property setter is called with OrderStatus.Approved
	/// // 4. Console output: "Status changed to: Approved"
	/// </code>
	/// </example>
	public override Enum ConvertBack(NamedEnumValue result, CultureInfo culture)
	{
		return result.Value;
	}
}