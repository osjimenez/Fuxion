using System;
using System.Net.Http;
using System.Text.Json;
using Fuxion;
using Test.Responses.Shared;

namespace Test.AspNet;

/// <summary>Maps the shared wire-contract matrix onto the in-memory Web API 2 host.</summary>
sealed class AspNetWireHost : IWireHost
{
	public HttpClient CreateClient(Action<ResponseOptions>? configure = null, JsonNamingPolicy? namingPolicy = null)
		=> AspNetHost.Create(configure, namingPolicy is null ? null : new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = namingPolicy });

	// AspNet's routes are already the logical ones: there is no minimal/controller split for this host.
	// The one exception is "naming/malformed": Web API 2's "naming/echo" never validates ModelState, so
	// "naming/validate" (the endpoint that does) stands in for that logical route on this host.
	public string Route(string logical) => logical == Routes.NamingMalformed ? "naming/validate" : logical;
}
