using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.AspNetCore;
using Fuxion.Http;
using Fuxion.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Test.Aot.App;

// The responses from one end to the other in an application published with native AOT: minimal APIs that return them,
// and FuxionHttpClient that reads them back over HTTP. It exits with 1 if anything does not arrive as it left.
var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
// The context of the application goes before AddResponses(), which chains FuxionJsonContext after it
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
builder.Services.AddResponses();

var app = builder.Build();
var api = app.MapGroup(string.Empty).UseResponses();
api.MapGet("order", Response<Order> () => new Order("Ana", 30));
api.MapGet("missing", Response<Order> () => Error.NotFound("missing"));
api.MapGet("none", ResponseMaybe<Order> () => None.Value);
api.MapGet("unit", Response<Unit> () => Unit.Value);
api.MapGet("business", Response<Order, OrderError> () => new OrderError("E1"));
api.MapGet("boom", Response<Order> () => Error.InternalServerError("boom", exception: new InvalidOperationException("inner")));

await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.TypeInfoResolverChain.Add(AppJsonContext.Default);
jsonOptions.TypeInfoResolverChain.Add(FuxionJsonContext.Default);
using var http = new HttpClient { BaseAddress = new Uri(address) };
var client = new FuxionHttpClient(http, new FuxionHttpClientOptions { JsonOptions = jsonOptions });

var failures = new List<string>();
void Check(string name, bool passed)
{
	Console.WriteLine($"{(passed ? "ok  " : "FAIL")} {name}");
	if (!passed) failures.Add(name);
}

var order = await client.GetAsync<Order>("order");
Check("a payload", order.TryGetValue(out Order? read) && read == new Order("Ana", 30));

var missing = await client.GetAsync<Order>("missing");
Check("an error, as problem details", missing.TryGetValue(out Error notFound) && notFound.Type is HttpStatusCode.NotFound && notFound.Message == "missing");

var nativeClient = new FuxionHttpClient(http, new FuxionHttpClientOptions { JsonOptions = jsonOptions, PreferNativeErrors = true });
var nativeMissing = await nativeClient.GetAsync<Order>("missing");
Check("an error, native", nativeMissing.TryGetValue(out Error nativeNotFound) && nativeNotFound.Type is HttpStatusCode.NotFound && nativeNotFound.Message == "missing");

var none = await client.GetAsync<Order>("none");
Check("none", none.IsNone);

var unit = await client.GetAsync<Unit>("unit");
Check("unit", unit.IsSuccess);

var business = await client.GetAsync<Order, OrderError>("business");
Check("a business error", business.TryGetValue(out OrderError? businessError) && businessError.Code == "E1");

var boom = await client.GetAsync<Order>("boom");
Check("an error with its exception", boom.TryGetValue(out Error error) && error.Message == "boom" && error.Exception?.Message == "inner");

await app.StopAsync();
Console.WriteLine(failures.Count == 0 ? "All the responses arrived as they left." : $"{failures.Count} failed: {string.Join(", ", failures)}");
return failures.Count == 0 ? 0 : 1;

namespace Test.Aot.App
{
	public record Order(string Name, int Age);
	public record OrderError(string Code);

	[JsonSerializable(typeof(Response<Order>))]
	[JsonSerializable(typeof(ResponseMaybe<Order>))]
	[JsonSerializable(typeof(Response<Unit>))]
	[JsonSerializable(typeof(ResponseMaybe<Unit>))]
	[JsonSerializable(typeof(Response<Order, OrderError>))]
	[JsonSerializable(typeof(ResponseMaybe<Order, OrderError>))]
	[JsonSerializable(typeof(Order))]
	[JsonSerializable(typeof(OrderError))]
	partial class AppJsonContext : JsonSerializerContext;
}
