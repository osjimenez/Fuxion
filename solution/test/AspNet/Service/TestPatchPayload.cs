namespace Test.AspNet.Service;

using Fuxion.Union;

/// <summary>
/// Payload used to verify that undefined members are omitted by the ASP.NET serializers.
/// </summary>
public record TestPatchPayload(Undefinable<string> Name, Undefinable<int> Age)
{
	/// <summary>
	/// Payload where only <see cref="Age"/> is defined, so <see cref="Name"/> must be absent from the JSON.
	/// </summary>
	public static TestPatchPayload PartiallyDefined { get; } = new(None.Value, 123);
}
