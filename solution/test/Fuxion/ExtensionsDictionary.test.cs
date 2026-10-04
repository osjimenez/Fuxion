using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

// The mutable dictionary used to build extensions before they become part of a value (ImmutableExtensions).
public class ExtensionsDictionaryTest(ITestOutputHelper output) : BaseTest<ExtensionsDictionaryTest>(output)
{
	static ExtensionsDictionary Create() => new(new HashSet<string>(["Reserved"], StringComparer.OrdinalIgnoreCase)) { ["a"] = 1, ["b"] = "text" };

	[Fact(DisplayName = "A reserved key cannot be added, set or found, whatever its casing")]
	public void ReservedKeys()
	{
		var extensions = Create();

		Throws<ReservedKeyExtensionException>(() => extensions.Add("reserved", 1));
		Throws<ReservedKeyExtensionException>(() => extensions.Add(new KeyValuePair<string, object?>("RESERVED", 1)));
		Throws<ReservedKeyExtensionException>(() => extensions["Reserved"] = 1);
		IsFalse(extensions.ContainsKey("Reserved"));
		IsFalse(extensions.Contains(new KeyValuePair<string, object?>("Reserved", 1)));
		IsFalse(extensions.TryGetValue("Reserved", out _));
		IsFalse(extensions.Remove("Reserved"));
		IsFalse(extensions.Remove(new KeyValuePair<string, object?>("Reserved", 1)));
		Throws<KeyNotFoundException>(() => extensions.GetAs<int>("Reserved"));
		IsFalse(extensions.TryGetAs<int>("Reserved", out _));
	}

	[Fact(DisplayName = "It behaves as a dictionary: keys, values, count, contains, copy, remove and clear")]
	public void Dictionary()
	{
		var extensions = Create();

		Assert.Equal(2, extensions.Count);
		Assert.Equal(new[] { "a", "b" }, extensions.Keys.OrderBy(k => k).ToArray());
		Assert.Contains("text", extensions.Values);
		IsTrue(extensions.ContainsKey("a"));
		IsTrue(extensions.Contains(new KeyValuePair<string, object?>("a", 1)));
		IsFalse(extensions.IsReadOnly);
		IsTrue(extensions.TryGetValue("b", out var b));
		Assert.Equal("text", b);

		var copy = new KeyValuePair<string, object?>[3];
		extensions.CopyTo(copy, 1);
		Assert.Equal(new[] { "a", "b" }, copy.Skip(1).Select(p => p.Key).OrderBy(k => k).ToArray());
		Throws<ArgumentException>(() => extensions.CopyTo(new KeyValuePair<string, object?>[2], 1));
		Throws<ArgumentOutOfRangeException>(() => extensions.CopyTo(copy, -1));

		extensions.Add(new KeyValuePair<string, object?>("c", 3));
		IsTrue(extensions.Remove(new KeyValuePair<string, object?>("c", 3)));
		IsTrue(extensions.Remove("a"));
		Assert.Single(extensions);
		extensions.Clear();
		Assert.Empty(extensions);
	}

	[Fact(DisplayName = "GetAs and TryGetAs read typed values, also from JSON elements")]
	public void GetAs()
	{
		var extensions = Create();
		extensions["json"] = JsonDocument.Parse("42").RootElement.Clone();
		extensions["null"] = null;

		Assert.Equal(1, extensions.GetAs<int>("a"));
		Assert.Equal(42, extensions.GetAs<int>("json"));
		Assert.Equal(0, extensions.GetAs<int>("null"));
		Throws<InvalidCastException>(() => extensions.GetAs<int>("b"));
		Throws<KeyNotFoundException>(() => extensions.GetAs<int>("missing"));
		IsTrue(extensions.TryGetAs<int>("json", out var json));
		Assert.Equal(42, json);
		IsFalse(extensions.TryGetAs<int>("b", out _));
	}

	[Fact(DisplayName = "The typed copy keeps the entries and the reserved keys of the original")]
	public void TypedCopy()
	{
		var original = Create();
		var copy = new ExtensionsDictionary<ExtensionsDictionaryTest>(original);
		original["c"] = 3;

		Assert.Equal(2, copy.Count);
		Throws<ReservedKeyExtensionException>(() => copy.Add("reserved", 1));
	}
}
