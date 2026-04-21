using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Text.Json.Serialization;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ResponseTest(ITestOutputHelper output) : BaseTest<ResponseTest>(output)
{
	public IResponse GetSuccess() => Response.Get.Success();
	public IResponse GetSuccessMessage() => Response.Get.SuccessMessage("message");
	public IResponse GetSuccessMessageWithExtensions() => Response.Get.SuccessMessage("message", [("Extension", 123.456)]);
	public IResponse<int> GetSuccessWithPayload() => Response.Get.SuccessPayload(123);
	public IResponse<int> GetSuccessWithPayloadAndExtensions()
		=> Response.Get.SuccessPayload(123, extensions: [("Extension", 123.456)]);
	public IResponse GetError() => Response.Get.ErrorMessage("message");
	public IResponse<int> GetErrorWithPayload() => Response.Get.ErrorPayload(123, "message");
	public IResponse GetNotFound() => Response.Get.NotFound("message");
	public IResponse<int> GetNotFoundWithPayload() => Response.Get.NotFound("message", 123);
	public IResponse<int> GetNotFoundWithPayloadAndExtensions() => Response.Get.NotFound("message", 123, extensions: [("Extension", 123.456)]);
	public IResponse GetCustomError() => Response.Get.Custom("message", "customData");
	//[Fact]
	//public void ImplicitConversion()
	//{
	//	Assert.Equal(123, OkInt());
	//	Assert.Equal(456, ErrorInt());
	//	int val = OkResponse();
	//	Assert.Equal(123, val);

	//	return;
	//	int OkInt() => ResponseExt.Get.SuccessPayload(123);
	//	int ErrorInt() => ResponseExt.Get.ErrorPayload<int>(456, "message");
	//	IResponse<int> OkResponse() => ResponseExt.Get.SuccessPayload(123);
	//}
	[Fact]
	public void Success()
	{
		var s1 = Response.Get.Success();

		Assert.Null(s1.Message);
		Assert.Throws<InvalidOperationException>(() => s1.AsPayload<string?>().Payload);
		var s2 = Response.Get.SuccessMessage("message");
		Assert.NotNull(s2.Message);
		var s3 = Response.Get.SuccessPayload(payload: "payload");
		Assert.Null(s3.Message);
		Assert.NotNull(s3.Payload);
		var s4 = Response.Get.SuccessPayload(123);
		Assert.Null(s4.Message);
		Assert.Equal(123, s4.Payload);
	}
	[Fact]
	public void Serialize()
	{
		PrintVariable(GetSuccess().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetSuccessMessage().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetSuccessMessageWithExtensions().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetSuccessWithPayload().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetSuccessWithPayloadAndExtensions().Fx.Json.Serialize(true).Payload);

		PrintVariable(GetError().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetErrorWithPayload().Fx.Json.Serialize(true).Payload);

		PrintVariable(GetNotFound().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetNotFoundWithPayload().Fx.Json.Serialize(true).Payload);
		PrintVariable(GetNotFoundWithPayloadAndExtensions().Fx.Json.Serialize(true).Payload);

		PrintVariable(GetCustomError().Fx.Json.Serialize(true).Payload);

		var results = new[]
		{
			Response.Get.Success(),
			Response.Get.SuccessMessage("message", [("Extension", 123.456)]),
			Response.Get.SuccessPayload(123, "message"),
			Response.Get.NotFound("message"),
			Response.Get.ErrorPayload(new Payload("Bob", 25), "message", extensions: [("Extension", 123.456)])
		};
		PrintVariable(results.Fx.Json.Serialize(true).Payload);
		PrintVariable(results.CombineResponses().Fx.Json.Serialize(true).Payload);

		IsTrue(GetNotFound().IsNotFound);
		IsTrue(GetNotFound().IsErrorType(ErrorType.NotFound));
		IsTrue(GetNotFoundWithPayload().IsNotFound);
	}

	[Fact]
	public void SerializeCustom()
	{
		PrintVariable(GetCustomError().Fx.Json.Serialize(true).PayloadOrFallback(r=>throw r.Exception ?? new Exception("NOOOOOO")));
	}
	[Fact]
	public void Exception()
	{
		Dictionary<int, int> dic = new();
		var res = Do();
		PrintVariable(res.Fx.Json.Serialize(true).Payload);

		return;
		IResponse<int> Do()
		{
			try
			{
				return Do2();
			} catch (Exception ex)
			{
				PrintVariable(ex.Fx.Json.Serialize(true).Payload);
				return Response.Get.Critical("Exception", exception: ex).AsPayload<int>();
			}
		}
		IResponse<int> Do2()
		{
			return Response.Get.SuccessPayload(dic[1]);
		}
	}
	[Fact]
	public void AsPayload()
	{
		Throws<InvalidOperationException>(()=>Do(1));
		Throws<InvalidOperationException>(() => Do(2));
		int res = Do(3).PayloadOrDefault();
		Assert.Equal(123, res);
		IsTrue(Do(4).IsError);

		IsTrue(Do2(1).IsSuccess);
		IsTrue(Do2(2).IsSuccess);
		int res2 = Do2(3).AsPayload<int>().PayloadOrDefault();
		Assert.Equal(123, res2);
		IsTrue(Do2(4).IsError);


		IResponse<int> Do(int val)
			=> val switch
			{
				1 => Response.Get.Success().AsPayload<int>(),
				2 => Response.Get.SuccessMessage("message").AsPayload<int>(),
				3 => Response.Get.SuccessPayload(123),
				var _ => Response.Get.Critical("").AsPayload<int>()
			};
		IResponse Do2(int val)
			=> val switch
			{
				1 => Response.Get.Success(),
				2 => Response.Get.SuccessMessage("message"),
				3 => Response.Get.SuccessPayload(123),
				var _ => Response.Get.Critical("")
			};
	}
}

file record Payload(string Name, int Age);

public class CustomError(string message, string customData) : ResponseBase(false, message)
{
	public string CustomData { get; } = customData;
}

file static class CustomErrorExtensions
{
	extension(ResponseExtensions.ResponseGetExtensions me)
	{
		public CustomError Custom(string message, string customData) => new(message, customData);
	}
}