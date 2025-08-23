namespace Fuxion.Test;

public class ResponseTest(ITestOutputHelper output) : BaseTest<ResponseTest>(output)
{
	public Response GetSuccess() => Response.Success();
	public Response GetSuccessMessage() => Response.SuccessMessage("message");
	public Response GetSuccessMessageWithExtensions() => Response.SuccessMessage("message", [("Extension", 123.456)]);
	public Response<int> GetSuccessWithPayload() => Response.SuccessPayload(123);
	public Response<int> GetSuccessWithPayloadAndExtensions()
		=> Response.SuccessPayload(123, extensions: [("Extension", 123.456)]);
	public Response GetError() => Response.ErrorMessage("message");
	public Response<int> GetErrorWithPayload() => Response.ErrorPayload(123, "message");
	public Response GetNotFound() => Response.NotFound("message");
	public Response<int> GetNotFoundWithPayload() => Response.NotFound("message", 123);
	public Response<int> GetNotFoundWithPayloadAndExtensions() => Response.NotFound("message", 123, extensions: [("Extension", 123.456)]);
	public CustomError GetCustomError() => Response.Custom("message", "customData");
	[Fact]
	public void ImplicitConversion()
	{
		Assert.Equal(123, OkInt());
		Assert.Equal(456, ErrorInt());
		int val = OkResponse();
		Assert.Equal(123, val);

		return;
		int OkInt() => Response.SuccessPayload(123);
		int ErrorInt() => Response.ErrorPayload<int>(456, "message");
		Response<int> OkResponse() => 123;
	}
	[Fact]
	public void Success()
	{
		var s1 = Response.Success();

		Assert.Null(s1.Message);
		Assert.Throws<InvalidOperationException>(() => s1.AsPayload<string?>().Payload);
		var s2 = Response.SuccessMessage("message");
		Assert.NotNull(s2.Message);
		var s3 = Response.SuccessPayload(payload: "payload");
		Assert.Null(s3.Message);
		Assert.NotNull(s3.Payload);
		var s4 = Response.SuccessPayload(123);
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
			Response.Success(),
			Response.SuccessMessage("message", [("Extension", 123.456)]),
			Response.SuccessPayload(123, "message"),
			Response.NotFound("message"),
			Response.ErrorPayload(new Payload("Bob", 25), "message", extensions: [("Extension", 123.456)])
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
		Response<int> Do()
		{
			try
			{
				return Do2();
			} catch (Exception ex)
			{
				PrintVariable(ex.SerializeToJson(true));
				return Response.Critical("Exception", exception: ex).AsPayload<int>();
			}
		}
		Response<int> Do2()
		{
			return Response.SuccessPayload(dic[1]);
		}
	}
	[Fact]
	public void AsPayload()
	{
		Throws<InvalidOperationException>(()=>Do(1));
		Throws<InvalidOperationException>(() => Do(2));
		int res = Do(3);
		Assert.Equal(123, res);
		IsTrue(Do(4).IsError);

		IsTrue(Do2(1).IsSuccess);
		IsTrue(Do2(2).IsSuccess);
		int res2 = Do2(3).AsPayload<int>();
		Assert.Equal(123, res2);
		IsTrue(Do2(4).IsError);


		Response<int> Do(int val)
			=> val switch
			{
				1 => Response.Success().AsPayload<int>(),
				2 => Response.SuccessMessage("message").AsPayload<int>(),
				3 => Response.SuccessPayload(123),
				var _ => Response.Critical("").AsPayload<int>()
			};
		Response Do2(int val)
			=> val switch
			{
				1 => Response.Success(),
				2 => Response.SuccessMessage("message"),
				3 => Response.SuccessPayload(123),
				var _ => Response.Critical("")
			};
	}
}

file record Payload(string Name, int Age);

public class CustomError(string message, string customData) : Response(false, message)
{
	public string CustomData { get; } = customData;
}

file static class CustomErrorExtensions
{
	extension(Response)
	{
		public static CustomError Custom(string message, string customData) => new(message, customData);
	}
}