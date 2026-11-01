using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Fuxion.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuxion.Analyzers.Types;

/// <summary>
/// Writes, in each class marked with <c>[TypeDefinitions]</c>, the definitions of the types of its assembly that have a
/// <c>[UriKey]</c> and the method that registers them, so that the registry of types is filled without reflection; and
/// checks at compile time what those attributes say (the errors FXTY001 to FXTY006).
/// </summary>
/// <remarks>
/// <para>
/// The definition of a type takes its key and its revision from its attribute, its arity from its declaration, its base
/// from its nearest ancestor with a key, and its interfaces from all those it implements that have a key, in the ordinal
/// order of their keys. A base or an interface with type arguments is named by its construction; if an argument is a
/// generic parameter, by its key alone, as an open generic type: the notation of partially open types is not fixed yet.
/// </para>
/// <para>The schema of the data of each type is left empty: its generation comes later.</para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class TypeDefinitionsGenerator : IIncrementalGenerator
{
	const string UriKeyAttributeName = "Fuxion.Types.UriKeyAttribute";
	const string TypeDefinitionsAttributeName = "Fuxion.Types.TypeDefinitionsAttribute";

	// The official regex of Semantic Versioning 2.0.0, as SemanticVersion of Fuxion has it, from
	// https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
	static readonly Regex SemanticVersionRegex = new(
		@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
		RegexOptions.CultureInvariant);

	// The types of the system by their full name in metadata, from the same table Fuxion has
	static readonly Dictionary<string, string> SystemKeys = SystemTypeKeys.All
		.Where(entry => entry.ClrName is not null)
		.ToDictionary(entry => entry.ClrName!, entry => entry.Key, StringComparer.Ordinal);

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var keyed = context.SyntaxProvider.ForAttributeWithMetadataName(
				UriKeyAttributeName,
				static (node, _) => node is BaseTypeDeclarationSyntax,
				static (attributed, cancellation) => ReadKeyedType((INamedTypeSymbol)attributed.TargetSymbol, attributed.Attributes[0], attributed.TargetNode, cancellation))
			.Collect();
		var holders = context.SyntaxProvider.ForAttributeWithMetadataName(
			TypeDefinitionsAttributeName,
			static (node, _) => node is ClassDeclarationSyntax,
			static (attributed, _) => ReadHolder((INamedTypeSymbol)attributed.TargetSymbol, (ClassDeclarationSyntax)attributed.TargetNode));
		// The errors of the types are reported once, whether or not a class receives their definitions
		context.RegisterSourceOutput(keyed, static (output, types) => Report(output, types));
		context.RegisterSourceOutput(holders.Combine(keyed), static (output, pair) => Generate(output, pair.Left, pair.Right));
	}

	#region The types with a key
	sealed record KeyedType(
		string TypeOf,
		string Display,
		string? Key,
		string? Revision,
		int Arity,
		string? Base,
		EquatableArray<string> Interfaces,
		LocationInfo? Location,
		EquatableArray<DiagnosticInfo> Diagnostics);

	static KeyedType ReadKeyedType(INamedTypeSymbol type, AttributeData attribute, SyntaxNode node, CancellationToken cancellation)
	{
		var location = LocationInfo.From(attribute.ApplicationSyntaxReference?.GetSyntax(cancellation).GetLocation() ?? node.GetLocation());
		var display = Display(type);
		List<DiagnosticInfo> diagnostics = [];

		// Its key and its revision
		var keyText = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
		var revisionText = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value as string : null;
		string? key = null;
		uint major = 0;
		if (keyText is null)
			diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.InvalidKey, location, display, "", "a key cannot be empty"));
		else if (!UriKeyGrammar.TryValidateKey(keyText, 0, keyText.Length, out major, out var keyError))
			diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.InvalidKey, location, display, keyText, keyError));
		else
			key = keyText;
		string? revision = null;
		var match = revisionText is null ? null : SemanticVersionRegex.Match(revisionText);
		if (match is null || !match.Success)
			diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.InvalidRevision, location, display, revisionText ?? "",
				"it is not a semantic version, MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]"));
		else if (key is not null && (!uint.TryParse(match.Groups["major"].Value, out var revisionMajor) || revisionMajor != major))
			diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.InvalidRevision, location, display, revisionText!,
				$"its major version is not the one of its key, v{major}"));
		else
			revision = revisionText;

		// Whether the generated class can reach it
		if (Unreachable(type) is { } reason)
			diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.UnreachableType, location, display, reason));

		// Its base: the nearest ancestor with a key
		string? @base = null;
		if (type.TypeKind == TypeKind.Class)
			for (var ancestor = type.BaseType; ancestor is not null && ancestor.SpecialType != SpecialType.System_Object; ancestor = ancestor.BaseType)
			{
				var name = Name(ancestor);
				if (name.Kind == NameKind.NoKey) continue;
				if (name.Kind == NameKind.Unnameable)
					diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.UnnameableReference, location, display, "base", Display(ancestor), name.Culprit!));
				else
					@base = name.Key;
				break;
			}

		// Its interfaces with a key, in the ordinal order of their keys
		SortedSet<string> interfaces = new(StringComparer.Ordinal);
		foreach (var @interface in type.AllInterfaces)
		{
			var name = Name(@interface);
			if (name.Kind == NameKind.NoKey) continue;
			if (name.Kind == NameKind.Unnameable)
				diagnostics.Add(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.UnnameableReference, location, display, "interface", Display(@interface), name.Culprit!));
			else
				interfaces.Add(name.Key!);
		}

		var typeOf = (type.Arity > 0 ? type.ConstructUnboundGenericType() : type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		return new(typeOf, display, key, revision, type.Arity, @base, interfaces.ToArray(), location, diagnostics.ToArray());
	}

	static string? Unreachable(INamedTypeSymbol type)
	{
		if (type.IsFileLocal) return "it is local to its file";
		for (var current = type; current is not null; current = current.ContainingType)
		{
			if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal)
				return "it is private or protected";
			if (!SymbolEqualityComparer.Default.Equals(current, type) && current.IsGenericType) return "it is nested in a generic type";
		}
		return null;
	}

	enum NameKind
	{
		// A key or a construction
		Key,
		// A generic parameter, or something that has one: the key alone, if there is one
		Open,
		// A type without a key, of its own or of the system
		NoKey,
		// A construction with a type argument that has no key
		Unnameable
	}

	readonly record struct TypeName(NameKind Kind, string? Key, string? Culprit);

	// The UriKey of a type, as a definition names its base, an interface or a type argument
	static TypeName Name(ITypeSymbol type)
	{
		switch (type)
		{
			case ITypeParameterSymbol:
				return new(NameKind.Open, null, null);
			case IArrayTypeSymbol { IsSZArray: true } array:
				var element = Name(array.ElementType);
				return element.Kind switch
				{
					NameKind.Key => new(NameKind.Key, $"{SystemTypeKeys.Array}[{element.Key}]", null),
					NameKind.Open => new(NameKind.Open, SystemTypeKeys.Array, null),
					NameKind.NoKey => new(NameKind.Unnameable, null, Display(array.ElementType)),
					_ => element
				};
			case INamedTypeSymbol named:
				var definition = named.OriginalDefinition;
				var key = OwnKey(definition) ?? SystemKey(definition);
				if (key is null) return new(NameKind.NoKey, null, Display(named));
				if (named.TypeArguments.Length == 0) return new(NameKind.Key, key, null);
				List<string> arguments = [];
				var open = false;
				foreach (var argument in named.TypeArguments)
				{
					var name = Name(argument);
					switch (name.Kind)
					{
						case NameKind.Key:
							arguments.Add(name.Key!);
							break;
						case NameKind.Open:
							open = true;
							break;
						case NameKind.NoKey:
							return new(NameKind.Unnameable, null, Display(argument));
						default:
							return name;
					}
				}
				return open ? new(NameKind.Open, key, null) : new(NameKind.Key, $"{key}[{string.Join(",", arguments)}]", null);
			default:
				return new(NameKind.NoKey, null, Display(type));
		}
	}

	// The key of its own [UriKey], if it has one in its canonical form; one that is not is reported on its own type
	static string? OwnKey(INamedTypeSymbol definition)
	{
		foreach (var attribute in definition.GetAttributes())
		{
			if (attribute.AttributeClass?.ToDisplayString() != UriKeyAttributeName) continue;
			return attribute.ConstructorArguments.Length > 0
				&& attribute.ConstructorArguments[0].Value is string key
				&& UriKeyGrammar.TryValidateKey(key, 0, key.Length, out _, out _)
					? key
					: null;
		}
		return null;
	}

	static string? SystemKey(INamedTypeSymbol definition)
	{
		if (definition.ContainingType is not null || definition.ContainingNamespace is null) return null;
		var name = definition.ContainingNamespace.IsGlobalNamespace
			? definition.MetadataName
			: definition.ContainingNamespace.ToDisplayString() + "." + definition.MetadataName;
		return SystemKeys.TryGetValue(name, out var key) ? key : null;
	}

	static string Display(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

	static void Report(SourceProductionContext output, ImmutableArray<KeyedType> types)
	{
		foreach (var type in types)
		foreach (var diagnostic in type.Diagnostics)
			output.ReportDiagnostic(diagnostic.ToDiagnostic());
		foreach (var repeated in types.Where(type => type.Key is not null).GroupBy(type => type.Key, StringComparer.Ordinal).Where(group => group.Count() > 1))
		foreach (var type in repeated)
		{
			var other = repeated.First(candidate => !ReferenceEquals(candidate, type));
			output.ReportDiagnostic(DiagnosticInfo.Create(TypeDefinitionsDiagnostics.RepeatedKey, type.Location, type.Key!, type.Display, other.Display).ToDiagnostic());
		}
	}
	#endregion

	#region The classes that receive the definitions
	sealed record Holder(string? Namespace, string Name, string HintName, DiagnosticInfo? Error);

	static Holder ReadHolder(INamedTypeSymbol type, ClassDeclarationSyntax node)
	{
		var display = Display(type);
		string? reason = null;
		if (!type.IsStatic || !node.Modifiers.Any(SyntaxKind.PartialKeyword)) reason = "it has to be static and partial";
		else if (type.ContainingType is not null) reason = "it cannot be nested in another type";
		else if (type.IsGenericType) reason = "it cannot be generic";
		var error = reason is null
			? null
			: DiagnosticInfo.Create(TypeDefinitionsDiagnostics.InvalidDefinitionsClass, LocationInfo.From(node.Identifier.GetLocation()), display, reason);
		var @namespace = type.ContainingNamespace is null || type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
		return new(@namespace, type.Name, $"{(@namespace is null ? "" : @namespace + ".")}{type.Name}.TypeDefinitions.g.cs", error);
	}

	static void Generate(SourceProductionContext output, Holder holder, ImmutableArray<KeyedType> types)
	{
		if (holder.Error is not null)
		{
			output.ReportDiagnostic(holder.Error.ToDiagnostic());
			return;
		}
		// The valid ones, without the keys that are repeated (reported apart), in the ordinal order of their keys
		var valid = types
			.Where(type => type.Diagnostics.IsEmpty && type.Key is not null && type.Revision is not null)
			.GroupBy(type => type.Key!, StringComparer.Ordinal)
			.Where(group => group.Count() == 1)
			.Select(group => group.First())
			.OrderBy(type => type.Key, StringComparer.Ordinal)
			.ToArray();
		output.AddSource(holder.HintName, Emit(holder, valid));
	}

	static string Emit(Holder holder, KeyedType[] types)
	{
		const string definition = "global::Fuxion.Types.TypeDefinition";
		const string uriKey = "global::Fuxion.Types.UriKey";
		var code = new StringBuilder();
		code.AppendLine("// <auto-generated/>");
		// Without #nullable or other recent syntax: it compiles with the version of C# of whoever uses it (7.3 on .NET Framework)
		code.AppendLine("// The definitions of the types with a [UriKey] of this assembly, written by the generator of Fuxion.");
		code.AppendLine();
		var indent = "";
		if (holder.Namespace is not null)
		{
			code.AppendLine($"namespace {holder.Namespace}");
			code.AppendLine("{");
			indent = "\t";
		}
		code.AppendLine($"{indent}static partial class {holder.Name}");
		code.AppendLine($"{indent}{{");
		code.AppendLine($"{indent}\t/// <summary>The definitions of the types with a key of this assembly, in the order of their keys.</summary>");
		code.AppendLine($"{indent}\tpublic static global::System.Collections.Generic.IReadOnlyList<{definition}> Definitions {{ get; }} = new {definition}[]");
		code.AppendLine($"{indent}\t{{");
		foreach (var type in types)
		{
			var @base = type.Base is null ? "null" : $"{uriKey}.Parse({Literal(type.Base)})";
			var interfaces = type.Interfaces.IsEmpty
				? "null"
				: $"new {uriKey}[] {{ {string.Join(", ", type.Interfaces.Select(@interface => $"{uriKey}.Parse({Literal(@interface)})"))} }}";
			code.AppendLine($"{indent}\t\t// {type.Display}");
			code.AppendLine(
				$"{indent}\t\tnew {definition}({uriKey}.Parse({Literal(type.Key!)}), new global::Fuxion.SemanticVersion({Literal(type.Revision!)}), {type.Arity}, {@base}, {interfaces}),");
		}
		code.AppendLine($"{indent}\t}};");
		code.AppendLine();
		code.AppendLine($"{indent}\t/// <summary>Registers each type with a key of this assembly, with its definition.</summary>");
		code.AppendLine($"{indent}\t/// <param name=\"registry\">The registry of types.</param>");
		code.AppendLine($"{indent}\tpublic static void Register(global::Fuxion.Types.TypeRegistry registry)");
		code.AppendLine($"{indent}\t{{");
		code.AppendLine($"{indent}\t\tif (registry is null) throw new global::System.ArgumentNullException(nameof(registry));");
		for (var i = 0; i < types.Length; i++)
			code.AppendLine($"{indent}\t\tregistry.Register(typeof({types[i].TypeOf}), Definitions[{i}]);");
		code.AppendLine($"{indent}\t}}");
		code.AppendLine($"{indent}}}");
		if (holder.Namespace is not null) code.AppendLine("}");
		return code.ToString();
	}

	static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);
	#endregion
}
