using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Fuxion.Text.Json.Serialization.Metadata;

public class AlphabeticalOrderJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var jsonTypeInfo = base.GetTypeInfo(type, options);
		var order = 1;

		foreach (var property in jsonTypeInfo.Properties.OrderBy(p => p.Name)) property.Order = order++;

		return jsonTypeInfo;
	}
}