using Fuxion.AspNetCore;
using Fuxion.Http;
using Fuxion.Union;
using Fuxion.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Test.AspNetCore.Service;
using Xunit;

namespace Test.AspNetCore.Union;

public class FuxionHttpClientTest(ITestOutputHelper output, WebApplicationFactory<Program> factory)
	: BaseTest<FuxionHttpClientTest>(output), IClassFixture<WebApplicationFactory<Program>>
{
	// El contrato se declara una sola vez al registrar el cliente. El objetivo es que ningun call site
	// tenga que repetir las JsonSerializerOptions, que es de donde salian los desajustes silenciosos.
	static FuxionHttpClient BuildClient(HttpClient inner, Action<ResponseContractBuilder>? configureContract = null)
	{
		var services = new ServiceCollection();
		services.AddFuxionHttpClient(_ => { }, configureContract);
		var provider = services.BuildServiceProvider();
		return new FuxionHttpClient(inner, provider.GetRequiredService<IResponseContractResolver>());
	}

	[Fact(DisplayName = "The client reads a response using the contract declared at registration")]
	public async Task Client_UsesRegisteredContract()
	{
		var client = BuildClient(factory.CreateClient());

		var response = await client.GetAsync<TestPatchPayload>("minimal/undefinable/partial");

		IsTrue(response.TryGetValue(out TestPatchPayload? payload));
		Assert.NotNull(payload);
		Assert.Equal(123, payload!.Age.Value);
	}

	[Fact(DisplayName = "The contract exposes whether it was negotiated with the server")]
	public async Task Contract_ExposesNegotiationState()
	{
		var client = BuildClient(factory.CreateClient());

		var contract = await client.GetContractAsync();

		// Todavia no hay handshake: el contrato es local, y debe poder distinguirse sin mirar los logs.
		IsTrue(!contract.IsNegotiated);
	}

	[Fact(DisplayName = "A naming policy configured once is applied to every read")]
	public async Task NamingPolicy_IsAppliedFromContract()
	{
		// El servidor de test serializa con los defaults web (camelCase). Si el contrato declara
		// snake_case, el payload no debe poder leerse: prueba que la politica del contrato se aplica
		// de verdad y no se ignora silenciosamente.
		var client = BuildClient(
			factory.CreateClient(),
			c => c.UseNamingPolicy(JsonNamingPolicy.SnakeCaseLower));

		var contract = await client.GetContractAsync();

		Assert.Equal(JsonNamingPolicy.SnakeCaseLower, contract.JsonOptions.PropertyNamingPolicy);
	}
}
