using Fuxion.Collections.Generic;
using Fuxion.Text.Json;
using Fuxion.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Reflection;

namespace Fuxion.Union.Net.Http;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseExtensions
{
   public static ResponseHttpMode DefaultResponseMode { get; set; } = ResponseHttpMode.Result;

   static InvalidOperationException CreateResultModeError(string message, Exception? innerException = null)
      => new($"Result mode error handling for typed UNION responses is not implemented yet. {message}", innerException);

   extension(Task<HttpResponseMessage> me)
	{
		public async Task<IResponse> AsResponseAsync<T>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
		{
         var body = await (await me).Content.ReadAsStringAsync();
         return body.Fx.Json.Deserialize<Response<string>>(options: jsonOptions).PayloadOrThrow();
      }

		//public async Task<IResponse<TPayload>> AsResponseAsync<TPayload>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
		//	=> await Extensions.AsResponseFromMessageAsync<TPayload>(await me, jsonOptions, ct);
	}

	extension(HttpResponseMessage me)
	{
      public async Task<ResponseMaybe<TSuccess>> AsResponseAsync<TSuccess>(ResponseHttpMode? mode = null, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
         where TSuccess : notnull
      {
         if (me.StatusCode == HttpStatusCode.NoContent)
            return None.Value;
         mode ??= DefaultResponseMode;
         if (mode == ResponseHttpMode.Response)
         {
            var body = await me.Content.ReadAsStringAsync();
            var deserializationResponse = body.Fx.Json.Deserialize<ResponseMaybe<TSuccess>>(options: jsonOptions);
            if (deserializationResponse.IsError)
               return Error.Critical(deserializationResponse.Message, exception: deserializationResponse.Exception); // PEND to implement
            return deserializationResponse.Payload;
         }
         else if (mode == ResponseHttpMode.Result)
         {
            if (!me.IsSuccessStatusCode)
               return await ReadErrorResponse<TSuccess>(me, jsonOptions);

            var body = await me.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
               return typeof(TSuccess) == typeof(Unit)
                  ? (ResponseMaybe<TSuccess>)(object)Unit.Value
                  : Error.Critical($"The response status code is '{(int)me.StatusCode}' and the body is empty.");

            if (typeof(TSuccess) == typeof(Unit))
            {
               var responseDeserialization = body!.Fx.Json.Deserialize<Response<Unit>>(options: jsonOptions);
               if (responseDeserialization.IsSuccess)
               {
                  TSuccess success = (TSuccess)(object)((Response<Unit>)responseDeserialization.Payload).Value!;
                  return success;
               }

               return Error.Critical(responseDeserialization.Message, exception: responseDeserialization.Exception);
            }

            var payloadDeserialization = body!.Fx.Json.Deserialize<TSuccess>(options: jsonOptions);
            if (payloadDeserialization.IsSuccess)
               return payloadDeserialization.Payload;

            return Error.Critical(payloadDeserialization.Message, exception: payloadDeserialization.Exception);
         }
         else throw new NotSupportedException($"'{nameof(ResponseHttpMode)}.{mode}' not supported.");
      }
      public async Task<ResponseMaybe<TSuccess, TError>> AsResponseAsync<TSuccess, TError>(ResponseHttpMode? mode = null, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
         where TSuccess : notnull
         where TError : notnull
      {
         if (me.StatusCode == HttpStatusCode.NoContent)
            return None.Value;

         mode ??= DefaultResponseMode;
         if (mode == ResponseHttpMode.Response)
         {
            var body = await me.Content.ReadAsStringAsync();
            var deserializationResponse = body.Fx.Json.Deserialize<ResponseMaybe<TSuccess, TError>>(options: jsonOptions);
            if (deserializationResponse.IsError)
               return default; // PEND to implement
            return deserializationResponse.Payload;
         }
         else if (mode == ResponseHttpMode.Result)
         {
            if (!me.IsSuccessStatusCode)
               return await ReadErrorResponse<TSuccess, TError>(me, jsonOptions);

            var body = await me.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
               return typeof(TSuccess) == typeof(Unit)
                  ? (ResponseMaybe<TSuccess, TError>)(object)(TSuccess)(object)Unit.Value
                  : throw new InvalidOperationException($"Cannot deserialize an empty body as '{typeof(TSuccess).GetSignature()}' in '{nameof(ResponseHttpMode)}.{nameof(ResponseHttpMode.Result)}'.");

            if (typeof(TSuccess) == typeof(Unit))
            {
               var responseDeserialization = body!.Fx.Json.Deserialize<Response<Unit>>(options: jsonOptions);
               if (responseDeserialization.IsSuccess)
               {
                  TSuccess success = (TSuccess)(object)((Response<Unit>)responseDeserialization.Payload).Value!;
                  return success;
               }

               throw CreateResultModeError(responseDeserialization.Message ?? "The UNION response body could not be deserialized.", responseDeserialization.Exception);
            }

            var payloadDeserialization = body!.Fx.Json.Deserialize<TSuccess>(options: jsonOptions);
            if (payloadDeserialization.IsSuccess)
               return payloadDeserialization.Payload;

            throw CreateResultModeError(payloadDeserialization.Message ?? $"The payload body could not be deserialized as '{typeof(TSuccess).GetSignature()}'.", payloadDeserialization.Exception);
         }
         else throw new NotSupportedException($"'{nameof(ResponseHttpMode)}.{mode}' not supported.");
      }
      //public async Task<IResponse> AsResponseAsync(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
      //	=> await Extensions.AsResponseFromMessageAsync(res, jsonOptions, ct);

      //public async Task<IResponse<TPayload>> AsResponseAsync<TPayload>(JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
      //	=> await Extensions.AsResponseFromMessageAsync<TPayload>(res, jsonOptions, ct);
   }

   static async Task<ResponseMaybe<TSuccess>> ReadErrorResponse<TSuccess>(HttpResponseMessage me, JsonSerializerOptions? jsonOptions)
      where TSuccess : notnull
   {
      var body = await me.Content.ReadAsStringAsync();
      var deserialization = body.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions);
      if (deserialization.IsError)
         return Error.Critical(deserialization.Message, exception: deserialization.Exception);

      return CreateErrorFromProblemDetails(deserialization.Payload, jsonOptions);
   }

   static async Task<ResponseMaybe<TSuccess, TError>> ReadErrorResponse<TSuccess, TError>(HttpResponseMessage me, JsonSerializerOptions? jsonOptions)
      where TSuccess : notnull
      where TError : notnull
   {
      var body = await me.Content.ReadAsStringAsync();
      var deserialization = body.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions);
      if (deserialization.IsError)
         throw CreateResultModeError(deserialization.Message ?? "The error body could not be deserialized as ResponseProblemDetails.", deserialization.Exception);

      throw CreateResultModeError($"Typed error reconstruction for '{typeof(TError).GetSignature()}' is not implemented yet in '{nameof(ResponseHttpMode)}.{nameof(ResponseHttpMode.Result)}'.");
   }

   static Error CreateErrorFromProblemDetails(ResponseProblemDetails? problem, JsonSerializerOptions? jsonOptions)
   {
      if (problem is null)
         return Error.Critical("The error response body could not be deserialized.");

      object? payload = null;
      if (problem.Extensions.TryGetValue(nameof(Error.Payload), out var payloadObj))
         payload = payloadObj;

      Exception? exception = null;
      if (problem.Extensions.TryGetValue(nameof(Error.Exception), out var exceptionObj) && exceptionObj is JsonElement exceptionJson)
         try
         {
            exception = exceptionJson.Deserialize<Exception>(new JsonSerializerOptions(jsonOptions ?? new())
            {
               Converters = { new ExceptionConverter(true) }
            });
         }
         catch
         {
            // ignored for this phase
         }

      object? type = null;
      if (problem.Extensions.TryGetValue(nameof(Error.Type), out var typeObj))
         type = typeObj;

      var extensions = new ExtensionsDictionary(ErrorConstants.ErrorExtensionsReservedKeys);
      foreach (var extension in problem.Extensions)
         if (!string.Equals(extension.Key, nameof(Error.Payload), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension.Key, nameof(Error.Exception), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension.Key, nameof(Error.Type), StringComparison.OrdinalIgnoreCase))
            extensions[extension.Key] = extension.Value;

      return new Error(problem.Detail, type, payload, exception, extensions);
   }
}

public enum ResponseHttpMode { Response, Result }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member