using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Fuxion;
using Fuxion.Reflection;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;


public class ResponseTest(ITestOutputHelper output) : BaseTest<ResponseTest>(output)
{
	#region Static constructors

	void AssertStaticConstructor<TClass, TException>()
	{
		var ex = Assert.ThrowsAny<TypeInitializationException>(() =>
		{
			RuntimeHelpers.RunClassConstructor(typeof(TClass).TypeHandle);
		}, tiex => tiex.InnerException is TException ? null : $"InnerException isn't {typeof(TException).GetSignature()}");
		Output.WriteLine("Static constructor throw as expected: " + ex.InnerException!.Message);
	}
	[Fact(DisplayName = "TSuccess and TError cannot be the same type")]
	public void TSuccess_TError_CannotBeInitializedWithSameType()
	{
		AssertStaticConstructor<Response<string, string>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<string, string>, ResponseInitializationException>();
	}
	[Fact(DisplayName = "TSuccess cannot be None")]
	public void TSuccess_CannotBeInitializedWith_None_Type()
	{
		AssertStaticConstructor<Response<None>, ResponseInitializationException>();
		AssertStaticConstructor<Response<None, string>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<None>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<None, string>, ResponseInitializationException>();
	}
	[Fact(DisplayName = "TSuccess cannot be Error")]
	public void TSuccess_CannotBeInitializedWith_Error_Type()
	{
		AssertStaticConstructor<Response<Error>, ResponseInitializationException>();
		AssertStaticConstructor<Response<Error, string>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<Error>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<Error, string>, ResponseInitializationException>();
	}
	[Fact(DisplayName = "TError cannot be None")]
	public void TError_CannotBeInitializedWith_None_Type()
	{
		AssertStaticConstructor<Response<string, None>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<string, None>, ResponseInitializationException>();
	}
	[Fact(DisplayName = "TError cannot be Error: the native error is the single-generic form")]
	public void TError_CannotBeInitializedWith_Error_Type()
	{
		AssertStaticConstructor<Response<string, Error>, ResponseInitializationException>();
		AssertStaticConstructor<ResponseMaybe<string, Error>, ResponseInitializationException>();
	}

	#endregion

	#region Extensions

	[Fact(DisplayName = "The reserved extension keys of each form are its envelope members")]
	public void Extensions_ReservedKeys_AreDefined()
	{
		// Response
		Assert.Equal(3, ResponseConstants.ResponseExtensionsReservedKeys.Count);
		Assert.Contains(ResponseConstants.PayloadPropertyName, ResponseConstants.ResponseExtensionsReservedKeys);
		Assert.Contains(ResponseConstants.ErrorPropertyName, ResponseConstants.ResponseExtensionsReservedKeys);
		Assert.Contains(nameof(Response<>.IsSuccess), ResponseConstants.ResponseExtensionsReservedKeys);
		// ResponseMaybe
		Assert.Equal(4, ResponseConstants.ResponseMaybeExtensionsReservedKeys.Count);
		Assert.Contains(ResponseConstants.PayloadPropertyName, ResponseConstants.ResponseMaybeExtensionsReservedKeys);
		Assert.Contains(ResponseConstants.ErrorPropertyName, ResponseConstants.ResponseMaybeExtensionsReservedKeys);
		Assert.Contains(nameof(ResponseMaybe<>.IsSuccess), ResponseConstants.ResponseMaybeExtensionsReservedKeys);
		Assert.Contains(nameof(ResponseMaybe<>.IsNone), ResponseConstants.ResponseMaybeExtensionsReservedKeys);
	}
	// Every reserved key, per form, written literally so the expectation does not come from the implementation.
	public static TheoryData<string, string> ReservedKeysByForm()
	{
		var data = new TheoryData<string, string>();
		foreach (var form in new[] { "Response", "TypedResponse", "Maybe", "TypedMaybe" })
		{
			foreach (var key in new[] { "IsSuccess", "Payload", "Error" })
				data.Add(form, key);
			if (form is "Maybe" or "TypedMaybe")
				data.Add(form, "IsNone");
		}
		return data;
	}
	static IResponse CreateWithExtension(string form, string key) => form switch
	{
		"Response" => new Response<Unit>(Unit.Value) { Extensions = new() { [key] = "reserved" } },
		"TypedResponse" => new Response<Unit, CustomError>(Unit.Value) { Extensions = new() { [key] = "reserved" } },
		"Maybe" => new ResponseMaybe<Unit>(Unit.Value) { Extensions = new() { [key] = "reserved" } },
		"TypedMaybe" => new ResponseMaybe<Unit, CustomError>(Unit.Value) { Extensions = new() { [key] = "reserved" } },
		_ => throw new ArgumentOutOfRangeException(nameof(form)),
	};
	static IResponse Create(string form) => form switch
	{
		"Response" => new Response<Unit>(Unit.Value),
		"TypedResponse" => new Response<Unit, CustomError>(Unit.Value),
		"Maybe" => new ResponseMaybe<Unit>(Unit.Value),
		"TypedMaybe" => new ResponseMaybe<Unit, CustomError>(Unit.Value),
		_ => throw new ArgumentOutOfRangeException(nameof(form)),
	};
	[Theory(DisplayName = "A reserved key cannot be set as an extension at initialization")]
	[MemberData(nameof(ReservedKeysByForm))]
	public void Extensions_ReservedKey_ThrowsOnInit(string form, string key)
		=> Throws<ReservedKeyExtensionException>(() => CreateWithExtension(form, key));
	[Theory(DisplayName = "A reserved key cannot be added as an extension afterwards")]
	[MemberData(nameof(ReservedKeysByForm))]
	public void Extensions_ReservedKey_ThrowsOnAdd(string form, string key)
		=> Throws<ReservedKeyExtensionException>(() => Create(form).Extensions.Add(key, "reserved"));

	#endregion

	#region Explicit conversions

	[Theory(DisplayName = "A not allowed explicit conversion names the target type and the actual state of the response")]
	[InlineData("Response success to Error")]
	[InlineData("Response error to TSuccess")]
	[InlineData("TypedResponse success to TError")]
	[InlineData("TypedResponse error to TSuccess")]
	[InlineData("Maybe none to TSuccess")]
	[InlineData("Maybe error to TSuccess")]
	[InlineData("Maybe none to Error")]
	[InlineData("Maybe success to Error")]
	[InlineData("Maybe success to None")]
	[InlineData("Maybe error to None")]
	[InlineData("TypedMaybe none to TSuccess")]
	[InlineData("TypedMaybe error to TSuccess")]
	[InlineData("TypedMaybe none to TError")]
	[InlineData("TypedMaybe success to TError")]
	[InlineData("TypedMaybe success to None")]
	[InlineData("TypedMaybe error to None")]
	public void ExplicitConversion_NotAllowed_NamesTargetAndState(string conversion)
	{
		Response<TestPayload> responseSuccess = new TestPayload("test", 123);
		Response<TestPayload> responseError = Error.NotFound();
		Response<TestPayload, CustomError> typedSuccess = new TestPayload("test", 123);
		Response<TestPayload, CustomError> typedError = new CustomError("failure");
		ResponseMaybe<TestPayload> maybeSuccess = new TestPayload("test", 123);
		ResponseMaybe<TestPayload> maybeError = Error.NotFound();
		ResponseMaybe<TestPayload> maybeNone = None.Value;
		ResponseMaybe<TestPayload, CustomError> typedMaybeSuccess = new TestPayload("test", 123);
		ResponseMaybe<TestPayload, CustomError> typedMaybeError = new CustomError("failure");
		ResponseMaybe<TestPayload, CustomError> typedMaybeNone = None.Value;

		(Action convert, Type target, string state) = conversion switch
		{
			"Response success to Error" => ((Action)(() => _ = (Error)responseSuccess), typeof(Error), "success"),
			"Response error to TSuccess" => (() => _ = (TestPayload)responseError, typeof(TestPayload), "error"),
			"TypedResponse success to TError" => (() => _ = (CustomError)typedSuccess, typeof(CustomError), "success"),
			"TypedResponse error to TSuccess" => (() => _ = (TestPayload)typedError, typeof(TestPayload), "error"),
			"Maybe none to TSuccess" => (() => _ = (TestPayload)maybeNone, typeof(TestPayload), "none"),
			"Maybe error to TSuccess" => (() => _ = (TestPayload)maybeError, typeof(TestPayload), "error"),
			"Maybe none to Error" => (() => _ = (Error)maybeNone, typeof(Error), "none"),
			"Maybe success to Error" => (() => _ = (Error)maybeSuccess, typeof(Error), "success"),
			"Maybe success to None" => (() => _ = (None)maybeSuccess, typeof(None), "success"),
			"Maybe error to None" => (() => _ = (None)maybeError, typeof(None), "error"),
			"TypedMaybe none to TSuccess" => (() => _ = (TestPayload)typedMaybeNone, typeof(TestPayload), "none"),
			"TypedMaybe error to TSuccess" => (() => _ = (TestPayload)typedMaybeError, typeof(TestPayload), "error"),
			"TypedMaybe none to TError" => (() => _ = (CustomError)typedMaybeNone, typeof(CustomError), "none"),
			"TypedMaybe success to TError" => (() => _ = (CustomError)typedMaybeSuccess, typeof(CustomError), "success"),
			"TypedMaybe success to None" => (() => _ = (None)typedMaybeSuccess, typeof(None), "success"),
			"TypedMaybe error to None" => (() => _ = (None)typedMaybeError, typeof(None), "error"),
			_ => throw new ArgumentOutOfRangeException(nameof(conversion)),
		};

		var ex = Assert.Throws<InvalidOperationException>(convert);
		Assert.Equal($"Explicit conversion between this response and {target.GetSignature()} is not allowed because this response is {state}", ex.Message);
	}

	#endregion
}
file record CustomError(string Message);