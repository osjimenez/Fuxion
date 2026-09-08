using System.Text.Json;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ResponseMediaTypesTest(ITestOutputHelper output) : BaseTest<ResponseMediaTypesTest>(output)
{
	[Theory(DisplayName = "A media type is matched ignoring parameters and case")]
	[InlineData("application/vnd.fuxion.response+json", true)]
	[InlineData("application/vnd.fuxion.response+json; naming=snake", true)]
	[InlineData("Application/VND.Fuxion.Response+JSON; charset=utf-8", true)]
	[InlineData("application/json", false)]
	[InlineData(null, false)]
	public void Is(string? contentType, bool expected)
		=> Assert.Equal(expected, ResponseMediaTypes.Is(contentType, ResponseMediaTypes.ResponseJson));

	[Theory(DisplayName = "Only the vendor tree is recognized as Fuxion")]
	[InlineData("application/vnd.fuxion.unit+json", true)]
	[InlineData("application/vnd.fuxion.error+json; naming=camel", true)]
	[InlineData("application/problem+json", false)]
	[InlineData("application/json", false)]
	public void IsFuxion(string contentType, bool expected)
		=> Assert.Equal(expected, ResponseMediaTypes.IsFuxion(contentType));

	[Fact(DisplayName = "The naming parameter round-trips through the known policies")]
	public void NamingRoundTrip()
	{
		Assert.Equal(ResponseNaming.Camel, ResponseNaming.FromPolicy(JsonNamingPolicy.CamelCase));
		Assert.Equal(ResponseNaming.Snake, ResponseNaming.FromPolicy(JsonNamingPolicy.SnakeCaseLower));
		Assert.Equal(ResponseNaming.Kebab, ResponseNaming.FromPolicy(JsonNamingPolicy.KebabCaseLower));
		Assert.Equal(ResponseNaming.Pascal, ResponseNaming.FromPolicy(null));

		IsTrue(ResponseNaming.TryGetPolicy(ResponseNaming.Snake, out var snake));
		Assert.Same(JsonNamingPolicy.SnakeCaseLower, snake);
		IsTrue(ResponseNaming.TryGetPolicy(ResponseNaming.Pascal, out var pascal));
		Assert.Null(pascal);
		IsTrue(!ResponseNaming.TryGetPolicy("shouting", out _));
	}

	[Fact(DisplayName = "The naming parameter is read from a content type and written after the media type")]
	public void NamingParameter()
	{
		Assert.Equal(ResponseNaming.Snake, ResponseNaming.GetParameter("application/vnd.fuxion.response+json; naming=snake"));
		Assert.Null(ResponseNaming.GetParameter("application/json"));
		Assert.Null(ResponseNaming.GetParameter(null));

		Assert.Equal("application/vnd.fuxion.response+json; naming=camel",
			ResponseNaming.WithNaming(ResponseMediaTypes.ResponseJson, JsonNamingPolicy.CamelCase));
		Assert.Equal("application/vnd.fuxion.unit+json; naming=pascal",
			ResponseNaming.WithNaming(ResponseMediaTypes.UnitJson, null));
	}

	[Fact(DisplayName = "Applying a naming parameter clones the options once and reuses the clone")]
	public void ApplyIsCachedAndNeverMutates()
	{
		var source = new JsonSerializerOptions(JsonSerializerDefaults.Web);

		var first = ResponseNaming.Apply(source, ResponseNaming.Snake);
		var second = ResponseNaming.Apply(source, ResponseNaming.Snake);

		Assert.Same(JsonNamingPolicy.CamelCase, source.PropertyNamingPolicy); // untouched
		Assert.Same(JsonNamingPolicy.SnakeCaseLower, first.PropertyNamingPolicy);
		Assert.Same(first, second);
		Assert.Same(source, ResponseNaming.Apply(source, ResponseNaming.Camel)); // already matches
		Assert.Same(source, ResponseNaming.Apply(source, null));
		Assert.Same(source, ResponseNaming.Apply(source, "shouting"));
	}

	[Theory(DisplayName = "Every .NET naming policy has a wire value")]
	[InlineData(null, ResponseNaming.Pascal)]
	[InlineData("camel", ResponseNaming.Camel)]
	[InlineData("snake-lower", ResponseNaming.Snake)]
	[InlineData("snake-upper", ResponseNaming.SnakeUpper)]
	[InlineData("kebab-lower", ResponseNaming.Kebab)]
	[InlineData("kebab-upper", ResponseNaming.KebabUpper)]
	public void FromPolicy_CoversDotNet(string? policyName, string expected)
	{
		JsonNamingPolicy? policy = policyName switch
		{
			null => null,
			"camel" => JsonNamingPolicy.CamelCase,
			"snake-lower" => JsonNamingPolicy.SnakeCaseLower,
			"snake-upper" => JsonNamingPolicy.SnakeCaseUpper,
			"kebab-lower" => JsonNamingPolicy.KebabCaseLower,
			_ => JsonNamingPolicy.KebabCaseUpper
		};
		Assert.Equal(expected, ResponseNaming.FromPolicy(policy));
		IsTrue(ResponseNaming.TryGetPolicy(expected, out var back));
		Assert.Same(policy, back);
	}

	[Fact(DisplayName = "A custom policy is announced as custom and cannot be resolved back")]
	public void CustomPolicy_IsAnnouncedAsCustom()
	{
		var custom = new UpperInvariantPolicy();
		Assert.Equal(ResponseNaming.Custom, ResponseNaming.FromPolicy(custom));
		Assert.Equal($"{ResponseMediaTypes.ResponseJson}; naming=custom", ResponseNaming.WithNaming(ResponseMediaTypes.ResponseJson, custom));
		IsTrue(!ResponseNaming.TryGetPolicy(ResponseNaming.Custom, out _));
		IsTrue(!ResponseNaming.IsSupported(ResponseNaming.Custom));
		var source = new JsonSerializerOptions(JsonSerializerDefaults.Web);
		Assert.Same(source, ResponseNaming.Apply(source, ResponseNaming.Custom));
	}

	[Theory(DisplayName = "Only separator namings need transcoding")]
	[InlineData("snake", true)] [InlineData("SNAKE-UPPER", true)] [InlineData("kebab", true)] [InlineData("kebab-upper", true)]
	[InlineData("camel", false)] [InlineData("pascal", false)] [InlineData("custom", false)] [InlineData(null, false)]
	public void NeedsSeparatorTranscoding(string? naming, bool expected) => Assert.Equal(expected, ResponseNaming.NeedsSeparatorTranscoding(naming));

	sealed class UpperInvariantPolicy : JsonNamingPolicy { public override string ConvertName(string name) => name.ToUpperInvariant(); }
}
