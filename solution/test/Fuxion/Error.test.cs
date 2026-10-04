using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ErrorTest(ITestOutputHelper output) : BaseTest<ErrorTest>(output)
{
	#region Serialization

	// Builders shared by the serialization and deserialization tests: a test never calls another test.
	static string SerializeMessage() => Error.Custom("message").Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeType() => Error.Custom(type: CustomInfo.Default).Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeTypeHttpStatusCode() => Error.NotImplemented().Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializePayload() => Error.Custom(payload: CustomInfo.Default).Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeExtensions() => Error.Custom(extensions: new()
		{
			["Ext1"] = "extension",
			["Ext2"] = new
			{
				Message = "message",
			}
		}).Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeException() => Error.Custom(exception: new Exception("message")).Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeWrap() => Error.NotImplemented(
				"message",
				CustomInfo.Default,
				new Exception(),
				callerMemberName: "test")
			.Wrap().Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeInnerErrors() => Error.NotImplemented(
			innerErrors: [
				Error.NotFound(),
				Error.Unauthorized(),
				Error.Forbidden(),
				])
			.Fx.Json.Serialize(true).SuccessOrThrow();
	static string SerializeAggregateOfDifferentTypes() => Error.Aggregate(
				Error.NotFound(),
				Error.Unauthorized(),
				Error.Forbidden())
			.Fx.Json.Serialize(true).SuccessOrThrow();

	[Fact(DisplayName = "An error message is written as Message, together with the source")]
	public void Serialization_Message()
		=> AssertJson(SerializeMessage(), [
			new([nameof(Error.Message)], "message"),
			new([nameof(Error.Source)]),
			new([nameof(Error.Payload)], IsPresent: false),
			new([nameof(Error.Type)], IsPresent: false),
			new([nameof(Error.Exception)], IsPresent: false),
			]);
	[Fact(DisplayName = "A custom error type is written as an object under Type")]
	public void Serialization_Type()
		=> AssertJson(SerializeType(), [
			new([nameof(Error.Message)], IsPresent: false),
			new([nameof(Error.Source)]),
			new([nameof(Error.Payload)], IsPresent: false),
			new([nameof(Error.Type)], CustomInfo.Default),
			new([nameof(Error.Type), nameof(CustomInfo.Message)], CustomInfo.Default.Message),
			new([nameof(Error.Type), nameof(CustomInfo.Code)], CustomInfo.Default.Code),
			new([nameof(Error.Exception)], IsPresent: false),
		]);
	[Fact(DisplayName = "An HttpStatusCode type is written with its discriminator, name and code")]
	public void Serialization_Type_HttpStatusCode()
		=> AssertJson(SerializeTypeHttpStatusCode(), [
			new([nameof(Error.Message)], IsPresent: false),
			new([nameof(Error.Source)]),
			new([nameof(Error.Payload)], IsPresent: false),
			new([nameof(Error.Type), "$type"], nameof(HttpStatusCode)),
			new([nameof(Error.Type), "Name"], HttpStatusCode.NotImplemented.ToString()),
			new([nameof(Error.Type), "Code"], (int)HttpStatusCode.NotImplemented),
			new([nameof(Error.Exception)], IsPresent: false),
		]);
	[Fact(DisplayName = "A payload is written under Payload")]
	public void Serialization_Payload()
		=> AssertJson(SerializePayload(), [
			new([nameof(Error.Message)], IsPresent: false),
			new([nameof(Error.Source)]),
			new([nameof(Error.Payload)], CustomInfo.Default),
			new([nameof(Error.Type)], IsPresent: false),
			new([nameof(Error.Exception)], IsPresent: false),
		]);
	[Fact(DisplayName = "Extensions are written as top-level members of the error")]
	public void Serialization_Extensions()
		=> AssertJson(SerializeExtensions(), [
			new([nameof(Error.Message)], IsPresent: false),
			new([nameof(Error.Source)]),
			new([nameof(Error.Payload)], IsPresent: false),
			new([nameof(Error.Type)], IsPresent: false),
			new([nameof(Error.Exception)], IsPresent: false),
			new(["Ext1"],"extension"),
			new(["Ext2","Message"],"message"),
			]);
	[Fact(DisplayName = "An exception is written under Exception")]
	public void Serialization_Exception()
		=> AssertJson(SerializeException(), [
			new([nameof(Error.Message)], IsPresent: false),
			new([nameof(Error.Source)]),
			new([nameof(Error.Payload)], IsPresent: false),
			new([nameof(Error.Type)], IsPresent: false),
			new([nameof(Error.Exception)]),
			]);
	[Fact(DisplayName = "A wrapped error copies the message and type, but not the payload or exception, and nests the original")]
	public void Serialization_Wrap()
		=> AssertJson(SerializeWrap(), [
			new([nameof(Error.Message)], "message"), // Must be copied from the inner error
			new([nameof(Error.Source), nameof(ErrorSource.MethodName)], nameof(SerializeWrap)), // Where Wrap was called, not the inner source
			new([nameof(Error.Payload)], IsPresent: false), // Mustn't be copied from the inner error
			new([nameof(Error.Type),"Code"], (int)HttpStatusCode.NotImplemented), // Must be copied from the inner error
			new([nameof(Error.Exception)], IsPresent: false), // Mustn't be copied from the inner error

			new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Message)], "message"),
			new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotImplemented),
			new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Payload)], CustomInfo.Default),
			]);
	[Fact(DisplayName = "Inner errors are written in order under InnerErrors")]
	public void Serialization_InnerErrors()
		=> AssertJson(SerializeInnerErrors(), [
			new([nameof(Error.Type),"Code"], (int)HttpStatusCode.NotImplemented),
			new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
			new([nameof(Error.InnerErrors) + "[1]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Unauthorized),
			new([nameof(Error.InnerErrors) + "[2]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Forbidden),
			]);
	[Fact(DisplayName = "An aggregate takes the common type of its inner errors, or 500 when they differ")]
	public void Serialization_Aggregate()
	{
		{
			var json = Error.Aggregate(
					Error.NotFound("message"),
					Error.NotFound())
				.Fx.Json.Serialize(true).SuccessOrThrow();

			AssertJson(json, [
				new([nameof(Error.Message)], "One or more errors occurred. (message)"),
				new([nameof(Error.Type),"Code"], (int)HttpStatusCode.NotFound), // When all inner errors have the same type, the aggregate error takes that type
				new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
				new([nameof(Error.InnerErrors) + "[1]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
			]);
		}
		AssertJson(SerializeAggregateOfDifferentTypes(), [
			new([nameof(Error.Type),"Code"], (int)HttpStatusCode.InternalServerError),
			new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
			new([nameof(Error.InnerErrors) + "[1]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Unauthorized),
			new([nameof(Error.InnerErrors) + "[2]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Forbidden),
			]);
	}

	#endregion

	#region Deserialization

	[Fact(DisplayName = "An error message is read back")]
	public void Deserialization_Message()
	{
		Error error = SerializeMessage().Fx.Json.DeserializeErrorOrFail();
		Assert.NotNull(error.Message);
		Assert.Null(error.Payload);
		Assert.Null(error.Type);
		Assert.Null(error.Exception);
		Assert.NotNull(error.Source);

		Assert.Equal("message", error.Message);
	}
	[Fact(DisplayName = "A custom error type is read back as an object")]
	public void Deserialization_Type()
	{
		Error error = SerializeType().Fx.Json.DeserializeErrorOrFail();
		Assert.Null(error.Message);
		Assert.Null(error.Payload);
		Assert.NotNull(error.Type);
		Assert.Null(error.Exception);
		Assert.NotNull(error.Source);

		var info = error.GetTypeAs<CustomInfo>();
		Assert.NotNull(info);
		Assert.Equal(CustomInfo.Default.Message, info.Message);
		Assert.Equal(CustomInfo.Default.Code, info.Code);
	}
	[Fact(DisplayName = "An HttpStatusCode type is read back only when discriminator, name and code agree")]
	public void Deserialization_Type_HttpStatusCode()
	{
		// OK
		{
			Error error = SerializeTypeHttpStatusCode().Fx.Json.DeserializeErrorOrFail();
			Assert.Null(error.Message);
			Assert.Null(error.Payload);
			Assert.NotNull(error.Type);
			Assert.Null(error.Exception);

			if (error.Type is HttpStatusCode code)
				Assert.Equal(HttpStatusCode.NotImplemented, code);
			else
				Assert.Fail("Expected HttpStatusCode type");
		}
		// Not OK because 'Code' is not present (could be '$type' or 'Name' too)
		{
			var json = """
			{
				"Type": {
					"$type": "HttpStatusCode",
					"Name": "NotImplemented"
				}
			}
			""";
			PrintVariable(json, false);
			Error error = json.Fx.Json.DeserializeErrorOrFail();
			Assert.Null(error.Message);
			Assert.Null(error.Payload);
			Assert.NotNull(error.Type);
			Assert.Null(error.Exception);
			Assert.Null(error.Source);

			if (error.Type is not JsonElement element)
				Assert.Fail("Expected JsonElement");
		}
		// Not OK because 'Extra' is not expected
		{
			var json = """
			{
				"Type": {
					"$type": "HttpStatusCode",
					"Code": 501,
					"Name": "NotImplemented",
					"Extra": true
				}
			}
			""";
			PrintVariable(json, false);
			Error error = json.Fx.Json.DeserializeErrorOrFail();
			Assert.Null(error.Message);
			Assert.Null(error.Payload);
			Assert.NotNull(error.Type);
			Assert.Null(error.Exception);
			Assert.Null(error.Source);

			if (error.Type is not JsonElement element)
				Assert.Fail("Expected JsonElement");
		}
		// Not OK because the value of 'Code' not match with the value of 'Name'
		{
			var json = """
			{
				"Type": {
					"$type": "HttpStatusCode",
					"Code": 400,
					"Name": "NotImplemented"
				}
			}
			""";
			PrintVariable(json, false);
			Error error = json.Fx.Json.DeserializeErrorOrFail();
			Assert.Null(error.Message);
			Assert.Null(error.Payload);
			Assert.NotNull(error.Type);
			Assert.Null(error.Exception);
			Assert.Null(error.Source);

			if (error.Type is not JsonElement element)
				Assert.Fail("Expected JsonElement");
		}
	}
	[Fact(DisplayName = "A payload is read back")]
	public void Deserialization_Payload()
	{
		Error error = SerializePayload().Fx.Json.DeserializeErrorOrFail();
		Assert.Null(error.Message);
		Assert.NotNull(error.Payload);
		Assert.Null(error.Type);
		Assert.Null(error.Exception);
		Assert.NotNull(error.Source);

		var info = error.GetPayloadAs<CustomInfo>();
		Assert.NotNull(info);
		Assert.Equal(CustomInfo.Default.Message, info.Message);
		Assert.Equal(CustomInfo.Default.Code, info.Code);
	}
	[Fact(DisplayName = "Extensions are read back")]
	public void Deserialization_Extensions()
	{
		Error error = SerializeExtensions().Fx.Json.DeserializeErrorOrFail();
		Assert.Null(error.Message);
		Assert.Null(error.Payload);
		Assert.Null(error.Type);
		Assert.Null(error.Exception);
		Assert.NotNull(error.Source);

		Assert.Equal("extension", error.Extensions.GetAs<string>("Ext1"));
		Assert.Equal("message", error.Extensions.GetAs<CustomInfo>("Ext2")?.Message);
	}
	[Fact(DisplayName = "An exception is read back as a RemoteException and survives a second round trip")]
	public void Deserialization_Exception()
	{
		Error error = SerializeException().Fx.Json.DeserializeErrorOrFail();
		Assert.Null(error.Message);
		Assert.Null(error.Payload);
		Assert.Null(error.Type);
		Assert.NotNull(error.Exception);
		Assert.NotNull(error.Source);

		var json2 = error.Fx.Json.Serialize(true).SuccessOrThrow();
		Error error2 = json2.Fx.Json.DeserializeErrorOrFail();
		Assert.Null(error2.Message);
		Assert.Null(error2.Payload);
		Assert.Null(error2.Type);
		Assert.NotNull(error2.Exception);
		Assert.NotNull(error2.Source);
	}
	[Fact(DisplayName = "A wrapped error is read back with its nested original")]
	public void Deserialization_Wrap()
	{
		Error error = SerializeWrap().Fx.Json.DeserializeErrorOrFail();
		Assert.NotNull(error.Message);
		Assert.Null(error.Payload);
		Assert.NotNull(error.Type);
		Assert.Null(error.Exception);
		Assert.NotNull(error.Source);

		Assert.NotNull(error.InnerErrors);
		Assert.NotEmpty(error.InnerErrors);
		var inner = error.InnerErrors[0];
		Assert.NotNull(inner.Message);
		Assert.NotNull(inner.Payload);
		Assert.NotNull(inner.Type);
		Assert.NotNull(inner.Exception);
		Assert.NotNull(inner.Source);
	}
	[Fact(DisplayName = "Inner errors are read back in order")]
	public void Deserialization_InnerErrors()
	{
		Error error = SerializeInnerErrors().Fx.Json.DeserializeErrorOrFail();
		Assert.Equal(error.Type, HttpStatusCode.NotImplemented);
		Assert.NotNull(error.InnerErrors);
		Assert.NotEmpty(error.InnerErrors);
		Assert.Equal(3, error.InnerErrors.Length);
		Assert.Equal(error.InnerErrors[0].Type, HttpStatusCode.NotFound);
		Assert.Equal(error.InnerErrors[1].Type, HttpStatusCode.Unauthorized);
		Assert.Equal(error.InnerErrors[2].Type, HttpStatusCode.Forbidden);
	}
	[Fact(DisplayName = "An aggregate of different types is read back as 500 with its inner errors")]
	public void Deserialization_Aggregate()
	{
		Error error = SerializeAggregateOfDifferentTypes().Fx.Json.DeserializeErrorOrFail();
		Assert.Equal(error.Type, HttpStatusCode.InternalServerError);
		Assert.NotNull(error.InnerErrors);
		Assert.NotEmpty(error.InnerErrors);
		Assert.Equal(3, error.InnerErrors.Length);
		Assert.Equal(error.InnerErrors[0].Type, HttpStatusCode.NotFound);
		Assert.Equal(error.InnerErrors[1].Type, HttpStatusCode.Unauthorized);
		Assert.Equal(error.InnerErrors[2].Type, HttpStatusCode.Forbidden);
	}
	#endregion

	#region Extensions

	[Fact(DisplayName = "The reserved extension keys of an error are its own members")]
	public void Extensions_ReservedKeys_AreDefined()
	{
		Assert.Equal(6, ErrorConstants.ErrorExtensionsReservedKeys.Count);
		Assert.Contains(nameof(Error.Payload), ErrorConstants.ErrorExtensionsReservedKeys);
		Assert.Contains(nameof(Error.Message), ErrorConstants.ErrorExtensionsReservedKeys);
		Assert.Contains(nameof(Error.Type), ErrorConstants.ErrorExtensionsReservedKeys);
		Assert.Contains(nameof(Error.Exception), ErrorConstants.ErrorExtensionsReservedKeys);
		Assert.Contains(nameof(Error.InnerErrors), ErrorConstants.ErrorExtensionsReservedKeys);
		Assert.Contains(nameof(Error.Source), ErrorConstants.ErrorExtensionsReservedKeys);
	}
	[Fact(DisplayName = "A reserved key cannot be set as an error extension at initialization")]
	public void Extensions_ReservedKeys_ThrowOnInit()
	{
		foreach (var key in ErrorConstants.ErrorExtensionsReservedKeys)
			Throws<ReservedKeyExtensionException>(() => new Error(extensions: new()
			{
				[key] = "reserved"
			}));
		foreach (var key in ErrorConstants.ErrorExtensionsReservedKeys)
			Throws<ReservedKeyExtensionException>(() => new Error()
			{
				Extensions = new ExtensionsDictionary()
				{
					[key] = "reserved"
				}
			});
	}
	[Fact(DisplayName = "A reserved key cannot be added as an error extension afterwards")]
	public void Extensions_ReservedKeys_ThrowOnWith()
	{
		foreach (var key in ErrorConstants.ErrorExtensionsReservedKeys)
		{
			var error = new Error();
			Throws<ReservedKeyExtensionException>(() => error.WithExtension(key, "reserved"));
			Throws<ReservedKeyExtensionException>(() => error.WithExtensions([new(key, "reserved")]));
		}
	}

	[Fact(DisplayName = "WithExtension returns a new error with the extension and leaves the original untouched")]
	public void WithExtension_ReturnsANewError()
	{
		var original = Error.NotFound("missing");
		var extended = original.WithExtension("a", 1).WithExtensions([new("b", 2)]);

		Assert.Empty(original.Extensions);
		Assert.Equal(2, extended.Extensions.Count);
		Assert.Equal("missing", extended.Message);
	}

	[Fact(DisplayName = "Two errors with the same extensions are equal, however the extensions were given")]
	public void Equality_ComparesExtensionsByContent()
	{
		// Built by hand: the factories record the calling line as Source, which would make any two errors differ.
		var built = new Error { Message = "missing", Extensions = new ExtensionsDictionary { ["a"] = 1 } };
		var added = new Error { Message = "missing" }.WithExtension("a", 1);

		IsTrue(built == added);
		IsTrue(new Error { Message = "missing" } == new Error { Message = "missing", Extensions = new ExtensionsDictionary() });
		IsFalse(built == new Error { Message = "missing" }.WithExtension("a", 2));
	}

	#endregion

	#region InnerErrors

	[Fact(DisplayName = "An empty innerErrors array is stored as null")]
	public void InnerErrors_Empty_IsNull()
	{
		Assert.Null(Error.Custom("x", innerErrors: []).InnerErrors);
		Assert.Single(Error.Custom("x", innerErrors: [Error.Custom("inner")]).InnerErrors!);
	}
	[Fact(DisplayName = "Every factory accepts innerErrors and keeps them")]
	public void Factories_KeepInnerErrors()
	{
		Error[] inner = [Error.Custom("inner")];
		Error[] all =
		[
			Error.Custom("m", innerErrors: inner), Error.NotFound("m", innerErrors: inner), Error.Forbidden("m", innerErrors: inner),
			Error.Unauthorized("m", innerErrors: inner), Error.BadRequest("m", innerErrors: inner), Error.Conflict("m", innerErrors: inner),
			Error.InternalServerError("m", innerErrors: inner), Error.NotImplemented("m", innerErrors: inner), Error.ServiceUnavailable("m", innerErrors: inner),
			Error.RequestTimeout("m", innerErrors: inner)
		];
		Assert.All(all, e => Assert.Equal("inner", Assert.Single(e.InnerErrors!).Message));
	}

	#endregion

	#region Configuration

	[Fact(DisplayName = "Error type converters are startup configuration: adding one after an error was serialized throws")]
	public void ErrorTypeConverters_AfterFirstUse_AreFrozen()
	{
		// Serializing here guarantees the first use happened, whatever order the suite runs in.
		_ = Error.NotFound().Fx.Json.Serialize().SuccessOrThrow();
		var ex = Assert.Throws<InvalidOperationException>(() => ErrorJsonConverter.AddErrorTypeConverter(new NeverConvertsErrorType()));
		Assert.Contains("startup", ex.Message);
		Assert.DoesNotContain(ErrorJsonConverter.ErrorTypeConverters, c => c is NeverConvertsErrorType);
	}

	sealed class NeverConvertsErrorType : ErrorTypeJsonConverter
	{
		public override bool TryRead(JsonElement element, out object? value, JsonSerializerOptions options) { value = null; return false; }
		public override bool CanConvert(object? errorType) => false;
		public override void Write(Utf8JsonWriter writer, object errorType, JsonSerializerOptions options) => throw new NotSupportedException();
	}

	#endregion

	#region Typed access

	static Error DeserializedWithJsonPayloadAndExtension()
		=> """{ "Message": "m", "Payload": { "Message": "p", "Code": 1 }, "Ext": { "Message": "e", "Code": 2 } }""".Fx.Json.DeserializeErrorOrFail();

	[Fact(DisplayName = "Reading an extension as a type does not modify the dictionary")]
	public void ExtensionsGetAs_DoesNotMutate()
	{
		var error = DeserializedWithJsonPayloadAndExtension();
		Assert.IsType<JsonElement>(error.Extensions["Ext"]);
		Assert.Equal("e", error.Extensions.GetAs<CustomInfo>("Ext")?.Message);
		Assert.IsType<JsonElement>(error.Extensions["Ext"]);
	}

	[Fact(DisplayName = "TryGetPayloadAs, TryGetTypeAs and TryGetAs report expected failures as false but let unexpected exceptions through")]
	public void TryGetAs_OnlySwallowsExpectedFailures()
	{
		var error = DeserializedWithJsonPayloadAndExtension();
		var throwing = new JsonSerializerOptions { Converters = { new BrokenCustomInfoConverter() } };

		Assert.Throws<NotImplementedException>(() => error.TryGetPayloadAs<CustomInfo>(out _, throwing));
		Assert.Throws<NotImplementedException>(() => error.Extensions.TryGetAs<CustomInfo>("Ext", out _, throwing));

		// Expected failures stay a plain false.
		IsFalse(error.TryGetPayloadAs<int>(out _));
		IsFalse(error.Extensions.TryGetAs<int>("Ext", out _));
		IsFalse(Error.Custom(type: "plain").TryGetTypeAs<CustomInfo>(out _));
	}

	#endregion
	#region HTTP vocabulary predicates

	static readonly string[] HttpKinds = ["NotFound", "Forbidden", "Unauthorized", "BadRequest", "Conflict", "InternalServerError", "NotImplemented", "ServiceUnavailable", "RequestTimeout"];

	public static TheoryData<string> HttpKindData() => new(HttpKinds);

	static Error Make(string kind) => kind switch
	{
		"NotFound" => Error.NotFound(),
		"Forbidden" => Error.Forbidden(),
		"Unauthorized" => Error.Unauthorized(),
		"BadRequest" => Error.BadRequest(),
		"Conflict" => Error.Conflict(),
		"InternalServerError" => Error.InternalServerError(),
		"NotImplemented" => Error.NotImplemented(),
		"ServiceUnavailable" => Error.ServiceUnavailable(),
		_ => Error.RequestTimeout(),
	};

	static bool Is(Error error, string kind) => kind switch
	{
		"NotFound" => error.IsNotFound,
		"Forbidden" => error.IsForbidden,
		"Unauthorized" => error.IsUnauthorized,
		"BadRequest" => error.IsBadRequest,
		"Conflict" => error.IsConflict,
		"InternalServerError" => error.IsInternalServerError,
		"NotImplemented" => error.IsNotImplemented,
		"ServiceUnavailable" => error.IsServiceUnavailable,
		_ => error.IsRequestTimeout,
	};

	[Theory(DisplayName = "Each HTTP factory is recognized by its own predicate and by no other")]
	[MemberData(nameof(HttpKindData))]
	public void HttpPredicates_MatchTheirFactory(string kind)
	{
		var error = Make(kind);
		foreach (var other in HttpKinds)
			Assert.Equal(other == kind, Is(error, other));
	}

	#endregion

}
file record CustomInfo(string Message, int Code)
{
	public static CustomInfo Default { get; } = new CustomInfo("test", 123);
}
// Error cannot be the success type of a Response<T>, so bare errors are read through TryDeserializeError; this
// keeps the deserialization tests as one-liners and fails with the deserialization failure's message.
file static class ErrorJsonTestExtensions
{
	extension(JsonExtensions<string?> me)
	{
		public Error DeserializeErrorOrFail()
		{
			if (!me.TryDeserializeError(out var error, out var failure))
				Assert.Fail(failure.Message ?? "The JSON could not be read as an Error.");
			return error;
		}
	}
}
file sealed class BrokenCustomInfoConverter : JsonConverter<CustomInfo>
{
	public override CustomInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException("bug in a converter");
	public override void Write(Utf8JsonWriter writer, CustomInfo value, JsonSerializerOptions options) => throw new NotImplementedException("bug in a converter");
}