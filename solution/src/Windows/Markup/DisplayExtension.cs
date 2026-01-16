using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using Fuxion.Reflection;

namespace Fuxion.Windows.Markup;

/// <summary>
///    A XAML markup extension that extracts display information from <see cref="DisplayAttribute"/> decorating properties
///    and establishes data-binding to property changes.
/// </summary>
/// <remarks>
///    <para>
///       This markup extension provides a declarative way to bind to metadata from <see cref="DisplayAttribute"/> annotations
///       in your view models or data models. It supports binding to property chains (e.g., "Customer.Address.Street") and
///       automatically updates when properties implementing <see cref="INotifyPropertyChanged"/> change.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>DisplayAttribute support:</strong> Extracts Name, Description, GroupName, ShortName, Order, and Prompt
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Property chain binding:</strong> Supports nested properties (e.g., "Person.Address.City")
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Change notification:</strong> Automatically updates when property values change via <see cref="INotifyPropertyChanged"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Fallback values:</strong> Returns property name with optional prefix/suffix if no attribute found
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Mode selection:</strong> Specify which DisplayAttribute property to extract using inline syntax
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Syntax:</strong>
///    </para>
///    <code>
/// {fuxion:Display PropertyExpression}
/// {fuxion:Display [Mode]PropertyExpression}
/// </code>
///    <para>
///       Where Mode can be: Name (default), Description, GroupName, ShortName, Order, or Prompt.
///    </para>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display localized property labels from DisplayAttribute.Name</description>
///       </item>
///       <item>
///          <description>Show descriptions or tooltips from DisplayAttribute.Description</description>
///       </item>
///       <item>
///          <description>Group controls using DisplayAttribute.GroupName</description>
///       </item>
///       <item>
///          <description>Create dynamic forms that adapt to model metadata</description>
///       </item>
///       <item>
///          <description>Display watermark text from DisplayAttribute.Prompt</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage with DisplayAttribute:</strong>
///    <code>
/// public class PersonViewModel : INotifyPropertyChanged
/// {
///     [Display(Name = "Full Name", Description = "Enter the person's full name")]
///     public string Name { get; set; }
///     
///     [Display(Name = "Email Address", Prompt = "user@example.com")]
///     public string Email { get; set; }
///     
///     [Display(Name = "Birth Date", ShortName = "DOB")]
///     public DateTime DateOfBirth { get; set; }
/// }
/// </code>
///    <code><![CDATA[
/// <Window xmlns:fuxion="clr-namespace:Fuxion.Windows.Markup">
///     <StackPanel>
///         <!-- Display "Full Name" from DisplayAttribute.Name -->
///         <Label Content="{fuxion:Display Name}"/>
///         <TextBox Text="{Binding Name}"/>
///         
///         <!-- Display "Email Address" -->
///         <Label Content="{fuxion:Display Email}"/>
///         <TextBox Text="{Binding Email}"/>
///         
///         <!-- Display "Birth Date" -->
///         <Label Content="{fuxion:Display DateOfBirth}"/>
///         <DatePicker SelectedDate="{Binding DateOfBirth}"/>
///     </StackPanel>
/// </Window>
/// ]]></code>
///    <strong>Using different display modes:</strong>
///    <code><![CDATA[
/// <!-- Display Name (default) -->
/// <Label Content="{fuxion:Display Name}"/>
/// 
/// <!-- Display Description -->
/// <TextBlock Text="{fuxion:Display [Description]Name}" 
///            ToolTip="{fuxion:Display [Description]Name}"/>
/// 
/// <!-- Display ShortName -->
/// <Label Content="{fuxion:Display [ShortName]DateOfBirth}"/>
/// 
/// <!-- Display Prompt for watermark -->
/// <TextBox Text="{Binding Email}">
///     <TextBox.Tag>
///         <TextBlock Text="{fuxion:Display [Prompt]Email}" Foreground="Gray"/>
///     </TextBox.Tag>
/// </TextBox>
/// ]]></code>
///    <strong>Nested property chains:</strong>
///    <code>
/// public class OrderViewModel
/// {
///     public CustomerViewModel Customer { get; set; }
/// }
/// 
/// public class CustomerViewModel
/// {
///     public AddressViewModel Address { get; set; }
/// }
/// 
/// public class AddressViewModel
/// {
///     [Display(Name = "Street Address")]
///     public string Street { get; set; }
///     
///     [Display(Name = "City")]
///     public string City { get; set; }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- Bind to nested property -->
/// <Label Content="{fuxion:Display Customer.Address.Street}"/>
/// <TextBox Text="{Binding Customer.Address.Street}"/>
/// 
/// <Label Content="{fuxion:Display Customer.Address.City}"/>
/// <TextBox Text="{Binding Customer.Address.City}"/>
/// ]]></code>
///    <strong>Custom fallback prefix/suffix:</strong>
///    <code>
/// // In App.xaml.cs or application startup
/// DisplayExtension.NonAttrributePrefix = "[";
/// DisplayExtension.NonAttrributeSufix = "]";
/// 
/// // Now properties without DisplayAttribute will show as "[PropertyName]"
/// </code>
///    <strong>Dynamic forms from metadata:</strong>
///    <code><![CDATA[
/// <ItemsControl ItemsSource="{Binding EditableProperties}">
///     <ItemsControl.ItemTemplate>
///         <DataTemplate>
///             <StackPanel Margin="5">
///                 <!-- Label from Display.Name -->
///                 <Label Content="{fuxion:Display}"/>
///                 
///                 <!-- TextBox with tooltip from Display.Description -->
///                 <TextBox Text="{Binding}"
///                         ToolTip="{fuxion:Display [Description]}"/>
///             </StackPanel>
///         </DataTemplate>
///     </ItemsControl.ItemTemplate>
/// </ItemsControl>
/// ]]></code>
///    <strong>GroupName for sectioning:</strong>
///    <code>
/// public class SettingsViewModel
/// {
///     [Display(Name = "User Name", GroupName = "Account")]
///     public string UserName { get; set; }
///     
///     [Display(Name = "Email", GroupName = "Account")]
///     public string Email { get; set; }
///     
///     [Display(Name = "Theme", GroupName = "Appearance")]
///     public string Theme { get; set; }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- Display group name as section header -->
/// <TextBlock Text="{fuxion:Display [GroupName]UserName}" 
///            FontWeight="Bold" 
///            FontSize="16"/>
/// ]]></code>
/// </example>
public class DisplayExtension : MarkupExtension
{
	/// <summary>
	///    Initializes a new instance of the <see cref="DisplayExtension"/> class with a binding expression.
	/// </summary>
	/// <param name="bindExpression">
	///    The property binding expression, optionally prefixed with a display mode in square brackets.
	///    Format: "[Mode]Property.Path" or "Property.Path" (defaults to Name mode).
	/// </param>
	/// <exception cref="ArgumentException">
	///    Thrown when the mode specified in square brackets cannot be parsed as a valid <see cref="DisplayMode"/>.
	/// </exception>
	/// <remarks>
	///    <para>
	///       The <paramref name="bindExpression"/> can include an optional mode prefix:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><c>"PropertyName"</c> - Uses Name mode (default)</description>
	///       </item>
	///       <item>
	///          <description><c>"[Name]PropertyName"</c> - Explicitly uses Name mode</description>
	///       </item>
	///       <item>
	///          <description><c>"[Description]PropertyName"</c> - Uses Description mode</description>
	///       </item>
	///       <item>
	///          <description><c>"[GroupName]PropertyName"</c> - Uses GroupName mode</description>
	///       </item>
	///       <item>
	///          <description><c>"[ShortName]PropertyName"</c> - Uses ShortName mode</description>
	///       </item>
	///       <item>
	///          <description><c>"[Order]PropertyName"</c> - Uses Order mode</description>
	///       </item>
	///       <item>
	///          <description><c>"[Prompt]PropertyName"</c> - Uses Prompt mode</description>
	///       </item>
	///    </list>
	///    <para>
	///       The property path supports nested properties separated by dots (e.g., "Customer.Address.City").
	///       The extension creates a chain of <see cref="NotifierChainLink"/> objects to monitor changes
	///       at each level of the property path.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code><![CDATA[
	/// <!-- Default Name mode -->
	/// {fuxion:Display FirstName}
	/// 
	/// <!-- Explicit modes -->
	/// {fuxion:Display [Description]FirstName}
	/// {fuxion:Display [ShortName]DateOfBirth}
	/// {fuxion:Display [Prompt]Email}
	/// 
	/// <!-- Nested properties -->
	/// {fuxion:Display Customer.Address.City}
	/// {fuxion:Display [Description]Customer.Address.City}
	/// ]]></code>
	/// </example>
	public DisplayExtension(string bindExpression)
	{
		var mode = DisplayMode.Name;
		if (bindExpression.StartsWith("["))
		{
			var modeStr = bindExpression.Split(']')[0].Substring(1);
			if (!Enum.TryParse(modeStr, true, out mode))
				throw new ArgumentException($"El valor de fuxion:Display '{bindExpression}' no es válido. No se puede parsear '{modeStr}' como una propiedad de '{nameof(DisplayAttribute)}'.");
			bindExpression = bindExpression.Split(']')[1];
		}
		var pros = bindExpression.Split('.');
		for (var i = pros.Length - 1; i >= 0; i--)
			chain.Add(new(pros[i], pro => {
				var att = pro?.GetCustomAttribute<DisplayAttribute>(true, false);
				var nonAttributeValue = pro?.Name;
				if (nonAttributeValue != null) nonAttributeValue = NonAttrributePrefix + nonAttributeValue + NonAttrributeSufix;
				string? attRes = null;
				switch (mode)
				{
					case DisplayMode.Name:
						attRes = att?.GetName();
						break;
					case DisplayMode.Description:
						attRes = att?.GetDescription();
						break;
					case DisplayMode.GroupName:
						attRes = att?.GetGroupName();
						break;
					case DisplayMode.ShortName:
						attRes = att?.GetShortName();
						break;
					case DisplayMode.Order:
						attRes = att?.GetOrder()?.ToString();
						break;
					case DisplayMode.Prompt:
						attRes = att?.GetPrompt();
						break;
				}
				return attRes ?? nonAttributeValue;
			}) {
				NextLink = i == pros.Length - 1 ? null : chain.FirstOrDefault(l => l.PropertyName == pros[i + 1])
			});
		chain.Reverse();
		for (var i = 0; i < chain.Count; i++) chain[i].PreviousLink = i == 0 ? null : chain[i - 1];
	}

	/// <summary>
	///    Internal chain of notifier links for property path monitoring.
	/// </summary>
	internal List<NotifierChainLink> chain = new();

	/// <summary>
	///    Gets or sets the prefix to use when a property doesn't have a <see cref="DisplayAttribute"/>.
	/// </summary>
	/// <value>
	///    The prefix string. Default is empty string (<c>""</c>).
	/// </value>
	/// <remarks>
	///    When a property doesn't have a <see cref="DisplayAttribute"/>, the property name is used
	///    with this prefix and <see cref="NonAttrributeSufix"/>. For example, if prefix is "[" and
	///    suffix is "]", a property named "FirstName" without an attribute would display as "[FirstName]".
	/// </remarks>
	public static string NonAttrributePrefix { get; set; } = "";

	/// <summary>
	///    Gets or sets the suffix to use when a property doesn't have a <see cref="DisplayAttribute"/>.
	/// </summary>
	/// <value>
	///    The suffix string. Default is empty string (<c>""</c>).
	/// </value>
	/// <remarks>
	///    See <see cref="NonAttrributePrefix"/> for details on how this is used.
	/// </remarks>
	public static string NonAttrributeSufix { get; set; } = "";

	/// <summary>
	///    Returns an object that is set as the value of the target property for this markup extension.
	/// </summary>
	/// <param name="serviceProvider">
	///    A service provider helper that can provide services for the markup extension.
	/// </param>
	/// <returns>
	///    Always returns <c>null</c>. The actual value is set directly on the target property via
	///    the <see cref="NotifierChainLink"/> mechanism.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method sets up the binding infrastructure by:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Getting the target object and property from <see cref="IProvideValueTarget"/></description>
	///       </item>
	///       <item>
	///          <description>
	///             Hooking into <see cref="FrameworkElement.DataContextChanged"/> or
	///             <see cref="FrameworkContentElement.DataContextChanged"/> events
	///          </description>
	///       </item>
	///       <item>
	///          <description>Initializing the <see cref="NotifierChainLink"/> chain with the current DataContext</description>
	///       </item>
	///       <item>
	///          <description>Setting the initial value on the target property</description>
	///       </item>
	///    </list>
	///    <para>
	///       The return value is <c>null</c> because the value is set asynchronously when the DataContext
	///       becomes available and when it changes.
	///    </para>
	/// </remarks>
	public override object? ProvideValue(IServiceProvider serviceProvider)
	{
		if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget provider)
		{
			if (provider.TargetObject == null || provider.TargetProperty == null) return null;
			var first = chain.First();
			if (provider.TargetObject is FrameworkElement element)
			{
				first.TargetObject = element;
				element.DataContextChanged += (s, e) => {
					first.DataContext = e.NewValue;
					first.SetValue();
				};
				first.DataContext = element.DataContext;
			} else if (provider.TargetObject is FrameworkContentElement contentElement)
			{
				first.TargetObject = contentElement;
				contentElement.DataContextChanged += (s, e) => {
					first.DataContext = e.NewValue;
					first.SetValue();
				};
				first.DataContext = contentElement.DataContext;
			}
			first.TargetDependencyProperty = provider.TargetProperty as DependencyProperty;
			first.SetValue();
		}
		return null;
	}
}

/// <summary>
///    Specifies which property of <see cref="DisplayAttribute"/> to extract.
/// </summary>
enum DisplayMode
{
	/// <summary>
	///    Extract the <see cref="DisplayAttribute.Name"/> property.
	/// </summary>
	Name,

	/// <summary>
	///    Extract the <see cref="DisplayAttribute.Description"/> property.
	/// </summary>
	Description,

	/// <summary>
	///    Extract the <see cref="DisplayAttribute.GroupName"/> property.
	/// </summary>
	GroupName,

	/// <summary>
	///    Extract the <see cref="DisplayAttribute.ShortName"/> property.
	/// </summary>
	ShortName,

	/// <summary>
	///    Extract the <see cref="DisplayAttribute.Order"/> property.
	/// </summary>
	Order,

	/// <summary>
	///    Extract the <see cref="DisplayAttribute.Prompt"/> property.
	/// </summary>
	Prompt
}

/// <summary>
///    Represents a link in the property chain that monitors property changes and updates target properties.
/// </summary>
/// <remarks>
///    <para>
///       This class is the core mechanism for <see cref="DisplayExtension"/>'s reactive binding system.
///       It creates a chain of links, one for each property in a property path (e.g., "Customer.Address.City"
///       has three links). Each link:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Monitors its property for changes via <see cref="INotifyPropertyChanged"/></description>
///       </item>
///       <item>
///          <description>Updates the next link's context when its value changes</description>
///       </item>
///       <item>
///          <description>Propagates value updates through the chain to the final target property</description>
///       </item>
///    </list>
/// </remarks>
class NotifierChainLink
{
	/// <summary>
	///    Initializes a new instance of the <see cref="NotifierChainLink"/> class.
	/// </summary>
	/// <param name="propertyName">The name of the property this link represents.</param>
	/// <param name="getValueFunction">
	///    Function to extract the display value from the property's <see cref="PropertyInfo"/>.
	/// </param>
	public NotifierChainLink(string propertyName, Func<PropertyInfo?, string?> getValueFunction)
	{
		this.getValueFunction = getValueFunction;
		PropertyName = propertyName;
		EventHandler = PropertyChanged;
	}
	readonly Func<PropertyInfo?, string?> getValueFunction;
	object? _DataContext;
	DependencyProperty? _TargetDependencyProperty;
	object? _TargetElement;

	/// <summary>
	///    Gets or sets the next link in the property chain.
	/// </summary>
	public NotifierChainLink? NextLink { get; set; }

	/// <summary>
	///    Gets or sets the previous link in the property chain.
	/// </summary>
	public NotifierChainLink? PreviousLink { get; set; }

	/// <summary>
	///    Gets the name of the property this link represents.
	/// </summary>
	public string PropertyName { get; set; }

	/// <summary>
	///    Gets the event handler for <see cref="INotifyPropertyChanged.PropertyChanged"/> events.
	/// </summary>
	public PropertyChangedEventHandler EventHandler { get; set; }

	/// <summary>
	///    Gets the <see cref="INotifyPropertyChanged"/> implementation of the current context, if available.
	/// </summary>
	public INotifyPropertyChanged? ContextNotifier => DataContext != null ? typeof(INotifyPropertyChanged).IsAssignableFrom(DataContext.GetType()) ? (INotifyPropertyChanged?)Context : null : null;

	/// <summary>
	///    Gets or sets the data context for this link. When set, subscribes to <see cref="INotifyPropertyChanged"/>.
	/// </summary>
	public object? DataContext
	{
		get => _DataContext;
		set
		{
			_DataContext = value;
			if (ContextNotifier != null) ContextNotifier.PropertyChanged += EventHandler;
		}
	}

	/// <summary>
	///    Gets the actual context object (DataContext or value from previous link's property).
	/// </summary>
	object? Context => DataContext ?? (PreviousLink?.Context != null ? PreviousLink?.ContextProperty?.GetValue(PreviousLink.Context) : null);

	/// <summary>
	///    Gets the runtime type of the context.
	/// </summary>
	Type? ContextType => Context?.GetType();

	/// <summary>
	///    Gets the <see cref="PropertyInfo"/> for this link's property on the context type.
	/// </summary>
	PropertyInfo? ContextProperty => ContextType?.GetProperty(PropertyName);

	/// <summary>
	///    Gets the <see cref="DisplayAttribute"/> decorating the context property, if any.
	/// </summary>
	DisplayAttribute? ContextAttribute => ContextProperty?.GetCustomAttribute<DisplayAttribute>(true, false);

	/// <summary>
	///    Gets or sets the target dependency property where the final value will be set.
	/// </summary>
	public DependencyProperty? TargetDependencyProperty
	{
		get => _TargetDependencyProperty ?? PreviousLink?.TargetDependencyProperty;
		set => _TargetDependencyProperty = value;
	}

	/// <summary>
	///    Gets or sets the target object where the final value will be set.
	/// </summary>
	public object? TargetObject
	{
		get => _TargetElement ?? PreviousLink?.TargetObject;
		set => _TargetElement = value;
	}

	/// <summary>
	///    Gets the target property info based on the dependency property name.
	/// </summary>
	public PropertyInfo? TargetProperty => TargetDependencyProperty?.Name != null ? TargetObject?.GetType().GetProperty(TargetDependencyProperty.Name) : null;

	/// <summary>
	///    Handles <see cref="INotifyPropertyChanged.PropertyChanged"/> events to update the chain.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">Event data.</param>
	void PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (DataContext != null)
			if (NextLink != null)
				NextLink.DataContext = ContextProperty?.GetValue(DataContext);
		SetValue();
	}

	/// <summary>
	///    Sets the value on the target property, or propagates to the next link in the chain.
	/// </summary>
	/// <remarks>
	///    If this is the last link in the chain, sets the value on the target property using
	///    the <see cref="getValueFunction"/>. Otherwise, delegates to the next link.
	/// </remarks>
	public void SetValue()
	{
		if (NextLink == null)
		{
			if (TargetObject != null) TargetProperty?.SetValue(TargetObject, getValueFunction(ContextProperty));
		} else
			NextLink.SetValue();
	}

	/// <summary>
	///    Returns a string representation of this link (the property name).
	/// </summary>
	public override string? ToString() => PropertyName;
}