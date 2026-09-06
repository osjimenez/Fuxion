using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.AspNetCore;
using Fuxion.Text.Json.Serialization;
using Fuxion.Union;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Test.Responses.Shared;

namespace Test.AspNetCore.Service;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		// Add services to the container.

		// Configurar la serialización JSON
		builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
		{
			options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			//options.SerializerOptions.Converters.Add(new ExceptionConverter());
		});

		builder.Services.AddResponses(opts =>
		{
			// Defaults (kept for clarity)
			opts.SerializeFullResponses = false;
			opts.SerializeErrorAsProblemDetails = true;
			opts.StrictNone = false;
			opts.BusinessErrorStatus = e => e is TestForeignError ? HttpStatusCode.TooManyRequests : null;
			opts.RequestNamingMaxBodySize = 64 * 1024;
		});

		builder.Services.AddControllers(options => options.UseResponses())
			 .AddJsonOptions(options =>
			 {
				 options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				 //options.JsonSerializerOptions.Converters.Add(new ExceptionConverter());
			 });

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		var responses = app.MapGroup(string.Empty)
			 .UseResponses();

		// Example of overriding options at group level (demo)
		var sub = responses.MapGroup("sub").UseResponses(meta => meta.SerializeFullResponses = true);
		sub.MapGet("payload", Response<TestPayload> () => TestPayload.Default);
		sub.MapGet("none", ResponseMaybe<Unit> () => None.Value);

		// A third scope level: overrides StrictNone alone, inheriting SerializeFullResponses from "sub".
		var deep = sub.MapGroup("deep").UseResponses(meta => meta.StrictNone = true);
		deep.MapGet("payload", Response<TestPayload> () => TestPayload.Default);
		deep.MapGet("none", ResponseMaybe<Unit> () => None.Value);

		// Controllers get the response-side wire contract from AddControllers(o => o.UseResponses()) above,
		// not from this endpoint convention (MVC ignores endpoint filter factories). The chain here exists to
		// prove that RequestNamingEndpoint.Apply skips controller endpoints (they already have per-request
		// naming support via ResponseNamingInputFormatter), so request bodies are never transcoded twice.
		app.MapControllers().UseResponses();
		responses.MapEndpointsForAssembly(typeof(Program).Assembly);

		app.Run();
	}
}