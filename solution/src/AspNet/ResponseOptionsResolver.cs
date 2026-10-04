using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Fuxion.AspNet;

/// <summary>Resolves the effective options for a request: global defaults, controller/action layers, then Accept.</summary>
static class ResponseOptionsResolver
{
	public static ResponseOptions Resolve(HttpRequestMessage request)
	{
		var config = request.GetConfiguration();
		var global = config?.Properties.TryGetValue(HttpConfigurationResponseExtensions.OptionsKey, out var stored) == true && stored is ResponseOptions options
			? options
			: new ResponseOptions();

		var layers = new List<ResponseOptionsLayer>();
		var action = request.GetActionDescriptor();
		if (action is not null)
		{
			layers.AddRange(action.ControllerDescriptor.GetCustomAttributes<ResponsesAttribute>().Select(a => a.ToLayer()));
			layers.AddRange(action.GetCustomAttributes<ResponsesAttribute>().Select(a => a.ToLayer()));
		}

		return global.Merge(layers);
	}

	// The scope options plus what the client asked for in Accept.
	public static ResponseOptions ResolveEffective(HttpRequestMessage request)
		=> ResponseAccept.Apply(Resolve(request), request.Headers.Accept.ToString());

	public static JsonSerializerOptions ResolveJsonOptions(HttpRequestMessage request)
		=> request.GetConfiguration()?.Properties.TryGetValue(HttpConfigurationResponseExtensions.JsonOptionsKey, out var stored) == true && stored is JsonSerializerOptions json
			? json
			: FallbackJsonOptions;

	// Fallback for a request that somehow reaches the filter outside a configured HttpConfiguration
	// (e.g. UseResponses was never called on the pipeline serving it); must match what UseResponses
	// builds so behaviour does not silently change depending on how the request got here.
	static readonly JsonSerializerOptions FallbackJsonOptions = HttpConfigurationResponseExtensions.BuildJsonOptions(null);
}
