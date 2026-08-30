namespace Fuxion.Http;

using Fuxion.Union;
using System;
using System.Text.Json;

/// <summary>
/// The serialization contract used to talk to a given server.
/// </summary>
/// <remarks>
/// This is the single place where the naming policy and the response shape live together, because both
/// sides of a Fuxion response must agree on them: the envelope property names are resolved through
/// <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> at read time, so a client reading a body
/// with different options than the server used to write it would not match any property.
/// </remarks>
public sealed record ResponseContract
{
	/// <summary>
	/// The JSON options used to read bodies. Defaults to <see cref="JsonSerializerDefaults.Web"/>,
	/// matching what an ASP.NET Core server uses unless the application configured something else.
	/// </summary>
	public JsonSerializerOptions JsonOptions { get; init; } = new(JsonSerializerDefaults.Web);

	/// <summary>
	/// The response shape expected from the server.
	/// </summary>
	public ResponseSerializerOptions ResponseOptions { get; init; } = new();

	/// <summary>
	/// Whether this contract was agreed with the server or assumed from local defaults.
	/// </summary>
	/// <remarks>
	/// A consumer can inspect this to tell a negotiated contract apart from a fallback one without
	/// having to read the logs.
	/// </remarks>
	public bool IsNegotiated { get; init; }

	internal static ResponseContract Default { get; } = new();
}
