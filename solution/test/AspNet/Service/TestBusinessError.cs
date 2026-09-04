namespace Test.AspNet.Service;

public record TestBusinessError(string Code, string Reason)
{
	public static TestBusinessError Default { get; } = new TestBusinessError("stock", "not enough stock");
}
