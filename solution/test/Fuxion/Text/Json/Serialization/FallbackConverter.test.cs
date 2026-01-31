using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
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
			var res = ex.Fx.Json.Serialize(true).Payload;
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
					LoopException ipex = new("LoopException message")
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
			var res = ex.Fx.Json.Serialize(true).Payload;
			Output.WriteLine("Exception serialized JSON:");
			Output.WriteLine(res ?? "null");
		}
	}
}

public class LoopException : Exception
{
	public LoopException(string message) : base(message) { }
	public LoopException(string message, Exception innerException) : base(message, innerException) { }
	public Loop? Loop { get; init; }
}
public record Loop(string Name)
{
	public Loop? Data { get; set; }
}