using System.Text.Json;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class UnitTest(ITestOutputHelper output) : BaseTest<UnitTest>(output)
{
	[Theory(DisplayName = "Unit is read from null or from an empty object")]
	[InlineData("null")]
	[InlineData("{}")]
	[InlineData("{ }")]
	public void Read_NullOrEmptyObject(string json)
		=> Assert.Equal(Unit.Value, JsonSerializer.Deserialize<Unit>(json));

	[Theory(DisplayName = "Anything else is not a Unit")]
	[InlineData("""{"a":1}""")]
	[InlineData("1")]
	[InlineData("\"unit\"")]
	[InlineData("[]")]
	public void Read_AnythingElse_Throws(string json)
		=> Throws<JsonException>(() => JsonSerializer.Deserialize<Unit>(json));

	// The converter writes null; the "{}" body of a Unit response is chosen by the wire mapper, not here.
	[Fact(DisplayName = "Unit is written as null")]
	public void Write_Null()
		=> Assert.Equal("null", JsonSerializer.Serialize(Unit.Value));
}
