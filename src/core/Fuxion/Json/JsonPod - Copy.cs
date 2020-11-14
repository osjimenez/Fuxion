using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Json
{
	public static class JsonPodExtensions
	{
		public static JsonPod<TPayload, TKey> ToJsonPod<TPayload, TKey>(this TPayload me, TKey key) where TPayload : class => new JsonPod<TPayload, TKey>(me, key);
		public static JsonPod<TPayload, TKey>? FromJsonPod<TPayload, TKey>(this string me) where TPayload : class
			=> me.FromJson<JsonPod<TPayload, TKey>>(options: new JsonSerializerOptions()
				.Transform(_ => _.Converters.Add(new JsonPodConverter<TPayload, TKey>())));
	}
	public class JsonPodConverter<TPayload, TKey> : JsonConverter<JsonPod<TPayload, TKey>>
	{
		public override JsonPod<TPayload, TKey>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException($"Reader must start in '{nameof(JsonTokenType.StartObject)}' state");
			}
			TKey key;
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.PropertyName)
				{
					var proName = reader.GetString();
					reader.Read();
					if (proName == "PayloadKey")
					{
						key = JsonSerializer.Deserialize<TKey>(ref reader, options);
					}
					else if (proName == "Payload")
					{
					}
					else throw new InvalidProgramException();
				}
			}
			//var tt = reader.TokenType switch
			//{
			//	JsonTokenType.Comment => throw new NotImplementedException(),
			//	JsonTokenType.None => throw new NotImplementedException(),
			//	JsonTokenType.StartObject => throw new NotImplementedException(),
			//	JsonTokenType.EndObject => throw new NotImplementedException(),
			//	JsonTokenType.StartArray => throw new NotImplementedException(),
			//	JsonTokenType.EndArray => throw new NotImplementedException(),
			//	JsonTokenType.PropertyName => throw new NotImplementedException(),
			//	JsonTokenType.String => throw new NotImplementedException(),
			//	JsonTokenType.Number => throw new NotImplementedException(),
			//	JsonTokenType.True => throw new NotImplementedException(),
			//	JsonTokenType.False => throw new NotImplementedException(),
			//	JsonTokenType.Null => throw new NotImplementedException(),
			//	_ => throw new InvalidStateException("The value '' is not supported")
			//};
			return null!;
		}
		public override void Write(Utf8JsonWriter writer, JsonPod<TPayload, TKey> value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
	}
	public class JsonPod<TPayload, TKey>
	{
		public JsonPod(TPayload payload, TKey payloadKey)
		{
			PayloadKey = payloadKey;
			Payload = payload;
		}
		public TKey PayloadKey { get; private set; }
		public TPayload Payload { get; private set; }

		public static implicit operator TPayload(JsonPod<TPayload, TKey> payload)=> payload.Payload;
		public JsonPod<T, TKey>? CastWithPayload<T>()
		{
			return new JsonPod<T, TKey>(As<T>()!, PayloadKey);
		}
		public T? As<T>()
		{
			return Payload.ToJson().FromJson<T>();
		}
		public object? As(Type type) => Payload.ToJson().FromJson(type);
		public bool Is<T>() => Is(typeof(T));
		public bool Is(Type type)
		{
			try
			{
				As(type);
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("" + ex.Message);
				return false;
			}
		}
	}
}