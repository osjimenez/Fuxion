using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

namespace Fuxion.Text.Json.Serialization;

public abstract class PropertyFallbackResolver
{
	public abstract bool Match(object value, PropertyInfo propertyInfo);
	public abstract void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers);
	protected void FallbackWriteRaw(object value, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		var converterType = typeof(FallbackConverter<>).MakeGenericType(value.GetType());
		var converter = Activator.CreateInstance(converterType, new object?[] {
			resolvers.ToArray()
		});
		converterType.GetMethod(nameof(FallbackConverter<object>.FallbackWriteRaw))?.Invoke(converter, new[] {
			value, writer, options, resolvers
		});
	}
}

public class IfNullWritePropertyFallbackResolver : PropertyFallbackResolver
{
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is null;
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers) =>
		writer.WriteNull(propertyInfo.Name);
}

public class IfMemberInfoWriteNamePropertyFallbackResolver : PropertyFallbackResolver
{
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is MemberInfo;
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		writer.WritePropertyName(propertyInfo.Name);
		var mi = propertyInfo.GetValue(value) as MemberInfo;
		if (mi is MethodBase mb)
			writer.WriteRawValue($"\"{mi.GetType().Name} => {mb.GetSignature(true, true, true, true, true, true)}\"");
		else
			writer.WriteRawValue($"\"{mi?.Name}\"");
	}
}

public class CollectionPropertyFallbackResolver : PropertyFallbackResolver
{
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is ICollection;
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (propertyInfo.GetValue(value) is not ICollection collection) throw new InvalidProgramException("Collection cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();
		var writeComma = false;
		foreach (var item in collection)
		{
			if (writeComma) writer.WriteRawValue($",");
			writeComma = true;
			FallbackWriteRaw(item, writer, options, resolvers);
		}
		writer.WriteEndArray();
	}
}

public class MultilineStringToCollectionPropertyFallbackResolver : PropertyFallbackResolver
{
	public override bool Match(object value, PropertyInfo propertyInfo) => propertyInfo.GetValue(value) is string s && s.Contains('\r');
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (propertyInfo.GetValue(value) is not string str) throw new InvalidProgramException("str cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();
		foreach (var item in str.SplitInLines()) writer.WriteStringValue(item);
		writer.WriteEndArray();
	}
}

public class StackTraceFallbackResolver : PropertyFallbackResolver
{
	public override bool Match(object value, PropertyInfo propertyInfo)
		=> propertyInfo.Name == "StackTrace" && propertyInfo.GetValue(value) is string;// s && s.Contains('\r');
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (propertyInfo.GetValue(value) is not string str) throw new InvalidProgramException("str cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();
		var lineOrder = 1;
		foreach (var item in str.SplitInLines())
		{
			var val = item;
			val = val.Trim();
			val = val.Replace("at ", "");
			var s1 = val.Split([" in "], StringSplitOptions.RemoveEmptyEntries);
			if (s1.Length > 1)
			{
				var s2 = s1[1].Split([":line"], StringSplitOptions.RemoveEmptyEntries);
				JsonSerializer.Serialize(writer, new
				{
					Order = lineOrder,
					At = s1[0].Trim(),
					In = s2[0].Trim(),
					Line = s2[1].Trim()
				}, options);
			} else
			{
				JsonSerializer.Serialize(writer, new
				{
					Order = lineOrder,
					At = s1[0].Trim()
				}, options);
			}
			lineOrder++;
		}
		writer.WriteEndArray();
	}
}

public class ExceptionConverter() : FallbackConverter<Exception>(
	new StackTraceFallbackResolver(),
	new MultilineStringToCollectionPropertyFallbackResolver()
	);
public class FallbackConverter<T> : JsonConverter<T>
{
	public FallbackConverter() : this([]){}
	public FallbackConverter(params PropertyFallbackResolver[] resolvers)
	{
		if (!resolvers.OfType<IfNullWritePropertyFallbackResolver>().Any()) this.resolvers.Add(new IfNullWritePropertyFallbackResolver());
		if (!resolvers.OfType<IfMemberInfoWriteNamePropertyFallbackResolver>().Any()) this.resolvers.Add(new IfMemberInfoWriteNamePropertyFallbackResolver());
		if (!resolvers.OfType<CollectionPropertyFallbackResolver>().Any()) this.resolvers.Add(new CollectionPropertyFallbackResolver());
		this.resolvers.AddRange(resolvers);
	}
	readonly List<PropertyFallbackResolver> resolvers = new();
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		throw new NotSupportedException($"{nameof(FallbackConverter<T>)} doesn't support deserialization");
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		try
		{
			JsonSerializerOptions opt = new(options);
			var con = opt.Converters.FirstOrDefault(c => c is FallbackConverter<T>);
			if (con is not null) opt.Converters.Remove(con);
			opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			var json = value.SerializeToJson(options: opt);
			writer.WriteRawValue(json);
		} catch
		{
			writer.WriteStartObject();
			foreach (var prop in value.GetType().GetProperties())
			{
				var resolved = false;
				foreach (var resolver in resolvers)
					if (resolver.Match(value, prop))
					{
						resolver.Do(value, prop, writer, options, resolvers);
						resolved = true;
						break;
					}
				if (resolved) continue;
				writer.WritePropertyName(prop.Name);
				FallbackWriteRaw(prop.GetValue(value) ?? throw new NullReferenceException($"The value of property '{prop.Name}' is null"), writer, options, resolvers);
			}
			writer.WriteEndObject();
		}
	}
	public static void FallbackWriteRaw(object value, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		try
		{
			JsonSerializerOptions opt = new(options);
			var converterType = typeof(FallbackConverter<>).MakeGenericType(value.GetType());
			var converter = Activator.CreateInstance(converterType, new object?[] {
				resolvers.ToArray()
			});
			if (converter is null) throw new InvalidProgramException($"Program couldn't create FallbackConverter<{value.GetType().Name}>");
			opt.Converters.Add((JsonConverter)converter);
			var json = value.SerializeToJson(options: opt);
			writer.WriteRawValue(json);
		} catch (Exception ex)
		{
			writer.WriteRawValue($"\"ERROR '{ex.Message}'\"");
		}
	}
}