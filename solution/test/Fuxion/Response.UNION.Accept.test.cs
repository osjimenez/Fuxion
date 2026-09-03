using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class ResponseAcceptTest(ITestOutputHelper output) : BaseTest<ResponseAcceptTest>(output)
{
	static readonly ResponseOptions Defaults = new();

	[Theory(DisplayName = "An Accept that names no Fuxion type leaves the scope defaults untouched")]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("*/*")]
	[InlineData("application/json")]
	[InlineData("text/html, application/xhtml+xml, */*;q=0.8")]
	public void NoFuxionType_KeepsDefaults(string? accept)
		=> Assert.Same(Defaults, ResponseAccept.Apply(Defaults, accept));

	[Fact(DisplayName = "Asking for the envelope turns the full response on even when the scope default is off")]
	public void Envelope()
	{
		var result = ResponseAccept.Apply(Defaults, "application/vnd.fuxion.response+json, application/json;q=0.9");

		IsTrue(result.SerializeFullResponses);
		IsTrue(result.SerializeErrorAsProblemDetails); // untouched
		IsTrue(!Defaults.SerializeFullResponses);       // never mutated
	}

	[Fact(DisplayName = "Asking for native errors turns problem details off")]
	public void NativeErrors()
	{
		var result = ResponseAccept.Apply(Defaults, "application/vnd.fuxion.error+json");
		IsTrue(!result.SerializeErrorAsProblemDetails);
	}

	[Fact(DisplayName = "Asking for problem+json turns problem details on")]
	public void ProblemDetails()
	{
		var scope = new ResponseOptions { SerializeErrorAsProblemDetails = false };
		var result = ResponseAccept.Apply(scope, "application/problem+json");
		IsTrue(result.SerializeErrorAsProblemDetails);
	}

	[Fact(DisplayName = "Native errors win over problem+json when both are listed")]
	public void NativeWinsOverProblem()
	{
		var result = ResponseAccept.Apply(Defaults, "application/problem+json, application/vnd.fuxion.error+json");
		IsTrue(!result.SerializeErrorAsProblemDetails);
	}

	[Fact(DisplayName = "A media type with q=0 is not acceptable and is ignored")]
	public void QualityZeroIsIgnored()
		=> Assert.Same(Defaults, ResponseAccept.Apply(Defaults, "application/vnd.fuxion.response+json;q=0, application/json"));

	[Fact(DisplayName = "Accept never touches StrictNone")]
	public void StrictNoneUntouched()
	{
		var scope = new ResponseOptions { StrictNone = true };
		IsTrue(ResponseAccept.Apply(scope, "application/vnd.fuxion.response+json").StrictNone);
	}
}
