namespace Fuxion.AspNetCore;

using Fuxion.Union;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Text.Json.Serialization.Metadata;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class ResponseServiceExtensions
{
	 /// <summary>
	 /// Register global response options.
	 /// </summary>
	 /// <remarks>
	 /// This also makes the ASP.NET Core serializers omit undefined <see cref="Undefinable{TValue}"/> properties,
	 /// so an endpoint produces the same payload as <c>Fx.Json</c> and honors the JSON Merge Patch (RFC 7396)
	 /// convention of representing an untouched member by its absence.
	 /// </remarks>
	 public static IServiceCollection AddResponses(this IServiceCollection services, Action<ResponseOptions>? configure = null)
	 {
		  if (configure != null)
				services.Configure(configure);

		  services.Configure<HttpJsonOptions>(options =>
				options.SerializerOptions.TypeInfoResolver = ResolveWithOmission(options.SerializerOptions.TypeInfoResolver));
		  services.Configure<MvcJsonOptions>(options =>
				options.JsonSerializerOptions.TypeInfoResolver = ResolveWithOmission(options.JsonSerializerOptions.TypeInfoResolver));

		  services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, ResponseNamingStartupFilter>());

		  return services;
	 }

	 // The existing resolver is composed instead of replaced, so a source generated context configured by the
	 // application keeps working.
	 static IJsonTypeInfoResolver ResolveWithOmission(IJsonTypeInfoResolver? current)
		  => (current ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(UndefinableJsonTypeInfo.OmitUndefined);
}
#pragma warning restore CS1591
