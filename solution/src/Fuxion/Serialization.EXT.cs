using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using Fuxion.Text.Json.Serialization;
using Fuxion.Text.Json.Serialization.Metadata;

namespace Fuxion;

public static class SerializationExtensions
{
	private static JsonSerializerOptions SerializationJsonSerializerOptions() => new()
	{
		IndentCharacter = '\t',
		IndentSize = 1,
		WriteIndented = true,
		TypeInfoResolver = new AlphabeticalOrderJsonTypeInfoResolver()
	};
	private static JsonSerializerOptions DeserializationJsonSerializerOptions() => new()
	{
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		TypeInfoResolver = new PrivateConstructorJsonTypeInfoResolver()
	};
#if STANDARD_OR_OLD_FRAMEWORKS
	public static JsonSerializerOptions CreateFormattedFuxionJsonSerializerOptionsForSerialization() => SerializationJsonSerializerOptions();
	public static JsonSerializerOptions CreateFormattedFuxionJsonSerializerOptionsForDeserialization() => SerializationJsonSerializerOptions();
#endif
	extension(JsonSerializerOptions me)
	{
		public static JsonSerializerOptions CreateFormattedFuxionOptionsForSerialization() => SerializationJsonSerializerOptions();
		public static JsonSerializerOptions CreateFormattedFuxionOptionsForDeserialization() => DeserializationJsonSerializerOptions();
	}
	extension(object? me)
	{
		public string SerializeToJson(bool formatted = false)
		{
			JsonSerializerOptions? options = null;
			if (formatted)
				options = JsonSerializerOptions.CreateFormattedFuxionOptionsForSerialization();
			return JsonSerializer.Serialize(me, me?.GetType() ?? typeof(object), options);
		}
		public string SerializeToJson(JsonSerializerOptions? options)
			=> JsonSerializer.Serialize(me, me?.GetType() ?? typeof(object), options);
	}

	extension(Exception me)
	{
		public string SerializeToJson(bool formatted = false)
		{
			JsonSerializerOptions? options = null;
			if (formatted)
			{
				options = JsonSerializerOptions.CreateFormattedFuxionOptionsForSerialization();
				options.Converters.Add(new ExceptionConverter());
			}
			return JsonSerializer.Serialize(me, options);
		}

		public string SerializeToJson(JsonSerializerOptions? options)
		{
			options ??= new();
			if(options.IsReadOnly)
				options = new(options);
			if (!options.Converters.Any(c => c.GetType().IsSubclassOf(typeof(ExceptionConverter))))
				options.Converters.Add(new ExceptionConverter());
			return JsonSerializer.Serialize(me, options);
		}
	}

	extension(string me)
	{
		public T? DeserializeFromJson<T>(
			bool formatted = false,
			[DoesNotReturnIf(true)] bool exceptionIfNull = false)
		{
			JsonSerializerOptions? options = null;
			if (formatted)
				options = JsonSerializerOptions.CreateFormattedFuxionOptionsForDeserialization();
			var res = JsonSerializer.Deserialize<T>(me, options);
			if (exceptionIfNull && res is null) throw new SerializationException($"The string cannot be deserialized as '{typeof(T).GetSignature()}':\r\n{me}");
			return res;
		}
		public T? DeserializeFromJson<T>(
			JsonSerializerOptions? options,
			[DoesNotReturnIf(true)] bool exceptionIfNull = false)
		{
			var res = JsonSerializer.Deserialize<T>(me, options);
			if (exceptionIfNull && res is null) throw new SerializationException($"The string cannot be deserialized as '{typeof(T).GetSignature()}':\r\n{me}");
			return res;
		}
		public object? DeserializeFromJson(
			Type type,
			bool formatted = false,
			[DoesNotReturnIf(true)] bool exceptionIfNull = false)
		{
			JsonSerializerOptions? options = null;
			if (formatted)
				options = JsonSerializerOptions.CreateFormattedFuxionOptionsForDeserialization();
			var res = JsonSerializer.Deserialize(me, type, options);
			if (exceptionIfNull && res is null) throw new SerializationException($"The string cannot be deserialized as '{type.GetSignature()}':\r\n{me}");
			return res;
		}
		public object? DeserializeFromJson(
			Type type,
			JsonSerializerOptions? options,
			[DoesNotReturnIf(true)] bool exceptionIfNull = false)
		{
			var res = JsonSerializer.Deserialize(me, type, options);
			if (exceptionIfNull && res is null) throw new SerializationException($"The string cannot be deserialized as '{type.GetSignature()}':\r\n{me}");
			return res;
		}
	}

	extension<T>(T me)
	{
		public T? CloneWithJson()
			=> (T?)(DeserializeFromJson(
				me?.SerializeToJson() ?? throw new InvalidDataException(),
				me.GetType() ?? throw new InvalidDataException()) ?? null);
	}
}