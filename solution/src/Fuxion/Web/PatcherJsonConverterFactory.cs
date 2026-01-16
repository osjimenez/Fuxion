using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

namespace Fuxion.Web;

/// <summary>
/// Provides a JSON converter factory for creating converters that handle <see cref="Patcher{T}"/> types during serialization and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PatcherJsonConverterFactory"/> is a specialized <see cref="JsonConverterFactory"/> that dynamically creates
/// instances of <see cref="PatcherJsonConverter{T}"/> for any closed generic type derived from <see cref="Patcher{T}"/>.
/// This enables automatic JSON conversion support for the Patcher pattern, which is commonly used in HTTP PATCH operations
/// to track and apply partial updates to objects.
/// </para>
/// <para>
/// <strong>The Patcher pattern:</strong>
/// </para>
/// <para>
/// The Patcher pattern allows for selective property updates where only the properties explicitly set in the JSON payload
/// are updated, leaving other properties unchanged. This is particularly useful for:
/// </para>
/// <list type="bullet">
/// <item><description><strong>RESTful APIs:</strong> Implementing HTTP PATCH endpoints that update only specified fields</description></item>
/// <item><description><strong>Partial updates:</strong> Distinguishing between "not provided" and "set to null" in API requests</description></item>
/// <item><description><strong>Form submissions:</strong> Processing incomplete or partial form data</description></item>
/// <item><description><strong>Delta tracking:</strong> Recording which properties have been modified</description></item>
/// </list>
/// <para>
/// <strong>How it works:</strong>
/// </para>
/// <list type="number">
/// <item><description><see cref="CanConvert"/> checks if a type is a subclass of the generic <see cref="Patcher{T}"/> definition</description></item>
/// <item><description><see cref="CreateConverter"/> extracts the generic type argument(s) from the <see cref="Patcher{T}"/> type</description></item>
/// <item><description>Creates a corresponding <see cref="PatcherJsonConverter{T}"/> using reflection and <see cref="Activator.CreateInstance(Type, object[])"/></description></item>
/// <item><description>Returns the converter instance to be used by the JSON serialization infrastructure</description></item>
/// </list>
/// <para>
/// <strong>Type safety and validation:</strong>
/// </para>
/// <para>
/// The factory performs runtime type validation to ensure the converter is created correctly. If <see cref="Activator.CreateInstance(Type, object[])"/>
/// fails to create the converter instance, an <see cref="InvalidCastException"/> is thrown with a descriptive message.
/// </para>
/// <para>
/// <strong>Usage with JsonSerializerOptions:</strong>
/// </para>
/// <para>
/// To enable Patcher support, add this factory to the converters collection in your <see cref="JsonSerializerOptions"/>.
/// Once registered, all <see cref="Patcher{T}"/> types will be automatically handled during JSON operations.
/// </para>
/// </remarks>
/// <example>
/// <strong>Registering the factory:</strong>
/// <code>
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new PatcherJsonConverterFactory());
/// 
/// // Now Patcher&lt;T&gt; types can be serialized/deserialized
/// var json = JsonSerializer.Serialize(patcherInstance, options);
/// var patcher = JsonSerializer.Deserialize&lt;Patcher&lt;MyModel&gt;&gt;(json, options);
/// </code>
/// 
/// <strong>Example Patcher usage in an API:</strong>
/// <code>
/// // Model to be patched
/// public class UserProfile
/// {
///     public string Name { get; set; }
///     public string Email { get; set; }
///     public int Age { get; set; }
/// }
/// 
/// // API endpoint for partial updates
/// [HttpPatch("users/{id}")]
/// public IActionResult PatchUser(int id, [FromBody] Patcher&lt;UserProfile&gt; patcher)
/// {
///     var user = _repository.GetUser(id);
///     
///     // Apply only the properties that were provided in the JSON
///     patcher.ApplyTo(user);
///     
///     _repository.SaveChanges();
///     return Ok(user);
/// }
/// 
/// // Client sends partial update (only updating email):
/// // PATCH /users/123
/// // { "Email": "newemail@example.com" }
/// 
/// // Result: Only the Email property is updated, Name and Age remain unchanged
/// </code>
/// 
/// <strong>Distinguishing between null and not provided:</strong>
/// <code>
/// // JSON payload 1: Email is explicitly set to null
/// // { "Email": null }
/// 
/// // JSON payload 2: Email is not provided at all
/// // { }
/// 
/// // With Patcher, you can distinguish these cases:
/// if (patcher.HasProperty(x => x.Email))
/// {
///     if (patcher.GetValue(x => x.Email) == null)
///     {
///         Console.WriteLine("Email was explicitly set to null");
///     }
///     else
///     {
///         Console.WriteLine("Email was set to a value");
///     }
/// }
/// else
/// {
///     Console.WriteLine("Email was not provided in the request");
/// }
/// </code>
/// 
/// <strong>ASP.NET Core integration:</strong>
/// <code>
/// // In Startup.cs or Program.cs
/// builder.Services.AddControllers()
///     .AddJsonOptions(options =>
///     {
///         options.JsonSerializerOptions.Converters.Add(new PatcherJsonConverterFactory());
///     });
/// 
/// // Now all controllers automatically support Patcher&lt;T&gt; in their parameters
/// </code>
/// </example>
public class PatcherJsonConverterFactory : JsonConverterFactory
{
	/// <summary>
	/// Determines whether this factory can create a converter for the specified type.
	/// </summary>
	/// <param name="typeToConvert">The type to check for conversion compatibility.</param>
	/// <returns>
	/// <c>true</c> if <paramref name="typeToConvert"/> is a subclass of the generic <see cref="Patcher{T}"/> definition;
	/// otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method uses the <see cref="Fuxion.Reflection.ReflectionExtensions.IsSubclassOfGenericDefinition"/> extension method
	/// to check if the type is derived from <c>Patcher&lt;&gt;</c> (the open generic type definition).
	/// </para>
	/// <para>
	/// <strong>Matching types:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description><c>Patcher&lt;User&gt;</c> - Direct usage of Patcher (returns <c>true</c>)</description></item>
	/// <item><description><c>CustomPatcher&lt;User&gt; : Patcher&lt;User&gt;</c> - Derived class (returns <c>true</c>)</description></item>
	/// <item><description><c>User</c> - Not a Patcher type (returns <c>false</c>)</description></item>
	/// <item><description><c>List&lt;Patcher&lt;User&gt;&gt;</c> - Collection of Patchers (returns <c>false</c>, use collection converter)</description></item>
	/// </list>
	/// <para>
	/// <strong>Performance note:</strong> This method is called by the JSON serializer infrastructure for each type
	/// during serialization/deserialization setup. The check is relatively lightweight using cached reflection information.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var factory = new PatcherJsonConverterFactory();
	/// 
	/// // Direct Patcher type
	/// bool result1 = factory.CanConvert(typeof(Patcher&lt;User&gt;));  // true
	/// 
	/// // Derived Patcher type
	/// public class UserPatcher : Patcher&lt;User&gt; { }
	/// bool result2 = factory.CanConvert(typeof(UserPatcher));  // true
	/// 
	/// // Non-Patcher type
	/// bool result3 = factory.CanConvert(typeof(User));  // false
	/// 
	/// // Open generic (not typically used directly)
	/// bool result4 = factory.CanConvert(typeof(Patcher&lt;&gt;));  // false (open generic)
	/// </code>
	/// </example>
	public override bool CanConvert(Type typeToConvert) => typeToConvert.IsSubclassOfGenericDefinition(typeof(Patcher<>));

	/// <summary>
	/// Creates a JSON converter instance for the specified <see cref="Patcher{T}"/> type.
	/// </summary>
	/// <param name="typeToConvert">
	/// The type to create a converter for. Must be a type that passes the <see cref="CanConvert"/> check
	/// (i.e., a subclass of <see cref="Patcher{T}"/>).
	/// </param>
	/// <param name="options">The serializer options being used. This parameter is not currently used by the implementation.</param>
	/// <returns>
	/// A <see cref="JsonConverter"/> instance of type <see cref="PatcherJsonConverter{T}"/> configured for the
	/// generic type argument(s) of the <paramref name="typeToConvert"/>
	/// </returns>
	/// <exception cref="InvalidCastException">
	/// Thrown when the converter instance cannot be created, which should be rare but can occur in edge cases
	/// such as missing generic type arguments or reflection failures.
	/// </exception>
	/// <remarks>
	/// <para>
	/// This method performs the following steps to create the appropriate converter:
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Extract type arguments:</strong> Gets the generic type argument(s) from <paramref name="typeToConvert"/> using <see cref="Type.GetGenericArguments"/></description></item>
	/// <item><description><strong>Construct Patcher type:</strong> Creates a closed generic <c>Patcher&lt;T&gt;</c> type using the extracted arguments (currently unused but kept for potential validation)</description></item>
	/// <item><description><strong>Construct converter type:</strong> Creates a closed generic <c>PatcherJsonConverter&lt;T&gt;</c> type with the same type arguments</description></item>
	/// <item><description><strong>Instantiate converter:</strong> Uses <see cref="Activator.CreateInstance(Type)"/> to create an instance of the converter</description></item>
	/// <item><description><strong>Validate and return:</strong> Ensures the instance is not null (throws <see cref="InvalidCastException"/> if null) and casts it to <see cref="JsonConverter"/></description></item>
	/// </list>
	/// <para>
	/// <strong>Reflection usage:</strong>
	/// </para>
	/// <para>
	/// This method uses reflection to construct generic types dynamically. This has a small performance cost during
	/// the first serialization/deserialization of a given <see cref="Patcher{T}"/> type, but the converter instance
	/// is cached by the JSON serializer infrastructure for subsequent operations.
	/// </para>
	/// <para>
	/// <strong>Type argument handling:</strong>
	/// </para>
	/// <para>
	/// The method assumes that <paramref name="typeToConvert"/> is a valid <see cref="Patcher{T}"/> type with at least
	/// one generic type argument. If this assumption is violated (which shouldn't happen if <see cref="CanConvert"/>
	/// returned <c>true</c>), an exception may be thrown by the reflection operations.
	/// </para>
	/// <para>
	/// <strong>Error handling:</strong>
	/// </para>
	/// <para>
	/// The method includes a null check after <see cref="Activator.CreateInstance(Type, object[])"/> with a descriptive exception message.
	/// The exception message includes the converter type name to aid in debugging if issues occur.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Internal behavior demonstration (typically called by JSON serializer)
	/// var factory = new PatcherJsonConverterFactory();
	/// var options = new JsonSerializerOptions();
	/// 
	/// // For Patcher&lt;User&gt;
	/// var userPatcherType = typeof(Patcher&lt;User&gt;);
	/// var converter = factory.CreateConverter(userPatcherType, options);
	/// // Returns: PatcherJsonConverter&lt;User&gt; instance
	/// 
	/// // The converter can then be used for serialization/deserialization
	/// // (typically handled automatically by JsonSerializer)
	/// </code>
	/// 
	/// <strong>Type construction flow:</strong>
	/// <code>
	/// // Given: typeof(Patcher&lt;UserProfile&gt;)
	/// 
	/// // Step 1: Extract type arguments
	/// var types = typeToConvert.GetGenericArguments();
	/// // types = [typeof(UserProfile)]
	/// 
	/// // Step 2: Construct patchable type (for validation)
	/// var patchableType = typeof(Patcher&lt;&gt;).MakeGenericType(types);
	/// // patchableType = typeof(Patcher&lt;UserProfile&gt;)
	/// 
	/// // Step 3: Construct converter type
	/// var converterType = typeof(PatcherJsonConverter&lt;&gt;).MakeGenericType(types);
	/// // converterType = typeof(PatcherJsonConverter&lt;UserProfile&gt;)
	/// 
	/// // Step 4: Instantiate converter
	/// var converter = Activator.CreateInstance(converterType);
	/// // converter = new PatcherJsonConverter&lt;UserProfile&gt;()
	/// 
	/// // Step 5: Cast and return
	/// return (JsonConverter)converter;
	/// </code>
	/// </example>
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var types = typeToConvert.GetGenericArguments();
		var patchableType = typeof(Patcher<>).MakeGenericType(types);
		var converterType = typeof(PatcherJsonConverter<>).MakeGenericType(types);
		return (JsonConverter)(Activator.CreateInstance(converterType) ?? throw new InvalidCastException("PatchableJsonConverter<T> can not be created"));
	}
}