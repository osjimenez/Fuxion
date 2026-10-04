using System;
using System.Net;
using System.Text.Json;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ResponseProblemDetailsTest(ITestOutputHelper output) : BaseTest<ResponseProblemDetailsTest>(output)
{
	readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	[Fact(DisplayName = "An Error maps to ProblemDetails with its message, its status and an HTTP status code type")]
	public void Error_ToProblemDetails_MapsBasicFieldsAndHttpStatusCodeType()
	{
		var error = Error.NotImplemented("not implemented");

		var problem = error.ToProblemDetails(jsonOptions);

		Assert.Equal((int)HttpStatusCode.NotImplemented, problem.Status);
		Assert.Equal("Not implemented", problem.Title);
		Assert.Equal("not implemented", problem.Detail);
		Assert.True(problem.Extensions.TryGetValue(ProblemDetailsExtensionKeys.ErrorType, out var type));
		Assert.Equal(HttpStatusCode.NotImplemented, type);
	}

	[Fact(DisplayName = "ProblemDetails maps back to an Error with the standard members, using the status as the fallback type")]
	public void ProblemDetails_ToError_MapsStandardFieldsAndFallbackStatusCodeType()
	{
		var problem = new ResponseProblemDetails
		{
			Type = "https://example.test/problems/not-found",
			Title = "Not found",
			Status = (int)HttpStatusCode.NotFound,
			Detail = "missing",
			Instance = "/users/1",
			Extensions =
			{
				["TraceId"] = "abc"
			}
		};

		var error = problem.ToError(jsonOptions);

		Assert.Equal("missing", error.Message);
		Assert.Equal(HttpStatusCode.NotFound, error.Type);
		Assert.Equal("https://example.test/problems/not-found", error.Extensions[ProblemDetailsExtensionKeys.ProblemType]);
		Assert.Equal("Not found", error.Extensions[ProblemDetailsExtensionKeys.ProblemTitle]);
		Assert.Equal("/users/1", error.Extensions[ProblemDetailsExtensionKeys.ProblemInstance]);
		Assert.Equal("abc", error.Extensions["TraceId"]);
	}

	[Fact(DisplayName = "The errorType extension takes priority over the status when mapping back to an Error")]
	public void ProblemDetails_ToError_ErrorTypeHasPriorityOverStatus()
	{
		var problem = new ResponseProblemDetails
		{
			Status = (int)HttpStatusCode.BadRequest,
			Detail = "domain error"
		};
		problem.Extensions[ProblemDetailsExtensionKeys.ErrorType] = "DomainError";

		var error = problem.ToError(jsonOptions);

		Assert.Equal("DomainError", error.Type);
	}

	[Fact(DisplayName = "The reserved extension names follow the JSON naming policy")]
	public void Converter_AppliesJsonNamingPolicyToReservedExtensionNames()
	{
		var options = new JsonSerializerOptions(jsonOptions)
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		var error = Error.NotImplemented("not implemented", payload: TestPayload.Default);

		var problem = error.ToProblemDetails(options);

		Assert.True(problem.Extensions.ContainsKey("errorType"));
		Assert.True(problem.Extensions.ContainsKey("errorPayload"));
		Assert.False(problem.Extensions.ContainsKey(ProblemDetailsExtensionKeys.ErrorType));
		Assert.False(problem.Extensions.ContainsKey(ProblemDetailsExtensionKeys.ErrorPayload));

		var roundtrip = problem.ToError(options);

		Assert.Equal(HttpStatusCode.NotImplemented, roundtrip.Type);
		var payload = roundtrip.GetPayloadAs<TestPayload>(options);
		Assert.NotNull(payload);
		Assert.Equal(TestPayload.Default, payload);
	}

	[Fact(DisplayName = "An Error survives a round trip through ProblemDetails with its payload, exception, inner errors and source")]
	public void Error_ProblemDetails_Error_Roundtrip_PreservesPayloadExceptionInnerErrorsAndSource()
	{
		var inner = Error.NotFound("inner missing");
		var error = Error.NotImplemented(
			"outer",
			payload: TestPayload.Default,
			exception: new InvalidOperationException("boom"),
			extensions: new ExtensionsDictionary { ["Custom"] = "value" },
			innerErrors: [inner]);

		var problem = error.ToProblemDetails(jsonOptions);
		var roundtrip = problem.ToError(jsonOptions);

		Assert.Equal("outer", roundtrip.Message);
		Assert.Equal(HttpStatusCode.NotImplemented, roundtrip.Type);
		Assert.Equal(TestPayload.Default, roundtrip.Payload);
		Assert.IsType<RemoteException>(roundtrip.Exception);
		Assert.Equal("boom", roundtrip.Exception?.Message);
		Assert.NotNull(roundtrip.InnerErrors);
		var roundtripInner = Assert.Single(roundtrip.InnerErrors!);
		Assert.Equal("inner missing", roundtripInner.Message);
		Assert.Equal(HttpStatusCode.NotFound, roundtripInner.Type);
		Assert.NotNull(roundtrip.Source);
		Assert.Equal("value", roundtrip.Extensions["Custom"]);
	}

	[Fact(DisplayName = "An Error survives a round trip through ProblemDetails serialized as JSON")]
	public void Error_ProblemDetails_Json_Error_Roundtrip_PreservesJsonValues()
	{
		var error = Error.NotImplemented(
			"not implemented",
			payload: TestPayload.Default,
			exception: new NotImplementedException("missing feature"));

		var problem = error.ToProblemDetails(jsonOptions);
		var json = problem.Fx.Json.Serialize(options: jsonOptions).SuccessOrThrow();
		var deserializedProblem = json.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions).SuccessOrThrow();
		var roundtrip = deserializedProblem.ToError(jsonOptions);

		Assert.Equal(HttpStatusCode.NotImplemented, roundtrip.Type);
		var payload = roundtrip.GetPayloadAs<TestPayload>(jsonOptions);
		Assert.NotNull(payload);
		Assert.Equal(TestPayload.Default, payload);
		var exception = Assert.IsType<RemoteException>(roundtrip.Exception);
		Assert.Equal("missing feature", exception.Message);
	}
}

