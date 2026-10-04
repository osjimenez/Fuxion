using System;
using System.Net;
using System.Text.Json;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ResponseWireMapperTest(ITestOutputHelper output) : BaseTest<ResponseWireMapperTest>(output)
{

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
	public void Unit_MapsTo200OrEnvelope(bool full, ResponseWireShape shape)
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
	public void None_MapsTo204UnlessEnvelope(bool full, bool strict, int status, ResponseWireShape shape)
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
	public void Payload_MapsToBareValueOrEnvelope(bool full, ResponseWireShape shape)
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
	public void NativeError_FollowsFlagsAndType(bool full, bool problem, ResponseWireShape shape)
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
	public void TypedError_MapsToProblemEnvelopeOrBareValue(bool full, bool problem, ResponseWireShape shape)
	{
		Response<string, TestError> response = new TestError("stock");
		var mapping = Map(response, Options(full: full, problem: problem));

		Assert.Equal(500, mapping.StatusCode);
		Assert.Equal(shape, mapping.Shape);
		switch (shape)
		{
			case ResponseWireShape.ProblemError:
				Assert.Equal(ResponseWireMapper.BusinessErrorTitle, mapping.ProblemTitle);
				Assert.Equal(new TestError("stock"), ((Error)mapping.Value!).Payload);
				break;
			case ResponseWireShape.RawError:
				Assert.Equal(new TestError("stock"), mapping.Value);
				break;
			case ResponseWireShape.Envelope:
				Assert.Equal(response, (Response<string, TestError>)mapping.Value!);
				break;
		}
	}

	[Fact(DisplayName = "A business error declares its status through IHttpStatusError")]
	public void TypedError_DeclaredStatus()
	{
		Response<string, Conflicting> response = new Conflicting();
		IsTrue(ResponseWireMapper.TryMap(response, new ResponseOptions(), out var mapping));
		Assert.Equal(409, mapping.StatusCode);
		Assert.Equal(ResponseWireShape.ProblemError, mapping.Shape);
		Assert.Equal(409, ((ResponseProblemDetails)mapping.Materialize(null).Body!).Status);
	}

	[Fact(DisplayName = "The service's BusinessErrorStatus wins over the error's own declaration; null means no opinion")]
	public void TypedError_OptionsOverride()
	{
		var options = new ResponseOptions { BusinessErrorStatus = e => e is Conflicting ? HttpStatusCode.Gone : null };
		IsTrue(ResponseWireMapper.TryMap((Response<string, Conflicting>)new Conflicting(), options, out var overridden));
		Assert.Equal(410, overridden.StatusCode);
		IsTrue(ResponseWireMapper.TryMap((Response<string, Plain>)new Plain(), options, out var untouched));
		Assert.Equal(500, untouched.StatusCode);
	}

	[Fact(DisplayName = "The status also drives the raw and envelope shapes")]
	public void TypedError_StatusOnRawAndEnvelope()
	{
		var raw = new ResponseOptions { SerializeErrorAsProblemDetails = false };
		IsTrue(ResponseWireMapper.TryMap((Response<string, Conflicting>)new Conflicting(), raw, out var rawMapping));
		Assert.Equal(409, rawMapping.StatusCode);
		Assert.Equal(ResponseWireShape.RawError, rawMapping.Shape);
		IsTrue(ResponseWireMapper.TryMap((Response<string, Conflicting>)new Conflicting(), raw with { SerializeFullResponses = true }, out var envelope));
		Assert.Equal(409, envelope.StatusCode);
	}

	sealed record Conflicting : IHttpStatusError { public HttpStatusCode Status => HttpStatusCode.Conflict; }
	sealed record Plain;

	[Fact(DisplayName = "An uninitialized response is a 500 problem describing the programming error")]
	public void Unset_Is500Problem()
	{
		var mapping = Map(default(Response<string>), Options());

		Assert.Equal(500, mapping.StatusCode);
		Assert.Equal(ResponseWireShape.ProblemError, mapping.Shape);
		Assert.Contains("uninitialized", ((Error)mapping.Value!).Message);
	}

	[Theory(DisplayName = "An uninitialized response is a critical error that follows the error options")]
	[InlineData(true, ResponseWireShape.ProblemError)]
	[InlineData(false, ResponseWireShape.NativeError)]
	public void Default_FollowsErrorOptions(bool problem, ResponseWireShape expected)
	{
		IsTrue(ResponseWireMapper.TryMap(default(Response<string>), new ResponseOptions { SerializeErrorAsProblemDetails = problem }, out var mapping));
		Assert.Equal(500, mapping.StatusCode);
		Assert.Equal(expected, mapping.Shape);
		IsTrue(((Error)mapping.Value!).IsInternalServerError);
	}

	[Fact(DisplayName = "An uninitialized response with the envelope requested travels as an envelope")]
	public void Default_Envelope()
	{
		IsTrue(ResponseWireMapper.TryMap(default(Response<string>), new ResponseOptions { SerializeErrorAsProblemDetails = false, SerializeFullResponses = true }, out var mapping));
		Assert.Equal(ResponseWireShape.Envelope, mapping.Shape);
		IsTrue(((IResponse)mapping.Value!).IsError);
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
		IsFalse(ResponseWireMapper.TryMap("plain string", Options(), out _));
		IsFalse(ResponseWireMapper.TryMap(null, Options(), out _));
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

	// Some hosts report a null declared return type (e.g. Web API 2's ReflectedHttpActionDescriptor for
	// void and non-generic Task actions); the gate must treat that as "not supported", not throw.
	[Fact(DisplayName = "A null declared type is not a supported response type")]
	public void DeclaredTypeGate_Null_IsNotSupported()
		=> Assert.False(ResponseWireMapper.IsSupportedDeclaredResponseType(null));

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

	[Fact(DisplayName = "Map returns the same mapping as TryMap, and throws a NotSupportedException naming the value type when TryMap cannot map it")]
	public void Map_ThrowsWhereTryMapFails()
	{
		Response<int> response = 1;
		IsTrue(ResponseWireMapper.TryMap(response, new ResponseOptions(), out var expected));
		Assert.Equal(expected, ResponseWireMapper.Map(response, new ResponseOptions()));

		IsFalse(ResponseWireMapper.TryMap(42, new ResponseOptions(), out _));
		var ex = Assert.Throws<NotSupportedException>(() => ResponseWireMapper.Map(42, new ResponseOptions()));
		Assert.Equal("The union response value of type 'System.Int32' is not supported.", ex.Message);
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
	public void Unit_MaterializesAsEmptyObjectWithMediaType()
	{
		var (contentType, body, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Unit, null).Materialize(Web);

		Assert.Equal("application/vnd.fuxion.unit+json; naming=camel", contentType);
		Assert.Same(ResponseWireMapping.EmptyObject, body);
		Assert.Same(Web, serializerOptions);
		Assert.Equal("{}", JsonSerializer.Serialize(body, Web));
	}

	[Fact(DisplayName = "A bare payload is plain application/json without parameters")]
	public void Payload_MaterializesAsPlainJson()
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
	public void ProblemError_MaterializesAsProblemDetails()
	{
		var wrapped = new Error { Payload = new TestError("stock") };
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
		var wrapped = new Error { Payload = new TestError("stock") };
		var (_, body, serializerOptions) = new ResponseWireMapping(500, ResponseWireShape.ProblemError, wrapped, ResponseWireMapper.BusinessErrorTitle).Materialize(Snake);

		var problem = Assert.IsType<global::Fuxion.ResponseProblemDetails>(body);
		IsTrue(problem.Extensions.ContainsKey("errorPayload"));
		IsFalse(problem.Extensions.ContainsKey("error_payload"));
		Assert.Equal(JsonNamingPolicy.CamelCase, serializerOptions?.PropertyNamingPolicy);
	}

	[Fact(DisplayName = "A Payload shape keeps the exact options instance the caller passed in")]
	public void Payload_KeepsSameOptionsInstance()
	{
		var (_, _, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Payload, "test").Materialize(Snake);
		Assert.Same(Snake, serializerOptions);
	}

	[Fact(DisplayName = "A binary shape materializes as the file's own media type without parameters or JSON options")]
	public void Binary_MaterializesAsFileMediaType()
	{
		using var file = new FileContent(new System.IO.MemoryStream(new byte[1]), "image/png", "a.png");
		var (contentType, body, serializerOptions) = new ResponseWireMapping(200, ResponseWireShape.Binary, file).Materialize(Snake);

		Assert.Equal("image/png", contentType);
		Assert.Same(file, body);
		Assert.Null(serializerOptions);
	}

	[Fact(DisplayName = "A requested naming overrides the server policy only on shapes that announce it (Unit, Envelope, NativeError), never on Payload/RawError/problem+json")]
	public void RequestedNaming_OnlyOnAnnouncedShapes()
	{
		var serverSnake = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
		var options = ResponseAccept.Apply(new ResponseOptions { SerializeFullResponses = true }, $"{ResponseMediaTypes.ResponseJson}; naming={ResponseNaming.Kebab}");
		Response<TestPayload> response = new TestPayload("x", 1);
		IsTrue(ResponseWireMapper.TryMap(response, options, out var mapping));
		Assert.Equal(ResponseNaming.Kebab, mapping.Naming);
		var (contentType, _, serializerOptions) = mapping.Materialize(serverSnake);
		Assert.Equal($"{ResponseMediaTypes.ResponseJson}; naming=kebab", contentType);
		Assert.Same(JsonNamingPolicy.KebabCaseLower, serializerOptions!.PropertyNamingPolicy);

		IsTrue(ResponseWireMapper.TryMap((Response<Unit>)Error.NotFound("x"), options with { SerializeErrorAsProblemDetails = true }, out var problem));
		Assert.Same(JsonNamingPolicy.CamelCase, problem.Materialize(serverSnake).SerializerOptions!.PropertyNamingPolicy);

		// A bare payload under application/json never announces a naming policy: it must not carry the
		// requested naming at all, and must materialize with the server's own policy.
		IsTrue(ResponseWireMapper.TryMap(response, options with { SerializeFullResponses = false }, out var payload));
		Assert.Equal(ResponseWireShape.Payload, payload.Shape);
		Assert.Null(payload.Naming);
		Assert.Same(serverSnake, payload.Materialize(serverSnake).SerializerOptions);
	}
}
