using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fuxion.Pods.Json.Serialization;
using Fuxion.Text.Json;

namespace Fuxion.Pods.Json;

public static class JsonNodePodExtensions
{
	public static IPodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>> ToJsonNode<TDiscriminator, TPayload>(this IPodBuilder<TDiscriminator, TPayload,IPod<TDiscriminator, TPayload>> me, TDiscriminator discriminator)
		where TDiscriminator : notnull
		where TPayload : notnull
		=> new PodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>>(new(discriminator, me.Pod));
	public static IPodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>> ToJsonNode<TDiscriminator, TPayload>(this IPodPreBuilder<TPayload> me, TDiscriminator discriminator)
		where TDiscriminator : notnull
		where TPayload : notnull
		=> new PodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>>(new(discriminator, me.Payload));
	public static IPodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>> FromJsonNode<TDiscriminator>(this IPodBuilder<TDiscriminator, string,IPod<TDiscriminator, string>> me)
		where TDiscriminator : notnull
	{
		JsonSerializerOptions options = new();
		options.PropertyNameCaseInsensitive = true;
		options.Converters.Add(new IPodConverterFactory());
		var res = me.Pod.Payload.Fx.Json.Deserialize<JsonNodePod<TDiscriminator>>(options: options);
		return res.IsSuccess
			? new PodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>>(res.Payload)
			: throw new JsonException("string couldn't be deserialized", res.Exception);
	}
	public static IPodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>> FromJsonNode<TDiscriminator>(this IPodPreBuilder<string> me)
		where TDiscriminator : notnull
	{
		var res = me.Payload.Fx.Json.Deserialize<JsonNodePod<TDiscriminator>>();
		return res.IsSuccess
			? new PodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>>(res.Payload)
			: throw new JsonException("string couldn't be deserialized", res.Exception);
	}

	public static IPodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>> FromJsonNode<TDiscriminator>(this IPodPreBuilder<string> me, out JsonNodePod<TDiscriminator> pod)
		where TDiscriminator : notnull
	{
		var res = me.Payload.Fx.Json.Deserialize<JsonNodePod<TDiscriminator>>();
		if (res.IsError)
			throw new JsonException("string couldn't be deserialized", res.Exception);
		pod = res.Payload;
		return new PodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>>(res.Payload);
	}
	public static IPodBuilder<TDiscriminator, byte[], IPod<TDiscriminator, byte[]>> ToUtf8Bytes<TDiscriminator>(this IPodBuilder<TDiscriminator, JsonNode, JsonNodePod<TDiscriminator>> me, TDiscriminator discriminator)
		where TDiscriminator : notnull
		=> new PodBuilder<TDiscriminator, byte[], IPod<TDiscriminator, byte[]>>(new Pod<TDiscriminator, byte[]>(discriminator, Encoding.UTF8.GetBytes(me.Pod.Payload.ToJsonString())));
}