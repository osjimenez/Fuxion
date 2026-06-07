using Fuxion.Reflection;
using Fuxion.Text.Json;
using Fuxion.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Union;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

static class ErrorConstants
{
   public static readonly HashSet<string> ErrorExtensionsReservedKeys = new(
      [nameof(Error.Payload),
      nameof(Error.Message),
      nameof(Error.Type),
      nameof(Error.Exception)],
      StringComparer.OrdinalIgnoreCase);

   public static ExtensionsDictionary EnsureErrorReservedKeys(ExtensionsDictionary? extensions)
      => ExtensionsDictionary.EnsureReservedKeys(extensions, ErrorExtensionsReservedKeys);

}

[JsonConverter(typeof(ErrorJsonConverter))]
[method: JsonConstructor]
public readonly struct Error(string? message = null, object? type = null, object? payload = null, Exception? exception = null)
{
   public Error(string? message, object? type, object? payload, Exception? exception, ExtensionsDictionary? extensions)
      : this(message, type, payload, exception)
   {
      Extensions = ErrorConstants.EnsureErrorReservedKeys(extensions);
   }

   public Error WithMessage(string message)
   {
      if (message is null)
         throw new ArgumentNullException(nameof(message), $"'{nameof(message)}' cannot be null.");

      return new Error(message, Type, Payload, Exception, Extensions);
   }
   public Error WithType(object type)
   {
      if (type is null)
         throw new ArgumentNullException(nameof(type), $"'{nameof(type)}' cannot be null.");

      return new Error(Message, type, Payload, Exception, Extensions);
   }
   public Error WithPayload(object payload)
   {
      if (payload is null)
         throw new ArgumentNullException(nameof(payload), $"'{nameof(payload)}' cannot be null.");

      return new Error(Message, Type, payload, Exception, Extensions);
   }
   public Error WithException(Exception exception)
   {
      if (exception is null)
         throw new ArgumentNullException(nameof(exception), $"'{nameof(exception)}' cannot be null.");

      return new Error(Message, Type, Payload, exception, Extensions);
   }
   public Error WithExtensions(ExtensionsDictionary extensions)
   {
      if (extensions is null)
         throw new ArgumentNullException(nameof(extensions), $"'{nameof(extensions)}' cannot be null.");

      return new Error(Message, Type, Payload, Exception, extensions);
   }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public object? Payload { get; } = payload;
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? Message { get; } = message;
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public object? Type { get; } = type;
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   [ExceptionConverter(true)]
   public Exception? Exception { get; } = exception;
   [JsonExtensionData]
   public ExtensionsDictionary Extensions
   {
      get => field ?? [with(ErrorConstants.ErrorExtensionsReservedKeys)];
      init => field = ErrorConstants.EnsureErrorReservedKeys(value);
   }
   public T? GetPayloadAs<T>(JsonSerializerOptions? options = null)
   {
      if (Payload is null)
         return default;

      if (Payload is T value)
         return value;

      if (Payload is JsonElement json)
         return json.Deserialize<T>(options);

      throw new InvalidCastException($"Payload is neither '{typeof(T).GetSignature()}' nor '{nameof(JsonElement)}'.");
   }

   public bool TryGetPayloadAs<T>([NotNullWhen(true)] out T? value, JsonSerializerOptions? options = null)
   {
      try
      {
         value = GetPayloadAs<T>(options);
         return value is not null;
      }
      catch
      {
         value = default;
         return false;
      }
   }
   public T? GetTypeAs<T>(JsonSerializerOptions? options = null)
   {
      if (Type is null)
         return default;

      if (Type is T value)
         return value;

      if (Type is JsonElement json)
         return json.Deserialize<T>(options);

      throw new InvalidCastException($"Type is neither '{typeof(T).GetSignature()}' nor '{nameof(JsonElement)}'.");
   }

   public bool TryGetTypeAs<T>([NotNullWhen(true)] out T? value, JsonSerializerOptions? options = null)
   {
      try
      {
         value = GetTypeAs<T>(options);
         return value is not null;
      }
      catch
      {
         value = default;
         return false;
      }
   }
}
public sealed class ErrorJsonConverter : JsonConverter<Error>
{
   public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (reader.TokenType == JsonTokenType.Null)
         return default;

      if (reader.TokenType != JsonTokenType.StartObject)
         throw new JsonException($"Expected StartObject token when deserializing '{typeof(Error).GetSignature()}'.");

      using var json = JsonDocument.ParseValue(ref reader);
      var root = json.RootElement;
      var comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
      var payloadPropertyName = options.ApplyNamingPolicy(nameof(Error.Payload));
      var messagePropertyName = options.ApplyNamingPolicy(nameof(Error.Message));
      var typePropertyName = options.ApplyNamingPolicy(nameof(Error.Type));
      var exceptionPropertyName = options.ApplyNamingPolicy(nameof(Error.Exception));
      var reserved = new HashSet<string>([payloadPropertyName, messagePropertyName, typePropertyName, exceptionPropertyName], comparer);
      var extensions = new ExtensionsDictionary(ErrorConstants.ErrorExtensionsReservedKeys, comparer);

      object? payload = null;
      if (root.TryGetProperty(payloadPropertyName, options, out var payloadElement))
         payload = payloadElement.Deserialize<object?>(options);

      string? message = null;
      if (root.TryGetProperty(messagePropertyName, options, out var messageElement))
         message = messageElement.Deserialize<string?>(options);

      object? type = null;
      if (root.TryGetProperty(typePropertyName, options, out var typeElement))
         type = typeElement.Deserialize<object?>(options);

      foreach (var prop in root.EnumerateObject())
         if (!reserved.Contains(prop.Name))
            extensions[prop.Name] = prop.Value.Deserialize<object?>(options);

      return new Error(message, type, payload, null, extensions);
   }

   public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
   {
      writer.WriteStartObject();

      if (value.Payload is not null)
      {
         writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Payload)));
         JsonSerializer.Serialize(writer, value.Payload, options);
      }

      if (value.Message is not null)
      {
         writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Message)));
         JsonSerializer.Serialize(writer, value.Message, options);
      }

      if (value.Type is not null)
      {
         writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Type)));
         JsonSerializer.Serialize(writer, value.Type, options);
      }

      if (value.Exception is not null)
      {
         writer.WritePropertyName(options.ApplyNamingPolicy(nameof(Error.Exception)));
         var exceptionOptions = new JsonSerializerOptions(options);
         exceptionOptions.Converters.Add(new ExceptionConverter(true));
         JsonSerializer.Serialize(writer, value.Exception, exceptionOptions);
      }

      foreach (var extension in value.Extensions)
      {
         writer.WritePropertyName(extension.Key);
         if (extension.Value is JsonElement element)
            element.WriteTo(writer);
         else
            JsonSerializer.Serialize(writer, extension.Value, options);
      }

      writer.WriteEndObject();
   }
}
public static class ErrorExtensions
{
   extension(Error me)
   {
      public static Error NotFound(string? message = null, object? payload = null, Exception? exception = null, ExtensionsDictionary? extensions = null)
         => new Error(message, HttpStatusCode.NotFound, payload, exception, extensions);
      public bool IsNotFound => me.Type is HttpStatusCode.NotFound;

      public static Error Critical(string? message = null, object? payload = null, Exception? exception = null, ExtensionsDictionary? extensions = null)
         => new Error(message, HttpStatusCode.InternalServerError, payload, exception, extensions);
   }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member