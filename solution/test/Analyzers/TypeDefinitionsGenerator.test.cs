using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Fuxion.Analyzers.Types;
using Fuxion.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Test.Analyzers;

// The generator of type definitions, run on code of these tests: what it writes for the types with a [UriKey] and the
// class with [TypeDefinitions], and the errors FXTY001 to FXTY006. That the code it writes registers the types at run
// time is tested in Test.Fuxion and Test.Aot, with types of their own.
public class TypeDefinitionsGeneratorTest
{
	const string Root = "https://meta.fuxion.nexus/";
	const string Holder = "[TypeDefinitions] internal static partial class SampleTypes { }";

	static readonly ImmutableArray<MetadataReference> References =
	[
		..((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
		.Split(Path.PathSeparator)
		.Append(typeof(UriKey).Assembly.Location)
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.Select(path => MetadataReference.CreateFromFile(path))
	];

	sealed record Run(string? Generated, ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Diagnostic> CompilationErrors);

	static Run Generate(string source, LanguageVersion languageVersion = LanguageVersion.Preview)
	{
		var options = new CSharpParseOptions(languageVersion);
		var tree = CSharpSyntaxTree.ParseText($"using Fuxion.Types;\nusing System.Collections.Generic;\nnamespace Sample\n{{\n{source}\n}}", options);
		var compilation = CSharpCompilation.Create("Sample", [tree], References,
			new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: languageVersion >= LanguageVersion.CSharp8 ? NullableContextOptions.Enable : NullableContextOptions.Disable));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new TypeDefinitionsGenerator().AsSourceGenerator()], parseOptions: options);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);
		var generated = driver.GetRunResult().GeneratedTrees.SingleOrDefault()?.ToString();
		var errors = output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToImmutableArray();
		return new(generated, diagnostics, errors);
	}

	static Run GenerateClean(string source, LanguageVersion languageVersion = LanguageVersion.Preview)
	{
		var run = Generate(source, languageVersion);
		Assert.Empty(run.Diagnostics);
		Assert.Empty(run.CompilationErrors);
		Assert.NotNull(run.Generated);
		return run;
	}

	static Diagnostic Single(Run run, string id)
	{
		var diagnostic = Assert.Single(run.Diagnostics, diagnostic => diagnostic.Id == id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		return diagnostic;
	}

	[Fact(DisplayName = "A type with a key gets its definition and its registration")]
	public void Simple()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}erp/invoice/v1", "1.3.0")] public class Invoice { }
			{{Holder}}
			""");
		Assert.Contains($"new global::Fuxion.Types.TypeDefinition(global::Fuxion.Types.UriKey.Parse(\"{Root}erp/invoice/v1\"), new global::Fuxion.SemanticVersion(\"1.3.0\"), 0, null, null),", run.Generated);
		Assert.Contains("registry.Register(typeof(global::Sample.Invoice), Definitions[0]);", run.Generated);
		Assert.Contains("namespace Sample", run.Generated);
		Assert.Contains("static partial class SampleTypes", run.Generated);
	}

	[Fact(DisplayName = "A generic type has its arity, and is registered by its definition")]
	public void Generic()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/box/v1", "1.0.0")] public class Box<T> { }
			[UriKey("{{Root}}sample/pair/v2", "2.1.0-preview.1")] public struct Pair<TFirst, TSecond> { }
			{{Holder}}
			""");
		Assert.Contains($"UriKey.Parse(\"{Root}sample/box/v1\"), new global::Fuxion.SemanticVersion(\"1.0.0\"), 1, null, null)", run.Generated);
		Assert.Contains($"UriKey.Parse(\"{Root}sample/pair/v2\"), new global::Fuxion.SemanticVersion(\"2.1.0-preview.1\"), 2, null, null)", run.Generated);
		Assert.Contains("registry.Register(typeof(global::Sample.Box<>), Definitions[0]);", run.Generated);
		Assert.Contains("registry.Register(typeof(global::Sample.Pair<,>), Definitions[1]);", run.Generated);
	}

	[Fact(DisplayName = "The base is the nearest ancestor with a key")]
	public void Base()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}docs/document/v1", "1.0.0")] public abstract class Document { }
			public abstract class Helper : Document { }
			[UriKey("{{Root}}docs/adr/v1", "1.0.0")] public class Adr : Helper { }
			public class Unkeyed { }
			[UriKey("{{Root}}docs/note/v1", "1.0.0")] public class Note : Unkeyed { }
			{{Holder}}
			""");
		Assert.Contains($"UriKey.Parse(\"{Root}docs/adr/v1\"), new global::Fuxion.SemanticVersion(\"1.0.0\"), 0, global::Fuxion.Types.UriKey.Parse(\"{Root}docs/document/v1\"), null)", run.Generated);
		Assert.Contains($"UriKey.Parse(\"{Root}docs/note/v1\"), new global::Fuxion.SemanticVersion(\"1.0.0\"), 0, null, null)", run.Generated);
	}

	[Fact(DisplayName = "A generic base or interface is named by its construction, with the types of the system")]
	public void Constructions()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/box/v1", "1.0.0")] public class Box<T> { }
			[UriKey("{{Root}}sample/holder/v1", "1.0.0")] public interface IHolder<T> { }
			[UriKey("{{Root}}erp/invoice/v1", "1.0.0")] public class Invoice { }
			[UriKey("{{Root}}erp/invoices/v1", "1.0.0")] public class Invoices : Box<List<Invoice>>, IHolder<int[]>, IHolder<Dictionary<string, Invoice>> { }
			{{Holder}}
			""");
		var system = Root + "system/";
		Assert.Contains($"UriKey.Parse(\"{Root}sample/box/v1[{system}list/v1[{Root}erp/invoice/v1]]\")", run.Generated);
		Assert.Contains($"UriKey.Parse(\"{Root}sample/holder/v1[{system}array/v1[{system}int/v1]]\")", run.Generated);
		Assert.Contains($"UriKey.Parse(\"{Root}sample/holder/v1[{system}dictionary/v1[{system}string/v1,{Root}erp/invoice/v1]]\")", run.Generated);
	}

	[Fact(DisplayName = "The interfaces are all those with a key, inherited too, in the order of their keys")]
	public void Interfaces()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/b-entity/v1", "1.0.0")] public interface IEntity { }
			[UriKey("{{Root}}sample/a-named/v1", "1.0.0")] public interface INamed : IEntity { }
			public interface IUnkeyed { }
			[UriKey("{{Root}}sample/base/v1", "1.0.0")] public class Base : INamed, IUnkeyed { }
			[UriKey("{{Root}}sample/derived/v1", "1.0.0")] public class Derived : Base, System.IEquatable<Derived> { public bool Equals(Derived? other) => false; }
			{{Holder}}
			""");
		var interfaces = $"new global::Fuxion.Types.UriKey[] {{ global::Fuxion.Types.UriKey.Parse(\"{Root}sample/a-named/v1\"), global::Fuxion.Types.UriKey.Parse(\"{Root}sample/b-entity/v1\") }}";
		Assert.Contains($"UriKey.Parse(\"{Root}sample/base/v1\"), new global::Fuxion.SemanticVersion(\"1.0.0\"), 0, null, {interfaces})", run.Generated);
		Assert.Contains($"global::Fuxion.Types.UriKey.Parse(\"{Root}sample/base/v1\"), {interfaces})", run.Generated);
		// An interface with a key has its interfaces with a key, and no base
		Assert.Contains($"UriKey.Parse(\"{Root}sample/a-named/v1\"), new global::Fuxion.SemanticVersion(\"1.0.0\"), 0, null, new global::Fuxion.Types.UriKey[] {{ global::Fuxion.Types.UriKey.Parse(\"{Root}sample/b-entity/v1\") }})", run.Generated);
	}

	[Fact(DisplayName = "A base or an interface on a generic parameter is named by its key alone")]
	public void Open()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/holder/v1", "1.0.0")] public interface IHolder<T> { }
			[UriKey("{{Root}}sample/box/v1", "1.0.0")] public class Box<T> : IHolder<List<T>> { }
			{{Holder}}
			""");
		Assert.Contains($"UriKey.Parse(\"{Root}sample/box/v1\"), new global::Fuxion.SemanticVersion(\"1.0.0\"), 1, null, new global::Fuxion.Types.UriKey[] {{ global::Fuxion.Types.UriKey.Parse(\"{Root}sample/holder/v1\") }})", run.Generated);
	}

	[Fact(DisplayName = "Records, structs, interfaces, enums and nested types have their definitions")]
	public void Kinds()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/record/v1", "1.0.0")] public record Order(string Name);
			[UriKey("{{Root}}sample/value/v1", "1.0.0")] public readonly record struct Amount(decimal Value);
			[UriKey("{{Root}}sample/status/v1", "1.0.0")] public enum Status { Draft, Accepted }
			public class Outer { [UriKey("{{Root}}sample/inner/v1", "1.0.0")] internal class Inner { } }
			{{Holder}}
			""");
		Assert.Contains("registry.Register(typeof(global::Sample.Order), ", run.Generated);
		Assert.Contains("registry.Register(typeof(global::Sample.Amount), ", run.Generated);
		Assert.Contains("registry.Register(typeof(global::Sample.Status), ", run.Generated);
		Assert.Contains("registry.Register(typeof(global::Sample.Outer.Inner), ", run.Generated);
	}

	[Fact(DisplayName = "The generated code compiles with C# 7.3, as on .NET Framework")]
	public void CSharp73()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/box/v1", "1.0.0")] public class Box<T> { }
			[UriKey("{{Root}}erp/invoice/v1", "1.0.0")] public class Invoice : Box<string> { }
			{{Holder}}
			""", LanguageVersion.CSharp7_3);
		Assert.Contains($"global::Fuxion.Types.UriKey.Parse(\"{Root}sample/box/v1[{Root}system/string/v1]\")", run.Generated);
	}

	[Fact(DisplayName = "Without a class with [TypeDefinitions] nothing is generated, but the errors are reported")]
	public void WithoutHolder()
	{
		Assert.Null(GenerateClean2("""[UriKey("https://meta.fuxion.nexus/erp/invoice/v1", "1.0.0")] public class Invoice { }""").Generated);
		Single(Generate("""[UriKey("https://Meta.fuxion.nexus/erp/invoice/v1", "1.0.0")] public class Invoice { }"""), "FXTY001");

		static Run GenerateClean2(string source)
		{
			var run = Generate(source);
			Assert.Empty(run.Diagnostics);
			Assert.Empty(run.CompilationErrors);
			return run;
		}
	}

	[Theory(DisplayName = "FXTY001: a key that is not in its canonical form, and why")]
	[InlineData("http://meta.fuxion.nexus/erp/invoice/v1", "starts with 'https://'")]
	[InlineData("https://Meta.fuxion.nexus/erp/invoice/v1", "lowercase")]
	[InlineData("https://meta.fuxion.nexus/erp/Invoice/v1", "kebab-case")]
	[InlineData("https://meta.fuxion.nexus/erp/invoice/1.0.0", "major version")]
	[InlineData("https://meta.fuxion.nexus/erp/invoice/v01", "major version")]
	[InlineData("https://meta.fuxion.nexus/v1", "at least one segment")]
	[InlineData("https://meta.fuxion.nexus:443/erp/invoice/v1", "port")]
	[InlineData("https://meta.fuxion.nexus/erp/invoice/v1/", "trailing slash")]
	[InlineData("https://meta.fuxion.nexus/erp/invoice/v1?a=b", "query")]
	[InlineData("https://meta.fuxion.nexus/system/list/v1[https://meta.fuxion.nexus/erp/invoice/v1]", "kebab-case")]
	public void InvalidKey(string key, string reason)
	{
		var diagnostic = Single(Generate($$"""
			[UriKey("{{key}}", "1.0.0")] public class Invoice { }
			{{Holder}}
			"""), "FXTY001");
		Assert.Contains(reason, diagnostic.GetMessage());
		Assert.Contains("Invoice", diagnostic.GetMessage());
	}

	[Theory(DisplayName = "FXTY002: a revision that is not a semantic version, or not of the major version of its key")]
	[InlineData("1.0", "semantic version")]
	[InlineData("v1.0.0", "semantic version")]
	[InlineData("2.0.0", "v1")]
	[InlineData("0.9.0", "v1")]
	public void InvalidRevision(string revision, string reason)
	{
		var diagnostic = Single(Generate($$"""
			[UriKey("{{Root}}erp/invoice/v1", "{{revision}}")] public class Invoice { }
			{{Holder}}
			"""), "FXTY002");
		Assert.Contains(reason, diagnostic.GetMessage());
	}

	[Fact(DisplayName = "FXTY003: a base or an interface with a type argument that has no key")]
	public void UnnameableReference()
	{
		var run = Generate($$"""
			[UriKey("{{Root}}sample/box/v1", "1.0.0")] public class Box<T> { }
			[UriKey("{{Root}}sample/holder/v1", "1.0.0")] public interface IHolder<T> { }
			public class Unkeyed { }
			[UriKey("{{Root}}erp/invoice/v1", "1.0.0")] public class Invoice : Box<Unkeyed>, IHolder<List<Unkeyed>> { }
			{{Holder}}
			""");
		var diagnostics = run.Diagnostics.Where(diagnostic => diagnostic.Id == "FXTY003").Select(diagnostic => diagnostic.GetMessage()).ToArray();
		Assert.Equal(2, diagnostics.Length);
		Assert.Contains(diagnostics, message => message.Contains("base") && message.Contains("Unkeyed"));
		Assert.Contains(diagnostics, message => message.Contains("interface") && message.Contains("Unkeyed"));
		Assert.DoesNotContain("erp/invoice", run.Generated);
	}

	[Fact(DisplayName = "FXTY004: the same key on two types, also with different arities")]
	public void RepeatedKey()
	{
		var run = Generate($$"""
			[UriKey("{{Root}}sample/response/v1", "1.0.0")] public class Response { }
			[UriKey("{{Root}}sample/response/v1", "1.0.0")] public class Response<T> { }
			{{Holder}}
			""");
		Assert.Equal(2, run.Diagnostics.Count(diagnostic => diagnostic.Id == "FXTY004"));
		Assert.DoesNotContain("sample/response", run.Generated);
	}

	[Theory(DisplayName = "FXTY005: a type with a key that the generated class cannot reach")]
	[InlineData("public class Outer { [UriKey(\"https://meta.fuxion.nexus/sample/inner/v1\", \"1.0.0\")] private class Inner { } }", "private")]
	[InlineData("public class Outer { [UriKey(\"https://meta.fuxion.nexus/sample/inner/v1\", \"1.0.0\")] protected class Inner { } }", "protected")]
	[InlineData("public class Outer<T> { [UriKey(\"https://meta.fuxion.nexus/sample/inner/v1\", \"1.0.0\")] public class Inner { } }", "generic")]
	public void UnreachableType(string source, string reason)
	{
		var diagnostic = Single(Generate(source + "\n" + Holder), "FXTY005");
		Assert.Contains(reason, diagnostic.GetMessage());
	}

	[Theory(DisplayName = "FXTY006: a class with [TypeDefinitions] that is not static and partial, or is nested or generic")]
	[InlineData("[TypeDefinitions] internal static class SampleTypes { }", "static and partial")]
	[InlineData("[TypeDefinitions] internal partial class SampleTypes { }", "static and partial")]
	[InlineData("public static partial class Outer { [TypeDefinitions] internal static partial class SampleTypes { } }", "nested")]
	[InlineData("[TypeDefinitions] internal static partial class SampleTypes<T> { }", "generic")]
	public void InvalidDefinitionsClass(string source, string reason)
	{
		var run = Generate($$"""
			[UriKey("{{Root}}erp/invoice/v1", "1.0.0")] public class Invoice { }
			{{source}}
			""");
		var diagnostic = Single(run, "FXTY006");
		Assert.Contains(reason, diagnostic.GetMessage());
		Assert.Null(run.Generated);
	}

	[Fact(DisplayName = "The generator and Fuxion name the types of the system the same way")]
	public void SystemTypesAgree()
	{
		var run = GenerateClean($$"""
			[UriKey("{{Root}}sample/holder/v1", "1.0.0")] public interface IHolder<T> { }
			[UriKey("{{Root}}sample/all/v1", "1.0.0")] public class All :
				IHolder<bool>, IHolder<byte>, IHolder<sbyte>, IHolder<short>, IHolder<ushort>, IHolder<int>, IHolder<uint>, IHolder<long>,
				IHolder<ulong>, IHolder<float>, IHolder<double>, IHolder<decimal>, IHolder<char>, IHolder<string>, IHolder<object>,
				IHolder<System.DateTime>, IHolder<System.DateTimeOffset>, IHolder<System.DateOnly>, IHolder<System.TimeOnly>,
				IHolder<System.TimeSpan>, IHolder<System.Guid>, IHolder<System.Uri>, IHolder<int[]>, IHolder<List<int>>,
				IHolder<Dictionary<int, int>> { }
			{{Holder}}
			""");
		var arguments = SystemTypes.All.Select(definition => definition.Arity switch
		{
			0 => definition.Key,
			1 => definition.Key.Construct(SystemTypes.Int.Key),
			_ => definition.Key.Construct(SystemTypes.Int.Key, SystemTypes.Int.Key)
		});
		foreach (var argument in arguments)
			Assert.Contains($"global::Fuxion.Types.UriKey.Parse(\"{Root}sample/holder/v1[{argument}]\")", run.Generated);
	}
}
