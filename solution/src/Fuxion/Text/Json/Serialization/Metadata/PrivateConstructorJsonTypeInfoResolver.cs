using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Fuxion.Reflection;

namespace Fuxion.Text.Json.Serialization.Metadata;

/// <summary>
/// Provides a custom JSON type information resolver that enables deserialization of types with private or non-public constructors.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PrivateConstructorJsonTypeInfoResolver"/> extends <see cref="DefaultJsonTypeInfoResolver"/> to automatically
/// detect and handle types that don't have public constructors. By default, System.Text.Json cannot deserialize such types
/// because it requires a public parameterless constructor. This resolver works around that limitation by using reflection
/// to invoke non-public constructors via <see cref="Activator.CreateInstance(Type, bool)"/>.
/// </para>
/// <para>
/// <strong>Use cases:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Immutable types:</strong> Classes with private constructors that ensure immutability</description></item>
/// <item><description><strong>Singleton patterns:</strong> Types that use private constructors to control instantiation</description></item>
/// <item><description><strong>Factory patterns:</strong> Classes where direct instantiation should be restricted</description></item>
/// <item><description><strong>Domain models:</strong> Entities with protected constructors for ORM frameworks</description></item>
/// <item><description><strong>Security:</strong> Types that need to control object creation for security reasons</description></item>
/// <item><description><strong>Legacy code:</strong> Existing types that can't be modified to add public constructors</description></item>
/// </list>
/// <para>
/// <strong>How it works:</strong>
/// </para>
/// <para>
/// The resolver inspects each type's metadata during deserialization. If it detects that:
/// </para>
/// <list type="number">
/// <item><description>The type is an object type (not a primitive or collection)</description></item>
/// <item><description>The type has no public instance constructors</description></item>
/// <item><description>No custom <see cref="JsonTypeInfo.CreateObject"/> delegate is already set</description></item>
/// </list>
/// <para>
/// Then it assigns a factory function that uses <see cref="Activator.CreateInstance(Type, bool)"/> with the
/// <c>nonPublic</c> parameter set to <c>true</c>, allowing instantiation of types with private constructors.
/// </para>
/// <para>
/// <strong>Important considerations:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Reflection overhead:</strong> Using reflection to create instances has a performance cost compared to public constructors</description></item>
/// <item><description><strong>Constructor validation:</strong> Private constructors may have invariant checks that aren't called during deserialization</description></item>
/// <item><description><strong>Security implications:</strong> Bypassing access modifiers can create objects in invalid states if not carefully designed</description></item>
/// <item><description><strong>Parameterless requirement:</strong> Only works with parameterless constructors (public or non-public)</description></item>
/// <item><description><strong>Constructor logic:</strong> Any logic in the private constructor WILL execute during deserialization</description></item>
/// </list>
/// <para>
/// <strong>Alternatives to consider:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Add a public parameterless constructor (preferred for serialization scenarios)</description></item>
/// <item><description>Use <c>[JsonConstructor]</c> attribute with constructor parameters</description></item>
/// <item><description>Implement a custom <see cref="JsonConverter{T}"/> for fine-grained control</description></item>
/// <item><description>Use factory methods with custom deserialization logic</description></item>
/// </list>
/// </remarks>
/// <example>
/// <strong>Basic usage with a private constructor:</strong>
/// <code>
/// public class SecureEntity
/// {
///     // Private constructor - normally can't be deserialized
///     private SecureEntity() 
///     { 
///         Id = Guid.NewGuid();
///         CreatedAt = DateTime.UtcNow;
///     }
///     
///     public Guid Id { get; set; }
///     public string Name { get; set; }
///     public DateTime CreatedAt { get; set; }
///     
///     // Factory method for controlled creation
///     public static SecureEntity Create(string name)
///     {
///         return new SecureEntity { Name = name };
///     }
/// }
/// 
/// // Configure the resolver
/// var options = new JsonSerializerOptions
/// {
///     TypeInfoResolver = new PrivateConstructorJsonTypeInfoResolver()
/// };
/// 
/// // Serialize an instance
/// var entity = SecureEntity.Create("Test");
/// string json = JsonSerializer.Serialize(entity, options);
/// // Result: {"Id":"...","Name":"Test","CreatedAt":"..."}
/// 
/// // Deserialize - works even with private constructor!
/// var deserialized = JsonSerializer.Deserialize&lt;SecureEntity&gt;(json, options);
/// Console.WriteLine(deserialized.Name); // "Test"
/// </code>
/// 
/// <strong>Singleton pattern support:</strong>
/// <code>
/// public class Configuration
/// {
///     private static Configuration? _instance;
///     
///     // Private constructor for singleton
///     private Configuration() 
///     { 
///         LoadDefaults();
///     }
///     
///     public static Configuration Instance =&gt; _instance ??= new Configuration();
///     
///     public string ApiUrl { get; set; }
///     public int Timeout { get; set; }
///     
///     private void LoadDefaults()
///     {
///         ApiUrl = "https://api.example.com";
///         Timeout = 30;
///     }
/// }
/// 
/// // Can now deserialize singletons
/// var json = "{\"ApiUrl\":\"https://custom.api.com\",\"Timeout\":60}";
/// var config = JsonSerializer.Deserialize&lt;Configuration&gt;(json, options);
/// </code>
/// 
/// <strong>Combining with other resolvers:</strong>
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     TypeInfoResolver = JsonTypeInfoResolver.Combine(
///         new PrivateConstructorJsonTypeInfoResolver(),
///         new AlphabeticalOrderJsonTypeInfoResolver(),
///         new DefaultJsonTypeInfoResolver()
///     ),
///     WriteIndented = true
/// };
/// </code>
/// 
/// <strong>Protected constructor for Entity Framework:</strong>
/// <code>
/// public class DomainEntity
/// {
///     // Protected constructor for EF Core
///     protected DomainEntity() { }
///     
///     // Public constructor for application code
///     public DomainEntity(string name, string description)
///     {
///         Name = name ?? throw new ArgumentNullException(nameof(name));
///         Description = description;
///     }
///     
///     public int Id { get; set; }
///     public string Name { get; set; }
///     public string Description { get; set; }
/// }
/// 
/// // Resolver enables JSON deserialization for EF entities
/// var json = "{\"Id\":1,\"Name\":\"Product\",\"Description\":\"A sample product\"}";
/// var entity = JsonSerializer.Deserialize&lt;DomainEntity&gt;(json, options);
/// </code>
/// 
/// <strong>Handling initialization logic in private constructors:</strong>
/// <code>
/// public class ValidatedEntity
/// {
///     private ValidatedEntity()
///     {
///         // This initialization logic WILL run during deserialization
///         ValidationErrors = new List&lt;string&gt;();
///         CreatedAt = DateTime.UtcNow; // Gets overwritten by property setters
///     }
///     
///     public string Name { get; set; }
///     public DateTime CreatedAt { get; set; }
///     public List&lt;string&gt; ValidationErrors { get; set; }
/// }
/// 
/// // Constructor runs, then properties are set from JSON
/// var json = "{\"Name\":\"Test\",\"CreatedAt\":\"2024-01-01T00:00:00Z\"}";
/// var entity = JsonSerializer.Deserialize&lt;ValidatedEntity&gt;(json, options);
/// // entity.ValidationErrors exists (from constructor)
/// // entity.CreatedAt is "2024-01-01..." (from JSON, overriding constructor value)
/// </code>
/// </example>
public class PrivateConstructorJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
	/// <summary>
	/// Gets the type information for the specified type and configures object creation for types with non-public constructors.
	/// </summary>
	/// <param name="type">The type to get information for.</param>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization.</param>
	/// <returns>
	/// A <see cref="JsonTypeInfo"/> instance with a custom <see cref="JsonTypeInfo.CreateObject"/> delegate
	/// configured for types without public constructors.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method implements the core logic of the resolver with the following algorithm:
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Base processing:</strong> Calls <see cref="DefaultJsonTypeInfoResolver.GetTypeInfo"/> to get standard metadata</description></item>
	/// <item><description><strong>Type validation:</strong> Checks if the type requires special handling:
	///   <list type="bullet">
	///     <item><description>Must be an object type (<see cref="JsonTypeInfo.Kind"/> == <see cref="JsonTypeInfoKind.Object"/>)</description></item>
	///     <item><description>Must not already have a custom <see cref="JsonTypeInfo.CreateObject"/> factory</description></item>
	///     <item><description>Must have zero public instance constructors</description></item>
	///   </list>
	/// </description></item>
	/// <item><description><strong>Factory assignment:</strong> If conditions are met, assigns a factory lambda that:
	///   <list type="bullet">
	///     <item><description>Uses <see cref="Activator.CreateInstance(Type, bool)"/> with <c>nonPublic: true</c></description></item>
	///     <item><description>Validates the result is not null (throws <see cref="InvalidOperationException"/> if creation fails)</description></item>
	///     <item><description>Returns the created instance</description></item>
	///   </list>
	/// </description></item>
	/// </list>
	/// <para>
	/// <strong>Public constructor detection:</strong>
	/// </para>
	/// <para>
	/// The method uses <see cref="Type.GetConstructors(BindingFlags)"/> with <see cref="BindingFlags.Public"/> and
	/// <see cref="BindingFlags.Instance"/> flags. If this returns an empty array, the type has no public constructors
	/// and the custom factory is applied.
	/// </para>
	/// <para>
	/// <strong>Error handling:</strong>
	/// </para>
	/// <para>
	/// If <see cref="Activator.CreateInstance(Type, bool)"/> returns <c>null</c> (which should be rare but possible
	/// in edge cases), an <see cref="InvalidOperationException"/> is thrown with a detailed message including the
	/// type signature via <see cref="Fuxion.Reflection.ReflectionExtensions.GetSignature(Type, bool)"/>.
	/// </para>
	/// <para>
	/// <strong>Performance notes:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Constructor inspection happens once per type during metadata initialization</description></item>
	/// <item><description>Reflection-based instantiation occurs on every deserialization (runtime cost)</description></item>
	/// <item><description>The factory delegate is cached with the <see cref="JsonTypeInfo"/> metadata</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Internal behavior demonstration
	/// 
	/// // Type with no public constructors
	/// public class PrivateEntity
	/// {
	///     private PrivateEntity() { }
	///     public int Id { get; set; }
	/// }
	/// 
	/// // During first deserialization:
	/// // 1. GetTypeInfo is called with typeof(PrivateEntity)
	/// // 2. Base resolver creates jsonTypeInfo
	/// // 3. Checks: Kind == Object ✓, CreateObject == null ✓
	/// // 4. GetConstructors(Public | Instance).Length == 0 ✓
	/// // 5. Assigns: jsonTypeInfo.CreateObject = () => Activator.CreateInstance(...)
	/// // 6. Returns modified jsonTypeInfo
	/// 
	/// // On subsequent deserializations:
	/// // - Cached jsonTypeInfo is reused
	/// // - CreateObject factory is invoked to create instances
	/// </code>
	/// </example>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <see cref="Activator.CreateInstance(Type, bool)"/> fails to create an instance
	/// and returns <c>null</c>. The exception message includes the type signature for debugging.
	/// </exception>
	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var jsonTypeInfo = base.GetTypeInfo(type, options);
		if (jsonTypeInfo is not { Kind: JsonTypeInfoKind.Object, CreateObject: null }) return jsonTypeInfo;
		if (jsonTypeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
			// The type doesn't have public constructors
			jsonTypeInfo.CreateObject = () =>
				Activator.CreateInstance(jsonTypeInfo.Type, true)
				?? throw new InvalidOperationException($"Instance of type '{jsonTypeInfo.Type.GetSignature()}' could not be created with non public constructor");
		return jsonTypeInfo;
	}
}