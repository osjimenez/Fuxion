using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Fuxion.Reflection;
using Fuxion.Text.Json;

namespace Fuxion.Text.Json.Serialization;

/// <summary>
/// Base class for implementing custom property serialization fallback logic in <see cref="FallbackConverter{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Property fallback resolvers provide a pluggable mechanism to customize how specific properties are serialized
/// when the standard JSON serialization fails or requires special handling. Each resolver can:
/// </para>
/// <list type="number">
/// <item><description>Match specific properties based on their type, value, or metadata</description></item>
/// <item><description>Execute custom serialization logic for matched properties</description></item>
/// <item><description>Chain to other resolvers for nested serialization</description></item>
/// </list>
/// <para>
/// <strong>Built-in resolvers:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IfNullWritePropertyFallbackResolver"/>: Handles null values</description></item>
/// <item><description><see cref="IfMemberInfoWriteNamePropertyFallbackResolver"/>: Serializes MemberInfo as signatures</description></item>
/// <item><description><see cref="CollectionPropertyFallbackResolver"/>: Serializes collections element by element</description></item>
/// <item><description><see cref="MultilineStringToCollectionPropertyFallbackResolver"/>: Converts multiline strings to arrays</description></item>
/// <item><description><see cref="StackTraceFallbackResolver"/>: Formats exception stack traces as structured arrays</description></item>
/// </list>
/// <para>
/// <strong>Resolver execution order:</strong> Resolvers are evaluated in the order they are provided to the converter.
/// The first matching resolver handles the property serialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom resolver example
/// public class CustomPropertyResolver : PropertyFallbackResolver
/// {
///     public override bool Match(object value, PropertyInfo propertyInfo)
///     {
///         // Match properties of a specific type
///         return propertyInfo.PropertyType == typeof(MyCustomType);
///     }
///     
///     public override void Do(object value, PropertyInfo propertyInfo, 
///         Utf8JsonWriter writer, JsonSerializerOptions options, 
///         List&lt;PropertyFallbackResolver&gt; resolvers)
///     {
///         writer.WritePropertyName(propertyInfo.Name);
///         var customValue = propertyInfo.GetValue(value) as MyCustomType;
///         writer.WriteStringValue(customValue?.ToString() ?? "null");
///     }
/// }
/// 
/// // Using the custom resolver
/// var converter = new FallbackConverter&lt;MyObject&gt;(0, new CustomPropertyResolver());
/// </code>
/// </example>
public abstract class PropertyFallbackResolver
{
	/// <summary>
	/// Gets or sets the current nesting depth level for this resolver.
	/// </summary>
	/// <value>The depth level, starting from 0 for the root object.</value>
	/// <remarks>
	/// This property is set internally by <see cref="FallbackConverter{T}"/> and is used to
	/// prevent infinite recursion by limiting the depth of nested object serialization.
	/// The default maximum depth is controlled by the converter's logic (typically 2-3 levels).
	/// </remarks>
	internal int Deep { get; set; }

	/// <summary>
	/// Determines whether this resolver should handle the serialization of the specified property.
	/// </summary>
	/// <param name="value">The object instance containing the property.</param>
	/// <param name="propertyInfo">The property metadata to evaluate.</param>
	/// <returns><c>true</c> if this resolver should handle the property; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// <para>
	/// This method is called for each property during serialization. Implement matching logic based on:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Property type (<c>propertyInfo.PropertyType</c>)</description></item>
	/// <item><description>Property value (<c>propertyInfo.GetValue(value)</c>)</description></item>
	/// <item><description>Property name (<c>propertyInfo.Name</c>)</description></item>
	/// <item><description>Custom attributes on the property</description></item>
	/// <item><description>Declaring type context (<c>value.GetType()</c>)</description></item>
	/// </list>
	/// <para>
	/// <strong>Performance tip:</strong> Keep matching logic lightweight as it's called for every property.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Match by property type
	/// public override bool Match(object value, PropertyInfo propertyInfo)
	/// {
	///     return propertyInfo.PropertyType == typeof(DateTime);
	/// }
	/// 
	/// // Match by property value
	/// public override bool Match(object value, PropertyInfo propertyInfo)
	/// {
	///     return propertyInfo.GetValue(value) is ICollection;
	/// }
	/// 
	/// // Match by name and type combination
	/// public override bool Match(object value, PropertyInfo propertyInfo)
	/// {
	///     return propertyInfo.Name == "StackTrace" 
	///         &amp;&amp; value is Exception;
	/// }
	/// </code>
	/// </example>
	public abstract bool Match(object value, PropertyInfo propertyInfo);

	/// <summary>
	/// Executes the custom serialization logic for the matched property.
	/// </summary>
	/// <param name="value">The object instance containing the property.</param>
	/// <param name="propertyInfo">The property to serialize.</param>
	/// <param name="writer">The JSON writer to output to.</param>
	/// <param name="options">The current serializer options.</param>
	/// <param name="resolvers">The list of available resolvers for nested serialization.</param>
	/// <remarks>
	/// <para>
	/// This method is responsible for writing both the property name and its value to the JSON writer.
	/// Common patterns include:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Write property name: <c>writer.WritePropertyName(propertyInfo.Name)</c></description></item>
	/// <item><description>Write simple value: <c>writer.WriteStringValue(...)</c>, <c>writer.WriteNumberValue(...)</c></description></item>
	/// <item><description>Write null: <c>writer.WriteNull(propertyInfo.Name)</c></description></item>
	/// <item><description>Write complex value: <c>FallbackWriteRaw(...)</c> for nested serialization</description></item>
	/// <item><description>Write array: <c>writer.WriteStartArray()</c>, write elements, <c>writer.WriteEndArray()</c></description></item>
	/// </list>
	/// <para>
	/// Use <see cref="FallbackWriteRaw"/> to delegate serialization of nested objects back to the converter,
	/// allowing resolvers to be applied recursively.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Simple value serialization
	/// public override void Do(object value, PropertyInfo propertyInfo, 
	///     Utf8JsonWriter writer, JsonSerializerOptions options, 
	///     List&lt;PropertyFallbackResolver&gt; resolvers)
	/// {
	///     writer.WritePropertyName(propertyInfo.Name);
	///     var propValue = propertyInfo.GetValue(value);
	///     writer.WriteStringValue(propValue?.ToString() ?? "null");
	/// }
	/// 
	/// // Array serialization
	/// public override void Do(object value, PropertyInfo propertyInfo, 
	///     Utf8JsonWriter writer, JsonSerializerOptions options, 
	///     List&lt;PropertyFallbackResolver&gt; resolvers)
	/// {
	///     writer.WritePropertyName(propertyInfo.Name);
	///     writer.WriteStartArray();
	///     var collection = propertyInfo.GetValue(value) as ICollection;
	///     foreach (var item in collection)
	///     {
	///         FallbackWriteRaw(item, writer, options, resolvers);
	///     }
	///     writer.WriteEndArray();
	/// }
	/// </code>
	/// </example>
	public abstract void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers);

	/// <summary>
	/// Serializes a nested object using the fallback converter system recursively.
	/// </summary>
	/// <param name="value">The object to serialize.</param>
	/// <param name="writer">The JSON writer to output to.</param>
	/// <param name="options">The current serializer options.</param>
	/// <param name="resolvers">The list of resolvers to apply to nested properties.</param>
	/// <remarks>
	/// <para>
	/// This protected helper method enables resolvers to delegate serialization of complex nested objects
	/// back to the <see cref="FallbackConverter{T}"/> system, ensuring consistent resolver application
	/// throughout the object graph.
	/// </para>
	/// <para>
	/// The method creates a new converter instance at depth <c>Deep + 1</c> to prevent infinite recursion.
	/// </para>
	/// </remarks>
	protected void FallbackWriteRaw(object value, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		var converterType = typeof(FallbackConverter<>).MakeGenericType(value.GetType());
		var converter = Activator.CreateInstance(converterType, Deep + 1, resolvers.ToArray());
		converterType.GetMethod(nameof(FallbackConverter<>.FallbackWriteRaw))?.Invoke(converter, [value, writer, options, resolvers]);
	}
}

/// <summary>
/// Resolver that serializes null property values as JSON <c>null</c>.
/// </summary>
/// <remarks>
/// This is a default resolver automatically added to <see cref="FallbackConverter{T}"/> if not explicitly provided.
/// It ensures that null values are properly represented in the JSON output rather than causing serialization errors.
/// </remarks>
/// <example>
/// <code>
/// // JSON output example:
/// // {
/// //   "name": "John",
/// //   "email": null,
/// //   "phone": "555-1234"
/// // }
/// </code>
/// </example>
public class IfNullWritePropertyFallbackResolver : PropertyFallbackResolver
{
	/// <summary>
	/// Matches properties with null values.
	/// </summary>
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is null;

	/// <summary>
	/// Writes the property as JSON <c>null</c>.
	/// </summary>
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers) =>
		writer.WriteNull(propertyInfo.Name);
}

/// <summary>
/// Resolver that serializes <see cref="MemberInfo"/> properties (methods, properties, fields, types) as their signature strings.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MemberInfo"/> objects cannot be directly serialized to JSON. This resolver converts them to human-readable
/// signature strings that include access modifiers, return types, declaring types, parameters, etc.
/// </para>
/// <para>
/// This is particularly useful when serializing reflection-based metadata, diagnostic information, or expression trees.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example output for a MethodInfo:
/// // {
/// //   "method": "public string MyClass.MyMethod(int value, string name)"
/// // }
/// 
/// // Example output for a PropertyInfo:
/// // {
/// //   "property": "Name"
/// // }
/// </code>
/// </example>
public class IfMemberInfoWriteNamePropertyFallbackResolver : PropertyFallbackResolver
{
	/// <summary>
	/// Matches properties of type <see cref="MemberInfo"/> or its derived types.
	/// </summary>
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is MemberInfo;

	/// <summary>
	/// Writes the <see cref="MemberInfo"/> as its signature string.
	/// For <see cref="MethodBase"/> (methods, constructors), writes the full signature.
	/// For other members, writes just the name.
	/// </summary>
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		writer.WritePropertyName(propertyInfo.Name);
		var mi = propertyInfo.GetValue(value) as MemberInfo;
		if (mi is MethodBase mb)
			writer.WriteRawValue($"\"{mb.GetSignature(
				includeAccessModifiers: true,
				includeReturn: true,
				includeDeclaringType: true,
				useFullNames: true,
				fullNamesOnlyInMethodName: true,
				includeParameters: true,
				includeParametersNames: true)}\"");
		else
			writer.WriteRawValue($"\"{mi?.Name}\"");
	}
}

/// <summary>
/// Resolver that serializes <see cref="ICollection"/> properties as JSON arrays, handling each element recursively.
/// </summary>
/// <remarks>
/// <para>
/// This is a default resolver automatically added to <see cref="FallbackConverter{T}"/> if not explicitly provided.
/// It ensures collections are properly serialized as JSON arrays even when standard serialization fails.
/// </para>
/// <para>
/// Each element in the collection is serialized using <see cref="PropertyFallbackResolver.FallbackWriteRaw"/>,
/// allowing resolvers to be applied to nested objects within the collection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Input: List&lt;User&gt; { new User("John"), new User("Jane") }
/// // Output:
/// // {
/// //   "users": [
/// //     { "name": "John" },
/// //     { "name": "Jane" }
/// //   ]
/// // }
/// </code>
/// </example>
public class CollectionPropertyFallbackResolver : PropertyFallbackResolver
{
	/// <summary>
	/// Matches properties that implement <see cref="ICollection"/>.
	/// </summary>
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is ICollection;

	/// <summary>
	/// Writes the collection as a JSON array, serializing each element using the fallback system.
	/// </summary>
	/// <exception cref="InvalidProgramException">Thrown if the property value is unexpectedly null after matching.</exception>
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (propertyInfo.GetValue(value) is not ICollection collection) throw new InvalidProgramException("Collection cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();
		//var writeComma = false;
		foreach (var item in collection)
		{
			//if (writeComma) writer.WriteRawValue($",");
			//writeComma = true;
			FallbackWriteRaw(item, writer, options, resolvers);
		}
		writer.WriteEndArray();
	}
}

/// <summary>
/// Resolver that converts multiline string properties (containing <c>\r</c>) into JSON arrays with one element per line.
/// </summary>
/// <remarks>
/// <para>
/// This resolver is particularly useful for serializing text properties that naturally span multiple lines
/// (like stack traces, log messages, or formatted text) in a more readable JSON structure.
/// </para>
/// <para>
/// Lines are split using <see cref="StringExtensions.SplitInLines"/>, which handles Windows (CRLF),
/// Unix (LF), and Mac (CR) line endings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Input: "Line 1\r\nLine 2\r\nLine 3"
/// // Output:
/// // {
/// //   "text": [
/// //     "Line 1",
/// //     "Line 2",
/// //     "Line 3"
/// //   ]
/// // }
/// </code>
/// </example>
public class MultilineStringToCollectionPropertyFallbackResolver : PropertyFallbackResolver
{
	/// <summary>
	/// Matches string properties that contain carriage return characters (<c>\r</c>).
	/// </summary>
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is string s && s.Contains('\r');

	/// <summary>
	/// Splits the multiline string into lines and writes them as a JSON array.
	/// </summary>
	/// <exception cref="InvalidProgramException">Thrown if the property value is unexpectedly null after matching.</exception>
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (propertyInfo.GetValue(value) is not string str) throw new InvalidProgramException("str cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();
		foreach (var item in str.SplitInLines()) writer.WriteStringValue(item);
		writer.WriteEndArray();
	}
}

/// <summary>
/// Resolver that formats exception stack traces as structured JSON arrays with method signatures, file names, and line numbers.
/// </summary>
/// <remarks>
/// <para>
/// This resolver specifically targets the <c>StackTrace</c> property of <see cref="Exception"/> objects,
/// converting the raw stack trace string into a structured format that's easier to parse and display.
/// </para>
/// <para>
/// Each stack frame is represented as an object with:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Method:</strong> Full method signature with access modifiers, return type, declaring type, parameters</description></item>
/// <item><description><strong>File:</strong> Source file path (if available from PDB)</description></item>
/// <item><description><strong>Line:</strong> Line number in source file (if available from PDB)</description></item>
/// </list>
/// <para>
/// <strong>Note:</strong> File and line information requires debug symbols (.pdb files) to be available.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Output format:
/// // {
/// //   "stackTrace": [
/// //     {
/// //       "method": "public void MyClass.MyMethod(int value)",
/// //       "file": "C:\\Projects\\MyClass.cs",
/// //       "line": 42
/// //     },
/// //     {
/// //       "method": "private void Program.Main(string[] args)",
/// //       "file": "C:\\Projects\\Program.cs",
/// //       "line": 15
/// //     }
/// //   ]
/// // }
/// </code>
/// </example>
public class StackTraceFallbackResolver : PropertyFallbackResolver
{
	/// <summary>
	/// Matches the <c>StackTrace</c> property of <see cref="Exception"/> objects that have a string value.
	/// </summary>
	public override bool Match(object value, PropertyInfo propertyInfo)
		=> value is Exception && propertyInfo.Name == "StackTrace" && propertyInfo.GetValue(value) is string;

	/// <summary>
	/// Writes the stack trace as a structured JSON array with method signatures, file names, and line numbers.
	/// </summary>
	/// <exception cref="InvalidProgramException">Thrown if the value is not an <see cref="Exception"/>.</exception>
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (value is not Exception ex) throw new InvalidProgramException("ex cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();

		var trace = new StackTrace(ex, true); // 'true' pide info de archivo si hay .pdb

		foreach (var frame in trace.GetFrames() ?? [])
		{
			JsonSerializer.Serialize(writer, new StackFrameEntry
			{
				Method = frame.GetMethod()?.GetSignature(
					includeAccessModifiers: true,
					includeReturn: true,
					includeDeclaringType: true,
					useFullNames: true,
					fullNamesOnlyInMethodName: true,
					includeParameters: true,
					includeParametersNames: true),
				File = frame.GetFileName(),
				Line = frame.GetFileLineNumber()
			}, options);
		}
		writer.WriteEndArray();
	}

	/// <summary>
	/// Represents a single stack frame entry with method, file, and line information.
	/// </summary>
	class StackFrameEntry
	{
		/// <summary>
		/// Gets or sets the method signature. Omitted from JSON if null.
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Method { get; set; }

		/// <summary>
		/// Gets or sets the source file path. Omitted from JSON if null.
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? File { get; set; }

		/// <summary>
		/// Gets or sets the line number in the source file. Omitted from JSON if null or 0.
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? Line
		{
			get => field;
			set => field = value == 0 ? null : value;
		}
	}
}

/// <summary>
/// Specialized converter for <see cref="Exception"/> objects with stack trace formatting and multiline string handling.
/// </summary>
/// <remarks>
/// <para>
/// This converter is pre-configured with:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="StackTraceFallbackResolver"/>: Formats exception stack traces as structured arrays</description></item>
/// <item><description><see cref="MultilineStringToCollectionPropertyFallbackResolver"/>: Converts multiline exception messages to arrays</description></item>
/// </list>
/// <para>
/// Use this converter when serializing exceptions to JSON for logging, diagnostics, or error responses.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register globally
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new ExceptionConverter());
/// 
/// try
/// {
///     throw new InvalidOperationException("Something went wrong");
/// }
/// catch (Exception ex)
/// {
///     var json = JsonSerializer.Serialize(ex, options);
///     // Output includes formatted stack trace and properties
/// }
/// </code>
/// </example>
public class ExceptionConverter() : FallbackConverter<Exception>(0,
	new StackTraceFallbackResolver(),
	new MultilineStringToCollectionPropertyFallbackResolver()
	);

/// <summary>
/// JSON converter with fallback serialization logic for complex types that may fail standard serialization.
/// </summary>
/// <typeparam name="T">The type of object to serialize.</typeparam>
/// <remarks>
/// <para>
/// <see cref="FallbackConverter{T}"/> provides robust JSON serialization with the following strategy:
/// </para>
/// <list type="number">
/// <item><description><strong>Attempt standard serialization:</strong> Try using System.Text.Json's default serializer</description></item>
/// <item><description><strong>Apply resolvers on failure:</strong> If serialization fails, use <see cref="PropertyFallbackResolver"/>s to handle problematic properties</description></item>
/// <item><description><strong>Prevent circular references:</strong> Uses <see cref="ReferenceHandler.IgnoreCycles"/> to avoid infinite loops</description></item>
/// <item><description><strong>Limit depth:</strong> Restricts nesting to prevent stack overflow (MaxDepth = 6, resolver depth ? 2)</description></item>
/// </list>
/// <para>
/// <strong>Default resolvers</strong> (auto-added if not provided):
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IfNullWritePropertyFallbackResolver"/>: Handles null values</description></item>
/// <item><description><see cref="IfMemberInfoWriteNamePropertyFallbackResolver"/>: Serializes reflection metadata</description></item>
/// <item><description><see cref="CollectionPropertyFallbackResolver"/>: Handles collections</description></item>
/// </list>
/// <para>
/// <strong>Use cases:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Serializing exception objects with stack traces</description></item>
/// <item><description>Handling objects with circular references</description></item>
/// <item><description>Serializing types that don't support standard JSON serialization (reflection types, complex graphs)</description></item>
/// <item><description>Applying custom formatting to specific properties (e.g., multiline strings as arrays)</description></item>
/// </list>
/// <para>
/// <strong>Important:</strong> This converter only supports serialization. Deserialization throws <see cref="NotSupportedException"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage
/// var converter = new FallbackConverter&lt;MyClass&gt;(0);
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(converter);
/// 
/// // With custom resolvers
/// var customConverter = new FallbackConverter&lt;MyClass&gt;(0,
///     new CustomResolver1(),
///     new CustomResolver2()
/// );
/// 
/// // Exception serialization (pre-configured)
/// var exceptionConverter = new ExceptionConverter();
/// options.Converters.Add(exceptionConverter);
/// 
/// try
/// {
///     // Some code that throws
/// }
/// catch (Exception ex)
/// {
///     var json = JsonSerializer.Serialize(ex, options);
///     // Produces structured JSON with formatted stack trace
/// }
/// 
/// // Handling circular references
/// var obj = new MyClass();
/// obj.Parent = obj;  // Circular reference
/// var json = JsonSerializer.Serialize(obj, options);
/// // Serializes without infinite loop
/// </code>
/// </example>
public class FallbackConverter<T> : JsonConverter<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FallbackConverter{T}"/> class with default resolvers.
	/// </summary>
	/// <param name="deep">The current nesting depth level (typically 0 for root objects).</param>
	public FallbackConverter(int deep) : this(deep, []) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="FallbackConverter{T}"/> class with custom resolvers.
	/// </summary>
	/// <param name="deep">The current nesting depth level.</param>
	/// <param name="resolvers">Custom resolvers to apply. Default resolvers are automatically added if not present.</param>
	/// <remarks>
	/// <para>
	/// The following resolvers are automatically added if not explicitly provided:
	/// </para>
	/// <list type="bullet">
	/// <item><description><see cref="IfNullWritePropertyFallbackResolver"/></description></item>
	/// <item><description><see cref="IfMemberInfoWriteNamePropertyFallbackResolver"/></description></item>
	/// <item><description><see cref="CollectionPropertyFallbackResolver"/></description></item>
	/// </list>
	/// </remarks>
	public FallbackConverter(int deep, params PropertyFallbackResolver[] resolvers)
	{
		if (!resolvers.OfType<IfNullWritePropertyFallbackResolver>().Any()) this.resolvers.Add(new IfNullWritePropertyFallbackResolver { Deep = deep });
		if (!resolvers.OfType<IfMemberInfoWriteNamePropertyFallbackResolver>().Any()) this.resolvers.Add(new IfMemberInfoWriteNamePropertyFallbackResolver { Deep = deep });
		if (!resolvers.OfType<CollectionPropertyFallbackResolver>().Any()) this.resolvers.Add(new CollectionPropertyFallbackResolver { Deep = deep });
		this.resolvers.AddRange(resolvers.Do(t => t.Deep = deep));
		this.deep = deep;
	}
	readonly int deep = 0;
	readonly List<PropertyFallbackResolver> resolvers = [];

	/// <summary>
	/// Reads and converts JSON to an object. Not supported by this converter.
	/// </summary>
	/// <exception cref="NotSupportedException">Always thrown as this converter only supports serialization.</exception>
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		throw new NotSupportedException($"{nameof(FallbackConverter<>)} doesn't support deserialization");

	/// <summary>
	/// Writes an object as JSON using fallback logic when standard serialization fails.
	/// </summary>
	/// <param name="writer">The JSON writer to output to.</param>
	/// <param name="value">The object to serialize.</param>
	/// <param name="options">The serializer options.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
	/// <remarks>
	/// <para>
	/// <strong>Serialization strategy:</strong>
	/// </para>
	/// <list type="number">
	/// <item><description>Create modified options with <see cref="ReferenceHandler.IgnoreCycles"/> and MaxDepth = 6</description></item>
	/// <item><description>Remove this converter from options to prevent infinite recursion</description></item>
	/// <item><description>Attempt standard serialization using System.Text.Json</description></item>
	/// <item><description>On failure, iterate through properties and apply resolvers</description></item>
	/// <item><description>For unmatched properties, use <see cref="FallbackWriteRaw"/> for nested serialization</description></item>
	/// </list>
	/// <para>
	/// <strong>Circular reference handling:</strong> Uses <see cref="ReferenceHandler.IgnoreCycles"/> to detect
	/// and skip circular references rather than throwing exceptions.
	/// </para>
	/// </remarks>
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		//try
		//{
		JsonSerializerOptions opt = new(options);
		var con = opt.Converters.FirstOrDefault(c => c is FallbackConverter<T>);
		if (con is not null) opt.Converters.Remove(con);
		//opt.ReferenceHandler = ReferenceHandler.Preserve;
		opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
		opt.MaxDepth = 6;
		var json = value.Fx.Json.Serialize(options: opt);
		if (json.IsSuccess)
			writer.WriteRawValue(json.Payload);
		else
		{
			writer.WriteStartObject();
			foreach (var prop in value.GetType().GetProperties())
			{
				var resolved = false;
				foreach (var resolver in resolvers)
					if (resolver.Match(value, prop))
					{
						resolver.Do(value, prop, writer, options, resolvers);
						resolved = true;
						break;
					}
				if (resolved) continue;
				writer.WritePropertyName(prop.Name);
				FallbackWriteRaw(prop.GetValue(value) ?? throw new NullReferenceException($"The value of property '{prop.Name}' is null"), writer, options, resolvers);
			}
			writer.WriteEndObject();
		}
		//} catch
		//{
		//	writer.WriteStartObject();
		//	foreach (var prop in value.GetType().GetProperties())
		//	{
		//		var resolved = false;
		//		foreach (var resolver in resolvers)
		//			if (resolver.Match(value, prop))
		//			{
		//				resolver.Do(value, prop, writer, options, resolvers);
		//				resolved = true;
		//				break;
		//			}
		//		if (resolved) continue;
		//		writer.WritePropertyName(prop.Name);
		//		FallbackWriteRaw(prop.GetValue(value) ?? throw new NullReferenceException($"The value of property '{prop.Name}' is null"), writer, options, resolvers);
		//	}
		//	writer.WriteEndObject();
		//}
	}

	/// <summary>
	/// Serializes a nested object using fallback logic with depth limiting.
	/// </summary>
	/// <param name="value">The object to serialize (can be null).</param>
	/// <param name="writer">The JSON writer to output to.</param>
	/// <param name="options">The serializer options.</param>
	/// <param name="resolvers">The list of resolvers to apply.</param>
	/// <remarks>
	/// <para>
	/// This public method enables property resolvers to serialize nested objects. It implements depth limiting
	/// to prevent infinite recursion:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Depth ? 2:</strong> Creates a new <see cref="FallbackConverter{T}"/> at depth + 1 and applies resolvers</description></item>
	/// <item><description><strong>Depth > 2:</strong> Uses standard serialization without converter to prevent deep nesting</description></item>
	/// </list>
	/// <para>
	/// On serialization error, writes an error message string instead of throwing an exception.
	/// </para>
	/// </remarks>
	public void FallbackWriteRaw(object? value, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		//try
		//{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}
		JsonSerializerOptions opt = new(options);
		if (deep <= 2) // INFO: Con 2 funciona, con 3 a veces, con 4 casi nunca
		{
			var converterType = typeof(FallbackConverter<>).MakeGenericType(value.GetType());
			var converter = Activator.CreateInstance(converterType, deep + 1, resolvers.ToArray()) ?? throw new InvalidProgramException($"Program couldn't create FallbackConverter<{value.GetType().Name}>");
			opt.Converters.Add((JsonConverter)converter);
		}
		//opt.ReferenceHandler = ReferenceHandler.Preserve;
		opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
		opt.MaxDepth = 6;
		var json = value.Fx.Json.Serialize(options: opt);
		if (json.IsSuccess)
			writer.WriteRawValue(json.Payload);
		else
			writer.WriteRawValue($"\"ERROR '{json.Exception?.Message}'\"");
		//} catch (Exception ex)
		//{
		//	writer.WriteRawValue($"\"ERROR '{ex.Message}'\"");
		//}
	}
}