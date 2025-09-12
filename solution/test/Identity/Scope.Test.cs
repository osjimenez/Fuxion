using Fuxion.Identity.Test.Dao;
using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Identity.Test;

public class ScopeTest(ITestOutputHelper helper) : BaseTest<ScopeTest>(helper)
{
	[Fact(DisplayName = "Scope - Null discriminator validation")]
	public void Validate_WrongName() => Assert.False(new ScopeDao("", "", null!, ScopePropagation.ToMe).IsValid()); //Assert.IsTrue(new Scope(null, ScopePropagation.ToMe).IsValid());
}