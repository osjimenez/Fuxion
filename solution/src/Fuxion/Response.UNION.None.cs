using Fuxion.Reflection;
using Fuxion.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[JsonConverter(typeof(NoneJsonConverter))]
public readonly struct None
{
   public static readonly None Value = default;
}

public sealed class NoneJsonConverter : JsonConverter<None>
{
   public override None Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (reader.TokenType == JsonTokenType.Null)
         return None.Value;

      if (reader.TokenType == JsonTokenType.StartObject)
      {
         reader.Read();
         if (reader.TokenType == JsonTokenType.EndObject)
            return None.Value;
      }

      throw new JsonException("None must be null or an empty json object");
   }

   public override void Write(Utf8JsonWriter writer, None value, JsonSerializerOptions options)
      => writer.WriteNullValue();
}


#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member