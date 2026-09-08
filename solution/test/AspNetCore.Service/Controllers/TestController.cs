using System;
using System.IO;
using System.Linq;
using Fuxion;
using Fuxion.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Test.Responses.Shared.Fixtures;

namespace Test.AspNetCore.Service.Controllers;

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
	// A multi-word payload under bare application/json: proves a requested naming policy is never
	// stamped on an un-announced shape (see Fuxion.Union.ResponseWireMapper.TryMap).
	[Route("naming-payload")]
	[HttpGet]
	public Response<TestNamingPayload> NamingPayload()
	{
		return new TestNamingPayload("Ada", 36);
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
	[Route("typed-error-foreign")]
	[HttpGet]
	public Response<string, TestForeignError> TypedErrorForeign()
	{
		return new TestForeignError("quota");
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
	// Explicit options passed to ToActionResult() win over both the scope's cascade and the request's Accept header.
	[HttpGet("explicit-envelope")]
	public IActionResult ExplicitEnvelope() => ((Response<TestPayload>)TestPayload.Default).ToActionResult(new ResponseOptions { SerializeFullResponses = true });
	[HttpGet("explicit-plain")]
	public IActionResult ExplicitPlain() => ((Response<TestPayload>)TestPayload.Default).ToActionResult(new ResponseOptions { SerializeFullResponses = false });
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

	// A declared Consumes media type: proves the naming parameter is still honoured (or ignored, if the
	// controller is skipped by RequestNamingEndpoint.Apply) by MVC's own per-request formatter path,
	// independent of the minimal-API RequestDelegate wrapper.
	[HttpPost("consumes")]
	[Consumes(typeof(TestNamingPayload), "application/json")]
	public object Consumes([FromBody] TestNamingPayload payload) => new { firstName = payload.FirstName, age = payload.Age };

	[HttpPost("dictionary")]
	public object Dictionary([FromBody] TestDictionaryPayload payload) => new { keys = payload.Tags.Keys.OrderBy(k => k).ToArray(), name = payload.FirstName };
}

[ApiController]
[Route("controller/binary")]
public class BinaryTestController : ControllerBase
{
	[HttpGet("file")]
	public Response<FileContent> File_() => TestFile.Create();
	[HttpGet("stream")]
	public Response<Stream> Stream_() => new MemoryStream(TestFile.Bytes, writable: false);
	[HttpGet("bytes")]
	public Response<byte[]> Bytes() => TestFile.Bytes;
	[HttpGet("none")]
	public ResponseMaybe<FileContent> None_() => None.Value;
	[HttpGet("error")]
	public Response<FileContent> Error_() => Error.NotFound("missing");
	[HttpGet("chunked")]
	public Response<Stream> Chunked() => new NonSeekableStream(TestFile.Bytes);
}

// Proves the ResponseOptionsAttribute cascade (global -> controller -> action) still works after the
// bool? -> bool+has-value-flag rework required by CS0655 (Nullable<T> is not a legal attribute parameter type).
[ApiController]
[Route("attribute-test")]
[ResponseOptions(SerializeFullResponses = true)]
public class AttributeTestController : ControllerBase
{
	[HttpGet("payload")]
	public Response<TestPayload> Payload() => TestPayload.Default;

	[HttpGet("payload-bare"), ResponseOptions(SerializeFullResponses = false)]
	public Response<TestPayload> PayloadBare() => TestPayload.Default;
}