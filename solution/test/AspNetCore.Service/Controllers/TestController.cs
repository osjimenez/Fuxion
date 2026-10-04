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
	[HttpGet("unit")] public Response<Unit> UnitValue() => Unit.Value;
	[HttpGet("none")] public ResponseMaybe<Unit> NoneValue() => None.Value;
	[HttpGet("string")] public Response<string> String() => "test";
	[HttpGet("payload")] public Response<TestPayload> Payload() => TestPayload.Default;
	// A multi-word payload under bare application/json: proves a requested naming policy is never
	// stamped on an un-announced shape (see Fuxion.ResponseWireMapper.TryMap).
	[HttpGet("naming-payload")] public Response<TestNamingPayload> NamingPayload() => new TestNamingPayload("Ada", 36);
	[HttpGet("error-message")] public Response<Unit> ErrorMessage() => Error.Custom("test");
	[HttpGet("error-type")] public Response<Unit> ErrorType() => Error.NotImplemented();
	[HttpGet("error-payload")] public Response<Unit> ErrorPayload() => Error.Custom(payload: TestPayload.Default);
	[HttpGet("typed-error")] public Response<string, TestBusinessError> TypedError() => TestBusinessError.Default;
	[HttpGet("typed-error-foreign")] public Response<string, TestForeignError> TypedErrorForeign() => new TestForeignError("quota");
	[HttpGet("error-exception")]
	public Response<Unit> ErrorException()
	{
		try
		{
			ThrowingFixture.Throw();
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
	[HttpGet("unit")] public IActionResult UnitValue() => Unit.ActionResult;
	[HttpGet("none")] public IActionResult NoneValue() => None.ActionResult;
	[HttpGet("string")]
	public IActionResult String()
	{
		Response<string> response = "test";
		return response.ToActionResult();
	}
	[HttpGet("payload")]
	public IActionResult Payload()
	{
		Response<TestPayload> response = TestPayload.Default;
		return response.ToActionResult();
	}
	[HttpGet("error-message")]
	public IActionResult ErrorMessage()
	{
		Response<Unit> response = Error.Custom("test");
		return response.ToActionResult();
	}
	[HttpGet("error-type")]
	public IActionResult ErrorType()
	{
		Response<Unit> response = Error.NotImplemented();
		return response.ToActionResult();
	}
	[HttpGet("error-payload")]
	public IActionResult ErrorPayload()
	{
		Response<Unit> response = Error.Custom(payload: TestPayload.Default);
		return response.ToActionResult();
	}
	[HttpGet("error-exception")]
	public IActionResult ErrorException()
	{
		try
		{
			ThrowingFixture.Throw();
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
	[HttpGet("partial")]
	public TestPatchPayload Partial() => TestPatchPayload.PartiallyDefined;

	[Route("echo")]
	[HttpPost]
	public object Echo([FromBody] TestPatchPayload payload) => new
	{
		nameDefined = payload.Name.IsDefined,
		ageDefined = payload.Age.IsDefined
	};
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
	public Response<FileContent> GetFile() => TestFile.Create();
	[HttpGet("stream")]
	public Response<Stream> GetStream() => new MemoryStream(TestFile.Bytes, writable: false);
	[HttpGet("bytes")]
	public Response<byte[]> Bytes() => TestFile.Bytes;
	[HttpGet("none")]
	public ResponseMaybe<FileContent> GetNone() => None.Value;
	[HttpGet("error")]
	public Response<FileContent> GetError() => Error.NotFound("missing");
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