namespace Fuxion.AspNetCore;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Fuxion.Text.Json.Serialization;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseMiddlewareExtensions
{
   public static MvcOptions UseResponses(this MvcOptions options)
   {
      options.Filters.Add<ResponseActionFilter>();
      return options;
   }

   extension(IResponse me)
   {
      public IResult ToApiResult()
      {
         if (ResponseHttpMapper.TryMap(me, ResponseHttpMode.Result, out var mapping))
            return mapping.ToResult();

         throw new NotSupportedException($"The union response value of type '{me.Value?.GetType().FullName ?? "null"}' is not supported yet in '{nameof(ResponseHttpMode)}.{nameof(ResponseHttpMode.Result)}'.");
      }

      public IActionResult ToApiActionResult()
      {
         if (ResponseHttpMapper.TryMap(me, ResponseHttpMode.Result, out var mapping))
            return mapping.ToActionResult();

         throw new NotSupportedException($"The union response value of type '{me.Value?.GetType().FullName ?? "null"}' is not supported yet in '{nameof(ResponseHttpMode)}.{nameof(ResponseHttpMode.Result)}'.");
      }
   }

   public static TBuilder UseResponses<TBuilder>(this TBuilder builder)
      where TBuilder : IEndpointConventionBuilder
   {
      builder.AddEndpointFilterFactory(ResponseEndpointFilterFactory.Create);
      return builder;
   }
}

static class ResponseEndpointFilterFactory
{
   public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
   {
      if (!ResponseHttpMapper.IsSupportedDeclaredResponseType(context.MethodInfo.ReturnType))
         return next;

      return async invocationContext =>
      {
         var value = await next(invocationContext);
         return ResponseHttpMapper.TryMap(value, ResponseHttpMode.Response, out var mapping)
            ? mapping.ToResult()
            : value;
      };
   }
}

sealed class ResponseActionFilter : IActionFilter
{
   public void OnActionExecuting(ActionExecutingContext context) { }

   public void OnActionExecuted(ActionExecutedContext context)
   {
      if (context.ActionDescriptor is not ControllerActionDescriptor action)
         return;

      if (!ResponseHttpMapper.IsSupportedDeclaredResponseType(action.MethodInfo.ReturnType))
         return;

      if (context.Result is not ObjectResult result)
         return;

      if (!ResponseHttpMapper.TryMap(result.Value, ResponseHttpMode.Response, out var mapping))
         return;

      context.Result = mapping.ToActionResult();
   }
}

static class ResponseHttpMapper
{
   public static bool IsSupportedDeclaredResponseType(Type type)
      => IsResponseReturnType(UnwrapTypeIfATaskWrapIt(type));

   public static bool TryMap(object? value, ResponseHttpMode mode, out ResponseHttpMapping mapping)
      => mode switch
      {
         ResponseHttpMode.Response => TryMapFullResponse(value, out mapping),
         ResponseHttpMode.Result => TryMapResult(value, out mapping),
         _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
      };

   static bool TryMapFullResponse(object? value, out ResponseHttpMapping mapping)
   {
      if (value is IResponse response)
      {
         if (response.IsError)
         {
            if(response.Value is Error error && error.Type is HttpStatusCode status)
            {
               mapping = new ResponseHttpMapping((int)status, value, false);
               return true;
            }
            mapping = new ResponseHttpMapping(StatusCodes.Status500InternalServerError, value, false);
            return true;
         }

         if (response.Value is Unit)
         {
            mapping = new ResponseHttpMapping(StatusCodes.Status200OK, value, false);
            return true;
         }
         if (response.Value is None)
         {
            mapping = new ResponseHttpMapping(StatusCodes.Status204NoContent, value, true);
            return true;
         }
      }

      mapping = default;
      return false;
   }

   static bool TryMapResult(object? value, out ResponseHttpMapping mapping)
   {
      if (value is IResponse response)
      {
         if (response.IsError)
         {
            mapping = MapErrorResult(response.Value);
            return true;
         }

         if (response.Value is Unit)
         {
            mapping = new ResponseHttpMapping(StatusCodes.Status200OK, value, false);
            return true;
         }
         if (response.Value is None)
         {
            mapping = new ResponseHttpMapping(StatusCodes.Status204NoContent, null, true);
            return true;
         }

         if (response.Value is not null)
         {
            mapping = new ResponseHttpMapping(StatusCodes.Status200OK, response.Value, false);
            return true;
         }
      }

      mapping = default;
      return false;
   }

   static ResponseHttpMapping MapErrorResult(object? value)
   {
      if (value is Error error)
      {
         var extensions = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
         if (error.Payload is not null)
            extensions[nameof(Error.Payload)] = error.Payload;
         if (error.Exception is not null)
            extensions[nameof(Error.Exception)] = JsonSerializer.SerializeToElement(error.Exception, options: new()
            {
               Converters = { new ExceptionConverter() }
            });
         foreach (var extension in error.Extensions)
            extensions[extension.Key] = extension.Value;

         var problem = new ProblemDetails
         {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Detail = error.Message
         };
         if (error.Type is not null) // PEND When have to put Type in Extension ?
            problem.Extensions["Error" + nameof(Error.Type)] = error.Type;

         foreach (var extension in extensions)
            problem.Extensions[extension.Key] = extension.Value;

         if (error.Type is HttpStatusCode status)
         {
            problem.Status = (int)status;
            return new ResponseHttpMapping((int)status, problem, false);
         }
         return new ResponseHttpMapping(StatusCodes.Status500InternalServerError, problem, false);
      }

      return new ResponseHttpMapping(StatusCodes.Status500InternalServerError, new ProblemDetails
      {
         Status = StatusCodes.Status500InternalServerError,
         Title = "Internal server error",
         Detail = "The response is error but it does not contain a supported error payload."
      }, false);
   }

   static Type UnwrapTypeIfATaskWrapIt(Type type)
   {
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
         return type.GetGenericArguments()[0];

      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
         return type.GetGenericArguments()[0];

      return type;
   }

   static bool IsResponseReturnType(Type type)
   {
      if (!type.IsGenericType)
         return false;

      var definition = type.GetGenericTypeDefinition();
      return definition == typeof(Response<>)
         || definition == typeof(Response<,>)
         || definition == typeof(ResponseMaybe<>)
         || definition == typeof(ResponseMaybe<,>);
   }
}

readonly struct ResponseHttpMapping(int statusCode, object? body, bool suppressBody)
{
   public int StatusCode { get; } = statusCode;
   public object? Body { get; } = body;
   public bool SuppressBody { get; } = suppressBody;

   public IResult ToResult()
      => SuppressBody
         ? Results.StatusCode(StatusCode)
         : Results.Json(Body, statusCode: StatusCode);

   public IActionResult ToActionResult()
      => SuppressBody
         ? new StatusCodeResult(StatusCode)
         : new JsonResult(Body) { StatusCode = StatusCode };
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member