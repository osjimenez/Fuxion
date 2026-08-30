namespace Fuxion.Http;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

/// <summary>
/// Registration of Fuxion HTTP clients.
/// </summary>
public static class FuxionHttpClientServiceExtensions
{
	/// <summary>
	/// Registers a <see cref="FuxionHttpClient"/> backed by <see cref="IHttpClientFactory"/>.
	/// </summary>
	/// <remarks>
	/// The serialization contract is declared here once and applied to every response read through the
	/// client, so a consumer configuring a naming policy does not have to repeat it at each call.
	/// </remarks>
	/// <param name="services">The service collection.</param>
	/// <param name="configureClient">Configures the underlying <see cref="HttpClient"/>.</param>
	/// <param name="configureContract">Configures the serialization contract expected from the server.</param>
	public static IHttpClientBuilder AddFuxionHttpClient(
		this IServiceCollection services,
		Action<HttpClient> configureClient,
		Action<ResponseContractBuilder>? configureContract = null)
	{
		var builder = new ResponseContractBuilder();
		configureContract?.Invoke(builder);
		var contract = builder.Build();

		return services.AddHttpClient<FuxionHttpClient>(configureClient)
			.AddTypedClientFactory(contract);
	}

	static IHttpClientBuilder AddTypedClientFactory(this IHttpClientBuilder builder, ResponseContract contract)
	{
		builder.Services.AddTransient<IResponseContractResolver>(_ => new StaticResponseContractResolver(contract));
		return builder;
	}
}
