using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Fuxion;

namespace Test.Responses.Shared;

// Request builders shared by the wire tests of every host.
public static class WireRequests
{
	public static HttpRequestMessage Get(string url, string? accept = null, string? range = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
		if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
		return request;
	}

	public static StringContent Body(string json, string? naming, string mediaType = "application/json")
	{
		var content = new StringContent(json, Encoding.UTF8, mediaType);
		if (naming is not null)
			content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(ResponseMediaTypes.NamingParameter, naming));
		return content;
	}
}
