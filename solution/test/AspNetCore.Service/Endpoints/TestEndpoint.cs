using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Test.Responses.Shared.Fixtures;

namespace Test.AspNetCore.Service.Endpoints;

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

		// Extensions on a bare payload: they are dropped on the wire and the adapter logs a warning.
		responseGroup.MapGet("payload-extended", Response<TestPayload> () => new Response<TestPayload>(TestPayload.Default).WithExtension("trace", "abc"));

		// Per-endpoint override: forces the full envelope on this single route, leaving the rest of the group untouched.
		responseGroup.MapGet("payload-enveloped", Response<TestPayload> () => TestPayload.Default).UseResponses(meta => meta.SerializeFullResponses = true);

		// A multi-word payload under bare application/json: proves a requested naming policy is never
		// stamped on an un-announced shape (see Fuxion.ResponseWireMapper.TryMap).
		responseGroup.MapGet("naming-payload", Response<TestNamingPayload> () =>
		{
			return new TestNamingPayload("Ada", 36);
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
				ThrowingFixture.Throw();
				return Unit.Value;
			}
			catch (Exception ex)
			{
				return Error.Custom(exception: ex);
			}
		});

		responseGroup.MapGet("typed-error", Response<string, TestBusinessError> () =>
		{
			return TestBusinessError.Default;
		});

		responseGroup.MapGet("typed-error-foreign", Response<string, TestForeignError> () => new TestForeignError("quota"));

		responseGroup.MapGet("unset", Response<string> () => default);

		#endregion

		#region IResult

		var resultGroup = minimalGroup.MapGroup("result");

		// A subgroup with its own options reached through ToResult(): the deferred result resolves the scope when it
		// executes. The union-returning counterpart is the "sub" group in Program.cs.
		var specialGroup = minimalGroup.MapGroup("special").UseResponses(meta => meta.SerializeFullResponses = true);
		specialGroup.MapGet("payload", IResult () =>
		{
			Response<TestPayload> response = TestPayload.Default;
			return response.ToResult();
		});

		// Reached through two UseResponses() call sites (the root group and this one): proves the naming
		// wrapper is idempotent instead of double-wrapping the RequestDelegate.
		specialGroup.MapPost("naming-echo", (TestNamingPayload payload) => new { firstName = payload.FirstName, age = payload.Age });

		// SUCCESS
		resultGroup.MapGet("unit", () => Unit.Result);

		resultGroup.MapGet("none", () => None.Result);

		resultGroup.MapGet("string", () =>
		{
			Response<string> response = "test";
			return response.ToResult();
		});

		resultGroup.MapGet("payload", () =>
		{
			Response<TestPayload> response = TestPayload.Default;
			return response.ToResult();
		});

		resultGroup.MapGet("error-message", () =>
		{
			Response<Unit> response = Error.Custom("test");
			return response.ToResult();
		});

		resultGroup.MapGet("error-type", () =>
		{
			Response<Unit> response = Error.NotImplemented();
			return response.ToResult();
		});

		resultGroup.MapGet("error-payload", () =>
		{
			Response<Unit> response = Error.Custom(payload: TestPayload.Default);
			return response.ToResult();
		});

		resultGroup.MapGet("error-exception", () =>
		{
			try
			{
				ThrowingFixture.Throw();
				Response<Unit> response = Unit.Value;
				return response.ToResult();
			}
			catch (Exception ex)
			{
				Response<Unit> response = Error.Custom(exception: ex);
				return response.ToResult();
			}
		});
		#endregion

		// Bare Error and None returns are mapped like the union they imply (README, syntax demos 5.x); a bare value of
		// any other type is left to the framework.
		resultGroup.MapGet("bare-error", () => Error.Custom("test"));
		resultGroup.MapGet("bare-none", () => None.Value);
		resultGroup.MapGet("bare-value", () => 123);

		// Explicit options passed to ToResult() win over both the scope's cascade and the request's Accept header.
		resultGroup.MapGet("explicit-envelope", IResult () => Do().ToResult(new ResponseOptions { SerializeFullResponses = true }));
		resultGroup.MapGet("explicit-plain", IResult () => Do().ToResult(new ResponseOptions { SerializeFullResponses = false }));

		#region PLAIN

		// Neighbours with no union return type: UseResponses must leave them exactly as the framework would.
		var plainGroup = minimalGroup.MapGroup("plain");
		plainGroup.MapGet("list", () => new[] { "a", "b" });
		plainGroup.MapGet("ok", IResult () => Results.Ok(new { name = "plain" }));

		#endregion

		#region FORMS

		// Deterministic (no Random): every union form, sync or wrapped in Task/ValueTask.
		var formsGroup = minimalGroup.MapGroup("forms");
		formsGroup.MapGet("response", Response<int> () => 123);
		formsGroup.MapGet("response-task", async Task<Response<int>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("response-valuetask", async ValueTask<Response<int>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("typed", Response<int, string> () => 123);
		formsGroup.MapGet("typed-task", async Task<Response<int, string>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("typed-valuetask", async ValueTask<Response<int, string>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("maybe", ResponseMaybe<int> () => 123);
		formsGroup.MapGet("maybe-task", async Task<ResponseMaybe<int>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("maybe-valuetask", async ValueTask<ResponseMaybe<int>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("maybe-typed", ResponseMaybe<int, string> () => 123);
		formsGroup.MapGet("maybe-typed-task", async Task<ResponseMaybe<int, string>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("maybe-typed-valuetask", async ValueTask<ResponseMaybe<int, string>> () =>
		{
			await Task.Yield();
			return 123;
		});
		formsGroup.MapGet("maybe-typed-none-valuetask", async ValueTask<ResponseMaybe<int, string>> () =>
		{
			await Task.Yield();
			return None.Value;
		});
		formsGroup.MapGet("typed-error-task", async Task<Response<int, string>> () =>
		{
			await Task.Yield();
			return "business";
		});

		#endregion

		#region NAMING

		// Echoes what the binder understood, so a test can prove the request naming parameter was honoured.
		var namingGroup = minimalGroup.MapGroup("naming");
		namingGroup.MapPost("echo", (TestNamingPayload payload) => new { firstName = payload.FirstName, age = payload.Age });
		namingGroup.MapPost("dictionary", (TestDictionaryPayload payload) => new { keys = payload.Tags.Keys.OrderBy(k => k).ToArray(), name = payload.FirstName });
		namingGroup.MapPost("raw", async (HttpContext ctx) =>
		{
			using var r = new StreamReader(ctx.Request.Body);
			var text = await r.ReadToEndAsync();
			return new { hasSnake = text.Contains("first_name") };
		});

		#endregion

		#region BINARY

		// A binary payload is a bare file body: its own media type, Content-Disposition, ranges. Never the envelope.
		var binaryGroup = minimalGroup.MapGroup("binary");
		binaryGroup.MapGet("file", Response<IOContent> () => TestFile.Create());
		binaryGroup.MapGet("stream", Response<Stream> () => new MemoryStream(TestFile.Bytes, writable: false));
		binaryGroup.MapGet("bytes", Response<byte[]> () => TestFile.Bytes);
		binaryGroup.MapGet("none", ResponseMaybe<IOContent> () => None.Value);
		binaryGroup.MapGet("error", Response<IOContent> () => Error.NotFound("missing"));
		binaryGroup.MapGet("chunked", Response<Stream> () => new NonSeekableStream(TestFile.Bytes));
		// A non-seekable stream whose length is known, and a remote-like source that opens ranges by itself.
		binaryGroup.MapGet("sized-stream", Response<IOContent> () => new IOContent(new NonSeekableStream(TestFile.Bytes)) { Length = TestFile.Bytes.Length });
		binaryGroup.MapGet("range-source", Response<IOContent> () => RangeSource.Content());

		#endregion

		#region UNDEFINABLE

		// An undefined member must be absent from the payload, which is the JSON Merge Patch (RFC 7396)
		// convention, instead of being written with the marker object.
		var undefinableGroup = minimalGroup.MapGroup("undefinable");

		undefinableGroup.MapGet("partial", () => TestPatchPayload.PartiallyDefined);

		undefinableGroup.MapPost("echo", (TestPatchPayload payload) => new
		{
			nameDefined = payload.Name.IsDefined,
			ageDefined = payload.Age.IsDefined
		});

		#endregion
	}
	private Response<int> Do() => 123;
}
