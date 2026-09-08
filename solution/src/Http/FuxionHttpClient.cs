using Fuxion.Net.Http;

namespace Fuxion.Http;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// An <see cref="HttpClient"/> wrapper that asks for the response shape it prefers and reads Fuxion
/// responses with the JSON options declared once at registration.
/// </summary>
public sealed class FuxionHttpClient(HttpClient httpClient, FuxionHttpClientOptions options)
{
	/// <summary>The underlying client, for requests that do not return a Fuxion response.</summary>
	public HttpClient HttpClient { get; } = httpClient;

	/// <summary>What this client asks for and how it reads.</summary>
	public FuxionHttpClientOptions Options { get; } = options;

	/// <summary>Sends a request and reads its Fuxion response.</summary>
	public async Task<ResponseMaybe<TSuccess>> SendAsync<TSuccess>(HttpRequestMessage request, CancellationToken ct = default)
		where TSuccess : notnull
	{
		ApplyAccept(request);
		var message = await HttpClient.SendAsync(request, CompletionFor<TSuccess>(), ct);
		return await message.AsResponseAsync<TSuccess>(Options.JsonOptions, ct);
	}

	/// <summary>Sends a request and reads its Fuxion response using a custom error type.</summary>
	public async Task<ResponseMaybe<TSuccess, TError>> SendAsync<TSuccess, TError>(HttpRequestMessage request, CancellationToken ct = default)
		where TSuccess : notnull
		where TError : notnull
	{
		ApplyAccept(request);
		var message = await HttpClient.SendAsync(request, CompletionFor<TSuccess>(), ct);
		return await message.AsResponseAsync<TSuccess, TError>(Options.JsonOptions, ct);
	}

	public Task<ResponseMaybe<TSuccess>> GetAsync<TSuccess>(string requestUri, CancellationToken ct = default)
		where TSuccess : notnull
		=> SendAsync<TSuccess>(new HttpRequestMessage(HttpMethod.Get, requestUri), ct);

	public Task<ResponseMaybe<TSuccess, TError>> GetAsync<TSuccess, TError>(string requestUri, CancellationToken ct = default)
		where TSuccess : notnull
		where TError : notnull
		=> SendAsync<TSuccess, TError>(new HttpRequestMessage(HttpMethod.Get, requestUri), ct);

	// An Accept set explicitly by the caller always wins over the client preferences.
	void ApplyAccept(HttpRequestMessage request)
	{
		if (request.Headers.Accept.Count == 0)
			request.Headers.TryAddWithoutValidation("Accept", Options.BuildAccept());
	}

	// A binary payload must reach the caller as a stream, so HttpClient must return as soon as the
	// headers arrive instead of buffering the whole body. A byte[] is materialized by the reader anyway,
	// so it keeps the default ResponseContentRead: that preserves HttpClient.MaxResponseContentBufferSize,
	// which ResponseHeadersRead would bypass. JSON bodies also keep the default behaviour.
	static HttpCompletionOption CompletionFor<TSuccess>()
		=> BinaryPayload.IsBinaryType(typeof(TSuccess)) && typeof(TSuccess) != typeof(byte[])
			? HttpCompletionOption.ResponseHeadersRead
			: HttpCompletionOption.ResponseContentRead;
}
