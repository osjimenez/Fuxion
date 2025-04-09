using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Fuxion.Reflection;

namespace Fuxion.Text.Json.Serialization;

public abstract class PropertyFallbackResolver
{
	internal int Deep { get; set; }
	public abstract bool Match(object value, PropertyInfo propertyInfo);
	public abstract void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers);
	protected void FallbackWriteRaw(object value, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		var converterType = typeof(FallbackConverter<>).MakeGenericType(value.GetType());
		var converter = Activator.CreateInstance(converterType, Deep + 1, resolvers.ToArray());
		converterType.GetMethod(nameof(FallbackConverter<object>.FallbackWriteRaw))?.Invoke(converter, [value, writer, options, resolvers]);
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
			writer.WriteRawValue($"\"{mb.GetSignature(
				includeAccessModifiers: true,
				includeReturn: true,
				includeDeclaringType: true,
				useFullNames: true,
				fullNamesOnlyInMethodName: true,
				includeParameters: true,
				includeParametersNames: true)}\"");
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
		//var writeComma = false;
		foreach (var item in collection)
		{
			//if (writeComma) writer.WriteRawValue($",");
			//writeComma = true;
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
		=> value is Exception && propertyInfo.Name == "StackTrace" && propertyInfo.GetValue(value) is string;
	public override void Do(object value, PropertyInfo propertyInfo, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		if (value is not Exception ex) throw new InvalidProgramException("ex cannot be null");
		writer.WritePropertyName(propertyInfo.Name);
		writer.WriteStartArray();

		var trace = new StackTrace(ex, true); // 'true' pide info de archivo si hay .pdb

		foreach (var frame in trace.GetFrames() ?? [])
		{
			JsonSerializer.Serialize(writer, new StackFrameEntry
			{
				Method = frame.GetMethod()?.GetSignature(
					includeAccessModifiers: true,
					includeReturn: true,
					includeDeclaringType: true,
					useFullNames: true,
					fullNamesOnlyInMethodName: true,
					includeParameters: true,
					includeParametersNames: true),
				File = frame.GetFileName(),
				Line = frame.GetFileLineNumber()
			}, options);
		}
		writer.WriteEndArray();
	}
	class StackFrameEntry
	{
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Method { get; set; }
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? File { get; set; }
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? Line
		{
			get => field;
			set
			{
				if (value == 0)
					field = null;
				else
					field = value;
			}
		}
	}
}

public class ExceptionConverter() : FallbackConverter<Exception>(0,
	new StackTraceFallbackResolver(),
	new MultilineStringToCollectionPropertyFallbackResolver()
	);
public class FallbackConverter<T> : JsonConverter<T>
{
	public FallbackConverter(int deep) : this(deep, []) { }
	public FallbackConverter(int deep, params PropertyFallbackResolver[] resolvers)
	{
		if (!resolvers.OfType<IfNullWritePropertyFallbackResolver>().Any()) this.resolvers.Add(new IfNullWritePropertyFallbackResolver { Deep = deep });
		if (!resolvers.OfType<IfMemberInfoWriteNamePropertyFallbackResolver>().Any()) this.resolvers.Add(new IfMemberInfoWriteNamePropertyFallbackResolver { Deep = deep });
		if (!resolvers.OfType<CollectionPropertyFallbackResolver>().Any()) this.resolvers.Add(new CollectionPropertyFallbackResolver { Deep = deep });
		this.resolvers.AddRange(resolvers.TransformEach(t => t.Deep = deep));
		this.deep = deep;
	}
	readonly int deep = 0;
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
			//opt.ReferenceHandler = ReferenceHandler.Preserve;
			opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			opt.MaxDepth = 8;
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
	public void FallbackWriteRaw(object? value, Utf8JsonWriter writer, JsonSerializerOptions options, List<PropertyFallbackResolver> resolvers)
	{
		try
		{
			if (value is null)
			{
				writer.WriteNullValue();
				return;
			}
			JsonSerializerOptions opt = new(options);
			if (deep <= 3) // INFO: Con 3 funciona, con 4 no
			{
				var converterType = typeof(FallbackConverter<>).MakeGenericType(value.GetType());
				var converter = Activator.CreateInstance(converterType, deep + 1, resolvers.ToArray());
				if (converter is null) throw new InvalidProgramException($"Program couldn't create FallbackConverter<{value.GetType().Name}>");
				opt.Converters.Add((JsonConverter)converter);
			}
			//opt.ReferenceHandler = ReferenceHandler.Preserve;
			opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			opt.MaxDepth = 8;
			var json = value.SerializeToJson(options: opt);
			writer.WriteRawValue(json);
		} catch (Exception ex)
		{
			writer.WriteRawValue($"\"ERROR '{ex.Message}'\"");
		}
	}
}