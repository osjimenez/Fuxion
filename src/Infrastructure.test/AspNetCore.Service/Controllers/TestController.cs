using Fuxion.AspNetCore.Service.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Fuxion.AspNetCore.Service.Controllers;

[ApiController]
[Route("controller")]
public class TestController : ControllerBase
{
	// SUCCESS
	[Route("test-empty-success")]
	[HttpGet]
	public IActionResult EmptySuccess() => Fuxion.Response.Success().ToApiActionResult();

	[Route("test-message-success")]
	[HttpGet]
	public IActionResult MessageSuccess() => Fuxion.Response.SuccessMessage("Success message").ToApiActionResult();

	[Route("test-payload-success")]
	[HttpGet]
	public IActionResult PayloadSuccess() => Fuxion.Response.SuccessPayload(new TestPayload
	{
		FirstName = "Test name",
		Age = 123
	}).ToApiActionResult();

	// ERROR
	[Route("test-message-error")]
	[HttpGet]
	public IActionResult MessageError() => Fuxion.Response.ErrorMessage("Error message").ToApiActionResult();

	[Route("test-payload-error")]
	[HttpGet]
	public IActionResult PayloadError() => Fuxion.Response.ErrorPayload(new TestPayload
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
			return Fuxion.Response.Success().ToApiActionResult();
		} catch (Exception ex)
		{
			return Fuxion.Response.Exception(ex).ToApiActionResult();
		}
	}

	// BAD REQUEST
	[Route("test-message-bad-request")]
	[HttpGet]
	public IActionResult MessageBadRequest() => Fuxion.Response.InvalidData("Error message").ToApiActionResult();

	[Route("test-payload-bad-request")]
	[HttpGet]
	public IActionResult PayloadBadRequest() => Fuxion.Response.InvalidData("Error message", new TestPayload
	{
		FirstName = "Test name",
		Age = 123
	}).ToApiActionResult();
}