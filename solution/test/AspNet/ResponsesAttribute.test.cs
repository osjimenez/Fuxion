using Fuxion.AspNet;
using Fuxion.Xunit;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNet;

public class WebApi2ResponsesAttributeTest(ITestOutputHelper output) : BaseTest<WebApi2ResponsesAttributeTest>(output)
{
	[Fact(DisplayName = "The Web API 2 attribute exposes exactly the shared public surface of ResponsesAttribute")]
	public void ResponsesAttribute_MatchesSharedShape()
		=> Assert.Equal(ResponsesAttributeShape.Expected, ResponsesAttributeShape.Describe(typeof(ResponsesAttribute)));
}
