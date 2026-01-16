using System;
using System.Globalization;
using Fuxion.Windows.Resources;

namespace Fuxion.Windows.Data;

/// <summary>
///    Specifies the formatting mode for converting <see cref="TimeSpan"/> values to strings.
/// </summary>
public enum TimeSpanToStringMode
{
	/// <summary>
	///    Format as individual time components with numbers and unit labels (e.g., "2 days, 3 hours, 45 minutes").
	/// </summary>
	PerElements,

	/// <summary>
	///    Format as individual time components using only letter abbreviations without numbers (e.g., "2d 3h 45m").
	/// </summary>
	PerElementsOnlyLetters,

	/// <summary>
	///    Format showing total values for each unit (e.g., "Total: 51 hours, 45 minutes").
	/// </summary>
	PerTotals,

	/// <summary>
	///    Format as raw tick count (e.g., "918000000000 ticks").
	/// </summary>
	Ticks
}

/// <summary>
///    A value converter that converts nullable <see cref="TimeSpan"/> values to formatted string representations.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide human-readable
///       string formatting of <see cref="TimeSpan"/> values with multiple formatting modes. It's particularly
///       useful for displaying durations, elapsed times, or time periods in a user-friendly format.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Multiple formatting modes:</strong> Four different presentation styles for TimeSpan values
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configurable precision:</strong> Control how many time components to display (days, hours, minutes, etc.)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Nullable support:</strong> Handles null TimeSpan values gracefully
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Localized output:</strong> Uses resource strings for proper localization
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Optimized for display purposes (no ConvertBack implementation)
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Formatting modes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <see cref="TimeSpanToStringMode.PerElements"/>: Full text with unit names (e.g., "2 days, 3 hours")
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="TimeSpanToStringMode.PerElementsOnlyLetters"/>: Compact format with letter abbreviations (e.g., "2d 3h")
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="TimeSpanToStringMode.PerTotals"/>: Shows total values for each unit
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="TimeSpanToStringMode.Ticks"/>: Raw tick count with "tick/ticks" label
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display elapsed time in logs or status messages</description>
///       </item>
///       <item>
///          <description>Show duration of operations or processes</description>
///       </item>
///       <item>
///          <description>Format time intervals for user interfaces</description>
///       </item>
///       <item>
///          <description>Create readable time period descriptions</description>
///       </item>
///       <item>
///          <description>Display countdown or count-up timers in human-readable format</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Display elapsed time with full text:</strong>
///    <code>
/// public class ProcessViewModel : INotifyPropertyChanged
/// {
///     private TimeSpan? _elapsedTime;
///     
///     public TimeSpan? ElapsedTime
///     {
///         get => _elapsedTime;
///         set
///         {
///             _elapsedTime = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToStringConverter x:Key="TimeConverter"
///                                     Mode="PerElements"
///                                     NumberOfElements="3"/>
/// </Window.Resources>
/// 
/// <!-- Display as "2 hours, 30 minutes, 15 seconds" -->
/// <TextBlock Text="{Binding ElapsedTime, Converter={StaticResource TimeConverter}}"/>
/// ]]></code>
///    <strong>Compact format with letters:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToStringConverter x:Key="CompactConverter"
///                                     Mode="PerElementsOnlyLetters"
///                                     NumberOfElements="2"/>
/// </Window.Resources>
/// 
/// <!-- Display as "2h 30m" -->
/// <TextBlock Text="{Binding Duration, Converter={StaticResource CompactConverter}}"/>
/// ]]></code>
///    <strong>Usage in code-behind with different modes:</strong>
///    <code>
/// var time = TimeSpan.FromHours(2.5);  // 2 hours, 30 minutes
/// 
/// // PerElements mode
/// var elementsConverter = new TimeSpanToStringConverter
/// {
///     Mode = TimeSpanToStringMode.PerElements,
///     NumberOfElements = 3
/// };
/// string result1 = elementsConverter.Convert(time, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: "2 hours, 30 minutes"
/// 
/// // PerElementsOnlyLetters mode
/// var lettersConverter = new TimeSpanToStringConverter
/// {
///     Mode = TimeSpanToStringMode.PerElementsOnlyLetters,
///     NumberOfElements = 2
/// };
/// string result2 = lettersConverter.Convert(time, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: "2h 30m"
/// 
/// // Ticks mode
/// var ticksConverter = new TimeSpanToStringConverter
/// {
///     Mode = TimeSpanToStringMode.Ticks
/// };
/// string result3 = ticksConverter.Convert(time, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: "90000000000 ticks"
/// </code>
///    <strong>Null handling:</strong>
///    <code>
/// var converter = new TimeSpanToStringConverter
/// {
///     Mode = TimeSpanToStringMode.PerElements,
///     NumberOfElements = 3
/// };
/// 
/// TimeSpan? nullTime = null;
/// string result = converter.Convert(nullTime, CultureInfo.CurrentCulture);
/// Console.WriteLine(result == null);  // Output: true
/// </code>
///    <strong>Process monitoring with elapsed time:</strong>
///    <code>
/// public class OperationViewModel : INotifyPropertyChanged
/// {
///     private DateTime _startTime;
///     private TimeSpan? _elapsed;
///     
///     public TimeSpan? Elapsed
///     {
///         get => _elapsed;
///         set
///         {
///             _elapsed = value;
///             OnPropertyChanged();
///         }
///     }
///     
///     public void Start()
///     {
///         _startTime = DateTime.Now;
///         var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
///         timer.Tick += (s, e) => Elapsed = DateTime.Now - _startTime;
///         timer.Start();
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToStringConverter x:Key="ElapsedConverter"
///                                     Mode="PerElements"
///                                     NumberOfElements="3"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <TextBlock Text="Operation in progress..."/>
///     <TextBlock Text="{Binding Elapsed, 
///                              Converter={StaticResource ElapsedConverter}, 
///                              StringFormat='Elapsed: {0}'}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Different formatting for different contexts:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToStringConverter x:Key="DetailedConverter"
///                                     Mode="PerElements"
///                                     NumberOfElements="5"/>
///     
///     <data:TimeSpanToStringConverter x:Key="CompactConverter"
///                                     Mode="PerElementsOnlyLetters"
///                                     NumberOfElements="2"/>
///     
///     <data:TimeSpanToStringConverter x:Key="TicksConverter"
///                                     Mode="Ticks"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <!-- Detailed view -->
///     <TextBlock Text="{Binding ProcessDuration, Converter={StaticResource DetailedConverter}}"
///                FontSize="16"/>
///     
///     <!-- Compact view for status bar -->
///     <StatusBar>
///         <StatusBarItem>
///             <TextBlock Text="{Binding ProcessDuration, 
///                                      Converter={StaticResource CompactConverter}, 
///                                      StringFormat='Time: {0}'}"/>
///         </StatusBarItem>
///     </StatusBar>
///     
///     <!-- Debug/technical view -->
///     <TextBlock Text="{Binding ProcessDuration, Converter={StaticResource TicksConverter}}"
///                FontFamily="Consolas"
///                FontSize="10"
///                Foreground="Gray"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Controlling precision with NumberOfElements:</strong>
///    <code>
/// var time = TimeSpan.FromHours(25.5);  // 1 day, 1 hour, 30 minutes
/// var converter = new TimeSpanToStringConverter
/// {
///     Mode = TimeSpanToStringMode.PerElements
/// };
/// 
/// // Show 5 components (default)
/// converter.NumberOfElements = 5;
/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
/// // Output: "1 day, 1 hour, 30 minutes"
/// 
/// // Show only 2 components
/// converter.NumberOfElements = 2;
/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
/// // Output: "1 day, 1 hour"
/// 
/// // Show only 1 component
/// converter.NumberOfElements = 1;
/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
/// // Output: "1 day"
/// </code>
///    <strong>Timer display with auto-update:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToStringConverter x:Key="TimerConverter"
///                                     Mode="PerElementsOnlyLetters"
///                                     NumberOfElements="3"/>
/// </Window.Resources>
/// 
/// <Border Background="Black" Padding="10">
///     <TextBlock Text="{Binding CountdownTime, Converter={StaticResource TimerConverter}}"
///                Foreground="Lime"
///                FontFamily="Consolas"
///                FontSize="24"
///                HorizontalAlignment="Center"/>
/// </Border>
/// ]]></code>
///    <strong>Log entry with detailed timing:</strong>
///    <code>
/// public class LogEntryViewModel
/// {
///     public string Message { get; set; }
///     public TimeSpan? ExecutionTime { get; set; }
///     public DateTime Timestamp { get; set; }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToStringConverter x:Key="TimeConverter"
///                                     Mode="PerElements"
///                                     NumberOfElements="4"/>
/// </Window.Resources>
/// 
/// <ListBox ItemsSource="{Binding LogEntries}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <StackPanel>
///                 <TextBlock Text="{Binding Message}" FontWeight="Bold"/>
///                 <TextBlock>
///                     <Run Text="Executed in: "/>
///                     <Run Text="{Binding ExecutionTime, 
///                                        Converter={StaticResource TimeConverter}}"
///                          Foreground="Blue"/>
///                 </TextBlock>
///                 <TextBlock Text="{Binding Timestamp, StringFormat='at {0:HH:mm:ss}'}"
///                           FontSize="10"
///                           Foreground="Gray"/>
///             </StackPanel>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
/// </example>
public class TimeSpanToStringConverter : GenericConverter<TimeSpan?, string?>
{
	/// <summary>
	///    Gets or sets the formatting mode for the TimeSpan-to-string conversion.
	/// </summary>
	/// <value>
	///    A <see cref="TimeSpanToStringMode"/> value specifying the output format.
	///    Default value is not set; you must configure this property.
	/// </value>
	/// <remarks>
	///    The mode determines how the TimeSpan is formatted:
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.PerElements"/>: Uses <c>ToTimeString()</c> extension method
	///             with full unit names
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.PerElementsOnlyLetters"/>: Uses <c>ToTimeString()</c> with
	///             compact letter abbreviations
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.PerTotals"/>: Uses standard <see cref="TimeSpan.ToString()"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.Ticks"/>: Formats as tick count with localized "tick/ticks" label
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public TimeSpanToStringMode Mode { get; set; }

	/// <summary>
	///    Gets or sets the number of time components to include in the formatted output.
	/// </summary>
	/// <value>
	///    The number of time elements to display (e.g., 3 would show days, hours, and minutes).
	///    Default is <c>5</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property only affects the <see cref="TimeSpanToStringMode.PerElements"/> and
	///       <see cref="TimeSpanToStringMode.PerElementsOnlyLetters"/> modes.
	///    </para>
	///    <para>
	///       Time components are typically (in order): Days, Hours, Minutes, Seconds, Milliseconds.
	///       Setting <see cref="NumberOfElements"/> to 2 would show only the two most significant components.
	///    </para>
	/// </remarks>
	public int NumberOfElements { get; set; } = 5;

	/// <summary>
	///    Converts a nullable <see cref="TimeSpan"/> to its string representation using the configured formatting mode.
	/// </summary>
	/// <param name="source">The nullable <see cref="TimeSpan"/> to convert. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter for localization of unit names and formatting.
	/// </param>
	/// <returns>
	///    A formatted string representation of the <paramref name="source"/> TimeSpan, or <c>null</c>
	///    if <paramref name="source"/> is <c>null</c> or doesn't have a value.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The conversion behavior depends on the <see cref="Mode"/> property:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.PerElements"/>: Calls <c>ts.Fx.Time.ToTimeString(NumberOfElements)</c>
	///             to produce output like "2 hours, 30 minutes"
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.PerElementsOnlyLetters"/>: Calls <c>ts.Fx.Time.ToTimeString(NumberOfElements, true)</c>
	///             to produce compact output like "2h 30m"
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.Ticks"/>: Formats as "{ticks} {tick/ticks}" using localized
	///             resource strings from <see cref="Strings.tick"/> and <see cref="Strings.ticks"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="TimeSpanToStringMode.PerTotals"/> or any other value: Returns <see cref="TimeSpan.ToString()"/>
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       The <c>Fx.Time.ToTimeString()</c> extension methods are part of the Fuxion framework and provide
	///       intelligent formatting that shows only the most significant time components based on
	///       <see cref="NumberOfElements"/>.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new TimeSpanToStringConverter();
	/// var time = TimeSpan.FromHours(2.5);
	/// 
	/// // PerElements mode
	/// converter.Mode = TimeSpanToStringMode.PerElements;
	/// converter.NumberOfElements = 2;
	/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
	/// // Output: "2 hours, 30 minutes"
	/// 
	/// // PerElementsOnlyLetters mode
	/// converter.Mode = TimeSpanToStringMode.PerElementsOnlyLetters;
	/// converter.NumberOfElements = 2;
	/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
	/// // Output: "2h 30m"
	/// 
	/// // Ticks mode
	/// converter.Mode = TimeSpanToStringMode.Ticks;
	/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
	/// // Output: "90000000000 ticks"
	/// 
	/// // PerTotals mode (default ToString)
	/// converter.Mode = TimeSpanToStringMode.PerTotals;
	/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));
	/// // Output: "02:30:00"
	/// </code>
	/// </example>
	public override string? Convert(TimeSpan? source, CultureInfo culture)
	{
		if (source == null || !source.HasValue) return null;
		var ts = source.Value;
		var res = "";
		switch (Mode)
		{
			case TimeSpanToStringMode.PerElements:            return ts.Fx.Time.ToTimeString(NumberOfElements);
			case TimeSpanToStringMode.PerElementsOnlyLetters: return ts.Fx.Time.ToTimeString(NumberOfElements, true);
			case TimeSpanToStringMode.Ticks:
				res += $"{ts.Ticks} {(ts.Ticks > 1 ? Strings.ticks : Strings.tick)}, ";
				return res;
			default: return ts.ToString();
		}
	}
}