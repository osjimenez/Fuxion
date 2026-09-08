using Fuxion;
using Fuxion.Net.Http;
using Test.Responses.Shared.Fixtures;

namespace Test.Responses.Shared;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion.Http;
using Fuxion.Xunit;
using Xunit;

/// <summary>
/// The client-side extensions (<c>AsResponseAsync</c> and <see cref="FuxionHttpClient"/>) read a real
/// server's answer identically whichever host produced it.
/// </summary>
public abstract class ClientTests : BaseTest<ClientTests>
{
	/// <summary>The host this test matrix runs against.</summary>
	protected IWireHost Host { get; }

	/// <summary>Initializes the matrix against the given host, logging which one this run is exercising.</summary>
	protected ClientTests(ITestOutputHelper output, IWireHost host) : base(output)
	{
		Host = host;
		Output.WriteLine($"Host: {host.GetType().Name}");
	}

	[Fact(DisplayName = "AsResponseAsync reads a payload")]
	public async Task Client_ReadsPayload()
	{
		var response = await Host.CreateClient().GetAsync(Host.Route("response/payload")).AsResponseAsync<TestPayload>();
		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.Equal(TestPayload.Default, payload);
	}

	[Fact(DisplayName = "AsResponseAsync reads a native error")]
	public async Task Client_ReadsError()
	{
		var response = await Host.CreateClient().GetAsync(Host.Route("response/error-type")).AsResponseAsync<TestPayload>();
		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.NotImplemented, error.Type);
	}

	[Fact(DisplayName = "AsResponseAsync reads a typed error")]
	public async Task Client_ReadsTypedError()
	{
		var response = await Host.CreateClient().GetAsync(Host.Route("response/typed-error")).AsResponseAsync<string, TestBusinessError>();
		IsTrue(response.TryGetValue(out TestBusinessError? business));
		Assert.Equal(TestBusinessError.Default, business);
	}

	[Fact(DisplayName = "AsResponseAsync reads Unit")]
	public async Task Client_ReadsUnit()
	{
		var response = await Host.CreateClient().GetAsync(Host.Route("response/unit")).AsResponseAsync<Unit>();
		IsTrue(response.TryGetValue(out Unit _));
	}

	[Fact(DisplayName = "AsResponseAsync reads None")]
	public async Task Client_ReadsNone()
	{
		var response = await Host.CreateClient().GetAsync(Host.Route("response/none")).AsResponseAsync<Unit>();
		IsTrue(response.IsNone);
	}

	[Fact(DisplayName = "AsResponseAsync streams a file")]
	public async Task Client_ReadsFile()
	{
		var response = await Host.CreateClient().GetAsync(Host.Route("binary/file"), HttpCompletionOption.ResponseHeadersRead).AsResponseAsync<FileContent>();
		IsTrue(response.TryGetValue(out FileContent? file));
		Assert.Equal(TestFile.ContentType, file!.ContentType);
		Assert.Equal(TestFile.Name, file.FileName);
		Assert.Equal(TestFile.Bytes.Length, file.Length);
		Assert.Equal(TestFile.ETag, file.ETag);
		Assert.Equal(TestFile.LastModified, file.LastModified);
		using var memory = new MemoryStream();
		await file.Stream.CopyToAsync(memory);
		Assert.Equal(TestFile.Bytes, memory.ToArray());
	}

	[Fact(DisplayName = "FuxionHttpClient adds the envelope Accept and the server answers with the envelope")]
	public async Task Client_FuxionHttpClient_PreferEnvelope()
	{
		var options = new FuxionHttpClientOptions { PreferEnvelope = true };
		var client = new FuxionHttpClient(Host.CreateClient(), options);

		var request = new HttpRequestMessage(HttpMethod.Get, Host.Route("response/payload"));
		var response = await client.SendAsync<TestPayload>(request);

		// The request is mutated in place by the client before it is sent, so the header survives the send.
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ResponseJson);
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.Json && a.Quality == 0.9);
		IsTrue(response.TryGetValue(out TestPayload? payload));
		Assert.Equal(TestPayload.Default, payload);
	}

	[Fact(DisplayName = "PreferNaming with PreferEnvelope round-trips a snake-cased payload nested in the envelope")]
	public async Task Client_PreferNaming_WithEnvelope_RoundTrips()
	{
		var options = new FuxionHttpClientOptions { PreferNaming = ResponseNaming.Snake, PreferEnvelope = true };
		var client = new FuxionHttpClient(Host.CreateClient(), options);

		var response = await client.GetAsync<TestNamingPayload>(Host.Route("response/naming-payload"));

		IsTrue(response.TryGetValue(out TestNamingPayload? payload));
		Assert.Equal("Ada", payload!.FirstName);
	}

	[Fact(DisplayName = "PreferNaming without PreferEnvelope still round-trips: the un-announced payload stays camel on the wire, and the client reads camel")]
	public async Task Client_PreferNaming_WithoutEnvelope_StillRoundTrips()
	{
		var options = new FuxionHttpClientOptions { PreferNaming = ResponseNaming.Snake };
		var client = new FuxionHttpClient(Host.CreateClient(), options);

		var response = await client.GetAsync<TestNamingPayload>(Host.Route("response/naming-payload"));

		IsTrue(response.TryGetValue(out TestNamingPayload? payload));
		Assert.Equal("Ada", payload!.FirstName);
	}

	[Fact(DisplayName = "PreferNativeErrors makes the server answer with the native error and the client reads it")]
	public async Task PreferNativeErrors_EndToEnd()
	{
		var options = new FuxionHttpClientOptions { PreferNativeErrors = true };
		var client = new FuxionHttpClient(Host.CreateClient(), options);
		var request = new HttpRequestMessage(HttpMethod.Get, Host.Route("response/error-type"));
		var response = await client.SendAsync<TestPayload>(request);
		Assert.Contains(request.Headers.Accept, a => a.MediaType == ResponseMediaTypes.ErrorJson);
		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.NotImplemented, error.Type);

		var raw = new HttpRequestMessage(HttpMethod.Get, Host.Route("response/error-type"));
		foreach (var accept in request.Headers.Accept) raw.Headers.Accept.Add(accept);
		var wire = await client.HttpClient.SendAsync(raw);
		Assert.Equal(ResponseMediaTypes.ErrorJson, wire.Content.Headers.ContentType?.MediaType);
	}
}
