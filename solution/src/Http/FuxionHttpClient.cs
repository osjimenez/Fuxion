namespace Fuxion.Http;

using Fuxion.Union;
using Fuxion.Union.Net.Http;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// An <see cref="HttpClient"/> wrapper that reads Fuxion responses using a negotiated contract.
/// </summary>
/// <remarks>
/// The point of this type is that the serialization contract is declared once, when the client is
/// registered, instead of being repeated at every call site. Reading a Fuxion response requires the
/// same JSON options the server used to write it, so leaving that to each caller is a source of
/// silent mismatches.
/// </remarks>
public sealed class FuxionHttpClient(HttpClient httpClient, IResponseContractResolver contractResolver)
{
	/// <summary>
	/// The underlying client, for requests that do not return a Fuxion response.
	/// </summary>
	public HttpClient HttpClient { get; } = httpClient;

	/// <summary>
	/// Gets the contract currently used against the client base address.
	/// </summary>
	public ValueTask<ResponseContract> GetContractAsync(CancellationToken ct = default)
		=> contractResolver.GetContractAsync(HttpClient.BaseAddress, ct);

	/// <summary>
	/// Sends a request and reads its Fuxion response.
	/// </summary>
	public async Task<ResponseMaybe<TSuccess>> SendAsync<TSuccess>(HttpRequestMessage request, CancellationToken ct = default)
		where TSuccess : notnull
	{
		var contract = await contractResolver.GetContractAsync(request.RequestUri ?? HttpClient.BaseAddress, ct);
		var message = await HttpClient.SendAsync(request, ct);
		return await message.AsResponseAsync<TSuccess>(contract.ResponseOptions, contract.JsonOptions, ct);
	}

	/// <summary>
	/// Sends a request and reads its Fuxion response using a custom error type.
	/// </summary>
	public async Task<ResponseMaybe<TSuccess, TError>> SendAsync<TSuccess, TError>(HttpRequestMessage request, CancellationToken ct = default)
		where TSuccess : notnull
		where TError : notnull
	{
		var contract = await contractResolver.GetContractAsync(request.RequestUri ?? HttpClient.BaseAddress, ct);
		var message = await HttpClient.SendAsync(request, ct);
		return await message.AsResponseAsync<TSuccess, TError>(contract.ResponseOptions, contract.JsonOptions, ct);
	}

	/// <summary>
	/// Performs a GET request and reads its Fuxion response.
	/// </summary>
	public Task<ResponseMaybe<TSuccess>> GetAsync<TSuccess>(string requestUri, CancellationToken ct = default)
		where TSuccess : notnull
		=> SendAsync<TSuccess>(new HttpRequestMessage(HttpMethod.Get, requestUri), ct);

	/// <summary>
	/// Performs a GET request and reads its Fuxion response using a custom error type.
	/// </summary>
	public Task<ResponseMaybe<TSuccess, TError>> GetAsync<TSuccess, TError>(string requestUri, CancellationToken ct = default)
		where TSuccess : notnull
		where TError : notnull
		=> SendAsync<TSuccess, TError>(new HttpRequestMessage(HttpMethod.Get, requestUri), ct);
}
