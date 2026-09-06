using System;
using System.Net.Http;
using System.Text.Json;
using Fuxion.Union;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;
using Test.Responses.Shared;
using Xunit;

namespace Test.AspNetCore.Union;

/// <summary>
/// Maps the shared wire-contract matrix onto a <see cref="WebApplicationFactory{Program}"/>, under either
/// the minimal-API routes ("minimal/...") or their controller counterparts ("controller/...").
/// </summary>
sealed class AspNetCoreWireHost(WebApplicationFactory<Program> factory, string prefix) : IWireHost
{
	public HttpClient CreateClient(Action<ResponseOptions>? configure = null, JsonNamingPolicy? namingPolicy = null)
		=> factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
		{
			if (configure is not null) s.Configure(configure);
			if (namingPolicy is not null)
			{
				// Minimal APIs and MVC controllers each read their own JSON options; override both so the
				// naming policy applies whichever routing style this host instance exercises. Only the naming
				// policy is reassigned, on the same SerializerOptions instance Program.cs already configured,
				// so any other setting (e.g. the Undefinable-omitting resolver) survives untouched.
				s.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = namingPolicy);
				s.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => o.JsonSerializerOptions.PropertyNamingPolicy = namingPolicy);
			}
		})).CreateClient();

	// Both minimal APIs and [ApiController] controllers validate the body on the "echo" endpoint itself:
	// there is no separate "validate" endpoint on this host, unlike Web API 2.
	public string Route(string logical) => logical == "naming/malformed" ? $"{prefix}/naming/echo" : $"{prefix}/{logical}";
}

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
