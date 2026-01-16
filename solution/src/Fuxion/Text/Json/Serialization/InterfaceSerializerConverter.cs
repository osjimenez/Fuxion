using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

namespace Fuxion.Text.Json.Serialization;

/// <summary>
/// Provides a custom JSON converter for interface types that supports serialization but not deserialization.
/// </summary>
/// <typeparam name="TInterface">The interface type to handle during JSON serialization.</typeparam>
/// <remarks>
/// <para>
/// <see cref="InterfaceSerializerConverter{TInterface}"/> extends <see cref="JsonConverter{T}"/> to enable JSON serialization
/// of interface-typed properties by automatically resolving to the concrete implementation type at runtime. This is useful when:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Working with polymorphic types:</strong> Interface properties that hold different concrete implementations</description></item>
/// <item><description><strong>Data transfer objects:</strong> DTOs with interface properties that need JSON serialization</description></item>
/// <item><description><strong>API responses:</strong> Returning interface-based models where concrete types vary</description></item>
/// <item><description><strong>Dependency injection:</strong> Serializing objects with injected interface dependencies</description></item>
/// <item><description><strong>Plugin architectures:</strong> Systems with dynamically loaded implementations</description></item>
/// </list>
/// <para>
/// <strong>Key features:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Runtime type resolution:</strong> Automatically detects and serializes the actual concrete type</description></item>
/// <item><description><strong>Null handling:</strong> Properly handles null interface values</description></item>
/// <item><description><strong>Type safety:</strong> Validates that types are interfaces and assignable to TInterface</description></item>
/// <item><description><strong>Deserialization prevention:</strong> Explicitly blocks deserialization with clear error messages</description></item>
/// </list>
/// <para>
/// <strong>Why deserialization is not supported:</strong>
/// </para>
/// <para>
/// Interfaces cannot be instantiated directly, and there's no reliable way to determine which concrete type
/// should be created during deserialization without additional metadata. For deserialization scenarios, consider:
/// </para>
/// <list type="bullet">
/// <item><description>Using concrete types in your models</description></item>
/// <item><description>Implementing a custom type discriminator pattern</description></item>
/// <item><description>Using polymorphic serialization with type metadata (e.g., $type property)</description></item>
/// <item><description>Employing a custom deserializer with type resolution logic</description></item>
/// </list>
/// <para>
/// <strong>Type compatibility:</strong>
/// </para>
/// <para>
/// The converter handles both exact interface matches and derived interfaces through the <see cref="CanConvert"/> method.
/// It checks if the type is an interface and if it's either TInterface itself or assignable from TInterface.
/// </para>
/// </remarks>
/// <example>
/// <strong>Basic usage with a single interface:</strong>
/// <code>
/// public interface IEntity
/// {
///     int Id { get; set; }
///     string Name { get; set; }
/// }
/// 
/// public class Product : IEntity
/// {
///     public int Id { get; set; }
///     public string Name { get; set; }
///     public decimal Price { get; set; }
/// }
/// 
/// public class OrderItem
/// {
///     public IEntity Entity { get; set; } // Interface property
///     public int Quantity { get; set; }
/// }
/// 
/// // Configure the converter
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new InterfaceSerializerConverter&lt;IEntity&gt;());
/// 
/// var item = new OrderItem
/// {
///     Entity = new Product { Id = 1, Name = "Widget", Price = 19.99m },
///     Quantity = 5
/// };
/// 
/// // Serialization works - concrete type is detected and serialized
/// string json = JsonSerializer.Serialize(item, options);
/// // Result: {"Entity":{"Id":1,"Name":"Widget","Price":19.99},"Quantity":5}
/// 
/// // Deserialization throws SerializationException
/// try
/// {
///     var deserialized = JsonSerializer.Deserialize&lt;OrderItem&gt;(json, options);
/// }
/// catch (SerializationException ex)
/// {
///     Console.WriteLine(ex.Message); // "Deserialize from an interface is not supported..."
/// }
/// </code>
/// 
/// <strong>Handling null values:</strong>
/// <code>
/// var item = new OrderItem
/// {
///     Entity = null, // Null interface value
///     Quantity = 3
/// };
/// 
/// string json = JsonSerializer.Serialize(item, options);
/// // Result: {"Entity":null,"Quantity":3}
/// </code>
/// 
/// <strong>Multiple interface types:</strong>
/// <code>
/// public interface IPaymentMethod { }
/// public interface IShippingMethod { }
/// 
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new InterfaceSerializerConverter&lt;IPaymentMethod&gt;());
/// options.Converters.Add(new InterfaceSerializerConverter&lt;IShippingMethod&gt;());
/// </code>
/// 
/// <strong>Collections of interfaces:</strong>
/// <code>
/// public class Container
/// {
///     public List&lt;IEntity&gt; Items { get; set; }
/// }
/// 
/// var container = new Container
/// {
///     Items = new List&lt;IEntity&gt;
///     {
///         new Product { Id = 1, Name = "A" },
///         new Product { Id = 2, Name = "B" }
///     }
/// };
/// 
/// // Each item in the collection is serialized with its concrete type
/// string json = JsonSerializer.Serialize(container, options);
/// </code>
/// </example>
public class InterfaceSerializerConverter<TInterface> : JsonConverter<TInterface>
{
	/// <summary>
	/// Determines whether this converter can handle the specified type.
	/// </summary>
	/// <param name="type">The type to check for conversion compatibility.</param>
	/// <returns>
	/// <c>true</c> if the type is an interface and is either <typeparamref name="TInterface"/> or assignable from it;
	/// otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method performs two validations:
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Interface check:</strong> Verifies that <paramref name="type"/> is an interface type</description></item>
	/// <item><description><strong>Type compatibility:</strong> Ensures the type matches or is assignable from <typeparamref name="TInterface"/></description></item>
	/// </list>
	/// <para>
	/// <strong>Type hierarchy support:</strong>
	/// </para>
	/// <para>
	/// The converter supports interface hierarchies. For example, if you have:
	/// </para>
	/// <code>
	/// public interface IBase { }
	/// public interface IDerived : IBase { }
	/// 
	/// var converter = new InterfaceSerializerConverter&lt;IBase&gt;();
	/// </code>
	/// <para>
	/// The converter will handle both <c>IBase</c> and <c>IDerived</c> types because <c>IBase.IsAssignableFrom(IDerived)</c> returns <c>true</c>.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var converter = new InterfaceSerializerConverter&lt;IEntity&gt;();
	/// 
	/// // Returns true - exact match
	/// bool result1 = converter.CanConvert(typeof(IEntity));
	/// 
	/// // Returns true - derived interface
	/// bool result2 = converter.CanConvert(typeof(IDerivedEntity));
	/// 
	/// // Returns false - concrete class
	/// bool result3 = converter.CanConvert(typeof(Product));
	/// 
	/// // Returns false - unrelated interface
	/// bool result4 = converter.CanConvert(typeof(IDisposable));
	/// </code>
	/// </example>
	public override bool CanConvert(Type type) => type.IsInterface && (type == typeof(TInterface) || typeof(TInterface).IsAssignableFrom(type));
	
	/// <summary>
	/// Reads and converts the JSON to the specified type. This operation is not supported and always throws an exception.
	/// </summary>
	/// <param name="reader">The reader to use for reading JSON data.</param>
	/// <param name="type">The type of object to convert to.</param>
	/// <param name="options">The serializer options to use.</param>
	/// <returns>This method never returns as it always throws an exception.</returns>
	/// <exception cref="SerializationException">
	/// Always thrown because deserialization from interface types is not supported.
	/// The exception message includes the full type signature of the converter for debugging purposes.
	/// </exception>
	/// <remarks>
	/// <para>
	/// <strong>Why this throws:</strong>
	/// </para>
	/// <para>
	/// Interfaces cannot be instantiated, and there's no reliable way to determine which concrete implementation
	/// should be created during deserialization without additional type information or discriminators.
	/// </para>
	/// <para>
	/// <strong>Alternatives for deserialization:</strong>
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Use concrete types:</strong> Change your model to use concrete classes instead of interfaces</description></item>
	/// <item><description><strong>Type discriminator:</strong> Add a $type property and implement custom deserialization logic</description></item>
	/// <item><description><strong>Factory pattern:</strong> Use a factory method to create instances based on JSON content</description></item>
	/// <item><description><strong>Separate DTOs:</strong> Use different models for serialization and deserialization</description></item>
	/// </list>
	/// <para>
	/// The error message includes the type signature via <see cref="Fuxion.Reflection.ReflectionExtensions.GetSignature(Type, bool)"/> for easier debugging
	/// and identification of which converter is causing the issue.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var options = new JsonSerializerOptions();
	/// options.Converters.Add(new InterfaceSerializerConverter&lt;IEntity&gt;());
	/// 
	/// string json = "{\"Id\":1,\"Name\":\"Test\"}";
	/// 
	/// try
	/// {
	///     var entity = JsonSerializer.Deserialize&lt;IEntity&gt;(json, options);
	/// }
	/// catch (SerializationException ex)
	/// {
	///     Console.WriteLine(ex.Message);
	///     // Output: "Deserialize from an interface is not supported by 
	///     //          'InterfaceSerializerConverter&lt;IEntity&gt;'"
	/// }
	/// </code>
	/// </example>
	public override TInterface Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		=> throw new SerializationException($"Deserialize from an interface is not supported by '{typeof(InterfaceSerializerConverter<>).MakeGenericType(type).GetSignature()}'");
	
	/// <summary>
	/// Writes the specified value as JSON by automatically detecting and serializing the concrete implementation type.
	/// </summary>
	/// <param name="writer">The writer to use for writing JSON data.</param>
	/// <param name="value">The value to serialize. Can be <c>null</c>.</param>
	/// <param name="options">The serializer options to use.</param>
	/// <remarks>
	/// <para>
	/// This method implements the core serialization logic with the following behavior:
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Null check:</strong> If <paramref name="value"/> is <c>null</c>, writes a JSON null value</description></item>
	/// <item><description><strong>Type resolution:</strong> Gets the actual runtime type of the value using <see cref="object.GetType()"/></description></item>
	/// <item><description><strong>Concrete serialization:</strong> Serializes the value using its concrete type, not the interface type</description></item>
	/// </list>
	/// <para>
	/// <strong>How it works:</strong>
	/// </para>
	/// <para>
	/// When an interface property is serialized, this method uses <see cref="JsonSerializer.Serialize(Utf8JsonWriter, object, Type, JsonSerializerOptions)"/>
	/// with the actual concrete type. This ensures all properties of the concrete implementation are included in the JSON output,
	/// not just the interface members.
	/// </para>
	/// <para>
	/// <strong>Example behavior:</strong>
	/// </para>
	/// <code>
	/// public interface IBase 
	/// { 
	///     int Id { get; set; } 
	/// }
	/// 
	/// public class Derived : IBase 
	/// { 
	///     public int Id { get; set; }
	///     public string Extra { get; set; } // Not in interface
	/// }
	/// 
	/// IBase obj = new Derived { Id = 1, Extra = "test" };
	/// 
	/// // Serializes as: {"Id":1,"Extra":"test"}
	/// // The Extra property is included because we serialize the concrete type
	/// </code>
	/// <para>
	/// <strong>Null value handling:</strong>
	/// </para>
	/// <para>
	/// Null interface values are written as JSON null, which is the standard behavior expected by consumers.
	/// This ensures compatibility with nullable reference types and standard JSON semantics.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Serializing a concrete instance through an interface property
	/// public class Order
	/// {
	///     public IPaymentMethod Payment { get; set; }
	/// }
	/// 
	/// public class CreditCardPayment : IPaymentMethod
	/// {
	///     public string CardNumber { get; set; }
	///     public DateTime Expiry { get; set; }
	/// }
	/// 
	/// var order = new Order
	/// {
	///     Payment = new CreditCardPayment 
	///     { 
	///         CardNumber = "1234", 
	///         Expiry = new DateTime(2025, 12, 31) 
	///     }
	/// };
	/// 
	/// // The converter detects CreditCardPayment type and serializes all its properties
	/// // Result: {"Payment":{"CardNumber":"1234","Expiry":"2025-12-31T00:00:00"}}
	/// 
	/// // Handling null
	/// order.Payment = null;
	/// // Result: {"Payment":null}
	/// </code>
	/// </example>
	public override void Write(Utf8JsonWriter writer, TInterface? value, JsonSerializerOptions options)
	{
		if (value is null)
			writer.WriteNullValue();
		else
		{
			var type = value.GetType();
			JsonSerializer.Serialize(writer, value, type, options);
		}
	}
}