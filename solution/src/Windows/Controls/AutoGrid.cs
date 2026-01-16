using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Fuxion.Windows.Controls;

/// <summary>
///    A specialized <see cref="Grid"/> that automatically arranges its children in a grid layout
///    without requiring explicit <see cref="Grid"/>.Row and <see cref="Grid"/>.Column attached properties.
/// </summary>
/// <remarks>
///    <para>
///       <see cref="AutoGrid"/> simplifies grid-based layouts by automatically calculating row and column
///       positions for child elements based on their order in the <see cref="Grid"/>.Children collection
///       and the number of column definitions. Children are placed left-to-right, top-to-bottom, similar
///       to a <see cref="WrapPanel"/> but with precise grid-based sizing.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic positioning:</strong> Children are automatically assigned to rows and columns
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Dynamic row generation:</strong> Rows are created automatically based on the number of children
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Uniform row heights:</strong> All rows share the same height defined by <see cref="RowHeight"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Column-specific alignment:</strong> Use <see cref="AutoColumnDefinition"/> for per-column content alignment
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic relayout:</strong> Updates automatically when children are added or removed
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>How it works:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>Define <see cref="Grid.ColumnDefinitions"/> to specify the number and width of columns</description>
///       </item>
///       <item>
///          <description>Set <see cref="RowHeight"/> to define a uniform height for all rows</description>
///       </item>
///       <item>
///          <description>Add child elements - they are automatically arranged in grid cells</description>
///       </item>
///       <item>
///          <description>Use <see cref="AutoColumnDefinition"/> for column-specific content alignment and margins</description>
///       </item>
///    </list>
///    <para>
///       <strong>Layout algorithm:</strong> Children are positioned using the formula:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Column Index = (Child Index) % (Number of Columns)</description>
///       </item>
///       <item>
///          <description>Row Index = (Child Index) / (Number of Columns)</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage with uniform row height:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="Auto">
///     <AutoGrid.ColumnDefinitions>
///         <ColumnDefinition Width="100"/>
///         <ColumnDefinition Width="*"/>
///         <ColumnDefinition Width="100"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <!-- First row -->
///     <TextBlock Text="Label 1:"/>
///     <TextBox/>
///     <Button Content="..."/>
///     
///     <!-- Second row (automatically created) -->
///     <TextBlock Text="Label 2:"/>
///     <TextBox/>
///     <Button Content="..."/>
///     
///     <!-- Third row (automatically created) -->
///     <TextBlock Text="Label 3:"/>
///     <TextBox/>
///     <Button Content="..."/>
/// </AutoGrid>
/// ]]></code>
///    <strong>Using AutoColumnDefinition for column-specific alignment:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="30">
///     <AutoGrid.ColumnDefinitions>
///         <!-- Labels right-aligned -->
///         <AutoColumnDefinition Width="120" 
///                               HorizontalContentAlignment="Right"
///                               VerticalContentAlignment="Center"
///                               ContentMargin="0,0,10,0"/>
///         
///         <!-- Input controls stretched -->
///         <AutoColumnDefinition Width="*" 
///                               HorizontalContentAlignment="Stretch"
///                               VerticalContentAlignment="Center"/>
///         
///         <!-- Buttons centered -->
///         <AutoColumnDefinition Width="80" 
///                               HorizontalContentAlignment="Center"
///                               VerticalContentAlignment="Center"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <TextBlock Text="Name:"/>
///     <TextBox/>
///     <Button Content="Clear"/>
///     
///     <TextBlock Text="Email:"/>
///     <TextBox/>
///     <Button Content="Verify"/>
///     
///     <TextBlock Text="Phone:"/>
///     <TextBox/>
///     <Button Content="Format"/>
/// </AutoGrid>
/// ]]></code>
///    <strong>Dynamic content with data binding:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="Auto">
///     <AutoGrid.ColumnDefinitions>
///         <ColumnDefinition Width="*"/>
///         <ColumnDefinition Width="*"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <!-- Children added dynamically via code-behind or ItemsControl -->
///     <ItemsControl ItemsSource="{Binding Items}">
///         <ItemsControl.ItemsPanel>
///             <ItemsPanelTemplate>
///                 <AutoGrid RowHeight="Auto"/>
///             </ItemsPanelTemplate>
///         </ItemsControl.ItemsPanel>
///     </ItemsControl>
/// </AutoGrid>
/// ]]></code>
///    <strong>Form layout example:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="35">
///     <AutoGrid.ColumnDefinitions>
///         <AutoColumnDefinition Width="Auto" 
///                               HorizontalContentAlignment="Right"
///                               ContentMargin="0,0,10,0"/>
///         <AutoColumnDefinition Width="200"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <TextBlock Text="First Name:"/>
///     <TextBox Text="{Binding FirstName}"/>
///     
///     <TextBlock Text="Last Name:"/>
///     <TextBox Text="{Binding LastName}"/>
///     
///     <TextBlock Text="Birth Date:"/>
///     <DatePicker SelectedDate="{Binding BirthDate}"/>
///     
///     <TextBlock Text="Country:"/>
///     <ComboBox ItemsSource="{Binding Countries}" SelectedItem="{Binding Country}"/>
/// </AutoGrid>
/// ]]></code>
/// </example>
public class AutoGrid : Grid
{
	/// <summary>
	///    Identifies the <see cref="RowHeight"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(GridLength), typeof(AutoGrid), new FrameworkPropertyMetadata(default(GridLength),
		FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => {
			if (d is AutoGrid ag) ag.PerformLayout(true);
		}));
	bool mustRelayout = true;

	/// <summary>
	///    Gets or sets the height for all rows in the grid.
	/// </summary>
	/// <value>
	///    A <see cref="GridLength"/> value that specifies the height of all rows.
	///    Default is <see cref="GridLength"/> default value.
	/// </value>
	/// <remarks>
	///    <para>
	///       All rows in the <see cref="AutoGrid"/> will have the same height. Common values include:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><c>Auto</c>: Rows size to fit their content</description>
	///       </item>
	///       <item>
	///          <description><c>*</c> (Star): Rows share available space proportionally</description>
	///       </item>
	///       <item>
	///          <description>Fixed value (e.g., 30, 50): Rows have exact pixel height</description>
	///       </item>
	///    </list>
	///    <para>
	///       Changing this property triggers a complete relayout of the grid.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code><![CDATA[
	/// <!-- Auto-sized rows -->
	/// <AutoGrid RowHeight="Auto"/>
	/// 
	/// <!-- Fixed 40-pixel rows -->
	/// <AutoGrid RowHeight="40"/>
	/// 
	/// <!-- Star-sized rows (proportional) -->
	/// <AutoGrid RowHeight="*"/>
	/// ]]></code>
	/// </example>
	[Category("Layout")]
	[Description("Define height for all rows of the grid")]
	public GridLength RowHeight
	{
		get => (GridLength)GetValue(RowHeightProperty);
		set => SetValue(RowHeightProperty, value);
	}

	/// <summary>
	///    Gets the column index for a child element based on its position in the children collection.
	/// </summary>
	/// <param name="child">The child element to get the column index for.</param>
	/// <returns>
	///    The zero-based column index calculated as: (child index) % (number of columns).
	/// </returns>
	int GetChildColumnIndex(UIElement child) => Children.IndexOf(child) % ColumnDefinitions.Count;

	/// <summary>
	///    Gets the row index for a child element based on its position in the children collection.
	/// </summary>
	/// <param name="child">The child element to get the row index for.</param>
	/// <returns>
	///    The zero-based row index calculated as: (child index) / (number of columns).
	/// </returns>
	int GetChildRowIndex(UIElement child) => Children.IndexOf(child) / ColumnDefinitions.Count;

	/// <summary>
	///    Performs the layout calculation, creating row definitions and assigning children to grid cells.
	/// </summary>
	/// <param name="force">
	///    If <c>true</c>, forces a relayout even if <see cref="mustRelayout"/> is <c>false</c>.
	///    Default is <c>false</c>.
	/// </param>
	/// <remarks>
	///    <para>
	///       This method is called automatically during measure/arrange cycles and when children change.
	///       It performs the following operations:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Clears existing <see cref="Grid.RowDefinitions"/></description>
	///       </item>
	///       <item>
	///          <description>Calculates required number of rows based on children count and columns</description>
	///       </item>
	///       <item>
	///          <description>Creates new row definitions with the specified <see cref="RowHeight"/></description>
	///       </item>
	///       <item>
	///          <description>Assigns each child to its calculated row and column</description>
	///       </item>
	///       <item>
	///          <description>
	///             Applies column-specific alignment and margin from <see cref="AutoColumnDefinition"/> if present
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	internal void PerformLayout(bool force = false)
	{
		if (mustRelayout || force)
		{
			mustRelayout = false;
			RowDefinitions.Clear();
			if (ColumnDefinitions.Count == 0) return;
			for (var i = 0; i < System.Math.Ceiling((decimal)Children.Count / ColumnDefinitions.Count); i++)
				RowDefinitions.Add(new() {
					Height = RowHeight
				});
			foreach (UIElement? child in Children)
				if (child != null)
				{
					var colIndex = GetChildColumnIndex(child);
					child.SetValue(ColumnProperty, colIndex);
					child.SetValue(RowProperty, GetChildRowIndex(child));
					if (ColumnDefinitions[colIndex] is AutoColumnDefinition acd)
					{
						child.SetValue(HorizontalAlignmentProperty, acd.HorizontalContentAlignment);
						child.SetValue(VerticalAlignmentProperty, acd.VerticalContentAlignment);
						child.SetValue(MarginProperty, acd.ContentMargin);
					}
				}
		}
	}

	#region Overrides
	/// <summary>
	///    Measures the size required for child elements and determines the size required for the grid.
	/// </summary>
	/// <param name="constraint">
	///    The available size that this element can give to child elements.
	/// </param>
	/// <returns>
	///    The size that this element determines it needs during layout, based on its calculations
	///    of child element sizes.
	/// </returns>
	/// <remarks>
	///    This override ensures that <see cref="PerformLayout"/> is called before the base
	///    measure logic, guaranteeing that row definitions and child positions are up-to-date.
	/// </remarks>
	protected override Size MeasureOverride(Size constraint)
	{
		PerformLayout();
		return base.MeasureOverride(constraint);
	}

	/// <summary>
	///    Handles changes to the visual children collection.
	/// </summary>
	/// <param name="visualAdded">The visual child that was added, or <c>null</c> if a child was removed.</param>
	/// <param name="visualRemoved">The visual child that was removed, or <c>null</c> if a child was added.</param>
	/// <remarks>
	///    This override marks the grid as requiring a relayout whenever children are added or removed,
	///    ensuring that the grid structure is recalculated on the next measure pass.
	/// </remarks>
	protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
	{
		mustRelayout = true;
		base.OnVisualChildrenChanged(visualAdded, visualRemoved);
	}
	#endregion Overrides
}

/// <summary>
///    A specialized <see cref="ColumnDefinition"/> for use with <see cref="AutoGrid"/> that allows
///    specifying alignment and margin properties for all content within the column.
/// </summary>
/// <remarks>
///    <para>
///       <see cref="AutoColumnDefinition"/> extends the standard <see cref="ColumnDefinition"/> to provide
///       column-level content formatting. When used in an <see cref="AutoGrid"/>, all child elements
///       placed in this column automatically inherit the specified alignment and margin settings.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Horizontal alignment:</strong> Set <see cref="HorizontalContentAlignment"/> for all column content
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Vertical alignment:</strong> Set <see cref="VerticalContentAlignment"/> for all column content
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Content margin:</strong> Set <see cref="ContentMargin"/> to add spacing around column content
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic application:</strong> Properties are automatically applied to children during layout
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Right-aligning labels in a form's label column</description>
///       </item>
///       <item>
///          <description>Centering buttons or icons in an action column</description>
///       </item>
///       <item>
///          <description>Adding consistent padding between columns</description>
///       </item>
///       <item>
///          <description>Stretching input controls in a data entry column</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Form layout with label and input columns:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="30">
///     <AutoGrid.ColumnDefinitions>
///         <!-- Label column: right-aligned with right margin -->
///         <AutoColumnDefinition Width="100" 
///                               HorizontalContentAlignment="Right"
///                               VerticalContentAlignment="Center"
///                               ContentMargin="0,0,10,0"/>
///         
///         <!-- Input column: stretched -->
///         <AutoColumnDefinition Width="*" 
///                               HorizontalContentAlignment="Stretch"
///                               VerticalContentAlignment="Center"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <TextBlock Text="Username:"/>
///     <TextBox/>
///     
///     <TextBlock Text="Password:"/>
///     <PasswordBox/>
/// </AutoGrid>
/// ]]></code>
///    <strong>Data grid with aligned columns:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="25">
///     <AutoGrid.ColumnDefinitions>
///         <!-- ID column: centered -->
///         <AutoColumnDefinition Width="50" 
///                               HorizontalContentAlignment="Center"
///                               VerticalContentAlignment="Center"/>
///         
///         <!-- Name column: left-aligned -->
///         <AutoColumnDefinition Width="*" 
///                               HorizontalContentAlignment="Left"
///                               VerticalContentAlignment="Center"
///                               ContentMargin="5,0,0,0"/>
///         
///         <!-- Amount column: right-aligned -->
///         <AutoColumnDefinition Width="100" 
///                               HorizontalContentAlignment="Right"
///                               VerticalContentAlignment="Center"
///                               ContentMargin="0,0,5,0"/>
///         
///         <!-- Actions column: centered -->
///         <AutoColumnDefinition Width="80" 
///                               HorizontalContentAlignment="Center"
///                               VerticalContentAlignment="Center"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <!-- Headers -->
///     <TextBlock Text="ID" FontWeight="Bold"/>
///     <TextBlock Text="Name" FontWeight="Bold"/>
///     <TextBlock Text="Amount" FontWeight="Bold"/>
///     <TextBlock Text="Actions" FontWeight="Bold"/>
///     
///     <!-- Data rows -->
///     <TextBlock Text="001"/>
///     <TextBlock Text="Product A"/>
///     <TextBlock Text="$99.99"/>
///     <Button Content="Edit"/>
/// </AutoGrid>
/// ]]></code>
///    <strong>Using all three columns with different alignments:</strong>
///    <code><![CDATA[
/// <AutoGrid RowHeight="40">
///     <AutoGrid.ColumnDefinitions>
///         <AutoColumnDefinition Width="Auto" 
///                               HorizontalContentAlignment="Left"/>
///         <AutoColumnDefinition Width="*" 
///                               HorizontalContentAlignment="Center"/>
///         <AutoColumnDefinition Width="Auto" 
///                               HorizontalContentAlignment="Right"/>
///     </AutoGrid.ColumnDefinitions>
///     
///     <Button Content="Previous"/>
///     <TextBlock Text="Page 1 of 10" VerticalAlignment="Center"/>
///     <Button Content="Next"/>
/// </AutoGrid>
/// ]]></code>
/// </example>
public class AutoColumnDefinition : ColumnDefinition
{
	/// <summary>
	///    Identifies the <see cref="HorizontalContentAlignment"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register(nameof(HorizontalContentAlignment), typeof(HorizontalAlignment),
		typeof(AutoColumnDefinition), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
			(d, e) => {
				if (d is AutoColumnDefinition acd && acd.Parent is AutoGrid ag) ag.PerformLayout(true);
			}));

	/// <summary>
	///    Identifies the <see cref="VerticalContentAlignment"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty VerticalContentAlignmentProperty = DependencyProperty.Register(nameof(VerticalContentAlignment), typeof(VerticalAlignment), typeof(AutoColumnDefinition),
		new FrameworkPropertyMetadata(VerticalAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => {
			if (d is AutoColumnDefinition acd && acd.Parent is AutoGrid ag) ag.PerformLayout(true);
		}));

	/// <summary>
	///    Identifies the <see cref="ContentMargin"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty ContentMarginProperty = DependencyProperty.Register(nameof(ContentMargin), typeof(Thickness), typeof(AutoColumnDefinition), new FrameworkPropertyMetadata(
		default(Thickness), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => {
			if (d is AutoColumnDefinition acd && acd.Parent is AutoGrid ag) ag.PerformLayout(true);
		}));

	/// <summary>
	///    Gets or sets the horizontal alignment for all elements in this column.
	/// </summary>
	/// <value>
	///    A <see cref="HorizontalAlignment"/> value that specifies how child elements are
	///    horizontally positioned within their cell. Default is <see cref="HorizontalAlignment.Stretch"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property is automatically applied to all child elements placed in this column
	///       by the <see cref="AutoGrid"/> during layout. Common values:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="HorizontalAlignment.Left"/>: Align content to the left</description>
	///       </item>
	///       <item>
	///          <description><see cref="HorizontalAlignment.Center"/>: Center content horizontally</description>
	///       </item>
	///       <item>
	///          <description><see cref="HorizontalAlignment.Right"/>: Align content to the right</description>
	///       </item>
	///       <item>
	///          <description><see cref="HorizontalAlignment.Stretch"/>: Stretch content to fill available width</description>
	///       </item>
	///    </list>
	/// </remarks>
	[Category("Layout")]
	[Description("Define horizontal alignment for all elements of this column")]
	public HorizontalAlignment HorizontalContentAlignment
	{
		get => (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty);
		set => SetValue(HorizontalContentAlignmentProperty, value);
	}

	/// <summary>
	///    Gets or sets the vertical alignment for all elements in this column.
	/// </summary>
	/// <value>
	///    A <see cref="VerticalAlignment"/> value that specifies how child elements are
	///    vertically positioned within their cell. Default is <see cref="VerticalAlignment.Stretch"/>.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property is automatically applied to all child elements placed in this column
	///       by the <see cref="AutoGrid"/> during layout. Common values:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="VerticalAlignment.Top"/>: Align content to the top</description>
	///       </item>
	///       <item>
	///          <description><see cref="VerticalAlignment.Center"/>: Center content vertically</description>
	///       </item>
	///       <item>
	///          <description><see cref="VerticalAlignment.Bottom"/>: Align content to the bottom</description>
	///       </item>
	///       <item>
	///          <description><see cref="VerticalAlignment.Stretch"/>: Stretch content to fill available height</description>
	///       </item>
	///    </list>
	/// </remarks>
	[Category("Layout")]
	[Description("Define vertical alignment for all elements of this column")]
	public VerticalAlignment VerticalContentAlignment
	{
		get => (VerticalAlignment)GetValue(VerticalContentAlignmentProperty);
		set => SetValue(VerticalContentAlignmentProperty, value);
	}

	/// <summary>
	///    Gets or sets the margin to apply to all elements in this column.
	/// </summary>
	/// <value>
	///    A <see cref="Thickness"/> value that specifies the margin around child elements.
	///    Default is <see cref="Thickness"/> default value (0,0,0,0).
	/// </value>
	/// <remarks>
	///    <para>
	///       This property is automatically applied to all child elements placed in this column
	///       by the <see cref="AutoGrid"/> during layout. Use this to add consistent spacing
	///       between columns or to add padding within a column.
	///    </para>
	///    <para>
	///       The <see cref="Thickness"/> structure specifies margins in the order:
	///       Left, Top, Right, Bottom.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code><![CDATA[
	/// <!-- Add 10 pixels of right margin -->
	/// <AutoColumnDefinition ContentMargin="0,0,10,0"/>
	/// 
	/// <!-- Add 5 pixels on all sides -->
	/// <AutoColumnDefinition ContentMargin="5"/>
	/// 
	/// <!-- Add different margins on each side -->
	/// <AutoColumnDefinition ContentMargin="5,2,10,2"/>
	/// ]]></code>
	/// </example>
	[Category("Layout")]
	[Description("Define margin for all elements of this column")]
	public Thickness ContentMargin
	{
		get => (Thickness)GetValue(ContentMarginProperty);
		set => SetValue(ContentMarginProperty, value);
	}
}