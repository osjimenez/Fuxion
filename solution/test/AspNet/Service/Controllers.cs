namespace Test.AspNet.Service;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Fuxion.AspNet;
using Fuxion.Union;
using Test.Responses.Shared;

[RoutePrefix("response")]
public class ResponseController : ApiController
{
	[HttpGet, Route("unit")] public Response<Unit> Unit_() => Unit.Value;
	[HttpGet, Route("none")] public ResponseMaybe<Unit> None_() => None.Value;
	[HttpGet, Route("string")] public Response<string> String_() => "test";
	[HttpGet, Route("payload")] public Response<TestPayload> Payload() => TestPayload.Default;
	// A multi-word payload under bare application/json: proves a requested naming policy is never
	// stamped on an un-announced shape (see Fuxion.Union.ResponseWireMapper.TryMap).
	[HttpGet, Route("naming-payload")] public Response<TestNamingPayload> NamingPayload() => new TestNamingPayload("Ada", 36);
	[HttpGet, Route("error-message")] public Response<Unit> ErrorMessage() => Error.Custom("test");
	[HttpGet, Route("error-type")] public Response<Unit> ErrorType() => Error.NotImplemented();
	[HttpGet, Route("typed-error")] public Response<string, TestBusinessError> TypedError() => TestBusinessError.Default;
	[HttpGet, Route("typed-error-foreign")] public Response<string, TestForeignError> TypedErrorForeign() => new TestForeignError("quota");
	[HttpGet, Route("unset")] public Response<string> Unset() => default;
	// Proves the filter maps an async union action the same as a sync one: the framework awaits the
	// Task before OnActionExecuted runs, but ActionDescriptor.ReturnType still reports Task<Response<T>>.
	[HttpGet, Route("async-payload")]
	public async Task<Response<TestPayload>> AsyncPayload()
	{
		await Task.Yield();
		return TestPayload.Default;
	}
}

// Controller-level attribute: the scope cascade is global -> controller -> action.
[RoutePrefix("special")]
[ResponseOptions(SerializeFullResponses = true)]
public class SpecialController : ApiController
{
	[HttpGet, Route("payload")] public Response<TestPayload> Payload() => TestPayload.Default;
	[HttpGet, Route("payload-bare"), ResponseOptions(SerializeFullResponses = false)] public Response<TestPayload> PayloadBare() => TestPayload.Default;
}

[RoutePrefix("plain")]
public class PlainController : ApiController
{
	// Not a union type: must never be touched by the filter.
	[HttpGet, Route("list")] public string[] List() => ["a", "b"];

	// ReflectedHttpActionDescriptor.ReturnType is null for both of these; the filter must leave the
	// framework's own 204 (no content) alone rather than throw on a null declared return type.
	[HttpGet, Route("void")] public void Void_() { }
	[HttpGet, Route("task")] public Task Task_() => Task.CompletedTask;

	// A plain (non-Fuxion) POCO: whichever JSON formatter is in effect handles it, not the Fuxion one.
	[HttpGet, Route("poco")] public PlainPoco Poco() => new("x", 1);

	// A single-word property (PlainPoco.Name) never exercises naming: snake_case of "Name" is still "name",
	// so this needs a multi-word one to tell transcoding apart from an untouched (Newtonsoft) bind.
	[HttpPost, Route("two-words")] public object TwoWords([FromBody] PlainTwoWords poco) => new { firstName = poco?.FirstName };
}

[RoutePrefix("undefinable")]
public class UndefinableController : ApiController
{
	[HttpGet, Route("partial")] public TestPatchPayload Partial() => TestPatchPayload.PartiallyDefined;
	[HttpPost, Route("echo")] public object Echo([FromBody] TestPatchPayload payload) => new { nameDefined = payload.Name.IsDefined, ageDefined = payload.Age.IsDefined };
}

[RoutePrefix("naming")]
public class NamingController : ApiController
{
	[HttpPost, Route("echo")] public object Echo([FromBody] TestNamingPayload payload) => new { firstName = payload?.FirstName, age = payload?.Age };

	// A malformed body must still fail model binding (400), exactly like the framework always did with
	// Newtonsoft, now that the deserialization exception is reported through IFormatterLogger instead
	// of escaping as an unhandled 500.
	[HttpPost, Route("validate")]
	public IHttpActionResult Validate([FromBody] TestNamingPayload? payload)
		=> ModelState.IsValid && payload is not null ? Ok() : BadRequest(ModelState);

	// Dictionary keys must never be renamed by a naming policy, only object property names.
	[HttpPost, Route("dictionary")]
	public object Dictionary([FromBody] TestDictionaryPayload payload)
		=> new { keys = payload.Tags.Keys.OrderBy(k => k).ToArray(), name = payload.FirstName };
}

[RoutePrefix("binary")]
public class BinaryController : ApiController
{
	[HttpGet, Route("file")] public Response<FileContent> File_() => TestFile.Create();
	[HttpGet, Route("stream")] public Response<Stream> Stream_() => new MemoryStream(TestFile.Bytes, writable: false);
	[HttpGet, Route("bytes")] public Response<byte[]> Bytes() => TestFile.Bytes;
	[HttpGet, Route("none")] public ResponseMaybe<FileContent> None_() => None.Value;
	[HttpGet, Route("error")] public Response<FileContent> Error_() => Error.NotFound("missing");
}

// Exercises the deferred ToHttpActionResult extensions: the action does not touch the request itself,
// only the extension does (options resolution + Accept), proving the filter is not needed for this path.
[RoutePrefix("result")]
public class ResultController : ApiController
{
	[HttpGet, Route("payload")] public IHttpActionResult Payload() { Response<TestPayload> response = TestPayload.Default; return response.ToHttpActionResult(Request); }
	[HttpGet, Route("error")] public IHttpActionResult Error_() { Response<TestPayload> response = Error.NotFound("missing"); return response.ToHttpActionResult(Request); }
	[HttpGet, Route("explicit")] public IHttpActionResult Explicit() { Response<TestPayload> response = TestPayload.Default; return response.ToHttpActionResult(Request, new ResponseOptions { SerializeFullResponses = true }); }
}
