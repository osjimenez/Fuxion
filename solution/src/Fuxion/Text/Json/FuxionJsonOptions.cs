using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Fuxion.Text.Json;

// The options Fuxion resolves the JSON contracts with, when it reads or writes outside a converter.
static class FuxionJsonOptions
{
	const string ReflectionGuarded = "Only while the feature switch System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault is on: trimming and "
		+ "native AOT turn it off by default, and then the trimmer removes this branch.";

	// When there are no options: the contracts of FuxionJsonContext, and the reflection-based ones for the rest while
	// reflection is enabled, as the default options of System.Text.Json; Web, with the defaults of JsonSerializerDefaults.Web.
	internal static JsonSerializerOptions Default { get; } = Create(new JsonSerializerOptions());
	internal static JsonSerializerOptions Web { get; } = Create(new JsonSerializerOptions(JsonSerializerDefaults.Web));

	static JsonSerializerOptions Create(JsonSerializerOptions options)
	{
		options.TypeInfoResolver = FuxionJsonContext.Default;
		if (JsonSerializer.IsReflectionEnabledByDefault)
			AddReflectionResolver(options);
		options.MakeReadOnly();
		return options;
	}

	// The options a caller gives resolve the contracts. Options without a resolver yet get the reflection-based one, as
	// JsonSerializer and ASP.NET Core do, only while reflection is enabled; without it, they have to bring their own
	// resolver (a source-generated context). Then they are locked, as JsonSerializer locks them on their first use.
	internal static JsonSerializerOptions Configured(this JsonSerializerOptions? options)
	{
		if (options is null)
			return Default;
		if (options.IsReadOnly)
			return options;
		if (options.TypeInfoResolver is not null)
			options.MakeReadOnly();
		else if (JsonSerializer.IsReflectionEnabledByDefault)
			PopulateReflectionResolver(options);
		return options;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionGuarded)]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionGuarded)]
	static void AddReflectionResolver(JsonSerializerOptions options) => options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionGuarded)]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionGuarded)]
	static void PopulateReflectionResolver(JsonSerializerOptions options) => options.MakeReadOnly(populateMissingResolver: true);
}
