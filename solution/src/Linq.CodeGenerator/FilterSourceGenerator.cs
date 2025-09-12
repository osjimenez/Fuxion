// Source Generator para filtros de Fuxion.Linq
// Genera clases parciales de filtros basados en un esquema declarativo.
// Soporta: propiedades escalares (incl. computadas), colecciones escalares, navegaciones simples y colecciones de navegación.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuxion.Linq.CodeGenerator;

[Generator]
public sealed class FilterSourceGenerator : IIncrementalGenerator
{
	#region Diagnostics
	private static readonly string[] SchemaAttributeSimpleNames = ["FilterSchema", "FilterSchemaAttribute"]; // nombres aceptados
	private static readonly DiagnosticDescriptor DxInfo = new("FXLQ000", "Info", "{0}", "FilterGenerator", DiagnosticSeverity.Info, true);
	private static readonly DiagnosticDescriptor DxMissingAttributeArg = new("FXLQ001", "Filter schema attribute sin argumento", "[{0}] requiere un argumento: el nombre del miembro estático que contiene el schema", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxMemberNotFound = new("FXLQ002", "Miembro schema no encontrado", "No se encontró el miembro estático '{0}' en la clase '{1}'", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxNoInitializer = new("FXLQ003", "Schema sin inicializador", "El miembro schema '{0}' en '{1}' no tiene inicializador (builder pipeline)", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxNoDescriptors = new("FXLQ004", "No se detectaron descriptores", "No se detectaron llamadas a Property/Navigation en el schema '{0}' de '{1}'", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxCannotInferEntity = new("FXLQ005", "No se pudo inferir el tipo de entidad", "No se pudo inferir el tipo de entidad para '{0}'", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxUnsupportedProperty = new("FXLQ006", "Propiedad no soportada", "La propiedad '{0}' de '{1}' tiene tipo no soportada: {2}", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxNavFilterRequired = new("FXLQ007", "Navigation requiere filtro explícito", "Usa Navigation<TFilter>(lambda) para la navegación '{0}'", "FilterGenerator", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor DxNavFilterMismatch = new("FXLQ008", "Filtro de navegación incompatible", "El filtro '{0}' no corresponde con el tipo de navegación '{1}' en '{2}'", "FilterGenerator", DiagnosticSeverity.Error, true);
	#endregion

	#region Initialize
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var candidates = context.SyntaxProvider
			.CreateSyntaxProvider(IsPotentialClass, Transform)
			.Where(c => c is not null)
			.Select((c, _) => c!);
		context.RegisterSourceOutput(candidates, (spc, cand) => Generate(spc, cand));
	}
	private static bool IsPotentialClass(SyntaxNode node, CancellationToken _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0 && cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
	#endregion

	#region Transform helpers
	private INamedTypeSymbol? InferEntityType(IFieldSymbol field, SemanticModel model, CancellationToken ct)
	{
		var sr = field.DeclaringSyntaxReferences.FirstOrDefault();
		if (sr?.GetSyntax(ct) is not VariableDeclaratorSyntax vds) return null;
		var init = vds.Initializer?.Value;
		if (init == null) return null;
		foreach (var ma in init.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
			if (ma.Name is GenericNameSyntax g && g.Identifier.Text == "For" && g.TypeArgumentList.Arguments.Count == 1)
			{
				var ti = model.GetTypeInfo(g.TypeArgumentList.Arguments[0], ct);
				return ti.Type as INamedTypeSymbol;
			}
		return null;
	}

	private static INamedTypeSymbol? TryGetNavEntityFromFilter(INamedTypeSymbol filterSym, Compilation compilation, CancellationToken ct)
	{
		// 1) Si ya hereda de GeneratedFilter<T>, usarlo
		for (var cur = filterSym; cur != null; cur = cur.BaseType)
		{
			if (cur is INamedTypeSymbol ns && ns.IsGenericType && ns.ConstructedFrom.Name == "GeneratedFilter" && ns.ConstructedFrom.ContainingNamespace.ToDisplayString() == "Fuxion.Linq")
				return ns.TypeArguments[0] as INamedTypeSymbol;
		}
		// 2) Deducir desde su propio schema [FilterSchema("...")] leyendo el For<T>
		var attr = filterSym.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name is "FilterSchema" or "FilterSchemaAttribute");
		string? member = null;
		if (attr != null && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Kind == TypedConstantKind.Primitive && attr.ConstructorArguments[0].Value is string s)
			member = s;
		if (string.IsNullOrWhiteSpace(member)) return null;
		var field = filterSym.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => f.IsStatic && f.Name == member);
		if (field == null) return null;
		var sr = field.DeclaringSyntaxReferences.FirstOrDefault();
		if (sr == null) return null;
		var tree = sr.SyntaxTree;
		var sm = compilation.GetSemanticModel(tree);
		return new FilterSourceGenerator().InferEntityType(field, sm, ct); // use same logic
	}
	#endregion

	#region Transform
	private Candidate? Transform(GeneratorSyntaxContext ctx, CancellationToken ct)
	{
		var cls = (ClassDeclarationSyntax)ctx.Node;
		var model = ctx.SemanticModel;
		var symbol = model.GetDeclaredSymbol(cls, ct);
		if (symbol == null) return null;

		AttributeSyntax? attr = null;
		foreach (var al in cls.AttributeLists)
		foreach (var a in al.Attributes)
		{
			var name = a.Name switch
			{
				IdentifierNameSyntax id => id.Identifier.Text,
				QualifiedNameSyntax q => q.Right.Identifier.Text,
				AliasQualifiedNameSyntax an => an.Name.Identifier.Text,
				_ => a.Name.ToString()
			};
			if (SchemaAttributeSimpleNames.Contains(name)) { attr = a; break; }
		}
		if (attr == null) return null;

		var diags = ImmutableArray.CreateBuilder<Diagnostic>();
		string? member = null;
		if (attr.ArgumentList?.Arguments.Count == 1)
		{
			var cv = model.GetConstantValue(attr.ArgumentList.Arguments[0].Expression, ct);
			if (cv.HasValue) member = cv.Value as string;
		}
		if (string.IsNullOrWhiteSpace(member))
		{
			diags.Add(Diagnostic.Create(DxMissingAttributeArg, attr.GetLocation(), symbol.Name));
			return new(symbol, null, null, null, null, ImmutableArray<Descriptor>.Empty, diags.ToImmutable(), true);
		}

		var field = symbol.GetMembers().FirstOrDefault(m => m.Name == member && m is IFieldSymbol fs && fs.IsStatic) as IFieldSymbol;
		if (field == null)
		{
			diags.Add(Diagnostic.Create(DxMemberNotFound, cls.Identifier.GetLocation(), member, symbol.Name));
			return new(symbol, null, member, null, null, ImmutableArray<Descriptor>.Empty, diags.ToImmutable(), true);
		}

		var entity = InferEntityType(field, model, ct);
		if (entity == null)
		{
			diags.Add(Diagnostic.Create(DxCannotInferEntity, cls.Identifier.GetLocation(), symbol.Name));
			return new(symbol, field, member, null, null, ImmutableArray<Descriptor>.Empty, diags.ToImmutable(), true);
		}

		var syntaxRef = field.DeclaringSyntaxReferences.FirstOrDefault();
		var vds = syntaxRef?.GetSyntax(ct) as VariableDeclaratorSyntax;
		if (vds?.Initializer?.Value == null)
		{
			diags.Add(Diagnostic.Create(DxNoInitializer, cls.Identifier.GetLocation(), member, symbol.Name));
			return new(symbol, field, member, entity, vds, ImmutableArray<Descriptor>.Empty, diags.ToImmutable(), true);
		}

		var descriptors = ExtractDescriptors(vds.Initializer.Value, model, ct, diags).ToList();
		if (descriptors.Count == 0)
		{
			diags.Add(Diagnostic.Create(DxNoDescriptors, cls.Identifier.GetLocation(), member, symbol.Name));
			return new(symbol, field, member, entity, vds, ImmutableArray<Descriptor>.Empty, diags.ToImmutable(), true);
		}

		diags.Add(Diagnostic.Create(DxInfo, cls.Identifier.GetLocation(), $"FilterGenerator: {symbol.Name} -> {descriptors.Count} descriptor(s)"));
		return new(symbol, field, member, entity, vds, descriptors.ToImmutableArray(), diags.ToImmutable(), false);
	}

	private IEnumerable<Descriptor> ExtractDescriptors(ExpressionSyntax root, SemanticModel model, CancellationToken ct, ImmutableArray<Diagnostic>.Builder diags)
	{
		foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			if (inv.Expression is not MemberAccessExpressionSyntax me) continue;
			var method = me.Name.Identifier.Text;
			var isNav = method == "Navigation";
			var isProp = method == "Property";
			var isComp = method == "Computed";
			if (!isNav && !isProp && !isComp) continue;
			if (inv.ArgumentList.Arguments.Count == 0) continue;

			// Computed
			if (isComp)
			{
				if (inv.ArgumentList.Arguments.Count < 2) continue;
				var nameCv = model.GetConstantValue(inv.ArgumentList.Arguments[0].Expression, ct);
				if (!nameCv.HasValue || nameCv.Value is not string compName) continue;
				if (inv.ArgumentList.Arguments[1].Expression is not SimpleLambdaExpressionSyntax lam) continue;
				var bodyExpr = lam.Body as ExpressionSyntax; if (bodyExpr == null) continue;
				var rt = (model.GetTypeInfo(bodyExpr, ct).Type ?? model.GetTypeInfo(bodyExpr, ct).ConvertedType) ??
					(model.GetTypeInfo(lam, ct).ConvertedType as INamedTypeSymbol)?.DelegateInvokeMethod?.ReturnType;
				if (rt == null) continue;
				bool isNullable = false; ITypeSymbol eff = rt;
				if (rt is INamedTypeSymbol nts && nts.IsGenericType && nts.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) { isNullable = true; eff = nts.TypeArguments[0]; }
				bool isStr = eff.SpecialType == SpecialType.System_String;
				bool isEnum = eff.TypeKind == TypeKind.Enum;
				bool isComparable = !isStr && !isEnum && eff.AllInterfaces.Any(i => i.ToDisplayString().StartsWith("System.IComparable"));
				yield return new Descriptor(compName, compName, DescriptorKind.Property, null, false, eff, isNullable, isEnum, isStr, isComparable, true, bodyExpr, lam.Parameter.Identifier.Text, false, false, null, null);
				continue;
			}

			// Property (incluye detección de colección escalar)
			if (isProp)
			{
				if (inv.ArgumentList.Arguments[0].Expression is not SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax bodyProp }) continue;
				var propName = bodyProp.Name.Identifier.Text;
				var propSymbol = model.GetSymbolInfo(bodyProp, ct).Symbol as IPropertySymbol; if (propSymbol == null) continue;
				var pType = propSymbol.Type;
				// Colección escalar
				ITypeSymbol? elem = null; bool isScalarCollection = false;
				if (pType.TypeKind == TypeKind.Array && pType is IArrayTypeSymbol ats2) { elem = ats2.ElementType; isScalarCollection = true; }
				else if (pType.SpecialType != SpecialType.System_String)
				{
					var ien2 = pType.AllInterfaces.FirstOrDefault(i => i is INamedTypeSymbol ins && ins.Name == "IEnumerable" && ins.TypeArguments.Length == 1) as INamedTypeSymbol;
					if (ien2 != null) { elem = ien2.TypeArguments[0]; isScalarCollection = true; }
				}
				if (isScalarCollection && elem != null)
				{
					bool isElemStr = elem.SpecialType == SpecialType.System_String;
					bool isElemEnum = elem.TypeKind == TypeKind.Enum;
					bool isElemComparable = !isElemStr && !isElemEnum && elem.AllInterfaces.Any(i => i.ToDisplayString().StartsWith("System.IComparable"));
					yield return new Descriptor(propName, propName, DescriptorKind.CollectionScalar, null, false, elem, false, isElemEnum, isElemStr, isElemComparable, false, null, null, true, false, null, null);
					continue;
				}
				// Propiedad escalar normal
				bool nullable = false; ITypeSymbol effType = pType;
				if (pType is INamedTypeSymbol nns && nns.IsGenericType && nns.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) { nullable = true; effType = nns.TypeArguments[0]; }
				bool isStrProp = effType.SpecialType == SpecialType.System_String;
				bool isEnumProp = effType.TypeKind == TypeKind.Enum;
				bool isCompProp = !isStrProp && !isEnumProp && effType.AllInterfaces.Any(i => i.ToDisplayString().StartsWith("System.IComparable"));
				yield return new Descriptor(propName, propName, DescriptorKind.Property, null, false, effType, nullable, isEnumProp, isStrProp, isCompProp, false, null, null, false, false, null, null);
				continue;
			}

			// Navigation: nueva API Navigation<TFilter>(lambda)
			if (isNav)
			{
				var isGeneric = me.Name is GenericNameSyntax gnav && gnav.TypeArgumentList.Arguments.Count == 1;
				if (!isGeneric)
				{
					var name = (inv.ArgumentList.Arguments[0].Expression as SimpleLambdaExpressionSyntax)?.Body.ToString() ?? "?";
					diags.Add(Diagnostic.Create(DxNavFilterRequired, me.Name.GetLocation(), name));
					continue;
				}
				var filterType = model.GetTypeInfo(((GenericNameSyntax)me.Name).TypeArgumentList.Arguments[0], ct).Type as INamedTypeSymbol;
				if (filterType == null) continue;
				var navEntity = TryGetNavEntityFromFilter(filterType, model.Compilation, ct);
				if (navEntity == null)
				{
					diags.Add(Diagnostic.Create(DxNavFilterMismatch, me.Name.GetLocation(), filterType.ToDisplayString(), "<unknown>", method));
					continue;
				}
				if (inv.ArgumentList.Arguments[0].Expression is not SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax bodyNav }) continue;
				var propName = bodyNav.Name.Identifier.Text;
				var propSymbol = model.GetSymbolInfo(bodyNav, ct).Symbol as IPropertySymbol; if (propSymbol == null) continue;
				var pType = propSymbol.Type;
				// ¿colección?
				INamedTypeSymbol? elemType = null; bool isCollectionNav = false;
				if (pType.TypeKind == TypeKind.Array && pType is IArrayTypeSymbol ats) { isCollectionNav = true; elemType = ats.ElementType as INamedTypeSymbol; }
				else if (pType.SpecialType != SpecialType.System_String)
				{
					var ien = pType.AllInterfaces.FirstOrDefault(i => i is INamedTypeSymbol ins && ins.Name == "IEnumerable" && ins.TypeArguments.Length == 1) as INamedTypeSymbol;
					if (ien != null) { isCollectionNav = true; elemType = ien.TypeArguments[0] as INamedTypeSymbol; }
				}
				if (isCollectionNav)
				{
					if (!SymbolEqualityComparer.Default.Equals(elemType, navEntity))
					{
						diags.Add(Diagnostic.Create(DxNavFilterMismatch, me.Name.GetLocation(), filterType.ToDisplayString(), pType.ToDisplayString(), propName));
						continue;
					}
					yield return new Descriptor(propName, propName, DescriptorKind.NavigationCollection, $"global::{filterType.ToDisplayString()}", false, elemType, false, false, false, false, false, null, null, false, true, $"global::{navEntity.ToDisplayString()}", $"global::{filterType.ToDisplayString()}");
					continue;
				}
				// simple
				if (!SymbolEqualityComparer.Default.Equals(pType, navEntity))
				{
					diags.Add(Diagnostic.Create(DxNavFilterMismatch, me.Name.GetLocation(), filterType.ToDisplayString(), pType.ToDisplayString(), propName));
					continue;
				}
				yield return new Descriptor(propName, propName, DescriptorKind.Navigation, $"global::{filterType.ToDisplayString()}", false, null, false, false, false, false, false, null, null, false, false, null, null);
				continue;
			}
		}
	}
	#endregion

	#region Generate
	private void Generate(SourceProductionContext context, Candidate cand)
	{
		foreach (var d in cand.Diagnostics) context.ReportDiagnostic(d);
		if (cand.SkipGeneration || cand.EntityType == null) return;
		var entity = cand.EntityType;
		var filter = cand.ClassSymbol;
		var ns = filter.ContainingNamespace.IsGlobalNamespace ? null : filter.ContainingNamespace.ToDisplayString();
		var sb = new StringBuilder();
		if (ns != null) sb.AppendLine($"namespace {ns};");
		sb.AppendLine("// <auto-generated>\n// Generated by Fuxion.Linq source generator\n// </auto-generated>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine("using Fuxion.Linq;");
		sb.AppendLine("using System.Text.Json.Serialization;");
		sb.AppendLine("using System.Linq.Expressions;");
		sb.AppendLine();
		sb.AppendLine("[JsonConverter(typeof(FilterConverterFactory))]");
		sb.AppendLine($"public partial class {filter.Name} : GeneratedFilter<{entity.ToDisplayString()}> ");
		sb.AppendLine("{");
		sb.AppendLine("\t// PROPERTIES / COLLECTIONS / NAVIGATIONS\n");

		// Scalar properties & computed
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.Property))
		{
			var ifaceName = $"I{d.Name}Operations";
			var className = $"{d.Name}Operations";
			var bt = d.PropertyType?.ToDisplayString() ?? "object";
			if (d.IsNullable) bt += "?";
			var ifaceList = new List<string>
			{
				$"IFilterOperations<{bt}>",
				$"IEqualityFilterOperations<{bt}>",
				$"ISetFilterOperations<{bt}>"
			};
			if (d.IsComparable || d.IsString || d.IsEnum) ifaceList.Add($"IRelationalFilterOperations<{bt}>");
			if (d.IsNullable || !(d.PropertyType?.IsValueType ?? false)) ifaceList.Add("INullabilityFilterOperations");
			if (d.IsString) ifaceList.Add($"ITextFilterOperations<{bt}>");
			if (d.IsEnum) ifaceList.Add($"IEnumFlagFilterOperations<{bt}>");
			var inherit = string.Join(", ", ifaceList.Distinct());
			sb.AppendLine($"\tpublic interface {ifaceName} : {inherit} {{ }}");
			sb.AppendLine($"\tpublic sealed class {className} : FilterOperations<{bt}>, {ifaceName} {{ }}");
			sb.AppendLine($"\tpublic {ifaceName} {d.Name} {{ get; }} = new {className}();\n");
		}

		// Scalar collections
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.CollectionScalar))
		{
			var et = d.PropertyType?.ToDisplayString() ?? "object";
			sb.AppendLine($"\tpublic ScalarCollectionFilterOperations<{et}> {d.Name} {{ get; }} = new();\n");
		}

		// Navigations simples
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.Navigation))
		{
			if (d.TargetFilterType is not null)
				sb.AppendLine($"\tpublic {d.TargetFilterType} {d.Name} {{ get; }} = new {d.TargetFilterType}();");
		}
		if (cand.Descriptors.Any(x => x.Kind == DescriptorKind.Navigation)) sb.AppendLine();

		// Navigation collections
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.NavigationCollection))
		{
			if (d.CollectionElementFilterType is not null && d.CollectionElementEntityType is not null)
				sb.AppendLine($"\tpublic NavigationCollectionFilterOperations<{d.CollectionElementFilterType}, {d.CollectionElementEntityType}> {d.Name} {{ get; }} = new();");
		}
		if (cand.Descriptors.Any(x => x.Kind == DescriptorKind.NavigationCollection)) sb.AppendLine();

		// HasAny override
		var hasParts = cand.Descriptors
			.Where(d => d.Kind is DescriptorKind.Property or DescriptorKind.Navigation or DescriptorKind.CollectionScalar or DescriptorKind.NavigationCollection)
			.Select(d => $"{d.Name}.HasAny()")
			.ToList();
		sb.AppendLine($"\tpublic override bool HasAny() => {(hasParts.Count == 0 ? "false" : string.Join(" || ", hasParts))};\n");

		// Predicate
		sb.AppendLine($"\tExpression<System.Func<{entity.ToDisplayString()}, bool>>? _predicate;");
		sb.AppendLine($"\tpublic override Expression<System.Func<{entity.ToDisplayString()}, bool>> Predicate => _predicate ??= Build();\n");
		sb.AppendLine($"\tExpression<System.Func<{entity.ToDisplayString()}, bool>> Build() {{");
		sb.AppendLine($"\t\tvar x = Parameter<{entity.ToDisplayString()}>(\"x\");");
		sb.AppendLine("\t\tExpression body = TrueConstant;");

		// Apply properties
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.Property))
		{
			if (d.IsComputed && d.RawExpression != null)
				sb.AppendLine($"\t\tbody = And(body, ApplyComputed({d.Name}, ({entity.ToDisplayString()} {d.ParamName ?? "p"}) => {d.RawExpression}, x));");
			else
				sb.AppendLine($"\t\tbody = And(body, ApplyProperty({d.Name}, x, nameof({entity.ToDisplayString()}.{d.SourceMember}))); ");
		}

		// Apply navigations
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.Navigation))
			if (d.TargetFilterType is not null)
				sb.AppendLine($"\t\tif ({d.Name}.HasAny()) body = And(body, ApplyNavigation({d.Name}.Predicate, Access(x, nameof({entity.ToDisplayString()}.{d.SourceMember})), true));");

		// Apply scalar collections
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.CollectionScalar))
			sb.AppendLine($"\t\tif ({d.Name}.HasAny()) body = And(body, ApplyScalarCollection(Access(x, nameof({entity.ToDisplayString()}.{d.SourceMember})), {d.Name}));");

		// Apply navigation collections
		foreach (var d in cand.Descriptors.Where(x => x.Kind == DescriptorKind.NavigationCollection))
			if (d.CollectionElementFilterType is not null && d.CollectionElementEntityType is not null)
				sb.AppendLine($"\t\tif ({d.Name}.HasAny()) body = And(body, ApplyNavigationCollection(Access(x, nameof({entity.ToDisplayString()}.{d.SourceMember})), {d.Name}));");

		sb.AppendLine($"\t\treturn Expression.Lambda<System.Func<{entity.ToDisplayString()}, bool>>(body, x);");
		sb.AppendLine("\t}");
		sb.AppendLine("}");

		context.AddSource(filter.Name + ".generated.cs", sb.ToString());
	}
	#endregion

	#region Records
	private record Candidate(
		INamedTypeSymbol ClassSymbol,
		IFieldSymbol? SchemaField,
		string? SchemaFieldName,
		INamedTypeSymbol? EntityType,
		VariableDeclaratorSyntax? FieldSyntax,
		ImmutableArray<Descriptor> Descriptors,
		ImmutableArray<Diagnostic> Diagnostics,
		bool SkipGeneration);

	private record Descriptor(
		string Name,
		string SourceMember,
		DescriptorKind Kind,
		string? TargetFilterType,
		bool Unsupported,
		ITypeSymbol? PropertyType,
		bool IsNullable,
		bool IsEnum,
		bool IsString,
		bool IsComparable,
		bool IsComputed,
		ExpressionSyntax? RawExpression,
		string? ParamName,
		bool IsCollectionScalar,
		bool IsNavigationCollection,
		string? CollectionElementEntityType,
		string? CollectionElementFilterType);

	private enum DescriptorKind
	{
		Property,
		Navigation,
		CollectionScalar,
		NavigationCollection
	}
	#endregion
}