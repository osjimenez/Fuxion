namespace Fuxion.AspNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
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

/// <summary>
/// How much of a Web API 2 configuration's JSON traffic goes through <see cref="SystemTextJsonMediaTypeFormatter"/>,
/// as chosen by <see cref="HttpConfigurationResponseExtensions.UseResponses"/>.
/// </summary>
public enum JsonFormatterScope
{
	/// <summary>
	/// System.Text.Json only for Fuxion types (the union types themselves, <see cref="Undefinable{TValue}"/>,
	/// and any type whose public property graph reaches one, per <see cref="FuxionJsonTypes.RequiresSystemTextJson"/>);
	/// everything else keeps the host's own formatter (Newtonsoft, by default).
	/// </summary>
	FuxionTypes,

	/// <summary>System.Text.Json for every type; the Newtonsoft formatter is removed.</summary>
	All,

	/// <summary>
	/// No formatter change at all: the host's own formatter handles every type, including one containing an
	/// <see cref="Undefinable{TValue}"/> member, which will then never bind as "absent vs. explicitly null".
	/// </summary>
	None
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
	/// <param name="jsonOptions">Base JSON options for the union wire and, per <paramref name="scope"/>, for the
	/// rest of the app; defaults to <see cref="JsonSerializerDefaults.Web"/>.</param>
	/// <param name="scope">How much of the app's JSON traffic goes through <see cref="SystemTextJsonMediaTypeFormatter"/>.
	/// Defaults to <see cref="JsonFormatterScope.FuxionTypes"/>; see the remarks for what each value does.</param>
	/// <param name="useSystemTextJsonFor">Extra predicate consulted only under <see cref="JsonFormatterScope.FuxionTypes"/>:
	/// a type for which it returns <see langword="true"/> is also read with System.Text.Json, in addition to
	/// whatever <see cref="FuxionJsonTypes.RequiresSystemTextJson"/> already selects.</param>
	/// <remarks>
	/// <para>
	/// Everything Fuxion goes through System.Text.Json, always: the union wire itself (responses shaped as
	/// <see cref="Response{TSuccess}"/> and friends) and any request or response body whose type reaches an
	/// <see cref="Undefinable{TValue}"/> member, so that "absent vs. explicitly null" binds correctly. That much
	/// is true under every <paramref name="scope"/> and does not depend on which formatter is installed - the
	/// union wire writer never calls into <see cref="MediaTypeFormatter"/> at all.
	/// </para>
	/// <para>
	/// What <paramref name="scope"/> controls is everything else - plain, non-Fuxion request and response bodies:
	/// </para>
	/// <list type="bullet">
	/// <item><description>
	/// <see cref="JsonFormatterScope.FuxionTypes"/> (the default) inserts <see cref="SystemTextJsonMediaTypeFormatter"/>
	/// ahead of the existing formatters, but only for the Fuxion types described above (plus anything
	/// <paramref name="useSystemTextJsonFor"/> opts in); nothing else changes. <c>config.Formatters.JsonFormatter</c>
	/// stays: the host's own Newtonsoft formatter (PascalCase by default) keeps handling every plain type, exactly
	/// as it did before this call.
	/// </description></item>
	/// <item><description>
	/// <see cref="JsonFormatterScope.All"/> reproduces the old (pre-per-type) behaviour: the Newtonsoft formatter
	/// is removed and <see cref="SystemTextJsonMediaTypeFormatter"/> takes over every JSON type, request and
	/// response alike, with the naming policy carried by <paramref name="jsonOptions"/> (camelCase by default).
	/// After this call, <c>config.Formatters.JsonFormatter</c> is <see langword="null"/> - there is no Newtonsoft
	/// formatter left to look up. <see cref="HttpError"/> bodies (produced by the framework itself, e.g. an
	/// unhandled exception or a route-not-found) are not affected: they keep the PascalCase keys Web API always
	/// gives them, whichever JSON formatter is installed.
	/// </description></item>
	/// <item><description>
	/// <see cref="JsonFormatterScope.None"/> touches no formatter at all: the host's own formatter handles every
	/// type, including a Fuxion one used outside the union wire - so a plain action binding an
	/// <see cref="Undefinable{TValue}"/> member directly (not through <see cref="Response{TSuccess}"/>) will not
	/// see "absent vs. explicitly null" bind correctly.
	/// </description></item>
	/// </list>
	/// <para>
	/// Request bodies of any JSON media type read by <see cref="SystemTextJsonMediaTypeFormatter"/> are always
	/// decoded as UTF-8, regardless of a <c>charset</c> parameter on the request's Content-Type (see
	/// <see cref="SystemTextJsonMediaTypeFormatter"/>).
	/// </para>
	/// <para>
	/// The request's own <c>naming</c> Content-Type parameter (spec §3 - snake_case, kebab-case, etc.) is honoured
	/// only for a type actually bound by <see cref="SystemTextJsonMediaTypeFormatter"/>: under
	/// <see cref="JsonFormatterScope.FuxionTypes"/> that means a Fuxion type (a union type, <see cref="Undefinable{TValue}"/>,
	/// or a type whose public property graph reaches one) plus anything <paramref name="useSystemTextJsonFor"/> opts
	/// in - nothing more. A plain POCO outside that set still lands on the host's own formatter (Newtonsoft, by
	/// default), which knows nothing about <c>naming</c> and binds the request body however it always did (its
	/// default, case-insensitive PascalCase/camelCase matching), silently ignoring the parameter rather than
	/// transcoding the body. Under <see cref="JsonFormatterScope.All"/>, every type goes through
	/// <see cref="SystemTextJsonMediaTypeFormatter"/>, so <c>naming</c> is honoured for every request body, plain
	/// POCOs included.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">The configuration already has the wire contract enabled.</exception>
	public static HttpConfiguration UseResponses(this HttpConfiguration config, Action<ResponseOptions>? configure = null, JsonSerializerOptions? jsonOptions = null, JsonFormatterScope scope = JsonFormatterScope.FuxionTypes, Func<Type, bool>? useSystemTextJsonFor = null)
	{
		if (config.Properties.ContainsKey(OptionsKey))
			throw new InvalidOperationException($"{nameof(UseResponses)} was already called on this {nameof(HttpConfiguration)}; it can only be applied once.");

		var options = new ResponseOptions();
		configure?.Invoke(options);
		config.Properties[OptionsKey] = options;
		var json = BuildJsonOptions(jsonOptions);
		config.Properties[JsonOptionsKey] = json;

		switch (scope)
		{
			case JsonFormatterScope.FuxionTypes:
				// Keep the host's own formatter for everything else; only Fuxion types (union types,
				// Undefinable and anything containing them) - plus whatever the caller opts in - go
				// through System.Text.Json.
				config.Formatters.Insert(0, new SystemTextJsonMediaTypeFormatter(json, t => FuxionJsonTypes.RequiresSystemTextJson(t) || useSystemTextJsonFor?.Invoke(t) == true));
				break;
			case JsonFormatterScope.All:
				// Replace the default Newtonsoft formatter so request binding and any non-union response
				// use the same JSON options as the union wire (naming policy, converters, OmitUndefined).
				config.Formatters.Remove(config.Formatters.JsonFormatter);
				config.Formatters.Insert(0, new SystemTextJsonMediaTypeFormatter(json, canRead: null, writeAll: true));
				break;
			case JsonFormatterScope.None:
				// No formatter change: the host's own formatter handles every type unaided.
				break;
		}

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
