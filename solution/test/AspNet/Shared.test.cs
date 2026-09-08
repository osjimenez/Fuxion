using System;
using System.Net.Http;
using System.Text.Json;
using Fuxion;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNet;

/// <summary>Maps the shared wire-contract matrix onto the in-memory Web API 2 host.</summary>
sealed class AspNetWireHost : IWireHost
{
	public HttpClient CreateClient(Action<ResponseOptions>? configure = null, JsonNamingPolicy? namingPolicy = null)
		=> AspNetHost.Create(configure, namingPolicy is null ? null : new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = namingPolicy });

	// AspNet's routes are already the logical ones: there is no minimal/controller split for this host.
	// The one exception is "naming/malformed": Web API 2's "naming/echo" never validates ModelState, so
	// "naming/validate" (the endpoint that does) stands in for that logical route on this host.
	public string Route(string logical) => logical == "naming/malformed" ? "naming/validate" : logical;
}

public sealed class SharedWireContractTest(ITestOutputHelper output) : WireContractTests(output, new AspNetWireHost()) { }

public sealed class SharedBinaryWireTest(ITestOutputHelper output) : BinaryWireTests(output, new AspNetWireHost()) { }

public sealed class SharedRequestNamingTest(ITestOutputHelper output) : RequestNamingTests(output, new AspNetWireHost())
{
	// Web API 2's own MediaTypeHeaderValue parser may reject a Content-Type with a duplicated parameter
	// before the request ever reaches our formatter (415), or it may let it through for the formatter to
	// reject itself (400): either outcome proves a duplicated naming parameter is never silently accepted.
	protected override bool DuplicatedNamingMayBe415 => true;
}

public sealed class SharedUndefinableTest(ITestOutputHelper output) : UndefinableTests(output, new AspNetWireHost()) { }

public sealed class SharedClientTest(ITestOutputHelper output) : ClientTests(output, new AspNetWireHost()) { }
