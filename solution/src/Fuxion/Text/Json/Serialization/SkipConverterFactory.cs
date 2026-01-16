using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

namespace Fuxion.Text.Json.Serialization;

/// <summary>
/// Provides a JSON converter factory that skips the execution of a wrapped converter for a specified number of invocations.
/// </summary>
/// <typeparam name="T">The type of the JSON converter to wrap and control.</typeparam>
/// <param name="converter">The converter instance to wrap and skip.</param>
/// <param name="skipCount">The number of times to skip the converter before allowing it to execute. Default is 1.</param>
/// <remarks>
/// <para>
/// <see cref="SkipConverterFactory{T}"/> is a specialized wrapper that intercepts converter invocations and delays
/// their execution until a specified skip count is reached. This is particularly useful for:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Preventing infinite recursion:</strong> When a custom converter needs to serialize/deserialize using default behavior</description></item>
/// <item><description><strong>Conditional converter application:</strong> Applying a converter only after certain conditions are met</description></item>
/// <item><description><strong>Converter chaining:</strong> Creating complex converter pipelines with controlled execution order</description></item>
/// <item><description><strong>Debugging:</strong> Temporarily bypassing converters to isolate serialization issues</description></item>
/// <item><description><strong>Performance optimization:</strong> Skipping expensive converters for specific scenarios</description></item>
/// </list>
/// <para>
/// <strong>How it works:</strong>
/// </para>
/// <para>
/// The factory maintains an internal counter that increments each time <see cref="CanConvert"/> is called.
/// When the counter reaches <paramref name="skipCount"/>, the wrapped converter is allowed to execute:
/// </para>
/// <list type="number">
/// <item><description><strong>Initial calls:</strong> Returns <c>false</c> from <see cref="CanConvert"/>, incrementing the counter</description></item>
/// <item><description><strong>After skip count:</strong> Delegates to the wrapped converter's logic</description></item>
/// <item><description><strong>Converter creation:</strong> Creates the actual converter instance when needed</description></item>
/// </list>
/// <para>
/// <strong>Primary use case - Avoiding recursion in custom converters:</strong>
/// </para>
/// <para>
/// When implementing a custom <see cref="JsonConverter{T}"/> that needs to call <see cref="JsonSerializer"/>.Serialize
/// or <see cref="JsonSerializer"/>.Deserialize for the same type, you typically encounter infinite recursion because
/// the serializer will invoke your converter again. <see cref="SkipConverterFactory{T}"/> solves this by temporarily
/// removing your converter from the pipeline.
/// </para>
/// <para>
/// <strong>Important considerations:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>State management:</strong> The counter is instance-specific and not thread-safe by default</description></item>
/// <item><description><strong>Skip count accuracy:</strong> Must match the exact number of recursion levels needed</description></item>
/// <item><description><strong>Options cloning:</strong> The extension method creates new options to avoid mutating the original</description></item>
/// <item><description><strong>Single converter assumption:</strong> The <see cref="SkipConverterFactoryExtensions.Skip{T}"/> method expects exactly one instance of the target converter</description></item>
/// </list>
/// <para>
/// <strong>Working with JsonConverterFactory:</strong>
/// </para>
/// <para>
/// If the wrapped converter is itself a <see cref="JsonConverterFactory"/>, the skip factory correctly delegates
/// both <see cref="CanConvert"/> and <see cref="CreateConverter"/> calls to the underlying factory.
/// </para>
/// </remarks>
/// <example>
/// <strong>Basic usage - Preventing infinite recursion:</strong>
/// <code>
/// public class CustomPersonConverter : JsonConverter&lt;Person&gt;
/// {
///     public override Person Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
///     {
///         // Skip this converter to avoid infinite recursion
///         var newOptions = options.Skip&lt;CustomPersonConverter&gt;();
///         
///         // Deserialize with default behavior (won't call this converter again)
///         var person = JsonSerializer.Deserialize&lt;Person&gt;(ref reader, newOptions);
///         
///         // Apply custom logic
///         person.Name = person.Name?.ToUpper();
///         return person;
///     }
///     
///     public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
///     {
///         // Skip this converter to use default serialization
///         var newOptions = options.Skip&lt;CustomPersonConverter&gt;();
///         
///         // Custom logic before serialization
///         value.LastModified = DateTime.UtcNow;
///         
///         // Serialize without recursion
///         JsonSerializer.Serialize(writer, value, newOptions);
///     }
/// }
/// </code>
/// 
/// <strong>Multiple skip levels:</strong>
/// <code>
/// public class NestedConverter : JsonConverter&lt;MyType&gt;
/// {
///     public override void Write(Utf8JsonWriter writer, MyType value, JsonSerializerOptions options)
///     {
///         // Skip 2 levels deep
///         var newOptions = options.Skip&lt;NestedConverter&gt;(skipCount: 2);
///         JsonSerializer.Serialize(writer, value, newOptions);
///     }
///     
///     public override MyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
///     {
///         var newOptions = options.Skip&lt;NestedConverter&gt;(skipCount: 2);
///         return JsonSerializer.Deserialize&lt;MyType&gt;(ref reader, newOptions);
///     }
/// }
/// </code>
/// 
/// <strong>Wrapping a JsonConverterFactory:</strong>
/// <code>
/// // If your converter is a factory
/// public class MyConverterFactory : JsonConverterFactory
/// {
///     public override bool CanConvert(Type typeToConvert) 
///         => typeToConvert.IsGenericType;
///     
///     public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
///         => ...; // Factory logic
/// }
/// 
/// // Usage in another converter
/// var newOptions = options.Skip&lt;MyConverterFactory&gt;();
/// // The factory's CanConvert and CreateConverter are properly delegated
/// </code>
/// 
/// <strong>Manual instantiation (without extension method):</strong>
/// <code>
/// var options = new JsonSerializerOptions();
/// var myConverter = new CustomConverter();
/// 
/// // Wrap the converter to skip it once
/// var skipFactory = new SkipConverterFactory&lt;CustomConverter&gt;(myConverter, skipCount: 1);
/// options.Converters.Add(skipFactory);
/// 
/// // First serialization: converter is skipped (Counter = 0, increments to 1)
/// // Second serialization: converter executes (Counter >= SkipCount)
/// </code>
/// 
/// <strong>Error handling:</strong>
/// <code>
/// try
/// {
///     var newOptions = options.Skip&lt;MyConverter&gt;();
/// }
/// catch (JsonException ex) when (ex.Message.Contains("no se ha encontrado"))
/// {
///     Console.WriteLine("Converter not found in options");
/// }
/// catch (JsonException ex) when (ex.Message.Contains("se han encontrado varios"))
/// {
///     Console.WriteLine("Multiple converters of the same type found");
/// }
/// </code>
/// </example>
public class SkipConverterFactory<T>(T converter, int skipCount = 1) : JsonConverterFactory
	where T : JsonConverter
{
	/// <summary>
	/// Gets the current invocation counter indicating how many times <see cref="CanConvert"/> has been called.
	/// </summary>
	/// <value>The number of times the converter has been skipped so far.</value>
	/// <remarks>
	/// <para>
	/// This counter increments each time <see cref="CanConvert"/> returns <c>false</c> (i.e., while skipping).
	/// Once it reaches <see cref="SkipCount"/>, the counter stops incrementing and the wrapped converter
	/// becomes active.
	/// </para>
	/// <para>
	/// <strong>Thread safety:</strong> This counter is not thread-safe. If the same <see cref="JsonSerializerOptions"/>
	/// instance is used concurrently, race conditions may occur.
	/// </para>
	/// </remarks>
	public int Counter { get; private set; } = 0;
	
	/// <summary>
	/// Gets the number of times the converter should be skipped before allowing execution.
	/// </summary>
	/// <value>The skip count specified during construction.</value>
	/// <remarks>
	/// This value determines when the wrapped converter becomes active. When <see cref="Counter"/>
	/// reaches this value, <see cref="CanConvert"/> will delegate to the wrapped converter.
	/// </remarks>
	public int SkipCount => skipCount;
	
	/// <summary>
	/// Gets the wrapped converter instance being controlled by this factory.
	/// </summary>
	/// <value>The original converter passed to the constructor.</value>
	/// <remarks>
	/// This can be either a concrete <see cref="JsonConverter{T}"/> or a <see cref="JsonConverterFactory"/>.
	/// The factory's behavior adapts based on the converter type.
	/// </remarks>
	public T Converter => converter;

	/// <summary>
	/// Determines whether the wrapped converter can convert the specified type, implementing the skip logic.
	/// </summary>
	/// <param name="typeToConvert">The type to check for conversion compatibility.</param>
	/// <returns>
	/// <list type="bullet">
	/// <item><description><c>false</c> if <see cref="Counter"/> is less than <see cref="SkipCount"/> (skip phase)</description></item>
	/// <item><description><c>true</c> if <see cref="Counter"/> >= <see cref="SkipCount"/> and the wrapped converter can handle the type</description></item>
	/// <item><description><c>false</c> if the wrapped converter cannot handle the type (even after skip count reached)</description></item>
	/// </list>
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method implements the core skip logic:
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Check counter:</strong> If <see cref="Counter"/> >= <see cref="SkipCount"/>, proceed to step 2</description></item>
	/// <item><description><strong>Delegate to converter:</strong> 
	///   <list type="bullet">
	///     <item><description>If <see cref="Converter"/> is a <see cref="JsonConverterFactory"/>, call its <see cref="JsonConverterFactory"/>.CanConvert method</description></item>
	///     <item><description>Otherwise, return <c>true</c> (simple converters are assumed to handle their registered type)</description></item>
	///   </list>
	/// </description></item>
	/// <item><description><strong>Skip phase:</strong> If counter hasn't reached skip count, increment <see cref="Counter"/> and return <c>false</c></description></item>
	/// </list>
	/// <para>
	/// <strong>Behavior with JsonConverterFactory:</strong>
	/// </para>
	/// <para>
	/// When the wrapped converter is a <see cref="JsonConverterFactory"/>, this method properly delegates
	/// the type compatibility check to the factory's <see cref="JsonConverterFactory"/>.CanConvert method.
	/// This ensures that factory-based converters maintain their type filtering logic.
	/// </para>
	/// <para>
	/// <strong>Counter increment:</strong>
	/// </para>
	/// <para>
	/// The counter only increments when returning <c>false</c> during the skip phase. Once the skip count
	/// is reached, the counter stops incrementing, and subsequent calls always delegate to the wrapped converter.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Execution flow example
	/// var skipFactory = new SkipConverterFactory&lt;MyConverter&gt;(myConverter, skipCount: 2);
	/// 
	/// // First call: Counter=0, skipCount=2 → returns false, Counter becomes 1
	/// bool result1 = skipFactory.CanConvert(typeof(MyType)); // false
	/// 
	/// // Second call: Counter=1, skipCount=2 → returns false, Counter becomes 2
	/// bool result2 = skipFactory.CanConvert(typeof(MyType)); // false
	/// 
	/// // Third call: Counter=2, skipCount=2 → delegates to myConverter.CanConvert
	/// bool result3 = skipFactory.CanConvert(typeof(MyType)); // true (if myConverter can handle it)
	/// 
	/// // Fourth call: Counter=2 (no longer increments), skipCount=2 → delegates again
	/// bool result4 = skipFactory.CanConvert(typeof(MyType)); // true
	/// </code>
	/// </example>
	public override bool CanConvert(Type typeToConvert)
	{
		if (Counter >= skipCount)
		{
			if (Converter is JsonConverterFactory fac)
			{
				if (fac.CanConvert(typeToConvert))
				{
					return true;
				}
			}
			else
			{
				return true;
			}
		}
		Counter++;
		return false;
	}

	/// <summary>
	/// Creates the actual converter instance for the specified type.
	/// </summary>
	/// <param name="typeToConvert">The type that needs conversion.</param>
	/// <param name="options">The serializer options being used.</param>
	/// <returns>
	/// <list type="bullet">
	/// <item><description>The converter created by the wrapped <see cref="JsonConverterFactory"/> if <see cref="Converter"/> is a factory</description></item>
	/// <item><description>The wrapped <see cref="Converter"/> instance directly if it's not a factory</description></item>
	/// </list>
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method is called by the JSON serializer infrastructure after <see cref="CanConvert"/> returns <c>true</c>.
	/// The implementation checks the type of the wrapped converter:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>If factory:</strong> Calls <see cref="JsonConverterFactory.CreateConverter"/> on the wrapped factory</description></item>
	/// <item><description><strong>If concrete converter:</strong> Returns the converter instance directly</description></item>
	/// </list>
	/// <para>
	/// <strong>Important:</strong> This method is only invoked after the skip count has been reached and
	/// <see cref="CanConvert"/> has returned <c>true</c>.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // With a factory
	/// var factory = new MyConverterFactory();
	/// var skipFactory = new SkipConverterFactory&lt;MyConverterFactory&gt;(factory);
	/// 
	/// // After skip count reached and CanConvert returns true:
	/// var converter = skipFactory.CreateConverter(typeof(MyType), options);
	/// // Returns: factory.CreateConverter(typeof(MyType), options)
	/// 
	/// // With a concrete converter
	/// var concreteConverter = new MyConverter();
	/// var skipFactory2 = new SkipConverterFactory&lt;MyConverter&gt;(concreteConverter);
	/// 
	/// // After skip count reached:
	/// var converter2 = skipFactory2.CreateConverter(typeof(MyType), options);
	/// // Returns: concreteConverter (the same instance)
	/// </code>
	/// </example>
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		=> Converter is JsonConverterFactory fac
			? fac.CreateConverter(typeToConvert, options)
			: Converter;
}

/// <summary>
/// Provides extension methods for <see cref="JsonSerializerOptions"/> to easily skip specific converters.
/// </summary>
/// <remarks>
/// <para>
/// This static class contains the <see cref="Skip{T}"/> extension method that simplifies the process of
/// wrapping a converter with <see cref="SkipConverterFactory{T}"/>. It handles:
/// </para>
/// <list type="bullet">
/// <item><description>Finding the target converter in the options</description></item>
/// <item><description>Validating that exactly one instance exists</description></item>
/// <item><description>Creating a new options instance to avoid mutation</description></item>
/// <item><description>Replacing the converter with a skip-wrapped version</description></item>
/// <item><description>Restoring previously skipped converters that have reached their count</description></item>
/// </list>
/// </remarks>
public static class SkipConverterFactoryExtensions
{
	/// <summary>
	/// Creates a new <see cref="JsonSerializerOptions"/> instance with the specified converter wrapped in a <see cref="SkipConverterFactory{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the converter to skip. Must inherit from <see cref="JsonConverter"/>.</typeparam>
	/// <param name="options">The original serializer options containing the converter to skip.</param>
	/// <param name="skipCount">The number of times to skip the converter before allowing it to execute. Default is 1.</param>
	/// <returns>
	/// A new <see cref="JsonSerializerOptions"/> instance with the converter wrapped in a skip factory.
	/// The original options are not modified.
	/// </returns>
	/// <exception cref="JsonException">
	/// Thrown in the following cases:
	/// <list type="bullet">
	/// <item><description>No converter of type <typeparamref name="T"/> is found in the options (message: "no se ha encontrado")</description></item>
	/// <item><description>Multiple converters of type <typeparamref name="T"/> are found (message: "se han encontrado varios")</description></item>
	/// <item><description>A previously skipped converter hasn't reached its skip count yet (message: "no se ha alcanzado el contador")</description></item>
	/// </list>
	/// </exception>
	/// <remarks>
	/// <para>
	/// <strong>Method behavior:</strong>
	/// </para>
	/// <list type="number">
	/// <item><description><strong>Clone options:</strong> Creates a new <see cref="JsonSerializerOptions"/> instance to avoid mutating the original</description></item>
	/// <item><description><strong>Process existing skip factories:</strong> 
	///   <list type="bullet">
	///     <item><description>Finds any existing <see cref="SkipConverterFactory{T}"/> instances for type <typeparamref name="T"/></description></item>
	///     <item><description>Validates that their counters have reached the skip count (throws if not)</description></item>
	///     <item><description>Removes the skip factory and restores the original converter</description></item>
	///   </list>
	/// </description></item>
	/// <item><description><strong>Find target converter:</strong> 
	///   <list type="bullet">
	///     <item><description>Searches for converters of type <typeparamref name="T"/> in the new options</description></item>
	///     <item><description>Validates exactly one instance exists (throws if zero or multiple)</description></item>
	///   </list>
	/// </description></item>
	/// <item><description><strong>Wrap and replace:</strong>
	///   <list type="bullet">
	///     <item><description>Removes the original converter from the collection</description></item>
	///     <item><description>Wraps it in a new <see cref="SkipConverterFactory{T}"/> with the specified skip count</description></item>
	///     <item><description>Adds the skip factory to the converter collection</description></item>
	///   </list>
	/// </description></item>
	/// </list>
	/// <para>
	/// <strong>Options cloning:</strong>
	/// </para>
	/// <para>
	/// The method creates a new options instance using the copy constructor <c>new JsonSerializerOptions(options)</c>.
	/// This ensures the original options remain unchanged, which is important for:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Preventing side effects on shared options instances</description></item>
	/// <item><description>Allowing multiple skip operations without interference</description></item>
	/// <item><description>Maintaining thread safety when options are used concurrently</description></item>
	/// </list>
	/// <para>
	/// <strong>Restoration of completed skips:</strong>
	/// </para>
	/// <para>
	/// If the options already contain a <see cref="SkipConverterFactory{T}"/> that has completed its skip count,
	/// this method automatically unwraps it and restores the original converter before applying a new skip wrapper.
	/// This prevents accumulation of nested skip factories.
	/// </para>
	/// <para>
	/// <strong>Error messages (Spanish):</strong>
	/// </para>
	/// <para>
	/// The exception messages are in Spanish:
	/// </para>
	/// <list type="bullet">
	/// <item><description>"No se puede saltar el convertidor '...' porque no se ha encontrado." - Converter not found</description></item>
	/// <item><description>"No se puede saltar el convertidor '...' porque se han encontrado varios." - Multiple converters found</description></item>
	/// <item><description>"No se puede saltar el convertidor '...' porque no se ha alcanzado el contador." - Skip count not reached</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Basic usage in a custom converter
	/// public class MyConverter : JsonConverter&lt;MyType&gt;
	/// {
	///     public override void Write(Utf8JsonWriter writer, MyType value, JsonSerializerOptions options)
	///     {
	///         // Create new options with this converter skipped once
	///         var newOptions = options.Skip&lt;MyConverter&gt;();
	///         
	///         // This won't call MyConverter again - uses default serialization
	///         JsonSerializer.Serialize(writer, value, newOptions);
	///     }
	/// }
	/// 
	/// // Skip multiple times
	/// var options2 = options.Skip&lt;MyConverter&gt;(skipCount: 3);
	/// 
	/// // Chain multiple skip operations
	/// var options3 = originalOptions
	///     .Skip&lt;ConverterA&gt;()
	///     .Skip&lt;ConverterB&gt;();
	/// 
	/// // Error handling
	/// try
	/// {
	///     var newOptions = options.Skip&lt;NonExistentConverter&gt;();
	/// }
	/// catch (JsonException ex)
	/// {
	///     Console.WriteLine("Converter not found: " + ex.Message);
	/// }
	/// </code>
	/// </example>
	public static JsonSerializerOptions Skip<T>(this JsonSerializerOptions options, int skipCount = 1)
		where T : JsonConverter
	{
		var newOptions = new JsonSerializerOptions(options);

		var skipCurrents = newOptions.Converters.OfType<SkipConverterFactory<T>>().ToList();
		foreach (var skipCurrent in skipCurrents)
		{
			if (skipCurrent.Counter < skipCurrent.SkipCount) throw new JsonException($"No se puede saltar el convertidor '{skipCurrent.Converter.GetType().GetSignature()}' porque no se ha alcanzado el contador.");
			newOptions.Converters.Remove(skipCurrent);
			newOptions.Converters.Add(skipCurrent.Converter);
		}

		var currents = newOptions.Converters.OfType<T>().ToList();
		if (currents.Count == 0) throw new JsonException($"No se puede saltar el convertidor '{typeof(T).GetSignature()}' porque no se ha encontrado.");
		if (currents.Count > 1) throw new JsonException($"No se puede saltar el convertidor '{typeof(T).GetSignature()}' porque se han encontrado varios.");
		var converter = currents.First();
		newOptions.Converters.Remove(converter);

		newOptions.Converters.Add(new SkipConverterFactory<T>(converter, skipCount));
		return newOptions;
	}
}