using Fuxion.Reflection;
using Fuxion.Text.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Fuxion.Union.Helpers;
using static Fuxion.Union.ResponseConstants;

namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public sealed class ResponseOfTSuccessJsonConverterFactory : JsonConverterFactory
{
   public override bool CanConvert(Type typeToConvert)
      => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Response<>);

   public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
      => CreateGenericConverter(typeof(ResponseOfTSuccessJsonConverter<>), typeToConvert);
}

public sealed class ResponseOfTSuccessJsonConverter<TSuccess> : JsonConverter<Response<TSuccess>>
   where TSuccess : notnull
{
   public override Response<TSuccess> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (TryReadNullOrThrow(ref reader, typeToConvert))
         return default;

      using var json = ReadObject(ref reader);
      var root = json.RootElement;
      var propertyNames = GetPropertyNames(nameof(Response<>.IsSuccess), options);
      var extensions = GetExtensionData(root, options, propertyNames.ReservedPropertyNames);
      var isSuccess = ReadRequiredBoolean(root, propertyNames.IsSuccess, options, typeToConvert);

      if (isSuccess)
         return new Response<TSuccess>(ReadSuccessPayload<TSuccess>(root, propertyNames, options, typeToConvert))
         {
            Extensions = extensions
         };

      return new Response<TSuccess>(ReadErrorPayload<Error>(root, propertyNames, options, typeToConvert, rejectNullElement: true))
      {
         Extensions = extensions
      };
   }

   public override void Write(Utf8JsonWriter writer, Response<TSuccess> value, JsonSerializerOptions options)
   {
      if (WriteNullIfHasNoValue(writer, value.HasValue))
         return;

      var propertyNames = GetPropertyNames(nameof(Response<>.IsSuccess), options);
      WriteResponseObject(writer, propertyNames, value.IsSuccess, null, value.Extensions, options,
         () => WriteSuccessPayload(writer, (TSuccess)value, propertyNames, options),
         () => WriteErrorPayload(writer, (Error)value, propertyNames, options));
   }
}

public sealed class ResponseOfTSuccessAndTErrorJsonConverterFactory : JsonConverterFactory
{
   public override bool CanConvert(Type typeToConvert)
      => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Response<,>);

   public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
      => CreateGenericConverter(typeof(ResponseOfTSuccessAndTErrorJsonConverter<,>), typeToConvert);
}

public sealed class ResponseOfTSuccessAndTErrorJsonConverter<TSuccess, TError> : JsonConverter<Response<TSuccess, TError>>
   where TSuccess : notnull
   where TError : notnull
{
   public override Response<TSuccess, TError> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (TryReadNullOrThrow(ref reader, typeToConvert))
         return default;

      using var json = ReadObject(ref reader);
      var root = json.RootElement;
      var propertyNames = GetPropertyNames(nameof(Response<,>.IsSuccess), options);
      var extensions = GetExtensionData(root, options, propertyNames.ReservedPropertyNames);
      var isSuccess = ReadRequiredBoolean(root, propertyNames.IsSuccess, options, typeToConvert);

      if (isSuccess)
         return new Response<TSuccess, TError>(ReadSuccessPayload<TSuccess>(root, propertyNames, options, typeToConvert))
         {
            Extensions = extensions
         };

      return new Response<TSuccess, TError>(ReadErrorPayload<TError>(root, propertyNames, options, typeToConvert))
      {
         Extensions = extensions
      };
   }

   public override void Write(Utf8JsonWriter writer, Response<TSuccess, TError> value, JsonSerializerOptions options)
   {
      if (WriteNullIfHasNoValue(writer, value.HasValue))
         return;

      var propertyNames = GetPropertyNames(nameof(Response<,>.IsSuccess), options);
      WriteResponseObject(writer, propertyNames, value.IsSuccess, null, value.Extensions, options,
         () => WriteSuccessPayload(writer, (TSuccess)value, propertyNames, options),
         () => WriteErrorPayload(writer, (TError)value, propertyNames, options));
   }
}

public sealed class ResponseMaybeOfTSuccessJsonConverterFactory : JsonConverterFactory
{
   public override bool CanConvert(Type typeToConvert)
      => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ResponseMaybe<>);

   public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
      => CreateGenericConverter(typeof(ResponseMaybeOfTSuccessJsonConverter<>), typeToConvert);
}

public sealed class ResponseMaybeOfTSuccessJsonConverter<TSuccess> : JsonConverter<ResponseMaybe<TSuccess>>
   where TSuccess : notnull
{
   public override ResponseMaybe<TSuccess> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (TryReadNullOrThrow(ref reader, typeToConvert))
         return default;

      using var json = ReadObject(ref reader);
      var root = json.RootElement;
      var propertyNames = GetPropertyNames(nameof(ResponseMaybe<>.IsSuccess), nameof(ResponseMaybe<>.IsNone), options);
      var extensions = GetExtensionData(root, options, propertyNames.ReservedPropertyNames);
      var state = ReadMaybeState(root, propertyNames, options, typeToConvert);

      if (state.IsSuccess)
      {
         if (state.IsNone)
            return new ResponseMaybe<TSuccess>(ReadNonePayload(root, propertyNames, options))
            {
               Extensions = extensions
            };

         return new ResponseMaybe<TSuccess>(ReadSuccessPayload<TSuccess>(root, propertyNames, options, typeToConvert, includeIsNoneInNullMessage: true))
         {
            Extensions = extensions
         };
      }

      EnsureNoneIsNotError(state, propertyNames);
      return new ResponseMaybe<TSuccess>(ReadErrorPayload<Error>(root, propertyNames, options, typeToConvert, rejectNullElement: true))
      {
         Extensions = extensions
      };
   }

   public override void Write(Utf8JsonWriter writer, ResponseMaybe<TSuccess> value, JsonSerializerOptions options)
   {
      if (WriteNullIfHasNoValue(writer, value.HasValue))
         return;

      var propertyNames = GetPropertyNames(nameof(ResponseMaybe<>.IsSuccess), nameof(ResponseMaybe<>.IsNone), options);
      WriteResponseObject(writer, propertyNames, value.IsSuccess, value.IsNone, value.Extensions, options,
         () => WriteSuccessPayload(writer, (TSuccess)value, propertyNames, options),
         () => WriteErrorPayload(writer, (Error)value, propertyNames, options));
   }
}

public sealed class ResponseMaybeOfTSuccessAndTErrorJsonConverterFactory : JsonConverterFactory
{
   public override bool CanConvert(Type typeToConvert)
      => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ResponseMaybe<,>);

   public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
      => CreateGenericConverter(typeof(ResponseMaybeOfTSuccessAndTErrorJsonConverter<,>), typeToConvert);
}

public sealed class ResponseMaybeOfTSuccessAndTErrorJsonConverter<TSuccess, TError> : JsonConverter<ResponseMaybe<TSuccess, TError>>
   where TSuccess : notnull
   where TError : notnull
{
   public override ResponseMaybe<TSuccess, TError> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (TryReadNullOrThrow(ref reader, typeToConvert))
         return default;

      using var json = ReadObject(ref reader);
      var root = json.RootElement;
      var propertyNames = GetPropertyNames(nameof(ResponseMaybe<,>.IsSuccess), nameof(ResponseMaybe<,>.IsNone), options);
      var extensions = GetExtensionData(root, options, propertyNames.ReservedPropertyNames);
      var state = ReadMaybeState(root, propertyNames, options, typeToConvert);

      if (state.IsSuccess)
      {
         if (state.IsNone)
            return new ResponseMaybe<TSuccess, TError>(ReadNonePayload(root, propertyNames, options))
            {
               Extensions = extensions
            };

         return new ResponseMaybe<TSuccess, TError>(ReadSuccessPayload<TSuccess>(root, propertyNames, options, typeToConvert, includeIsNoneInNullMessage: true))
         {
            Extensions = extensions
         };
      }

      EnsureNoneIsNotError(state, propertyNames);
      return new ResponseMaybe<TSuccess, TError>(ReadErrorPayload<TError>(root, propertyNames, options, typeToConvert))
      {
         Extensions = extensions
      };
   }

   public override void Write(Utf8JsonWriter writer, ResponseMaybe<TSuccess, TError> value, JsonSerializerOptions options)
   {
      if (WriteNullIfHasNoValue(writer, value.HasValue))
         return;

      var propertyNames = GetPropertyNames(nameof(ResponseMaybe<,>.IsSuccess), nameof(ResponseMaybe<,>.IsNone), options);
      WriteResponseObject(writer, propertyNames, value.IsSuccess, value.IsNone, value.Extensions, options,
         () => WriteSuccessPayload(writer, (TSuccess)value, propertyNames, options),
         () => WriteErrorPayload(writer, (TError)value, propertyNames, options));
   }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

file static class Helpers
{
   public static JsonConverter CreateGenericConverter(Type converterGenericType, Type typeToConvert)
      => (JsonConverter)(Activator.CreateInstance(converterGenericType.MakeGenericType(typeToConvert.GetGenericArguments()))
         ?? throw new InvalidOperationException($"Can't create converter for '{typeToConvert.GetSignature()}'."));

   public static bool TryReadNullOrThrow(ref Utf8JsonReader reader, Type typeToConvert)
   {
      if (reader.TokenType == JsonTokenType.Null)
         return true;

      if (reader.TokenType != JsonTokenType.StartObject)
         throw new JsonException($"Expected StartObject token when deserializing '{typeToConvert.GetSignature()}'.");

      return false;
   }

   public static JsonDocument ReadObject(ref Utf8JsonReader reader)
      => JsonDocument.ParseValue(ref reader);

   public static ResponseJsonPropertyNames GetPropertyNames(string isSuccessPropertyName, JsonSerializerOptions options)
      => GetPropertyNames(isSuccessPropertyName, null, options);

   public static ResponseJsonPropertyNames GetPropertyNames(string isSuccessPropertyName, string? isNonePropertyName, JsonSerializerOptions options)
      => new(
         options.ApplyNamingPolicy(isSuccessPropertyName),
         isNonePropertyName is null ? null : options.ApplyNamingPolicy(isNonePropertyName),
         options.ApplyNamingPolicy(PayloadPropertyName),
         options.ApplyNamingPolicy(ErrorPropertyName));

   public static bool ReadRequiredBoolean(JsonElement root, string propertyName, JsonSerializerOptions options, Type typeToConvert)
   {
      if (!root.TryGetProperty(propertyName, options, out var element))
         throw new JsonException($"Property '{propertyName}' is required when deserializing '{typeToConvert.GetSignature()}'.");

      EnsureBoolean(element, propertyName, typeToConvert);
      return element.GetBoolean();
   }

   public static MaybeState ReadMaybeState(JsonElement root, ResponseJsonPropertyNames propertyNames, JsonSerializerOptions options, Type typeToConvert)
   {
      var isSuccess = ReadRequiredBoolean(root, propertyNames.IsSuccess, options, typeToConvert);
      var isNone = false;

      if (propertyNames.IsNone is not null && root.TryGetProperty(propertyNames.IsNone, options, out var isNoneElement))
      {
         EnsureBoolean(isNoneElement, propertyNames.IsNone, typeToConvert);
         isNone = isNoneElement.GetBoolean();
      }

      return new(isSuccess, isNone);
   }

   public static TSuccess ReadSuccessPayload<TSuccess>(JsonElement root, ResponseJsonPropertyNames propertyNames, JsonSerializerOptions options, Type typeToConvert, bool includeIsNoneInNullMessage = false)
      where TSuccess : notnull
   {
      if (!root.TryGetProperty(propertyNames.Payload, options, out var payloadElement))
      {
         if (typeof(TSuccess) == typeof(Unit))
            return (TSuccess)(object)Unit.Value;

         throw new JsonException($"Property '{propertyNames.Payload}' is required when '{propertyNames.IsSuccess}' is true deserializing '{typeToConvert.GetSignature()}'");
      }

      var success = payloadElement.Deserialize<TSuccess>(options);
      if (success is null)
         throw new JsonException(includeIsNoneInNullMessage && propertyNames.IsNone is not null
            ? $"Property '{propertyNames.Payload}' cannot be null when '{propertyNames.IsSuccess}' is true and '{propertyNames.IsNone}' is false."
            : $"Property '{propertyNames.Payload}' cannot be null when '{propertyNames.IsSuccess}' is true.");

      return success;
   }

   public static TError ReadErrorPayload<TError>(JsonElement root, ResponseJsonPropertyNames propertyNames, JsonSerializerOptions options, Type typeToConvert, bool rejectNullElement = false)
      where TError : notnull
   {
      if (!root.TryGetProperty(propertyNames.Error, options, out var errorElement))
         throw new JsonException($"Property '{propertyNames.Error}' is required when '{propertyNames.IsSuccess}' is false deserializing '{typeToConvert.GetSignature()}'.");

      if (rejectNullElement && errorElement.ValueKind == JsonValueKind.Null)
         throw new JsonException($"Property '{propertyNames.Error}' cannot be null when '{propertyNames.IsSuccess}' is false.");

      var error = errorElement.Deserialize<TError>(options);
      if (error is null)
         throw new JsonException($"Property '{propertyNames.Error}' cannot be null when '{propertyNames.IsSuccess}' is false.");

      return error;
   }

   public static None ReadNonePayload(JsonElement root, ResponseJsonPropertyNames propertyNames, JsonSerializerOptions options)
   {
      if (!root.TryGetProperty(propertyNames.Payload, options, out var payloadElement))
         return None.Value;

      return payloadElement.Deserialize<None>(options);
   }

   public static void EnsureNoneIsNotError(MaybeState state, ResponseJsonPropertyNames propertyNames)
   {
      if (state.IsNone && propertyNames.IsNone is not null)
         throw new JsonException($"Property '{propertyNames.IsNone}' cannot be true when '{propertyNames.IsSuccess}' is false.");
   }

   public static bool WriteNullIfHasNoValue(Utf8JsonWriter writer, bool hasValue)
   {
      if (hasValue)
         return false;

      writer.WriteNullValue();
      return true;
   }

   public static void WriteResponseObject(Utf8JsonWriter writer, ResponseJsonPropertyNames propertyNames, bool isSuccess, bool? isNone, ExtensionsDictionary extensions, JsonSerializerOptions options, Action writeSuccess, Action writeError)
   {
      writer.WriteStartObject();
      writer.WriteBoolean(propertyNames.IsSuccess, isSuccess);
      if (isNone is not null && propertyNames.IsNone is not null)
         writer.WriteBoolean(propertyNames.IsNone, isNone.Value);

      if (isSuccess)
      {
         if (isNone != true)
            writeSuccess();
      }
      else
         writeError();

      WriteExtensionData(writer, extensions, options);
      writer.WriteEndObject();
   }

   public static void WriteSuccessPayload<TSuccess>(Utf8JsonWriter writer, TSuccess value, ResponseJsonPropertyNames propertyNames, JsonSerializerOptions options)
      where TSuccess : notnull
   {
      if (typeof(TSuccess) == typeof(Unit))
         return;

      writer.WritePropertyName(propertyNames.Payload);
      JsonSerializer.Serialize(writer, value, options);
   }

   public static void WriteErrorPayload<TError>(Utf8JsonWriter writer, TError value, ResponseJsonPropertyNames propertyNames, JsonSerializerOptions options)
      where TError : notnull
   {
      writer.WritePropertyName(propertyNames.Error);
      JsonSerializer.Serialize(writer, value, options);
   }

   public static ExtensionsDictionary GetExtensionData(JsonElement root, JsonSerializerOptions options, params string[] reservedPropertyNames)
   {
      var reserved = new HashSet<string>(reservedPropertyNames, options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
      var extensions = new ExtensionsDictionary(reserved, options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

      foreach (var prop in root.EnumerateObject())
         if (!reserved.Contains(prop.Name))
            extensions[prop.Name] = prop.Value.Deserialize<object?>(options);

      return extensions;
   }

   public static void WriteExtensionData(Utf8JsonWriter writer, ExtensionsDictionary extensions, JsonSerializerOptions options)
   {
      foreach (var extension in extensions)
      {
         writer.WritePropertyName(extension.Key);
         if (extension.Value is JsonElement element)
            element.WriteTo(writer);
         else
            JsonSerializer.Serialize(writer, extension.Value, options);
      }
   }

   private static void EnsureBoolean(JsonElement element, string propertyName, Type typeToConvert)
   {
      if (element.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
         throw new JsonException($"Property '{propertyName}' must be a boolean when deserializing '{typeToConvert.GetSignature()}'.");
   }
}

file readonly struct ResponseJsonPropertyNames(string isSuccess, string? isNone, string payload, string error)
{
   public string IsSuccess { get; } = isSuccess;
   public string? IsNone { get; } = isNone;
   public string Payload { get; } = payload;
   public string Error { get; } = error;
   public string[] ReservedPropertyNames => IsNone is null
      ? [IsSuccess, Payload, Error]
      : [IsSuccess, IsNone, Payload, Error];
}

file readonly struct MaybeState(bool isSuccess, bool isNone)
{
   public bool IsSuccess { get; } = isSuccess;
   public bool IsNone { get; } = isNone;
}

