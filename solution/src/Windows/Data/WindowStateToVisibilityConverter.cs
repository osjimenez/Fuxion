using System;
using System.Globalization;
using System.Windows;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that converts <see cref="WindowState"/> values to <see cref="Visibility"/> values.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide window state-based
///       visibility control. It's particularly useful for controlling UI element visibility based on the window's
///       current state (Maximized, Minimized, or Normal), allowing you to create adaptive UIs that respond to
///       window state changes.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Three-state mapping:</strong> Independent visibility configuration for Maximized, Minimized, and Normal states
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Default configuration:</strong> Maximized and Normal are Visible, Minimized is Collapsed by default
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Window integration:</strong> Easily binds to <see cref="Window.WindowState"/> property
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>One-way conversion:</strong> Optimized for display purposes (no ConvertBack implementation)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Adaptive UI:</strong> Create responsive interfaces that adapt to window state changes
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Default behavior:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Window State</term>
///          <description>Default Visibility</description>
///       </listheader>
///       <item>
///          <term><see cref="WindowState.Maximized"/></term>
///          <description><see cref="Visibility.Visible"/></description>
///       </item>
///       <item>
///          <term><see cref="WindowState.Minimized"/></term>
///          <description><see cref="Visibility.Collapsed"/></description>
///       </item>
///       <item>
///          <term><see cref="WindowState.Normal"/></term>
///          <description><see cref="Visibility.Visible"/></description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Hide certain UI elements when window is minimized</description>
///       </item>
///       <item>
///          <description>Show different controls based on window state (e.g., maximize/restore buttons)</description>
///       </item>
///       <item>
///          <description>Adjust layouts dynamically when window is maximized or restored</description>
///       </item>
///       <item>
///          <description>Display window state indicators or badges</description>
///       </item>
///       <item>
///          <description>Create custom window chrome with state-aware controls</description>
///       </item>
///    </list>
///    <para>
///       <strong>Default behavior:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>Window State</term>
///          <description>Default Visibility</description>
///       </listheader>
///       <item>
///          <term><see cref="WindowState.Maximized"/></term>
///          <description><see cref="Visibility.Visible"/></description>
///       </item>
///       <item>
///          <term><see cref="WindowState.Minimized"/></term>
///          <description><see cref="Visibility.Collapsed"/></description>
///       </item>
///       <item>
///          <term><see cref="WindowState.Normal"/></term>
///          <description><see cref="Visibility.Visible"/></description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Hide certain UI elements when window is minimized</description>
///       </item>
///       <item>
///          <description>Show different controls based on window state (e.g., maximize/restore buttons)</description>
///       </item>
///       <item>
///          <description>Adjust layouts dynamically when window is maximized or restored</description>
///       </item>
///       <item>
///          <description>Display window state indicators or badges</description>
///       </item>
///       <item>
///          <description>Create custom window chrome with state-aware controls</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Hide element when window is minimized:</strong>
///    <code><![CDATA[
/// <Window x:Class="MyApp.MainWindow"
///         WindowState="{Binding CurrentWindowState, Mode=TwoWay}">
///     <Window.Resources>
///         <data:WindowStateToVisibilityConverter x:Key="WindowStateConverter"/>
///     </Window.Resources>
///     
///     <!-- This panel will be collapsed when window is minimized -->
///     <StackPanel Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                                     Path=WindowState, 
///                                     Converter={StaticResource WindowStateConverter}}">
///         <TextBlock Text="Content visible when not minimized"/>
///     </StackPanel>
/// </Window>
/// ]]></code>
///    <strong>Custom visibility for each window state:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <!-- Show only when maximized -->
///     <data:WindowStateToVisibilityConverter x:Key="MaximizedOnlyConverter"
///                                            MaximizedValue="Visible"
///                                            MinimizedValue="Collapsed"
///                                            NormalValue="Collapsed"/>
///     
///     <!-- Show only when normal (not maximized or minimized) -->
///     <data:WindowStateToVisibilityConverter x:Key="NormalOnlyConverter"
///                                            MaximizedValue="Collapsed"
///                                            MinimizedValue="Collapsed"
///                                            NormalValue="Visible"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Visible only when maximized -->
///     <TextBlock Text="Window is maximized" 
///                Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                                    Path=WindowState, 
///                                    Converter={StaticResource MaximizedOnlyConverter}}"/>
///     
///     <!-- Visible only when normal -->
///     <TextBlock Text="Window is in normal state" 
///                Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                                    Path=WindowState, 
///                                    Converter={StaticResource NormalOnlyConverter}}"/>
/// </Grid>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new WindowStateToVisibilityConverter();
/// 
/// // Default behavior
/// Visibility result1 = converter.Convert(WindowState.Maximized, CultureInfo.CurrentCulture);
/// Console.WriteLine(result1);  // Output: Visible
/// 
/// Visibility result2 = converter.Convert(WindowState.Minimized, CultureInfo.CurrentCulture);
/// Console.WriteLine(result2);  // Output: Collapsed
/// 
/// Visibility result3 = converter.Convert(WindowState.Normal, CultureInfo.CurrentCulture);
/// Console.WriteLine(result3);  // Output: Visible
/// 
/// // Custom configuration
/// converter.MaximizedValue = Visibility.Collapsed;
/// converter.NormalValue = Visibility.Visible;
/// converter.MinimizedValue = Visibility.Hidden;
/// 
/// Visibility result4 = converter.Convert(WindowState.Maximized, CultureInfo.CurrentCulture);
/// Console.WriteLine(result4);  // Output: Collapsed
/// </code>
///    <strong>Custom window chrome with maximize/restore button:</strong>
///    <code>
/// public class MainViewModel : INotifyPropertyChanged
/// {
///     private WindowState _windowState = WindowState.Normal;
///     
///     public WindowState WindowState
///     {
///         get => _windowState;
///         set
///         {
///             _windowState = value;
///             OnPropertyChanged();
///         }
///     }
///     
///     public ICommand MaximizeCommand { get; }
///     public ICommand RestoreCommand { get; }
///     
///     public MainViewModel()
///     {
///         MaximizeCommand = new RelayCommand(() => WindowState = WindowState.Maximized);
///         RestoreCommand = new RelayCommand(() => WindowState = WindowState.Normal);
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Window WindowStyle="None"
///         WindowState="{Binding WindowState, Mode=TwoWay}">
///     <Window.Resources>
///         <!-- Show maximize button when not maximized -->
///         <data:WindowStateToVisibilityConverter x:Key="ShowMaximizeConverter"
///                                                MaximizedValue="Collapsed"
///                                                MinimizedValue="Visible"
///                                                NormalValue="Visible"/>
///         
///         <!-- Show restore button only when maximized -->
///         <data:WindowStateToVisibilityConverter x:Key="ShowRestoreConverter"
///                                                MaximizedValue="Visible"
///                                                MinimizedValue="Collapsed"
///                                                NormalValue="Collapsed"/>
///     </Window.Resources>
///     
///     <Grid>
///         <Grid.RowDefinitions>
///             <RowDefinition Height="30"/> <!-- Title bar -->
///             <RowDefinition Height="*"/>
///         </Grid.RowDefinitions>
///         
///         <!-- Custom title bar -->
///         <Border Grid.Row="0" Background="#2D2D30">
///             <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
///                 <!-- Maximize button -->
///                 <Button Content="🗖" 
///                         Command="{Binding MaximizeCommand}"
///                         Visibility="{Binding WindowState, 
///                                             Converter={StaticResource ShowMaximizeConverter}}"/>
///                 
///                 <!-- Restore button -->
///                 <Button Content="🗗" 
///                         Command="{Binding RestoreCommand}"
///                         Visibility="{Binding WindowState, 
///                                             Converter={StaticResource ShowRestoreConverter}}"/>
///                 
///                 <!-- Close button (always visible) -->
///                 <Button Content="✕" Command="{Binding CloseCommand}"/>
///             </StackPanel>
///         </Border>
///         
///         <!-- Main content -->
///         <ContentControl Grid.Row="1" Content="{Binding MainContent}"/>
///     </Grid>
/// </Window>
/// ]]></code>
///    <strong>Window state indicator badge:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:WindowStateToVisibilityConverter x:Key="MaximizedIndicator"
///                                            MaximizedValue="Visible"
///                                            MinimizedValue="Collapsed"
///                                            NormalValue="Collapsed"/>
///     
///     <data:WindowStateToVisibilityConverter x:Key="MinimizedIndicator"
///                                            MaximizedValue="Collapsed"
///                                            MinimizedValue="Visible"
///                                            NormalValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <StatusBar>
///     <StatusBarItem>
///         <StackPanel Orientation="Horizontal">
///             <TextBlock Text="Window State: "/>
///             
///             <!-- Maximized badge -->
///             <Border Background="Green" 
///                     Padding="5,2"
///                     CornerRadius="3"
///                     Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                                         Path=WindowState, 
///                                         Converter={StaticResource MaximizedIndicator}}">
///                 <TextBlock Text="Maximized" Foreground="White"/>
///             </Border>
///             
///             <!-- Minimized badge -->
///             <Border Background="Orange" 
///                     Padding="5,2"
///                     CornerRadius="3"
///                     Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                                         Path=WindowState, 
///                                         Converter={StaticResource MinimizedIndicator}}">
///                 <TextBlock Text="Minimized" Foreground="White"/>
///             </Border>
///         </StackPanel>
///     </StatusBarItem>
/// </StatusBar>
/// ]]></code>
///    <strong>Adaptive layout based on window state:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:WindowStateToVisibilityConverter x:Key="CompactLayoutConverter"
///                                            MaximizedValue="Collapsed"
///                                            MinimizedValue="Collapsed"
///                                            NormalValue="Visible"/>
///     
///     <data:WindowStateToVisibilityConverter x:Key="ExpandedLayoutConverter"
///                                            MaximizedValue="Visible"
///                                            MinimizedValue="Collapsed"
///                                            NormalValue="Collapsed"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <!-- Compact layout for normal window -->
///     <StackPanel Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                                     Path=WindowState, 
///                                     Converter={StaticResource CompactLayoutConverter}}">
///         <TextBlock Text="Compact View"/>
///         <ListBox ItemsSource="{Binding Items}" MaxHeight="200"/>
///     </StackPanel>
///     
///     <!-- Expanded layout for maximized window -->
///     <Grid Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                               Path=WindowState, 
///                               Converter={StaticResource ExpandedLayoutConverter}}">
///         <Grid.ColumnDefinitions>
///             <ColumnDefinition Width="*"/>
///             <ColumnDefinition Width="2*"/>
///         </Grid.ColumnDefinitions>
///         
///         <TextBlock Grid.Column="0" Text="Expanded View - Sidebar"/>
///         <ListBox Grid.Column="1" ItemsSource="{Binding Items}"/>
///     </Grid>
/// </Grid>
/// ]]></code>
///    <strong>Hide taskbar preview content when minimized:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:WindowStateToVisibilityConverter x:Key="HideWhenMinimized"/>
/// </Window.Resources>
/// 
/// <!-- Sensitive content hidden when minimized -->
/// <Border Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                             Path=WindowState, 
///                             Converter={StaticResource HideWhenMinimized}}">
///     <StackPanel>
///         <TextBlock Text="Sensitive Data"/>
///         <TextBlock Text="{Binding CreditCardNumber}"/>
///     </StackPanel>
/// </Border>
/// 
/// <!-- Placeholder shown when minimized -->
/// <Border Background="Gray"
///         Visibility="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
///                             Path=WindowState, 
///                             Converter={StaticResource HideWhenMinimized}}">
///     <TextBlock Text="Content hidden for privacy" 
///                HorizontalAlignment="Center"
///                VerticalAlignment="Center"/>
/// </Border>
/// ]]></code>
/// </example>
public class WindowStateToVisibilityConverter : GenericConverter<WindowState, Visibility>
{
	/// <summary>
	///    Initializes a new instance of the <see cref="WindowStateToVisibilityConverter"/> class with default values.
	/// </summary>
	/// <remarks>
	///    Default values:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="MaximizedValue"/> = <see cref="Visibility.Visible"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="MinimizedValue"/> = <see cref="Visibility.Collapsed"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="NormalValue"/> = <see cref="Visibility.Visible"/></description>
	///       </item>
	///    </list>
	/// </remarks>
	public WindowStateToVisibilityConverter()
	{
		MaximizedValue = Visibility.Visible;
		MinimizedValue = Visibility.Collapsed;
		NormalValue = Visibility.Visible;
	}

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the window is in <see cref="WindowState.Maximized"/> state.
	/// </summary>
	/// <value>
	///    The visibility state for maximized windows. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Visible"/> to show elements when maximized,
	///    or <see cref="Visibility.Collapsed"/> to hide elements in maximized mode.
	/// </remarks>
	public Visibility MaximizedValue { get; set; }

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the window is in <see cref="WindowState.Minimized"/> state.
	/// </summary>
	/// <value>
	///    The visibility state for minimized windows. Default is <see cref="Visibility.Collapsed"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       Typically set to <see cref="Visibility.Collapsed"/> to hide elements when the window is minimized,
	///       reducing resource usage and protecting sensitive content from taskbar previews.
	///    </para>
	///    <para>
	///       Can be set to <see cref="Visibility.Visible"/> if elements should remain visible even when minimized,
	///       or <see cref="Visibility.Hidden"/> to preserve layout space.
	///    </para>
	/// </remarks>
	public Visibility MinimizedValue { get; set; }

	/// <summary>
	///    Gets or sets the <see cref="Visibility"/> value to return when the window is in <see cref="WindowState.Normal"/> state.
	/// </summary>
	/// <value>
	///    The visibility state for normal windows. Default is <see cref="Visibility.Visible"/>.
	/// </value>
	/// <remarks>
	///    Common configurations include <see cref="Visibility.Visible"/> for standard visibility,
	///    or <see cref="Visibility.Collapsed"/> to hide elements in normal (non-maximized) window state.
	/// </remarks>
	public Visibility NormalValue { get; set; }

	/// <summary>
	///    Converts a <see cref="WindowState"/> value to a <see cref="Visibility"/> value.
	/// </summary>
	/// <param name="source">The window state to convert.</param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    One of the following <see cref="Visibility"/> values based on the window state:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="MaximizedValue"/> if <paramref name="source"/> is <see cref="WindowState.Maximized"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="MinimizedValue"/> if <paramref name="source"/> is <see cref="WindowState.Minimized"/></description>
	///       </item>
	///       <item>
	///          <description><see cref="NormalValue"/> if <paramref name="source"/> is <see cref="WindowState.Normal"/></description>
	///       </item>
	///    </list>
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown if <paramref name="source"/> is not one of the recognized <see cref="WindowState"/> values.
	/// </exception>
	/// <remarks>
	///    The conversion uses a switch statement to map each window state to its corresponding visibility value.
	///    Any unrecognized window state will result in a <see cref="NotSupportedException"/>.
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new WindowStateToVisibilityConverter();
	/// 
	/// // Default behavior
	/// Console.WriteLine(converter.Convert(WindowState.Maximized, CultureInfo.CurrentCulture));  // Visible
	/// Console.WriteLine(converter.Convert(WindowState.Minimized, CultureInfo.CurrentCulture));  // Collapsed
	/// Console.WriteLine(converter.Convert(WindowState.Normal, CultureInfo.CurrentCulture));     // Visible
	/// 
	/// // Custom configuration
	/// converter.MaximizedValue = Visibility.Collapsed;
	/// converter.MinimizedValue = Visibility.Hidden;
	/// converter.NormalValue = Visibility.Visible;
	/// 
	/// Console.WriteLine(converter.Convert(WindowState.Maximized, CultureInfo.CurrentCulture));  // Collapsed
	/// Console.WriteLine(converter.Convert(WindowState.Minimized, CultureInfo.CurrentCulture));  // Hidden
	/// Console.WriteLine(converter.Convert(WindowState.Normal, CultureInfo.CurrentCulture));     // Visible
	/// </code>
	/// </example>
	public override Visibility Convert(WindowState source, CultureInfo culture)
	{
		switch (source)
		{
			case WindowState.Maximized: return MaximizedValue;
			case WindowState.Minimized: return MinimizedValue;
			case WindowState.Normal:    return NormalValue;
			default:                    throw new NotSupportedException($"The value '{source}' is not supported");
		}
	}
}