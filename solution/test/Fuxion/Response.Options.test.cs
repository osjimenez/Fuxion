using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

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

	[Fact(DisplayName = "Layers merge in order and only override what they set")]
	public void MergeLayers()
	{
		var global = new ResponseOptions { SerializeFullResponses = false, SerializeErrorAsProblemDetails = true, StrictNone = false };
		var controller = new ResponseOptionsLayer { SerializeFullResponses = true };
		var action = new ResponseOptionsLayer { StrictNone = true, SerializeFullResponses = null };

		var merged = global.Merge([controller, action]);

		IsTrue(merged.SerializeFullResponses);          // from controller, untouched by action (null)
		IsTrue(merged.SerializeErrorAsProblemDetails);  // inherited from global
		IsTrue(merged.StrictNone);                      // from action
		IsTrue(!global.SerializeFullResponses);          // source never mutated
		Assert.Equal(global, global.Merge([]));         // no layers: equal by value
	}
}
