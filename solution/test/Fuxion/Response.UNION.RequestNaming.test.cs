using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class JsonNamingTranscoderTest(ITestOutputHelper output) : BaseTest<JsonNamingTranscoderTest>(output)
{
	record Inner(string LastName);
	record Body(string FirstName, Dictionary<string, int> Tags, JsonElement Free, List<Inner> Items, Undefinable<string> NickName, Inner? Maybe);

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

	[Fact(DisplayName = "Type-guided transcoding renames properties only, never dictionary keys or free JSON")]
	public void Typed_RenamesPropertiesOnly()
	{
		var json = """{"first_name":"Ada","tags":{"my_tag":1,"other-tag":2},"free":{"keep_me":{"and_me":1}},"items":[{"last_name":"L"}],"nick_name":"a","maybe":{"last_name":"M"},"unknown_key":true}""";
		var result = Encoding.UTF8.GetString(JsonNamingTranscoder.Transcode(Encoding.UTF8.GetBytes(json), typeof(Body), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
		var node = JsonNode.Parse(result)!.AsObject();
		Assert.Equal("Ada", (string?)node["firstName"]);
		Assert.Equal(1, (int?)node["tags"]!["my_tag"]);
		Assert.Equal(2, (int?)node["tags"]!["other-tag"]);
		Assert.Equal(1, (int?)node["free"]!["keep_me"]!["and_me"]);
		Assert.Equal("L", (string?)node["items"]![0]!["lastName"]);
		Assert.Equal("a", (string?)node["nickName"]);
		Assert.Equal("M", (string?)node["maybe"]!["lastName"]);
		Assert.True((bool?)node["unknown_key"]);
		var bound = JsonSerializer.Deserialize<Body>(result, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
		Assert.Equal("Ada", bound.FirstName);
		Assert.Equal(new[] { "my_tag", "other-tag" }, bound.Tags.Keys.OrderBy(k => k));
	}

	[Fact(DisplayName = "Upper-case separators are handled too")]
	public void Typed_Upper()
	{
		var result = Encoding.UTF8.GetString(JsonNamingTranscoder.Transcode(Encoding.UTF8.GetBytes("""{"FIRST_NAME":"Ada","TAGS":{"MY_TAG":1}}"""), typeof(Body), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
		var node = JsonNode.Parse(result)!.AsObject();
		Assert.Equal("Ada", (string?)node["firstName"]);
		Assert.Equal(1, (int?)node["tags"]!["MY_TAG"]);
	}

	[Fact(DisplayName = "A malformed body still throws JsonException")]
	public void Typed_Malformed_Throws()
		=> Assert.ThrowsAny<JsonException>(() => JsonNamingTranscoder.Transcode(Encoding.UTF8.GetBytes("{ not json"), typeof(Body), new JsonSerializerOptions(JsonSerializerDefaults.Web)));

	[Fact(DisplayName = "Unicode keys and escaped values survive transcoding: textual yields Pascal, typed yields the property name")]
	public void Unicode_Survives()
	{
		const string json = """{"nombre_día":"añó 😀","emoji_😀":"x","tags":{"clave_ñ":1}}""";
		var textual = JsonNode.Parse(JsonNamingTranscoder.Transcode(json))!.AsObject();
		Assert.Equal("añó 😀", (string?)textual["NombreDía"]);
		Assert.Equal("x", (string?)textual["Emoji😀"]);
		var typed = JsonNode.Parse(Encoding.UTF8.GetString(JsonNamingTranscoder.Transcode(Encoding.UTF8.GetBytes(json), typeof(Unicode), new JsonSerializerOptions(JsonSerializerDefaults.Web))))!.AsObject();
		Assert.Equal("añó 😀", (string?)typed["nombreDía"]);
		Assert.Equal(1, (int?)typed["tags"]!["clave_ñ"]);
	}
	record Unicode(string NombreDía, Dictionary<string, int> Tags);
}
