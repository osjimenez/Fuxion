using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Test.Fuxion.Text.Json.Serialization;

public class FallbackConverterTest(ITestOutputHelper output) : BaseTest<FallbackConverterTest>(output)
{
	[Fact(DisplayName = "FallbackConverter - Serialize")]
	public async Task FallbackConverter_Serialize()
	{
		try
		{
			try
			{
				await Task.Run(() => {
					InvalidProgramException ipex = new("InvalidProgramException message");
					throw ipex;
				}, TestContext.Current.CancellationToken);
			} catch (Exception ex)
			{
				InvalidOperationException ioex = new("InvalidOperationException message", ex);
				throw ioex;
			}
		} catch (Exception ex)
		{
			var res = ex.Fx.Json.Serialize(true).SuccessOrThrow();
			Output.WriteLine("Exception serialized JSON:");
			Output.WriteLine(res ?? "null");
		}
	}
	[Fact(DisplayName = "FallbackConverter - Serialize loop")]
	public async Task FallbackConverter_Serialize_Loop()
	{
		try
		{
			try
			{
				await Task.Run(() =>
				{
					Loop loop = new("Loop name");
					loop.Data = loop;
					LoopException ipex = new("LoopException message\r\nNew line")
					{
						Loop = loop
					};
					throw ipex;
				}, TestContext.Current.CancellationToken);
			} catch (Exception ex)
			{
				InvalidOperationException ioex = new("InvalidOperationException message", ex);
				throw ioex;
			}
		} catch (Exception ex)
		{
			var json = ex.Fx.Json.Serialize(true).SuccessOrThrow();
			AssertJson(json, [
				new(["Message"], "InvalidOperationException message"),
            new(["InnerException", "Message"], "LoopException message\r\nNew line"),
				new(["InnerException", "Loop", "Name"], "Loop name"),
				new(["InnerException", "Loop", "Data"], null),
			]);
		}
	}
}

file class LoopException : Exception
{
	public LoopException(string message) : base(message) { }
	public LoopException(string message, Exception innerException) : base(message, innerException) { }
	public Loop? Loop { get; init; }
}
file record Loop(string Name)
{
	public Loop? Data { get; set; }
}