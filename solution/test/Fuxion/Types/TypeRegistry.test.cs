using System;
using System.Collections.Generic;
using System.Linq;
using Fuxion.Types;
using Xunit;

namespace Test.Fuxion.Types;

public class TypeRegistryTest
{
	const string SystemRoot = "https://meta.fuxion.nexus/system/";
	static readonly UriKey Box = UriKey.Parse("https://meta.fuxion.nexus/test/registry-box/v1");
	static readonly UriKey Item = UriKey.Parse("https://meta.fuxion.nexus/test/registry-item/v1");

	sealed class RegistryBox<T>;
	sealed class RegistryItem;

	static TypeRegistry WithBoxAndItem()
	{
		var registry = new TypeRegistry();
		registry.Register(typeof(RegistryBox<>), new(Box, new("1.0.0"), 1));
		registry.Register<RegistryItem>(new(Item, new("1.0.0")));
		return registry;
	}

	[Fact(DisplayName = "A registry starts with the types of the system")]
	public void SystemTypesInside()
	{
		var registry = new TypeRegistry();
		Assert.All(SystemTypes.All, definition => Assert.True(registry.TryGetDefinition(definition.Key, out _)));
		Assert.Equal(SystemTypes.All.Count, registry.Definitions.Count);
		Assert.Equal(SystemTypes.Int.Key, registry.GetKey<int>());
		Assert.Equal(SystemTypes.String.Key, registry.GetKey<string>());
		Assert.Equal(SystemTypes.Guid.Key, registry.GetKey<Guid>());
		Assert.Equal(SystemTypes.Uri.Key, registry.GetKey<Uri>());
#if !STANDARD_OR_OLD_FRAMEWORKS
		Assert.Equal(SystemTypes.DateOnly.Key, registry.GetKey<DateOnly>());
#endif
		Assert.True(registry.TryGetType(SystemTypes.List.Key, out var list) && list == typeof(List<>));
		Assert.True(registry.TryGetType(SystemTypes.Bool.Key, out var @bool) && @bool == typeof(bool));
		// The arrays have no .NET type of their own
		Assert.False(registry.TryGetType(SystemTypes.Array.Key, out _));
	}

	[Fact(DisplayName = "A constructed generic type or an array gives its construction, without registering it")]
	public void Constructions()
	{
		var registry = new TypeRegistry();
		Assert.Equal($"{SystemRoot}array/v1[{SystemRoot}int/v1]", registry.GetKey<int[]>().ToString());
		Assert.Equal($"{SystemRoot}list/v1[{SystemRoot}string/v1]", registry.GetKey<List<string>>().ToString());
		Assert.Equal($"{SystemRoot}dictionary/v1[{SystemRoot}string/v1,{SystemRoot}array/v1[{SystemRoot}list/v1[{SystemRoot}bool/v1]]]",
			registry.GetKey<Dictionary<string, List<bool>[]>>().ToString());
		// A multidimensional array, an open type or a type without a key have none
		Assert.False(registry.TryGetKey(typeof(int[,]), out _));
		Assert.False(registry.TryGetKey(typeof(List<>).GetGenericArguments()[0], out _));
		Assert.False(registry.TryGetKey(typeof(List<RegistryItem>), out _));
		Assert.Throws<KeyNotFoundException>(() => registry.GetKey<RegistryItem>());
		// Without registering it, a construction has no .NET type
		Assert.False(registry.TryGetType(registry.GetKey<List<string>>(), out _));
	}

	[Fact(DisplayName = "A type is registered with its definition, generic or not")]
	public void Register()
	{
		var registry = WithBoxAndItem();
		Assert.Equal(Item, registry.GetKey<RegistryItem>());
		Assert.True(registry.TryGetType(Item, out var item) && item == typeof(RegistryItem));
		Assert.True(registry.TryGetType(Box, out var box) && box == typeof(RegistryBox<>));
		Assert.Equal(Box.Construct(Item), registry.GetKey<RegistryBox<RegistryItem>>());
		Assert.Equal(Box.Construct(SystemTypes.Array.Key.Construct(Item)), registry.GetKey<RegistryBox<RegistryItem[]>>());
		Assert.True(registry.TryGetDefinition(Box.Construct(Item), out var definition) && definition.Key == Box);
	}

	[Fact(DisplayName = "A construction is registered to give its .NET type back")]
	public void RegisterConstruction()
	{
		var registry = WithBoxAndItem();
		var key = registry.RegisterConstruction<RegistryBox<RegistryItem>>();
		Assert.Equal(Box.Construct(Item), key);
		Assert.True(registry.TryGetType(key, out var type) && type == typeof(RegistryBox<RegistryItem>));
		Assert.True(registry.TryGetKeyByFingerprint(key.Fingerprint, out var byFingerprint) && byFingerprint == key);
		// Again, the same: nothing changes
		Assert.Equal(key, registry.RegisterConstruction(typeof(RegistryBox<RegistryItem>)));
		var array = registry.RegisterConstruction<int[]>();
		Assert.True(registry.TryGetType(array, out var arrayType) && arrayType == typeof(int[]));
		// Not a construction, an open one, or one with an argument without a key
		Assert.Throws<ArgumentException>(() => registry.RegisterConstruction<RegistryItem>());
		Assert.Throws<ArgumentException>(() => registry.RegisterConstruction(typeof(RegistryBox<>)));
		Assert.Throws<ArgumentException>(() => registry.RegisterConstruction<RegistryBox<TypeRegistryTest>>());
	}

	[Fact(DisplayName = "A definition without a .NET type is known by its key")]
	public void WithoutType()
	{
		var registry = new TypeRegistry();
		var adr = UriKey.Parse("https://meta.fuxion.nexus/docs/adr/v1");
		registry.Register(new TypeDefinition(adr, new("1.0.0")));
		Assert.True(registry.TryGetDefinition(adr, out var definition) && definition.Revision.ToString() == "1.0.0");
		Assert.False(registry.TryGetType(adr, out _));
		Assert.True(registry.TryGetKeyByFingerprint(adr.Fingerprint, out var byFingerprint) && byFingerprint == adr);
		Assert.True(registry.IsValid(adr, out _));
	}

	[Fact(DisplayName = "A key, or a type, is registered once")]
	public void Repeated()
	{
		var registry = WithBoxAndItem();
		Assert.Throws<InvalidOperationException>(() => registry.Register(new TypeDefinition(Item, new("1.1.0"))));
		Assert.Throws<InvalidOperationException>(() => registry.Register<RegistryItem>(new(UriKey.Parse("https://meta.fuxion.nexus/test/other/v1"), new("1.0.0"))));
		Assert.Throws<InvalidOperationException>(() => registry.Register<int>(new(UriKey.Parse("https://meta.fuxion.nexus/test/integer/v1"), new("1.0.0"))));
		Assert.Throws<InvalidOperationException>(() => registry.Register(SystemTypes.Bool));
	}

	[Fact(DisplayName = "A type goes with a definition of its arity, and not as a construction")]
	public void Arity()
	{
		var registry = new TypeRegistry();
		Assert.Throws<ArgumentException>(() => registry.Register(typeof(RegistryBox<>), new(Box, new("1.0.0"))));
		Assert.Throws<ArgumentException>(() => registry.Register<RegistryItem>(new(Item, new("1.0.0"), 1)));
		Assert.Throws<ArgumentException>(() => registry.Register<RegistryBox<int>>(new(Box, new("1.0.0"), 1)));
		Assert.Throws<ArgumentException>(() => registry.Register<int[]>(new(Box, new("1.0.0"), 1)));
	}

	[Fact(DisplayName = "A construction is valid with the arity of its definition; a key alone, always")]
	public void Valid()
	{
		var registry = WithBoxAndItem();
		Assert.True(registry.IsValid(Box.Construct(Item), out _));
		Assert.True(registry.IsValid(Box, out _));
		Assert.True(registry.IsValid(SystemTypes.Dictionary.Key.Construct(SystemTypes.String.Key, Box.Construct(Item)), out _));
		Assert.False(registry.IsValid(Box.Construct(Item, Item), out var error));
		Assert.Contains("takes 1 type arguments, not 2", error);
		Assert.False(registry.IsValid(Item.Construct(Item), out error));
		Assert.Contains("takes 0 type arguments, not 1", error);
		Assert.False(registry.IsValid(Box.Construct(UriKey.Parse("https://meta.fuxion.nexus/test/unknown/v1")), out error));
		Assert.Contains("has no definition here", error);
	}

	[Fact(DisplayName = "The definitions of a registry include those registered")]
	public void Definitions()
	{
		var registry = WithBoxAndItem();
		Assert.Equal(SystemTypes.All.Count + 2, registry.Definitions.Count);
		Assert.Contains(registry.Definitions, definition => definition.Key == Box && definition.Arity == 1);
	}
}
