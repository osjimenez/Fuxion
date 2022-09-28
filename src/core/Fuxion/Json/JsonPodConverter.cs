﻿namespace Fuxion.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public class JsonPodConverter<TPod, TPayload, TKey> : JsonConverter<TPod> where TPod : JsonPod<TPayload, TKey>
{
	public override TPod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var ins = Activator.CreateInstance(typeof(TPod), true);
		if (ins is null) throw new InvalidProgramException($"Could not be created instance of type '{typeof(TPod).GetSignature()}' using its private constructor");
		var pod = (TPod)ins;
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException();
		}
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return pod;
			}
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("The reader expected JsonTokenType.PropertyName");
			}
			string propertyName = reader.GetString() ?? throw new InvalidProgramException("Current property name could not be read from Utf8JsonReader.");
			PropertyInfo prop = pod.GetType().GetProperty(propertyName) ?? throw new InvalidProgramException("Current property could not be obtained from pod object");
			var ele = JsonDocument.ParseValue(ref reader).RootElement;
			if (propertyName == nameof(JsonPod<string, string>.Payload))
			{
				PropertyInfo rawProp = pod.GetType().GetProperty(nameof(JsonPod<string, string>.PayloadValue), BindingFlags.NonPublic | BindingFlags.Instance)
					?? throw new InvalidProgramException($"'{nameof(JsonPod<string, string>.PayloadValue)}' property could not be obtained from pod object");
				var jsonValue = JsonSerializer.Deserialize<JsonValue>(ele.GetRawText(), options);
				rawProp.SetValue(pod, jsonValue);
				try
				{
					var val = ele.Deserialize(prop.PropertyType, new JsonSerializerOptions()
					{
						TypeInfoResolver = new PrivateConstructorContractResolver()
					});
					pod.SetPrivatePropertyValue(prop.Name, val);
				}
				catch { }
			}
			else
			{
				var val = ele.Deserialize(prop.PropertyType, new JsonSerializerOptions()
				{
					TypeInfoResolver = new PrivateConstructorContractResolver()
				});
				pod.SetPrivatePropertyValue(prop.Name, val);
			}
		}
		return pod;
	}
	public override void Write(Utf8JsonWriter writer, TPod value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var prop in value.GetType().GetProperties()
			.Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null))
		{
			writer.WritePropertyName(prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name);
			writer.WriteRawValue(JsonSerializer.Serialize(prop.GetValue(value), options));
		}
		var rawProp = value.GetType().GetProperty(nameof(JsonPod<string, string>.PayloadValue), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		if (rawProp is null) throw new InvalidProgramException($"The pod '{value.GetType().Name}' doesn't has '{nameof(JsonPod<string, string>.PayloadValue)}' property");
		writer.WritePropertyName(rawProp.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? rawProp.Name);
		value.PayloadValue.WriteTo(writer, options);
		writer.WriteEndObject();
	}
}