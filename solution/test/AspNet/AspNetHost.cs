namespace Test.AspNet.Union;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Web.Http;
using Fuxion.AspNet;
using Fuxion.Union;

/// <summary>Full Web API 2 pipeline in memory: HttpServer is an HttpMessageHandler, so no sockets, no IIS, no OWIN.</summary>
public static class AspNetHost
{
	public static HttpClient Create(Action<ResponseOptions>? configure = null, JsonSerializerOptions? jsonOptions = null, bool replaceJsonFormatter = true)
	{
		var config = new HttpConfiguration();
		config.MapHttpAttributeRoutes();
		config.UseResponses(configure, jsonOptions, replaceJsonFormatter);
		config.EnsureInitialized();
		return new HttpClient(new HttpServer(config)) { BaseAddress = new Uri("http://localhost/") };
	}
}
