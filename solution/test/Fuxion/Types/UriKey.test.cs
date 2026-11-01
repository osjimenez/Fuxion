using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fuxion.Types;
using Xunit;

namespace Test.Fuxion.Types;

public class UriKeyTest
{
	const string Bool = "https://meta.fuxion.nexus/system/bool/v1";
	const string String = "https://meta.fuxion.nexus/system/string/v1";
	const string List = "https://meta.fuxion.nexus/system/list/v1";
	const string Dictionary = "https://meta.fuxion.nexus/system/dictionary/v1";
	const string Invoice = "https://meta.fuxion.nexus/erp/invoice/v1";

	[Theory(DisplayName = "A key in its canonical form is read as it is")]
	[InlineData(Bool, 1u, "https://meta.fuxion.nexus/system/bool")]
	[InlineData("https://meta.fuxion.nexus/docs/adr/v0", 0u, "https://meta.fuxion.nexus/docs/adr")]
	[InlineData("https://acme.example/api/v2/add-customer-command/v12", 12u, "https://acme.example/api/v2/add-customer-command")]
	[InlineData("https://xn--bcher-kva.example/a1/b-2-c/v4294967295", 4294967295u, "https://xn--bcher-kva.example/a1/b-2-c")]
	[InlineData("https://localhost/x/v1", 1u, "https://localhost/x")]
	public void Key(string text, uint major, string lineage)
	{
		var key = UriKey.Parse(text);
		Assert.Equal(text, key.ToString());
		Assert.False(key.IsConstruction);
		Assert.Empty(key.Arguments);
		Assert.Same(key, key.Key);
		Assert.Equal(major, key.Major);
		Assert.Equal(lineage, key.Lineage);
	}

	[Theory(DisplayName = "Anything else is rejected, never normalized, and says why")]
	[InlineData("", "cannot be empty")]
	[InlineData("http://meta.fuxion.nexus/system/bool/v1", "starts with 'https://'")]
	[InlineData("HTTPS://meta.fuxion.nexus/system/bool/v1", "starts with 'https://'")]
	[InlineData("https://Meta.fuxion.nexus/system/bool/v1", "lowercase")]
	[InlineData("https://meta.fuxion.nexus:443/system/bool/v1", "no port")]
	[InlineData("https://user@meta.fuxion.nexus/system/bool/v1", "no user information")]
	[InlineData("https://bücher.example/system/bool/v1", "punycode")]
	[InlineData("https://-meta.fuxion.nexus/system/bool/v1", "neither starts nor ends with '-'")]
	[InlineData("https://meta..fuxion.nexus/system/bool/v1", "no empty labels")]
	[InlineData("https:///system/bool/v1", "has a host")]
	[InlineData("https://meta.fuxion.nexus", "at least one segment and its major version")]
	[InlineData("https://meta.fuxion.nexus/v1", "at least one segment before its major version")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v1/", "no trailing slash")]
	[InlineData("https://meta.fuxion.nexus/system//bool/v1", "no empty segments")]
	[InlineData("https://meta.fuxion.nexus/System/bool/v1", "kebab-case")]
	[InlineData("https://meta.fuxion.nexus/system/bool_value/v1", "kebab-case")]
	[InlineData("https://meta.fuxion.nexus/system/bool--value/v1", "kebab-case")]
	[InlineData("https://meta.fuxion.nexus/system/-bool/v1", "kebab-case")]
	[InlineData("https://meta.fuxion.nexus/system/bool/1.0.0", "major version")]
	[InlineData("https://meta.fuxion.nexus/system/bool/V1", "major version")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v01", "major version")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v", "major version")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v4294967296", "at most")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v1?a=b", "no query")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v1#a", "no fragment")]
	[InlineData("https://meta.fuxion.nexus/system/b%6Fol/v1", "no percent-encoding")]
	[InlineData(" https://meta.fuxion.nexus/system/bool/v1", "starts with 'https://'")]
	[InlineData("https://meta.fuxion.nexus/system/bool/v1 ", "major version")]
	[InlineData(List + "[]", "a key is missing")]
	[InlineData(List + "[" + Invoice, "closes its arguments")]
	[InlineData(List + "[" + Invoice + "]]", "']' at position")]
	[InlineData(List + "[" + Invoice + ",]", "a key is missing")]
	[InlineData(List + "[" + Invoice + ", " + String + "]", "starts with 'https://'")]
	[InlineData(List + "[" + Invoice + "]" + "[" + Invoice + "]", "'[' at position")]
	[InlineData("[" + Invoice + "]", "a key is missing")]
	public void Rejected(string text, string reason)
	{
		Assert.False(UriKey.TryParse(text, out var key));
		Assert.Null(key);
		var exception = Assert.Throws<FormatException>(() => UriKey.Parse(text));
		Assert.Contains(reason, exception.Message);
	}

	[Fact(DisplayName = "A null text is not a UriKey")]
	public void Null()
	{
		Assert.False(UriKey.TryParse(null, out _));
		Assert.Throws<ArgumentNullException>(() => UriKey.Parse(null!));
	}

	[Fact(DisplayName = "A construction has its key and its arguments, in order, and nests")]
	public void Construction()
	{
		var text = $"{Dictionary}[{String},{List}[{Invoice}]]";
		var construction = UriKey.Parse(text);
		Assert.Equal(text, construction.ToString());
		Assert.True(construction.IsConstruction);
		Assert.Equal(Dictionary, construction.Key.ToString());
		Assert.False(construction.Key.IsConstruction);
		Assert.Equal(1u, construction.Major);
		Assert.Equal("https://meta.fuxion.nexus/system/dictionary", construction.Lineage);
		Assert.Equal(new[] { String, $"{List}[{Invoice}]" }, construction.Arguments.Select(argument => argument.ToString()));
		Assert.Equal(new[] { Invoice }, construction.Arguments[1].Arguments.Select(argument => argument.ToString()));
	}

	[Fact(DisplayName = "Construct gives the same construction as its text")]
	public void Construct()
	{
		var invoice = UriKey.Parse(Invoice);
		var built = UriKey.Parse(Dictionary).Construct(UriKey.Parse(String), UriKey.Parse(List).Construct(invoice));
		Assert.Equal(UriKey.Parse($"{Dictionary}[{String},{List}[{Invoice}]]"), built);
		Assert.Equal($"{Dictionary}[{String},{List}[{Invoice}]]", built.ToString());
		Assert.Throws<InvalidOperationException>(() => built.Construct(invoice));
		Assert.Throws<ArgumentException>(() => invoice.Construct());
		Assert.Throws<ArgumentException>(() => invoice.Construct(null!, invoice));
	}

	[Fact(DisplayName = "A construction nests 64 levels at most")]
	public void Depth()
	{
		string Nested(int levels) => levels == 0 ? Bool : $"{List}[{Nested(levels - 1)}]";
		Assert.True(UriKey.TryParse(Nested(UriKey.MaxDepth), out _));
		var exception = Assert.Throws<FormatException>(() => UriKey.Parse(Nested(UriKey.MaxDepth + 1)));
		Assert.Contains("64 levels at most", exception.Message);
		var deepest = UriKey.Parse(Nested(UriKey.MaxDepth));
		Assert.Throws<ArgumentException>(() => UriKey.Parse(List).Construct(deepest));
		// A text received cannot exhaust the stack
		Assert.False(UriKey.TryParse(string.Concat(Enumerable.Repeat(List + "[", 100_000)), out _));
	}

	[Fact(DisplayName = "Two UriKey are equal when their texts are, character by character")]
	public void Equality()
	{
		var one = UriKey.Parse($"{List}[{Invoice}]");
		var other = UriKey.Parse(List).Construct(UriKey.Parse(Invoice));
		Assert.Equal(one, other);
		Assert.True(one == other);
		Assert.False(one != other);
		Assert.Equal(one.GetHashCode(), other.GetHashCode());
		// The arguments count: List<Invoice> is not List<string>, nor the list alone
		Assert.NotEqual(one, UriKey.Parse($"{List}[{String}]"));
		Assert.NotEqual(one, UriKey.Parse(List));
		// Another major version is another type
		Assert.NotEqual(UriKey.Parse(Invoice), UriKey.Parse("https://meta.fuxion.nexus/erp/invoice/v2"));
		Assert.False(one.Equals(null));
		Assert.True((UriKey?)null == null);
		HashSet<UriKey> set = [one, other, UriKey.Parse(List)];
		Assert.Equal(2, set.Count);
	}

	[Fact(DisplayName = "The fingerprint is the SHA-256 of the canonical form, truncated to 128 bits, in base64url without padding")]
	public void Fingerprint()
	{
		// Computed apart: printf '%s' <key> | openssl dgst -sha256 -binary | head -c 16 | base64 | tr '+/' '-_' | tr -d '='
		Assert.Equal("Gc8y5zgKwl5Rx7pNcuGDng", UriKey.Parse(Bool).Fingerprint);
		Assert.Equal("kmUR2FeMIkTwKsxQ9biFRQ", UriKey.Parse($"{List}[{Invoice}]").Fingerprint);
		Assert.All(new[] { Bool, String, $"{Dictionary}[{String},{Invoice}]" }, text =>
		{
			var fingerprint = UriKey.Parse(text).Fingerprint;
			Assert.Equal(22, fingerprint.Length);
			Assert.DoesNotContain('=', fingerprint);
			Assert.DoesNotContain('+', fingerprint);
			Assert.DoesNotContain('/', fingerprint);
		});
		Assert.NotEqual(UriKey.Parse(List).Fingerprint, UriKey.Parse($"{List}[{Invoice}]").Fingerprint);
	}

	public record Message(UriKey Type, Dictionary<UriKey, int> Counts);

	[Fact(DisplayName = "In JSON, a UriKey is a string with its canonical form, also as the name of a property")]
	public void Json()
	{
		var message = new Message(UriKey.Parse($"{List}[{Invoice}]"), new() { [UriKey.Parse(Invoice)] = 3 });
		var json = JsonSerializer.Serialize(message);
		Assert.Equal($$$"""{"Type":"{{{List}}}[{{{Invoice}}}]","Counts":{"{{{Invoice}}}":3}}""", json);
		var back = JsonSerializer.Deserialize<Message>(json)!;
		Assert.Equal(message.Type, back.Type);
		Assert.Equal(3, back.Counts[UriKey.Parse(Invoice)]);
	}

	[Theory(DisplayName = "In JSON, what is not a UriKey in its canonical form fails")]
	[InlineData("""{"Type":"https://meta.fuxion.nexus/Erp/invoice/v1","Counts":{}}""")]
	[InlineData("""{"Type":42,"Counts":{}}""")]
	[InlineData("""{"Type":"https://meta.fuxion.nexus/erp/invoice/v1","Counts":{"invoice":1}}""")]
	public void JsonRejected(string json) => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Message>(json));
}
