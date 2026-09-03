using System.Net;
using System.Text.Json;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class ResponseWireMapperTest(ITestOutputHelper output) : BaseTest<ResponseWireMapperTest>(output)
{
	public record BusinessError(string Code);

	static ResponseOptions Options(bool full = false, bool problem = true, bool strict = false)
		=> new() { SerializeFullResponses = full, SerializeErrorAsProblemDetails = problem, StrictNone = strict };

	static ResponseWireMapping Map(object? value, ResponseOptions options)
	{
		Assert.True(ResponseWireMapper.TryMap(value, options, out var mapping), "The value must be mapped");
		return mapping;
	}

	[Theory(DisplayName = "Unit is a 200 with an empty object body, or the envelope when requested")]
	[InlineData(false, ResponseWireShape.Unit)]
	[InlineData(true, ResponseWireShape.Envelope)]
	public void Unit_(bool full, ResponseWireShape shape)
	{
		Response<Unit> response = Unit.Value;
		var mapping = Map(response, Options(full: full));

		Assert.Equal(200, mapping.StatusCode);
		Assert.Equal(shape, mapping.Shape);
		if (shape == ResponseWireShape.Envelope) Assert.Equal(response, (Response<Unit>)mapping.Value!);
	}

	[Theory(DisplayName = "None is a body-less 204 unless the envelope is on and StrictNone is off")]
	[InlineData(false, false, 204, ResponseWireShape.NoContent)]
	[InlineData(false, true, 204, ResponseWireShape.NoContent)]
	[InlineData(true, true, 204, ResponseWireShape.NoContent)]
	[InlineData(true, false, 200, ResponseWireShape.Envelope)]
	public void None_(bool full, bool strict, int status, ResponseWireShape shape)
	{
		ResponseMaybe<Unit> response = None.Value;
		var mapping = Map(response, Options(full: full, strict: strict));

		Assert.Equal(status, mapping.StatusCode);
		Assert.Equal(shape, mapping.Shape);
		Assert.Equal(shape != ResponseWireShape.NoContent, mapping.HasBody);
	}

	[Theory(DisplayName = "A payload is the bare value or the envelope")]
	[InlineData(false, ResponseWireShape.Payload)]
	[InlineData(true, ResponseWireShape.Envelope)]
	public void Payload_(bool full, ResponseWireShape shape)
	{
		Response<string> response = "test";
		var mapping = Map(response, Options(full: full));

		Assert.Equal(200, mapping.StatusCode);
		Assert.Equal(shape, mapping.Shape);
		if (shape == ResponseWireShape.Payload) Assert.Equal("test", mapping.Value);
		else Assert.Equal(response, (Response<string>)mapping.Value!);
	}

	[Theory(DisplayName = "A native error follows the two flags and takes its status from Error.Type")]
	[InlineData(false, true, ResponseWireShape.ProblemError)]
	[InlineData(true, true, ResponseWireShape.ProblemError)]  // problem wins over the envelope
	[InlineData(true, false, ResponseWireShape.Envelope)]
	[InlineData(false, false, ResponseWireShape.NativeError)]
	public void NativeError_(bool full, bool problem, ResponseWireShape shape)
	{
		Response<string> response = Error.NotFound("missing");
		var mapping = Map(response, Options(full: full, problem: problem));

		Assert.Equal(404, mapping.StatusCode);
		Assert.Equal(shape, mapping.Shape);
		if (shape != ResponseWireShape.Envelope)
			Assert.Equal("missing", ((Error)mapping.Value!).Message);
	}

	[Fact(DisplayName = "An error without an HTTP status type falls back to 500")]
	public void ErrorWithoutStatus_Is500()
	{
		Response<string> response = Error.Custom("boom");
		Assert.Equal(500, Map(response, Options()).StatusCode);
	}

	[Theory(DisplayName = "A typed business error travels as problem+json (wrapped), envelope, or bare value")]
	[InlineData(false, true, ResponseWireShape.ProblemError)]
	[InlineData(true, true, ResponseWireShape.ProblemError)]
	[InlineData(true, false, ResponseWireShape.Envelope)]
	[InlineData(false, false, ResponseWireShape.RawError)]
	public void TypedError_(bool full, bool problem, ResponseWireShape shape)
	{
		Response<string, BusinessError> response = new BusinessError("stock");
		var mapping = Map(response, Options(full: full, problem: problem));

		Assert.Equal(500, mapping.StatusCode);
		Assert.Equal(shape, mapping.Shape);
		switch (shape)
		{
			case ResponseWireShape.ProblemError:
				Assert.Equal(ResponseWireMapper.BusinessErrorTitle, mapping.ProblemTitle);
				Assert.Equal(new BusinessError("stock"), ((Error)mapping.Value!).Payload);
				break;
			case ResponseWireShape.RawError:
				Assert.Equal(new BusinessError("stock"), mapping.Value);
				break;
			case ResponseWireShape.Envelope:
				Assert.Equal(response, (Response<string, BusinessError>)mapping.Value!);
				break;
		}
	}

	[Fact(DisplayName = "An uninitialized response is a 500 problem describing the programming error")]
	public void Unset_Is500Problem()
	{
		var mapping = Map(default(Response<string>), Options());

		Assert.Equal(500, mapping.StatusCode);
		Assert.Equal(ResponseWireShape.ProblemError, mapping.Shape);
		Assert.Contains("uninitialized", ((Error)mapping.Value!).Message);
	}

	[Fact(DisplayName = "A bare Error is mapped like an erroneous Response<Unit>")]
	public void BareError()
	{
		var mapping = Map(Error.NotFound("missing"), Options(problem: false));
		Assert.Equal(404, mapping.StatusCode);
		Assert.Equal(ResponseWireShape.NativeError, mapping.Shape);
	}

	[Fact(DisplayName = "A bare None is mapped like a ResponseMaybe in the None state")]
	public void BareNone()
	{
		Assert.Equal(204, Map(None.Value, Options()).StatusCode);
		Assert.Equal(ResponseWireShape.Envelope, Map(None.Value, Options(full: true)).Shape);
	}

	[Fact(DisplayName = "Anything that is not a union value is not mapped")]
	public void NotAUnionValue_IsNotMapped()
	{
		IsTrue(!ResponseWireMapper.TryMap("plain string", Options(), out _));
		IsTrue(!ResponseWireMapper.TryMap(null, Options(), out _));
	}

	[Theory(DisplayName = "Only the union shapes, Error and None (and their Task/ValueTask wrappers) are supported declared types")]
	[InlineData(typeof(Response<int>), true)]
	[InlineData(typeof(ResponseMaybe<int, string>), true)]
	[InlineData(typeof(System.Threading.Tasks.Task<Response<int>>), true)]
	[InlineData(typeof(System.Threading.Tasks.ValueTask<ResponseMaybe<int>>), true)]
	[InlineData(typeof(Error), true)]
	[InlineData(typeof(None), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(System.Collections.Generic.List<int>), false)]
	public void DeclaredTypeGate(System.Type type, bool supported)
		=> Assert.Equal(supported, ResponseWireMapper.IsSupportedDeclaredResponseType(type));

	[Theory(DisplayName = "A binary success is always a bare file, never the envelope")]
	[InlineData(false)]
	[InlineData(true)]
	public void Binary_NeverEnvelope(bool full)
	{
		using var file = new FileContent(new System.IO.MemoryStream(new byte[4]), "application/pdf", "a.pdf");
		Response<FileContent> response = file;

		var mapping = Map(response, Options(full: full));

		Assert.Equal(200, mapping.StatusCode);
		Assert.Equal(ResponseWireShape.Binary, mapping.Shape);
		Assert.Same(file, mapping.Value);
	}

	[Fact(DisplayName = "A bare Stream or byte[] success is wrapped as octet-stream file content")]
	public void StreamAndBytes_AreWrapped()
	{
		var stream = new System.IO.MemoryStream(new byte[2]);
		Response<System.IO.Stream> streamResponse = stream;
		var streamMapping = Map(streamResponse, Options());
		Assert.Equal(ResponseWireShape.Binary, streamMapping.Shape);
		var wrapped = Assert.IsType<FileContent>(streamMapping.Value);
		Assert.Same(stream, wrapped.Stream);
		Assert.Equal(BinaryPayload.DefaultContentType, wrapped.ContentType);

		Response<byte[]> bytesResponse = new byte[] { 1, 2, 3 };
		var bytesMapping = Map(bytesResponse, Options(full: true));
		Assert.Equal(ResponseWireShape.Binary, bytesMapping.Shape);
		Assert.Equal(3, Assert.IsType<FileContent>(bytesMapping.Value).Length);
	}

	[Fact(DisplayName = "None and errors of a binary response follow the normal rules")]
	public void BinaryResponse_NoneAndError_AreNormal()
	{
		ResponseMaybe<FileContent> none = None.Value;
		Assert.Equal(ResponseWireShape.NoContent, Map(none, Options()).Shape);

		Response<FileContent> error = Error.NotFound("missing");
		var mapping = Map(error, Options());
		Assert.Equal(404, mapping.StatusCode);
		Assert.Equal(ResponseWireShape.ProblemError, mapping.Shape);
	}
}

public class ResponseWireMaterializeTest(ITestOutputHelper output) : BaseTest<ResponseWireMaterializeTest>(output)
{
	static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
	static readonly JsonSerializerOptions Snake = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

	[Fact(DisplayName = "No content has neither content type nor body")]
	public void NoContent()
	{
		var (contentType, body, serializerOptions) = new ResponseWireMapping(204, ResponseWireShape.NoContent, null).Materialize(Web);
		Assert.Null(contentType);
		Assert.Null(body);
		Assert.Same(Web, serializerOptions);
	}

	[Fact(DisplayName = "Unit is an empty object announced with its media type and the naming parameter")]
	public void Unit_()
	{
		var (contentType, body, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Unit, null).Materialize(Web);

		Assert.Equal("application/vnd.fuxion.unit+json; naming=camel", contentType);
		Assert.Same(ResponseWireMapping.EmptyObject, body);
		Assert.Same(Web, serializerOptions);
		Assert.Equal("{}", JsonSerializer.Serialize(body, Web));
	}

	[Fact(DisplayName = "A bare payload is plain application/json without parameters")]
	public void Payload_()
	{
		var (contentType, body, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Payload, "test").Materialize(Snake);
		Assert.Equal(ResponseMediaTypes.Json, contentType);
		Assert.Equal("test", body);
		Assert.Same(Snake, serializerOptions);
	}

	[Fact(DisplayName = "Fuxion media types always announce the naming policy in use")]
	public void FuxionTypes_CarryNaming()
	{
		Response<string> response = "test";
		Assert.Equal("application/vnd.fuxion.response+json; naming=snake",
			new ResponseWireMapping(200, ResponseWireShape.Envelope, response).Materialize(Snake).ContentType);
		Assert.Equal("application/vnd.fuxion.error+json; naming=camel",
			new ResponseWireMapping(500, ResponseWireShape.NativeError, Error.Custom("boom")).Materialize(Web).ContentType);
		Assert.Equal("application/vnd.fuxion.unit+json; naming=pascal",
			new ResponseWireMapping(200, ResponseWireShape.Unit, null).Materialize(null).ContentType);
	}

	[Fact(DisplayName = "A problem error becomes RFC 9457 ProblemDetails, honouring the title override")]
	public void ProblemError_()
	{
		var wrapped = new Error { Payload = new ResponseWireMapperTest.BusinessError("stock") };
		var (contentType, body, serializerOptions) = new ResponseWireMapping(500, ResponseWireShape.ProblemError, wrapped, ResponseWireMapper.BusinessErrorTitle).Materialize(Web);

		Assert.Equal(ResponseMediaTypes.ProblemJson, contentType);
		var problem = Assert.IsType<global::Fuxion.ResponseProblemDetails>(body);
		Assert.Equal(ResponseWireMapper.BusinessErrorTitle, problem.Title);
		Assert.Equal(500, problem.Status);
		IsTrue(problem.Extensions.ContainsKey("errorPayload"));
		Assert.Equal(JsonNamingPolicy.CamelCase, serializerOptions?.PropertyNamingPolicy);
	}

	[Fact(DisplayName = "A problem error without title override keeps the status-derived title")]
	public void ProblemError_DefaultTitle()
	{
		var (_, body, _) = new ResponseWireMapping(404, ResponseWireShape.ProblemError, Error.NotFound("missing")).Materialize(Web);
		var problem = Assert.IsType<global::Fuxion.ResponseProblemDetails>(body);
		Assert.Equal(404, problem.Status);
		Assert.Equal("missing", problem.Detail);
		Assert.NotEqual(ResponseWireMapper.BusinessErrorTitle, problem.Title);
	}

	[Fact(DisplayName = "problem+json is always written in camel, whatever the server policy")]
	public void ProblemError_AlwaysCamel_WhateverServerPolicy()
	{
		var wrapped = new Error { Payload = new ResponseWireMapperTest.BusinessError("stock") };
		var (_, body, serializerOptions) = new ResponseWireMapping(500, ResponseWireShape.ProblemError, wrapped, ResponseWireMapper.BusinessErrorTitle).Materialize(Snake);

		var problem = Assert.IsType<global::Fuxion.ResponseProblemDetails>(body);
		IsTrue(problem.Extensions.ContainsKey("errorPayload"));
		IsTrue(!problem.Extensions.ContainsKey("error_payload"));
		Assert.Equal(JsonNamingPolicy.CamelCase, serializerOptions?.PropertyNamingPolicy);
	}

	[Fact(DisplayName = "A Payload shape keeps the exact options instance the caller passed in")]
	public void Payload_KeepsSameOptionsInstance()
	{
		var (_, _, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Payload, "test").Materialize(Snake);
		Assert.Same(Snake, serializerOptions);
	}

	[Fact(DisplayName = "A binary shape materializes as the file's own media type without parameters or JSON options")]
	public void Binary_()
	{
		using var file = new FileContent(new System.IO.MemoryStream(new byte[1]), "image/png", "a.png");
		var (contentType, body, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Binary, file).Materialize(Snake);

		Assert.Equal("image/png", contentType);
		Assert.Same(file, body);
		Assert.Null(serializerOptions);
	}
}
