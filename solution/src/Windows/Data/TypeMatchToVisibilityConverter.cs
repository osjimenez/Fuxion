using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fuxion.Windows.Data;

/// <summary>
///    Specifies predefined types to match against in <see cref="TypeMatchToVisibilityConverter"/>.
/// </summary>
public enum TypeMatchConverterDefaultType
{
	/// <summary>
	///    Match against <see cref="string"/> type.
	/// </summary>
	String,

	/// <summary>
	///    Match against <see cref="int"/> type.
	/// </summary>
	Int,

	/// <summary>
	///    Match against <see cref="long"/> type.
	/// </summary>
	Long,

	/// <summary>
	///    Match against <see cref="UIElement"/> type.
	/// </summary>
	UIElement
}

/// <summary>
///    A value converter that converts objects to <see cref="Visibility"/> values based on runtime type matching.
/// </summary>
/// <remarks>
///    <para>
///       This converter implements <see cref="IValueConverter"/> to provide type-based visibility control.
///       It checks if the input object is of a specific type and returns different visibility values
///       for matching and non-matching types. This is particularly useful in scenarios with polymorphic
///       data or when you need to show/hide UI elements based on the actual type of bound data.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Runtime type checking:</strong> Uses <c>is</c> operator for type matching at runtime
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Predefined types:</strong> Supports four common types: string, int, long, and UIElement
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable visibility:</strong> Set different visibility for match and non-match cases
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Only supports forward conversion (ConvertBack throws NotImplementedException)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Polymorphism support:</strong> Works with inheritance (e.g., Button matches UIElement)
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Show different templates or controls based on data type</description>
///       </item>
///       <item>
///          <description>Toggle UI elements visibility in heterogeneous collections</description>
///       </item>
///       <item>
///          <description>Display type-specific editors or viewers in dynamic content scenarios</description>
///       </item>
///       <item>
///          <description>Create adaptive UIs that respond to polymorphic data types</description>
///       </item>
///       <item>
///          <description>Filter or hide content based on runtime type information</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Show different content for string vs numeric types:</strong>
///    <code>
/// public class DataViewModel : INotifyPropertyChanged
/// {
///     private object _data;
///     
///     public object Data
///     {
///         get => _data;
///         set
///         {
///             _data = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TypeMatchToVisibilityConverter x:Key="StringMatchConverter"
///                                          TargetType="String"
///                                          Match="Visible"
///                                          NotMatch="Collapsed"/>
///     
///     <data:TypeMatchToVisibilityConverter x:Key="IntMatchConverter"
///                                          TargetType="Int"
///                                          Match="Visible"
///                                          NotMatch="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Show TextBox for strings -->
///     <TextBox Text="{Binding Data}"
///              Visibility="{Binding Data, Converter={StaticResource StringMatchConverter}}"/>
///     
///     <!-- Show NumericUpDown for integers -->
///     <TextBox Text="{Binding Data}"
///              Visibility="{Binding Data, Converter={StaticResource IntMatchConverter}}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Heterogeneous collection with type-based visibility:</strong>
///    <code>
/// public class MixedDataViewModel
/// {
///     public ObservableCollection&lt;object&gt; Items { get; } = new()
///     {
///         "Text item",
///         42,
///         "Another text",
///         100L,
///         "More text"
///     };
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TypeMatchToVisibilityConverter x:Key="StringConverter"
///                                          TargetType="String"/>
///     <data:TypeMatchToVisibilityConverter x:Key="IntConverter"
///                                          TargetType="Int"/>
///     <data:TypeMatchToVisibilityConverter x:Key="LongConverter"
///                                          TargetType="Long"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding Items}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <Grid>
///                 <!-- String template -->
///                 <Border Background="LightBlue" 
///                        Padding="5"
///                        Visibility="{Binding Converter={StaticResource StringConverter}}">
///                     <TextBlock Text="{Binding}" FontStyle="Italic"/>
///                 </Border>
///                 
///                 <!-- Int template -->
///                 <Border Background="LightGreen" 
///                        Padding="5"
///                        Visibility="{Binding Converter={StaticResource IntConverter}}">
///                     <TextBlock Text="{Binding, StringFormat='Integer: {0}'}" FontWeight="Bold"/>
///                 </Border>
///                 
///                 <!-- Long template -->
///                 <Border Background="LightYellow" 
///                        Padding="5"
///                        Visibility="{Binding Converter={StaticResource LongConverter}}">
///                     <TextBlock Text="{Binding, StringFormat='Long: {0}'}" FontFamily="Consolas"/>
///                 </Border>
///             </Grid>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var stringConverter = new TypeMatchToVisibilityConverter
/// {
///     TargetType = TypeMatchConverterDefaultType.String,
///     Match = Visibility.Visible,
///     NotMatch = Visibility.Collapsed
/// };
/// 
/// // String value
/// object value1 = "Hello";
/// Visibility result1 = (Visibility)stringConverter.Convert(value1, typeof(Visibility), null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Visible
/// 
/// // Int value
/// object value2 = 42;
/// Visibility result2 = (Visibility)stringConverter.Convert(value2, typeof(Visibility), null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Collapsed
/// 
/// // UIElement matching with inheritance
/// var uiElementConverter = new TypeMatchToVisibilityConverter
/// {
///     TargetType = TypeMatchConverterDefaultType.UIElement
/// };
/// 
/// object button = new Button();
/// Visibility result3 = (Visibility)uiElementConverter.Convert(button, typeof(Visibility), null, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Visible (Button is a UIElement)
/// </code>
///    <strong>Dynamic content editor based on type:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TypeMatchToVisibilityConverter x:Key="StringEditor"
///                                          TargetType="String"
///                                          Match="Visible"
///                                          NotMatch="Collapsed"/>
///     
///     <data:TypeMatchToVisibilityConverter x:Key="NumberEditor"
///                                          TargetType="Int"
///                                          Match="Visible"
///                                          NotMatch="Collapsed"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- String editor -->
///     <StackPanel Visibility="{Binding SelectedValue, Converter={StaticResource StringEditor}}">
///         <Label Content="Text Editor:"/>
///         <TextBox Text="{Binding SelectedValue, Mode=TwoWay}"
///                  AcceptsReturn="True"
///                  TextWrapping="Wrap"
///                  Height="100"/>
///     </StackPanel>
///     
///     <!-- Number editor -->
///     <StackPanel Visibility="{Binding SelectedValue, Converter={StaticResource NumberEditor}}">
///         <Label Content="Number Editor:"/>
///         <Slider Value="{Binding SelectedValue, Mode=TwoWay}"
///                 Minimum="0"
///                 Maximum="100"/>
///         <TextBlock Text="{Binding SelectedValue, StringFormat='Value: {0}'}"/>
///     </StackPanel>
/// </Grid>
/// ]]></code>
///    <strong>UIElement type matching for custom controls:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TypeMatchToVisibilityConverter x:Key="UIElementConverter"
///                                          TargetType="UIElement"
///                                          Match="Visible"
///                                          NotMatch="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Show controls for UIElement items -->
///     <Border Visibility="{Binding Content, Converter={StaticResource UIElementConverter}}">
///         <TextBlock Text="This is a UI element" Foreground="Green"/>
///     </Border>
///     
///     <!-- Alternative content for non-UIElement -->
///     <data:TypeMatchToVisibilityConverter x:Key="NonUIElementConverter"
///                                          TargetType="UIElement"
///                                          Match="Collapsed"
///                                          NotMatch="Visible"/>
///     
///     <Border Visibility="{Binding Content, Converter={StaticResource NonUIElementConverter}}">
///         <TextBlock Text="This is not a UI element" Foreground="Red"/>
///     </Border>
/// </StackPanel>
/// ]]></code>
///    <strong>Type-based filtering in data views:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TypeMatchToVisibilityConverter x:Key="ShowStrings"
///                                          TargetType="String"
///                                          Match="Visible"
///                                          NotMatch="Collapsed"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <CheckBox x:Name="ShowOnlyStrings" Content="Show only text items"/>
///     
///     <ListBox ItemsSource="{Binding MixedItems}">
///         <ListBox.ItemTemplate>
///             <DataTemplate>
///                 <TextBlock Text="{Binding}">
///                     <TextBlock.Visibility>
///                         <MultiBinding Converter="{StaticResource BooleanAndTypeConverter}">
///                             <Binding Path="IsChecked" ElementName="ShowOnlyStrings"/>
///                             <Binding Converter="{StaticResource ShowStrings}"/>
///                         </MultiBinding>
///                     </TextBlock.Visibility>
///                 </TextBlock>
///             </DataTemplate>
///         </ListBox.ItemTemplate>
///     </ListBox>
/// </StackPanel>
/// ]]></code>
///    <strong>Polymorphic data display:</strong>
///    <code>
/// public abstract class DataItem { }
/// public class TextData : DataItem 
/// { 
///     public string Text { get; set; } 
/// }
/// public class NumericData : DataItem 
/// { 
///     public int Value { get; set; } 
/// }
/// 
/// public class ViewModel
/// {
///     public ObservableCollection&lt;DataItem&gt; Items { get; } = new();
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Note: This converter works with concrete types, not base classes -->
///     <!-- For custom types, you might need a different approach -->
///     <data:TypeMatchToVisibilityConverter x:Key="StringTypeConverter"
///                                          TargetType="String"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding Items}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <ContentPresenter>
///                 <!-- Type-specific templates would need custom type matching -->
///                 <!-- This converter is limited to the predefined types -->
///             </ContentPresenter>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
/// </example>
public class TypeMatchToVisibilityConverter : IValueConverter
{
	/// <summary>
	///    Gets or sets the target type to match against.
	/// </summary>
	/// <value>
	///    A <see cref="TypeMatchConverterDefaultType"/> value specifying which type to check for.
	///    Default value is not set; you must configure this property.
	/// </value>
	public TypeMatchConverterDefaultType TargetType { get; set; }

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the type matches.
	/// </summary>
	/// <value>
	///    The visibility state for matching types. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	public Visibility Match { get; set; } = Visibility.Visible;

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the type does not match.
	/// </summary>
	/// <value>
	///    The visibility state for non-matching types. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	public Visibility NotMatch { get; set; } = Visibility.Collapsed;

	/// <summary>
	///    Converts an object to a <see cref="Visibility"/> value based on runtime type matching.
	/// </summary>
	/// <param name="value">The object to check the type of.</param>
	/// <param name="targetType">
	///    The type of the binding target property. This parameter is not used.
	/// </param>
	/// <param name="parameter">
	///    An optional parameter. This parameter is not used.
	/// </param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used.
	/// </param>
	/// <returns>
	///    <see cref="Match"/> if the runtime type of <paramref name="value"/> matches <see cref="TargetType"/>;
	///    otherwise, <see cref="NotMatch"/>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The type matching uses the <c>is</c> operator, which means:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             It checks the exact runtime type of the value
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             It supports inheritance (e.g., Button matches UIElement)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             It returns <see cref="NotMatch"/> for null values
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       Type matching behavior:
	///    </para>
	///    <list type="table">
	///       <listheader>
	///          <term>TargetType</term>
	///          <description>Matching Condition</description>
	///       </listheader>
	///       <item>
	///          <term><see cref="TypeMatchConverterDefaultType.String"/></term>
	///          <description>value is string</description>
	///       </item>
	///       <item>
	///          <term><see cref="TypeMatchConverterDefaultType.Int"/></term>
	///          <description>value is int</description>
	///       </item>
	///       <item>
	///          <term><see cref="TypeMatchConverterDefaultType.Long"/></term>
	///          <description>value is long</description>
	///       </item>
	///       <item>
	///          <term><see cref="TypeMatchConverterDefaultType.UIElement"/></term>
	///          <description>value is UIElement (includes all derived types)</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new TypeMatchToVisibilityConverter
	/// {
	///     TargetType = TypeMatchConverterDefaultType.String,
	///     Match = Visibility.Visible,
	///     NotMatch = Visibility.Collapsed
	/// };
	/// 
	/// var result1 = converter.Convert("text", typeof(Visibility), null, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result1);  // Visible
	/// 
	/// var result2 = converter.Convert(42, typeof(Visibility), null, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result2);  // Collapsed
	/// 
	/// var result3 = converter.Convert(null, typeof(Visibility), null, CultureInfo.CurrentCulture);
	/// Console.WriteLine(result3);  // Collapsed
	/// </code>
	/// </example>
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch (TargetType)
		{
			case TypeMatchConverterDefaultType.String:
				if (value is string) return Match;
				break;
			case TypeMatchConverterDefaultType.Int:
				if (value is int) return Match;
				break;
			case TypeMatchConverterDefaultType.Long:
				if (value is long) return Match;
				break;
			case TypeMatchConverterDefaultType.UIElement:
				if (value is UIElement) return Match;
				break;
		}
		return NotMatch;
	}

	/// <summary>
	///    Not implemented. This converter does not support backward conversion.
	/// </summary>
	/// <exception cref="NotImplementedException">Always thrown.</exception>
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}