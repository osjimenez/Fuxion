namespace Test.Responses.Shared.Fixtures;

/// <summary>A plain success payload shared by every wire-contract test host.</summary>
public record TestPayload(string Name, int Age)
{
	/// <summary>The canonical instance every endpoint returns.</summary>
	public static TestPayload Default { get; } = new TestPayload("test", 123);
}
