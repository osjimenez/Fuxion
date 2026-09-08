using Fuxion;

namespace Test.Responses.Shared;

using System;
using System.Net.Http;
using System.Text.Json;

/// <summary>What a host must provide for the shared wire matrix: a client against an in-memory server and its routes.</summary>
public interface IWireHost
{
	/// <summary>Builds an <see cref="HttpClient"/> against the host's in-memory server.</summary>
	/// <param name="configure">Global <see cref="ResponseOptions"/> for this client (null = the host's defaults).</param>
	/// <param name="namingPolicy">The server's property naming policy for this client (null = the host's default, camelCase).</param>
	HttpClient CreateClient(Action<ResponseOptions>? configure = null, JsonNamingPolicy? namingPolicy = null);

	/// <summary>Maps a logical route ("response/payload", "binary/file", "naming/echo", "undefinable/partial"...) to the host's URL.</summary>
	string Route(string logical);
}
