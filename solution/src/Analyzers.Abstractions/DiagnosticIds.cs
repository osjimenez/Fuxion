namespace Fuxion.Analyzers.Abstractions;

/// <summary>
/// Centralized diagnostic IDs for all Fuxion analyzers.
/// Pattern: FX + [Area Code] + [Number]
/// </summary>
public static class DiagnosticIds
{
	// ==================== CORE FUXION (FX) ====================
	// Range: FX001-099
	const string CoreBase = "FX";
	
	/// <summary>
	/// FX001: Member requiring non-null accessed on nullable type.
	/// Cannot access member marked with [RequiresNotNull] on nullable type.
	/// </summary>
	public const string RequiresNotNullViolation = CoreBase + "001";

	// ==================== PODS (FXPD) ====================
	// Range: FXPD001-099
	//public const string PodsBase = "FXPD";
	// Reserved for future Pods-specific analyzers

	// ==================== JSON (FXJN) ====================
	// Range: FXJN001-099
	//public const string JsonBase = "FXJN";
	// Reserved for future JSON analyzers

	// ==================== LINQ (FXLQ) ====================
	// Range: FXLQ001-099 (migrated from Fuxion.Linq.CodeGenerator)
	//public const string LinqBase = "FXLQ";
	// Note: FXLQ000-009 are currently in Fuxion.Linq.CodeGenerator
	// and will remain there as they are Source Generator diagnostics

	// ==================== TYPES (FXTY) ====================
	// Range: FXTY001-099: the generator of type definitions, from the [UriKey] of each type
	const string TypesBase = "FXTY";

	/// <summary>
	/// FXTY001: the key of a <c>[UriKey]</c> is not a key in its canonical form.
	/// </summary>
	public const string TypesInvalidKey = TypesBase + "001";

	/// <summary>
	/// FXTY002: the revision of a <c>[UriKey]</c> is not a semantic version, or not of the major version of its key.
	/// </summary>
	public const string TypesInvalidRevision = TypesBase + "002";

	/// <summary>
	/// FXTY003: the base or an interface of a type with a key cannot be named: a type argument has no key.
	/// </summary>
	public const string TypesUnnameableReference = TypesBase + "003";

	/// <summary>
	/// FXTY004: the same key is on more than one type of an assembly.
	/// </summary>
	public const string TypesRepeatedKey = TypesBase + "004";

	/// <summary>
	/// FXTY005: a type with a key cannot be reached by the generated class (private, protected, local to its file, or
	/// nested in a generic type).
	/// </summary>
	public const string TypesUnreachableType = TypesBase + "005";

	/// <summary>
	/// FXTY006: the class marked with <c>[TypeDefinitions]</c> is not static and partial, or is nested or generic.
	/// </summary>
	public const string TypesInvalidDefinitionsClass = TypesBase + "006";

	// ==================== REFLECTION (FXRF) ====================
	// Range: FXRF001-099
	//public const string ReflectionBase = "FXRF";
	// Reserved for future Reflection analyzers

	// ==================== TOOL (FXTL) ====================
	// Range: FXTL001-099
	// Reserved for Fuxion.Tool.Sdk and fx (Fuxion-tool repo): MSBuild errors and warnings, not analyzers.
	// FXTL001: a repository that needs the workspace has an FxReference that is not mounted there.
}
