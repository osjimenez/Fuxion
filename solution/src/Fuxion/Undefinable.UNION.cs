using Fuxion.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[Union]
[JsonConverter(typeof(UndefinableConverterFactory))]
public readonly struct Undefinable<TValue> : IUnion, IEquatable<Undefinable<TValue>>
{
   private const byte UndefinedKind = 0;
   private const byte DefinedKind = 1;

   private readonly byte _kind;
   private readonly TValue? _value;

   static Undefinable()
   {
      if (typeof(TValue) == typeof(None))
         throw new InvalidOperationException($"The {typeof(Undefinable<TValue>).GetSignature()} type arguments are invalid: {nameof(TValue)} cannot be '{nameof(None)}'.");
   }

   public bool IsDefined => _kind == DefinedKind;

   public bool IsUndefined => _kind == UndefinedKind;

   public Undefinable(TValue value)
   {
      _kind = DefinedKind;
      _value = value;
   }

   public Undefinable(None value)
   {
      _kind = UndefinedKind;
      _value = default;
   }

   [EditorBrowsable(EditorBrowsableState.Never)]
   [JsonIgnore]
   public bool HasValue => true;

   [EditorBrowsable(EditorBrowsableState.Never)]
   public object? Value => _kind switch
   {
      DefinedKind => _value,
      _ => None.Value
   };

   [EditorBrowsable(EditorBrowsableState.Never)]
   public bool TryGetValue(out TValue? value)
   {
      if (IsDefined)
      {
         value = _value;
         return true;
      }

      value = default;
      return false;
   }

   [EditorBrowsable(EditorBrowsableState.Never)]
   public bool TryGetValue(out None value)
   {
      if (IsUndefined)
      {
         value = None.Value;
         return true;
      }

      value = default;
      return false;
   }

   public static implicit operator Undefinable<TValue>(TValue value)
      => new(value);

   public static implicit operator Undefinable<TValue>(None value)
      => new(value);

   public static Undefinable<TValue> Undefined => default;

   public bool Equals(Undefinable<TValue> other)
      => _kind == other._kind &&
         (_kind != DefinedKind || EqualityComparer<TValue?>.Default.Equals(_value, other._value));

   public override bool Equals(object? obj)
      => obj is Undefinable<TValue> other && Equals(other);

   public override int GetHashCode()
   {
      if (_kind != DefinedKind) return _kind.GetHashCode();
      if (_value is null) return HashCode.Combine(_kind, 0);
      return HashCode.Combine(_kind, EqualityComparer<TValue>.Default.GetHashCode(_value));
   }

   public static bool operator ==(Undefinable<TValue> left, Undefinable<TValue> right)
      => left.Equals(right);

   public static bool operator !=(Undefinable<TValue> left, Undefinable<TValue> right)
      => !left.Equals(right);

   public static implicit operator TValue(Undefinable<TValue> value)
      => value.IsDefined
         ? value._value!
         : throw new UndefinedException("Implicit conversion between this undefinable and its value type is not allowed because this undefinable is not defined");

   public static implicit operator None(Undefinable<TValue> value)
      => value.IsUndefined
         ? None.Value
         : throw new UndefinedException("Implicit conversion between this undefinable and None is not allowed because this undefinable is not undefined");
}

public class UndefinableConverterFactory : JsonConverterFactory
{
   public static string UndefinedSentinelValue { get; set; } = "--undefined--";

   public override bool CanConvert(Type type) => type.IsSubclassOfGenericDefinition(typeof(Undefinable<>));

   public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
      => (JsonConverter?)Activator.CreateInstance(typeof(UndefinableConverter<>).MakeGenericType(type.GetGenericArguments()[0])) ?? null;
}

public class UndefinableConverter<T> : JsonConverter<Undefinable<T?>>
{
   static readonly Dictionary<Type, bool> UndefinableTypes = new();

   public override bool CanConvert(Type type)
   {
      if (UndefinableTypes.ContainsKey(type)) return true;
      if (type.IsSubclassOfGenericDefinition(typeof(Undefinable<>)))
      {
         UndefinableTypes.Add(type, true);
         return true;
      }

      return false;
   }

   public override Undefinable<T?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if(reader.TokenType == JsonTokenType.String && reader.GetString() == UndefinableConverterFactory.UndefinedSentinelValue)
         return default;
      //if (reader.TokenType == JsonTokenType.StartArray)
      //{
      //   var node = JsonNode.Parse(ref reader);
      //   if (node is JsonArray { Count: 1 } ja && ja[0] == null) return default;
      //   return new(node.Deserialize<T>(options));
      //}

      return new(JsonSerializer.Deserialize<T>(ref reader, options));
   }

   public override void Write(Utf8JsonWriter writer, Undefinable<T?> value, JsonSerializerOptions options)
   {
      if (value.IsUndefined)
      {
         //writer.WriteStartObject();
         //writer.WritePropertyName("IsUndefined");
         //writer.WriteBooleanValue(true);
         //writer.WriteEndObject();
         writer.WriteStringValue(UndefinableConverterFactory.UndefinedSentinelValue);
      }
      else
         JsonSerializer.Serialize(writer, value.Value, options);
   }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member