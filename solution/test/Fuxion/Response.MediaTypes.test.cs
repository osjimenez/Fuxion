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

	[Theory(DisplayName = "A content type is JSON when it is application/json, text/json or any +json type, with or without parameters")]
	[InlineData("application/json", true)]
	[InlineData("text/json", true)]
	[InlineData("application/problem+json", true)]
	[InlineData("application/vnd.fuxion.response+json", true)]
	[InlineData("Application/JSON; charset=utf-8; naming=snake", true)]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("text/plain", false)]
	[InlineData("application/octet-stream", false)]
	[InlineData("not a media type", false)]
	public void IsJson(string? contentType, bool expected)
		=> Assert.Equal(expected, ResponseMediaTypes.IsJson(contentType));
}
