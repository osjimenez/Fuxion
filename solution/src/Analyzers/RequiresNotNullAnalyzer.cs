using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Fuxion.Analyzers;

namespace Fuxion.Analyzers;

/// <summary>
/// Analyzer that detects access to members marked with [RequiresNotNull] on nullable types.
/// Reports diagnostic FX001 when a member requiring non-null is accessed on a nullable type
/// without using the null-conditional operator (?.).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiresNotNullAnalyzer : DiagnosticAnalyzer
{
	// Fully qualified names as constants (safer for analyzers)
	private const string RequiresNotNullAttributeFullName = "Fuxion.Analyzers.RequiresNotNullAttribute";
	private const string HasMembersRequiringNotNullAttributeFullName = "Fuxion.Analyzers.HasMembersRequiringNotNullAttribute";

	private const string Category = "Usage";

	private static readonly LocalizableString Title =
		"Member requiring non-null accessed on nullable type";
	
	private static readonly LocalizableString MessageFormat =
		"Cannot access '{0}' on nullable type '{1}'. This member requires non-null values.";
	
	private static readonly LocalizableString Description =
		"Members marked with [RequiresNotNull] throw exceptions when accessed with null values. Use null-conditional operator or ensure the value is not null before accessing this member.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.RequiresNotNullViolation,
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description,
		helpLinkUri: $"https://github.com/osjimenez/Fuxion/blob/main/docs/analyzers/{DiagnosticIds.RequiresNotNullViolation}.md");

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
		ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		Debug.WriteLine("[FX001 Analyzer] ========== ANALYZER INITIALIZED ==========");
		
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// Register analysis for member access expressions
		context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
		
		Debug.WriteLine("[FX001 Analyzer] Registered for SimpleMemberAccessExpression analysis");
	}

	private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;
		var memberName = memberAccess.Name.Identifier.Text;

		Debug.WriteLine($"[FX001 Analyzer] ===== Analyzing member access: {memberName} =====");
		Debug.WriteLine($"[FX001 Analyzer] Location: {memberAccess.GetLocation().GetLineSpan()}");

		// Skip if this is a null-conditional access (e.g., obj?.Property)
		if (memberAccess.Parent is MemberBindingExpressionSyntax)
		{
			Debug.WriteLine($"[FX001 Analyzer] ? Is part of null-conditional access (?.), skipping");
			return;
		}

		// Get the symbol being accessed
		var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
		
		Debug.WriteLine($"[FX001 Analyzer] Member symbol: {memberSymbol?.ToDisplayString() ?? "NULL"}");
		Debug.WriteLine($"[FX001 Analyzer] Symbol kind: {memberSymbol?.Kind}");

		if (memberSymbol is not (IPropertySymbol or IMethodSymbol))
		{
			Debug.WriteLine($"[FX001 Analyzer] ? Symbol is not property or method, skipping");
			return;
		}

		Debug.WriteLine($"[FX001 Analyzer] ? Symbol is property or method");

		// Check attributes
		var hasDirectAttribute = HasRequiresNotNullAttribute(memberSymbol);
		var hasContainingTypeAttribute = HasRequiresNotNullInContainingType(memberSymbol);
		
		Debug.WriteLine($"[FX001 Analyzer] Has [RequiresNotNull] on member: {hasDirectAttribute}");
		Debug.WriteLine($"[FX001 Analyzer] Has [HasMembersRequiringNotNull] on type: {hasContainingTypeAttribute}");

		// Check if the member requires non-null
		bool requiresNotNull = hasDirectAttribute || hasContainingTypeAttribute;

		if (!requiresNotNull)
		{
			Debug.WriteLine($"[FX001 Analyzer] ? Member does NOT require non-null, skipping");
			return;
		}

		Debug.WriteLine($"[FX001 Analyzer] ? Member REQUIRES non-null");

		// Get type info of the expression on which the member is accessed
		var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
		
		Debug.WriteLine($"[FX001 Analyzer] Expression type: {typeInfo.Type?.ToDisplayString() ?? "NULL"}");
		Debug.WriteLine($"[FX001 Analyzer] Nullable annotation: {typeInfo.Type?.NullableAnnotation}");
		
		if (typeInfo.Type is null)
		{
			Debug.WriteLine($"[FX001 Analyzer] ? Type is null, skipping");
			return;
		}

		// Check if the type is nullable
		bool isNullable = IsNullableType(typeInfo.Type);
		
		// Special handling for FuxionExtensions<T?>
		// We need to analyze the ORIGINAL expression to determine if it was nullable
		if (typeInfo.Type is INamedTypeSymbol namedType && 
		    (namedType.Name == "FuxionExtensions" || namedType.Name.StartsWith("FuxionExtensions`")))
		{
			Debug.WriteLine($"[FX001 Analyzer] Detected FuxionExtensions<T?> - analyzing origin");
			
			// Analyze the origin expression (before .Fx)
			bool originIsNullable = AnalyzeFxOrigin(memberAccess.Expression, context.SemanticModel);
			
			Debug.WriteLine($"[FX001 Analyzer] Origin is nullable: {originIsNullable}");
			isNullable = originIsNullable;
		}
		else
		{
			// For non-FuxionExtensions types, check generics normally
			isNullable = isNullable || HasNullableGenericParameter(typeInfo.Type);
		}
		
		Debug.WriteLine($"[FX001 Analyzer] Is nullable type (direct): {IsNullableType(typeInfo.Type)}");
		Debug.WriteLine($"[FX001 Analyzer] Is nullable (final): {isNullable}");

		if (isNullable)
		{
			Debug.WriteLine($"[FX001 Analyzer] ?? REPORTING DIAGNOSTIC FX001 for '{memberName}' on '{typeInfo.Type.ToDisplayString()}'");
			
			var diagnostic = Diagnostic.Create(
				Rule,
				memberAccess.Name.GetLocation(),
				memberName,
				typeInfo.Type.ToDisplayString());

			context.ReportDiagnostic(diagnostic);
			
			Debug.WriteLine($"[FX001 Analyzer] ? Diagnostic reported successfully");
		}
		else
		{
			Debug.WriteLine($"[FX001 Analyzer] ? Type is NOT nullable, no diagnostic");
		}

		Debug.WriteLine($"[FX001 Analyzer] ===== END analysis for {memberName} =====\n");
	}

	/// <summary>
	/// Analyzes the origin of a .Fx expression to determine if the original value was nullable.
	/// For example: "hello".Fx vs nullable?.Fx
	/// </summary>
	private static bool AnalyzeFxOrigin(ExpressionSyntax expression, SemanticModel semanticModel)
	{
		Debug.WriteLine($"[FX001 Analyzer] ===== Analyzing Fx origin =====");
		
		// If the expression is a MemberAccessExpression ending with .Fx, get the base
		if (expression is MemberAccessExpressionSyntax memberAccess && 
		    memberAccess.Name.Identifier.Text == "Fx")
		{
			var baseExpression = memberAccess.Expression;
			Debug.WriteLine($"[FX001 Analyzer] Base expression: {baseExpression}");
			
			// Get the type of the base expression (before .Fx)
			var baseTypeInfo = semanticModel.GetTypeInfo(baseExpression);
			
			if (baseTypeInfo.Type != null)
			{
				Debug.WriteLine($"[FX001 Analyzer] Base type: {baseTypeInfo.Type.ToDisplayString()}");
				Debug.WriteLine($"[FX001 Analyzer] Base nullable annotation: {baseTypeInfo.Type.NullableAnnotation}");
				
				// Check if the base type is nullable
				bool baseIsNullable = IsNullableType(baseTypeInfo.Type);
				
				Debug.WriteLine($"[FX001 Analyzer] Base is nullable: {baseIsNullable}");
				
				return baseIsNullable;
			}
		}
		
		Debug.WriteLine($"[FX001 Analyzer] Could not determine origin nullability, assuming non-nullable");
		return false;
	}

	/// <summary>
	/// Checks if the member has [RequiresNotNull] attribute directly.
	/// This works for traditional properties and methods.
	/// </summary>
	private static bool HasRequiresNotNullAttribute(ISymbol symbol)
	{
		return symbol.GetAttributes()
			.Any(attr => attr.AttributeClass?.ToDisplayString() == RequiresNotNullAttributeFullName);
	}

	/// <summary>
	/// Checks if the containing type has [HasMembersRequiringNotNull] attribute
	/// listing this member. This is a workaround for C# 14 extension members where
	/// individual member attributes are not visible to analyzers.
	/// </summary>
	private static bool HasRequiresNotNullInContainingType(ISymbol symbol)
	{
		if (symbol is not (IPropertySymbol or IMethodSymbol))
			return false;

		var containingType = symbol.ContainingType;
		if (containingType == null)
			return false;

		// Look for [HasMembersRequiringNotNull] attribute on the containing type
		var attribute = containingType.GetAttributes()
			.FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == HasMembersRequiringNotNullAttributeFullName);

		if (attribute == null)
			return false;

		// Get the list of member names from the attribute constructor
		if (attribute.ConstructorArguments.Length == 0)
			return false;

		var firstArg = attribute.ConstructorArguments[0];
		
		// The constructor takes params string[], so we need to extract the array values
		if (firstArg.Kind != TypedConstantKind.Array)
			return false;

		var memberNames = firstArg.Values
			.Select(val => val.Value as string)
			.Where(name => name != null)
			.ToList();

		// Check if the current member is in the list
		return memberNames.Contains(symbol.Name);
	}

	private static string? GetCustomMessage(ISymbol symbol)
	{
		var attribute = symbol.GetAttributes()
			.FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == RequiresNotNullAttributeFullName);

		if (attribute is null)
			return null;

		// Look for CustomMessage property
		var customMessageArg = attribute.NamedArguments
			.FirstOrDefault(arg => arg.Key == "CustomMessage");

		return customMessageArg.Value.Value as string;
	}

	private static bool IsNullableType(ITypeSymbol type)
	{
		// Nullable reference type (string?, MyClass?)
		if (type.NullableAnnotation == NullableAnnotation.Annotated)
			return true;

		// Nullable value type (int?, DateTime?)
		if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
			return true;

		return false;
	}

	/// <summary>
	/// Checks if the type has any nullable generic type parameters.
	/// For example, List&lt;string?&gt; has a nullable generic parameter.
	/// </summary>
	private static bool HasNullableGenericParameter(ITypeSymbol type)
	{
		// Check if it's a generic type
		if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
			return false;

		// Check each type argument for nullability
		foreach (var typeArg in namedType.TypeArguments)
		{
			if (IsNullableType(typeArg))
			{
				Debug.WriteLine($"[FX001 Analyzer] Found nullable generic parameter: {typeArg.ToDisplayString()}");
				return true;
			}
			
			// Recursively check nested generic types
			if (HasNullableGenericParameter(typeArg))
				return true;
		}

		return false;
	}
}
