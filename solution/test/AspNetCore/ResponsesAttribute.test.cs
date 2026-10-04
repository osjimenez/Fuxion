using Fuxion.AspNetCore;
using Fuxion.Xunit;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNetCore;

public class AspNetCoreResponsesAttributeTest(ITestOutputHelper output) : BaseTest<AspNetCoreResponsesAttributeTest>(output)
{
	[Fact(DisplayName = "The ASP.NET Core attribute exposes exactly the shared public surface of ResponsesAttribute")]
	public void ResponsesAttribute_MatchesSharedShape()
		=> Assert.Equal(ResponsesAttributeShape.Expected, ResponsesAttributeShape.Describe(typeof(ResponsesAttribute)));
}
