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
	public class FuxionFormattedTypeInfoResolver : DefaultJsonTypeInfoResolver
	{
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
		/// Create a JsonSerializerOptions instance configured for Fuxion formatted serialization and deserialization.
		/// - Indented with tabs
		/// - Allows trailing commas
		/// - Skips comments during reading
		/// - Allow private parameterless constructors during deserialization and orders properties alphabetically
		/// The property give us a new instance if the current one is read-only, otherwise returns the current one.
		/// </summary>
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
public class JsonExtensions<T>(T me) : Extensions<T>(me);
