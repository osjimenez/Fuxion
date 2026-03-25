using Fuxion.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Fuxion;

/// <summary>
/// Defines the contract for types that can represent undefined values.
/// </summary>
/// <typeparam name="T">The type of the value when defined.</typeparam>
/// <remarks>
/// This interface enables distinguishing between three states:
/// <list type="bullet">
/// <item><description><c>undefined</c>: The value has not been set or provided</description></item>
/// <item><description><c>null</c>: The value has been explicitly set to null</description></item>
/// <item><description><c>defined</c>: The value has been set to a non-null value</description></item>
/// </list>
/// This distinction is particularly useful in scenarios like JSON PATCH operations, GraphQL mutations,
/// or API updates where you need to differentiate between "don't change this field" (undefined),
/// "set this field to null" (null), and "set this field to this value" (defined).
/// </remarks>
/// <example>
/// <code>
/// public class UserUpdateDto
/// {
///     public Undefinable&lt;string&gt; Name { get; set; }
///     public Undefinable&lt;string?&gt; Email { get; set; }
/// }
/// 
/// // Usage:
/// var update = new UserUpdateDto
/// {
///     Name = "John",              // Defined with value
///     Email = null                // Explicitly set to null
///     // Age is undefined (not provided in JSON)
/// };
/// 
/// if (update.Name.IsDefined)
///     user.Name = update.Name.Value;
/// 
/// if (update.Email.IsDefined)
///     user.Email = update.Email.Value;  // Sets to null
/// </code>
/// </example>
public interface IUndefinable<out T>
{
	/// <summary>
	/// Gets the value if it is defined.
	/// </summary>
	/// <value>The defined value of type <typeparamref name="T"/>.</value>
	/// <exception cref="UndefinedException">Thrown when attempting to access the value while <see cref="IsDefined"/> is <c>false</c>.</exception>
	/// <remarks>
	/// Always check <see cref="IsDefined"/> before accessing this property, or use <see cref="ValueOrDefault"/> for safe access.
	/// </remarks>
	T Value { get; }
	
	/// <summary>
	/// Gets a value indicating whether the value has been defined (set).
	/// </summary>
	/// <value><c>true</c> if the value is defined; <c>false</c> if undefined.</value>
	/// <remarks>
	/// When <c>true</c>, the <see cref="Value"/> property can be safely accessed.
	/// When <c>false</c>, accessing <see cref="Value"/> will throw <see cref="UndefinedException"/>.
	/// </remarks>
	bool IsDefined { get; }
	
	/// <summary>
	/// Gets a value indicating whether the value is undefined (not set).
	/// </summary>
	/// <value><c>true</c> if the value is undefined; <c>false</c> if defined.</value>
	/// <remarks>
	/// This is the logical inverse of <see cref="IsDefined"/>. Provided for code readability.
	/// </remarks>
	bool IsUndefined { get; }
	
	/// <summary>
	/// Gets the value if defined, or the default value of <typeparamref name="T"/> if undefined.
	/// </summary>
	/// <value>
	/// The defined value if <see cref="IsDefined"/> is <c>true</c>; 
	/// otherwise, <c>default(T)</c>.
	/// </value>
	/// <remarks>
	/// This property provides safe access to the value without throwing exceptions.
	/// For reference types, returns <c>null</c> when undefined. For value types, returns the default value.
	/// </remarks>
	T? ValueOrDefault { get; }
}

/// <summary>
/// Represents a value that can be in one of three states: undefined, null, or defined with a value.
/// </summary>
/// <typeparam name="T">The type of the value when defined.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Undefinable{T}"/> is a readonly struct that enables distinguishing between:
/// </para>
/// <list type="number">
/// <item><description><strong>Undefined:</strong> The value has not been provided or set (useful for optional fields in updates)</description></item>
/// <item><description><strong>Null:</strong> The value has been explicitly set to null (clear/delete the field)</description></item>
/// <item><description><strong>Defined:</strong> The value has been set to a specific value (update the field)</description></item>
/// </list>
/// <para>
/// <strong>JSON Serialization:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Undefined:</strong> Serialized as <c>[null]</c> (array with single null element)</description></item>
/// <item><description><strong>Null:</strong> Serialized as <c>null</c></description></item>
/// <item><description><strong>Defined:</strong> Serialized as the value itself</description></item>
/// </list>
/// <para>
/// The special <c>[null]</c> format for undefined values is used because JSON doesn't have a native "undefined" type,
/// and this format is distinguishable from both <c>null</c> and actual array values.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>JSON PATCH operations where you need to know which fields to update</description></item>
/// <item><description>GraphQL mutations where undefined means "don't change"</description></item>
/// <item><description>REST API updates where you want to distinguish between "no value provided" and "explicitly null"</description></item>
/// <item><description>Configuration systems where undefined uses a default but null disables a feature</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Creating undefinable values
/// Undefinable&lt;string&gt; undefined = Undefinable&lt;string&gt;.Undefined;
/// Undefinable&lt;string&gt; withNull = new Undefinable&lt;string&gt;(null);
/// Undefinable&lt;string&gt; withValue = new Undefinable&lt;string&gt;("Hello");
/// 
/// // Implicit conversions
/// Undefinable&lt;int&gt; number = 42;  // Implicitly converts from int
/// int value = number;             // Implicitly converts to int (throws if undefined)
/// 
/// // Checking state
/// if (withValue.IsDefined)
///     Console.WriteLine(withValue.Value);  // Safe access
/// 
/// if (undefined.IsUndefined)
///     Console.WriteLine("No value provided");
/// 
/// // Safe access with default
/// var safeValue = undefined.ValueOrDefault;  // Returns null for reference types
/// 
/// // JSON PATCH scenario
/// public class UpdateUserDto
/// {
///     public Undefinable&lt;string&gt; Name { get; set; }
///     public Undefinable&lt;string?&gt; Email { get; set; }
///     public Undefinable&lt;int&gt; Age { get; set; }
/// }
/// 
/// // JSON: { "name": "John", "email": null }
/// // Deserialized:
/// // - Name.IsDefined = true, Name.Value = "John"
/// // - Email.IsDefined = true, Email.Value = null (explicitly set to null)
/// // - Age.IsDefined = false (undefined, not in JSON)
/// 
/// void ApplyUpdate(User user, UpdateUserDto update)
/// {
///     if (update.Name.IsDefined)
///         user.Name = update.Name.Value;  // Update name
///     
///     if (update.Email.IsDefined)
///         user.Email = update.Email.Value;  // Set to null (delete email)
///     
///     // Age is undefined, so we don't touch user.Age
/// }
/// 
/// // JSON serialization examples:
/// // Undefined: [null]
/// // Null: null
/// // "Hello": "Hello"
/// // 42: 42
/// </code>
/// </example>
[JsonConverter(typeof(UndefinableConverterFactory))]
public readonly struct Undefinable<T> : IUndefinable<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Undefinable{T}"/> struct with a defined value.
	/// </summary>
	/// <param name="value">The value to set. Can be <c>null</c> for reference types.</param>
	/// <remarks>
	/// <para>
	/// This constructor creates an <see cref="Undefinable{T}"/> in the "defined" state, where <see cref="IsDefined"/> is <c>true</c>.
	/// The value can be <c>null</c> for reference types or nullable value types, which is distinct from the "undefined" state.
	/// </para>
	/// <para>
	/// <strong>Important:</strong> This constructor must exist for JSON deserialization to work correctly.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Defined with value
	/// var withValue = new Undefinable&lt;string&gt;("Hello");
	/// Console.WriteLine(withValue.IsDefined);  // true
	/// Console.WriteLine(withValue.Value);      // "Hello"
	/// 
	/// // Defined as null (different from undefined!)
	/// var withNull = new Undefinable&lt;string&gt;(null);
	/// Console.WriteLine(withNull.IsDefined);   // true
	/// Console.WriteLine(withNull.Value);       // null
	/// 
	/// // Undefined (use static property instead)
	/// var undefined = Undefinable&lt;string&gt;.Undefined;
	/// Console.WriteLine(undefined.IsDefined);  // false
	/// </code>
	/// </example>
	public Undefinable(T value)
	{
		Value = value;
		IsDefined = true;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Undefinable{T}"/> struct with explicit control over the defined state.
	/// </summary>
	/// <param name="value">The value to store (may be ignored if <paramref name="isDefined"/> is <c>false</c>).</param>
	/// <param name="isDefined">
	/// <c>true</c> to create a defined value; <c>false</c> to create an undefined value.
	/// </param>
	/// <remarks>
	/// This private constructor is used internally to create the <see cref="Undefined"/> static property.
	/// External code should use the public constructor for defined values or <see cref="Undefined"/> for undefined values.
	/// </remarks>
	private Undefinable(T value, bool isDefined)
	{
		Value = value;
		IsDefined = isDefined;
	}

	/// <summary>
	/// Gets the value if it is defined.
	/// </summary>
	/// <value>The defined value of type <typeparamref name="T"/>.</value>
	/// <exception cref="UndefinedException">
	/// Thrown when attempting to access this property while <see cref="IsDefined"/> is <c>false</c>.
	/// </exception>
	/// <remarks>
	/// <para>
	/// <strong>Important:</strong> Always check <see cref="IsDefined"/> before accessing this property to avoid exceptions.
	/// For safe access without exceptions, use <see cref="ValueOrDefault"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var defined = new Undefinable&lt;string&gt;("Hello");
	/// Console.WriteLine(defined.Value);  // "Hello" - OK
	/// 
	/// var undefined = Undefinable&lt;string&gt;.Undefined;
	/// // Console.WriteLine(undefined.Value);  // Throws UndefinedException!
	/// 
	/// // Safe pattern:
	/// if (defined.IsDefined)
	///     Console.WriteLine(defined.Value);  // Safe
	/// </code>
	/// </example>
	public T Value
	{
		private init;
		get => !IsDefined ? throw new UndefinedException($"Value cannot be get if '{nameof(IsDefined)}' is false") : field;
	}

	/// <summary>
	/// Implicitly converts an <see cref="Undefinable{T}"/> to its underlying type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="undefinable">The undefinable value to convert.</param>
	/// <returns>The underlying value.</returns>
	/// <exception cref="UndefinedException">Thrown if the value is undefined.</exception>
	/// <remarks>
	/// This operator allows <see cref="Undefinable{T}"/> to be used directly as <typeparamref name="T"/> in assignments and method calls.
	/// However, it will throw if the value is undefined, so use with caution.
	/// </remarks>
	/// <example>
	/// <code>
	/// Undefinable&lt;int&gt; number = 42;
	/// int value = number;  // Implicit conversion - value is 42
	/// 
	/// Undefinable&lt;string&gt; text = "Hello";
	/// string str = text;  // Implicit conversion - str is "Hello"
	/// 
	/// // Be careful with undefined values
	/// Undefinable&lt;int&gt; undefined = Undefinable&lt;int&gt;.Undefined;
	/// // int badValue = undefined;  // Throws UndefinedException!
	/// </code>
	/// </example>
	public static implicit operator T(Undefinable<T> undefinable) => undefinable.Value;
	
	/// <summary>
	/// Implicitly converts a value of type <typeparamref name="T"/> to <see cref="Undefinable{T}"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>An <see cref="Undefinable{T}"/> in the defined state with the specified value.</returns>
	/// <remarks>
	/// This operator allows values to be assigned directly to <see cref="Undefinable{T}"/> properties without explicit construction.
	/// </remarks>
	/// <example>
	/// <code>
	/// // Implicit conversion from value
	/// Undefinable&lt;int&gt; number = 42;
	/// Undefinable&lt;string&gt; text = "Hello";
	/// Undefinable&lt;string?&gt; nullText = null;  // Defined as null (not undefined)
	/// 
	/// // All create defined values:
	/// Console.WriteLine(number.IsDefined);    // true
	/// Console.WriteLine(text.IsDefined);      // true
	/// Console.WriteLine(nullText.IsDefined);  // true (defined as null!)
	/// </code>
	/// </example>
	public static implicit operator Undefinable<T>(T value) => new(value);

	/// <summary>
	/// Gets a value indicating whether the value has been defined (set).
	/// </summary>
	/// <value><c>true</c> if the value is defined; <c>false</c> if undefined.</value>
	/// <remarks>
	/// This property is the primary way to determine if a value has been provided.
	/// When <c>true</c>, <see cref="Value"/> can be safely accessed. When <c>false</c>, accessing
	/// <see cref="Value"/> throws <see cref="UndefinedException"/>.
	/// </remarks>
	public bool IsDefined { get; }

	/// <summary>
	/// Gets a value indicating whether the value is undefined (not set).
	/// </summary>
	/// <value><c>true</c> if the value is undefined; <c>false</c> if defined.</value>
	/// <remarks>
	/// This property is the logical inverse of <see cref="IsDefined"/> and is provided for code readability.
	/// It is marked with <see cref="DebuggerBrowsableAttribute"/> to hide it in debugger watch windows since
	/// it provides the same information as <see cref="IsDefined"/>.
	/// </remarks>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsUndefined => !IsDefined;

	/// <summary>
	/// Gets the value if defined, or the default value of <typeparamref name="T"/> if undefined.
	/// </summary>
	/// <value>
	/// The defined value if <see cref="IsDefined"/> is <c>true</c>;
	/// otherwise, <c>default(T)</c> (which is <c>null</c> for reference types, zero for numeric types, etc.).
	/// </value>
	/// <remarks>
	/// This property provides safe access to the value without throwing exceptions.
	/// Use this when you want a fallback behavior for undefined values.
	/// </remarks>
	/// <example>
	/// <code>
	/// var defined = new Undefinable&lt;int&gt;(42);
	/// Console.WriteLine(defined.ValueOrDefault);  // 42
	/// 
	/// var undefined = Undefinable&lt;int&gt;.Undefined;
	/// Console.WriteLine(undefined.ValueOrDefault);  // 0 (default for int)
	/// 
	/// var undefinedString = Undefinable&lt;string&gt;.Undefined;
	/// Console.WriteLine(undefinedString.ValueOrDefault ?? "default");  // "default"
	/// </code>
	/// </example>
	public T? ValueOrDefault => IsDefined ? Value : default;

	/// <summary>
	/// Gets a static instance representing an undefined value.
	/// </summary>
	/// <value>An <see cref="Undefinable{T}"/> where <see cref="IsDefined"/> is <c>false</c>.</value>
	/// <remarks>
	/// Use this static property to create undefined instances instead of trying to construct them directly.
	/// </remarks>
	/// <example>
	/// <code>
	/// var undefined = Undefinable&lt;string&gt;.Undefined;
	/// Console.WriteLine(undefined.IsDefined);    // false
	/// Console.WriteLine(undefined.IsUndefined);  // true
	/// 
	/// // Common usage: initializing DTOs
	/// public class UserUpdateDto
	/// {
	///     public Undefinable&lt;string&gt; Name { get; set; } = Undefinable&lt;string&gt;.Undefined;
	/// }
	/// </code>
	/// </example>
	public static readonly Undefinable<T> Undefined = new(default!, false);

	/// <summary>
	/// Returns a string representation of the undefinable value.
	/// </summary>
	/// <returns>
	/// <c>"undefined"</c> if <see cref="IsUndefined"/> is <c>true</c>;
	/// <c>"null"</c> if the value is defined but is <c>null</c>;
	/// otherwise, the string representation of <see cref="Value"/>.
	/// </returns>
	/// <example>
	/// <code>
	/// var undefined = Undefinable&lt;string&gt;.Undefined;
	/// Console.WriteLine(undefined.ToString());  // "undefined"
	/// 
	/// var withNull = new Undefinable&lt;string&gt;(null);
	/// Console.WriteLine(withNull.ToString());  // "null"
	/// 
	/// var withValue = new Undefinable&lt;string&gt;("Hello");
	/// Console.WriteLine(withValue.ToString());  // "Hello"
	/// </code>
	/// </example>
	public override string ToString() => IsDefined ? Value?.ToString() ?? "null" : "undefined";
}

/// <summary>
/// Represents an error that occurs when attempting to access the value of an <see cref="Undefinable{T}"/> 
/// that is in the undefined state.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when accessing the <see cref="IUndefinable{T}.Value"/> property
/// of an <see cref="Undefinable{T}"/> where <see cref="IUndefinable{T}.IsDefined"/> is <c>false</c>.
/// </para>
/// <para>
/// To avoid this exception, always check <see cref="IUndefinable{T}.IsDefined"/> before accessing
/// <see cref="IUndefinable{T}.Value"/>, or use <see cref="IUndefinable{T}.ValueOrDefault"/> for safe access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var undefined = Undefinable&lt;string&gt;.Undefined;
/// 
/// // This will throw UndefinedException
/// try
/// {
///     var value = undefined.Value;
/// }
/// catch (UndefinedException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // Output: "Value cannot be get if 'IsDefined' is false"
/// }
/// 
/// // Safe patterns to avoid the exception:
/// 
/// // 1. Check IsDefined first
/// if (undefined.IsDefined)
///     var value = undefined.Value;
/// 
/// // 2. Use ValueOrDefault
/// var safeValue = undefined.ValueOrDefault;
/// 
/// // 3. Use pattern matching
/// var result = undefined.IsDefined ? undefined.Value : "default";
/// </code>
/// </example>
public class UndefinedException(string message) : Exception(message);

/// <summary>
/// JSON converter factory for <see cref="Undefinable{T}"/> types.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates appropriate <see cref="UndefinableConverter{T}"/> instances for serializing
/// and deserializing <see cref="Undefinable{T}"/> values to and from JSON.
/// </para>
/// <para>
/// <strong>JSON Format:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Undefined:</strong> <c>[null]</c> - An array containing a single null element</description></item>
/// <item><description><strong>Null:</strong> <c>null</c> - The JSON null value</description></item>
/// <item><description><strong>Defined:</strong> The value itself (e.g., <c>"Hello"</c>, <c>42</c>, <c>{"name":"John"}</c>)</description></item>
/// </list>
/// <para>
/// The <c>[null]</c> format for undefined is chosen because:
/// </para>
/// <list type="number">
/// <item><description>JSON doesn't have a native "undefined" type</description></item>
/// <item><description>It's distinguishable from actual <c>null</c> values</description></item>
/// <item><description>It's distinguishable from actual array values (which would have different structure)</description></item>
/// <item><description>It's compact and easily recognizable in JSON payloads</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class TestDto
/// {
///     public Undefinable&lt;string&gt; Name { get; set; }
///     public Undefinable&lt;int&gt; Age { get; set; }
///     public Undefinable&lt;string?&gt; Email { get; set; }
/// }
/// 
/// // JSON serialization examples:
/// 
/// // All undefined
/// var dto1 = new TestDto();
/// // JSON: { "name": [null], "age": [null], "email": [null] }
/// 
/// // Mixed states
/// var dto2 = new TestDto
/// {
///     Name = "John",      // Defined
///     Age = 30,           // Defined
///     Email = null        // Defined as null (not undefined!)
/// };
/// // JSON: { "name": "John", "age": 30, "email": null }
/// 
/// // Deserialization:
/// // JSON: { "name": "Jane" }
/// // Result: Name.IsDefined = true, Age.IsDefined = false, Email.IsDefined = false
/// </code>
/// </example>
public class UndefinableConverterFactory : JsonConverterFactory
{
	/// <summary>
	/// Determines whether this factory can convert the specified type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns><c>true</c> if the type is <see cref="Undefinable{T}"/>; otherwise, <c>false</c>.</returns>
	public override bool CanConvert(Type type) => type.IsSubclassOfGenericDefinition(typeof(Undefinable<>));

	/// <summary>
	/// Creates a converter instance for the specified <see cref="Undefinable{T}"/> type.
	/// </summary>
	/// <param name="type">The type to create a converter for.</param>
	/// <param name="options">The serializer options.</param>
	/// <returns>A <see cref="UndefinableConverter{T}"/> instance for the specified type.</returns>
	public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
		=> (JsonConverter?)Activator.CreateInstance(typeof(UndefinableConverter<>).MakeGenericType(type.GetGenericArguments()[0])) ?? null;
}

/// <summary>
/// JSON converter for <see cref="Undefinable{T}"/> that handles serialization and deserialization.
/// </summary>
/// <typeparam name="T">The underlying value type of the <see cref="Undefinable{T}"/>.</typeparam>
/// <remarks>
/// <para>
/// This converter implements the special serialization format for <see cref="Undefinable{T}"/>:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Undefined:</strong> Serializes to <c>[null]</c>, deserializes from <c>[null]</c></description></item>
/// <item><description><strong>Null:</strong> Serializes to <c>null</c>, deserializes from <c>null</c></description></item>
/// <item><description><strong>Defined:</strong> Serializes to the value, deserializes from the value</description></item>
/// </list>
/// <para>
/// The converter maintains a cache of recognized <see cref="Undefinable{T}"/> types for performance.
/// </para>
/// </remarks>
public class UndefinableConverter<T> : JsonConverter<Undefinable<T?>>
{
	/// <summary>
	/// Cache of types that have been confirmed as <see cref="Undefinable{T}"/> types.
	/// </summary>
	static readonly Dictionary<Type, bool> UndefinableTypes = new();

	/// <summary>
	/// Determines whether this converter can handle the specified type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns><c>true</c> if the type is <see cref="Undefinable{T}"/>; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method caches positive results to avoid repeated reflection calls for type checking.
	/// </remarks>
	public override bool CanConvert(Type type)
	{
		if (UndefinableTypes.ContainsKey(type)) return true;
		if (type.IsSubclassOfGenericDefinition(typeof(Undefinable<>)))
		{
			UndefinableTypes.Add(type, true);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Reads and converts JSON to an <see cref="Undefinable{T}"/>
	/// </summary>
	/// <param name="reader">The reader to read JSON from.</param>
	/// <param name="typeToConvert">The type to convert to.</param>
	/// <param name="options">The serializer options.</param>
	/// <returns>
	/// An <see cref="Undefinable{T}"/> representing the deserialized value:
	/// <list type="bullet">
	/// <item><description><c>[null]</c> ? <see cref="Undefinable{T}.Undefined"/></description></item>
	/// <item><description><c>null</c> ? Defined with null value</description></item>
	/// <item><description>Any other value ? Defined with that value</description></item>
	/// </list>
	/// </returns>
	/// <remarks>
	/// The special <c>[null]</c> format (array with single null element) is used to represent undefined values
	/// since JSON doesn't have a native undefined type.
	/// </remarks>
	public override Undefinable<T?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			var node = JsonNode.Parse(ref reader);
			if (node is JsonArray ja && ja.Count == 1 && ja[0] == null) return Undefinable<T?>.Undefined;
			return new(node.Deserialize<T>(options));
		}

		return new(JsonSerializer.Deserialize<T>(ref reader, options));
	}

	/// <summary>
	/// Writes an <see cref="Undefinable{T}"/> as JSON.
	/// </summary>
	/// <param name="writer">The writer to write JSON to.</param>
	/// <param name="value">The <see cref="Undefinable{T}"/> to serialize.</param>
	/// <param name="options">The serializer options.</param>
	/// <remarks>
	/// <para>
	/// Serialization format:
	/// </para>
	/// <list type="bullet">
	/// <item><description>If <see cref="IUndefinable{T}.IsUndefined"/> is <c>true</c> ? writes <c>[null]</c></description></item>
	/// <item><description>If <see cref="IUndefinable{T}.IsDefined"/> is <c>true</c> ? writes the actual value (which may be null)</description></item>
	/// </list>
	/// </remarks>
	public override void Write(Utf8JsonWriter writer, Undefinable<T?> value, JsonSerializerOptions options)
	{
		if (value.IsUndefined)
		{
			writer.WriteStartArray();
			writer.WriteNullValue();
			writer.WriteEndArray();
		} else
			JsonSerializer.Serialize(writer, value.Value, options);
	}
}