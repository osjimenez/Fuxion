using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Types;
using Xunit;

namespace Test.Aot;

// The keys and the registry of types with the reflection of System.Text.Json disabled, as with trimming and native AOT:
// the types are registered by the code the generator of Fuxion writes in AotTypes, and a UriKey travels in JSON through
// a source-generated context.
public class TypesWithoutReflectionTest
{
	[Fact(DisplayName = "The generated code registers the types, and the registry gives their keys and types back")]
	public void Registry()
	{
		var registry = new TypeRegistry();
		AotTypes.Register(registry);
		var order = UriKey.Parse("https://meta.fuxion.nexus/aot/order/v1");
		var envelope = UriKey.Parse("https://meta.fuxion.nexus/aot/envelope/v1");
		Assert.Equal(order, registry.GetKey<AotOrder>());
		Assert.Equal(envelope.Construct(order), registry.GetKey<AotEnvelope<AotOrder>>());
		Assert.Equal(envelope.Construct(SystemTypes.List.Key.Construct(SystemTypes.Int.Key)), registry.GetKey<AotEnvelope<List<int>>>());
		Assert.True(registry.TryGetType(order, out var type) && type == typeof(AotOrder));
		var construction = registry.RegisterConstruction<AotEnvelope<AotOrder>>();
		Assert.True(registry.TryGetType(construction, out var constructed) && constructed == typeof(AotEnvelope<AotOrder>));
		Assert.True(registry.TryGetKeyByFingerprint(construction.Fingerprint, out var byFingerprint) && byFingerprint == construction);
		Assert.True(registry.TryGetDefinition(order, out var definition) && definition.Interfaces.Count == 1);
	}

	[Fact(DisplayName = "A UriKey travels in JSON as its canonical form, with a source-generated context")]
	public void Json()
	{
		var message = new AotTypedMessage(UriKey.Parse("https://meta.fuxion.nexus/system/list/v1[https://meta.fuxion.nexus/aot/order/v1]"));
		var json = JsonSerializer.Serialize(message, AotTypesJsonContext.Default.AotTypedMessage);
		Assert.Equal("""{"type":"https://meta.fuxion.nexus/system/list/v1[https://meta.fuxion.nexus/aot/order/v1]"}""", json);
		Assert.Equal(message.Type, JsonSerializer.Deserialize(json, AotTypesJsonContext.Default.AotTypedMessage)!.Type);
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("""{"type":"https://meta.fuxion.nexus/Aot/order/v1"}""", AotTypesJsonContext.Default.AotTypedMessage));
	}
}

[UriKey("https://meta.fuxion.nexus/aot/named/v1", "1.0.0")]
public interface IAotNamed;

[UriKey("https://meta.fuxion.nexus/aot/order/v1", "1.0.0")]
public record AotOrder(string Name) : IAotNamed;

[UriKey("https://meta.fuxion.nexus/aot/envelope/v1", "1.0.0")]
public record AotEnvelope<T>(T Content);

[TypeDefinitions]
static partial class AotTypes;

public record AotTypedMessage(UriKey Type);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AotTypedMessage))]
partial class AotTypesJsonContext : JsonSerializerContext;
