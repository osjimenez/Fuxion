using System;
using System.Globalization;

namespace Fuxion.Windows.Data;

/// <summary>
///    Specifies which time component of a <see cref="TimeSpan"/> to extract when converting to a long integer.
/// </summary>
public enum TimeSpanToLongValue
{
	/// <summary>
	///    Extract the total number of ticks (1 tick = 100 nanoseconds).
	/// </summary>
	Ticks,

	/// <summary>
	///    Extract milliseconds component or total milliseconds depending on <see cref="TimeSpanToLongConverter.ConvertToLongAsTotalCount"/>.
	/// </summary>
	Milliseconds,

	/// <summary>
	///    Extract seconds component or total seconds depending on <see cref="TimeSpanToLongConverter.ConvertToLongAsTotalCount"/>.
	/// </summary>
	Seconds,

	/// <summary>
	///    Extract minutes component or total minutes depending on <see cref="TimeSpanToLongConverter.ConvertToLongAsTotalCount"/>.
	/// </summary>
	Minutes,

	/// <summary>
	///    Extract hours component or total hours depending on <see cref="TimeSpanToLongConverter.ConvertToLongAsTotalCount"/>.
	/// </summary>
	Hours,

	/// <summary>
	///    Extract days component or total days depending on <see cref="TimeSpanToLongConverter.ConvertToLongAsTotalCount"/>.
	/// </summary>
	Days
}

/// <summary>
///    A value converter that converts nullable <see cref="TimeSpan"/> values to nullable long integers by extracting
///    a specific time component or total time value.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide bidirectional conversion
///       between <see cref="TimeSpan"/> and <see cref="long"/> values. It can extract either time components
///       (e.g., the seconds part of a TimeSpan) or total time values (e.g., total seconds) depending on configuration.
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
///             <strong>Nullable support:</strong> Handles null TimeSpan and null long values gracefully
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible extraction:</strong> Choose between component values or total count
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Six time units:</strong> Supports Ticks, Milliseconds, Seconds, Minutes, Hours, and Days
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Two-way binding support:</strong> Works seamlessly with bidirectional WPF bindings
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Conversion modes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Component mode</strong> (<see cref="ConvertToLongAsTotalCount"/> = <c>false</c>):
///             Extracts the specific component (e.g., for "1:30:45", Seconds = 45)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Total mode</strong> (<see cref="ConvertToLongAsTotalCount"/> = <c>true</c>):
///             Calculates total value (e.g., for "1:30:45", TotalSeconds = 5445)
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display or edit specific time components in numeric controls</description>
///       </item>
///       <item>
///          <description>Convert TimeSpan durations to numeric values for calculations</description>
///       </item>
///       <item>
///          <description>Bind TimeSpan properties to sliders or numeric up/down controls</description>
///       </item>
///       <item>
///          <description>Store time durations as integers in databases or configurations</description>
///       </item>
///       <item>
///          <description>Create time input controls with separate numeric fields</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Extract total seconds from TimeSpan:</strong>
///    <code>
/// public class TimerViewModel : INotifyPropertyChanged
/// {
///     private TimeSpan? _duration = TimeSpan.FromMinutes(5);
///     
///     public TimeSpan? Duration
///     {
///         get => _duration;
///         set
///         {
///             _duration = value;
///             OnPropertyChanged();
///         }
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToLongConverter x:Key="TotalSecondsConverter"
///                                   Value="Seconds"
///                                   ConvertToLongAsTotalCount="True"/>
/// </Window.Resources>
/// 
/// <!-- Display duration as total seconds (300) -->
/// <TextBlock Text="{Binding Duration, 
///                          Converter={StaticResource TotalSecondsConverter}, 
///                          StringFormat='{}{0} seconds'}"/>
/// ]]></code>
///    <strong>Time component editor with separate fields:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToLongConverter x:Key="HoursConverter"
///                                   Value="Hours"
///                                   ConvertToLongAsTotalCount="False"/>
///     <data:TimeSpanToLongConverter x:Key="MinutesConverter"
///                                   Value="Minutes"
///                                   ConvertToLongAsTotalCount="False"/>
///     <data:TimeSpanToLongConverter x:Key="SecondsConverter"
///                                   Value="Seconds"
///                                   ConvertToLongAsTotalCount="False"/>
/// </Window.Resources>
/// 
/// <StackPanel Orientation="Horizontal">
///     <Label Content="Hours:"/>
///     <TextBox Text="{Binding Duration, 
///                            Converter={StaticResource HoursConverter}, 
///                            Mode=TwoWay}" 
///              Width="50"/>
///     
///     <Label Content="Minutes:"/>
///     <TextBox Text="{Binding Duration, 
///                            Converter={StaticResource MinutesConverter}, 
///                            Mode=TwoWay}" 
///              Width="50"/>
///     
///     <Label Content="Seconds:"/>
///     <TextBox Text="{Binding Duration, 
///                            Converter={StaticResource SecondsConverter}, 
///                            Mode=TwoWay}" 
///              Width="50"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Usage in code-behind with component mode:</strong>
///    <code>
/// var converter = new TimeSpanToLongConverter
/// {
///     Value = TimeSpanToLongValue.Seconds,
///     ConvertToLongAsTotalCount = false  // Component mode
/// };
/// 
/// TimeSpan time = TimeSpan.FromHours(1.5);  // 1:30:00
/// long? seconds = converter.Convert(time, CultureInfo.CurrentCulture);
/// Console.WriteLine(seconds);  // Output: 30 (component, not total)
/// 
/// // Convert back
/// TimeSpan? restored = converter.ConvertBack(45, CultureInfo.CurrentCulture);
/// Console.WriteLine(restored);  // Output: 00:00:45
/// </code>
///    <strong>Usage with total mode:</strong>
///    <code>
/// var totalSecondsConverter = new TimeSpanToLongConverter
/// {
///     Value = TimeSpanToLongValue.Seconds,
///     ConvertToLongAsTotalCount = true  // Total mode
/// };
/// 
/// TimeSpan time = TimeSpan.FromHours(1.5);  // 1:30:00
/// long? totalSeconds = totalSecondsConverter.Convert(time, CultureInfo.CurrentCulture);
/// Console.WriteLine(totalSeconds);  // Output: 5400 (total seconds)
/// 
/// // Different units
/// var totalMinutesConverter = new TimeSpanToLongConverter
/// {
///     Value = TimeSpanToLongValue.Minutes,
///     ConvertToLongAsTotalCount = true
/// };
/// 
/// long? totalMinutes = totalMinutesConverter.Convert(time, CultureInfo.CurrentCulture);
/// Console.WriteLine(totalMinutes);  // Output: 90 (total minutes)
/// </code>
///    <strong>Slider bound to total hours:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToLongConverter x:Key="TotalHoursConverter"
///                                   Value="Hours"
///                                   ConvertToLongAsTotalCount="True"/>
/// </Window.Resources>
/// 
/// <StackPanel>
///     <Slider Minimum="0" 
///             Maximum="24"
///             Value="{Binding WorkDuration, 
///                            Converter={StaticResource TotalHoursConverter}, 
///                            Mode=TwoWay}"/>
///     
///     <TextBlock Text="{Binding WorkDuration, StringFormat='Duration: {0:hh\\:mm\\:ss}'}"/>
/// </StackPanel>
/// ]]></code>
///    <strong>Null handling:</strong>
///    <code>
/// var converter = new TimeSpanToLongConverter
/// {
///     Value = TimeSpanToLongValue.Seconds,
///     ConvertToLongAsTotalCount = true
/// };
/// 
/// // Null TimeSpan
/// TimeSpan? nullTime = null;
/// long? result1 = converter.Convert(nullTime, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1 == null);  // Output: true
/// 
/// // Null long
/// long? nullLong = null;
/// TimeSpan? result2 = converter.ConvertBack(nullLong, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2 == null);  // Output: true
/// </code>
///    <strong>All time units example:</strong>
///    <code>
/// var time = TimeSpan.FromHours(25.5);  // 1 day, 1 hour, 30 minutes
/// 
/// // Ticks (always total)
/// var ticksConverter = new TimeSpanToLongConverter { Value = TimeSpanToLongValue.Ticks };
/// Console.WriteLine(ticksConverter.Convert(time, CultureInfo.CurrentCulture));  // 918000000000
/// 
/// // Days - component vs total
/// var daysConverter = new TimeSpanToLongConverter 
/// { 
///     Value = TimeSpanToLongValue.Days,
///     ConvertToLongAsTotalCount = false 
/// };
/// Console.WriteLine(daysConverter.Convert(time, CultureInfo.CurrentCulture));  // 1 (component)
/// 
/// daysConverter.ConvertToLongAsTotalCount = true;
/// Console.WriteLine(daysConverter.Convert(time, CultureInfo.CurrentCulture));  // 1 (total, truncated)
/// 
/// // Hours - component vs total
/// var hoursConverter = new TimeSpanToLongConverter 
/// { 
///     Value = TimeSpanToLongValue.Hours,
///     ConvertToLongAsTotalCount = false 
/// };
/// Console.WriteLine(hoursConverter.Convert(time, CultureInfo.CurrentCulture));  // 1 (component)
/// 
/// hoursConverter.ConvertToLongAsTotalCount = true;
/// Console.WriteLine(hoursConverter.Convert(time, CultureInfo.CurrentCulture));  // 25 (total)
/// 
/// // Minutes - component vs total
/// var minutesConverter = new TimeSpanToLongConverter 
/// { 
///     Value = TimeSpanToLongValue.Minutes,
///     ConvertToLongAsTotalCount = false 
/// };
/// Console.WriteLine(minutesConverter.Convert(time, CultureInfo.CurrentCulture));  // 30 (component)
/// 
/// minutesConverter.ConvertToLongAsTotalCount = true;
/// Console.WriteLine(minutesConverter.Convert(time, CultureInfo.CurrentCulture));  // 1530 (total)
/// </code>
///    <strong>Duration picker with up/down controls:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToLongConverter x:Key="MinutesConverter"
///                                   Value="Minutes"
///                                   ConvertToLongAsTotalCount="False"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.ColumnDefinitions>
///         <ColumnDefinition Width="Auto"/>
///         <ColumnDefinition Width="*"/>
///     </Grid.ColumnDefinitions>
///     
///     <Label Grid.Column="0" Content="Duration (minutes):"/>
///     
///     <StackPanel Grid.Column="1" Orientation="Horizontal">
///         <Button Content="▲" 
///                 Command="{Binding IncrementMinutesCommand}"/>
///         <TextBlock Text="{Binding Duration, 
///                                  Converter={StaticResource MinutesConverter}}"
///                    VerticalAlignment="Center"
///                    Margin="5,0"/>
///         <Button Content="▼" 
///                 Command="{Binding DecrementMinutesCommand}"/>
///     </StackPanel>
/// </Grid>
/// ]]></code>
///    <strong>Progress indicator with milliseconds:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:TimeSpanToLongConverter x:Key="MillisecondsConverter"
///                                   Value="Milliseconds"
///                                   ConvertToLongAsTotalCount="True"/>
/// </Window.Resources>
/// 
/// <ProgressBar Minimum="0"
///              Maximum="10000"
///              Value="{Binding ElapsedTime, 
///                             Converter={StaticResource MillisecondsConverter}}"/>
/// ]]></code>
/// </example>
public class TimeSpanToLongConverter : GenericConverter<TimeSpan?, long?>
{
	/// <summary>
	///    Gets or sets which time component to extract from the <see cref="TimeSpan"/>.
	/// </summary>
	/// <value>
	///    A <see cref="TimeSpanToLongValue"/> specifying which component to extract.
	///    Default value is not set; you must configure this property.
	/// </value>
	public TimeSpanToLongValue Value { get; set; }

	/// <summary>
	///    Gets or sets whether to extract the total count or just the component value.
	/// </summary>
	/// <value>
	///    <c>true</c> to extract total values (e.g., <see cref="TimeSpan.TotalSeconds"/>);
	///    <c>false</c> to extract component values (e.g., <see cref="TimeSpan.Seconds"/>).
	///    Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       <strong>Component mode</strong> (<c>false</c>): Extracts the specific time component
	///       (range 0-59 for seconds, 0-59 for minutes, 0-23 for hours, etc.)
	///    </para>
	///    <para>
	///       <strong>Total mode</strong> (<c>true</c>): Calculates the total value in the specified unit
	///       (can be any value depending on the TimeSpan magnitude)
	///    </para>
	///    <para>
	///       <strong>Note:</strong> This property is ignored when <see cref="Value"/> is <see cref="TimeSpanToLongValue.Ticks"/>,
	///       as <see cref="TimeSpan.Ticks"/> always returns the total tick count.
	///    </para>
	/// </remarks>
	public bool ConvertToLongAsTotalCount { get; set; }

	/// <summary>
	///    Converts a nullable <see cref="TimeSpan"/> to a nullable long integer by extracting the specified time component.
	/// </summary>
	/// <param name="source">The nullable <see cref="TimeSpan"/> to convert. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A nullable long integer containing the extracted time component value, or <c>null</c> if
	///    <paramref name="source"/> is <c>null</c> or doesn't have a value. Returns <c>null</c> for
	///    unrecognized <see cref="Value"/> settings.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The conversion behavior depends on both <see cref="Value"/> and <see cref="ConvertToLongAsTotalCount"/>:
	///    </para>
	///    <list type="table">
	///       <listheader>
	///          <term>Value</term>
	///          <term>ConvertToLongAsTotalCount</term>
	///          <description>Returns</description>
	///       </listheader>
	///       <item>
	///          <term>Ticks</term>
	///          <term>N/A</term>
	///          <description><see cref="TimeSpan.Ticks"/></description>
	///       </item>
	///       <item>
	///          <term>Milliseconds</term>
	///          <term>false</term>
	///          <description><see cref="TimeSpan.Milliseconds"/> (0-999)</description>
	///       </item>
	///       <item>
	///          <term>Milliseconds</term>
	///          <term>true</term>
	///          <description>(long)<see cref="TimeSpan.TotalMilliseconds"/></description>
	///       </item>
	///       <item>
	///          <term>Seconds</term>
	///          <term>false</term>
	///          <description><see cref="TimeSpan.Seconds"/> (0-59)</description>
	///       </item>
	///       <item>
	///          <term>Seconds</term>
	///          <term>true</term>
	///          <description>(int)<see cref="TimeSpan.TotalSeconds"/></description>
	///       </item>
	///       <item>
	///          <term>Minutes</term>
	///          <term>false</term>
	///          <description><see cref="TimeSpan.Minutes"/> (0-59)</description>
	///       </item>
	///       <item>
	///          <term>Minutes</term>
	///          <term>true</term>
	///          <description>(int)<see cref="TimeSpan.TotalMinutes"/></description>
	///       </item>
	///       <item>
	///          <term>Hours</term>
	///          <term>false</term>
	///          <description><see cref="TimeSpan.Hours"/> (0-23)</description>
	///       </item>
	///       <item>
	///          <term>Hours</term>
	///          <term>true</term>
	///          <description>(int)<see cref="TimeSpan.TotalHours"/></description>
	///       </item>
	///       <item>
	///          <term>Days</term>
	///          <term>false</term>
	///          <description><see cref="TimeSpan.Days"/></description>
	///       </item>
	///       <item>
	///          <term>Days</term>
	///          <term>true</term>
	///          <description>(int)<see cref="TimeSpan.TotalDays"/></description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Note:</strong> Total values are cast to <c>int</c> or <c>long</c>, which truncates
	///       any fractional parts.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new TimeSpanToLongConverter();
	/// var time = TimeSpan.FromSeconds(125);  // 2 minutes, 5 seconds
	/// 
	/// // Component mode
	/// converter.Value = TimeSpanToLongValue.Seconds;
	/// converter.ConvertToLongAsTotalCount = false;
	/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));  // 5
	/// 
	/// // Total mode
	/// converter.ConvertToLongAsTotalCount = true;
	/// Console.WriteLine(converter.Convert(time, CultureInfo.CurrentCulture));  // 125
	/// </code>
	/// </example>
	public override long? Convert(TimeSpan? source, CultureInfo culture)
	{
		if (source == null || !source.HasValue) return null;
		return Value switch
		{
			TimeSpanToLongValue.Ticks => source.Value.Ticks,
			TimeSpanToLongValue.Milliseconds => ConvertToLongAsTotalCount
				? (long)source.Value.TotalMilliseconds
				: source.Value.Milliseconds,
			TimeSpanToLongValue.Seconds => ConvertToLongAsTotalCount
				? (int)source.Value.TotalSeconds
				: source.Value.Seconds,
			TimeSpanToLongValue.Minutes => ConvertToLongAsTotalCount
				? (int)source.Value.TotalMinutes
				: source.Value.Minutes,
			TimeSpanToLongValue.Hours => ConvertToLongAsTotalCount ? (int)source.Value.TotalHours : source.Value.Hours,
			TimeSpanToLongValue.Days => ConvertToLongAsTotalCount ? (int)source.Value.TotalDays : source.Value.Days,
			_ => null
		};
	}

	/// <summary>
	///    Converts a nullable long integer back to a nullable <see cref="TimeSpan"/> using the specified time unit.
	/// </summary>
	/// <param name="result">The nullable long integer to convert back. Can be <c>null</c>.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    A nullable <see cref="TimeSpan"/> created from the <paramref name="result"/> value using
	///    the time unit specified in <see cref="Value"/>, or <c>null</c> if <paramref name="result"/>
	///    is <c>null</c> or doesn't have a value.
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when <see cref="Value"/> is set to an unrecognized value.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method enables two-way data binding by creating TimeSpan instances from long values.
	///       The <see cref="ConvertToLongAsTotalCount"/> property is not used during backward conversion;
	///       the conversion always interprets the long value as a quantity in the specified unit.
	///    </para>
	///    <para>
	///       Conversion methods used:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="TimeSpanToLongValue.Ticks"/>: <see cref="TimeSpan.FromTicks"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="TimeSpanToLongValue.Milliseconds"/>: <see cref="TimeSpan.FromMilliseconds(double)"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="TimeSpanToLongValue.Seconds"/>: <see cref="TimeSpan.FromSeconds(double)"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="TimeSpanToLongValue.Minutes"/>: <see cref="TimeSpan.FromMinutes(double)"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="TimeSpanToLongValue.Hours"/>: <see cref="TimeSpan.FromHours(double)"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="TimeSpanToLongValue.Days"/>: <see cref="TimeSpan.FromDays(double)"/></description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new TimeSpanToLongConverter
	/// {
	///     Value = TimeSpanToLongValue.Seconds
	/// };
	/// 
	/// TimeSpan? time = converter.ConvertBack(125, CultureInfo.CurrentCulture);
	/// Console.WriteLine(time);  // Output: 00:02:05 (2 minutes, 5 seconds)
	/// 
	/// // Different units
	/// converter.Value = TimeSpanToLongValue.Hours;
	/// TimeSpan? hours = converter.ConvertBack(3, CultureInfo.CurrentCulture);
	/// Console.WriteLine(hours);  // Output: 03:00:00
	/// 
	/// converter.Value = TimeSpanToLongValue.Days;
	/// TimeSpan? days = converter.ConvertBack(2, CultureInfo.CurrentCulture);
	/// Console.WriteLine(days);  // Output: 2.00:00:00
	/// 
	/// // Null handling
	/// TimeSpan? nullTime = converter.ConvertBack(null, CultureInfo.CurrentCulture);
	/// Console.WriteLine(nullTime == null);  // Output: true
	/// </code>
	/// </example>
	public override TimeSpan? ConvertBack(long? result, CultureInfo culture)
	{
		if (result is null) return null;
		return Value switch
		{
			TimeSpanToLongValue.Ticks => TimeSpan.FromTicks(result.Value),
			TimeSpanToLongValue.Milliseconds => TimeSpan.FromMilliseconds(result.Value),
			TimeSpanToLongValue.Seconds => TimeSpan.FromSeconds(result.Value),
			TimeSpanToLongValue.Minutes => TimeSpan.FromMinutes(result.Value),
			TimeSpanToLongValue.Hours => TimeSpan.FromHours(result.Value),
			TimeSpanToLongValue.Days => TimeSpan.FromDays(result.Value),
			_ => throw new NotSupportedException("")
		};
	}
}