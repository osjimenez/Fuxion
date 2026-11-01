namespace Fuxion;

// The messages of what Fuxion marks as not compatible with trimming or native AOT: whoever uses it there gets them in
// their warning.
static class AotMessages
{
	internal const string Json = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload "
		+ "that takes a JsonTypeInfo, or options with a source-generated JsonSerializerContext (chain FuxionJsonContext for the types of Fuxion).";
	internal const string JsonDynamic = "JSON serialization and deserialization might require types that cannot be statically analyzed and might need "
		+ "runtime code generation. Use System.Text.Json source generation for native AOT applications.";
	internal const string Reflection = "It reflects over members of types that trimming may remove.";
	internal const string DynamicCode = "It creates generic types or methods at run time, which native AOT may not have compiled.";
	internal const string Xml = "XML serialization might require types that cannot be statically analyzed.";
	internal const string Configuration = "Binding the configuration might require types that cannot be statically analyzed.";
	internal const string Validation = "Validating with data annotations reflects over the members of the validated type.";
}
