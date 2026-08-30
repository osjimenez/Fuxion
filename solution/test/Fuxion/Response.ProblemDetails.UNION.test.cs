using Fuxion;
using Fuxion.Collections.Generic;
using Fuxion.Text.Json;
using Fuxion.Union;
using Fuxion.Xunit;
using System;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Test.Fuxion.Union;

public class ResponseProblemDetailsTest(ITestOutputHelper output) : BaseTest<ResponseProblemDetailsTest>(output)
{
   readonly JsonSerializerOptions jsonOptions = new()
   {
      PropertyNameCaseInsensitive = true
   };

   [Fact]
   public void Error_ToProblemDetails_MapsBasicFieldsAndHttpStatusCodeType()
   {
      var error = Error.NotImplemented("not implemented");

      var problem = ErrorProblemDetailsConverter.ToProblemDetails(error, jsonOptions);

      Assert.Equal((int)HttpStatusCode.NotImplemented, problem.Status);
      Assert.Equal("Not implemented", problem.Title);
      Assert.Equal("not implemented", problem.Detail);
      Assert.True(problem.Extensions.TryGetValue(ErrorProblemDetailsConverter.ErrorTypeExtensionName, out var type));
      Assert.Equal(HttpStatusCode.NotImplemented, type);
   }

   [Fact]
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

      var error = ErrorProblemDetailsConverter.ToError(problem, jsonOptions);

      Assert.Equal("missing", error.Message);
      Assert.Equal(HttpStatusCode.NotFound, error.Type);
      Assert.Equal("https://example.test/problems/not-found", error.Extensions[ErrorProblemDetailsConverter.ProblemTypeExtensionName]);
      Assert.Equal("Not found", error.Extensions[ErrorProblemDetailsConverter.ProblemTitleExtensionName]);
      Assert.Equal("/users/1", error.Extensions[ErrorProblemDetailsConverter.ProblemInstanceExtensionName]);
      Assert.Equal("abc", error.Extensions["TraceId"]);
   }

   [Fact]
   public void ProblemDetails_ToError_ErrorTypeHasPriorityOverStatus()
   {
      var problem = new ResponseProblemDetails
      {
         Status = (int)HttpStatusCode.BadRequest,
         Detail = "domain error"
      };
      problem.Extensions[ErrorProblemDetailsConverter.ErrorTypeExtensionName] = "DomainError";

      var error = ErrorProblemDetailsConverter.ToError(problem, jsonOptions);

      Assert.Equal("DomainError", error.Type);
   }

   [Fact]
   public void Converter_AppliesJsonNamingPolicyToReservedExtensionNames()
   {
      var options = new JsonSerializerOptions(jsonOptions)
      {
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };
      var error = Error.NotImplemented("not implemented", payload: TestProblemPayload.Default);

      var problem = ErrorProblemDetailsConverter.ToProblemDetails(error, options);

      Assert.True(problem.Extensions.ContainsKey("errorType"));
      Assert.True(problem.Extensions.ContainsKey("errorPayload"));
      Assert.False(problem.Extensions.ContainsKey(ErrorProblemDetailsConverter.ErrorTypeExtensionName));
      Assert.False(problem.Extensions.ContainsKey(ErrorProblemDetailsConverter.ErrorPayloadExtensionName));

      var roundtrip = ErrorProblemDetailsConverter.ToError(problem, options);

      Assert.Equal(HttpStatusCode.NotImplemented, roundtrip.Type);
      var payload = roundtrip.GetPayloadAs<TestProblemPayload>(options);
      Assert.NotNull(payload);
      Assert.Equal(TestProblemPayload.Default, payload);
   }

   [Fact]
   public void Error_ProblemDetails_Error_Roundtrip_PreservesPayloadExceptionInnerErrorsAndSource()
   {
      var inner = Error.NotFound("inner missing");
      var error = Error.NotImplemented(
         "outer",
         payload: TestProblemPayload.Default,
         exception: new InvalidOperationException("boom"),
         extensions: new ExtensionsDictionary { ["Custom"] = "value" },
         innerErrors: [inner]);

      var problem = ErrorProblemDetailsConverter.ToProblemDetails(error, jsonOptions);
      var roundtrip = ErrorProblemDetailsConverter.ToError(problem, jsonOptions);

      Assert.Equal("outer", roundtrip.Message);
      Assert.Equal(HttpStatusCode.NotImplemented, roundtrip.Type);
      Assert.Equal(TestProblemPayload.Default, roundtrip.Payload);
      Assert.IsType<RemoteException>(roundtrip.Exception);
      Assert.Equal("boom", roundtrip.Exception?.Message);
      Assert.NotNull(roundtrip.InnerErrors);
      var roundtripInner = Assert.Single(roundtrip.InnerErrors!);
      Assert.Equal("inner missing", roundtripInner.Message);
      Assert.Equal(HttpStatusCode.NotFound, roundtripInner.Type);
      Assert.NotNull(roundtrip.Source);
      Assert.Equal("value", roundtrip.Extensions["Custom"]);
   }

   [Fact]
   public void Error_ProblemDetails_Json_Error_Roundtrip_PreservesJsonValues()
   {
      var error = Error.NotImplemented(
         "not implemented",
         payload: TestProblemPayload.Default,
         exception: new NotImplementedException("missing feature"));

      var problem = ErrorProblemDetailsConverter.ToProblemDetails(error, jsonOptions);
      var json = problem.Fx.Json.Serialize(options: jsonOptions).PayloadOrThrow();
      var deserializedProblem = json.Fx.Json.Deserialize<ResponseProblemDetails>(options: jsonOptions).PayloadOrThrow();
      var roundtrip = ErrorProblemDetailsConverter.ToError(deserializedProblem, jsonOptions);

      Assert.Equal(HttpStatusCode.NotImplemented, roundtrip.Type);
      var payload = roundtrip.GetPayloadAs<TestProblemPayload>(jsonOptions);
      Assert.NotNull(payload);
      Assert.Equal(TestProblemPayload.Default, payload);
      var exception = Assert.IsType<RemoteException>(roundtrip.Exception);
      Assert.Equal("missing feature", exception.Message);
   }
}

public sealed record TestProblemPayload(string Name, int Age)
{
   public static TestProblemPayload Default { get; } = new("test", 123);
}
