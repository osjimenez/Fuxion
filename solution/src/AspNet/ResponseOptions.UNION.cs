namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Web.Http;
using System.Web.Http.Controllers;
using Fuxion.Union;

/// <summary>
/// Partial response options for a controller or an action. Null properties inherit from the outer
/// scope (global, then controller, then action).
/// </summary>
// Nullable<T> is not a legal attribute parameter type (CS0655), so a tri-state "not set / true / false"
// property cannot be typed as bool? here; each value is tracked with a shadow "has a value" flag instead,
// and only ToLayer() exposes the nullable semantics documented for a response options layer.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResponseOptionsAttribute : Attribute
{
	bool serializeFullResponses;
	bool hasSerializeFullResponses;
	public bool SerializeFullResponses
	{
		get => serializeFullResponses;
		set { serializeFullResponses = value; hasSerializeFullResponses = true; }
	}

	bool serializeErrorAsProblemDetails;
	bool hasSerializeErrorAsProblemDetails;
	public bool SerializeErrorAsProblemDetails
	{
		get => serializeErrorAsProblemDetails;
		set { serializeErrorAsProblemDetails = value; hasSerializeErrorAsProblemDetails = true; }
	}

	bool strictNone;
	bool hasStrictNone;
	public bool StrictNone
	{
		get => strictNone;
		set { strictNone = value; hasStrictNone = true; }
	}

	public ResponseOptionsLayer ToLayer() => new()
	{
		SerializeFullResponses = hasSerializeFullResponses ? serializeFullResponses : null,
		SerializeErrorAsProblemDetails = hasSerializeErrorAsProblemDetails ? serializeErrorAsProblemDetails : null,
		StrictNone = hasStrictNone ? strictNone : null
	};
}

public static class HttpConfigurationResponseExtensions
{
	internal const string OptionsKey = "Fuxion.Union.ResponseOptions";
	internal const string JsonOptionsKey = "Fuxion.Union.JsonSerializerOptions";

	/// <summary>
	/// Enables the union response wire contract on a Web API 2 configuration: global defaults, the
	/// action filter that maps union return values, and the JSON options used to write them (Web
	/// defaults plus the omission of undefined <see cref="Undefinable{TValue}"/> members).
	/// </summary>
	/// <param name="config">The configuration to enable the wire contract on.</param>
	/// <param name="configure">Sets the global default options (scope + error shape).</param>
	/// <param name="jsonOptions">Base JSON options for the union wire and, unless <paramref name="replaceJsonFormatter"/>
	/// is <see langword="false"/>, for the whole app; defaults to <see cref="JsonSerializerDefaults.Web"/>.</param>
	/// <param name="replaceJsonFormatter">Whether the default Newtonsoft <c>JsonMediaTypeFormatter</c> is replaced
	/// by <see cref="SystemTextJsonMediaTypeFormatter"/>. Defaults to <see langword="true"/>; see the remarks for
	/// what setting it to <see langword="false"/> does and does not change.</param>
	/// <remarks>
	/// <para>
	/// With the default <paramref name="replaceJsonFormatter"/> of <see langword="true"/>, this call replaces the
	/// app's JSON formatter for every action, not only union ones: a plain (non-union) action's request and
	/// response bodies switch from Newtonsoft (PascalCase by default) to System.Text.Json with the naming policy
	/// carried by <paramref name="jsonOptions"/> (camelCase by default), and gain the omission of undefined
	/// <see cref="Undefinable{TValue}"/> members. After this call, <c>config.Formatters.JsonFormatter</c> is
	/// <see langword="null"/> - there is no Newtonsoft formatter left to look up. <see cref="HttpError"/> bodies
	/// (produced by the framework itself, e.g. an unhandled exception or a route-not-found) are not affected: they
	/// keep the PascalCase keys Web API always gives them, whichever JSON formatter is installed. Request bodies
	/// of any JSON media type are always decoded as UTF-8, regardless of a <c>charset</c> parameter on the
	/// request's Content-Type (see <see cref="SystemTextJsonMediaTypeFormatter"/>).
	/// </para>
	/// <para>
	/// Passing <see langword="false"/> leaves the Newtonsoft formatter (and its PascalCase-by-default behaviour)
	/// in place for non-union actions; the union wire itself is unaffected, since it always serializes with
	/// System.Text.Json regardless of which formatter is installed. Binding an <see cref="Undefinable{TValue}"/>
	/// member as "absent vs. explicitly null" requires the System.Text.Json formatter's converter, so a
	/// non-union action that binds one still needs <paramref name="replaceJsonFormatter"/> left at its default.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">The configuration already has the wire contract enabled.</exception>
	public static HttpConfiguration UseResponses(this HttpConfiguration config, Action<ResponseOptions>? configure = null, JsonSerializerOptions? jsonOptions = null, bool replaceJsonFormatter = true)
	{
		if (config.Properties.ContainsKey(OptionsKey))
			throw new InvalidOperationException($"{nameof(UseResponses)} was already called on this {nameof(HttpConfiguration)}; it can only be applied once.");

		var options = new ResponseOptions();
		configure?.Invoke(options);
		config.Properties[OptionsKey] = options;
		var json = BuildJsonOptions(jsonOptions);
		config.Properties[JsonOptionsKey] = json;

		if (replaceJsonFormatter)
		{
			// Replace the default Newtonsoft formatter so request binding and any non-union response
			// use the same JSON options as the union wire (naming policy, converters, OmitUndefined).
			config.Formatters.Remove(config.Formatters.JsonFormatter);
			config.Formatters.Insert(0, new SystemTextJsonMediaTypeFormatter(json));
		}

		config.MessageHandlers.Add(new RequestNamingHandler());

		config.Filters.Add(new ResponseActionFilter());
		return config;
	}

	/// <summary>Web defaults (or <paramref name="source"/>) plus the omission of undefined <see cref="Undefinable{TValue}"/> members.</summary>
	internal static JsonSerializerOptions BuildJsonOptions(JsonSerializerOptions? source)
	{
		var json = new JsonSerializerOptions(source ?? new JsonSerializerOptions(JsonSerializerDefaults.Web));
		json.TypeInfoResolver = (json.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(UndefinableJsonTypeInfo.OmitUndefined);
		return json;
	}
}

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
			layers.AddRange(action.ControllerDescriptor.GetCustomAttributes<ResponseOptionsAttribute>().Select(a => a.ToLayer()));
			layers.AddRange(action.GetCustomAttributes<ResponseOptionsAttribute>().Select(a => a.ToLayer()));
		}

		return ResponseAccept.Apply(global.Merge(layers), request.Headers.Accept.ToString());
	}

	public static JsonSerializerOptions ResolveJsonOptions(HttpRequestMessage request)
		=> request.GetConfiguration()?.Properties.TryGetValue(HttpConfigurationResponseExtensions.JsonOptionsKey, out var stored) == true && stored is JsonSerializerOptions json
			? json
			: FallbackJsonOptions;

	// Fallback for a request that somehow reaches the filter outside a configured HttpConfiguration
	// (e.g. UseResponses was never called on the pipeline serving it); must match what UseResponses
	// builds so behaviour does not silently change depending on how the request got here.
	static readonly JsonSerializerOptions FallbackJsonOptions = HttpConfigurationResponseExtensions.BuildJsonOptions(null);
}
