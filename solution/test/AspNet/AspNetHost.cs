namespace Test.AspNet.Union;

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web.Http;
using Fuxion.AspNet;
using Fuxion.Union;
using Test.Responses.Shared;

/// <summary>Full Web API 2 pipeline in memory: HttpServer is an HttpMessageHandler, so no sockets, no IIS, no OWIN.</summary>
public static class AspNetHost
{
	public static HttpClient Create(Action<ResponseOptions>? configure = null, JsonSerializerOptions? jsonOptions = null, JsonFormatterScope scope = JsonFormatterScope.FuxionTypes)
	{
		var config = new HttpConfiguration();
		config.MapHttpAttributeRoutes();
		config.UseResponses(o =>
		{
			// The default for every test host: a foreign error (no IHttpStatusError of its own) is
			// service-classified; the caller's own configure runs after and can still override it.
			// net472's System.Net.HttpStatusCode has no TooManyRequests member, hence the numeric cast.
			o.BusinessErrorStatus = e => e is TestForeignError ? (HttpStatusCode)429 : null;
			configure?.Invoke(o);
		}, jsonOptions, scope,
		// The naming-parameter tests exercise plain (non-Fuxion) payloads on purpose, to prove naming is a
		// wire-level concern independent of the union contract; opt them in so scope FuxionTypes still
		// routes them through the formatter that understands the request's own naming parameter.
		useSystemTextJsonFor: t => t == typeof(TestNamingPayload) || t == typeof(TestDictionaryPayload));
		config.EnsureInitialized();
		return new HttpClient(new HttpServer(config)) { BaseAddress = new Uri("http://localhost/") };
	}
}
