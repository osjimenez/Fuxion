using Fuxion.Analyzers.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fuxion.Analyzers.Types;

// The errors of the generator of type definitions (area FXTY): what the canonical form asks of a key, checked
// at compile time, where whoever writes the type sees it
static class TypeDefinitionsDiagnostics
{
	const string Category = "Usage";

	internal static readonly DiagnosticDescriptor InvalidKey = new(
		DiagnosticIds.TypesInvalidKey,
		"The key of a type is not in its canonical form",
		"The key '{1}' of '{0}' is not a UriKey in its canonical form: {2}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A key is https://<host>/<segment>[/<segment>…]/v<major>, in lowercase ASCII, without query, fragment, trailing slash or percent-encoding; anything else is rejected, never normalized.");

	internal static readonly DiagnosticDescriptor InvalidRevision = new(
		DiagnosticIds.TypesInvalidRevision,
		"The revision of a type is not valid",
		"The revision '{1}' of '{0}' is not valid: {2}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The revision of a type is a semantic version, MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD], of the same major version as its key.");

	internal static readonly DiagnosticDescriptor UnnameableReference = new(
		DiagnosticIds.TypesUnnameableReference,
		"The base or an interface of a type cannot be named",
		"The {1} '{2}' of '{0}' cannot be named: its type argument '{3}' has no key",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A definition names its base and its interfaces by their UriKey, a construction when they have type arguments, so each argument needs a key of its own or of the types of the system.");

	internal static readonly DiagnosticDescriptor RepeatedKey = new(
		DiagnosticIds.TypesRepeatedKey,
		"A key is on more than one type",
		"The key '{0}' of '{1}' is also the key of '{2}'",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A key names one type, with one arity: two types of an assembly cannot share it, not even a generic one and one that is not.");

	internal static readonly DiagnosticDescriptor UnreachableType = new(
		DiagnosticIds.TypesUnreachableType,
		"A type with a key cannot be registered",
		"The type '{0}' has a key, but the generated definitions cannot register it: {1}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The class marked with [TypeDefinitions] registers each type with a key of its assembly with typeof, so it has to see it.");

	internal static readonly DiagnosticDescriptor InvalidDefinitionsClass = new(
		DiagnosticIds.TypesInvalidDefinitionsClass,
		"The class of the type definitions is not valid",
		"The class '{0}' marked with [TypeDefinitions] cannot be generated: {1}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The generator fills a class marked with [TypeDefinitions] that is static, partial, not nested and not generic.");
}
