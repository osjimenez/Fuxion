namespace Test.Responses.Shared;

// No IHttpStatusError here: this error travels only with the status the service assigns it
// through ResponseOptions.BusinessErrorStatus.
/// <summary>A typed business error that declares no HTTP status of its own; the service classifies it.</summary>
public record TestForeignError(string Code);
