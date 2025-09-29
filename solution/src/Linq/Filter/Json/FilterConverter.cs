// Re-implement converter to support new Any/All (And/Or) for scalar and navigation collections, keep existing scalar ops and nested filters, and allow primitive literal to map to Equal.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Linq.Filter.Operations;

namespace Fuxion.Linq.Filter.Json;

public class FilterConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) => typeof(IFilter).IsAssignableFrom(typeToConvert);
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var genType = typeof(FilterConverter<>).MakeGenericType(typeToConvert);
		return (JsonConverter?)Activator.CreateInstance(genType);
	}
}

public class FilterConverter<TFilter> : JsonConverter<TFilter> where TFilter : class, IFilter, new()
{
	public override TFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
		var filter = new TFilter();
		var targetType = typeof(TFilter);
		var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject) break;
			if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
			var name = reader.GetString();
			reader.Read();
			var prop = props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (prop == null) { reader.Skip(); continue; }
			var propType = prop.PropertyType;
			var propVal = prop.GetValue(filter);

			// Scalar collections
			if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(ScalarCollectionFilterOperations<>))
			{
				using var doc = JsonDocument.ParseValue(ref reader);
				var root = doc.RootElement;
				if (propVal == null)
				{
					// If writable, create and set; otherwise create temp and configure it (rare case)
					var inst = Activator.CreateInstance(propType)!;
					if (prop.CanWrite) prop.SetValue(filter, inst);
					propVal = prop.CanWrite ? inst : propVal ?? inst;
				}
				ConfigureScalarCollection(root, propVal!, options);
				continue;
			}
			// Navigation collections
			if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(NavigationCollectionFilterOperations<,>))
			{
				using var doc = JsonDocument.ParseValue(ref reader);
				var root = doc.RootElement;
				if (propVal == null)
				{
					var inst = Activator.CreateInstance(propType)!;
					if (prop.CanWrite) prop.SetValue(filter, inst);
					propVal = prop.CanWrite ? inst : propVal ?? inst;
				}
				ConfigureNavigationCollection(root, propVal!, options);
				continue;
			}
			// Scalar operations node
			if (typeof(IFilterOperation).IsAssignableFrom(propType))
			{
				var instance = propVal ?? Activator.CreateInstance(propType)!;
				if (reader.TokenType == JsonTokenType.StartObject)
					ReadOperations(ref reader, instance, options);
				else
					SetEqualOnOperations(instance, ref reader);
				// No SetValue here; properties are usually read-only with initialized instance
				continue;
			}
			// Nested filter
			if (typeof(IFilter).IsAssignableFrom(propType))
			{
				if (prop.CanWrite)
				{
					var nested = JsonSerializer.Deserialize(ref reader, propType, options);
					prop.SetValue(filter, nested);
				}
				else
				{
					using var doc = JsonDocument.ParseValue(ref reader);
					ApplyFilterObject(propVal!, doc.RootElement, options);
				}
				continue;
			}
			reader.Skip();
		}
		return filter;
	}

	void ConfigureScalarCollection(JsonElement root, object ops, JsonSerializerOptions options)
	{
		var type = ops.GetType();
		var anyAndField = type.GetField("AnyAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var anyOrField = type.GetField("AnyOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var allAndField = type.GetField("AllAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var allOrField = type.GetField("AllOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var anyAndList = (IList)anyAndField.GetValue(ops)!;
		var anyOrList = (IList)anyOrField.GetValue(ops)!;
		var allAndList = (IList)allAndField.GetValue(ops)!;
		var allOrList = (IList)allOrField.GetValue(ops)!;
		var elementOpsType = type.GetNestedType("ElementOperations", BindingFlags.Public | BindingFlags.NonPublic)!;

		void AddBlocks(JsonElement? arr, IList list)
		{
			if (arr is null || arr.Value.ValueKind != JsonValueKind.Array) return;
			foreach (var item in arr.Value.EnumerateArray())
			{
				var blk = Activator.CreateInstance(elementOpsType)!;
				ApplyOperationsObject(item, blk);
				var prop = blk.GetType().GetProperty("HasSomeOperationsDefined", BindingFlags.Public | BindingFlags.Instance);
				var hasAny = prop != null && (bool)(prop.GetValue(blk) ?? false);
				if (hasAny) list.Add(blk);
			}
		}
		if (root.TryGetProperty("Any", out var anyEl) && anyEl.ValueKind == JsonValueKind.Object)
		{
			JsonElement tmp;
			AddBlocks(anyEl.TryGetProperty("And", out tmp) ? tmp : (JsonElement?)null, anyAndList);
			AddBlocks(anyEl.TryGetProperty("Or", out tmp) ? tmp : (JsonElement?)null, anyOrList);
		}
		if (root.TryGetProperty("All", out var allEl) && allEl.ValueKind == JsonValueKind.Object)
		{
			JsonElement tmp;
			AddBlocks(allEl.TryGetProperty("And", out tmp) ? tmp : (JsonElement?)null, allAndList);
			AddBlocks(allEl.TryGetProperty("Or", out tmp) ? tmp : (JsonElement?)null, allOrList);
		}
	}

	void ConfigureNavigationCollection(JsonElement root, object ops, JsonSerializerOptions options)
	{
		var type = ops.GetType();
		var childFilterType = type.GetGenericArguments()[0];
		var anyAndField = type.GetField("AnyAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var anyOrField = type.GetField("AnyOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var allAndField = type.GetField("AllAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var allOrField = type.GetField("AllOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
		var anyAndList = (IList)anyAndField.GetValue(ops)!;
		var anyOrList = (IList)anyOrField.GetValue(ops)!;
		var allAndList = (IList)allAndField.GetValue(ops)!;
		var allOrList = (IList)allOrField.GetValue(ops)!;

		void AddBlocks(JsonElement? arr, IList list)
		{
			if (arr is null || arr.Value.ValueKind != JsonValueKind.Array) return;
			foreach (var item in arr.Value.EnumerateArray())
			{
				var child = Activator.CreateInstance(childFilterType)!;
				ApplyFilterObject(child, item);
				var hasAny = (bool)childFilterType.GetMethod("HasAny", BindingFlags.Public | BindingFlags.Instance)!.Invoke(child, null)!;
				if (hasAny) list.Add(child);
			}
		}
		if (root.TryGetProperty("Any", out var anyEl) && anyEl.ValueKind == JsonValueKind.Object)
		{
			JsonElement tmp;
			AddBlocks(anyEl.TryGetProperty("And", out tmp) ? tmp : (JsonElement?)null, anyAndList);
			AddBlocks(anyEl.TryGetProperty("Or", out tmp) ? tmp : (JsonElement?)null, anyOrList);
		}
		if (root.TryGetProperty("All", out var allEl) && allEl.ValueKind == JsonValueKind.Object)
		{
			JsonElement tmp;
			AddBlocks(allEl.TryGetProperty("And", out tmp) ? tmp : (JsonElement?)null, allAndList);
			AddBlocks(allEl.TryGetProperty("Or", out tmp) ? tmp : (JsonElement?)null, allOrList);
		}
	}

	void ApplyFilterObject(object target, JsonElement obj, JsonSerializerOptions? options = null)
	{
		var type = target.GetType();
		foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!obj.TryGetProperty(prop.Name, out var val)) continue;
			var propType = prop.PropertyType;
			var propVal = prop.GetValue(target);
			if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(ScalarCollectionFilterOperations<>))
			{
				ConfigureScalarCollection(val, propVal!, options ?? new());
				continue;
			}
			if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(NavigationCollectionFilterOperations<,>))
			{
				ConfigureNavigationCollection(val, propVal!, options ?? new());
				continue;
			}
			if (typeof(IFilterOperation).IsAssignableFrom(propType))
			{
				if (val.ValueKind == JsonValueKind.Object)
					ApplyOperationsObject(val, propVal!);
				else
					SetLiteralEqual(val, propVal!);
				continue;
			}
			if (typeof(IFilter).IsAssignableFrom(propType))
			{
				ApplyFilterObject(propVal!, val, options);
				continue;
			}
		}
	}
	void ApplyFilterObject<TChildFilter>(TChildFilter target, JsonElement obj) where TChildFilter : class, IFilter
		=> ApplyFilterObject((object)target, obj, null);

	void ApplyOperationsObject(JsonElement obj, object ops)
	{
		var type = ops.GetType();
		foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!obj.TryGetProperty(p.Name, out var val)) continue;
			var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
			object? value = ConvertJsonElement(val, t);
			if (value != null) p.SetValue(ops, value);
		}
	}

	static object? ConvertJsonElement(JsonElement val, Type t)
	{
		switch (val.ValueKind)
		{
			case JsonValueKind.String:
				if (t == typeof(Guid)) return val.GetGuid();
				if (t == typeof(DateTime)) return DateTime.Parse(val.GetString()!);
				if (t == typeof(DateTimeOffset)) return DateTimeOffset.Parse(val.GetString()!);
				if (t == typeof(TimeSpan)) return TimeSpan.Parse(val.GetString()!);
				return val.GetString();
			case JsonValueKind.Number:
				if (t == typeof(int)) return val.GetInt32();
				if (t == typeof(long)) return val.GetInt64();
				if (t == typeof(short)) return val.GetInt16();
				if (t == typeof(byte)) return (byte)val.GetInt32();
				if (t == typeof(sbyte)) return (sbyte)val.GetInt32();
				if (t == typeof(uint)) return val.GetUInt32();
				if (t == typeof(ulong)) return val.GetUInt64();
				if (t == typeof(ushort)) return (ushort)val.GetInt32();
				if (t == typeof(float)) return val.GetSingle();
				if (t == typeof(double)) return val.GetDouble();
				if (t == typeof(decimal)) return val.GetDecimal();
				break;
			case JsonValueKind.True:
			case JsonValueKind.False:
				if (t == typeof(bool)) return val.GetBoolean();
				break;
			case JsonValueKind.Array:
				if (t == typeof(string[])) return val.EnumerateArray().Select(e => e.GetString()!).ToArray();
				break;
		}
		return null;
	}

	void ReadOperations(ref Utf8JsonReader reader, object ops, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
		var type = ops.GetType();
		var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject) break;
			if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
			var name = reader.GetString();
			reader.Read();
			var prop = props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (prop == null) { reader.Skip(); continue; }
			var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
			object? value = null;
			switch (reader.TokenType)
			{
				case JsonTokenType.String:
					value = ConvertJsonElement(JsonDocument.Parse(reader.GetString()!).RootElement, t);
					break;
				case JsonTokenType.Number:
				case JsonTokenType.True:
				case JsonTokenType.False:
				case JsonTokenType.StartArray:
					using (var doc = JsonDocument.ParseValue(ref reader))
						value = ConvertJsonElement(doc.RootElement, t);
					break;
				case JsonTokenType.StartObject:
					using (var doc = JsonDocument.ParseValue(ref reader))
					{
						value = null; // Objects not expected here
					}
					break;
				default:
					reader.Skip();
					break;
			}
			if (value != null) prop.SetValue(ops, value);
		}
	}

	void SetEqualOnOperations(object ops, ref Utf8JsonReader reader)
	{
		var equalProp = ops.GetType().GetProperty("Equal", BindingFlags.Public | BindingFlags.Instance);
		if (equalProp == null) { reader.Skip(); return; }
		object? value;
		var t = Nullable.GetUnderlyingType(equalProp.PropertyType) ?? equalProp.PropertyType;
		using (var doc = JsonDocument.ParseValue(ref reader))
			value = ConvertJsonElement(doc.RootElement, t);
		equalProp.SetValue(ops, value);
	}

	public override void Write(Utf8JsonWriter writer, TFilter value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		var type = typeof(TFilter);
		var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var prop in props)
		{
			var val = prop.GetValue(value);
			if (val == null) continue;
			var pType = prop.PropertyType;
			if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(ScalarCollectionFilterOperations<>))
			{
				if (!HasAnyCollection(val)) continue;
				writer.WritePropertyName(prop.Name);
				WriteScalarCollection(writer, val, options);
				continue;
			}
			if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(NavigationCollectionFilterOperations<,>))
			{
				if (!HasAnyCollection(val)) continue;
				writer.WritePropertyName(prop.Name);
				WriteNavigationCollection(writer, val, options);
				continue;
			}
			if (val is IFilterOperation node && node.IsDefined)
			{
				writer.WritePropertyName(prop.Name);
				WriteOperations(writer, node);
				continue;
			}
			if (val is IFilter nested && FilterHasAny(nested))
			{
				writer.WritePropertyName(prop.Name);
				JsonSerializer.Serialize(writer, nested, nested.GetType(), options);
			}
		}
		writer.WriteEndObject();
	}

	static bool HasAnyCollection(object col)
	{
		var m = col.GetType().GetMethod("HasAny", BindingFlags.Public | BindingFlags.Instance);
		return m != null && (bool)m.Invoke(col, null)!;
	}

	void WriteScalarCollection(Utf8JsonWriter writer, object sc, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		var t = sc.GetType();
		var anyAnd = t.GetField("AnyAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(sc) as IEnumerable;
		var anyOr = t.GetField("AnyOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(sc) as IEnumerable;
		var allAnd = t.GetField("AllAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(sc) as IEnumerable;
		var allOr = t.GetField("AllOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(sc) as IEnumerable;

		void WriteGroup(string name, IEnumerable? and, IEnumerable? or)
		{
			if ((and == null || !and.Cast<object>().Any()) && (or == null || !or.Cast<object>().Any())) return;
			writer.WritePropertyName(name);
			writer.WriteStartObject();
			if (and != null && and.Cast<object>().Any())
			{
				writer.WritePropertyName("And");
				writer.WriteStartArray();
				foreach (var blk in and)
					WriteOperations(writer, blk!);
				writer.WriteEndArray();
			}
			if (or != null && or.Cast<object>().Any())
			{
				writer.WritePropertyName("Or");
				writer.WriteStartArray();
				foreach (var blk in or)
					WriteOperations(writer, blk!);
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
		WriteGroup("Any", anyAnd, anyOr);
		WriteGroup("All", allAnd, allOr);
		writer.WriteEndObject();
	}

	void WriteNavigationCollection(Utf8JsonWriter writer, object nc, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		var t = nc.GetType();
		var anyAnd = t.GetField("AnyAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(nc) as IEnumerable;
		var anyOr = t.GetField("AnyOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(nc) as IEnumerable;
		var allAnd = t.GetField("AllAndBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(nc) as IEnumerable;
		var allOr = t.GetField("AllOrBlocks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(nc) as IEnumerable;

		void WriteGroup(string name, IEnumerable? and, IEnumerable? or)
		{
			if ((and == null || !and.Cast<object>().Any()) && (or == null || !or.Cast<object>().Any())) return;
			writer.WritePropertyName(name);
			writer.WriteStartObject();
			if (and != null && and.Cast<object>().Any())
			{
				writer.WritePropertyName("And");
				writer.WriteStartArray();
				foreach (var blk in and) JsonSerializer.Serialize(writer, blk!, blk!.GetType(), options);
				writer.WriteEndArray();
			}
			if (or != null && or.Cast<object>().Any())
			{
				writer.WritePropertyName("Or");
				writer.WriteStartArray();
				foreach (var blk in or) JsonSerializer.Serialize(writer, blk!, blk!.GetType(), options);
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
		WriteGroup("Any", anyAnd, anyOr);
		WriteGroup("All", allAnd, allOr);
		writer.WriteEndObject();
	}

	static bool FilterHasAny(object filter)
	{
		var props = filter.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var p in props)
		{
			var v = p.GetValue(filter);
			if (v is IFilterOperation n && n.IsDefined) return true;
			if (v is IFilter f && FilterHasAny(f)) return true;
			var t = p.PropertyType;
			if (t.IsGenericType)
			{
				var def = t.GetGenericTypeDefinition();
				if ((def == typeof(ScalarCollectionFilterOperations<>) || def == typeof(NavigationCollectionFilterOperations<,>)) && v != null && HasAnyCollection(v)) return true;
			}
		}
		return false;
	}

	void WriteOperations(Utf8JsonWriter writer, object node)
	{
		writer.WriteStartObject();
		var t = node.GetType();
		var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var prop in props)
		{
			// Skip Specified flags
			if (prop.Name.EndsWith("Specified", StringComparison.Ordinal)) continue;
			var spec = t.GetProperty(prop.Name + "Specified", BindingFlags.Public | BindingFlags.Instance);
			var include = spec != null && spec.PropertyType == typeof(bool) && (bool)(spec.GetValue(node) ?? false);
			if (!include) continue;
			var propVal = prop.GetValue(node);
			if (propVal == null) continue;
			var pt = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
			if (pt == typeof(string)) writer.WriteString(prop.Name, (string)propVal);
			else if (pt == typeof(uint)) writer.WriteNumber(prop.Name, Convert.ToUInt32(propVal));
			else if (pt == typeof(int)) writer.WriteNumber(prop.Name, Convert.ToInt32(propVal));
			else if (pt == typeof(long)) writer.WriteNumber(prop.Name, Convert.ToInt64(propVal));
			else if (pt == typeof(short)) writer.WriteNumber(prop.Name, Convert.ToInt16(propVal));
			else if (pt == typeof(byte)) writer.WriteNumber(prop.Name, Convert.ToByte(propVal));
			else if (pt == typeof(sbyte)) writer.WriteNumber(prop.Name, Convert.ToSByte(propVal));
			else if (pt == typeof(ulong)) writer.WriteNumber(prop.Name, Convert.ToUInt64(propVal));
			else if (pt == typeof(ushort)) writer.WriteNumber(prop.Name, Convert.ToUInt16(propVal));
			else if (pt == typeof(float)) writer.WriteNumber(prop.Name, Convert.ToSingle(propVal));
			else if (pt == typeof(double)) writer.WriteNumber(prop.Name, Convert.ToDouble(propVal));
			else if (pt == typeof(decimal)) writer.WriteNumber(prop.Name, Convert.ToDecimal(propVal));
			else if (pt == typeof(bool)) writer.WriteBoolean(prop.Name, Convert.ToBoolean(propVal));
			else if (pt == typeof(Guid)) writer.WriteString(prop.Name, (Guid)propVal);
			else if (pt == typeof(DateTime)) writer.WriteString(prop.Name, ((DateTime)propVal).ToString("O"));
			else if (pt == typeof(DateTimeOffset)) writer.WriteString(prop.Name, ((DateTimeOffset)propVal).ToString("O"));
			else if (pt == typeof(TimeSpan)) writer.WriteString(prop.Name, propVal.ToString());
			else if (typeof(IEnumerable).IsAssignableFrom(pt) && pt != typeof(string))
			{
				// Basic support for IEnumerable<string>
				if (propVal is IEnumerable<string> strEnum)
				{
					writer.WritePropertyName(prop.Name);
					writer.WriteStartArray();
					foreach (var s in strEnum) writer.WriteStringValue(s);
					writer.WriteEndArray();
				}
			}
		}
		writer.WriteEndObject();
	}

	void SetLiteralEqual(JsonElement val, object opsInstance)
	{
		var equalProp = opsInstance.GetType().GetProperty("Equal", BindingFlags.Public | BindingFlags.Instance);
		if (equalProp == null) return;
		var t = Nullable.GetUnderlyingType(equalProp.PropertyType) ?? equalProp.PropertyType;
		var converted = ConvertJsonElement(val, t);
		if (converted != null) equalProp.SetValue(opsInstance, converted);
	}
}