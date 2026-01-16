using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Fuxion.Text.Json.Serialization.Metadata;

/// <summary>
/// Provides a custom JSON type information resolver that orders object properties alphabetically during serialization.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AlphabeticalOrderJsonTypeInfoResolver"/> extends <see cref="DefaultJsonTypeInfoResolver"/> to automatically
/// sort all JSON object properties in alphabetical order by their property names. This ensures consistent property ordering
/// in serialized JSON output, which is useful for:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Consistent output:</strong> Guaranteed predictable JSON structure regardless of property declaration order</description></item>
/// <item><description><strong>Diff-friendly:</strong> Makes JSON output easier to compare in version control systems</description></item>
/// <item><description><strong>Documentation:</strong> Generated JSON examples have consistent, readable ordering</description></item>
/// <item><description><strong>Testing:</strong> Simplifies string-based JSON comparison in unit tests</description></item>
/// <item><description><strong>API contracts:</strong> Ensures stable property ordering in public APIs</description></item>
/// </list>
/// <para>
/// <strong>How it works:</strong>
/// </para>
/// <para>
/// This resolver intercepts the type information retrieval process and assigns sequential <see cref="JsonPropertyInfo.Order"/>
/// values to all properties based on alphabetical sorting of property names. Properties with lower order values are
/// serialized first.
/// </para>
/// <para>
/// <strong>Performance considerations:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Sorting happens once per type during metadata initialization, not on every serialization</description></item>
/// <item><description>Minimal overhead compared to default resolver</description></item>
/// <item><description>Recommended for scenarios where output consistency is more important than property declaration order</description></item>
/// </list>
/// <para>
/// <strong>Compatibility:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Works with System.Text.Json serialization (not Newtonsoft.Json)</description></item>
/// <item><description>Compatible with all standard JSON converters and attributes</description></item>
/// <item><description>Respects [JsonIgnore] and other serialization attributes</description></item>
/// <item><description>Can be combined with other custom converters and resolvers</description></item>
/// </list>
/// </remarks>
/// <example>
/// <strong>Basic usage with JsonSerializerOptions:</strong>
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     TypeInfoResolver = new AlphabeticalOrderJsonTypeInfoResolver(),
///     WriteIndented = true
/// };
/// 
/// var person = new Person
/// {
///     Name = "John",
///     Age = 30,
///     Email = "john@example.com"
/// };
/// 
/// string json = JsonSerializer.Serialize(person, options);
/// // Output (alphabetically ordered):
/// // {
/// //   "Age": 30,
/// //   "Email": "john@example.com",
/// //   "Name": "John"
/// // }
/// </code>
/// 
/// <strong>Combining with a custom resolver chain:</strong>
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     TypeInfoResolver = JsonTypeInfoResolver.Combine(
///         new AlphabeticalOrderJsonTypeInfoResolver(),
///         new DefaultJsonTypeInfoResolver()
///     )
/// };
/// </code>
/// 
/// <strong>Using with JsonSerializerContext (source generation):</strong>
/// <code>
/// [JsonSourceGenerationOptions(
///     WriteIndented = true,
///     PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
/// [JsonSerializable(typeof(Person))]
/// public partial class MyJsonContext : JsonSerializerContext
/// {
///     public MyJsonContext() : base(new JsonSerializerOptions
///     {
///         TypeInfoResolver = new AlphabeticalOrderJsonTypeInfoResolver()
///     })
///     {
///     }
/// }
/// 
/// var json = JsonSerializer.Serialize(person, MyJsonContext.Default.Person);
/// </code>
/// 
/// <strong>Before and after comparison:</strong>
/// <code>
/// // Without AlphabeticalOrderJsonTypeInfoResolver:
/// // Properties appear in declaration order
/// public class Product
/// {
///     public string Name { get; set; }
///     public decimal Price { get; set; }
///     public string Category { get; set; }
/// }
/// // JSON: {"Name":"Widget","Price":19.99,"Category":"Tools"}
/// 
/// // With AlphabeticalOrderJsonTypeInfoResolver:
/// // Properties appear in alphabetical order
/// // JSON: {"Category":"Tools","Name":"Widget","Price":19.99}
/// </code>
/// </example>
public class AlphabeticalOrderJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
	/// <summary>
	/// Gets the type information for the specified type and applies alphabetical ordering to its properties.
	/// </summary>
	/// <param name="type">The type to get information for.</param>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization.</param>
	/// <returns>
	/// A <see cref="JsonTypeInfo"/> instance with properties ordered alphabetically by name.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method overrides the base implementation to add property ordering logic. The process is:
	/// </para>
	/// <list type="number">
	/// <item><description>Call the base <see cref="DefaultJsonTypeInfoResolver.GetTypeInfo"/> to get standard type metadata</description></item>
	/// <item><description>Sort all properties in the <see cref="JsonTypeInfo.Properties"/> collection by <see cref="JsonPropertyInfo.Name"/></description></item>
	/// <item><description>Assign sequential <see cref="JsonPropertyInfo.Order"/> values starting from 1</description></item>
	/// <item><description>Return the modified <see cref="JsonTypeInfo"/> with ordered properties</description></item>
	/// </list>
	/// <para>
	/// <strong>Property ordering rules:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Case-sensitive alphabetical sorting (A-Z, then a-z)</description></item>
	/// <item><description>Numbers come before letters in ASCII order</description></item>
	/// <item><description>Special characters sort according to their ASCII/Unicode values</description></item>
	/// <item><description>All properties receive explicit order values, overriding any existing order settings</description></item>
	/// </list>
	/// <para>
	/// <strong>Note:</strong> This method is called once per type during the first serialization/deserialization
	/// operation for that type. Subsequent operations reuse the cached metadata.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Internal behavior demonstration (for understanding only)
	/// // Given a type with properties: Zebra, Apple, Mango
	/// 
	/// // Step 1: Get base type info
	/// var jsonTypeInfo = base.GetTypeInfo(type, options);
	/// // Properties: [Zebra (Order=0), Apple (Order=0), Mango (Order=0)]
	/// 
	/// // Step 2: Sort alphabetically
	/// var sortedProps = jsonTypeInfo.Properties.OrderBy(p => p.Name);
	/// // Sorted: [Apple, Mango, Zebra]
	/// 
	/// // Step 3: Assign sequential orders
	/// // Result: [Apple (Order=1), Mango (Order=2), Zebra (Order=3)]
	/// 
	/// // JSON output will serialize in this order:
	/// // { "Apple": ..., "Mango": ..., "Zebra": ... }
	/// </code>
	/// </example>
	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var jsonTypeInfo = base.GetTypeInfo(type, options);
		var order = 1;

		foreach (var property in jsonTypeInfo.Properties.OrderBy(p => p.Name)) property.Order = order++;

		return jsonTypeInfo;
	}
}