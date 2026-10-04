using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.AspNetCore.Service;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNetCore;

public sealed class MinimalWireContractTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: WireContractTests(output, new AspNetCoreWireHost(factory, "minimal")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class ControllerWireContractTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: WireContractTests(output, new AspNetCoreWireHost(factory, "controller")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class MinimalBinaryWireTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: BinaryWireTests(output, new AspNetCoreWireHost(factory, "minimal")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class ControllerBinaryWireTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: BinaryWireTests(output, new AspNetCoreWireHost(factory, "controller")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class MinimalRequestNamingTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: RequestNamingTests(output, new AspNetCoreWireHost(factory, "minimal")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class ControllerRequestNamingTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: RequestNamingTests(output, new AspNetCoreWireHost(factory, "controller")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class MinimalUndefinableTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: UndefinableTests(output, new AspNetCoreWireHost(factory, "minimal")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class ControllerUndefinableTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: UndefinableTests(output, new AspNetCoreWireHost(factory, "controller")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class MinimalClientTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: ClientTests(output, new AspNetCoreWireHost(factory, "minimal")), IClassFixture<WebApplicationFactory<Program>>
{ }

public sealed class ControllerClientTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: ClientTests(output, new AspNetCoreWireHost(factory, "controller")), IClassFixture<WebApplicationFactory<Program>>
{ }
