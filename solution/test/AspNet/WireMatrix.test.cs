using Test.Responses.Shared;
using Xunit;

namespace Test.AspNet;

public sealed class WebApi2WireContractTest(ITestOutputHelper output) : WireContractTests(output, new AspNetWireHost()) { }

public sealed class WebApi2BinaryWireTest(ITestOutputHelper output) : BinaryWireTests(output, new AspNetWireHost()) { }

public sealed class WebApi2RequestNamingTest(ITestOutputHelper output) : RequestNamingTests(output, new AspNetWireHost())
{
	// Web API 2's own MediaTypeHeaderValue parser may reject a Content-Type with a duplicated parameter
	// before the request ever reaches our formatter (415), or it may let it through for the formatter to
	// reject itself (400): either outcome proves a duplicated naming parameter is never silently accepted.
	protected override bool DuplicatedNamingMayBe415 => true;
}

public sealed class WebApi2UndefinableTest(ITestOutputHelper output) : UndefinableTests(output, new AspNetWireHost()) { }

public sealed class WebApi2ClientTest(ITestOutputHelper output) : ClientTests(output, new AspNetWireHost()) { }
