﻿using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Fuxion.Text.Json.Serialization.Metadata;

public class PrivateConstructorJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
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
		return jsonTypeInfo;
	}
}