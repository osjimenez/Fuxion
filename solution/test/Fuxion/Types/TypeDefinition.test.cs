using System;
using System.Linq;
using System.Text.Json;
using Fuxion.Types;
using Xunit;

namespace Test.Fuxion.Types;

public class TypeDefinitionTest
{
	static readonly UriKey Invoice = UriKey.Parse("https://meta.fuxion.nexus/erp/invoice/v1");
	static readonly UriKey Document = UriKey.Parse("https://meta.fuxion.nexus/erp/document/v1");
	static readonly UriKey Named = UriKey.Parse("https://meta.fuxion.nexus/erp/named/v1");
	static readonly UriKey Holder = UriKey.Parse("https://meta.fuxion.nexus/erp/holder/v1");

	[Fact(DisplayName = "A definition has its key, its revision, its arity, its base, its interfaces and its schema")]
	public void Complete()
	{
		using var schema = JsonDocument.Parse("""{"type":"object","properties":{"number":{"type":"string"}}}""");
		var holderOfString = Holder.Construct(SystemTypes.String.Key);
		var definition = new TypeDefinition(Invoice, new("1.3.0-preview.2"), 0, Document, [Named, holderOfString], schema.RootElement);
		Assert.Equal(Invoice, definition.Key);
		Assert.Equal("1.3.0-preview.2", definition.Revision.ToString());
		Assert.Equal(0, definition.Arity);
		Assert.Equal(Document, definition.Base);
		Assert.Equal(new[] { Named, holderOfString }, definition.Interfaces);
		Assert.Equal("https://meta.fuxion.nexus/erp/invoice/v1 1.3.0-preview.2", definition.ToString());
		// The schema outlives the document it came from
		schema.Dispose();
		Assert.Equal("object", definition.Schema!.Value.GetProperty("type").GetString());
	}

	[Fact(DisplayName = "The schema can be empty, or a boolean")]
	public void Schema()
	{
		Assert.Null(new TypeDefinition(Invoice, new("1.0.0")).Schema);
		using var always = JsonDocument.Parse("true");
		Assert.Equal(JsonValueKind.True, new TypeDefinition(Invoice, new("1.0.0"), schema: always.RootElement).Schema!.Value.ValueKind);
		using var array = JsonDocument.Parse("[]");
		Assert.Throws<ArgumentException>(() => new TypeDefinition(Invoice, new("1.0.0"), schema: array.RootElement));
	}

	[Fact(DisplayName = "The base of a generic definition can be a construction")]
	public void GenericBase()
	{
		var box = UriKey.Parse("https://meta.fuxion.nexus/erp/box/v1");
		var definition = new TypeDefinition(Invoice, new("1.0.0"), @base: box.Construct(SystemTypes.List.Key.Construct(Document)));
		Assert.Equal("https://meta.fuxion.nexus/erp/box/v1[https://meta.fuxion.nexus/system/list/v1[https://meta.fuxion.nexus/erp/document/v1]]", definition.Base!.ToString());
	}

	[Fact(DisplayName = "What does not fit is rejected")]
	public void Rejected()
	{
		// The key of a definition is a key, not a construction
		Assert.Throws<ArgumentException>(() => new TypeDefinition(Holder.Construct(Invoice), new("1.0.0"), 1));
		// The revision is of the major version of its key
		var exception = Assert.Throws<ArgumentException>(() => new TypeDefinition(Invoice, new("2.0.0")));
		Assert.Contains("v1", exception.Message);
		Assert.Throws<ArgumentException>(() => new TypeDefinition(Invoice, new("0.9.0")));
		Assert.Throws<ArgumentOutOfRangeException>(() => new TypeDefinition(Invoice, new("1.0.0"), -1));
		// A type is neither its own base nor its own interface
		Assert.Throws<ArgumentException>(() => new TypeDefinition(Holder, new("1.0.0"), 1, Holder.Construct(Invoice)));
		Assert.Throws<ArgumentException>(() => new TypeDefinition(Invoice, new("1.0.0"), interfaces: [Invoice]));
		// An interface is not repeated, nor null
		Assert.Throws<ArgumentException>(() => new TypeDefinition(Invoice, new("1.0.0"), interfaces: [Named, Named]));
		Assert.Throws<ArgumentNullException>(() => new TypeDefinition(Invoice, new("1.0.0"), interfaces: new UriKey[] { null! }));
		Assert.Throws<ArgumentNullException>(() => new TypeDefinition(null!, new("1.0.0")));
		Assert.Throws<ArgumentNullException>(() => new TypeDefinition(Invoice, null!));
	}

	[Fact(DisplayName = "The types of the system are under meta.fuxion.nexus/system, revision 1.0.0")]
	public void SystemTypesAreDefinitions()
	{
		Assert.All(SystemTypes.All, definition =>
		{
			Assert.StartsWith("https://meta.fuxion.nexus/system/", definition.Key.ToString());
			Assert.Equal("1.0.0", definition.Revision.ToString());
		});
		Assert.Equal(SystemTypes.All.Count, SystemTypes.All.Select(definition => definition.Key).Distinct().Count());
		Assert.Equal("https://meta.fuxion.nexus/system/bool/v1", SystemTypes.Bool.Key.ToString());
		Assert.Equal("https://meta.fuxion.nexus/system/date-time-offset/v1", SystemTypes.DateTimeOffset.Key.ToString());
		Assert.Equal(1, SystemTypes.Array.Arity);
		Assert.Equal(1, SystemTypes.List.Arity);
		Assert.Equal(2, SystemTypes.Dictionary.Arity);
	}
}
