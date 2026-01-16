using System.Globalization;
using System.Windows.Data;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that applies various capitalization transformations to strings.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide culture-aware
///       string capitalization transformations. It supports five different capitalization modes: uppercase,
///       lowercase, title case, camel case, and Pascal case. The conversion respects the culture parameter
///       for proper text transformations.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Five capitalization modes:</strong> Upper, Lower, Title, Camel, and Pascal case
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Culture-aware:</strong> Uses <see cref="CultureInfo"/> for proper case transformations
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>String-to-string conversion:</strong> Input and output are both strings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Only supports forward conversion (no ConvertBack implementation)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>ValueConversion attribute:</strong> Decorated for use with <see cref="ValueConverterGroup"/>
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Capitalization modes:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Mode</term>
///          <description>Transformation Example</description>
///       </listheader>
///       <item>
///          <term><see cref="StringCapitalization.ToUpper"/></term>
///          <description>"hello world" → "HELLO WORLD"</description>
///       </item>
///       <item>
///          <term><see cref="StringCapitalization.ToLower"/></term>
///          <description>"HELLO WORLD" → "hello world"</description>
///       </item>
///       <item>
///          <term><see cref="StringCapitalization.ToTitleCase"/></term>
///          <description>"hello world" → "Hello World"</description>
///       </item>
///       <item>
///          <term><see cref="StringCapitalization.ToCamelCase"/></term>
///          <description>"hello world" → "helloWorld"</description>
///       </item>
///       <item>
///          <term><see cref="StringCapitalization.ToPascalCase"/></term>
///          <description>"hello world" → "HelloWorld"</description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Format user input to consistent capitalization for display</description>
///       </item>
///       <item>
///          <description>Display enum values or identifiers in proper case</description>
///       </item>
///       <item>
///          <description>Convert code identifiers to user-friendly formats</description>
///       </item>
///       <item>
///          <description>Normalize text display across UI elements</description>
///       </item>
///       <item>
///          <description>Apply consistent styling to data-bound text</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Convert text to uppercase:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToCapitalizedStringConverter x:Key="UpperConverter"
///                                              Capitalization="ToUpper"/>
/// </Window.Resources>
/// 
/// <!-- Display name in uppercase -->
/// <TextBlock Text="{Binding UserName, Converter={StaticResource UpperConverter}}"/>
/// ]]></code>
///    <strong>Display title in title case:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToCapitalizedStringConverter x:Key="TitleConverter"
///                                              Capitalization="ToTitleCase"/>
/// </Window.Resources>
/// 
/// <!-- Convert "hello world" to "Hello World" -->
/// <TextBlock Text="{Binding ArticleTitle, Converter={StaticResource TitleConverter}}"
///            FontSize="18"
///            FontWeight="Bold"/>
/// ]]></code>
///    <strong>Multiple capitalizations in one UI:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToCapitalizedStringConverter x:Key="UpperConverter"
///                                              Capitalization="ToUpper"/>
///     <data:StringToCapitalizedStringConverter x:Key="LowerConverter"
///                                              Capitalization="ToLower"/>
///     <data:StringToCapitalizedStringConverter x:Key="TitleConverter"
///                                              Capitalization="ToTitleCase"/>
///     <data:StringToCapitalizedStringConverter x:Key="PascalConverter"
///                                              Capitalization="ToPascalCase"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <TextBlock Text="{Binding CompanyName, Converter={StaticResource UpperConverter}}"
///                FontSize="24"
///                FontWeight="Bold"/>
///     
///     <TextBlock Text="{Binding Tagline, Converter={StaticResource TitleConverter}}"
///                FontSize="14"
///                Foreground="Gray"/>
///     
///     <TextBlock Text="{Binding Website, Converter={StaticResource LowerConverter}}"
///                FontSize="12"/>
///     
///     <TextBlock Text="{Binding PropertyName, Converter={StaticResource PascalConverter}}"
///                FontFamily="Consolas"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var upperConverter = new StringToCapitalizedStringConverter
/// {
///     Capitalization = StringCapitalization.ToUpper
/// };
/// 
/// string result1 = upperConverter.Convert("hello world", CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: "HELLO WORLD"
/// 
/// var lowerConverter = new StringToCapitalizedStringConverter
/// {
///     Capitalization = StringCapitalization.ToLower
/// };
/// 
/// string result2 = lowerConverter.Convert("HELLO WORLD", CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: "hello world"
/// 
/// var titleConverter = new StringToCapitalizedStringConverter
/// {
///     Capitalization = StringCapitalization.ToTitleCase
/// };
/// 
/// string result3 = titleConverter.Convert("hello world", CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: "Hello World"
/// 
/// var camelConverter = new StringToCapitalizedStringConverter
/// {
///     Capitalization = StringCapitalization.ToCamelCase
/// };
/// 
/// string result4 = camelConverter.Convert("hello world", CultureInfo.CurrentCulture);
/// Console.WriteLine(result4);  // Output: "helloWorld"
/// 
/// var pascalConverter = new StringToCapitalizedStringConverter
/// {
///     Capitalization = StringCapitalization.ToPascalCase
/// };
/// 
/// string result5 = pascalConverter.Convert("hello world", CultureInfo.CurrentCulture);
/// Console.WriteLine(result5);  // Output: "HelloWorld"
/// </code>
///    <strong>Data grid with formatted columns:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToCapitalizedStringConverter x:Key="TitleConverter"
///                                              Capitalization="ToTitleCase"/>
///     <data:StringToCapitalizedStringConverter x:Key="UpperConverter"
///                                              Capitalization="ToUpper"/>
/// </Window.Resources>
/// 
/// <DataGrid ItemsSource="{Binding Employees}">
///     <DataGrid.Columns>
///         <!-- Name in Title Case -->
///         <DataGridTextColumn Header="Name" 
///                            Binding="{Binding Name, 
///                                             Converter={StaticResource TitleConverter}}"/>
///         
///         <!-- Department in Uppercase -->
///         <DataGridTextColumn Header="Department" 
///                            Binding="{Binding Department, 
///                                             Converter={StaticResource UpperConverter}}"/>
///         
///         <!-- Email as-is -->
///         <DataGridTextColumn Header="Email" Binding="{Binding Email}"/>
///     </DataGrid.Columns>
/// </DataGrid>
/// ]]></code>
///    <strong>Form with consistent capitalization:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:StringToCapitalizedStringConverter x:Key="TitleConverter"
///                                              Capitalization="ToTitleCase"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Label Content="First Name:"/>
///     <TextBox x:Name="FirstNameTextBox" Text="{Binding FirstName, UpdateSourceTrigger=PropertyChanged}"/>
///     <TextBlock Text="{Binding Text, ElementName=FirstNameTextBox, 
///                              Converter={StaticResource TitleConverter}}"
///                FontStyle="Italic"
///                Foreground="Gray"
///                Margin="0,2,0,10"/>
///     
///     <Label Content="Last Name:"/>
///     <TextBox x:Name="LastNameTextBox" Text="{Binding LastName, UpdateSourceTrigger=PropertyChanged}"/>
///     <TextBlock Text="{Binding Text, ElementName=LastNameTextBox, 
///                              Converter={StaticResource TitleConverter}}"
///                FontStyle="Italic"
///                Foreground="Gray"
///                Margin="0,2,0,10"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Culture-specific transformations:</strong>
///    <code>
/// // Turkish locale has special case rules for 'i'
/// var converter = new StringToCapitalizedStringConverter
/// {
///     Capitalization = StringCapitalization.ToUpper
/// };
/// 
/// string turkish = "istanbul";
/// 
/// // Using Turkish culture
/// string result1 = converter.Convert(turkish, new CultureInfo("tr-TR"));
/// Console.WriteLine(result1);  // Output: "İSTANBUL" (note the dotted İ)
/// 
/// // Using invariant culture
/// string result2 = converter.Convert(turkish, CultureInfo.InvariantCulture);
/// Console.WriteLine(result2);  // Output: "ISTANBUL"
/// </code>
///    <strong>Combining with PipeConverter:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:PipeConverter x:Key="TrimAndTitlePipe">
///         <data:StringTrimConverter/>
///         <data:StringToCapitalizedStringConverter Capitalization="ToTitleCase"/>
///     </data:PipeConverter>
/// </Window.Resources>
/// 
/// <!-- Trims whitespace then converts to title case -->
///  <TextBlock Text="{Binding Input, Converter={StaticResource TrimAndTitlePipe}}"/>
/// ]]></code>
/// </example>
[ValueConversion(typeof(string), typeof(string))]
public class StringToCapitalizedStringConverter : GenericConverter<string, string>
{
	/// <summary>
	///    Gets or sets the capitalization mode to apply to the source string.
	/// </summary>
	/// <value>
	///    A <see cref="StringCapitalization"/> value specifying the transformation mode. 
	///    Default value is not set; you must configure this property.
	/// </value>
	/// <remarks>
	///    <para>
	///       Available modes:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="StringCapitalization.ToUpper"/>: Converts all characters to uppercase
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="StringCapitalization.ToLower"/>: Converts all characters to lowercase
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="StringCapitalization.ToTitleCase"/>: Capitalizes the first letter of each word
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="StringCapitalization.ToCamelCase"/>: Converts to camelCase (first word lowercase, rest capitalized)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="StringCapitalization.ToPascalCase"/>: Converts to PascalCase (all words capitalized)
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public StringCapitalization Capitalization { get; set; }

	/// <summary>
	///    Converts a string by applying the specified capitalization transformation.
	/// </summary>
	/// <param name="source">The string to transform.</param>
	/// <param name="culture">
	///    The culture to use for the transformation. This is important for proper case conversions
	///    in different languages (e.g., Turkish 'i' vs. English 'I').
	/// </param>
	/// <returns>
	///    The transformed string according to the <see cref="Capitalization"/> mode:
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If <see cref="Capitalization"/> is <see cref="StringCapitalization.ToUpper"/>,
	///             returns <paramref name="source"/> converted to uppercase
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="Capitalization"/> is <see cref="StringCapitalization.ToLower"/>,
	///             returns <paramref name="source"/> converted to lowercase
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="Capitalization"/> is <see cref="StringCapitalization.ToTitleCase"/>,
	///             returns <paramref name="source"/> in title case (first letter of each word capitalized)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="Capitalization"/> is <see cref="StringCapitalization.ToCamelCase"/>,
	///             returns <paramref name="source"/> in camel case
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="Capitalization"/> is <see cref="StringCapitalization.ToPascalCase"/>,
	///             returns <paramref name="source"/> in Pascal case
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             For any other value, returns the original <paramref name="source"/> unchanged
	///          </description>
	///       </item>
	///    </list>
	/// </returns>
	/// <remarks>
	///    <para>
	///       The conversion uses culture-aware extension methods (<c>ToUpper(culture)</c>, <c>ToLower(culture)</c>, etc.)
	///       to ensure proper text transformation for different locales. This is particularly important for languages
	///       with special case rules like Turkish.
	///    </para>
	///    <para>
	///       The camel case and Pascal case conversions use custom extension methods (<c>ToCamelCase</c> and <c>ToPascalCase</c>)
	///       which handle word boundary detection and capitalization.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new StringToCapitalizedStringConverter();
	/// 
	/// // Uppercase
	/// converter.Capitalization = StringCapitalization.ToUpper;
	/// Console.WriteLine(converter.Convert("hello", CultureInfo.InvariantCulture));  // "HELLO"
	/// 
	/// // Lowercase
	/// converter.Capitalization = StringCapitalization.ToLower;
	/// Console.WriteLine(converter.Convert("HELLO", CultureInfo.InvariantCulture));  // "hello"
	/// 
	/// // Title Case
	/// converter.Capitalization = StringCapitalization.ToTitleCase;
	/// Console.WriteLine(converter.Convert("hello world", CultureInfo.InvariantCulture));  // "Hello World"
	/// 
	/// // Camel Case
	/// converter.Capitalization = StringCapitalization.ToCamelCase;
	/// Console.WriteLine(converter.Convert("hello world", CultureInfo.InvariantCulture));  // "helloWorld"
	/// 
	/// // Pascal Case
	/// converter.Capitalization = StringCapitalization.ToPascalCase;
	/// Console.WriteLine(converter.Convert("hello world", CultureInfo.InvariantCulture));  // "HelloWorld"
	/// </code>
	/// </example>
	public override string Convert(string source, CultureInfo culture)
	{
		return Capitalization switch
		{
			StringCapitalization.ToUpper => source.ToUpper(culture),
			StringCapitalization.ToLower => source.ToLower(culture),
			StringCapitalization.ToTitleCase => source.ToTitleCase(culture),
			StringCapitalization.ToCamelCase => source.ToCamelCase(culture),
			StringCapitalization.ToPascalCase => source.ToPascalCase(culture),
			_ => source
		};
	}
}

/// <summary>
///    Specifies the capitalization transformation mode for <see cref="StringToCapitalizedStringConverter"/>.
/// </summary>
public enum StringCapitalization
{
	/// <summary>
	///    Convert all characters to uppercase (e.g., "hello" → "HELLO").
	/// </summary>
	ToUpper,

	/// <summary>
	///    Convert all characters to lowercase (e.g., "HELLO" → "hello").
	/// </summary>
	ToLower,

	/// <summary>
	///    Convert to title case where the first letter of each word is capitalized (e.g., "hello world" → "Hello World").
	/// </summary>
	ToTitleCase,

	/// <summary>
	///    Convert to camel case where the first word is lowercase and subsequent words are capitalized
	///    (e.g., "hello world" → "helloWorld").
	/// </summary>
	ToCamelCase,

	/// <summary>
	///    Convert to Pascal case where all words are capitalized with no spaces (e.g., "hello world" → "HelloWorld").
	/// </summary>
	ToPascalCase
}