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
	public IActionResult EmptySuccess() => ResponseExt.Get.Success().ToApiActionResult();

	[Route("test-message-success")]
	[HttpGet]
	public IActionResult MessageSuccess() => ResponseExt.Get.SuccessMessage("Success message").ToApiActionResult();

	[Route("test-payload-success")]
	[HttpGet]
	public IActionResult PayloadSuccess() => ResponseExt.Get.SuccessPayload(new TestPayload
	{
		FirstName = "Test name",
		Age = 123
	}).ToApiActionResult();

	// ERROR
	[Route("test-message-error")]
	[HttpGet]
	public IActionResult MessageError() => ResponseExt.Get.ErrorMessage("Error message").ToApiActionResult();

	[Route("test-payload-error")]
	[HttpGet]
	public IActionResult PayloadError() => ResponseExt.Get.ErrorPayload(new TestPayload
	{
		FirstName = "Test name",
		Age = 123
	}, "Error message").ToApiActionResult();

	[Route("test-message-exception")]
	[HttpGet]
	public IActionResult MessageException()
	{
		try
		{
			new Level1().Throw();
			return ResponseExt.Get.Success().ToApiActionResult();
		} catch (Exception ex)
		{
			return ResponseExt.Get.Exception(ex).ToApiActionResult();
		}
	}

	// BAD REQUEST
	[Route("test-message-bad-request")]
	[HttpGet]
	public IActionResult MessageBadRequest() => ResponseExt.Get.InvalidData("Error message").ToApiActionResult();

	[Route("test-payload-bad-request")]
	[HttpGet]
	public IActionResult PayloadBadRequest() => ResponseExt.Get.InvalidData("Error message", new TestPayload
	{
		FirstName = "Test name",
		Age = 123
	}).ToApiActionResult();
}