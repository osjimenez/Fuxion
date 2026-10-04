using Fuxion.AspNetCore;
using Fuxion.Xunit;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNetCore;

public class AspNetCoreResponseOptionsAttributeTest(ITestOutputHelper output) : BaseTest<AspNetCoreResponseOptionsAttributeTest>(output)
{
	[Fact(DisplayName = "The ASP.NET Core attribute exposes exactly the shared public surface of ResponseOptionsAttribute")]
	public void ResponseOptionsAttribute_MatchesSharedShape()
		=> Assert.Equal(ResponseOptionsAttributeShape.Expected, ResponseOptionsAttributeShape.Describe(typeof(ResponseOptionsAttribute)));
}
