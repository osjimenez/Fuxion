using Fuxion.Reflection;
using Fuxion.Text.Json;
using Fuxion.Text.Json.Serialization;
using Fuxion.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Fuxion.Text.Json;

/// <summary>
/// Provides extension methods for JSON serialization and deserialization using the Fuxion fluent API pattern.
/// </summary>
/// <remarks>
/// <para>
/// This static class contains extension methods that enable fluent-style JSON operations through the <c>.Fx.Json</c> syntax.
/// The extensions provide:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Serialization:</strong> Convert objects to JSON strings, <see cref="JsonNode"/>, or <see cref="JsonElement"/></description></item>
/// <item><description><strong>Deserialization:</strong> Convert JSON strings to strongly-typed objects</description></item>
/// <item><description><strong>Response pattern integration:</strong> All operations return <see cref="Response{T}"/> for consistent error handling</description></item>
/// <item><description><strong>Formatted output:</strong> Built-in support for Fuxion's formatted JSON style (indented with tabs, allows trailing commas, skips comments)</description></item>
/// <item><description><strong>Exception serialization:</strong> Specialized handling for <see cref="Exception"/> objects with automatic converter injection</description></item>
/// <item><description><strong>Private constructor support:</strong> Automatically handles types with private constructors during deserialization</description></item>
/// </list>
/// <para>
/// <strong>Fluent API pattern:</strong>
/// </para>
/// <para>
/// The extensions use the Fuxion extension pattern: <c>value.Fx.Json.Operation()</c> where:
/// </para>
/// <list type="bullet">
/// <item><description><c>.Fx</c> - Entry point to Fuxion extensions (from <see cref="FuxionExtensions{T}"/>)</description></item>
/// <item><description><c>.Json</c> - Accesses the JSON operations wrapper</description></item>
/// <item><description><c>.Operation()</c> - Performs serialize/deserialize operations</description></item>
/// </list>
/// </remarks>
/// <example>
/// <strong>Basic serialization:</strong>
/// <code>
/// var person = new Person { Name = "John", Age = 30 };
/// var json = person.Fx.Json.Serialize().Payload;
/// // Result: {"Name":"John","Age":30}
/// </code>
/// 
/// <strong>Formatted serialization:</strong>
/// <code>
/// var formattedJson = person.Fx.Json.Serialize(formatted: true).Payload;
/// // Result (with tabs and sorted properties):
/// // {
/// //     "Age": 30,
/// //     "Name": "John"
/// // }
/// </code>
/// 
/// <strong>Deserialization with error handling:</strong>
/// <code>
/// string json = "{\"Name\":\"John\",\"Age\":30}";
/// var response = json.Fx.Json.Deserialize&lt;Person&gt;();
/// if (response.IsSuccess)
/// {
///     var person = response.Payload;
/// }
/// else
/// {
///     Console.WriteLine($"Deserialization failed: {response.Message}");
/// }
/// </code>
/// 
/// <strong>Exception serialization:</strong>
/// <code>
/// try
/// {
///     throw new InvalidOperationException("Something went wrong");
/// }
/// catch (Exception ex)
/// {
///     var exJson = ex.Fx.Json.Serialize(formatted: true).Payload;
///     // Includes exception type, message, stack trace, inner exceptions, etc.
/// }
/// </code>
/// </example>
public static class JsonExtensions
{
	//extension(string? me)
	//{
	//	[Obsolete("Este método ha sido sustituido por la nueva sintaxis (string).Fx.Json.Deserialize(). Solo funciona en Visual Studio 2026 o superior.")]
	//	public object? DeserializeFromJson(Type type, bool formatted = false, JsonSerializerOptions? options = null)
	//		=> me.Fx.Json.Deserialize(type, formatted, options).Payload;
	//	[Obsolete("Este método ha sido sustituido por la nueva sintaxis (string).Fx.Json.Deserialize(). Solo funciona en Visual Studio 2026 o superior.")]
	//	public T? DeserializeFromJson<T>(bool formatted = false, JsonSerializerOptions? options = null)
	//		=> me.Fx.Json.Deserialize<T>(formatted, options).Payload;
	//}

	//extension<T>(T? me)
	//{
	//	[Obsolete("Este método ha sido sustituido por la nueva sintaxis (string).Fx.Json.Serialize(). Solo funciona en Visual Studio 2026 o superior.")]
	//	public string? SerializeToJson(bool formatted = false, JsonSerializerOptions? options = null, bool errorIfNull = false)
	//		=> me.Fx.Json.Serialize(formatted, options).Payload;
	//}

	/// <summary>
	/// Provides a <see cref="JsonTypeInfoResolver"/> that combines private constructor support with alphabetical property ordering.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="FuxionFormattedTypeInfoResolver"/> is the default type info resolver used by Fuxion's formatted JSON serialization.
	/// It extends <see cref="DefaultJsonTypeInfoResolver"/> to provide two key features:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Private constructor support:</strong> Enables deserialization of types with private or non-public parameterless constructors</description></item>
	/// <item><description><strong>Alphabetical property ordering:</strong> Automatically sorts properties by name for consistent, predictable JSON output</description></item>
	/// </list>
	/// <para>
	/// This resolver is automatically used when the <c>formatted: true</c> parameter is specified in JSON operations,
	/// or when using <see cref="JsonSerializerOptions"/>.Formatted.
	/// </para>
	/// <para>
	/// <strong>Private constructor handling:</strong>
	/// </para>
	/// <para>
	/// For types without public constructors, the resolver assigns a <see cref="JsonTypeInfo.CreateObject"/> delegate
	/// that uses <see cref="Activator.CreateInstance(Type, bool)"/> with <c>nonPublic: true</c>. This allows
	/// deserialization of:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Types with private parameterless constructors (singletons, factory patterns)</description></item>
	/// <item><description>Types with protected constructors (EF Core entities, domain models)</description></item>
	/// <item><description>Immutable types that restrict direct instantiation</description></item>
	/// </list>
	/// <para>
	/// <strong>Alphabetical property ordering:</strong>
	/// </para>
	/// <para>
	/// All properties in <see cref="JsonTypeInfo.Properties"/> are sorted by <see cref="JsonPropertyInfo.Name"/>
	/// and assigned sequential <see cref="JsonPropertyInfo.Order"/> values starting from 1. This ensures:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Consistent JSON output regardless of property declaration order</description></item>
	/// <item><description>Diff-friendly JSON for version control</description></item>
	/// <item><description>Readable, predictable structure in formatted output</description></item>
	/// </list>
	/// <para>
	/// <strong>Integration:</strong> This resolver is used internally by <see cref="JsonSerializerOptions"/>.Formatted
	/// and the <c>ApplyFormat()</c> extension method.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Used automatically with formatted serialization
	/// var person = new Person { Name = "John", Age = 30 };
	/// var json = person.Fx.Json.Serialize(formatted: true).Payload;
	/// // Properties are alphabetically ordered: Age, Name
	/// 
	/// // Manual usage
	/// var options = new JsonSerializerOptions
	/// {
	///     TypeInfoResolver = new FuxionFormattedTypeInfoResolver(),
	///     WriteIndented = true
	/// };
	/// 
	/// // Deserialize type with private constructor
	/// public class SecureEntity
	/// {
	///     private SecureEntity() { }  // Private constructor
	///     public int Id { get; set; }
	///     public string Name { get; set; }
	/// }
	/// 
	/// var json = "{\"Id\":1,\"Name\":\"Test\"}";
	/// var entity = JsonSerializer.Deserialize&lt;SecureEntity&gt;(json, options);
	/// // Works! Private constructor is invoked via reflection
	/// </code>
	/// </example>
	public class FuxionFormattedTypeInfoResolver : DefaultJsonTypeInfoResolver
	{
		/// <summary>
		/// Gets the type information for the specified type, applying private constructor support and alphabetical property ordering.
		/// </summary>
		/// <param name="type">The type to get information for.</param>
		/// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
		/// <returns>
		/// A <see cref="JsonTypeInfo"/> instance with properties ordered alphabetically and, if applicable,
		/// a factory function for creating instances via private constructors.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method performs the following operations:
		/// </para>
		/// <list type="number">
		/// <item><description><strong>Base processing:</strong> Calls <see cref="DefaultJsonTypeInfoResolver.GetTypeInfo"/> to get standard type metadata</description></item>
		/// <item><description><strong>Private constructor check:</strong> If the type has no public instance constructors and is an object type without an existing <see cref="JsonTypeInfo.CreateObject"/> delegate:
		///   <list type="bullet">
		///     <item><description>Assigns a factory using <see cref="Activator.CreateInstance(Type, bool)"/> with <c>nonPublic: true</c></description></item>
		///     <item><description>Throws <see cref="InvalidOperationException"/> if instance creation fails</description></item>
		///   </list>
		/// </description></item>
		/// <item><description><strong>Property ordering:</strong> Sorts all properties in <see cref="JsonTypeInfo.Properties"/> by name and assigns sequential order values</description></item>
		/// <item><description><strong>Return:</strong> Returns the modified <see cref="JsonTypeInfo"/> with all enhancements applied</description></item>
		/// </list>
		/// <para>
		/// <strong>Type constraints:</strong> The private constructor support only applies to object types
		/// (<see cref="JsonTypeInfo.Kind"/> == <see cref="JsonTypeInfoKind.Object"/>) that don't already have
		/// a <see cref="JsonTypeInfo.CreateObject"/> delegate set.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Internal behavior demonstration
		/// 
		/// // Type with private constructor and properties
		/// public class MyType
		/// {
		///     private MyType() { }
		///     public string Zebra { get; set; }
		///     public int Apple { get; set; }
		/// }
		/// 
		/// // Step 1: Get base type info
		/// var jsonTypeInfo = base.GetTypeInfo(typeof(MyType), options);
		/// 
		/// // Step 2: Check for public constructors
		/// // typeof(MyType).GetConstructors(Public | Instance).Length == 0 ✓
		/// 
		/// // Step 3: Assign CreateObject factory
		/// jsonTypeInfo.CreateObject = () => Activator.CreateInstance(typeof(MyType), true);
		/// 
		/// // Step 4: Order properties alphabetically
		/// // Before: [Zebra (Order=0), Apple (Order=0)]
		/// // After:  [Apple (Order=1), Zebra (Order=2)]
		/// 
		/// // Result: Can deserialize MyType with private constructor,
		/// //         properties appear in JSON as: {"Apple":..., "Zebra":...}
		/// </code>
		/// </example>
		public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			var jsonTypeInfo = base.GetTypeInfo(type, options);
			if (jsonTypeInfo is not { Kind: JsonTypeInfoKind.Object, CreateObject: null }) return jsonTypeInfo;
			if (jsonTypeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
				// The type doesn't have public constructors
				jsonTypeInfo.CreateObject = () =>
					Activator.CreateInstance(jsonTypeInfo.Type, true)
					?? throw new InvalidOperationException($"Instance of type '{jsonTypeInfo.Type.GetSignature()}' could not be created with non public constructor");

			var order = 1;
			foreach (var property in jsonTypeInfo.Properties.OrderBy(p => p.Name)) property.Order = order++;

			return jsonTypeInfo;
		}
	}
	const string FormattedKey = "Fuxion_JsonSerializerOptions_Formatted_SingletonKey";
	extension(JsonSerializerOptions me)
	{

		/// <summary>
		/// Gets or sets the singleton instance of <see cref="JsonSerializerOptions"/> configured for Fuxion formatted serialization.
		/// </summary>
		/// <value>
		/// A <see cref="JsonSerializerOptions"/> instance with the following settings:
		/// <list type="bullet">
		/// <item><description><see cref="JsonSerializerOptions.IndentCharacter"/> = '\t' (tab character)</description></item>
		/// <item><description><see cref="JsonSerializerOptions.IndentSize"/> = 1</description></item>
		/// <item><description><see cref="JsonSerializerOptions.WriteIndented"/> = <c>true</c></description></item>
		/// <item><description><see cref="JsonSerializerOptions.AllowTrailingCommas"/> = <c>true</c></description></item>
		/// <item><description><see cref="JsonSerializerOptions.ReadCommentHandling"/> = <see cref="JsonCommentHandling.Skip"/></description></item>
		/// <item><description><see cref="JsonSerializerOptions.TypeInfoResolver"/> = <see cref="FuxionFormattedTypeInfoResolver"/></description></item>
		/// </list>
		/// </value>
		/// <remarks>
		/// <para>
		/// This property provides a centralized, singleton instance of formatted serializer options used throughout Fuxion.
		/// The instance is cached using <see cref="Singleton"/> and can be customized by setting a new value.
		/// </para>
		/// <para>
		/// <strong>Get behavior:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>If a singleton instance exists and is read-only, returns a new mutable copy</description></item>
		/// <item><description>If a singleton instance exists and is mutable, returns that instance</description></item>
		/// <item><description>If no singleton exists, creates a new instance with default formatted settings and caches it</description></item>
		/// </list>
		/// <para>
		/// <strong>Set behavior:</strong> Replaces the singleton instance with the provided value.
		/// </para>
		/// <para>
		/// <strong>Read-only handling:</strong> If the cached instance is read-only (e.g., after being used in serialization),
		/// the getter automatically creates a new mutable copy. This prevents exceptions when trying to modify options
		/// that have been frozen by the serializer.
		/// </para>
		/// <para>
		/// <strong>Formatted style:</strong> The configured options produce JSON with:
		/// </para>
		/// <list type="bullet">
		/// <item><description>Tab indentation for readability</description></item>
		/// <item><description>Trailing commas allowed for easier editing</description></item>
		/// <item><description>Comments skipped during deserialization</description></item>
		/// <item><description>Properties sorted alphabetically</description></item>
		/// <item><description>Private constructor support for domain models</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <strong>Using the formatted options:</strong>
		/// <code>
		/// var person = new Person { Name = "John", Age = 30, Email = "john@example.com" };
		/// 
		/// // Using the formatted singleton
		/// var json = JsonSerializer.Serialize(person, JsonSerializerOptions.Formatted);
		/// // Result:
		/// // {
		/// //     "Age": 30,
		/// //     "Email": "john@example.com",
		/// //     "Name": "John"
		/// // }
		/// </code>
		/// 
		/// <strong>Customizing the formatted options:</strong>
		/// <code>
		/// // Get the current formatted options (mutable copy if read-only)
		/// var options = JsonSerializerOptions.Formatted;
		/// options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
		/// 
		/// // Replace the singleton with custom options
		/// JsonSerializerOptions.Formatted = options;
		/// 
		/// // All subsequent formatted operations use the custom options
		/// var json2 = person.Fx.Json.Serialize(formatted: true).Payload;
		/// </code>
		/// 
		/// <strong>Automatic read-only handling:</strong>
		/// <code>
		/// // First use - creates and caches the options
		/// var json1 = JsonSerializer.Serialize(person, JsonSerializerOptions.Formatted);
		/// // After serialization, the options might be read-only
		/// 
		/// // Next access - automatically returns a mutable copy
		/// var options = JsonSerializerOptions.Formatted;  // New mutable copy
		/// options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;  // ✓ Works!
		/// </code>
		/// </example>
		public static JsonSerializerOptions Formatted
		{
			get
			{
				var current = Singleton.Find<JsonSerializerOptions?>(FormattedKey);
				return current is not null
					? current.IsReadOnly ? new(current) : current
					: Singleton.Add<JsonSerializerOptions>(new()
					{
						IndentCharacter = '\t',
						IndentSize = 1,
						WriteIndented = true,
						AllowTrailingCommas = true,
						ReadCommentHandling = JsonCommentHandling.Skip,
						TypeInfoResolver = new FuxionFormattedTypeInfoResolver()
					}, FormattedKey);
			}
			set => Singleton.Set(value, FormattedKey);
		}

		/// <summary>
		/// Creates a new <see cref="JsonSerializerOptions"/> instance by copying the current options and applying Fuxion formatted settings.
		/// </summary>
		/// <returns>
		/// A new <see cref="JsonSerializerOptions"/> instance with formatted settings applied.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method creates a copy of the current options using the copy constructor, then overwrites
		/// specific properties to apply Fuxion's formatted style. The following properties are set:
		/// </para>
		/// <list type="bullet">
		/// <item><description><see cref="JsonSerializerOptions.IndentCharacter"/> = '\t'</description></item>
		/// <item><description><see cref="JsonSerializerOptions.IndentSize"/> = 1</description></item>
		/// <item><description><see cref="JsonSerializerOptions.WriteIndented"/> = <c>true</c></description></item>
		/// <item><description><see cref="JsonSerializerOptions.AllowTrailingCommas"/> = <c>true</c></description></item>
		/// <item><description><see cref="JsonSerializerOptions.ReadCommentHandling"/> = <see cref="JsonCommentHandling.Skip"/></description></item>
		/// <item><description><see cref="JsonSerializerOptions.TypeInfoResolver"/> = new <see cref="FuxionFormattedTypeInfoResolver"/></description></item>
		/// </list>
		/// <para>
		/// <strong>Important:</strong> This method overwrites the <see cref="JsonSerializerOptions.TypeInfoResolver"/> property,
		/// replacing any existing resolver with <see cref="FuxionFormattedTypeInfoResolver"/>. Other custom settings
		/// (converters, naming policies, etc.) are preserved from the source options.
		/// </para>
		/// <para>
		/// <strong>Use case:</strong> This method is used internally by the <see cref="ToFinalOptions"/> method when
		/// <c>formatted: true</c> is specified with custom options. It allows merging user-provided options with
		/// Fuxion's formatted style.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Create options with custom converters
		/// var customOptions = new JsonSerializerOptions();
		/// customOptions.Converters.Add(new CustomConverter());
		/// customOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
		/// 
		/// // Apply formatted settings while preserving custom converters
		/// var formattedOptions = customOptions.ApplyFormat();
		/// 
		/// // Result:
		/// // - Custom converter is preserved
		/// // - CamelCase naming is preserved
		/// // - Formatted settings (tabs, indented, etc.) are applied
		/// // - TypeInfoResolver is replaced with FuxionFormattedTypeInfoResolver
		/// </code>
		/// </example>
		private JsonSerializerOptions ApplyFormat() => new(me)
		{
			IndentCharacter = '\t',
			IndentSize = 1,
			WriteIndented = true,
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			TypeInfoResolver = new FuxionFormattedTypeInfoResolver()
		};
	}
	extension((bool Formatted, JsonSerializerOptions? Options) me)
	{
		/// <summary>
		/// Resolves the final <see cref="JsonSerializerOptions"/> to use based on the formatted flag and provided options.
		/// </summary>
		/// <returns>
		/// The resolved <see cref="JsonSerializerOptions"/> instance, or <c>null</c> if using default settings.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method implements the logic for combining the <c>formatted</c> parameter with user-provided options.
		/// It uses pattern matching to determine the appropriate options based on the combination of flags:
		/// </para>
		/// <list type="table">
		/// <listheader>
		/// <term>Formatted</term>
		/// <term>Options</term>
		/// <description>Result</description>
		/// </listheader>
		/// <item>
		/// <term><c>true</c></term>
		/// <term><c>null</c></term>
		/// <description>Returns <see cref="JsonSerializerOptions"/>.Formatted (singleton instance)</description>
		/// </item>
		/// <item>
		/// <term><c>true</c></term>
		/// <term>not <c>null</c></term>
		/// <description>Returns <c>Options.ApplyFormat()</c> (user options + formatted settings)</description>
		/// </item>
		/// <item>
		/// <term><c>false</c></term>
		/// <term>IsReadOnly = <c>false</c></term>
		/// <description>Returns the options as-is (mutable)</description>
		/// </item>
		/// <item>
		/// <term><c>false</c></term>
		/// <term>IsReadOnly = <c>true</c></term>
		/// <description>Returns <c>new JsonSerializerOptions(Options)</c> (mutable copy)</description>
		/// </item>
		/// <item>
		/// <term><c>false</c></term>
		/// <term><c>null</c></term>
		/// <description>Returns <c>null</c> (use default serializer settings)</description>
		/// </item>
		/// </list>
		/// <para>
		/// <strong>Read-only handling:</strong> If the provided options are read-only, a new mutable copy is created
		/// to prevent exceptions during serialization. This can happen when options have been used in a previous
		/// serialization operation and were frozen by the serializer.
		/// </para>
		/// <para>
		/// <strong>Use case:</strong> This method is used internally by all serialization and deserialization extension
		/// methods to resolve the final options before calling <see cref="JsonSerializer"/> methods.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Internal usage examples
		/// 
		/// // Case 1: formatted = true, no custom options
		/// (true, null).ToFinalOptions();
		/// // Returns: JsonSerializerOptions.Formatted
		/// 
		/// // Case 2: formatted = true, with custom options
		/// var custom = new JsonSerializerOptions();
		/// custom.Converters.Add(new CustomConverter());
		/// (true, custom).ToFinalOptions();
		/// // Returns: custom.ApplyFormat() (preserves converter, adds formatted settings)
		/// 
		/// // Case 3: formatted = false, mutable options
		/// var mutable = new JsonSerializerOptions { WriteIndented = true };
		/// (false, mutable).ToFinalOptions();
		/// // Returns: mutable (as-is)
		/// 
		/// // Case 4: formatted = false, read-only options
		/// var readOnly = new JsonSerializerOptions();
		/// JsonSerializer.Serialize(someObject, readOnly); // Makes it read-only
		/// (false, readOnly).ToFinalOptions();
		/// // Returns: new JsonSerializerOptions(readOnly) (mutable copy)
		/// 
		/// // Case 5: formatted = false, no options
		/// (false, null).ToFinalOptions();
		/// // Returns: null (JsonSerializer uses its defaults)
		/// </code>
		/// </example>
		private JsonSerializerOptions? ToFinalOptions() =>
			me switch
			{
				(true, null) => JsonSerializerOptions.Formatted,
				(true, not null) => me.Options.ApplyFormat(),
				(false, { IsReadOnly: false }) => me.Options,
				(false, { IsReadOnly: true }) => new(me.Options),
				_ => null
			};
	}
	extension<T>(FuxionExtensions<T?> me)
	{
		/// <summary>
		/// Provides JSON serialization and deserialization extension operations for this value.
		/// </summary>
		public JsonExtensions<T?> Json
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(JsonExtensions<string?> me)
	{
		/// <summary>
		/// Deserializes the underlying JSON string value into an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The target type for deserialization. Must be a non-nullable reference type.</typeparam>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control deserialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the deserialized object when successful,
		/// or an error response when the underlying string is <c>null</c>, empty, whitespace-only,
		/// deserialization produces a <c>null</c> result, or an exception occurs during deserialization.
		/// </returns>
		/// <example>
		/// <code>
		/// var json = "{\"Name\":\"John\",\"Age\":30}";
		/// var response = json.Fx.Json.Deserialize&lt;Person&gt;();
		/// if (response.IsSuccess)
		/// {
		///     var person = response.Payload;
		///     Console.WriteLine(person.Name);  // Output: John
		/// }
		/// </code>
		/// </example>
		public Response<T> Deserialize<T>(bool formatted = false, JsonSerializerOptions? options = null)
		{
			if (me.Value.IsNullOrWhiteSpace())
				return Response.Get.Critical(
						$"The string cannot be deserialized as '{typeof(T).GetSignature()}' because source string is null, empty or only white spaces")
					.AsPayload<T>();

			try
			{
				var res = JsonSerializer.Deserialize<T>(me.Value, (formatted, options).ToFinalOptions());
				return res is null
					? Response.Get.Critical($"Deserialization produced a null result").AsPayload<T>()
					: Response.Get.SuccessPayload<T>(res);
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<T>();
			}
		}

		/// <summary>
		/// Deserializes the underlying JSON string value into an instance of type <typeparamref name="T"/>, returning <c>null</c> when the source string is <c>null</c>, empty, or whitespace-only.
		/// </summary>
		/// <typeparam name="T">The target type for deserialization. Must be a non-nullable reference type.</typeparam>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control deserialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the deserialized object when successful;
		/// <c>null</c> when the underlying string is <c>null</c>, empty, whitespace-only, or deserialization produces a <c>null</c> result;
		/// or an error response when an exception occurs during deserialization.
		/// </returns>
		/// <example>
		/// <code>
		/// string? json = null;
		/// var response = json.Fx.Json.DeserializeNullable&lt;Person&gt;();  // returns null
		/// 
		/// json = "{\"Name\":\"John\"}";
		/// response = json.Fx.Json.DeserializeNullable&lt;Person&gt;();
		/// if (response != null &amp;&amp; response.IsSuccess)
		/// {
		///     var person = response.Payload;
		/// }
		/// </code>
		/// </example>
		public Response<T>? DeserializeNullable<T>(bool formatted = false, JsonSerializerOptions? options = null)
		{
			if (me.Value.IsNullOrWhiteSpace())
				return null;

			try
			{
				var res = JsonSerializer.Deserialize<T>(me.Value, (formatted,options).ToFinalOptions());
				return res is null
					? null
					: Response.Get.SuccessPayload<T>(res);
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<T>();
			}
		}
		
		/// <summary>
		/// Deserializes the underlying JSON string value into an instance of the specified <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The target type for deserialization.</param>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control deserialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> of <see cref="object"/> whose payload contains the deserialized object when successful,
		/// or an error response when the underlying string is <c>null</c>, empty, whitespace-only,
		/// deserialization produces a <c>null</c> result, or an exception occurs during deserialization.
		/// </returns>
		/// <example>
		/// <code>
		/// var json = "{\"Name\":\"John\",\"Age\":30}";
		/// var personType = typeof(Person);
		/// var response = json.Fx.Json.Deserialize(personType);
		/// if (response.IsSuccess)
		/// {
		///     var person = (Person)response.Payload;
		///     Console.WriteLine(person.Name);  // Output: John
		/// }
		/// </code>
		/// </example>
		public Response<object> Deserialize(Type type, bool formatted = false, JsonSerializerOptions? options = null)
		{
			if (me.Value.IsNullOrWhiteSpace())
				return Response.Get
					.Critical($"The string cannot be deserialized as '{type.GetSignature()}' because source string is null, empty or only white spaces")
					.AsPayload<object>();

			try
			{
				var res = JsonSerializer.Deserialize(me.Value, type, (formatted, options).ToFinalOptions());
				return res is null
					? Response.Get.Critical("Deserialization produced a null result").AsPayload<object>()
					: Response.Get.SuccessPayload(res);
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<object>();
			}
		}

		/// <summary>
		/// Deserializes the underlying JSON string value into an instance of the specified <paramref name="type"/>, returning <c>null</c> when the source string is <c>null</c>, empty, or whitespace-only.
		/// </summary>
		/// <param name="type">The target type for deserialization.</param>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control deserialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> of <see cref="object"/> whose payload contains the deserialized object when successful;
		/// <c>null</c> when the underlying string is <c>null</c>, empty, whitespace-only, or deserialization produces a <c>null</c> result;
		/// or an error response when an exception occurs during deserialization.
		/// </returns>
		/// <example>
		/// <code>
		/// string? json = null;
		/// var personType = typeof(Person);
		/// var response = json.Fx.Json.DeserializeNullable(personType);  // returns null
		/// 
		/// json = "{\"Name\":\"John\"}";
		/// response = json.Fx.Json.DeserializeNullable(personType);
		/// if (response != null &amp;&amp; response.IsSuccess)
		/// {
		///     var person = (Person)response.Payload;
		/// }
		/// </code>
		/// </example>
		public Response<object>? DeserializeNullable(Type type, bool formatted = false, JsonSerializerOptions? options = null)
		{
			if (me.Value.IsNullOrWhiteSpace())
				return null;

			try
			{
				var res = JsonSerializer.Deserialize(me.Value, type, (formatted,options).ToFinalOptions());
				return res is null
					? null
					: Response.Get.SuccessPayload(res);
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<object>();
			}
		}
	}
	extension<T>(JsonExtensions<T?> me)
	{
		/// <summary>
		/// Serializes the underlying value to a JSON string.
		/// </summary>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options for indented, readable output.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings for compact output.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control serialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <param name="errorIfNull">
		/// When <c>true</c>, returns an error response if the underlying value is <c>null</c>.
		/// When <c>false</c>, serializes <c>null</c> values as the JSON literal <c>"null"</c>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the JSON string representation when successful,
		/// or an error response when <paramref name="errorIfNull"/> is <c>true</c> and the value is <c>null</c>,
		/// or an exception occurs during serialization.
		/// </returns>
		/// <example>
		/// <code>
		/// var person = new Person { Name = "John", Age = 30 };
		/// var response = person.Fx.Json.Serialize(formatted: true);
		/// if (response.IsSuccess)
		/// {
		///     Console.WriteLine(response.Payload);
		///     // Output (formatted):
		///     // {
		///     //     "Age": 30,
		///     //     "Name": "John"
		///     // }
		/// }
		/// 
		/// // Handling null values
		/// Person? nullPerson = null;
		/// var nullResponse1 = nullPerson.Fx.Json.Serialize();  // Success: "null"
		/// var nullResponse2 = nullPerson.Fx.Json.Serialize(errorIfNull: true);  // Error response
		/// </code>
		/// </example>
		public Response<string> Serialize(bool formatted = false, JsonSerializerOptions? options = null, bool errorIfNull = false)
		{
			try
			{
				if (errorIfNull && me.Value is null)
					return Response.Get.Critical($"The object cannot be serialized as '{typeof(T).GetSignature()}' because source object is null")
						.AsPayload<string>();

				return Response.Get.SuccessPayload(JsonSerializer.Serialize(me.Value, (formatted, options).ToFinalOptions()));
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<string>();
			}
		}

		/// <summary>
		/// Serializes the underlying value to a <see cref="JsonNode"/>.
		/// </summary>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control serialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the <see cref="JsonNode"/> representation when successful,
		/// or an error response when the underlying value is <c>null</c>, serialization produces a <c>null</c> result,
		/// or an exception occurs during serialization.
		/// </returns>
		/// <example>
		/// <code>
		/// var person = new Person { Name = "John", Age = 30 };
		/// var response = person.Fx.Json.SerializeToNode();
		/// if (response.IsSuccess)
		/// {
		///     var node = response.Payload;
		///     var name = node["Name"]?.GetValue&lt;string&gt;();  // "John"
		///     var age = node["Age"]?.GetValue&lt;int&gt;();        // 30
		/// }
		/// </code>
		/// </example>
		public Response<JsonNode> SerializeToNode(bool formatted = false, JsonSerializerOptions? options = null)
		{
			try
			{
				if (me.Value is null)
					return Response.Get
						.Critical($"The object cannot be serialized as '{typeof(T).GetSignature()}' because source object is null")
						.AsPayload<JsonNode>();

				var res = JsonSerializer.SerializeToNode(me.Value, (formatted, options).ToFinalOptions());
				return res is null
					? Response.Get.Critical($"The object cannot be serializer as '{typeof(T).GetSignature()}' because result was null").AsPayload<JsonNode>()
					: Response.Get.SuccessPayload(res);
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<JsonNode>();
			}
		}

		/// <summary>
		/// Serializes the underlying value to a <see cref="JsonElement"/>.
		/// </summary>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control serialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// </param>
		/// <param name="errorIfNull">
		/// When <c>true</c>, returns an error response if the underlying value is <c>null</c>.
		/// When <c>false</c>, serializes <c>null</c> values as a <see cref="JsonElement"/> representing <c>null</c>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the <see cref="JsonElement"/> representation when successful,
		/// or an error response when <paramref name="errorIfNull"/> is <c>true</c> and the value is <c>null</c>,
		/// or an exception occurs during serialization.
		/// </returns>
		/// <example>
		/// <code>
		/// var person = new Person { Name = "John", Age = 30 };
		/// var response = person.Fx.Json.SerializeToElement();
		/// if (response.IsSuccess)
		/// {
		///     var element = response.Payload;
		///     var name = element.GetProperty("Name").GetString();  // "John"
		///     var age = element.GetProperty("Age").GetInt32();     // 30
		/// }
		/// </code>
		/// </example>
		public Response<JsonElement> SerializeToElement(bool formatted = false, JsonSerializerOptions? options = null, bool errorIfNull = false)
		{
			try
			{
				if (errorIfNull && me.Value is null)
					return Response.Get
						.Critical(
							$"The object cannot be serialized as '{typeof(T).GetSignature()}' because source object is null")
						.AsPayload<JsonElement>();
				
				return Response.Get.SuccessPayload(JsonSerializer.SerializeToElement(me.Value, (formatted, options).ToFinalOptions()));
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<JsonElement>();
			}
		}
	}
	extension(JsonExtensions<Exception?> me)
	{
		/// <summary>
		/// Serializes the underlying <see cref="Exception"/> value to a JSON string.
		/// Uses a specialized <see cref="ExceptionConverter"/> to ensure proper serialization of exception properties.
		/// </summary>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options for indented, readable output.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings for compact output.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control serialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// An <see cref="ExceptionConverter"/> is automatically added if not already present.
		/// </param>
		/// <param name="errorIfNull">
		/// When <c>true</c>, returns an error response if the underlying exception is <c>null</c>.
		/// When <c>false</c>, serializes <c>null</c> values as the JSON literal <c>"null"</c>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the JSON string representation when successful,
		/// or an error response when <paramref name="errorIfNull"/> is <c>true</c> and the exception is <c>null</c>,
		/// or an exception occurs during serialization.
		/// </returns>
		/// <example>
		/// <code>
		/// try
		/// {
		///     throw new InvalidOperationException("Something went wrong");
		/// }
		/// catch (Exception ex)
		/// {
		///     var response = ex.Fx.Json.Serialize(formatted: true);
		///     if (response.IsSuccess)
		///     {
		///         Console.WriteLine(response.Payload);
		///         // Output includes exception type, message, stack trace, etc.
		///     }
		/// }
		/// </code>
		/// </example>
		public Response<string> Serialize(bool formatted = false, JsonSerializerOptions? options = null, bool errorIfNull = false)
		{
			try
			{
				if (errorIfNull && me.Value is null)
					return Response.Get
						.Critical("The Exception cannot be serialized because source Exception is null")
						.AsPayload<string>();

				var finalOptions = (formatted, options).ToFinalOptions();
				if (finalOptions is null)
					finalOptions = new()
					{
						Converters = { new ExceptionConverter() }
					};
				else if (!finalOptions.Converters.Any(c => c.GetType().IsSubclassOf(typeof(ExceptionConverter))))
					finalOptions.Converters.Add(new ExceptionConverter());

				return Response.Get.SuccessPayload(JsonSerializer.Serialize(me.Value, finalOptions));
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<string>();
			}
		}

		/// <summary>
		/// Serializes the underlying <see cref="Exception"/> value to a <see cref="JsonNode"/>.
		/// Uses a specialized <see cref="ExceptionConverter"/> to ensure proper serialization of exception properties.
		/// </summary>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control serialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// An <see cref="ExceptionConverter"/> is automatically added if not already present.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the <see cref="JsonNode"/> representation when successful,
		/// or an error response when the underlying exception is <c>null</c>, serialization produces a <c>null</c> result,
		/// or an exception occurs during serialization.
		/// </returns>
		/// <example>
		/// <code>
		/// try
		/// {
		///     throw new InvalidOperationException("Something went wrong");
		/// }
		/// catch (Exception ex)
		/// {
		///     var response = ex.Fx.Json.SerializeToNode();
		///     if (response.IsSuccess)
		///     {
		///         var node = response.Payload;
		///         var message = node["Message"]?.GetValue&lt;string&gt;();
		///     }
		/// }
		/// </code>
		/// </example>
		public Response<JsonNode> SerializeToNode(bool formatted = false, JsonSerializerOptions? options = null)
		{
			try
			{
				if (me.Value is null)
					return Response.Get
						.Critical(
							$"The Exception cannot be serialized because source Exception is null")
						.AsPayload<JsonNode>();

				var finalOptions = (formatted, options).ToFinalOptions();
				if (finalOptions is null)
					finalOptions = new()
					{
						Converters = { new ExceptionConverter() }
					};
				else if (!finalOptions.Converters.Any(c => c.GetType().IsSubclassOf(typeof(ExceptionConverter))))
					finalOptions.Converters.Add(new ExceptionConverter());

				var res = JsonSerializer.SerializeToNode(me.Value, finalOptions);
				if (res is null)
					return Response.Get.Critical($"The Exception cannot be serializer as '{me.Value?.GetType().GetSignature()}' because result was null")
						.AsPayload<JsonNode>();
				return Response.Get.SuccessPayload(res);
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<JsonNode>();
			}
		}

		/// <summary>
		/// Serializes the underlying <see cref="Exception"/> value to a <see cref="JsonElement"/>.
		/// Uses a specialized <see cref="ExceptionConverter"/> to ensure proper serialization of exception properties.
		/// </summary>
		/// <param name="formatted">
		/// When <c>true</c>, uses the formatted JSON serializer options.
		/// When <c>false</c>, uses the provided <paramref name="options"/> or default settings.
		/// </param>
		/// <param name="options">
		/// Optional <see cref="JsonSerializerOptions"/> to control serialization behavior.
		/// When <paramref name="formatted"/> is <c>true</c>, these options are merged with the formatted settings.
		/// An <see cref="ExceptionConverter"/> is automatically added if not already present.
		/// </param>
		/// <param name="errorIfNull">
		/// When <c>true</c>, returns an error response if the underlying exception is <c>null</c>.
		/// When <c>false</c>, serializes <c>null</c> values as a <see cref="JsonElement"/> representing <c>null</c>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains the <see cref="JsonElement"/> representation when successful,
		/// or an error response when <paramref name="errorIfNull"/> is <c>true</c> and the exception is <c>null</c>,
		/// or an exception occurs during serialization.
		/// </returns>
		/// <example>
		/// <code>
		/// try
		/// {
		///     throw new InvalidOperationException("Something went wrong");
		/// }
		/// catch (Exception ex)
		/// {
		///     var response = ex.Fx.Json.SerializeToElement();
		///     if (response.IsSuccess)
		///     {
		///         var element = response.Payload;
		///         var message = element.GetProperty("Message").GetString();
		///     }
		/// }
		/// </code>
		/// </example>
		public Response<JsonElement> SerializeToElement(bool formatted = false, JsonSerializerOptions? options = null, bool errorIfNull = false)
		{
			try
			{
				if (errorIfNull && me.Value is null)
					return Response.Get
						.Critical(
							$"The Exception cannot be serialized because source Exception is null")
						.AsPayload<JsonElement>();

				var finalOptions = (formatted, options).ToFinalOptions();
				if (finalOptions is null)
					finalOptions = new()
					{
						Converters = { new ExceptionConverter() }
					};
				else if (!finalOptions.Converters.Any(c => c.GetType().IsSubclassOf(typeof(ExceptionConverter))))
					finalOptions.Converters.Add(new ExceptionConverter());

				return Response.Get.SuccessPayload(JsonSerializer.SerializeToElement(me.Value, finalOptions));
			}
			catch (Exception ex)
			{
				return Response.Get.Exception(ex).AsPayload<JsonElement>();
			}
		}
	}
}

/// <summary>
/// Wrapper class for JSON extension operations on values of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value being wrapped for JSON operations.</typeparam>
/// <param name="me">The value to wrap for JSON serialization and deserialization operations.</param>
/// <remarks>
/// <para>
/// This class extends <see cref="Extensions{T}"/> to provide a type-safe wrapper for JSON operations.
/// It's accessed through the <c>.Fx.Json</c> fluent API pattern and provides methods for:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Serialization:</strong> Convert values to JSON strings, nodes, or elements</description></item>
/// <item><description><strong>Deserialization:</strong> Convert JSON strings to strongly-typed objects (when <typeparamref name="T"/> is <see cref="string"/>)</description></item>
/// <item><description><strong>Response wrapping:</strong> All operations return <see cref="Response{T}"/> for consistent error handling</description></item>
/// </list>
/// <para>
/// <strong>Design pattern:</strong> This class uses the primary constructor syntax (C# 12+) to create an immutable
/// wrapper around a value. The base class <see cref="Extensions{T}"/> stores the value and provides it to extension
/// methods defined in <see cref="JsonExtensions"/>.
/// </para>
/// <para>
/// <strong>Usage:</strong> Instances of this class are typically created automatically by the <c>.Fx.Json</c> property
/// in the fluent API. Direct instantiation is rare.
/// </para>
/// </remarks>
/// <example>
/// <strong>Typical usage through fluent API:</strong>
/// <code>
/// // Serialization
/// var person = new Person { Name = "John", Age = 30 };
/// var json = person.Fx.Json.Serialize().Payload;
/// // Behind the scenes:
/// // 1. person.Fx returns FuxionExtensions&lt;Person&gt;
/// // 2. .Json returns new JsonExtensions&lt;Person&gt;(person)
/// // 3. .Serialize() is an extension method that operates on JsonExtensions&lt;Person&gt;
/// 
/// // Deserialization
/// string json = "{\"Name\":\"John\",\"Age\":30}";
/// var response = json.Fx.Json.Deserialize&lt;Person&gt;();
/// // Behind the scenes:
/// // 1. json.Fx returns FuxionExtensions&lt;string&gt;
/// // 2. .Json returns new JsonExtensions&lt;string&gt;(json)
/// // 3. .Deserialize&lt;Person&gt;() is an extension method for JsonExtensions&lt;string&gt;
/// </code>
/// 
/// <strong>Direct instantiation (rarely needed):</strong>
/// <code>
/// var wrapper = new JsonExtensions&lt;Person&gt;(person);
/// // Can now call extension methods directly
/// var json = wrapper.Serialize().Payload;
/// </code>
/// </example>
public class JsonExtensions<T>(T me) : Extensions<T>(me);
