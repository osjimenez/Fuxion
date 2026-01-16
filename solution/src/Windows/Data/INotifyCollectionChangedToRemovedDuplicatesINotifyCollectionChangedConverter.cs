using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that wraps an <see cref="INotifyCollectionChanged"/> collection and automatically removes
///    duplicate items, maintaining a synchronized deduplicated collection.
/// </summary>
/// <remarks>
///    <para>
///       This converter extends <see cref="GenericConverter{TSource,TResult}"/> to provide automatic duplicate removal
///       from observable collections. It creates a new <see cref="ObservableCollection{T}"/> that stays synchronized
///       with the source collection, ensuring no duplicate items exist based on reference equality.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic deduplication:</strong> Removes duplicate items from the source collection
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Live synchronization:</strong> Maintains synchronization as source collection changes
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Event-driven updates:</strong> Responds to CollectionChanged events from source
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Reference equality:</strong> Uses object reference equality for duplicate detection
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Observable output:</strong> Returns an <see cref="ObservableCollection{T}"/> for WPF binding
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>How it works:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>
///             Initial conversion uses <see cref="Enumerable"/>.Distinct to create deduplicated collection
///          </description>
///       </item>
///       <item>
///          <description>
///             Subscribes to source's <see cref="INotifyCollectionChanged.CollectionChanged"/> event
///          </description>
///       </item>
///       <item>
///          <description>
///             When items are added: Only adds if not already in result collection
///          </description>
///       </item>
///       <item>
///          <description>
///             When items are removed: Only removes if no longer in source collection
///          </description>
///       </item>
///       <item>
///          <description>
///             When reset: Clears the result collection completely
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Important notes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             The source must implement both <see cref="INotifyCollectionChanged"/> and <see cref="IEnumerable{T}"/> of <see cref="object"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             Duplicate detection is based on reference equality (same object instance)
///          </description>
///       </item>
///       <item>
///          <description>
///             The converter maintains a subscription to source events; ensure proper lifetime management
///          </description>
///       </item>
///       <item>
///          <description>
///             Null items are handled but not added to the result collection
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display unique items from a collection that may contain duplicates</description>
///       </item>
///       <item>
///          <description>Show selected items without duplicates in multi-selection scenarios</description>
///       </item>
///       <item>
///          <description>Create tag or category lists without duplicate entries</description>
///       </item>
///       <item>
///          <description>Filter merged collections to show unique items only</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage in XAML:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter 
///         x:Key="DuplicateRemoverConverter"/>
/// </Window.Resources>
/// 
/// <!-- Display unique items from a collection that may have duplicates -->
/// <ListBox ItemsSource="{Binding ItemsWithDuplicates, 
///                               Converter={StaticResource DuplicateRemoverConverter}}"
///          DisplayMemberPath="Name"/>
/// ]]></code>
///    <strong>ViewModel with collection that may have duplicates:</strong>
///    <code>
/// public class ProductsViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;Product&gt; AllProducts { get; } = new();
///     
///     public void AddProduct(Product product)
///     {
///         // May add the same product multiple times
///         AllProducts.Add(product);
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- UI automatically shows only unique products -->
/// <ListBox ItemsSource="{Binding AllProducts, 
///                               Converter={StaticResource DuplicateRemoverConverter}}"/>
/// ]]></code>
///    <strong>Multi-selection scenario with unique items:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter 
///         x:Key="UniqueItemsConverter"/>
/// </Window.Resources>
/// 
/// <Grid>
///     <Grid.ColumnDefinitions>
///         <ColumnDefinition/>
///         <ColumnDefinition/>
///     </Grid.ColumnDefinitions>
///     
///     <!-- Source list with potential duplicates -->
///     <ListBox Grid.Column="0" 
///              ItemsSource="{Binding AvailableItems}"
///              SelectionMode="Multiple"/>
///     
///     <!-- Selected items list showing only unique selections -->
///     <ListBox Grid.Column="1" 
///              ItemsSource="{Binding SelectedItems, 
///                                   Converter={StaticResource UniqueItemsConverter}}"
///              DisplayMemberPath="Name">
///         <ListBox.Header>
///             <TextBlock Text="Unique Selections" FontWeight="Bold"/>
///         </ListBox.Header>
///     </ListBox>
/// </Grid>
/// ]]></code>
///    <strong>Usage in code-behind:</strong>
///    <code>
/// var converter = new INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter();
/// 
/// var sourceCollection = new ObservableCollection&lt;object&gt;();
/// var product1 = new Product { Name = "Product A" };
/// var product2 = new Product { Name = "Product B" };
/// 
/// // Add items including duplicates
/// sourceCollection.Add(product1);
/// sourceCollection.Add(product2);
/// sourceCollection.Add(product1); // Duplicate
/// sourceCollection.Add(product2); // Duplicate
/// 
/// // Convert to deduplicated collection
/// var uniqueCollection = converter.Convert(sourceCollection, CultureInfo.CurrentCulture);
/// 
/// Console.WriteLine(sourceCollection.Count);  // Output: 4
/// Console.WriteLine(uniqueCollection.Count);  // Output: 2
/// 
/// // Add another item to source
/// sourceCollection.Add(product1); // Another duplicate
/// 
/// // Unique collection stays the same
/// Console.WriteLine(uniqueCollection.Count);  // Output: 2 (still unique)
/// </code>
///    <strong>Dynamic tag collection example:</strong>
///    <code>
/// // ViewModel
/// public class DocumentViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;string&gt; AllTags { get; } = new();
///     
///     public void AddTag(string tag)
///     {
///         // Users may add the same tag multiple times
///         AllTags.Add(tag);
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- UI shows each tag only once -->
/// <Window.Resources>
///     <data:INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter 
///         x:Key="UniqueTagsConverter"/>
/// </Window.Resources>
/// 
/// <ItemsControl ItemsSource="{Binding AllTags, 
///                                    Converter={StaticResource UniqueTagsConverter}}">
///     <ItemsControl.ItemsPanel>
///         <ItemsPanelTemplate>
///             <WrapPanel/>
///         </ItemsPanelTemplate>
///     </ItemsControl.ItemsPanel>
///     <ItemsControl.ItemTemplate>
///         <DataTemplate>
///             <Border Background="LightBlue" 
///                     CornerRadius="3" 
///                     Padding="5,2" 
///                     Margin="2">
///                 <TextBlock Text="{Binding}"/>
///             </Border>
///         </DataTemplate>
///     </ItemsControl.ItemTemplate>
/// </ItemsControl>
/// ]]></code>
///    <strong>Merged collections with deduplication:</strong>
///    <code>
/// // ViewModel
/// public class CombinedViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;Item&gt; MergedItems { get; } = new();
///     
///     public void MergeCollections(IEnumerable&lt;Item&gt; collection1, IEnumerable&lt;Item&gt; collection2)
///     {
///         MergedItems.Clear();
///         foreach (var item in collection1)
///             MergedItems.Add(item);
///         foreach (var item in collection2)
///             MergedItems.Add(item); // May contain duplicates
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- Display unique items from merged collections -->
/// <ListBox ItemsSource="{Binding MergedItems, 
///                               Converter={StaticResource DuplicateRemoverConverter}}"/>
/// ]]></code>
///    <strong>Behavior with Reset action:</strong>
///    <code>
/// var converter = new INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter();
/// var source = new ObservableCollection&lt;object&gt;();
/// 
/// source.Add(new Product { Name = "A" });
/// source.Add(new Product { Name = "B" });
/// 
/// var unique = converter.Convert(source, CultureInfo.CurrentCulture);
/// Console.WriteLine(unique.Count);  // Output: 2
/// 
/// // Reset clears the unique collection
/// source.Clear(); // Triggers Reset action
/// Console.WriteLine(unique.Count);  // Output: 0
/// </code>
/// </example>
public class INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter : GenericConverter<INotifyCollectionChanged, INotifyCollectionChanged?>
{
	/// <summary>
	///    Converts an <see cref="INotifyCollectionChanged"/> collection to a deduplicated
	///    <see cref="INotifyCollectionChanged"/> collection.
	/// </summary>
	/// <param name="source">
	///    The source collection that implements both <see cref="INotifyCollectionChanged"/> and
	///    <see cref="IEnumerable{T}"/> of <see cref="object"/>. Can be <c>null</c>.
	/// </param>
	/// <param name="culture">
	///    The culture to use in the converter. This parameter is not used in the conversion
	///    but is required by the <see cref="GenericConverter{TSource,TResult}"/> interface.
	/// </param>
	/// <returns>
	///    An <see cref="ObservableCollection{T}"/> containing unique items from the source collection,
	///    or <c>null</c> if <paramref name="source"/> is <c>null</c>.
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when <paramref name="source"/> does not implement <see cref="IEnumerable{T}"/> of <see cref="object"/>.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method performs the following operations:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Returns <c>null</c> if source is <c>null</c></description>
	///       </item>
	///       <item>
	///          <description>
	///             Validates that source implements <see cref="IEnumerable{T}"/> of <see cref="object"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Creates an <see cref="ObservableCollection{T}"/> with distinct items using <see cref="Enumerable"/>.Distinct
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Subscribes to source's <see cref="INotifyCollectionChanged.CollectionChanged"/> event
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Handles collection changes to maintain deduplication:
	///             <list type="bullet">
	///                <item>
	///                   <description><strong>Add:</strong> Adds new items only if not already present</description>
	///                </item>
	///                <item>
	///                   <description>
	///                      <strong>Remove:</strong> Removes items only if they no longer exist in source
	///                   </description>
	///                </item>
	///                <item>
	///                   <description><strong>Reset:</strong> Clears the result collection</description>
	///                </item>
	///             </list>
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Important considerations:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             The event subscription creates a strong reference. Ensure proper disposal if needed.
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Duplicate detection is based on <see cref="object"/>.Equals as used by <see cref="Enumerable"/>.Distinct
	///             and <see cref="ObservableCollection{T}"/>.Contains
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Null items in NewItems/OldItems are skipped during processing
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             The converter creates a new collection instance each time it's called
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new INotifyCollectionChangedToRemovedDuplicatesINotifyCollectionChangedConverter();
	/// 
	/// // Create source with duplicates
	/// var source = new ObservableCollection&lt;string&gt; { "A", "B", "A", "C", "B" };
	/// 
	/// // Convert to unique collection
	/// var unique = converter.Convert(source, CultureInfo.CurrentCulture);
	/// // unique contains: ["A", "B", "C"]
	/// 
	/// // Add duplicate to source
	/// source.Add("A");
	/// // unique still contains: ["A", "B", "C"] - no duplicate added
	/// 
	/// // Add new item to source
	/// source.Add("D");
	/// // unique now contains: ["A", "B", "C", "D"]
	/// 
	/// // Remove item that appears once
	/// source.Remove("D");
	/// // unique now contains: ["A", "B", "C"]
	/// 
	/// // Remove item that appears multiple times
	/// source.Remove("A"); // Still one "A" left in source
	/// // unique still contains: ["A", "B", "C"] - item not removed yet
	/// 
	/// source.Remove("A"); // Now no "A" in source
	/// // unique now contains: ["B", "C"] - item removed
	/// </code>
	/// </example>
	public override INotifyCollectionChanged? Convert(INotifyCollectionChanged source, CultureInfo culture)
	{
		if (source == null) return null;
		if (!(source is IEnumerable<object>)) throw new NotSupportedException($"The source must be '{nameof(IEnumerable<object>)}'.");
		var res = new ObservableCollection<object>(((IEnumerable<object>)source).Distinct());
		source.CollectionChanged += (s, e) => {
			if (e.NewItems != null)
				foreach (var item in e.NewItems)
					if (item != null && !res.Contains(item))
						res.Add(item);
			if (e.OldItems != null)
				foreach (var item in e.OldItems)
					if (item != null && !((IEnumerable<object>)source).Contains(item))
						res.Remove(item);
			if (e.Action == NotifyCollectionChangedAction.Reset) res.Clear();
		};
		return res;
	}
}