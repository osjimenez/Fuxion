using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Net.Http;
using Fuxion.Text.Json;
using Xunit;

namespace Test.Aot;

// The responses with the reflection of System.Text.Json disabled, as with trimming and native AOT: the contracts come
// from FuxionJsonContext and from a context of the consumer, and nothing falls back to reflection without saying so.
// That they write the same JSON as with reflection is pinned in Test.Http (Response.Json.SourceGeneration.test.cs).
public class ResponsesWithoutReflectionTest
{
	static readonly JsonSerializerOptions Options = CreateOptions();

	static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
		options.TypeInfoResolverChain.Add(AotJsonContext.Default);
		options.TypeInfoResolverChain.Add(FuxionJsonContext.Default);
		options.MakeReadOnly();
		return options;
	}

	// The JSON of an error also carries where it was created (its source), so only the rest is compared literally.
	static T RoundTrip<T>(T value, string? expectedJson = null)
	{
		var json = JsonSerializer.Serialize(value, Options.GetTypeInfo<T>());
		if (expectedJson is not null)
			Assert.Equal(expectedJson, json);
		var back = JsonSerializer.Deserialize(json, Options.GetTypeInfo<T>());
		Assert.Equal(json, JsonSerializer.Serialize(back!, Options.GetTypeInfo<T>()));
		return back!;
	}

	[Fact(DisplayName = "The reflection of System.Text.Json is disabled in this process")]
	public void ReflectionIsDisabled() => Assert.False(JsonSerializer.IsReflectionEnabledByDefault);

	[Fact(DisplayName = "A success and an error of Response<T> round-trip")]
	public void Response_RoundTrips()
	{
		var success = RoundTrip((Response<AotPayload>)new AotPayload("Ana", 30), """{"isSuccess":true,"payload":{"name":"Ana","age":30}}""");
		Assert.True(success.TryGetValue(out AotPayload? payload) && payload.Name == "Ana");

		var error = RoundTrip((Response<AotPayload>)Error.NotFound("missing"));
		Assert.True(error.TryGetValue(out Error read) && read.Type is HttpStatusCode.NotFound);
	}

	[Fact(DisplayName = "A business error of Response<T, TError> round-trips")]
	public void ResponseWithBusinessError_RoundTrips()
	{
		var error = RoundTrip((Response<AotPayload, AotError>)new AotError("E1"), """{"isSuccess":false,"error":{"code":"E1"}}""");
		Assert.True(error.TryGetValue(out AotError? read) && read.Code == "E1");
	}

	[Fact(DisplayName = "None, a value and an error of ResponseMaybe<T> round-trip")]
	public void ResponseMaybe_RoundTrips()
	{
		Assert.True(RoundTrip((ResponseMaybe<AotPayload>)None.Value, """{"isSuccess":true,"isNone":true}""").IsNone);
		Assert.True(RoundTrip((ResponseMaybe<AotPayload>)new AotPayload("Ana", 30), """{"isSuccess":true,"isNone":false,"payload":{"name":"Ana","age":30}}""").IsSuccess);
		Assert.True(RoundTrip((ResponseMaybe<AotPayload, AotError>)new AotError("E1"), """{"isSuccess":false,"isNone":false,"error":{"code":"E1"}}""").IsError);
	}

	[Fact(DisplayName = "Unit and None round-trip on their own")]
	public void UnitAndNone_RoundTrip()
	{
		RoundTrip(Unit.Value, "null");
		var json = JsonSerializer.Serialize(None.Value, Options.GetTypeInfo<None>());
		Assert.Equal(None.Value, JsonSerializer.Deserialize(json, Options.GetTypeInfo<None>()));
	}

	[Fact(DisplayName = "An error with its exception, its extensions and its inner errors round-trips")]
	public void ErrorWithException_RoundTrips()
	{
		var error = Error.InternalServerError("boom", exception: new InvalidOperationException("inner"))
			.WithExtension("traceId", "abc")
			with { InnerErrors = [Error.BadRequest("first")] };
		var json = JsonSerializer.Serialize(error, Options.GetTypeInfo<Error>());
		var back = JsonSerializer.Deserialize(json, Options.GetTypeInfo<Error>());

		Assert.Equal("boom", back.Message);
		Assert.Equal("inner", back.Exception?.Message);
		Assert.Equal("abc", back.Extensions.GetAs<string>("traceId", Options));
		Assert.Equal("first", Assert.Single(back.InnerErrors!).Message);
	}

	[Fact(DisplayName = "Undefinable<T> omits an undefined property and keeps a defined one")]
	public void Undefinable_IsOmittedWhenUndefined()
	{
		var options = new JsonSerializerOptions(Options)
		{
			TypeInfoResolver = JsonTypeInfoResolver.Combine(AotJsonContext.Default, FuxionJsonContext.Default).WithAddedModifier(UndefinableJsonTypeInfo.OmitUndefined)
		};
		Assert.Equal("{}", JsonSerializer.Serialize(new AotPatch(default), options.GetTypeInfo<AotPatch>()));
		Assert.Equal("""{"name":"Ana"}""", JsonSerializer.Serialize(new AotPatch("Ana"), options.GetTypeInfo<AotPatch>()));
		Assert.True(JsonSerializer.Deserialize("""{"name":"Ana"}""", options.GetTypeInfo<AotPatch>())!.Name.TryGetValue(out string? name) && name == "Ana");
	}

	[Fact(DisplayName = "The payload of an error, read as JSON, is recovered with the options")]
	public void ErrorPayload_IsRecoveredWithTheOptions()
	{
		var json = JsonSerializer.Serialize(Error.BadRequest("invalid", payload: new AotError("E1")), Options.GetTypeInfo<Error>());
		var back = JsonSerializer.Deserialize(json, Options.GetTypeInfo<Error>());
		Assert.Equal("E1", back.GetPayloadAs<AotError>(Options)?.Code);
	}

	[Fact(DisplayName = "An error goes to problem details and back without options: the defaults of Fuxion need no reflection")]
	public void ProblemDetails_WithoutOptions()
	{
		var problem = Error.NotFound("missing", exception: new InvalidOperationException("inner")).ToProblemDetails();
		var json = JsonSerializer.Serialize(problem, FuxionJsonContext.Default.ResponseProblemDetails);
		var back = JsonSerializer.Deserialize(json, FuxionJsonContext.Default.ResponseProblemDetails).ToError();

		Assert.Equal("missing", back.Message);
		Assert.Equal(HttpStatusCode.NotFound, back.Type);
		Assert.Equal("inner", back.Exception?.Message);
	}

	[Fact(DisplayName = "The client reads an envelope, a payload and a problem with the options of the consumer")]
	public async Task Client_ReadsWithTheOptions()
	{
		static HttpResponseMessage Message(HttpStatusCode status, string body, string contentType)
			=> new(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };
		var ct = TestContext.Current.CancellationToken;

		var envelope = await Message(HttpStatusCode.OK, """{"isSuccess":true,"payload":{"name":"Ana","age":30}}""", ResponseMediaTypes.ResponseJson)
			.AsResponseAsync<AotPayload>(Options, ct);
		Assert.True(envelope.TryGetValue(out AotPayload? fromEnvelope) && fromEnvelope.Age == 30);

		var payload = await Message(HttpStatusCode.OK, """{"name":"Ana","age":30}""", ResponseMediaTypes.Json).AsResponseAsync<AotPayload>(Options, ct);
		Assert.True(payload.TryGetValue(out AotPayload? bare) && bare.Name == "Ana");

		var problem = await Message(HttpStatusCode.NotFound, """{"title":"Not found","status":404,"detail":"missing"}""", ResponseMediaTypes.ProblemJson)
			.AsResponseAsync<AotPayload>(Options, ct);
		Assert.True(problem.TryGetValue(out Error error) && error.Message == "missing" && error.Type is HttpStatusCode.NotFound);

		var native = await Message(HttpStatusCode.NotFound, """{"message":"missing","type":{"$type":"HttpStatusCode","name":"NotFound","code":404}}""", ResponseMediaTypes.ErrorJson)
			.AsResponseAsync<AotPayload>(Options, ct);
		Assert.True(native.TryGetValue(out Error nativeError) && nativeError.Message == "missing" && nativeError.Type is HttpStatusCode.NotFound);
	}
}

public record AotPayload(string Name, int Age);
public record AotError(string Code);
public record AotPatch(Undefinable<string> Name);

[JsonSerializable(typeof(Response<AotPayload>))]
[JsonSerializable(typeof(Response<AotPayload, AotError>))]
[JsonSerializable(typeof(ResponseMaybe<AotPayload>))]
[JsonSerializable(typeof(ResponseMaybe<AotPayload, AotError>))]
[JsonSerializable(typeof(AotPayload))]
[JsonSerializable(typeof(AotError))]
[JsonSerializable(typeof(AotPatch))]
[JsonSerializable(typeof(Undefinable<string>))]
partial class AotJsonContext : JsonSerializerContext;
