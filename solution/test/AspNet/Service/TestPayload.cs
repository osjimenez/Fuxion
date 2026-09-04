namespace Test.AspNet.Service;

public record TestPayload(string Name, int Age)
{
	public static TestPayload Default { get; } = new TestPayload("test", 123);
}
