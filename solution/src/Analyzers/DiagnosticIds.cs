namespace Fuxion.Analyzers;

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

	// ==================== REFLECTION (FXRF) ====================
	// Range: FXRF001-099
	//public const string ReflectionBase = "FXRF";
	// Reserved for future Reflection analyzers
}
