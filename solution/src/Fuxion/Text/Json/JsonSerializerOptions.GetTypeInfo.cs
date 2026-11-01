#if !NET11_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Fuxion.Text.Json;

// System.Text.Json 11 has JsonSerializerOptions.GetTypeInfo<T>(); this is the same for the frameworks before it.
static class JsonSerializerOptionsGetTypeInfoExtensions
{
	extension(JsonSerializerOptions me)
	{
		internal JsonTypeInfo<T> GetTypeInfo<T>() => (JsonTypeInfo<T>)me.GetTypeInfo(typeof(T));
	}
}
#endif
