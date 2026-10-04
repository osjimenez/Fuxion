using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

// Pins that the union types serialize the same through reflection and through a source-generated context
// declared in a consumer assembly (this one). The [JsonConverter] attributes on the unions point to converters
// in Fuxion; the context instantiates them from the attribute, so they keep working even when they are internal.
public class ResponseJsonSourceGenerationTest(ITestOutputHelper output) : BaseTest<ResponseJsonSourceGenerationTest>(output)
{
	public static IEnumerable<object[]> Values() =>
	[
		[typeof(Response<SourceGenPayload>), (Response<SourceGenPayload>)new SourceGenPayload("Ana", 30)],
		[typeof(Response<SourceGenPayload>), (Response<SourceGenPayload>)Error.NotFound("missing")],
		[typeof(Response<SourceGenPayload, SourceGenError>), (Response<SourceGenPayload, SourceGenError>)new SourceGenPayload("Ana", 30)],
		[typeof(Response<SourceGenPayload, SourceGenError>), (Response<SourceGenPayload, SourceGenError>)new SourceGenError("E1")],
		[typeof(ResponseMaybe<SourceGenPayload>), (ResponseMaybe<SourceGenPayload>)new SourceGenPayload("Ana", 30)],
		[typeof(ResponseMaybe<SourceGenPayload>), (ResponseMaybe<SourceGenPayload>)None.Value],
		[typeof(ResponseMaybe<SourceGenPayload>), (ResponseMaybe<SourceGenPayload>)Error.NotFound("missing")],
		[typeof(ResponseMaybe<SourceGenPayload, SourceGenError>), (ResponseMaybe<SourceGenPayload, SourceGenError>)new SourceGenPayload("Ana", 30)],
		[typeof(ResponseMaybe<SourceGenPayload, SourceGenError>), (ResponseMaybe<SourceGenPayload, SourceGenError>)None.Value],
		[typeof(ResponseMaybe<SourceGenPayload, SourceGenError>), (ResponseMaybe<SourceGenPayload, SourceGenError>)new SourceGenError("E1")],
		[typeof(None), None.Value],
		[typeof(Unit), Unit.Value],
	];

	[Theory(DisplayName = "Union values round-trip through reflection-based serialization")]
	[MemberData(nameof(Values))]
	public void Reflection_RoundTrips(Type type, object value)
	{
		var options = new JsonSerializerOptions();
		var json = JsonSerializer.Serialize(value, type, options);
		PrintVariable(json);
		var back = JsonSerializer.Deserialize(json, type, options);
		Assert.Equal(json, JsonSerializer.Serialize(back, type, options));
	}

	[Theory(DisplayName = "A source-generated context in a consumer assembly writes the same JSON as reflection and reads it back")]
	[MemberData(nameof(Values))]
	public void ConsumerSourceGenContext_MatchesReflection(Type type, object value)
	{
		var expected = JsonSerializer.Serialize(value, type, new JsonSerializerOptions());
		var json = JsonSerializer.Serialize(value, type, SourceGenerationJsonContext.Default);
		PrintVariable(json);
		Assert.Equal(expected, json);
		var back = JsonSerializer.Deserialize(json, type, SourceGenerationJsonContext.Default);
		Assert.Equal(json, JsonSerializer.Serialize(back, type, SourceGenerationJsonContext.Default));
	}
}

public record SourceGenPayload(string Name, int Age);
public record SourceGenError(string Code);

#pragma warning disable SYSLIB1227 // Union case types that serialize as the same JSON value kind
[JsonSerializable(typeof(Response<SourceGenPayload>))]
[JsonSerializable(typeof(Response<SourceGenPayload, SourceGenError>))]
[JsonSerializable(typeof(ResponseMaybe<SourceGenPayload>))]
[JsonSerializable(typeof(ResponseMaybe<SourceGenPayload, SourceGenError>))]
[JsonSerializable(typeof(None))]
[JsonSerializable(typeof(Unit))]
// The union converters delegate the cases to the options they receive, and the generator does not walk into
// types that declare their own converter, so the consumer registers the case types explicitly.
[JsonSerializable(typeof(SourceGenPayload))]
[JsonSerializable(typeof(SourceGenError))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(ErrorSource?))]
internal partial class SourceGenerationJsonContext : JsonSerializerContext;
#pragma warning restore SYSLIB1227
