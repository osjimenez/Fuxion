using System;
using Fuxion;
using Fuxion.AspNetCore;
using Test.AspNetCore.Service.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Fuxion.Union;
using Microsoft.AspNetCore.Http;
using Test.AspNetCore.Service;

namespace Test.AspNetCore.Union.Service.Controllers;

[ApiController]
[Route("controller/response")]
public class ResponseTestController : ControllerBase
{
	// SUCCESS
	[Route("unit")]
	[HttpGet]
	public Response<Unit> UnitValue()
	{
		return Unit.Value;
	}
   [Route("none")]
   [HttpGet]
   public ResponseMaybe<Unit> NoneValue()
   {
      return None.Value;
   }
   [Route("string")]
   [HttpGet]
   public Response<string> String()
   {
      return "test";
   }
   [Route("payload")]
   [HttpGet]
   public Response<TestPayload> Payload()
   {
      return TestPayload.Default;
   }
   [Route("error-empty")]
   [HttpGet]
   public Response<Unit> ErrorEmpty()
   {
      return new Error();
   }
   [Route("error-message")]
   [HttpGet]
   public Response<Unit> ErrorMessage()
   {
      return new Error("test");
   }
   [Route("error-payload")]
   [HttpGet]
   public Response<Unit> ErrorPayload()
   {
      return new Error(payload: TestPayload.Default);
   }
   [Route("error-exception")]
   [HttpGet]
   public Response<Unit> ErrorException()
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
   }

   //[Route("test-message-success")]
   //[HttpGet]
   //public IActionResult MessageSuccess() => Fuxion.Response.Get.SuccessMessage("Success message").ToApiActionResult();

   //[Route("test-payload-success")]
   //[HttpGet]
   //public IActionResult PayloadSuccess() => Fuxion.Response.Get.SuccessPayload(new TestPayload
   //{
   //	FirstName = "Test name",
   //	Age = 123
   //}).ToApiActionResult();

   //// ERROR
   //[Route("test-message-error")]
   //[HttpGet]
   //public IActionResult MessageError() => Fuxion.Response.Get.ErrorMessage("Error message").ToApiActionResult();

   //[Route("test-payload-error")]
   //[HttpGet]
   //public IActionResult PayloadError() => Fuxion.Response.Get.ErrorPayload(new TestPayload
   //{
   //	FirstName = "Test name",
   //	Age = 123
   //}, "Error message").ToApiActionResult();

   //[Route("test-message-exception")]
   //[HttpGet]
   //public IActionResult MessageException()
   //{
   //	try
   //	{
   //		new Level1().Throw();
   //		return Fuxion.Response.Get.Success().ToApiActionResult();
   //	} catch (Exception ex)
   //	{
   //		return Fuxion.Response.Get.Exception(ex).ToApiActionResult();
   //	}
   //}

   //// BAD REQUEST
   //[Route("test-message-bad-request")]
   //[HttpGet]
   //public IActionResult MessageBadRequest() => Fuxion.Response.Get.InvalidData("Error message").ToApiActionResult();

   //[Route("test-payload-bad-request")]
   //[HttpGet]
   //public IActionResult PayloadBadRequest() => Fuxion.Response.Get.InvalidData("Error message", new TestPayload
   //{
   //	FirstName = "Test name",
   //	Age = 123
   //}).ToApiActionResult();
}

[ApiController]
[Route("controller/result")]
public class ResultTestController : ControllerBase
{
   // SUCCESS
   [Route("unit")]
   [HttpGet]
   public IActionResult UnitValue()
   {
      Response<Unit> response = Unit.Value;
      return response.ToApiActionResult();
   }
   [Route("none")]
   [HttpGet]
   public IActionResult NoneValue()
   {
      ResponseMaybe<Unit> response = None.Value;
      return response.ToApiActionResult();
   }
   [Route("string")]
   [HttpGet]
   public IActionResult String()
   {
      Response<string> response = "test";
      return response.ToApiActionResult();
   }
   [Route("payload")]
   [HttpGet]
   public IActionResult Payload()
   {
      Response<TestPayload> response = TestPayload.Default;
      return response.ToApiActionResult();
   }
   [Route("error-empty")]
   [HttpGet]
   public IActionResult ErrorEmpty()
   {
      Response<Unit> response = new Error();
      return response.ToApiActionResult();
   }
   [Route("error-message")]
   [HttpGet]
   public IActionResult ErrorMessage()
   {
      Response<Unit> response = new Error("test");
      return response.ToApiActionResult();
   }
   [Route("error-payload")]
   [HttpGet]
   public IActionResult ErrorPayload()
   {
      Response<Unit> response = new Error(payload: TestPayload.Default);
      return response.ToApiActionResult();
   }
   [Route("error-exception")]
   [HttpGet]
   public IActionResult ErrorException()
   {
      try
      {
         new Level1().Throw();
         Response<Unit> response = Unit.Value;
         return response.ToApiActionResult();
      }
      catch (Exception ex)
      {
         Response<Unit> response = new Error(exception: ex);
         return response.ToApiActionResult();
      }
   }

   //[Route("test-message-success")]
   //[HttpGet]
   //public IActionResult MessageSuccess() => Fuxion.Response.Get.SuccessMessage("Success message").ToApiActionResult();

   //[Route("test-payload-success")]
   //[HttpGet]
   //public IActionResult PayloadSuccess() => Fuxion.Response.Get.SuccessPayload(new TestPayload
   //{
   //	FirstName = "Test name",
   //	Age = 123
   //}).ToApiActionResult();

   //// ERROR
   //[Route("test-message-error")]
   //[HttpGet]
   //public IActionResult MessageError() => Fuxion.Response.Get.ErrorMessage("Error message").ToApiActionResult();

   //[Route("test-payload-error")]
   //[HttpGet]
   //public IActionResult PayloadError() => Fuxion.Response.Get.ErrorPayload(new TestPayload
   //{
   //	FirstName = "Test name",
   //	Age = 123
   //}, "Error message").ToApiActionResult();

   //[Route("test-message-exception")]
   //[HttpGet]
   //public IActionResult MessageException()
   //{
   //	try
   //	{
   //		new Level1().Throw();
   //		return Fuxion.Response.Get.Success().ToApiActionResult();
   //	} catch (Exception ex)
   //	{
   //		return Fuxion.Response.Get.Exception(ex).ToApiActionResult();
   //	}
   //}

   //// BAD REQUEST
   //[Route("test-message-bad-request")]
   //[HttpGet]
   //public IActionResult MessageBadRequest() => Fuxion.Response.Get.InvalidData("Error message").ToApiActionResult();

   //[Route("test-payload-bad-request")]
   //[HttpGet]
   //public IActionResult PayloadBadRequest() => Fuxion.Response.Get.InvalidData("Error message", new TestPayload
   //{
   //	FirstName = "Test name",
   //	Age = 123
   //}).ToApiActionResult();
}
file class Level1
{
   public void Throw() => new Level2().Throw();
}
file class Level2
{
   public void Throw() => throw new NotImplementedException("Not implemented");
}