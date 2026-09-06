namespace Test.AspNet.Service;

/// <summary>A plain (non-Fuxion) payload, used to verify that scope-selected serialization does not affect it.</summary>
public record PlainPoco(string Name, int Age);
