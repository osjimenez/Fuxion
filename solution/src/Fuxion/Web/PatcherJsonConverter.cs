using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;
using Fuxion.Text.Json;
using Fuxion.Text.Json.Serialization.Metadata;

namespace Fuxion.Web;

/// <summary>
///    Provides JSON serialization and deserialization support for <see cref="Patcher{T}" /> instances.
/// </summary>
/// <typeparam name="T">The type of object that can be patched. Must be a reference type (class).</typeparam>
/// <remarks>
///    <para>
///       <see cref="PatcherJsonConverter{T}" /> is a specialized <see cref="JsonConverter{T}" /> that handles the
///       conversion
///       between JSON and <see cref="Patcher{T}" /> objects. This converter is essential for implementing HTTP PATCH
///       operations
///       where only a subset of properties should be updated based on what's present in the JSON payload.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Selective deserialization:</strong> Only properties present in JSON are added to the
///             Patcher's internal dictionary
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Property tracking:</strong> Maintains a mapping of property names to their (PropertyInfo,
///             value) tuples
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type-safe serialization:</strong> Preserves property types during round-trip
///             serialization
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Private constructor support:</strong> Uses
///             <see cref="PrivateConstructorJsonTypeInfoResolver" /> for types with non-public constructors
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Distinguishes between "property not provided" and "property set
///             to null"
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Deserialization process (Read):</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>
///             Creates a <see cref="Patcher{T}" /> instance using its private constructor via
///             <see cref="Activator.CreateInstance(Type, object[])" />
///          </description>
///       </item>
///       <item>
///          <description>Reads the JSON object token by token using <see cref="Utf8JsonReader" /></description>
///       </item>
///       <item>
///          <description>
///             For each property in the JSON:
///             <list type="bullet">
///                <item>
///                   <description>Gets the property name from the JSON token</description>
///                </item>
///                <item>
///                   <description>Resolves the corresponding <see cref="PropertyInfo" /> using reflection</description>
///                </item>
///                <item>
///                   <description>
///                      Deserializes the property value using <see cref="JsonDocument.ParseValue" /> and
///                      <see cref="JsonElement" />.Deserialize
///                   </description>
///                </item>
///                <item>
///                   <description>Adds the (PropertyInfo, value) pair to the Patcher's internal dictionary</description>
///                </item>
///             </list>
///          </description>
///       </item>
///       <item>
///          <description>Returns the populated <see cref="Patcher{T}" /> instance</description>
///       </item>
///    </list>
///    <para>
///       <strong>Serialization process (Write):</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>Writes a JSON start object token</description>
///       </item>
///       <item>
///          <description>Iterates through all properties in the Patcher's internal dictionary</description>
///       </item>
///       <item>
///          <description>
///             For each property:
///             <list type="bullet">
///                <item>
///                   <description>Writes the property name</description>
///                </item>
///                <item>
///                   <description>Serializes the property value using the Fuxion JSON extension methods</description>
///                </item>
///                <item>
///                   <description>Writes the raw JSON value</description>
///                </item>
///             </list>
///          </description>
///       </item>
///       <item>
///          <description>Writes a JSON end object token</description>
///       </item>
///    </list>
///    <para>
///       <strong>Error handling:</strong>
///    </para>
///    <para>
///       The converter includes robust error handling with descriptive exceptions:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <see cref="InvalidProgramException" /> - When Patcher instance creation fails, property name
///             can't be read, or property doesn't exist on type T
///          </description>
///       </item>
///       <item>
///          <description><see cref="JsonException" /> - When unexpected JSON tokens are encountered or serialization fails</description>
///       </item>
///    </list>
///    <para>
///       <strong>Type constraint:</strong> The generic parameter <typeparamref name="T" /> is constrained to <c>class</c>
///       (reference types)
///       because the Patcher pattern is designed for mutable reference types typically used in DTOs and domain models.
///    </para>
/// </remarks>
/// <example>
///    <strong>Basic usage with HTTP PATCH:</strong>
///    <code>
/// // Model definition
/// public class UserProfile
/// {
///     public string Name { get; set; }
///     public string Email { get; set; }
///     public int Age { get; set; }
/// }
/// 
/// // Configure JSON options with the converter factory
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new PatcherJsonConverterFactory());
/// 
/// // Deserialize a partial update JSON
/// string json = @"{""Email"": ""newemail@example.com"", ""Age"": 30}";
/// var patcher = JsonSerializer.Deserialize&lt;Patcher&lt;UserProfile&gt;&gt;(json, options);
/// 
/// // Apply the patch to an existing entity
/// var user = new UserProfile 
/// { 
///     Name = "John Doe", 
///     Email = "old@example.com", 
///     Age = 25 
/// };
/// 
/// patcher.ApplyTo(user);
/// // Result: Name = "John Doe" (unchanged), Email = "newemail@example.com" (updated), Age = 30 (updated)
/// </code>
///    <strong>Serialization (round-trip):</strong>
///    <code>
/// // Create a patcher with specific properties
/// var patcher = new Patcher&lt;UserProfile&gt;();
/// patcher.Set(x =&gt; x.Email, "newemail@example.com");
/// patcher.Set(x =&gt; x.Age, 30);
/// 
/// // Serialize to JSON
/// string json = JsonSerializer.Serialize(patcher, options);
/// // Result: {"Email":"newemail@example.com","Age":30}
/// // Note: Name property is not included because it wasn't set
/// </code>
///    <strong>Null value handling:</strong>
///    <code>
/// // JSON with explicit null
/// string json1 = @"{""Email"": null}";
/// var patcher1 = JsonSerializer.Deserialize&lt;Patcher&lt;UserProfile&gt;&gt;(json1, options);
/// 
/// // Check if property was provided
/// bool hasEmail = patcher1.HasProperty(x =&gt; x.Email); // true
/// var emailValue = patcher1.GetValue(x =&gt; x.Email);   // null
/// 
/// // JSON without the property
/// string json2 = @"{""Age"": 30}";
/// var patcher2 = JsonSerializer.Deserialize&lt;Patcher&lt;UserProfile&gt;&gt;(json2, options);
/// 
/// bool hasEmail2 = patcher2.HasProperty(x =&gt; x.Email); // false
/// // This is the key difference: we can distinguish "null" from "not provided"
/// </code>
///    <strong>Error scenarios:</strong>
///    <code>
/// // Invalid JSON property name
/// string invalidJson = @"{""NonExistentProperty"": ""value""}";
/// try
/// {
///     var patcher = JsonSerializer.Deserialize&lt;Patcher&lt;UserProfile&gt;&gt;(invalidJson, options);
/// }
/// catch (InvalidProgramException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // "The property 'NonExistentProperty' is not present in type 'UserProfile'"
/// }
/// 
/// // Serialization error
/// try
/// {
///     var brokenPatcher = new Patcher&lt;UserProfile&gt;();
///     // ... add property with problematic value ...
///     string json = JsonSerializer.Serialize(brokenPatcher, options);
/// }
/// catch (JsonException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // "Error writing 'Patcher&lt;UserProfile&gt;'.", ex.InnerException?.Message;
/// }
/// </code>
/// </example>
public class PatcherJsonConverter<T> : JsonConverter<Patcher<T>> where T : class
{
	private static JsonSerializerOptions JsonOptions
	{
		get
		{
			if (field is null)
			{
				field = new()
				{
					TypeInfoResolver = new PrivateConstructorJsonTypeInfoResolver() 
				};
				field.MakeReadOnly();
			}
			return field;
		}
	}

	/// <summary>
	///    Reads JSON and converts it to a <see cref="Patcher{T}" /> instance.
	/// </summary>
	/// <param name="reader">The <see cref="Utf8JsonReader" /> to read JSON data from.</param>
	/// <param name="typeToConvert">The type to convert (should be <see cref="Patcher{T}" />).</param>
	/// <param name="options">The serializer options to use during deserialization.</param>
	/// <returns>
	///    A <see cref="Patcher{T}" /> instance populated with properties from the JSON, or <c>null</c> if the JSON is null.
	/// </returns>
	/// <exception cref="InvalidProgramException">
	///    Thrown in the following cases:
	///    <list type="bullet">
	///       <item>
	///          <description>The <see cref="Patcher{T}" /> instance cannot be created using its private constructor</description>
	///       </item>
	///       <item>
	///          <description>A property name cannot be read from the JSON reader</description>
	///       </item>
	///       <item>
	///          <description>A property from the JSON doesn't exist on type <typeparamref name="T" /></description>
	///       </item>
	///    </list>
	/// </exception>
	/// <exception cref="JsonException">
	///    Thrown when the JSON structure is invalid (e.g., doesn't start with StartObject or has unexpected tokens).
	/// </exception>
	/// <remarks>
	///    <para>
	///       <strong>Deserialization algorithm:</strong>
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             <strong>Create Patcher instance:</strong> Uses
	///             <see cref="Activator.CreateInstance(Type, object[])" /> with <c>nonPublic: true</c> to call the private
	///             constructor
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Validate JSON start:</strong> Ensures the first token is
	///             <see cref="JsonTokenType.StartObject" />
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Read properties loop:</strong> Iterates through JSON properties until
	///             <see cref="JsonTokenType.EndObject" />
	///             <list type="bullet">
	///                <item>
	///                   <description>Reads property name from <see cref="JsonTokenType.PropertyName" /> token</description>
	///                </item>
	///                <item>
	///                   <description>
	///                      Resolves <see cref="PropertyInfo" /> using <see cref="Type" />.GetRuntimeProperty
	///                      (case-sensitive)
	///                   </description>
	///                </item>
	///                <item>
	///                   <description>
	///                      Parses property value using <see cref="JsonDocument.ParseValue" /> to get a
	///                      <see cref="JsonElement" />
	///                   </description>
	///                </item>
	///                <item>
	///                   <description>
	///                      Deserializes the <see cref="JsonElement" /> to the property's actual type using
	///                      <see cref="JsonElement" />.Deserialize(Type, JsonSerializerOptions)
	///                   </description>
	///                </item>
	///                <item>
	///                   <description>
	///                      Adds the property to the Patcher's internal dictionary:
	///                      <c>Properties.Add(propertyName, (PropertyInfo, value))</c>
	///                   </description>
	///                </item>
	///             </list>
	///          </description>
	///       </item>
	///       <item>
	///          <description><strong>Return result:</strong> Returns the populated <see cref="Patcher{T}" /> instance</description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Private constructor support:</strong>
	///    </para>
	///    <para>
	///       The deserialization of individual property values uses a <see cref="JsonSerializerOptions" /> configured with
	///       <see cref="PrivateConstructorJsonTypeInfoResolver" />. This enables deserialization of types that have private
	///       or protected constructors, which is common in domain models and DTOs.
	///    </para>
	///    <para>
	///       <strong>Property name matching:</strong>
	///    </para>
	///    <para>
	///       Property names are matched case-sensitively using <see cref="Type" />.GetRuntimeProperty. If a property in the
	///       JSON
	///       doesn't exist on type <typeparamref name="T" />, an <see cref="InvalidProgramException" /> is thrown. This
	///       ensures
	///       type safety and prevents silent failures from typos or schema mismatches.
	///    </para>
	///    <para>
	///       <strong>Token-by-token reading:</strong>
	///    </para>
	///    <para>
	///       The method uses low-level <see cref="Utf8JsonReader" /> operations for efficiency and control. Each call to
	///       <c>reader.Read()</c> advances to the next token, and token types are validated to ensure proper JSON structure.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Internal behavior demonstration
	/// // Given JSON: {"Name": "John", "Age": 30}
	/// 
	/// // Step 1: Create Patcher instance
	/// var patcher = (Patcher&lt;Person&gt;)Activator.CreateInstance(typeof(Patcher&lt;Person&gt;), true);
	/// 
	/// // Step 2: Validate StartObject
	/// // reader.TokenType == JsonTokenType.StartObject ✓
	/// 
	/// // Step 3: Read "Name" property
	/// // reader.Read() → JsonTokenType.PropertyName
	/// // propertyName = "Name"
	/// // prop = typeof(Person).GetRuntimeProperty("Name") → PropertyInfo for Name
	/// // ele = JsonDocument.ParseValue(ref reader).RootElement → "John"
	/// // val = ele.Deserialize(typeof(string), ...) → "John"
	/// // patcher.Properties.Add("Name", (prop, "John"))
	/// 
	/// // Step 4: Read "Age" property
	/// // reader.Read() → JsonTokenType.PropertyName
	/// // propertyName = "Age"
	/// // prop = typeof(Person).GetRuntimeProperty("Age") → PropertyInfo for Age
	/// // ele = JsonDocument.ParseValue(ref reader).RootElement → 30
	/// // val = ele.Deserialize(typeof(int), ...) → 30
	/// // patcher.Properties.Add("Age", (prop, 30))
	/// 
	/// // Step 5: EndObject reached
	/// // reader.Read() → JsonTokenType.EndObject
	/// // return patcher
	/// </code>
	/// </example>
	public override Patcher<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var ins = Activator.CreateInstance(typeof(Patcher<T>), true)
		          ?? throw new InvalidProgramException(
			          $"Could not be created instance of type '{typeof(Patcher<T>).GetSignature()}' using its private constructor");
		var patchable = (Patcher<T>)ins;
		if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject) return patchable;
			if (reader.TokenType != JsonTokenType.PropertyName)
				throw new JsonException("The reader expected JsonTokenType.PropertyName");
			var propertyName = reader.GetString() ??
			                   throw new InvalidProgramException(
				                   "Current property name could not be read from Utf8JsonReader.");
			var prop = typeof(T).GetRuntimeProperty(propertyName)
			           ?? throw new InvalidProgramException(
				           $"The property '{propertyName}' is not present in type '{typeof(T)}'");
			var ele = JsonDocument.ParseValue(ref reader).RootElement;
			var val = ele.Deserialize(prop.PropertyType, JsonOptions);
			patchable.Properties.Add(propertyName, (prop, val));
		}

		return patchable;
	}

	/// <summary>
	///    Writes a <see cref="Patcher{T}" /> instance to JSON.
	/// </summary>
	/// <param name="writer">The <see cref="Utf8JsonWriter" /> to write JSON data to.</param>
	/// <param name="value">The <see cref="Patcher{T}" /> instance to serialize.</param>
	/// <param name="options">The serializer options to use during serialization.</param>
	/// <exception cref="JsonException">
	///    Thrown when serialization of a property value fails. The exception message includes the type signature
	///    of the <see cref="Patcher{T}" /> being serialized and the inner exception with details about the failure.
	/// </exception>
	/// <remarks>
	///    <para>
	///       <strong>Serialization algorithm:</strong>
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             <strong>Write start:</strong> Writes <see cref="JsonTokenType.StartObject" /> token using
	///             <see cref="Utf8JsonWriter.WriteStartObject()" />
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Iterate properties:</strong> Loops through all entries in the Patcher's <c>Properties</c>
	///             dictionary
	///             <list type="bullet">
	///                <item>
	///                   <description>
	///                      Writes property name using <see cref="Utf8JsonWriter.WritePropertyName(string)" />
	///                   </description>
	///                </item>
	///                <item>
	///                   <description>
	///                      Serializes property value using Fuxion's fluent JSON extension:
	///                      <c>pvk.Value.Value.Fx.Json.Serialize()</c>
	///                   </description>
	///                </item>
	///                <item>
	///                   <description>Handles serialization result using <c>PayloadOrError</c> pattern (throws on error)</description>
	///                </item>
	///                <item>
	///                   <description>
	///                      Writes serialized value as raw JSON using
	///                      <see cref="Utf8JsonWriter.WriteRawValue(string, bool)" />
	///                   </description>
	///                </item>
	///             </list>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Write end:</strong> Writes <see cref="JsonTokenType.EndObject" /> token using
	///             <see cref="Utf8JsonWriter.WriteEndObject()" />
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Property value serialization:</strong>
	///    </para>
	///    <para>
	///       Individual property values are serialized using the Fuxion JSON extension methods (<c>.Fx.Json.Serialize()</c>).
	///       This provides:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Integration with Fuxion's Response pattern for error handling</description>
	///       </item>
	///       <item>
	///          <description>Support for custom converters configured in the <paramref name="options" /></description>
	///       </item>
	///       <item>
	///          <description>Proper handling of complex types, nulls, and nested objects</description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Raw value writing:</strong>
	///    </para>
	///    <para>
	///       The method uses <see cref="Utf8JsonWriter.WriteRawValue(string, bool)" /> to write the serialized JSON directly
	///       without
	///       additional encoding. This is efficient because the value has already been serialized to JSON by the
	///       <c>.Fx.Json.Serialize()</c> call.
	///    </para>
	///    <para>
	///       <strong>Error handling with PayloadOrError:</strong>
	///    </para>
	///    <para>
	///       The serialization uses Fuxion's Response pattern with <c>PayloadOrError</c>. If serialization of any
	///       property value fails:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>A <see cref="JsonException" /> is thrown with a descriptive message</description>
	///       </item>
	///       <item>
	///          <description>The message includes the full type signature of the Patcher being serialized</description>
	///       </item>
	///       <item>
	///          <description>The inner exception contains details about the serialization failure</description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Property ordering:</strong>
	///    </para>
	///    <para>
	///       Properties are serialized in the order they appear in the Patcher's internal dictionary, which typically
	///       reflects the order they were added during deserialization or manual patching operations.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Internal behavior demonstration
	/// // Given Patcher with: Properties = { "Name": (propInfo, "John"), "Age": (propInfo, 30) }
	/// 
	/// // Step 1: Write start object
	/// writer.WriteStartObject();
	/// // Output so far: {
	/// 
	/// // Step 2: Write "Name" property
	/// writer.WritePropertyName("Name");
	/// // "John".Fx.Json.Serialize() → "\"John\""
	/// writer.WriteRawValue("\"John\"");
	/// // Output so far: {"Name":"John"
	/// 
	/// // Step 3: Write "Age" property
	/// writer.WritePropertyName("Age");
	/// // 30.Fx.Json.Serialize() → "30"
	/// writer.WriteRawValue("30");
	/// // Output so far: {"Name":"John","Age":30
	/// 
	/// // Step 4: Write end object
	/// writer.WriteEndObject();
	/// // Final output: {"Name":"John","Age":30}
	/// </code>
	///    <strong>Error handling example:</strong>
	///    <code>
	/// var patcher = new Patcher&lt;ComplexType&gt;();
	/// patcher.Set(x =&gt; x.CircularReference, someObject);
	/// 
	/// try
	/// {
	///     string json = JsonSerializer.Serialize(patcher, options);
	/// }
	/// catch (JsonException ex)
	/// {
	///     Console.WriteLine(ex.Message);
	///     // "Error writing 'Patcher&lt;ComplexType&gt;'.",
	///     Console.WriteLine(ex.InnerException?.Message);
	///     // "A possible object cycle was detected..."
	/// }
	/// </code>
	/// </example>
	public override void Write(Utf8JsonWriter writer, Patcher<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var pvk in value.Properties)
		{
			writer.WritePropertyName(pvk.Key);
			writer.WriteRawValue(pvk.Value.Value.Fx.Json.Serialize(options: options).PayloadOrFallback(r =>
				throw new JsonException($"Error writing '{value.GetType().GetSignature()}'.", r.Exception)));
		}

		writer.WriteEndObject();
	}
}