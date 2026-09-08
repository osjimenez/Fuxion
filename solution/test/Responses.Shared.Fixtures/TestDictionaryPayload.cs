using System.Collections.Generic;

namespace Test.Responses.Shared.Fixtures;

/// <summary>Payload with a dictionary member, used to verify that a naming policy never renames its keys.</summary>
public record TestDictionaryPayload(string FirstName, Dictionary<string, int> Tags);
