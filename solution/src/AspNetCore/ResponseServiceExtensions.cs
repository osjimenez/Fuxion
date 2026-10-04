using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json.Serialization.Metadata;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Fuxion.AspNetCore;

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
	/// <para>
	/// It also registers <see cref="RequestNamingInputFormatterSetup"/> as an <see cref="IPostConfigureOptions{TOptions}"/>,
	/// which replaces the stock <c>SystemTextJsonInputFormatter</c> MVC installs for <c>MvcOptions</c> with
	/// <see cref="RequestNamingInputFormatter"/>, so a controller action's request body honors the same
	/// per-request <c>naming</c> Content-Type parameter (spec §3) as minimal APIs, whichever order
	/// <see cref="AddResponses"/> and <c>AddControllers</c> are called in. <see cref="IPostConfigureOptions{TOptions}"/>
	/// is required rather than a plain <see cref="IConfigureOptions{TOptions}"/>: MVC's own
	/// <c>MvcCoreMvcOptionsSetup</c> (which adds the stock formatter) also runs as an <see cref="IConfigureOptions{TOptions}"/>,
	/// so only a post-configure step is guaranteed to run after it and see the stock formatter to remove.
	/// </para>
	/// </remarks>
	public static IServiceCollection AddResponses(this IServiceCollection services, Action<ResponseOptions>? configure = null)
	{
		if (configure != null)
				services.Configure(configure);

		services.Configure<HttpJsonOptions>(options =>
				options.SerializerOptions.TypeInfoResolver = ResolveWithOmission(options.SerializerOptions.TypeInfoResolver));
		services.Configure<MvcJsonOptions>(options =>
				options.JsonSerializerOptions.TypeInfoResolver = ResolveWithOmission(options.JsonSerializerOptions.TypeInfoResolver));

		services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<MvcOptions>, RequestNamingInputFormatterSetup>());

		return services;
	}

	// The existing resolver is composed instead of replaced, so a source generated context configured by the
	// application keeps working.
	static IJsonTypeInfoResolver ResolveWithOmission(IJsonTypeInfoResolver? current)
		=> (current ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(UndefinableJsonTypeInfo.OmitUndefined);
}
#pragma warning restore CS1591
