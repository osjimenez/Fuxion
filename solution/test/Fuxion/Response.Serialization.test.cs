using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ResponseSerializationTest(ITestOutputHelper output) : BaseTest<ResponseSerializationTest>(output)
{
	public enum Form { Response, TypedResponse, Maybe, TypedMaybe }

	static bool IsMaybe(Form form) => form is Form.Maybe or Form.TypedMaybe;

	#region Serialization

	void AssertResponseJson(
		string json,
		bool isSuccess,
		bool? isNone = null,
		bool containsPayload = false,
		bool containsError = false,
		AssertJsonEntry[]? additionalAsserts = null)
	{
		PrintVariable(json, false);
		List<AssertJsonEntry> asserts = [new([nameof(IResponse.IsSuccess)], isSuccess)];

		if (isNone is null)
			asserts.Add(new([nameof(ResponseMaybe<>.IsNone)], IsPresent: false));
		else
			asserts.Add(new([nameof(ResponseMaybe<>.IsNone)], isNone.Value));

		if (containsPayload)
			asserts.Add(new([ResponseConstants.PayloadPropertyName]));
		else
			asserts.Add(new([ResponseConstants.PayloadPropertyName], IsPresent: false));

		if (containsError)
			asserts.Add(new([ResponseConstants.ErrorPropertyName]));
		else
			asserts.Add(new([ResponseConstants.ErrorPropertyName], IsPresent: false));

		asserts.Add(new([nameof(IUnion.Value)], IsPresent: false));

		if (additionalAsserts is not null)
			asserts.AddRange(additionalAsserts);

		AssertJson(json, asserts.ToArray());
	}

	// Serialized as object so System.Text.Json uses the runtime union type and its converter.
	static string Serialize(object response) => response.Fx.Json.Serialize(true).SuccessOrThrow();

	[Theory(DisplayName = "A success is written as IsSuccess and Payload, plus IsNone=false for the maybe forms")]
	[InlineData(Form.Response)]
	[InlineData(Form.TypedResponse)]
	[InlineData(Form.Maybe)]
	[InlineData(Form.TypedMaybe)]
	public void Serialization_Success(Form form)
	{
		var user = new TestPayload("test", 123);
		object response = form switch
		{
			Form.Response => (Response<TestPayload>)user,
			Form.TypedResponse => (Response<TestPayload, string>)user,
			Form.Maybe => (ResponseMaybe<TestPayload>)user,
			_ => (ResponseMaybe<TestPayload, string>)user,
		};
		AssertResponseJson(Serialize(response), true, isNone: IsMaybe(form) ? false : null, containsPayload: true);
	}

	[Theory(DisplayName = "None is written as IsSuccess and IsNone, without payload")]
	[InlineData(Form.Maybe)]
	[InlineData(Form.TypedMaybe)]
	public void Serialization_None(Form form)
	{
		object response = form is Form.Maybe ? (ResponseMaybe<TestPayload>)None.Value : (ResponseMaybe<TestPayload, string>)None.Value;
		AssertResponseJson(Serialize(response), true, isNone: true);
	}

	[Theory(DisplayName = "An error is written as IsSuccess=false and Error, plus IsNone=false for the maybe forms")]
	[InlineData(Form.Response)]
	[InlineData(Form.TypedResponse)]
	[InlineData(Form.Maybe)]
	[InlineData(Form.TypedMaybe)]
	public void Serialization_Error(Form form)
	{
		object response = form switch
		{
			Form.Response => (Response<TestPayload>)Error.Custom("message"),
			Form.TypedResponse => (Response<TestPayload, string>)"error",
			Form.Maybe => (ResponseMaybe<TestPayload>)Error.Custom("message"),
			_ => (ResponseMaybe<TestPayload, string>)"error",
		};
		AssertResponseJson(Serialize(response), false, isNone: IsMaybe(form) ? false : null, containsError: true);
	}

	[Theory(DisplayName = "A custom TError is written under Error with its own members")]
	[InlineData(Form.TypedResponse)]
	[InlineData(Form.TypedMaybe)]
	public void Serialization_CustomError(Form form)
	{
		var error = new CustomError("message");
		object response = form is Form.TypedResponse ? (Response<TestPayload, CustomError>)error : (ResponseMaybe<TestPayload, CustomError>)error;
		AssertResponseJson(Serialize(response), false, isNone: IsMaybe(form) ? false : null, containsError: true,
			additionalAsserts: [new(["Error", "Message"], "message")]);
	}

	static ExtensionsDictionary<IResponse> Extensions() => new()
	{
		["Ext1"] = "extension",
		["Ext2"] = new
		{
			Message = "extension",
		}
	};

	[Theory(DisplayName = "Extensions are written as top-level members of the envelope")]
	[InlineData(Form.Response)]
	[InlineData(Form.TypedResponse)]
	[InlineData(Form.Maybe)]
	[InlineData(Form.TypedMaybe)]
	public void Serialization_Extensions(Form form)
	{
		object response = form switch
		{
			Form.Response => new Response<Unit>(Unit.Value) { Extensions = Extensions() },
			Form.TypedResponse => new Response<Unit, string>(Unit.Value) { Extensions = Extensions() },
			Form.Maybe => new ResponseMaybe<Unit>(Unit.Value) { Extensions = Extensions() },
			_ => new ResponseMaybe<Unit, string>(Unit.Value) { Extensions = Extensions() },
		};
		AssertResponseJson(Serialize(response), true, isNone: IsMaybe(form) ? false : null,
			additionalAsserts: [new(["Ext1"], "extension"), new(["Ext2", "Message"], "extension")]);
	}

	[Fact(DisplayName = "The envelope follows the caller naming policy on both write and read")]
	public void Envelope_FollowsCallerNamingPolicy()
	{
		// A consumer that configures snake_case in its Program.cs knows nothing about Fuxion:
		// the envelope must be written and read with its policy, with no extra configuration.
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
		};

		ResponseMaybe<TestPayload> response = new TestPayload("test", 123);

		var json = JsonSerializer.Serialize(response, options);
		Output.WriteLine(json);

		var members = JsonNode.Parse(json)!.AsObject();
		IsTrue(members.ContainsKey("is_success"));
		IsTrue(members.ContainsKey("is_none"));

		var roundTrip = JsonSerializer.Deserialize<ResponseMaybe<TestPayload>>(json, options);

		Assert.True(roundTrip.TryGetValue(out TestPayload? payload));
		Assert.Equal("test", payload!.Name);
		Assert.Equal(123, payload.Age);
	}

	#endregion

	#region Deserialization

	static IResponse Deserialize(Form form, string json) => form switch
	{
		Form.Response => json.Fx.Json.Deserialize<Response<TestPayload>>().SuccessOrThrow(),
		Form.TypedResponse => json.Fx.Json.Deserialize<Response<TestPayload, string>>().SuccessOrThrow(),
		Form.Maybe => json.Fx.Json.Deserialize<ResponseMaybe<TestPayload>>().SuccessOrThrow(),
		_ => json.Fx.Json.Deserialize<ResponseMaybe<TestPayload, string>>().SuccessOrThrow(),
	};

	[Theory(DisplayName = "A success envelope is read back as the payload")]
	[InlineData(Form.Response)]
	[InlineData(Form.TypedResponse)]
	[InlineData(Form.Maybe)]
	[InlineData(Form.TypedMaybe)]
	public void Deserialization_Success(Form form)
	{
		var json = $$"""
			{
				{{(IsMaybe(form) ? "\"IsNone\": false," : "")}}
				"IsSuccess": true,
				"Payload": {
					"Age": 123,
					"Name": "test"
				},
				"Ext1": "extension"
			}
			""";
		var response = Deserialize(form, json);
		IsTrue(response.IsSuccess);
		IsFalse(response.IsError);
		var user = Assert.IsType<TestPayload>(response.Value);
		Assert.Equal("test", user.Name);
		Assert.Equal(123, user.Age);
	}

	[Theory(DisplayName = "IsNone=true is read back as None whatever the payload says")]
	[InlineData(Form.Maybe, "")]
	[InlineData(Form.Maybe, ", \"Payload\": null")]
	[InlineData(Form.Maybe, ", \"Payload\": {}")]
	[InlineData(Form.TypedMaybe, "")]
	[InlineData(Form.TypedMaybe, ", \"Payload\": null")]
	[InlineData(Form.TypedMaybe, ", \"Payload\": {}")]
	public void Deserialization_None(Form form, string payload)
	{
		var response = Assert.IsAssignableFrom<IResponseMaybe>(Deserialize(form, $$"""{ "IsNone": true, "IsSuccess": true{{payload}} }"""));
		IsTrue(response.IsNone);
		IsTrue(response.IsSuccess);
		IsFalse(response.IsError);
		Assert.IsType<None>(response.Value);
	}

	[Fact(DisplayName = "Response<T>: a native error is read back with its message, exception, type and payload")]
	public void Deserialization_Error_Response()
	{
		var json = """
			{
				"Error": {
					"Exception": {
						"Data": [],
						"HelpLink": null,
						"HResult": -2146233088,
						"InnerException": null,
						"Message": "exception",
						"Source": null,
						"StackTrace": null,
						"TargetSite": null
					},
					"Message": "error",
					"Payload": {
						"Age": 123,
						"Name": "test"
					},
					"Type": "NotFound"
				},
				"IsSuccess": false
			}
			""";
		Response<TestPayload> response = json.Fx.Json.Deserialize<Response<TestPayload>>().SuccessOrThrow();
		IsFalse(response.IsSuccess);
		IsTrue(response.IsError);
		var error = Assert.IsType<Error>(response.Value);
		Assert.Equal("error", error.Message);
		var remote = Assert.IsType<RemoteException>(error.Exception);
		Assert.Equal("exception", remote.Message);
		// The JSON carries the type as a plain string (no discriminator), so it is read back as text.
		Assert.Equal("NotFound", error.Type?.ToString());
		Assert.Equal("test", error.GetPayloadAs<TestPayload>()?.Name);
		Assert.Equal(123, error.GetPayloadAs<TestPayload>()?.Age);
	}

	[Fact(DisplayName = "Response<T, TError>: a TError is read back, whether it is a string or an object")]
	public void Deserialization_Error_TypedResponse()
	{
		Response<TestPayload, string> text = """{ "IsSuccess": false, "Error": "error" }""".Fx.Json.Deserialize<Response<TestPayload, string>>().SuccessOrThrow();
		IsFalse(text.IsSuccess);
		IsTrue(text.IsError);
		Assert.Equal("error", Assert.IsType<string>(text.Value));

		Response<TestPayload, CustomError> custom = """{ "IsSuccess": false, "Error": { "Message": "error" } }""".Fx.Json.Deserialize<Response<TestPayload, CustomError>>().SuccessOrThrow();
		IsFalse(custom.IsSuccess);
		IsTrue(custom.IsError);
		Assert.Equal("error", Assert.IsType<CustomError>(custom.Value).Message);
	}

	[Fact(DisplayName = "ResponseMaybe<T>: a native error with a numeric type is read back as an error")]
	public void Deserialization_Error_Maybe()
	{
		var json = """
			{
				"IsSuccess": false,
				"IsNone": false,
				"Error": {
					"Exception": null,
					"Message": "error",
					"Payload": null,
					"Type": 123
				}
			}
			""";
		ResponseMaybe<TestPayload> response = json.Fx.Json.Deserialize<ResponseMaybe<TestPayload>>().SuccessOrThrow();
		IsFalse(response.IsSuccess);
		IsFalse(response.IsNone);
		IsTrue(response.IsError);
		Assert.IsType<Error>(response.Value);
	}

	[Fact(DisplayName = "ResponseMaybe<T, TError>: a TError is read back without IsNone in the body")]
	public void Deserialization_Error_TypedMaybe()
	{
		ResponseMaybe<TestPayload, string> response = """{ "IsSuccess": false, "Error": "error" }""".Fx.Json.Deserialize<ResponseMaybe<TestPayload, string>>().SuccessOrThrow();
		IsFalse(response.IsSuccess);
		IsFalse(response.IsNone);
		IsTrue(response.IsError);
		Assert.Equal("error", (string)response);
	}

	[Theory(DisplayName = "Top-level unknown members are read back as extensions")]
	[InlineData(Form.Response)]
	[InlineData(Form.TypedResponse)]
	[InlineData(Form.Maybe)]
	[InlineData(Form.TypedMaybe)]
	public void Deserialization_Extensions(Form form)
	{
		var json = """
			{
				"IsSuccess": true,
				"Ext1": "extension",
				"Ext2": {
					"Message": "extension"
				}
			}
			""";
		IResponse response = form switch
		{
			Form.Response => json.Fx.Json.Deserialize<Response<Unit>>().SuccessOrThrow(),
			Form.TypedResponse => json.Fx.Json.Deserialize<Response<Unit, string>>().SuccessOrThrow(),
			Form.Maybe => json.Fx.Json.Deserialize<ResponseMaybe<Unit>>().SuccessOrThrow(),
			_ => json.Fx.Json.Deserialize<ResponseMaybe<Unit, string>>().SuccessOrThrow(),
		};
		Assert.Equal("extension", response.Extensions.GetAs<string>("Ext1"));
		Assert.Equal("extension", response.Extensions.GetAs<CustomError>("Ext2")?.Message);
	}

	[Fact(DisplayName = "Unknown members inside a native error are read back as the error's extensions")]
	public void Deserialization_Error_Extensions()
	{
		var json = """
			{
				"IsSuccess": false,
				"Error": {
					"Type": "NotFound",
					"Ext1": "extension",
					"Ext2": {
						"Message": "extension"
					}
				}
			}
			""";
		Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().SuccessOrThrow();
		var error = Assert.IsType<Error>(response.Value);
		Assert.Equal("extension", error.Extensions.GetAs<string>("Ext1"));
		Assert.Equal("extension", error.Extensions.GetAs<CustomError>("Ext2")?.Message);
	}

	#endregion
}
file record CustomError(string Message);
