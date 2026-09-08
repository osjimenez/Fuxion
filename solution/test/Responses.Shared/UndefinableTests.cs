using Fuxion;

namespace Test.Responses.Shared;

using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fuxion.Xunit;
using Xunit;

/// <summary>
/// An undefined member must be absent from the payload (the JSON Merge Patch, RFC 7396, convention),
/// instead of being written with a marker object, and an absent member must bind back as undefined.
/// </summary>
public abstract class UndefinableTests : BaseTest<UndefinableTests>
{
	/// <summary>The host this test matrix runs against.</summary>
	protected IWireHost Host { get; }

	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected UndefinableTests(ITestOutputHelper output, IWireHost host) : base(output)
	{
		Host = host;
		Output.WriteLine($"Host: {host.GetType().Name}");
	}

	[Fact(DisplayName = "An undefined member is omitted from the payload")]
	public async Task UndefinedMember_IsOmitted()
	{
		var res = await Host.CreateClient().GetAsync(Host.Route("undefinable/partial"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var body = await res.Content.ReadAsStringAsync();
		// The raw body is asserted on purpose: deserializing would yield an undefined value both when the
		// property is omitted and when the marker object is used, so it could not tell them apart.
		var json = JsonNode.Parse(body)!.AsObject();
		Assert.False(json.ContainsKey("name"), "The undefined member must be absent from the payload");
		Assert.DoesNotContain(UndefinableConverterFactory.UndefinedMarkerPropertyName, body);
		Assert.Equal(123, (int?)json["age"]);
	}

	[Fact(DisplayName = "An absent member is bound as undefined")]
	public async Task AbsentMember_IsBoundAsUndefined()
	{
		var res = await Host.CreateClient().PostAsync(Host.Route("undefinable/echo"), new StringContent("""{ "age": 7 }""", System.Text.Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!.AsObject();
		Assert.False((bool)json["nameDefined"]!);
		Assert.True((bool)json["ageDefined"]!);
	}
}
