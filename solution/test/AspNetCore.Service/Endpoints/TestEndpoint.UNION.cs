using System;
using System.Net;
using System.Text.Json;
//using Fuxion;
using Fuxion.Union;
using Fuxion.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Test.AspNetCore.Service;

namespace Test.AspNetCore.Union.Service.Endpoints;

public class TestEndpoint : IEndpoint
{
   public void MapEndpoint(IEndpointRouteBuilder builder)
   {
      var minimalGroup = builder.MapGroup("minimal");

      #region FULL RESPONSE

      var responseGroup = minimalGroup.MapGroup("response");

      // SUCCESS
      responseGroup.MapGet("unit", Response<Unit> () =>
      {
         return Unit.Value;
      });

      responseGroup.MapGet("none", ResponseMaybe<Unit> () =>
      {
         return None.Value;
      });

      responseGroup.MapGet("string", Response<string> () =>
      {
         return "test";
      });

      responseGroup.MapGet("payload", Response<TestPayload> () =>
      {
         return TestPayload.Default;
      });

      responseGroup.MapGet("error-message", Response<Unit> () =>
      {
         return Error.Custom("test");
      });

      responseGroup.MapGet("error-type", Response<Unit> () =>
      {
         return Error.NotImplemented();
      });

      responseGroup.MapGet("error-payload", Response<Unit> () =>
      {
         return Error.Custom(payload: TestPayload.Default);
      });

      responseGroup.MapGet("error-exception", Response<Unit> () =>
      {
         try
         {
            new Level1().Throw();
            return Unit.Value;
         }
         catch (Exception ex)
         {
            return Error.Custom(exception: ex);
         }
      });

      #endregion

      #region IResult

      var resultGroup = minimalGroup.MapGroup("result");

      // SUCCESS
      resultGroup.MapGet("unit", () =>
      {
         Response<Unit> response = Unit.Value;
         return response.ToApiResult();
      });

      resultGroup.MapGet("none", () =>
      {
         ResponseMaybe<Unit> response = None.Value;
         return response.ToApiResult();
      });

      resultGroup.MapGet("string", () =>
      {
         Response<string> response = "test";
         return response.ToApiResult();
      });

      resultGroup.MapGet("payload", () =>
      {
         Response<TestPayload> response = TestPayload.Default;
         return response.ToApiResult();
      });

      resultGroup.MapGet("error-message", () =>
      {
         Response<Unit> response = Error.Custom("test");
         return response.ToApiResult();
      });

      resultGroup.MapGet("error-type", () =>
      {
         Response<Unit> response = Error.NotImplemented();
         return response.ToApiResult();
      });

      resultGroup.MapGet("error-payload", () =>
      {
         Response<Unit> response = Error.Custom(payload: TestPayload.Default);
         return response.ToApiResult();
      });

      resultGroup.MapGet("error-exception", () =>
      {
         try
         {
            new Level1().Throw();
            Response<Unit> response = Unit.Value;
            return response.ToApiResult();
         }
         catch (Exception ex)
         {
            Response<Unit> response = Error.Custom(exception: ex);
            return response.ToApiResult();
         }
      });
      #endregion
   }
}
file class Level1
{
   public void Throw() => new Level2().Throw();
}
file class Level2
{
   public void Throw() => throw new NotImplementedException("Not implemented");
}