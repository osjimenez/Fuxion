using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseHttpActionResultExtensions
{
	extension(IResponse me)
	{
		/// <summary>Deferred: options (scope + Accept) are resolved from the request when the result executes.</summary>
		public IHttpActionResult ToHttpActionResult(HttpRequestMessage request) => new WireHttpActionResult(request, me, null);

		/// <summary>Explicit options; Accept is not consulted.</summary>
		public IHttpActionResult ToHttpActionResult(HttpRequestMessage request, ResponseOptions options) => new WireHttpActionResult(request, me, options);
	}
}

/// <summary>
/// Web API 2 offers no context outside the request (unlike ASP.NET Core's HttpContext), so the request
/// travels explicitly and options are resolved lazily when the framework executes the result.
/// </summary>
sealed class WireHttpActionResult(HttpRequestMessage request, IResponse response, ResponseOptions? explicitOptions) : IHttpActionResult
{
	public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
	{
		var options = explicitOptions ?? ResponseOptionsResolver.ResolveEffective(request);
		var mapping = ResponseWireMapper.Map(response, options);
		return Task.FromResult(ResponseHttpWriter.Write(request, mapping, ResponseOptionsResolver.ResolveJsonOptions(request)));
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
