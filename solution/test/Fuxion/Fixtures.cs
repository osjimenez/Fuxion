namespace Test.Fuxion;

// The records several Test.Fuxion files share. Test.Fuxion does not reference Test.Responses.Shared.Fixtures (a binding
// decision), so it keeps its own; a test that pins specific JSON member names keeps its own specific record.
sealed record TestPayload(string Name, int Age)
{
	public static TestPayload Default { get; } = new("test", 123);
}

sealed record TestError(string Code);
