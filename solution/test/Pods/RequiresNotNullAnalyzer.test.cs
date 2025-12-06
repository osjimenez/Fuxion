using Fuxion;
using Fuxion.Analyzers;
using Fuxion.Pods;
// ? NUEVO: Acceso a los atributos

#pragma warning disable CS8602 // Allow CS8602 for testing purposes - we want FX001 instead

namespace Test.Pods;

/// <summary>
/// Test file to verify that the FX001 analyzer works correctly with both
/// traditional classes and extension members.
/// </summary>
public class RequiresNotNullAnalyzerTest
{
	private IUriKeyResolver resolver = null!;

	public void TestTraditionalClass()
	{
		// ? Should compile: non-nullable access
		var obj = new TestClass { Value = "test" };
		var value = obj.Value;

		// ? Should fail with FX001: nullable access without ?
		TestClass? nullable = null;
		var x = nullable?.Value; // ERROR FX001 expected here
		nullable.Method(); // ERROR FX001 expected here

	}

	public void TestExtensionMember()
	{
		// ? Should compile: non-nullable access
		string str = "hello";
		str.Fx.Pod.BuildUriKeyPod(resolver);

		// ? Should fail with FX001: nullable access without ?.
		string? nullableStr = null;
		nullableStr?.Fx.Pod.BuildUriKeyPod(resolver); // ERROR FX001 expected here

		// ? Should compile: nullable with null-conditional operator
		nullableStr?.Fx.Pod.BuildUriKeyPod(resolver);
	}
}

// Traditional class for testing
public class TestClass
{
	private string? _value;

	[RequiresNotNull]
	public string Value
	{
		get
		{
			if (_value is null) throw new ArgumentNullException();
			return _value;
		}
		set => _value = value;
	}
	//[RequiresNotNull]
	public void Method() { }
}
