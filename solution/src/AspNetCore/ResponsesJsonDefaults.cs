using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Fuxion.Text.Json;

namespace Fuxion.AspNetCore;

// The JSON contracts the adapters fall back to when the application gives none: those of FuxionJsonContext and, only
// while reflection is enabled, the reflection-based ones, as ASP.NET Core does with its own defaults.
static class ResponsesJsonDefaults
{
	const string ReflectionGuarded = "Only while the feature switch System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault is on: trimming and "
		+ "native AOT turn it off by default, and then the trimmer removes this branch.";

	// Web defaults, for a host without the JSON options of ASP.NET Core.
	internal static JsonSerializerOptions Web { get; } = CreateWeb();

	internal static IJsonTypeInfoResolver? ReflectionIfEnabled() => JsonSerializer.IsReflectionEnabledByDefault ? CreateReflectionResolver() : null;

	static JsonSerializerOptions CreateWeb()
	{
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			TypeInfoResolver = JsonTypeInfoResolver.Combine(FuxionJsonContext.Default, ReflectionIfEnabled())
		};
		options.MakeReadOnly();
		return options;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionGuarded)]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionGuarded)]
	static IJsonTypeInfoResolver CreateReflectionResolver() => new DefaultJsonTypeInfoResolver();
}
