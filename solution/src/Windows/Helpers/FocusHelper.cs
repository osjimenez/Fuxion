using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Fuxion.Windows.Helpers;

/// <summary>
///    Provides attached properties and helper methods for managing keyboard focus in WPF applications.
/// </summary>
/// <remarks>
///    <para>
///       This static helper class provides functionality to automatically set keyboard focus on UI elements
///       when they are loaded. It's particularly useful for ensuring that specific controls receive focus
///       immediately when a window or user control is displayed, improving user experience by eliminating
///       the need for manual focus setting or mouse clicks.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Attached property:</strong> <see cref="SetKeyboardFocusOnLoadProperty"/> for declarative focus management in XAML
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic timing:</strong> Uses dispatcher to ensure focus is set at the right time during element loading
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Input priority:</strong> Executes focus operation at <see cref="DispatcherPriority.Input"/> for proper timing
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Simple usage:</strong> Just set attached property to <c>true</c> on any <see cref="UIElement"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Thread-safe:</strong> Uses dispatcher to ensure focus operation occurs on UI thread
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Set focus on first input field when window opens</description>
///       </item>
///       <item>
///          <description>Focus search box when navigation occurs</description>
///       </item>
///       <item>
///          <description>Automatically focus primary input in dialogs</description>
///       </item>
///       <item>
///          <description>Set focus on dynamically loaded user controls</description>
///       </item>
///       <item>
///          <description>Improve keyboard navigation and accessibility</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Set focus on TextBox when window loads:</strong>
///    <code><![CDATA[
/// <Window x:Class="MyApp.MainWindow"
///         xmlns:helpers="clr-namespace:Fuxion.Windows.Helpers">
///     <StackPanel>
///         <Label Content="Name:"/>
///         <TextBox helpers:FocusHelper.SetKeyboardFocusOnLoad="True"/>
///         
///         <Label Content="Email:"/>
///         <TextBox/>
///         
///         <Button Content="Submit"/>
///     </StackPanel>
/// </Window>
/// ]]></code>
///    <strong>Focus search box in navigation page:</strong>
///    <code><![CDATA[
/// <UserControl x:Class="MyApp.SearchView"
///              xmlns:helpers="clr-namespace:Fuxion.Windows.Helpers">
///     <Grid>
///         <TextBox x:Name="SearchBox"
///                  helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///                  Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}"/>
///         
///         <ListBox ItemsSource="{Binding SearchResults}"/>
///     </Grid>
/// </UserControl>
/// ]]></code>
///    <strong>Focus primary button in dialog:</strong>
///    <code><![CDATA[
/// <Window x:Class="MyApp.ConfirmDialog"
///         xmlns:helpers="clr-namespace:Fuxion.Windows.Helpers">
///     <StackPanel>
///         <TextBlock Text="Are you sure you want to continue?"/>
///         
///         <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
///             <Button Content="Yes" 
///                     helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///                     Command="{Binding ConfirmCommand}"
///                     Margin="5"/>
///             <Button Content="No" 
///                     Command="{Binding CancelCommand}"
///                     Margin="5"/>
///         </StackPanel>
///     </StackPanel>
/// </Window>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// // Set focus on element programmatically
/// TextBox textBox = new TextBox();
/// FocusHelper.SetSetKeyboardFocusOnLoad(textBox, true);
/// 
/// // Get current value
/// bool willFocusOnLoad = FocusHelper.GetSetKeyboardFocusOnLoad(textBox);
/// Console.WriteLine(willFocusOnLoad);  // Output: true
/// </code>
///    <strong>Login form with auto-focus:</strong>
///    <code><![CDATA[
/// <Window x:Class="MyApp.LoginWindow"
///         xmlns:helpers="clr-namespace:Fuxion.Windows.Helpers">
///     <StackPanel Margin="20">
///         <Label Content="Username:"/>
///         <TextBox x:Name="UsernameBox"
///                  helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///                  Text="{Binding Username}"/>
///         
///         <Label Content="Password:" Margin="0,10,0,0"/>
///         <PasswordBox x:Name="PasswordBox"/>
///         
///         <Button Content="Login" 
///                 Command="{Binding LoginCommand}"
///                 Margin="0,10,0,0"/>
///     </StackPanel>
/// </Window>
/// ]]></code>
///    <strong>Dynamic content with focus:</strong>
///    <code>
/// public class DynamicContentViewModel : INotifyPropertyChanged
/// {
///     private UserControl _currentView;
///     
///     public UserControl CurrentView
///     {
///         get => _currentView;
///         set
///         {
///             _currentView = value;
///             OnPropertyChanged();
///         }
///     }
///     
///     public void ShowSearchView()
///     {
///         // Create view with focused search box
///         CurrentView = new SearchView();
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- SearchView.xaml -->
/// <UserControl xmlns:helpers="clr-namespace:Fuxion.Windows.Helpers">
///     <TextBox helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///              Text="{Binding SearchQuery}"/>
/// </UserControl>
/// ]]></code>
///    <strong>TabControl with auto-focus on tab content:</strong>
///    <code><![CDATA[
/// <TabControl>
///     <TabItem Header="General">
///         <StackPanel>
///             <TextBox helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///                      Text="{Binding GeneralInfo}"/>
///         </StackPanel>
///     </TabItem>
///     
///     <TabItem Header="Details">
///         <StackPanel>
///             <TextBox helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///                      Text="{Binding Details}"/>
///         </StackPanel>
///     </TabItem>
/// </TabControl>
/// ]]></code>
///    <strong>Custom control with focus management:</strong>
///    <code>
/// public class FocusableTextBox : TextBox
/// {
///     public FocusableTextBox()
///     {
///         // Set focus when control is loaded
///         FocusHelper.SetSetKeyboardFocusOnLoad(this, true);
///     }
/// }
/// </code>
///    <strong>DataTemplate with focused element:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <DataTemplate x:Key="EditTemplate">
///         <StackPanel>
///             <TextBox helpers:FocusHelper.SetKeyboardFocusOnLoad="True"
///                      Text="{Binding Name}"/>
///             <TextBox Text="{Binding Description}"/>
///         </StackPanel>
///     </DataTemplate>
/// </Window.Resources>
/// 
/// <ContentControl Content="{Binding CurrentItem}"
///                 ContentTemplate="{StaticResource EditTemplate}"/>
/// ]]></code>
/// </example>
public static class FocusHelper
{
	/// <summary>
	///    Identifies the <c>SetKeyboardFocusOnLoad</c> attached dependency property.
	/// </summary>
	/// <remarks>
	///    <para>
	///       This attached property controls whether a <see cref="UIElement"/> should automatically
	///       receive keyboard focus when it is loaded. When set to <c>true</c>, the element will
	///       be focused using <see cref="Keyboard.Focus"/> via the dispatcher at
	///       <see cref="DispatcherPriority.Input"/> priority.
	///    </para>
	///    <para>
	///       The focus operation is deferred using <see cref="Dispatcher.BeginInvoke(Delegate, object[])"/> to ensure
	///       it occurs after the element is fully loaded and ready to receive input.
	///    </para>
	/// </remarks>
	public static readonly DependencyProperty SetKeyboardFocusOnLoadProperty =
		DependencyProperty.RegisterAttached("SetKeyboardFocusOnLoad", typeof(bool), typeof(FocusHelper), new(false, SetKeyboardFocusOnLoadChanged));

	/// <summary>
	///    Gets the value of the <see cref="SetKeyboardFocusOnLoadProperty"/> attached property from a given <see cref="DependencyObject"/>.
	/// </summary>
	/// <param name="obj">The <see cref="DependencyObject"/> from which to read the property value.</param>
	/// <returns>
	///    <c>true</c> if the element will receive keyboard focus when loaded; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	///    This method is the getter for the <c>SetKeyboardFocusOnLoad</c> attached property.
	/// </remarks>
	/// <example>
	///    <code>
	/// TextBox textBox = new TextBox();
	/// FocusHelper.SetSetKeyboardFocusOnLoad(textBox, true);
	/// 
	/// bool willFocus = FocusHelper.GetSetKeyboardFocusOnLoad(textBox);
	/// Console.WriteLine(willFocus);  // Output: true
	/// </code>
	/// </example>
	public static bool GetSetKeyboardFocusOnLoad(DependencyObject obj) => (bool)obj.GetValue(SetKeyboardFocusOnLoadProperty);

	/// <summary>
	///    Sets the value of the <see cref="SetKeyboardFocusOnLoadProperty"/> attached property on a given <see cref="DependencyObject"/>.
	/// </summary>
	/// <param name="obj">The <see cref="DependencyObject"/> on which to set the property value.</param>
	/// <param name="value">
	///    <c>true</c> to automatically set keyboard focus when the element loads; otherwise, <c>false</c>.
	/// </param>
	/// <remarks>
	///    <para>
	///       This method is the setter for the <c>SetKeyboardFocusOnLoad</c> attached property.
	///       When set to <c>true</c>, the element will automatically receive keyboard focus
	///       after it is loaded.
	///    </para>
	///    <para>
	///       Setting this to <c>true</c> triggers the <see cref="SetKeyboardFocusOnLoadChanged"/> callback,
	///       which schedules the focus operation on the dispatcher.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // In code-behind
	/// TextBox textBox = new TextBox();
	/// FocusHelper.SetSetKeyboardFocusOnLoad(textBox, true);
	/// 
	/// // In XAML
	/// // &lt;TextBox helpers:FocusHelper.SetKeyboardFocusOnLoad="True"/&gt;
	/// </code>
	/// </example>
	public static void SetSetKeyboardFocusOnLoad(DependencyObject obj, bool value) => obj.SetValue(SetKeyboardFocusOnLoadProperty, value);

	/// <summary>
	///    Called when the <see cref="SetKeyboardFocusOnLoadProperty"/> attached property value changes.
	/// </summary>
	/// <param name="sender">The <see cref="DependencyObject"/> whose property value changed.</param>
	/// <param name="e">Event data that contains the old and new property values.</param>
	/// <remarks>
	///    <para>
	///       When the property is set to <c>true</c> and the sender is a <see cref="UIElement"/>,
	///       this method schedules a focus operation using <see cref="Dispatcher.BeginInvoke(Delegate, object[])"/> with
	///       <see cref="DispatcherPriority.Input"/> priority.
	///    </para>
	///    <para>
	///       The deferred execution ensures that:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>The element is fully loaded and part of the visual tree</description>
	///       </item>
	///       <item>
	///          <description>The focus operation occurs at the appropriate time in the input processing</description>
	///       </item>
	///       <item>
	///          <description>Other initialization and loading operations have completed</description>
	///       </item>
	///    </list>
	///    <para>
	///       The method uses <see cref="ThreadStart"/> delegate to invoke <see cref="Keyboard.Focus"/>
	///       on the UI thread via the element's dispatcher.
	///    </para>
	/// </remarks>
	static void SetKeyboardFocusOnLoadChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if ((bool)e.NewValue)
			if (sender is UIElement ui)
				ui.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(delegate { Keyboard.Focus(ui); }));
	}
}