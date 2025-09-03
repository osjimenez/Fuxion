//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace Fuxion.Linq.Expressions;

//public class FilterConverterFactory : JsonConverterFactory
//{
//	public override bool CanConvert(Type typeToConvert) => typeToConvert.GetInterfaces().Any(i => i == typeof(IFilter));

//	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
//		typeof(FilterConverter<>)
//			.MakeGenericType(typeToConvert)
//			.GetConstructor(Type.EmptyTypes)?
//			.Invoke([]) as JsonConverter;
//}

//internal class MethodTreeNode
//{
//	public required MethodInfo Method { get; set; }
//	public bool Match { get; set; }
//	public List<MethodTreeNode> Children { get; set; } = new();
//}

//public class FilterConverter<T> : JsonConverter<T>
//	where T : class, IFilter
//{
//	private readonly JsonConverter<T> _fallbackConverter = (JsonConverter<T>)JsonSerializerOptions.Default.GetConverter(typeof(T));

//	public override bool CanConvert(Type typeToConvert) => typeToConvert.GetInterfaces().Any(i => i == typeof(IFilter));

//	private MethodTreeNode? SearchImplicitOperatorInDeep(Type searchType, Type operatorType)
//	{
//		MethodTreeNode? res = null;
//		var methods = searchType.GetMethods().Where(m => m.Name == "op_Implicit").ToList();
//		foreach (var method in methods)
//			if (method.GetParameters()[0].ParameterType == operatorType)
//			{
//				if (res is null) res = new MethodTreeNode { Method = method, Match = true };
//				else res.Children.Add(new MethodTreeNode { Method = method, Match = true });
//			}
//			else
//			{
//				var node = SearchImplicitOperatorInDeep(method.GetParameters()[0].ParameterType, operatorType);
//				if (node?.Match ?? false) // Alguno de los hijos tiene operador implícito a U
//				{
//					if (res is null) res = new MethodTreeNode { Method = method, Match = true, Children = node.Children };
//					else res.Children.Add(new MethodTreeNode { Method = method, Match = true, Children = node.Children });
//				}
//			}

//		return res switch
//		{
//			{ Match: true } => res,
//			{ Children.Count: > 1 } => throw new JsonException("No se puede inferir el operador implícito porque se encuentran varias rutas posibles"),
//			_ => res
//		};
//	}

//	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//	{
//		if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
//		{
//			var node = SearchImplicitOperatorInDeep(typeToConvert, typeof(bool));
//			if (node != null)
//			{
//				if (node.Method.GetParameters()[0].ParameterType == typeof(bool))
//				{
//					var val = reader.GetBoolean();
//					var res = node.Method.Invoke(null, [val]);
//					return (T?)res;
//				}
//				else
//				{
//					var val = JsonSerializer.Deserialize(ref reader, node.Method.GetParameters()[0].ParameterType, options);
//					return (T?)node.Method.Invoke(null, [val]);
//				}
//			}

//			throw new JsonException($"El tipo '{typeToConvert.GetSignature()}' no tiene una conversion implícita a Boolean.");
//		}

//		if (reader.TokenType == JsonTokenType.String)
//		{
//			var node = SearchImplicitOperatorInDeep(typeToConvert, typeof(string));
//			if (node != null)
//			{
//				if (node.Method.GetParameters()[0].ParameterType == typeof(string))
//				{
//					var val = reader.GetString();
//					var res = node.Method.Invoke(null, [val]);
//					return (T?)res;
//				}
//				else
//				{
//					var val = JsonSerializer.Deserialize(ref reader, node.Method.GetParameters()[0].ParameterType, options);
//					return (T?)node.Method.Invoke(null, [val]);
//				}
//			}

//			throw new JsonException($"El tipo '{typeToConvert.GetSignature()}' no tiene una conversion implícita a String.");
//		}

//		if (reader.TokenType == JsonTokenType.Number)
//		{
//			List<Type> typesToCheck =
//			[
//				typeof(long),
//				typeof(int),
//				typeof(decimal),
//				typeof(double),
//				typeof(ulong),
//				typeof(uint),
//				typeof(short),
//				typeof(ushort),
//				typeof(byte),
//				typeof(sbyte),
//				typeof(float)
//			];
//			while (typesToCheck.Any())
//			{
//				var node = SearchImplicitOperatorInDeep(typeToConvert, typesToCheck.First());
//				if (node != null)
//				{
//					if (node.Method.GetParameters()[0].ParameterType == typesToCheck.First())
//					{
//						var val = ReadNumberByType(ref reader, typesToCheck.First());
//						var res = node.Method.Invoke(null, [val]);
//						return (T?)res;
//					}
//					else
//					{
//						var val = JsonSerializer.Deserialize(ref reader, node.Method.GetParameters()[0].ParameterType, options);
//						return (T?)node.Method.Invoke(null, [val]);
//					}
//				}

//				typesToCheck.RemoveAt(0);
//			}

//			throw new JsonException($"El tipo '{typeToConvert.GetSignature()}' no tiene una conversion implícita a Number.");
//		}

//		if (reader.TokenType == JsonTokenType.StartArray && typeToConvert
//			.GetInterfaces()
//			.Any(i => i.GenericTypeArguments.Length > 0 && i.GetGenericTypeDefinition() == typeof(IFilterCombination<>)))
//		{
//			reader.Read();
//			T? res = null;
//			while (reader.TokenType != JsonTokenType.EndArray)
//			{
//				var val = (T)JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;
//				if (res is null)
//				{
//					res = val;
//				}
//				else
//				{
//					var comb = (IFilterCombination<T>)res;
//					comb.List ??= [];
//					comb.List.Add(val);
//				}

//				reader.Read();
//			}

//			return res;
//		}

//		return (T?)JsonSerializer.Deserialize(ref reader, typeToConvert, options.Skip<FilterConverterFactory>());

//		object? ReadNumberByType(ref Utf8JsonReader reader, Type type)
//		{
//			return Type.GetTypeCode(type) switch
//			{
//				TypeCode.Int64 => reader.GetInt64(),
//				TypeCode.Int32 => reader.GetInt32(),
//				TypeCode.Int16 => reader.GetInt16(),
//				TypeCode.UInt64 => reader.GetUInt64(),
//				TypeCode.UInt32 => reader.GetUInt32(),
//				TypeCode.UInt16 => reader.GetUInt16(),
//				TypeCode.Byte => reader.GetByte(),
//				TypeCode.SByte => reader.GetSByte(),
//				TypeCode.Decimal => reader.GetDecimal(),
//				TypeCode.Double => reader.GetDouble(),
//				TypeCode.Single => reader.GetSingle(),
//				_ => null
//			};
//		}
//	}

//	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
//	{
//		_fallbackConverter.Write(writer, value, options);
//	}
//}