using Fuxion.Reflection;
using Fuxion.Union;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public sealed class ExceptionJson
{
   [JsonPropertyName("$type")]
   [JsonPropertyOrder(-1)]
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? Type { get; init; }
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? Message { get; init; }
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? Source { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public StackFrameJson[]? StackTrace { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? HelpLink { get; init; }
   public int HResult { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public ExceptionJson? InnerException { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public ExceptionJson[]? InnerExceptions { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public KeyValuePair<string, object?>[]? Data { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public object? TargetSite { get; init; }

   [JsonExtensionData]
   public ExtensionsDictionary<ExceptionJson> Extensions
   {
      get => field ??= [with(ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys)];
      init => field = ExceptionJsonConstants.EnsureExceptionJsonReservedKeys(value);
   }
}
static class ExceptionJsonConstants
{
   public static readonly HashSet<string> ExceptionJsonExtensionsReservedKeys = new(
      [
         nameof(ExceptionJson.Type),
         nameof(ExceptionJson.Message),
         nameof(ExceptionJson.Source),
         nameof(ExceptionJson.StackTrace),
         nameof(ExceptionJson.HelpLink),
         nameof(ExceptionJson.HResult),
         nameof(ExceptionJson.InnerException),
         nameof(ExceptionJson.InnerExceptions),
         nameof(ExceptionJson.Data),
         nameof(ExceptionJson.TargetSite)
      ],
      StringComparer.OrdinalIgnoreCase);

   public static ExtensionsDictionary<ExceptionJson> EnsureExceptionJsonReservedKeys(ExtensionsDictionary? extensions = null)
      => ExtensionsDictionary.EnsureReservedKeys<ExceptionJson>(extensions, ExceptionJsonExtensionsReservedKeys);
}
public sealed class StackFrameJson
{
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? Method { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? File { get; init; }

   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public int? Line { get; init; }
}
public class ExceptionConverter : JsonConverter<Exception>
{
   public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (reader.TokenType == JsonTokenType.Null)
         return null;

      var res = JsonSerializer.Deserialize<ExceptionJson>(ref reader, options);
      return res?.ToRemoteException();
   }

   public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
   {
      if (value is RemoteException remote)
         JsonSerializer.Serialize(writer, remote.ExceptionJson, options);
      else
         JsonSerializer.Serialize(writer, value.ToExceptionJson(options), options);
   }
}
public static class ExceptionJsonMapper
{
   extension(Exception me)
   {
      public ExceptionJson ToExceptionJson(JsonSerializerOptions? options = null)
      {
         if (me is null)
            throw new ArgumentNullException(nameof(me));

         var extensions = ExceptionJsonConstants.EnsureExceptionJsonReservedKeys();
         foreach (var extension in GetExtensionProperties(me, options))
            extensions[extension.Key] = extension.Value;

         return new()
         {
            Type = me.GetType().GetSignature(),
            Message = GetMessage(me),
            Source = me.Source,
            StackTrace = GetStackTrace(me),
            HelpLink = me.HelpLink,
            HResult = me.HResult,
            InnerException = me.InnerException?.ToExceptionJson(options),
            InnerExceptions = me is AggregateException aggregate && aggregate.InnerExceptions.Count > 0
               ? [.. aggregate.InnerExceptions.Select(ex => ex.ToExceptionJson(options))]
               : null,
            Data = GetData(me, options),
            TargetSite = me.TargetSite?.GetSignature(
               includeAccessModifiers: true,
               includeReturn: true,
               includeDeclaringType: true,
               useFullNames: true,
               fullNamesOnlyInMethodName: true,
               includeParameters: true,
               includeParametersNames: true),
            Extensions = extensions
         };
      }
   }
   extension(ExceptionJson me)
   {
      public RemoteException ToRemoteException()
         => me.InnerExceptions is not null || string.Equals(me.Type, nameof(AggregateException), StringComparison.Ordinal)
            ? new RemoteAggregateException(me)
            : new RemoteException(me);
   }

   // INFO: The message of AggregateException changes between frameworks, with this method we ensure that the message is always the same, with all inner exceptions messages.
   static string? GetMessage(Exception exception)
   {
      var message = exception.Message;
      if (exception is not AggregateException aggregate || aggregate.InnerExceptions.Count == 0)
         return message;

      var suffix = string.Concat(aggregate.InnerExceptions.Select(ex => $" ({ex.Message})"));
      return message.IndexOf(suffix, StringComparison.Ordinal) >= 0
         ? message
         : message + suffix;
   }

   static StackFrameJson[]? GetStackTrace(Exception exception)
   {
      if (exception.StackTrace is null)
         return null;

      var trace = new StackTrace(exception, true);
      var frames = trace.GetFrames();
      if (frames is null || frames.Length == 0)
         return null;

      var res = new List<StackFrameJson>(frames.Length);
      foreach (var frame in frames)
         res.Add(new()
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
            Line = frame.GetFileLineNumber() == 0 ? null : frame.GetFileLineNumber()
         });

      return res.ToArray();
   }

   static KeyValuePair<string, object?>[]? GetData(Exception exception, JsonSerializerOptions? options)
   {
      if (exception.Data.Count == 0)
         return null;

      var res = new List<KeyValuePair<string, object?>>();
      foreach (var key in exception.Data.Keys)
         if (key is string str)
            res.Add(new(str, ToJsonSafeValue(exception.Data[key], options)));

      return res.Count == 0 ? null : res.ToArray();
   }

   static IEnumerable<KeyValuePair<string, object?>> GetExtensionProperties(Exception exception, JsonSerializerOptions? options)
   {
      foreach (var property in exception.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
      {
         if (!property.CanRead || property.GetIndexParameters().Length != 0 || ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys.Contains(property.Name))
            continue;

         object? value;
         try
         {
            value = property.GetValue(exception);
         }
         catch
         {
            continue;
         }

         yield return new(property.Name, ToJsonSafeValue(value, options));
      }
   }

   static object? ToJsonSafeValue(object? value, JsonSerializerOptions? options)
   {
      if (value is null)
         return null;

      if (value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or DateTime or DateTimeOffset or Guid)
         return value;

      if (value is Type type)
         return type.GetSignature(useFullNames: true);

      if (value is MethodBase method)
         return method.GetSignature();

      if (value is MemberInfo member)
         return member.Name;

      try
      {
         var safeOptions = options is null ? new JsonSerializerOptions() : new JsonSerializerOptions(options);
         safeOptions.ReferenceHandler ??= ReferenceHandler.IgnoreCycles;
         var serialized = JsonSerializer.Serialize(value, value.GetType(), safeOptions);
         using var doc = JsonDocument.Parse(serialized);
         return doc.RootElement.Clone();
      }
      catch
      {
         return value.GetType().GetSignature(useFullNames: true);
      }
   }
}
public class RemoteException : FuxionException
{
   public RemoteException(ExceptionJson json)
      : base(json?.Message, json?.InnerException is null ? null : new RemoteException(json.InnerException))
   {
      ExceptionJson = json ?? throw new ArgumentNullException(nameof(json));
      HResult = json.HResult;
      Source = json.Source;
      HelpLink = json.HelpLink;

      if (json.Data is not null)
         foreach (var kvp in json.Data)
            Data[kvp.Key] = ToExceptionDataValue(kvp.Value);
   }

   public ExceptionJson ExceptionJson { get; }

   public string? RemoteType => ExceptionJson.Type;

   public StackFrameJson[]? RemoteStackTrace => ExceptionJson.StackTrace;

   public override string ToString()
   {
      var sb = new StringBuilder();
      sb.Append(nameof(RemoteException));
      if (RemoteType is not null)
         sb.Append(" from '").Append(RemoteType).Append('\'');
      sb.Append(": ").Append(Message);

      if (RemoteStackTrace is not null)
      {
         sb.AppendLine();
         sb.AppendLine("Remote stack trace:");
         foreach (var frame in RemoteStackTrace)
         {
            sb.Append("   at ").Append(frame.Method);
            if (frame.File is not null)
               sb.Append(" in ").Append(frame.File);
            if (frame.Line is not null)
               sb.Append(":line ").Append(frame.Line);
            sb.AppendLine();
         }
      }

      if (InnerException is not null)
         sb.Append(" ---> ").Append(InnerException);

      return sb.ToString();
   }

   static object? ToExceptionDataValue(object? value)
   {
      if (value is not JsonElement json)
         return value;

      return json.ValueKind switch
      {
         JsonValueKind.Null or JsonValueKind.Undefined => null,
         JsonValueKind.String => json.GetString(),
         JsonValueKind.True => true,
         JsonValueKind.False => false,
         JsonValueKind.Number when json.TryGetInt32(out var intValue) => intValue,
         JsonValueKind.Number when json.TryGetInt64(out var longValue) => longValue,
         JsonValueKind.Number when json.TryGetDecimal(out var decimalValue) => decimalValue,
         JsonValueKind.Number when json.TryGetDouble(out var doubleValue) => doubleValue,
         _ => json.GetRawText()
      };
   }
}

public sealed class RemoteAggregateException : RemoteException
{
   public RemoteAggregateException(ExceptionJson json)
      : base(json)
   {
      RemoteInnerExceptions = json.InnerExceptions is null
         ? []
         : [.. json.InnerExceptions.Select(inner => inner.ToRemoteException())];
   }

   public IReadOnlyList<RemoteException> RemoteInnerExceptions { get; }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
