using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Fuxion.Windows.Resources;

namespace Fuxion.Windows.Documents;

/// <summary>
///    Provides extension methods for converting objects to WPF <see cref="FlowDocument"/> blocks with interactive collapsible sections.
/// </summary>
/// <remarks>
///    <para>
///       This class provides functionality to convert any .NET object into a hierarchical, interactive
///       representation using WPF <see cref="Block"/> elements. It's particularly useful for creating
///       debug views, object inspectors, or detailed data displays with expandable/collapsible sections.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic object introspection:</strong> Uses reflection to discover object properties
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Hierarchical display:</strong> Creates nested collapsible sections for complex objects
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Interactive UI:</strong> Toggle buttons allow expanding/collapsing object properties
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type-aware formatting:</strong> Different display for basic types vs complex objects
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Collection support:</strong> Special handling for IEnumerable with item count display
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Error handling:</strong> Gracefully handles exceptions during property expansion
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Basic types:</strong> The following types are considered basic and displayed inline:
///       short, ushort, int, uint, long, ulong, float, double, decimal, bool, char, string, byte, sbyte,
///       DateTime, TimeSpan, Guid.
///    </para>
/// </remarks>
/// <example>
///    <strong>Display object in FlowDocumentScrollViewer:</strong>
///    <code>
/// public class Person
/// {
///     public string Name { get; set; }
///     public int Age { get; set; }
///     public Address Address { get; set; }
/// }
/// 
/// public class Address
/// {
///     public string Street { get; set; }
///     public string City { get; set; }
/// }
/// </code>
///    <code><![CDATA[
/// var person = new Person
/// {
///     Name = "John Doe",
///     Age = 30,
///     Address = new Address
///     {
///         Street = "123 Main St",
///         City = "Springfield"
///     }
/// };
/// 
/// var flowDocument = new FlowDocument();
/// foreach (var block in person.ToBlocks())
/// {
///     flowDocument.Blocks.Add(block);
/// }
/// 
/// var viewer = new FlowDocumentScrollViewer { Document = flowDocument };
/// // Display viewer in window
/// ]]></code>
///    <strong>Usage in XAML with data binding:</strong>
///    <code><![CDATA[
/// <Window x:Class="MyApp.DebugWindow">
///     <FlowDocumentScrollViewer x:Name="DocumentViewer"/>
/// </Window>
/// ]]></code>
///    <code>
/// // In code-behind
/// public void ShowObject(object obj)
/// {
///     var document = new FlowDocument();
///     foreach (var block in obj.ToBlocks())
///     {
///         document.Blocks.Add(block);
///     }
///     DocumentViewer.Document = document;
/// }
/// </code>
/// </example>
public static class FlowDocumentExtensions
{
	/// <summary>
	///    Determines if a type is considered a basic (primitive or simple) type for display purposes.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>
	///    <c>true</c> if the type is a basic type that should be displayed inline;
	///    otherwise, <c>false</c> for complex types that should be displayed in collapsible sections.
	/// </returns>
	/// <remarks>
	///    <para>
	///       Basic types include:
	///    </para>
	///    <list type="bullet">
	///       <item><description>Numeric types: short, ushort, int, uint, long, ulong, float, double, decimal</description></item>
	///       <item><description>Boolean: bool</description></item>
	///       <item><description>Character types: char, string</description></item>
	///       <item><description>Byte types: byte, sbyte</description></item>
	///       <item><description>Date/Time types: DateTime, TimeSpan</description></item>
	///       <item><description>Other: Guid</description></item>
	///    </list>
	/// </remarks>
	static bool IsBasicType(Type type) =>
		type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double)
		|| type == typeof(decimal) || type == typeof(bool) || type == typeof(char) || type == typeof(string) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(DateTime)
		|| type == typeof(TimeSpan) || type == typeof(Guid);

	/// <summary>
	///    Converts an object to a collection of <see cref="Block"/> elements representing its properties.
	/// </summary>
	/// <param name="obj">The object to convert. Cannot be <c>null</c>.</param>
	/// <returns>
	///    An enumerable collection of <see cref="Block"/> elements, one for each property of the object.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	///    Thrown when <paramref name="obj"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method uses reflection to discover all readable, non-indexed properties of the object.
	///       Properties are displayed in alphabetical order by name.
	///    </para>
	///    <para>
	///       Each property is converted to a <see cref="Block"/> using <see cref="ProcessProperty"/>:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             Basic types are displayed as inline paragraphs with the property name and value
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Complex types are displayed as collapsible sections that can be expanded to show nested properties
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var person = new Person
	/// {
	///     Name = "Alice",
	///     Age = 25,
	///     IsActive = true
	/// };
	/// 
	/// var flowDocument = new FlowDocument();
	/// foreach (var block in person.ToBlocks())
	/// {
	///     flowDocument.Blocks.Add(block);
	/// }
	/// 
	/// // Document will contain:
	/// // ● Age = 25
	/// // ● IsActive = True
	/// // ● Name = Alice
	/// </code>
	/// </example>
	public static IEnumerable<Block> ToBlocks(this object? obj)
	{
		if (obj == null) throw new ArgumentNullException(nameof(obj));
		foreach (var pro in obj.GetType().GetProperties().Where(p => !p.GetIndexParameters().Any()).OrderBy(p => p.Name)) yield return ProcessProperty(pro.GetValue(obj), pro.Name, pro.PropertyType);
	}

	/// <summary>
	///    Processes a property value and converts it to an appropriate <see cref="Block"/> representation.
	/// </summary>
	/// <param name="obj">The property value to process. Can be <c>null</c>.</param>
	/// <param name="name">The name of the property.</param>
	/// <param name="type">The declared type of the property.</param>
	/// <returns>
	///    A <see cref="Block"/> element: either a <see cref="Paragraph"/> for basic types or a
	///    <see cref="CollapsibleSection"/> for complex types.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The method determines the actual runtime type of the object (or uses the declared type if null)
	///       and creates an appropriate block representation:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <strong>Basic types:</strong> Creates a <see cref="Paragraph"/> with a bullet (●), property name, and value.
	///             If the value contains line breaks (\r), adds a <see cref="LineBreak"/> before the value.
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Complex types:</strong> Creates a <see cref="CollapsibleSection"/> with an expandable toggle button
	///             and nested property display.
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	internal static Block ProcessProperty(this object? obj, string name, Type type)
	{
		var res = new List<Block>();
		var objType = obj?.GetType() ?? type;
		return IsBasicType(objType)
			? new Paragraph().Tap(p => {
				//p.Inlines.Add(new Bold(new Run("●")));
				p.Inlines.Add(new Bold(new Run($"  ●  {name} = ")));
				var valStr = obj?.ToString();
				if (valStr?.Contains('\r') ?? false) p.Inlines.Add(new LineBreak());
				p.Inlines.Add(new Run(obj?.ToString() ?? "null"));
			})
			: new CollapsibleSection(obj, name, type, false);
	}
}

/// <summary>
///    Represents a collapsible section in a <see cref="FlowDocument"/> that displays object properties with expand/collapse functionality.
/// </summary>
/// <remarks>
///    <para>
///       This class extends <see cref="Section"/> to create interactive, hierarchical displays of object data.
///       Each section has a toggle button (▼/▲) that allows users to expand or collapse the property details.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Toggle button:</strong> Visual indicator (▼/▲/◊) showing section state (collapsed/expanded/null)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Lazy loading:</strong> Property values are only inspected when the section is expanded
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Collection support:</strong> Displays element count and can expand to show individual items
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Error handling:</strong> Catches and displays exceptions during property expansion
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Shows "null" for null properties and disables interaction
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Visual indicators:</strong>
///    </para>
///    <list type="bullet">
///       <item><description>▼ - Section is collapsed (can be expanded)</description></item>
///       <item><description>▲ - Section is expanded (can be collapsed)</description></item>
///       <item><description>◊ - Property is null (no interaction)</description></item>
///    </list>
/// </remarks>
class CollapsibleSection : Section
{
	/// <summary>
	///    Initializes a new instance of the <see cref="CollapsibleSection"/> class.
	/// </summary>
	/// <param name="obj">The object to display. Can be <c>null</c>.</param>
	/// <param name="name">The name/label for this section.</param>
	/// <param name="type">The declared type of the object.</param>
	/// <param name="expandEnumerable">
	///    <c>true</c> to expand enumerable items individually; <c>false</c> to show properties and a nested enumerable section.
	/// </param>
	/// <remarks>
	///    <para>
	///       The constructor creates a collapsible section with a toggle button and header. The section's
	///       behavior depends on the object type and the <paramref name="expandEnumerable"/> parameter:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <strong>Regular objects:</strong> Displays properties in a nested list when expanded
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Enumerables (expandEnumerable=false):</strong> Shows properties plus a nested section for items
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Enumerables (expandEnumerable=true):</strong> Displays each item with an index
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Null objects:</strong> Shows "null" and disables the toggle button
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       For enumerable objects, the header displays the element count (e.g., "[5 Elements]")
	///    </para>
	/// </remarks>
	public CollapsibleSection(object? obj, string name, Type type, bool expandEnumerable)
	{
		var objType = obj?.GetType() ?? type;
		var isEnumerable = obj is IEnumerable || typeof(IEnumerable).IsAssignableFrom(objType);
		var enumerableCount = 0;
		if (isEnumerable)
			foreach (var item in (obj as IEnumerable)!)
				enumerableCount++;
		var button = new ToggleButton {
			Margin = new(1, 0, 1, 0)
		};
		button.Click += (s, e) => {
			try
			{
				var but = (s as ToggleButton)!;
				if (but.IsChecked ?? false)
				{
					button.Content = "▲";
					if (expandEnumerable)
					{
						var counter = 0;
						foreach (var item in (obj as IEnumerable)!)
						{
							var list = new List {
								MarkerStyle = TextMarkerStyle.None
							};
							if (item != null) list.ListItems.Add(new ListItem().Tap(i => i.Blocks.Add(item.ProcessProperty($"{name} [{counter++}]", item.GetType()))));
							Blocks.Add(list);
						}
					} else
					{
						var list = new List {
							MarkerStyle = TextMarkerStyle.None
						};
						if (obj != null)
							foreach (var pro in obj.GetType().GetProperties().Where(p => !p.GetIndexParameters().Any()).OrderBy(p => p.Name))
								try
								{
									list.ListItems.Add(new ListItem().Tap(item => item.Blocks.Add(pro.GetValue(obj).ProcessProperty(pro.Name, pro.PropertyType))));
								} catch (Exception ex)
								{
									list.ListItems.Add(new ListItem().Tap(item => item.Blocks.Add(new Paragraph().Tap(p =>
										p.Inlines.Add(new Bold(new Run(Strings.ErrorExpandingItem + $":\r\n'{ex.GetType().Name}': {ex.Message}").Tap<Run>(r => r.Foreground = Brushes.Red)))))));
								}
						if (isEnumerable)
						{
							var sec = new CollapsibleSection(obj, name, type, true);
							list.ListItems.Add(new ListItem().Tap(item => item.Blocks.Add(sec)));
						}
						Blocks.Add(list);
					}
				} else
				{
					button.Content = "▼";
					ResetBlocks();
				}
			} catch (Exception ex)
			{
				ResetBlocks();
				Blocks.Add(new Paragraph().Tap(p =>
					p.Inlines.Add(new Bold(new Run(Strings.ErrorExpandingItem + $":\r\n'{ex.GetType().Name}': {ex.Message}").Tap<Run>(r => r.Foreground = Brushes.Red)))));
			}
		};
		button.Content = obj != null ? "▼" : "◊"; // "■"; // "●";
		var inlineContainer = new InlineUIContainer(button) {
			BaselineAlignment = BaselineAlignment.Center, Cursor = Cursors.Hand
		};
		var header = new Paragraph();
		header.Inlines.Add(inlineContainer);
		if (expandEnumerable)
		{
			header.Inlines.Add(new Bold(new Run($"{Strings.List} ")));
			header.Foreground = Brushes.DarkBlue;
		} else
			header.Inlines.Add(new Bold(new Run(name)));
		if (isEnumerable) header.Inlines.Add(new Bold(new Run($" [{enumerableCount} {(enumerableCount == 1 ? Strings.Element : Strings.Elements)}]")));
		if (obj == null)
		{
			header.Inlines.Add(new Bold(new Run(" = ")));
			header.Inlines.Add(new Run("null"));
			button.IsHitTestVisible = false;
		}
		Blocks.Add(header);
	}

	/// <summary>
	///    Resets the section's blocks to only contain the header, effectively collapsing the section.
	/// </summary>
	/// <remarks>
	///    This method is called when the toggle button is clicked to collapse an expanded section.
	///    It preserves the first block (the header) and removes all other blocks (the expanded content).
	/// </remarks>
	void ResetBlocks()
	{
		var first = Blocks.First();
		Blocks.Clear();
		Blocks.Add(first);
	}
}