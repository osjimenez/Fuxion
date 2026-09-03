using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class ResponseOptionsTest(ITestOutputHelper output) : BaseTest<ResponseOptionsTest>(output)
{
	[Fact(DisplayName = "The server defaults are payload-only, problem+json errors and non-strict None")]
	public void Defaults()
	{
		var options = new ResponseOptions();

		IsTrue(!options.SerializeFullResponses);
		IsTrue(options.SerializeErrorAsProblemDetails);
		IsTrue(!options.StrictNone);
	}

	[Fact(DisplayName = "Options are a record with value equality so scopes can be compared and cached")]
	public void ValueEquality()
	{
		Assert.Equal(new ResponseOptions { StrictNone = true }, new ResponseOptions { StrictNone = true });
		Assert.NotEqual(new ResponseOptions(), new ResponseOptions { SerializeFullResponses = true });
	}
}
