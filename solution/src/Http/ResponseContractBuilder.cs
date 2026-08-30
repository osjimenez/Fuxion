namespace Fuxion.Http;

using Fuxion.Union;
using System;
using System.Text.Json;

/// <summary>
/// Builds the <see cref="ResponseContract"/> used by a Fuxion HTTP client.
/// </summary>
public sealed class ResponseContractBuilder
{
	JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
	readonly ResponseSerializerOptions responseOptions = new();

	/// <summary>
	/// Sets the JSON options used to read response bodies.
	/// </summary>
	/// <remarks>
	/// These must match the options the server serializes with, including the naming policy, because
	/// the response envelope property names are resolved through it.
	/// </remarks>
	public ResponseContractBuilder UseJsonOptions(JsonSerializerOptions options)
	{
		jsonOptions = options ?? throw new ArgumentNullException(nameof(options));
		return this;
	}

	/// <summary>
	/// Sets the naming policy used by the server, keeping the remaining web defaults.
	/// </summary>
	public ResponseContractBuilder UseNamingPolicy(JsonNamingPolicy? namingPolicy)
	{
		jsonOptions = new JsonSerializerOptions(jsonOptions) { PropertyNamingPolicy = namingPolicy };
		return this;
	}

	/// <summary>
	/// Configures the response shape expected from the server.
	/// </summary>
	public ResponseContractBuilder UseResponseOptions(Action<ResponseSerializerOptions> configure)
	{
		configure(responseOptions);
		return this;
	}

	internal ResponseContract Build()
		=> new()
		{
			JsonOptions = jsonOptions,
			ResponseOptions = responseOptions
		};
}
