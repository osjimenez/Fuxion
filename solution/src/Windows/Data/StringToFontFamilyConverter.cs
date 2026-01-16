using System.Globalization;
using System.Windows.Media;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts font family name strings to <see cref="FontFamily"/> objects and vice versa.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide bidirectional conversion
///       between string representations of font family names and WPF <see cref="FontFamily"/> objects. It's particularly
///       useful when you need to dynamically set font families from data bindings, configuration files, or user input.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Supports both <see cref="Convert"/> and <see cref="ConvertBack"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Simple string-to-FontFamily:</strong> Creates FontFamily instances from font name strings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>FontFamily-to-string:</strong> Extracts font family name back to string representation
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Two-way binding support:</strong> Works seamlessly with bidirectional WPF bindings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>System font support:</strong> Handles standard system fonts and custom font families
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Bind font families from ViewModel string properties</description>
///       </item>
///       <item>
///          <description>Allow users to select fonts from ComboBox with font names</description>
///       </item>
///       <item>
///          <description>Load font settings from configuration or database</description>
///       </item>
///       <item>
///          <description>Create font pickers or customization dialogs</description>
///       </item>
///       <item>
///          <description>Save font preferences as string values</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Bind font family from ViewModel string property:</strong>
///    <code>
/// public class TextSettingsViewModel : INotifyPropertyChanged
/// {
///     private string _fontFamily = "Arial";
///     
///     public string FontFamily
///     {
///         get => _fontFamily;
///         set
///         {
///             _fontFamily = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontFamilyConverter"/>
/// </Window.Resources>
/// 
/// <TextBlock Text="Sample Text"
///            FontFamily="{Binding FontFamily, Converter={StaticResource FontFamilyConverter}}"
///            FontSize="16"/>
/// ]]></code>
///    <strong>Font picker with ComboBox:</strong>
///    <code>
/// public class EditorViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;string&gt; AvailableFonts { get; } = new()
///     {
///         "Arial",
///         "Calibri",
///         "Consolas",
///         "Courier New",
///         "Georgia",
///         "Segoe UI",
///         "Times New Roman",
///         "Verdana"
///     };
///     
///     private string _selectedFont = "Segoe UI";
///     public string SelectedFont
///     {
///         get => _selectedFont;
///         set
///         {
///             _selectedFont = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontConverter"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="Select Font:"/>
///     <ComboBox ItemsSource="{Binding AvailableFonts}"
///               SelectedItem="{Binding SelectedFont}">
///         <ComboBox.ItemTemplate>
///             <DataTemplate>
///                 <TextBlock Text="{Binding}" 
///                           FontFamily="{Binding Converter={StaticResource FontConverter}}"/>
///             </DataTemplate>
///         </ComboBox.ItemTemplate>
///     </ComboBox>
///     
///     <!-- Preview -->
///     <TextBlock Text="The quick brown fox jumps over the lazy dog" 
///                FontFamily="{Binding SelectedFont, Converter={StaticResource FontConverter}}"
///                FontSize="16"
///                Margin="0,10,0,0"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new StringToFontFamilyConverter();
/// 
/// // Convert string to FontFamily
/// FontFamily arial = converter.Convert("Arial", CultureInfo.CurrentCulture);
/// Console.WriteLine(arial.Source);  // Output: "Arial"
/// 
/// FontFamily consolas = converter.Convert("Consolas", CultureInfo.CurrentCulture);
/// Console.WriteLine(consolas.Source);  // Output: "Consolas"
/// 
/// // Convert FontFamily back to string
/// string fontName = converter.ConvertBack(arial, CultureInfo.CurrentCulture);
/// Console.WriteLine(fontName);  // Output: "Arial"
/// 
/// // With custom or system fonts
/// FontFamily segoeUI = converter.Convert("Segoe UI", CultureInfo.CurrentCulture);
/// Console.WriteLine(segoeUI.Source);  // Output: "Segoe UI"
/// </code>
///    <strong>Two-way binding for font customization:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.RowDefinitions>
///         <RowDefinition Height="Auto"/>
///         <RowDefinition Height="*"/>
///     </Grid.RowDefinitions>
///     
///     <!-- Font selector -->
///     <ComboBox Grid.Row="0"
///               SelectedItem="{Binding DocumentFontFamily, Mode=TwoWay}">
///         <ComboBoxItem Content="Arial"/>
///         <ComboBoxItem Content="Calibri"/>
///         <ComboBoxItem Content="Consolas"/>
///         <ComboBoxItem Content="Georgia"/>
///     </ComboBox>
///     
///     <!-- Document editor with bound font -->
///     <TextBox Grid.Row="1"
///              Text="{Binding DocumentContent}"
///              FontFamily="{Binding DocumentFontFamily, 
///                                  Converter={StaticResource FontConverter}, 
///                                  Mode=TwoWay}"
///              AcceptsReturn="True"
///              TextWrapping="Wrap"/>
/// </Grid>
/// ]]></code>
///    <strong>Load font from configuration:</strong>
///    <code>
/// public class AppSettings
/// {
///     public string EditorFontFamily { get; set; } = "Consolas";
///     public int EditorFontSize { get; set; } = 12;
/// }
/// 
/// public class MainViewModel : INotifyPropertyChanged
/// {
///     private readonly AppSettings _settings;
///     
///     public MainViewModel(AppSettings settings)
///     {
///         _settings = settings;
///     }
///     
///     public string EditorFont => _settings.EditorFontFamily;
///     public int FontSize => _settings.EditorFontSize;
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontConverter"/>
/// </Window.Resources>
/// 
/// <!-- Editor with font from settings -->
/// <TextBox FontFamily="{Binding EditorFont, Converter={StaticResource FontConverter}}"
///          FontSize="{Binding FontSize}"
///          AcceptsReturn="True"/>
/// ]]></code>
///    <strong>Font family list with preview:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontConverter"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding AvailableFonts}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <StackPanel Orientation="Horizontal">
///                 <!-- Font name in its own font -->
///                 <TextBlock Text="{Binding}" 
///                           FontFamily="{Binding Converter={StaticResource FontConverter}}"
///                           FontSize="14"
///                           Width="150"/>
///                 
///                 <!-- Sample text -->
///                 <TextBlock Text="The quick brown fox" 
///                           FontFamily="{Binding Converter={StaticResource FontConverter}}"
///                           FontSize="12"
///                           Foreground="Gray"
///                           Margin="10,0,0,0"/>
///             </StackPanel>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
///    <strong>Style with dynamic font:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontConverter"/>
///     
///     <Style x:Key="DynamicTextStyle" TargetType="TextBlock">
///         <Setter Property="FontFamily" 
///                 Value="{Binding CurrentFontFamily, 
///                                Converter={StaticResource FontConverter}}"/>
///         <Setter Property="FontSize" Value="14"/>
///     </Style>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <TextBlock Text="Heading" 
///                Style="{StaticResource DynamicTextStyle}"
///                FontSize="18"
///                FontWeight="Bold"/>
///     
///     <TextBlock Text="Body text" 
///                Style="{StaticResource DynamicTextStyle}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Font persistence example:</strong>
///    <code>
/// public class UserPreferences
/// {
///     public string PreferredFont { get; set; } = "Segoe UI";
///     
///     public void SaveToFile(string path)
///     {
///         var json = JsonSerializer.Serialize(this);
///         File.WriteAllText(path, json);
///     }
///     
///     public static UserPreferences LoadFromFile(string path)
///     {
///         var json = File.ReadAllText(path);
///         return JsonSerializer.Deserialize&lt;UserPreferences&gt;(json);
///     }
/// }
/// 
/// // ViewModel
/// public class SettingsViewModel : INotifyPropertyChanged
/// {
///     private readonly UserPreferences _preferences;
///     
///     public string PreferredFont
///     {
///         get => _preferences.PreferredFont;
///         set
///         {
///             _preferences.PreferredFont = value;
///             OnPropertyChanged();
///             _preferences.SaveToFile("preferences.json");
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- Font selection with persistence -->
/// <Window.Resources>
///     <data:StringToFontFamilyConverter x:Key="FontConverter"/>
/// </Window.Resources>
/// 
/// <ComboBox ItemsSource="{Binding SystemFonts}"
///           SelectedItem="{Binding PreferredFont, Mode=TwoWay}">
///     <ComboBox.ItemTemplate>
///         <DataTemplate>
///             <TextBlock Text="{Binding}" 
///                       FontFamily="{Binding Converter={StaticResource FontConverter}}"/>
///         </DataTemplate>
///     </ComboBox.ItemTemplate>
/// </ComboBox>
/// ]]></code>
/// </example>
public class StringToFontFamilyConverter : GenericConverter<string, FontFamily>
{
	/// <summary>
	///    Converts a font family name string to a <see cref="FontFamily"/> object.
	/// </summary>
	/// <param name="source">The font family name as a string (e.g., "Arial", "Consolas", "Segoe UI").</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A new <see cref="FontFamily"/> instance created from the <paramref name="source"/> string.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method creates a new <see cref="FontFamily"/> object using the provided font name string.
	///       The FontFamily constructor accepts standard font family names as well as font URIs.
	///    </para>
	///    <para>
	///       <strong>Supported font name formats:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Simple font names: "Arial", "Calibri", "Consolas"</description>
	///       </item>
	///       <item>
	///          <description>Font names with spaces: "Times New Roman", "Courier New", "Segoe UI"</description>
	///       </item>
	///       <item>
	///          <description>Font family fallbacks: "Segoe UI, Tahoma, Arial"</description>
	///       </item>
	///       <item>
	///          <description>Font URIs: "pack://application:,,,/Fonts/#CustomFont"</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new StringToFontFamilyConverter();
	/// 
	/// FontFamily arial = converter.Convert("Arial", CultureInfo.CurrentCulture);
	/// FontFamily consolas = converter.Convert("Consolas", CultureInfo.CurrentCulture);
	/// FontFamily timesNewRoman = converter.Convert("Times New Roman", CultureInfo.CurrentCulture);
	/// 
	/// // Use in WPF
	/// textBlock.FontFamily = converter.Convert("Segoe UI", CultureInfo.CurrentCulture);
	/// </code>
	/// </example>
	public override FontFamily Convert(string source, CultureInfo culture) => new(source);

	/// <summary>
	///    Converts a <see cref="FontFamily"/> object back to its string representation.
	/// </summary>
	/// <param name="result">The <see cref="FontFamily"/> to convert back to a string.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    The string representation of the <paramref name="result"/> FontFamily, obtained by calling
	///    <see cref="FontFamily.ToString"/>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method enables two-way data binding by converting <see cref="FontFamily"/> objects
	///       back to their string representation. The returned string can be used to recreate the
	///       FontFamily object using the <see cref="Convert"/> method.
	///    </para>
	///    <para>
	///       The string returned is typically the font family name or source that was used to create
	///       the FontFamily object.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new StringToFontFamilyConverter();
	/// 
	/// FontFamily arial = new FontFamily("Arial");
	/// string fontName = converter.ConvertBack(arial, CultureInfo.CurrentCulture);
	/// Console.WriteLine(fontName);  // Output: "Arial"
	/// 
	/// // Round-trip conversion
	/// FontFamily original = converter.Convert("Consolas", CultureInfo.CurrentCulture);
	/// string name = converter.ConvertBack(original, CultureInfo.CurrentCulture);
	/// FontFamily restored = converter.Convert(name, CultureInfo.CurrentCulture);
	/// 
	/// Console.WriteLine(original.Source == restored.Source);  // Output: true
	/// </code>
	/// </example>
	public override string ConvertBack(FontFamily result, CultureInfo culture) => result.ToString();
}