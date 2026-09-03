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
	[Route("error-message")]
	[HttpGet]
	public Response<Unit> ErrorMessage()
	{
		return Error.Custom("test");
	}
	[Route("error-type")]
	[HttpGet]
	public Response<Unit> ErrorType()
	{
		return Error.NotImplemented();
	}
	[Route("error-payload")]
	[HttpGet]
	public Response<Unit> ErrorPayload()
	{
		return Error.Custom(payload: TestPayload.Default);
	}
	[Route("typed-error")]
	[HttpGet]
	public Response<string, TestBusinessError> TypedError()
	{
		return TestBusinessError.Default;
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
			return Error.Custom(exception: ex);
		}
	}
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
		return Unit.ActionResult;
	}
	[Route("none")]
	[HttpGet]
	public IActionResult NoneValue()
	{
		return None.ActionResult;
	}
	[Route("string")]
	[HttpGet]
	public IActionResult String()
	{
		Response<string> response = "test";
		return response.ToActionResult();
	}
	[Route("payload")]
	[HttpGet]
	public IActionResult Payload()
	{
		Response<TestPayload> response = TestPayload.Default;
		return response.ToActionResult();
	}
	[Route("error-message")]
	[HttpGet]
	public IActionResult ErrorMessage()
	{
		Response<Unit> response = Error.Custom("test");
		return response.ToActionResult();
	}
	[Route("error-type")]
	[HttpGet]
	public IActionResult ErrorType()
	{
		Response<Unit> response = Error.NotImplemented();
		return response.ToActionResult();
	}
	[Route("error-payload")]
	[HttpGet]
	public IActionResult ErrorPayload()
	{
		Response<Unit> response = Error.Custom(payload: TestPayload.Default);
		return response.ToActionResult();
	}
	[Route("error-exception")]
	[HttpGet]
	public IActionResult ErrorException()
	{
		try
		{
			new Level1().Throw();
			Response<Unit> response = Unit.Value;
			return response.ToActionResult();
		}
		catch (Exception ex)
		{
			Response<Unit> response = Error.Custom(exception: ex);
			return response.ToActionResult();
		}
	}
}

[ApiController]
[Route("controller/undefinable")]
public class UndefinableTestController : ControllerBase
{
	// An undefined member must be absent from the payload, which is the JSON Merge Patch (RFC 7396)
	// convention, instead of being written with the marker object.
	[Route("partial")]
	[HttpGet]
	public TestPatchPayload Partial() => TestPatchPayload.PartiallyDefined;

	[Route("echo")]
	[HttpPost]
	public object Echo([FromBody] TestPatchPayload payload) => new
	{
		nameDefined = payload.Name.IsDefined,
		ageDefined = payload.Age.IsDefined
	};
}
file class Level1
{
	public void Throw() => new Level2().Throw();
}
file class Level2
{
	public void Throw() => throw new NotImplementedException("message");
}

[ApiController]
[Route("controller/naming")]
public class NamingTestController : ControllerBase
{
	[HttpPost("echo")]
	public object Echo([FromBody] TestNamingPayload payload) => new { firstName = payload.FirstName, age = payload.Age };
}