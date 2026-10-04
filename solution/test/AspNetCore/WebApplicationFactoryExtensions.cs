using System.Net.Http;
using Fuxion;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.Service;

namespace Test.AspNetCore;

static class WebApplicationFactoryExtensions
{
	// A client for a server whose response shape the test chooses. Only the three shape flags are applied, on top of
	// the host's own configuration: BusinessErrorStatus and the rest stay as Program.cs sets them, which a copy of
	// a fresh ResponseOptions would silently reset.
	public static HttpClient CreateClient(this WebApplicationFactory<Program> factory, ResponseOptions shape)
		=> factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Configure<ResponseOptions>(o =>
		{
			o.SerializeFullResponses = shape.SerializeFullResponses;
			o.SerializeErrorAsProblemDetails = shape.SerializeErrorAsProblemDetails;
			o.StrictNone = shape.StrictNone;
		}))).CreateClient();
}
