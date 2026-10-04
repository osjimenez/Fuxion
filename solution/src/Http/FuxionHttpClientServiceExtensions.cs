using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Fuxion.Http;

/// <summary>Registration of Fuxion HTTP clients.</summary>
public static class FuxionHttpClientServiceExtensions
{
	/// <summary>
	/// Registers a <see cref="FuxionHttpClient"/> backed by <see cref="IHttpClientFactory"/>. The options
	/// are declared here once and applied to every request and every read through the client.
	/// </summary>
	public static IHttpClientBuilder AddFuxionHttpClient(
		this IServiceCollection services,
		Action<HttpClient> configureClient,
		Action<FuxionHttpClientOptions>? configure = null)
	{
		var options = new FuxionHttpClientOptions();
		configure?.Invoke(options);

		return services
			.AddHttpClient(nameof(FuxionHttpClient), configureClient)
			.AddTypedClient((httpClient, _) => new FuxionHttpClient(httpClient, options));
	}
}
