using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Text.Json.Serialization;

public class InterfaceSerializerConverter<TInterface> : JsonConverter<TInterface>
{
	public override bool CanConvert(Type type) => type.IsInterface && (type == typeof(TInterface) || typeof(TInterface).IsAssignableFrom(type));
	public override TInterface Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		=> throw new SerializationException($"Deserialize from an interface is not supported by '{typeof(InterfaceSerializerConverter<>).MakeGenericType(type).GetSignature()}'");
	public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
	{
		if (value is null)
			writer.WriteNullValue();
		else
		{
			var type = value.GetType();
			JsonSerializer.Serialize(writer, value, type, options);
		}
	}
}