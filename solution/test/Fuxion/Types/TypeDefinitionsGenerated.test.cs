using System.Collections.Generic;
using System.Linq;
using Fuxion.Types;
using Xunit;

namespace Test.Fuxion.Types;

// Types with a key of this assembly: the generator of Fuxion writes their definitions in GeneratedTypes, and the code to
// register them, at compile time
[UriKey("https://meta.fuxion.nexus/test/document/v1", "1.0.0")]
public abstract class GeneratedDocument;

[UriKey("https://meta.fuxion.nexus/test/named/v1", "1.0.0")]
public interface IGeneratedNamed;

[UriKey("https://meta.fuxion.nexus/test/adr/v1", "1.2.0")]
public sealed class GeneratedAdr : GeneratedDocument, IGeneratedNamed;

[UriKey("https://meta.fuxion.nexus/test/holder/v1", "1.0.0")]
public interface IGeneratedHolder<T>;

[UriKey("https://meta.fuxion.nexus/test/box/v2", "2.0.0-preview.1")]
public class GeneratedBox<T> : IGeneratedHolder<T>;

[UriKey("https://meta.fuxion.nexus/test/adrs/v1", "1.0.0")]
public class GeneratedAdrs : GeneratedBox<List<GeneratedAdr>>;

[TypeDefinitions]
internal static partial class GeneratedTypes;

public class TypeDefinitionsGeneratedTest
{
	static TypeRegistry Registry()
	{
		var registry = new TypeRegistry();
		GeneratedTypes.Register(registry);
		return registry;
	}

	static UriKey Key(string path) => UriKey.Parse($"https://meta.fuxion.nexus/{path}");

	[Fact(DisplayName = "The generated definitions are those of the types with a key, in the order of their keys")]
	public void Definitions()
	{
		Assert.Equal(
			new[] { "test/adr/v1", "test/adrs/v1", "test/box/v2", "test/document/v1", "test/holder/v1", "test/named/v1" }.Select(Key),
			GeneratedTypes.Definitions.Select(definition => definition.Key));
		var adr = GeneratedTypes.Definitions[0];
		Assert.Equal("1.2.0", adr.Revision.ToString());
		Assert.Equal(0, adr.Arity);
		Assert.Equal(Key("test/document/v1"), adr.Base);
		Assert.Equal(new[] { Key("test/named/v1") }, adr.Interfaces);
		Assert.Null(adr.Schema);
	}

	[Fact(DisplayName = "A generic type has its arity, and its interface on its parameter, by its key alone")]
	public void Generic()
	{
		var box = GeneratedTypes.Definitions.Single(definition => definition.Key == Key("test/box/v2"));
		Assert.Equal(1, box.Arity);
		Assert.Equal("2.0.0-preview.1", box.Revision.ToString());
		Assert.Equal(new[] { Key("test/holder/v1") }, box.Interfaces);
		var adrs = GeneratedTypes.Definitions.Single(definition => definition.Key == Key("test/adrs/v1"));
		Assert.Equal("https://meta.fuxion.nexus/test/box/v2[https://meta.fuxion.nexus/system/list/v1[https://meta.fuxion.nexus/test/adr/v1]]", adrs.Base!.ToString());
		Assert.Equal(new[] { Key("test/holder/v1").Construct(SystemTypes.List.Key.Construct(Key("test/adr/v1"))) }, adrs.Interfaces);
	}

	[Fact(DisplayName = "The generated code registers each type with its definition")]
	public void Register()
	{
		var registry = Registry();
		Assert.Equal(Key("test/adr/v1"), registry.GetKey<GeneratedAdr>());
		Assert.Equal(Key("test/box/v2").Construct(Key("test/adr/v1")), registry.GetKey<GeneratedBox<GeneratedAdr>>());
		Assert.Equal(Key("test/holder/v1").Construct(SystemTypes.Int.Key), registry.GetKey<IGeneratedHolder<int>>());
		Assert.True(registry.TryGetType(Key("test/box/v2"), out var box) && box == typeof(GeneratedBox<>));
		Assert.True(registry.TryGetDefinition(Key("test/adr/v1"), out var adr) && adr.Base == Key("test/document/v1"));
		var construction = registry.RegisterConstruction<GeneratedBox<GeneratedAdr>>();
		Assert.True(registry.TryGetType(construction, out var type) && type == typeof(GeneratedBox<GeneratedAdr>));
		Assert.True(registry.IsValid(construction, out _));
	}
}
