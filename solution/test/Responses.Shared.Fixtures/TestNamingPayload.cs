namespace Test.Responses.Shared.Fixtures;

/// <summary>Multi-word members so that snake_case and kebab-case actually differ from camelCase.</summary>
public record TestNamingPayload(string FirstName, int Age);
