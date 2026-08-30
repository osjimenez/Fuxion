namespace Fuxion.Http;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Resolves the <see cref="ResponseContract"/> to use against a given server.
/// </summary>
/// <remarks>
/// This is the extension point for contract negotiation. The default implementation is static: it
/// always returns the locally configured contract. A future implementation can ask the server which
/// naming policy and response shape it uses, cache the answer per authority, and fall back to the
/// local contract when the server does not support negotiation.
/// </remarks>
public interface IResponseContractResolver
{
	/// <summary>
	/// Gets the contract to use for a request targeting <paramref name="requestUri"/>.
	/// </summary>
	/// <remarks>
	/// Implementations resolving the contract remotely must cache per authority, because a single
	/// client can talk to several servers and a contract from one of them must never be applied to
	/// another. They must also avoid resolving once per request.
	/// </remarks>
	ValueTask<ResponseContract> GetContractAsync(Uri? requestUri, CancellationToken ct = default);
}

/// <summary>
/// An <see cref="IResponseContractResolver"/> that always returns the same locally configured contract.
/// </summary>
sealed class StaticResponseContractResolver(ResponseContract contract) : IResponseContractResolver
{
	public ValueTask<ResponseContract> GetContractAsync(Uri? requestUri, CancellationToken ct = default)
		=> new(contract);
}
