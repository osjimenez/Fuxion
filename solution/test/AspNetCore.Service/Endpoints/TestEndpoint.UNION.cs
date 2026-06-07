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

      responseGroup.MapGet("error-empty", Response<Unit> () =>
      {
         return new Error();
      });

      responseGroup.MapGet("error-message", Response<Unit> () =>
      {
         return new Error("test");
      });

      responseGroup.MapGet("error-payload", Response<Unit> () =>
      {
         return new Error(payload: TestPayload.Default);
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
            return new Error(exception: ex);
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

      resultGroup.MapGet("error-empty", () =>
      {
         Response<Unit> response = new Error();
         return response.ToApiResult();
      });

      resultGroup.MapGet("error-message", () =>
      {
         Response<Unit> response = new Error("test");
         return response.ToApiResult();
      });

      resultGroup.MapGet("error-payload", () =>
      {
         Response<Unit> response = new Error(payload: TestPayload.Default);
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
            Response<Unit> response = new Error(exception: ex);
            return response.ToApiResult();
         }
      });
      #endregion

      //// ERROR
      //builder.MapGet("endpoint-test-message-error", () => Response.Get.ErrorMessage("Error message").ToApiResult());
      //builder.MapGet("endpoint-test-payload-error", () => Response.Get.ErrorPayload(new TestPayload
      //	{
      //		FirstName = "Test name",
      //		Age = 123
      //	}, "Error message")
      //	.ToApiResult());

      //builder.MapGet("endpoint-test-message-exception", () =>
      //{
      //	try
      //	{
      //		new Level1().Throw();
      //		return Response.Get.Success().ToApiResult();
      //	} catch (Exception ex)
      //	{
      //		return Response.Get.Exception(ex).ToApiResult();
      //	}
      //});

      //// BAD REQUEST
      //builder.MapGet("endpoint-test-message-bad-request", () => Response.Get.InvalidData("Error message").ToApiResult());
      //builder.MapGet("endpoint-test-payload-bad-request", () => Response.Get.InvalidData("Error message", new TestPayload
      //	{
      //		FirstName = "Test name",
      //		Age = 123
      //	})
      //	.ToApiResult());
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