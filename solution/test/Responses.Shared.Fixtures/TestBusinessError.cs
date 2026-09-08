using System.Net;
using Fuxion;

namespace Test.Responses.Shared.Fixtures;

/// <summary>A typed business error that declares its own HTTP status through <see cref="IHttpStatusError"/>.</summary>
public record TestBusinessError(string Code, string Reason) : IHttpStatusError
{
	/// <summary>The canonical instance every endpoint returns.</summary>
	public static TestBusinessError Default { get; } = new TestBusinessError("stock", "not enough stock");

	/// <inheritdoc />
	public HttpStatusCode Status => HttpStatusCode.Conflict;
}
