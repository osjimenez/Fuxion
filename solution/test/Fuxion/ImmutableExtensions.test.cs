using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ImmutableExtensionsTest(ITestOutputHelper output) : BaseTest<ImmutableExtensionsTest>(output)
{
	static readonly HashSet<string> Reserved = new(["Reserved"], StringComparer.OrdinalIgnoreCase);

	static ImmutableExtensions<ImmutableExtensionsTest> Create(params (string Key, object? Value)[] items)
		=> ImmutableExtensions<ImmutableExtensionsTest>.For(Reserved, items.Select(i => new KeyValuePair<string, object?>(i.Key, i.Value)));

	[Fact(DisplayName = "Creating from a dictionary copies it: changing the dictionary afterwards does not change the extensions")]
	public void Create_CopiesTheSource()
	{
		var source = new ExtensionsDictionary { ["a"] = 1 };
		ImmutableExtensions<ImmutableExtensionsTest> extensions = source;
		source["b"] = 2;
		source["a"] = 3;

		Assert.Equal(1, extensions.Count);
		Assert.Equal(1, extensions["a"]);
	}

	[Fact(DisplayName = "A reserved key is rejected when creating the extensions and when adding one")]
	public void ReservedKey_Throws()
	{
		Throws<ReservedKeyExtensionException>(() => Create(("reserved", 1)));
		Throws<ReservedKeyExtensionException>(() => Create().With("RESERVED", 1));
		Throws<ReservedKeyExtensionException>(() => Create().With([new("Reserved", 1)]));
	}

	[Fact(DisplayName = "With returns new extensions and leaves the original untouched")]
	public void With_ReturnsACopy()
	{
		var original = Create(("a", 1));

		var added = original.With("b", 2);
		var replaced = original.With("a", 10);
		var many = original.With([new("c", 3), new("d", 4)]);

		Assert.Equal(1, original.Count);
		Assert.Equal(1, original["a"]);
		Assert.Equal(2, added.Count);
		Assert.Equal(2, added["b"]);
		Assert.Equal(10, replaced["a"]);
		Assert.Equal(3, many.Count);
		Assert.Equal(4, many["d"]);
	}

	[Fact(DisplayName = "Without returns new extensions without the key, or the same ones when the key is not there")]
	public void Without_ReturnsACopy()
	{
		var original = Create(("a", 1), ("b", 2));

		var removed = original.Without("a");

		Assert.Equal(2, original.Count);
		Assert.Equal(1, removed.Count);
		IsFalse(removed.ContainsKey("a"));
		Assert.Same(original, original.Without("missing"));
	}

	[Fact(DisplayName = "The extensions are read like a read-only dictionary")]
	public void Read()
	{
		var extensions = Create(("a", 1), ("b", "text"));

		Assert.Equal(2, extensions.Count);
		IsTrue(extensions.ContainsKey("a"));
		IsTrue(extensions.TryGetValue("b", out var b));
		Assert.Equal("text", b);
		IsFalse(extensions.TryGetValue("missing", out _));
		Throws<KeyNotFoundException>(() => _ = extensions["missing"]);
		Assert.Equal(new[] { "a", "b" }, extensions.Keys.OrderBy(k => k).ToArray());
		Assert.Equal(2, extensions.Count());
	}

	[Fact(DisplayName = "GetAs and TryGetAs read typed values, also from JSON elements")]
	public void GetAs()
	{
		var json = JsonDocument.Parse("""{"Number":42}""").RootElement.Clone();
		var extensions = Create(("typed", 7), ("json", json), ("text", "x"));

		Assert.Equal(7, extensions.GetAs<int>("typed"));
		Assert.Equal(42, extensions.GetAs<JsonPayload>("json")!.Number);
		IsTrue(extensions.TryGetAs<JsonPayload>("json", out var payload));
		Assert.Equal(42, payload.Number);
		IsFalse(extensions.TryGetAs<JsonPayload>("text", out _));
		IsFalse(extensions.TryGetAs<int>("missing", out _));
		Throws<KeyNotFoundException>(() => extensions.GetAs<int>("missing"));
		Throws<InvalidCastException>(() => extensions.GetAs<JsonPayload>("text"));
	}

	[Fact(DisplayName = "Two extensions with the same keys and equal values are equal, whatever the order")]
	public void Equality_ByContent()
	{
		var first = Create(("a", 1), ("b", "x"));
		var second = Create(("b", "x"), ("a", 1));

		IsTrue(first.Equals(second));
		IsTrue(first == second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		IsFalse(first == Create(("a", 1)));
		IsFalse(first == Create(("a", 1), ("b", "y")));
		IsTrue(first != Create(("a", 2), ("b", "x")));
	}

	[Fact(DisplayName = "Empty extensions are one shared instance per set of reserved keys")]
	public void Empty_IsShared()
	{
		Assert.Same(ImmutableExtensions<ImmutableExtensionsTest>.Empty(Reserved), ImmutableExtensions<ImmutableExtensionsTest>.Empty(Reserved));
		Assert.Same(ImmutableExtensions<ImmutableExtensionsTest>.Empty(Reserved), Create());
		Assert.Equal(0, Create().Count);
	}

	[Fact(DisplayName = "The extensions expose no way to change them")]
	public void NoMutators()
	{
		var type = typeof(ImmutableExtensions<ImmutableExtensionsTest>);
		var methods = new HashSet<string>(type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name));

		IsFalse(methods.Contains("Add"));
		IsFalse(methods.Contains("Remove"));
		IsFalse(methods.Contains("Clear"));
		IsFalse(type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.GetIndexParameters().Length > 0 && p.CanWrite));
	}

	sealed class JsonPayload
	{
		public int Number { get; init; }
	}
}
