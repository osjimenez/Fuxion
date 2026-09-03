using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.AspNetCore;
using Fuxion.Text.Json.Serialization;
using Fuxion.Union;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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

		app.MapControllers();
		responses.MapEndpointsForAssembly(typeof(Program).Assembly);

		app.Run();
	}
}