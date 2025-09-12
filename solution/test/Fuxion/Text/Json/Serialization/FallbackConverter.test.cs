using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Test.Text.Json.Serialization;

public class FallbackConverterTest : BaseTest<FallbackConverterTest>
{
	public FallbackConverterTest(ITestOutputHelper output) : base(output) { }
	[Fact(DisplayName = "FallbackConverter - Serialize")]
	public void FallbackConverter_Serialize()
	{
		try
		{
			try
			{
				Task.Run(() => {
					InvalidProgramException ipex = new("InvalidProgramException message");
					throw ipex;
#pragma warning disable xUnit1031
				}).Wait();
#pragma warning restore xUnit1031
			} catch (Exception ex)
			{
				InvalidOperationException ioex = new("InvalidOperationException message", ex);
				throw ioex;
			}
		} catch (Exception ex)
		{
			var res = ex.SerializeToJson(true);
			Output.WriteLine("Exception serialized JSON:");
			Output.WriteLine(res);
		}
	}
	[Fact(DisplayName = "FallbackConverter - Serialize loop")]
	public void FallbackConverter_Serialize_Loop()
	{
		try
		{
			try
			{
				Task.Run(() =>
				{
					Loop loop = new("Loop name");
					loop.Data = loop;
					LoopException ipex = new("LoopException message")
					{
						Loop = loop
					};
					throw ipex;
#pragma warning disable xUnit1031
				}).Wait();
#pragma warning restore xUnit1031
			} catch (Exception ex)
			{
				InvalidOperationException ioex = new("InvalidOperationException message", ex);
				throw ioex;
			}
		} catch (Exception ex)
		{
			var res = ex.SerializeToJson(true);
			Output.WriteLine("Exception serialized JSON:");
			Output.WriteLine(res);
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