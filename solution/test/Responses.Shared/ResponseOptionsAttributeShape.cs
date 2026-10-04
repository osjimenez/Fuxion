using System;
using System.Linq;
using System.Reflection;

namespace Test.Responses.Shared;

// Each adapter carries its own ResponseOptionsAttribute (core has no dependency on HTTP metadata), so the two copies
// can only drift silently. Both hosts compare their attribute against this one public surface: a member added to
// only one of them fails that host's test. The two types cannot be compared directly, because Fuxion.AspNet is
// net472 only and never loads in the same process as Fuxion.AspNetCore.
public static class ResponseOptionsAttributeShape
{
	public static readonly string[] Expected =
	[
		".ctor()",
		"Boolean SerializeErrorAsProblemDetails { get; set; }",
		"Boolean SerializeFullResponses { get; set; }",
		"Boolean StrictNone { get; set; }",
		"ResponseOptionsLayer ToLayer()",
	];

	public static string[] Describe(Type attribute)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		var constructors = attribute.GetConstructors(flags)
			.Select(c => $".ctor({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})");
		var properties = attribute.GetProperties(flags)
			.Select(p => $"{p.PropertyType.Name} {p.Name} {{ {(p.CanRead ? "get; " : "")}{(p.CanWrite ? "set; " : "")}}}");
		var methods = attribute.GetMethods(flags).Where(m => !m.IsSpecialName)
			.Select(m => $"{m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
		return [.. constructors.Concat(properties).Concat(methods).OrderBy(s => s, StringComparer.Ordinal)];
	}
}
