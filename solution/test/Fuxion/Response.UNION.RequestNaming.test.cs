using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class JsonNamingTranscoderTest(ITestOutputHelper output) : BaseTest<JsonNamingTranscoderTest>(output)
{
	[Theory(DisplayName = "Separated names lose their separators and capitalize the following letter")]
	[InlineData("first_name", "FirstName")]
	[InlineData("first-name", "FirstName")]
	[InlineData("shipping_address_line_2", "ShippingAddressLine2")]
	[InlineData("firstName", "firstName")]
	[InlineData("Age", "Age")]
	[InlineData("_leading", "Leading")]
	public void NormalizePropertyName(string input, string expected)
		=> Assert.Equal(expected, JsonNamingTranscoder.NormalizePropertyName(input));

	[Fact(DisplayName = "Only property names change: values, numbers and nesting are preserved")]
	public void Transcode_PreservesEverythingButNames()
	{
		const string json = """{"first_name":"snake_case_value","age":36,"price":1.50,"tags":["a_b",{"inner_key":true}],"nothing":null,"big":12345678901234567890}""";

		var result = JsonNamingTranscoder.Transcode(json);

		Assert.Equal("""{"FirstName":"snake_case_value","age":36,"price":1.50,"tags":["a_b",{"InnerKey":true}],"nothing":null,"big":12345678901234567890}""", result);
	}

	[Fact(DisplayName = "Camel or Pascal input passes through unchanged")]
	public void Transcode_IsIdentityForCamel()
	{
		const string json = """{"firstName":"Ada","Age":36}""";
		Assert.Equal(json, JsonNamingTranscoder.Transcode(json));
	}

	[Fact(DisplayName = "An array root is supported")]
	public void Transcode_ArrayRoot()
		=> Assert.Equal("""[{"FirstName":"Ada"},{"FirstName":"Grace"}]""", JsonNamingTranscoder.Transcode("""[{"first_name":"Ada"},{"first_name":"Grace"}]"""));

	[Fact(DisplayName = "Dictionary keys are rewritten too: a documented limitation of textual transcoding")]
	public void Transcode_RewritesDictionaryKeysToo()
		=> Assert.Equal("""{"tags":{"User123":true}}""", JsonNamingTranscoder.Transcode("""{"tags":{"user_123":true}}"""));
}
