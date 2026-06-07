using System;
using Fuxion;
using Fuxion.AspNetCore;
using Test.AspNetCore.Service.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Test.AspNetCore.Service.Controllers;

[ApiController]
[Route("controller")]
public class TestController : ControllerBase
{
	// SUCCESS
	[Route("test-empty-success")]
	[HttpGet]
	public IActionResult EmptySuccess() => Fuxion.Response.Get.Success().ToApiActionResult();

	[Route("test-message-success")]
	[HttpGet]
	public IActionResult MessageSuccess() => Fuxion.Response.Get.SuccessMessage("Success message").ToApiActionResult();

	[Route("test-payload-success")]
	[HttpGet]
	public IActionResult PayloadSuccess() => Fuxion.Response.Get.SuccessPayload(TestPayload.Default).ToApiActionResult();

	// ERROR
	[Route("test-message-error")]
	[HttpGet]
	public IActionResult MessageError() => Fuxion.Response.Get.ErrorMessage("Error message").ToApiActionResult();

	[Route("test-payload-error")]
	[HttpGet]
	public IActionResult PayloadError() => Fuxion.Response.Get.ErrorPayload(TestPayload.Default, "Error message").ToApiActionResult();

	[Route("test-message-exception")]
	[HttpGet]
	public IActionResult MessageException()
	{
		try
		{
			new Level1().Throw();
			return Fuxion.Response.Get.Success().ToApiActionResult();
		} catch (Exception ex)
		{
			return Fuxion.Response.Get.Exception(ex).ToApiActionResult();
		}
	}

	// BAD REQUEST
	[Route("test-message-bad-request")]
	[HttpGet]
	public IActionResult MessageBadRequest() => Fuxion.Response.Get.InvalidData("Error message").ToApiActionResult();

	[Route("test-payload-bad-request")]
	[HttpGet]
	public IActionResult PayloadBadRequest() => Fuxion.Response.Get.InvalidData("Error message", TestPayload.Default).ToApiActionResult();
}
file class Level1
{
   public void Throw() => new Level2().Throw();
}
file class Level2
{
   public void Throw() => throw new NotImplementedException("Not implemented");
}