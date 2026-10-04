using Fuxion.AspNet;
using Fuxion.Xunit;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNet;

public class WebApi2ResponseOptionsAttributeTest(ITestOutputHelper output) : BaseTest<WebApi2ResponseOptionsAttributeTest>(output)
{
	[Fact(DisplayName = "The Web API 2 attribute exposes exactly the shared public surface of ResponseOptionsAttribute")]
	public void ResponseOptionsAttribute_MatchesSharedShape()
		=> Assert.Equal(ResponseOptionsAttributeShape.Expected, ResponseOptionsAttributeShape.Describe(typeof(ResponseOptionsAttribute)));
}
