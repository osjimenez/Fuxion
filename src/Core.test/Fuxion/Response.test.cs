namespace Fuxion.Test;

public class ResponseTest(ITestOutputHelper output) : BaseTest<ResponseTest>(output)
{
	public IResponse GetSuccess() => Response.Get.Success();
	public IResponse GetSuccessMessage() => Response.Get.SuccessMessage("message");
	public IResponse GetSuccessMessageWithExtensions() => Response.Get.SuccessMessage("message", ("Extension", 123.456));
	public IResponse GetSuccessWithPayload() => Response.Get.SuccessPayload(123);
	public IResponse GetSuccessWithPayloadAndExtensions()
		=> Response.Get.SuccessPayload(123, extensions: ("Extension", 123.456));
	public IResponse GetError() => Response.Get.Error("message");
	public IResponse GetErrorWithPayload() => Response.Get.Error("message", 123);
	public IResponse GetNotFound() => Response.Get.Error.NotFound("message");
	public IResponse GetNotFoundWithPayload() => Response.Get.Error.NotFound("message", 123);
	public IResponse GetNotFoundWithPayloadAndExtensions() => Response.Get.Error.NotFound("message", 123, extensions: ("Extension", 123.456));
	public IResponse GetCustomError() => Response.Get.Custom("message", "customData");
	//[Fact]
	//public void ImplicitConversion()
	//{
	//	Assert.Equal(123, OkInt());
	//	Assert.Equal(456, ErrorInt());
	//	int val = OkResponse();
	//	Assert.Equal(123, val);

	//	return;
	//	int OkInt() => Response.Get.SuccessPayload(123);
	//	int ErrorInt() => Response.Get.Error("message", 456);
	//	Response<int> OkResponse() => 123;
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
		PrintVariable(GetSuccess().SerializeToJson(true));
		PrintVariable(GetSuccessMessage().SerializeToJson(true));
		PrintVariable(GetSuccessMessageWithExtensions().SerializeToJson(true));
		PrintVariable(GetSuccessWithPayload().SerializeToJson(true));
		PrintVariable(GetSuccessWithPayloadAndExtensions().SerializeToJson(true));

		PrintVariable(GetError().SerializeToJson(true));
		PrintVariable(GetErrorWithPayload().SerializeToJson(true));

		PrintVariable(GetNotFound().SerializeToJson(true));
		PrintVariable(GetNotFoundWithPayload().SerializeToJson(true));
		PrintVariable(GetNotFoundWithPayloadAndExtensions().SerializeToJson(true));

		PrintVariable(GetCustomError().SerializeToJson(true));

		var results = new[]
		{
			Response.Get.Success(),
			Response.Get.SuccessMessage("message",("Extension", 123.456)),
			Response.Get.SuccessPayload(123, "message"),
			Response.Get.Error.NotFound("message"),
			Response.Get.Error("message", new Payload("Bob", 25), extensions: ("Extension", 123.456))
		};
		PrintVariable(results.SerializeToJson(true));
		PrintVariable(results.CombineResponses().SerializeToJson(true));

		IsTrue(GetNotFound().IsNotFound());
		IsTrue(GetNotFound().IsErrorType(ErrorType.NotFound));
		IsTrue(GetNotFoundWithPayload().IsNotFound());
	}
	[Fact]
	public void Exception()
	{
		Dictionary<int, int> dic = new();
		var res = Do();
		PrintVariable(res.SerializeToJson(true));

		return;
		IResponse<int> Do()
		{
			try
			{
				return Do2();
			} catch (Exception ex)
			{
				PrintVariable(ex.SerializeToJson(true));
				return Response.Get.Error.Critical("Exception", exception: ex).AsPayload<int>();
			}
		}
		IResponse<int> Do2()
		{
			return Response.Get.SuccessPayload(dic[1]);
		}
	}
}

file record Payload(string Name, int Age);

file class CustomError(string message, string customData) : Response(false, message)
{
	public string CustomData { get; } = customData;
}

file static class CustomErrorExtensions
{
	public static CustomError Custom(this IResponseFactory _, string message, string customData) => new(message, customData);
}