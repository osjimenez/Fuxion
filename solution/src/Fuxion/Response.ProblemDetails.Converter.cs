using Fuxion.Collections.Generic;
using Fuxion.Union;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using Fuxion.Text.Json;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ErrorProblemDetailsConverter
{
   public const string ErrorTypeExtensionName = "ErrorType";
   public const string ErrorPayloadExtensionName = "ErrorPayload";
   public const string ErrorExceptionExtensionName = "ErrorException";
   public const string ErrorInnerErrorsExtensionName = "ErrorInnerErrors";
   public const string ErrorSourceExtensionName = "ErrorSource";
   public const string ProblemTypeExtensionName = "ProblemType";
   public const string ProblemTitleExtensionName = "ProblemTitle";
   public const string ProblemInstanceExtensionName = "ProblemInstance";

   public static ResponseProblemDetails ToProblemDetails(Error error, JsonSerializerOptions? jsonOptions = null)
   {
      var status = error.Type is HttpStatusCode httpStatus
         ? (int)httpStatus
         : (int)HttpStatusCode.InternalServerError;

      var problem = new ResponseProblemDetails
      {
         Status = status,
         Title = GetTitle(status),
         Detail = error.Message
      };

      if (error.Type is not null)
         //problem.Extensions[GetExtensionName(ErrorTypeExtensionName, jsonOptions)] = error.Type;
         problem.Extensions[jsonOptions.ApplyNamingPolicy(ErrorTypeExtensionName)] = error.Type;
      if (error.Payload is not null)
         problem.Extensions[jsonOptions.ApplyNamingPolicy(ErrorPayloadExtensionName)] = error.Payload;
      if (error.Exception is not null)
         problem.Extensions[jsonOptions.ApplyNamingPolicy(ErrorExceptionExtensionName)] = error.Exception is RemoteException remote
            ? remote.ExceptionJson
            : error.Exception.ToExceptionJson(jsonOptions);
      if (error.InnerErrors is not null)
         problem.Extensions[jsonOptions.ApplyNamingPolicy(ErrorInnerErrorsExtensionName)] = error.InnerErrors;
      if (error.Source is not null)
         problem.Extensions[jsonOptions.ApplyNamingPolicy(ErrorSourceExtensionName)] = error.Source;

      foreach (var extension in error.Extensions)
         problem.Extensions[extension.Key] = extension.Value;

      return problem;
   }

   public static Error ToError(ResponseProblemDetails? problem, JsonSerializerOptions? jsonOptions = null)
   {
      if (problem is null)
         return Error.Critical("The error response body could not be deserialized.");

      var extensions = new ExtensionsDictionary(ErrorConstants.ErrorExtensionsReservedKeys);
      foreach (var extension in problem.Extensions)
         if (!IsReservedProblemExtension(extension.Key))
            extensions[extension.Key] = extension.Value;

      if (problem.Type is not null)
         extensions[jsonOptions.ApplyNamingPolicy(ProblemTypeExtensionName)] = problem.Type;
      if (problem.Title is not null)
         extensions[jsonOptions.ApplyNamingPolicy(ProblemTitleExtensionName)] = problem.Title;
      if (problem.Instance is not null)
         extensions[jsonOptions.ApplyNamingPolicy(ProblemInstanceExtensionName)] = problem.Instance;

      var type = TryGetExtension(problem, ErrorTypeExtensionName, jsonOptions, out var typeObj)
         ? ConvertErrorType(typeObj, jsonOptions)
         : problem.Status is int status
            ? (HttpStatusCode)status
            : null;

      object? payload = null;
      if (TryGetExtension(problem, ErrorPayloadExtensionName, jsonOptions, out var payloadObj))
         payload = payloadObj;

      Exception? exception = null;
      if (TryGetExtension(problem, ErrorExceptionExtensionName, jsonOptions, out var exceptionObj))
         exception = ConvertException(exceptionObj, jsonOptions);

      Error[]? innerErrors = null;
      if (TryGetExtension(problem, ErrorInnerErrorsExtensionName, jsonOptions, out var innerErrorsObj))
         innerErrors = ConvertValue<Error[]>(innerErrorsObj, jsonOptions);

      ErrorSource? source = null;
      if (TryGetExtension(problem, ErrorSourceExtensionName, jsonOptions, out var sourceObj))
         source = ConvertValue<ErrorSource>(sourceObj, jsonOptions);

      return new Error(problem.Detail, type, payload, exception, extensions, innerErrors, source);
   }

   static bool TryGetExtension(ResponseProblemDetails problem, string key, JsonSerializerOptions? jsonOptions, out object? value)
   {
      var extensionName = jsonOptions.ApplyNamingPolicy(key);
      if (problem.Extensions.TryGetValue(extensionName, out value) || problem.Extensions.TryGetValue(key, out value))
         return true;

      foreach (var extension in problem.Extensions)
         if (string.Equals(extension.Key, extensionName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension.Key, key, StringComparison.OrdinalIgnoreCase))
         {
            value = extension.Value;
            return true;
         }

      value = null;
      return false;
   }

   static bool IsReservedProblemExtension(string key)
      => IsExtensionName(key, ErrorTypeExtensionName)
         || IsExtensionName(key, ErrorPayloadExtensionName)
         || IsExtensionName(key, ErrorExceptionExtensionName)
         || IsExtensionName(key, ErrorInnerErrorsExtensionName)
         || IsExtensionName(key, ErrorSourceExtensionName);

   static bool IsExtensionName(string key, string name)
      => string.Equals(key, name, StringComparison.OrdinalIgnoreCase)
         || string.Equals(key, JsonNamingPolicy.CamelCase.ConvertName(name), StringComparison.OrdinalIgnoreCase);

   static object? ConvertErrorType(object? value, JsonSerializerOptions? jsonOptions)
   {
      if (value is null)
         return null;
      if (value is HttpStatusCode)
         return value;
      if (value is JsonElement json)
      {
         if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var code))
            return (HttpStatusCode)code;
         if (json.ValueKind == JsonValueKind.String)
            return json.GetString();

         var options = jsonOptions ?? new JsonSerializerOptions();
         foreach (var converter in ErrorJsonConverter.ErrorTypeConverters)
            if (converter.TryRead(json, out var convertedType, options))
               return convertedType;

         return json.Deserialize<object?>(options);
      }

      return value;
   }

   static Exception? ConvertException(object? value, JsonSerializerOptions? jsonOptions)
   {
      if (value is null)
         return null;
      if (value is Exception exception)
         return exception;
      if (value is ExceptionJson exceptionJson)
         return exceptionJson.ToRemoteException();
      if (value is JsonElement json)
         return json.Deserialize<ExceptionJson?>(jsonOptions)?.ToRemoteException();
      return null;
   }

   static T? ConvertValue<T>(object? value, JsonSerializerOptions? jsonOptions)
   {
      if (value is null)
         return default;
      if (value is T typed)
         return typed;
      if (value is JsonElement json)
         return json.Deserialize<T>(jsonOptions);
      return default;
   }

   static string GetTitle(int status)
   {
      var statusName = Enum.IsDefined(typeof(HttpStatusCode), status)
         ? ((HttpStatusCode)status).ToString()
         : "Error";

      var builder = new StringBuilder(statusName.Length + 8);
      for (var i = 0; i < statusName.Length; i++)
      {
         var current = statusName[i];
         if (i > 0 && char.IsUpper(current) && !char.IsUpper(statusName[i - 1]))
            builder.Append(' ');
         builder.Append(i == 0 ? char.ToUpperInvariant(current) : char.ToLowerInvariant(current));
      }
      return builder.ToString();
   }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
