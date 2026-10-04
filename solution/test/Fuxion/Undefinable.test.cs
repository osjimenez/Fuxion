using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class UndefinableTest(ITestOutputHelper output) : BaseTest<UndefinableTest>(output)
{
	#region Struct semantics

	[Fact(DisplayName = "Undefinable<None> cannot be initialized: None is the undefined case itself")]
	public void CannotBeInitializedWithNoneType()
	{
		var ex = Assert.ThrowsAny<TypeInitializationException>(() =>
		{
			RuntimeHelpers.RunClassConstructor(typeof(Undefinable<None>).TypeHandle);
		}, tiex => tiex.InnerException is InvalidOperationException ? null : "InnerException isn't InvalidOperationException");
		Output.WriteLine("Static constructor throw as expected: " + ex.InnerException!.Message);
	}

	// What every defined value has in common, whatever its type: it is neither the default nor None.
	void AssertDefined<T>(Undefinable<T> u)
	{
		IsTrue(u.IsDefined);
		IsFalse(u.IsUndefined);
		IsFalse(u is None);
		IsFalse(u == default);
		IsFalse(u == None.Value);
	}
	// The default value and None are the same undefined state.
	void AssertUndefined<T>(Undefinable<T> u)
	{
		IsFalse(u.IsDefined);
		IsTrue(u.IsUndefined);
		IsTrue(u is None);
		IsTrue(u == default);
		IsTrue(u == None.Value);
	}

	[Fact(DisplayName = "A value type converts implicitly from its value, from default and from None")]
	public void ImplicitConversion_ValueType()
	{
		Undefinable<int> value = 123;
		AssertDefined(value);
		IsTrue(value is int);
		IsTrue(value == 123);

		Undefinable<int> fromDefault = default;
		AssertUndefined(fromDefault);
		IsFalse(fromDefault is int);
		IsFalse(fromDefault == 123);

		Undefinable<int> fromNone = None.Value;
		AssertUndefined(fromNone);
		IsFalse(fromNone is int);
		IsFalse(fromNone == 123);
	}

	[Fact(DisplayName = "A nullable value type keeps null as a defined value, distinct from undefined")]
	public void ImplicitConversion_ValueNullableType()
	{
		Undefinable<int?> value = 123;
		AssertDefined(value);
		IsTrue(value is int);
		IsTrue(value == 123);
		IsFalse(value == null);

		Undefinable<int?> nullValue = null;
		AssertDefined(nullValue);
		IsTrue(nullValue.TryGetValue(out int? val));
		Assert.Null(val);
		Assert.Null((int?)nullValue);
		IsFalse(nullValue == 123);
		IsTrue(nullValue == null);

		Undefinable<int?> fromDefault = default;
		AssertUndefined(fromDefault);
		IsFalse(fromDefault is int);
		IsFalse(fromDefault == 123);
		IsFalse(fromDefault == null);

		Undefinable<int?> fromNone = None.Value;
		AssertUndefined(fromNone);
		IsFalse(fromNone is int);
		IsFalse(fromNone == 123);
		IsFalse(fromNone == null);
	}

	[Fact(DisplayName = "A reference type converts implicitly from its value, from null, from default and from None")]
	public void ImplicitConversion_ReferenceType()
	{
		Undefinable<string> value = "test";
		AssertDefined(value);
		IsTrue(value is string);
		IsTrue(value == "test");

#nullable disable
		Undefinable<string> nullValue = null;
		AssertDefined(nullValue);
		IsTrue(nullValue is string);
		IsFalse(nullValue == "test");
		IsTrue(nullValue == null);
#nullable enable

		Undefinable<string> fromDefault = default;
		AssertUndefined(fromDefault);
		IsFalse(fromDefault is string);
		IsFalse(fromDefault == "test");

		Undefinable<string> fromNone = None.Value;
		AssertUndefined(fromNone);
		IsFalse(fromNone is string);
		IsFalse(fromNone == "test");
	}

	[Fact(DisplayName = "A nullable reference type keeps null as a defined value, distinct from undefined")]
	public void ImplicitConversion_ReferenceNullableType()
	{
		Undefinable<string?> value = "test";
		AssertDefined(value);
		IsTrue(value is string);
		IsTrue(value == "test");
		IsFalse(value == null);

		Undefinable<string?> nullValue = null;
		AssertDefined(nullValue);
		IsTrue(nullValue is string);
		IsTrue(nullValue.TryGetValue(out string? val));
		Assert.Null(val);
		Assert.Null((string?)nullValue);
		IsFalse(nullValue == "test");
		IsTrue(nullValue == null);

		Undefinable<string?> fromDefault = default;
		AssertUndefined(fromDefault);
		IsFalse(fromDefault is string);
		IsFalse(fromDefault == "test");
		IsFalse(fromDefault == null);

		Undefinable<string?> fromNone = None.Value;
		AssertUndefined(fromNone);
		IsFalse(fromNone is string);
		IsFalse(fromNone == "test");
		IsFalse(fromNone == null);
	}

	[Fact(DisplayName = "Converting to the case it does not hold throws UndefinedException")]
	public void ImplicitConversion_Exception()
	{
		Throws<UndefinedException>(() =>
		{
			Undefinable<int> u = None.Value;
			_ = (int)u;
		});
		Throws<UndefinedException>(() =>
		{
			Undefinable<int> u = 1;
			_ = (None)u;
		});
	}

	[Fact(DisplayName = "The default value of the struct is the undefined state")]
	public void DefaultValue_IsUndefined()
	{
		// Reading an omitted property relies on this, because the converter is never invoked when the
		// property is absent and the struct is left at its default value.
		IsTrue(default(Undefinable<int>).IsUndefined);
		IsTrue(default(Undefinable<string>).IsUndefined);
		IsTrue(Undefinable<int>.Undefined.IsUndefined);
		IsFalse(default(Undefinable<int>).IsDefined);
	}

	#endregion

	#region JSON

	[Fact(DisplayName = "Defined values (explicit nulls included), omitted properties and the marker read back right with JsonIgnore(WhenWritingDefault)")]
	public void Deserialization_WithJsonIgnore() => UndefinableSampleAssertions.Deserializes<UndefinableSampleWithIgnore>(Output);

	[Fact(DisplayName = "Defined values (explicit nulls included), omitted properties and the marker read back right without JsonIgnore")]
	public void Deserialization_WithoutJsonIgnore() => UndefinableSampleAssertions.Deserializes<UndefinableSampleWithoutIgnore>(Output);

	[Fact(DisplayName = "With RespectNullableAnnotations, an explicit null and a zero are defined values")]
	public void Nullables()
	{
		var json = """
			{
				"Demo": "",
				"Integer": 0,
				"String": null,
				"NullableString": null
			}
			""";
		var sample = JsonSerializer.Deserialize<UndefinableSampleWithIgnore>(json, new JsonSerializerOptions { RespectNullableAnnotations = true });

		Assert.NotNull(sample);
		IsTrue(sample.Integer.IsDefined);
		Assert.Equal(0, sample.Integer.Value);
		IsTrue(sample.String.IsDefined);
		Assert.Null(sample.String.Value);
	}

	[Fact(DisplayName = "An undefined property is omitted and comes back undefined")]
	public void UndefinedProperty_IsOmittedAndRoundTrips()
	{
		var sample = new UndefinableSampleWithoutIgnore(
			"test",
			default,
			123,
			default,
			null,
			default,
			default,
			default,
			default);

		var json = sample.Fx.Json.Serialize(true).SuccessOrThrow();
		PrintVariable(json);

		Assert.NotNull(json);
		var members = JsonNode.Parse(json)!.AsObject();
		IsFalse(members.ContainsKey("Integer"));
		IsFalse(members.ContainsKey("String"));
		IsTrue(members.ContainsKey("NullableInteger"));
		// The marker must not appear anywhere in the document, at any depth.
		Assert.DoesNotContain(UndefinableJsonConverterFactory.UndefinedMarkerPropertyName, json);


		var back = json.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true).SuccessOrThrow();
		Assert.NotNull(back);
		IsTrue(back.Integer.IsUndefined);
		IsTrue(back.String.IsUndefined);
		IsTrue(back.NullableInteger.IsDefined);
		Assert.Equal(sample, back);
	}

	[Fact(DisplayName = "Where a property cannot be omitted the marker object is used")]
	public void UndefinedWithoutProperty_UsesMarkerObject()
	{
		// The root, array elements and dictionary values have no property that could be omitted,
		// so the marker object is the only available representation.
		Undefinable<int> root = None.Value;
		var rootJson = root.Fx.Json.Serialize(true).SuccessOrThrow();
		PrintVariable(rootJson);
		Assert.Contains(UndefinableJsonConverterFactory.UndefinedMarkerPropertyName, rootJson!);
		IsTrue(rootJson.Fx.Json.Deserialize<Undefinable<int>>(true).SuccessOrThrow().IsUndefined);

		var array = new Undefinable<int>[] { new(1), None.Value, new(3) };
		var arrayJson = array.Fx.Json.Serialize(true).SuccessOrThrow();
		PrintVariable(arrayJson);
		Assert.Contains(UndefinableJsonConverterFactory.UndefinedMarkerPropertyName, arrayJson!);

		var backArray = arrayJson.Fx.Json.Deserialize<Undefinable<int>[]>(true).SuccessOrThrow();
		Assert.NotNull(backArray);
		Assert.Equal(3, backArray.Length);
		IsTrue(backArray[0].IsDefined);
		IsTrue(backArray[1].IsUndefined);
		IsTrue(backArray[2].IsDefined);
		Assert.Equal(3, backArray[2].Value);
	}

	[Fact(DisplayName = "A string equal to the old sentinel is no longer swallowed")]
	public void StringValueEqualToLegacySentinel_IsPreserved()
	{
		// The previous implementation used the "--undefined--" string as an in band sentinel, so this
		// exact value was silently turned into an undefined value when round tripping.
		Undefinable<string> value = "--undefined--";

		var json = value.Fx.Json.Serialize(true).SuccessOrThrow();
		PrintVariable(json);

		var back = json.Fx.Json.Deserialize<Undefinable<string>>(true).SuccessOrThrow();
		IsTrue(back.IsDefined);
		Assert.Equal("--undefined--", back.Value);
	}

	#endregion
}

// The same assertions for both samples: they differ only in whether their properties carry JsonIgnore.
file static class UndefinableSampleAssertions
{
	const string DefinedJson = """
		{
			"Integer": 123,
			"NullableInteger": null,
			"String": "Hello",
			"NullableString": null,
			"DateTime": "2021-09-01",
			"NullableDateTime": null,
			"Object": {
				"Integer": 123,
				"NullableInteger": null
			},
			"NullableObject": null
		}
		""";

	public static void Deserializes<TSample>(ITestOutputHelper output)
		where TSample : notnull, IUndefinableSample
	{
		var defined = DefinedJson.Fx.Json.Deserialize<TSample>(true).SuccessOrThrow();
		Assert.True(defined.Integer.IsDefined);
		Assert.True(defined.Integer is int integer && integer == 123);
		Assert.True(defined.NullableInteger.TryGetValue(out int? nullableInteger));
		Assert.Null(nullableInteger);
		Assert.True(defined.String.IsDefined);
		Assert.Equal("Hello", defined.String);
		Assert.True(defined.NullableString.IsDefined);
		Assert.True(defined.NullableString == null);
		Assert.True(defined.DateTime.IsDefined);
		Assert.Equal(new DateTime(2021, 9, 1), defined.DateTime);
		Assert.True(defined.NullableDateTime.IsDefined);
		Assert.True(defined.NullableDateTime == null);
		Assert.True(defined.Object.IsDefined);
		var @object = Assert.IsType<UndefinableObject>(defined.Object.Value);
		Assert.True(@object.Integer.IsDefined);
		Assert.Equal(123, @object.Integer.Value);
		Assert.True(@object.NullableInteger.IsDefined);
		Assert.True(@object.NullableInteger == null);
		Assert.True(defined.NullableObject.IsDefined);
		Assert.True(defined.NullableObject == null);

		// Undefined values are represented by the absence of the property. The marker object is only used
		// where omission is not possible, but it is still accepted on properties, so Integer covers that path.
		var undefinedJson = $$"""
			{
				"Demo": "test",
				"Integer": { "{{UndefinableJsonConverterFactory.UndefinedMarkerPropertyName}}": true },
				"NullableString": null,
				"Object": {
					"Integer": 123,
					"NullableInteger": null
				}
			}
			""";
		output.WriteLine(undefinedJson);
		var undefined = undefinedJson.Fx.Json.Deserialize<TSample>(true).SuccessOrThrow();
		Assert.True(undefined.Integer.IsUndefined);
		Assert.True(undefined.NullableInteger.IsUndefined);
		Assert.True(undefined.String.IsUndefined);
		Assert.True(undefined.DateTime.IsUndefined);
		Assert.True(undefined.NullableDateTime.IsUndefined);
		Assert.True(undefined.NullableObject.IsUndefined);
	}
}

file interface IUndefinableSample
{
	Undefinable<int> Integer { get; }
	Undefinable<int?> NullableInteger { get; }
	Undefinable<string> String { get; }
	Undefinable<string?> NullableString { get; }
	Undefinable<DateTime> DateTime { get; }
	Undefinable<DateTime?> NullableDateTime { get; }
	Undefinable<UndefinableObject> Object { get; }
	Undefinable<UndefinableObject?> NullableObject { get; }
}

file record UndefinableSampleWithIgnore(
	string Demo,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<int> Integer,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<int?> NullableInteger,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<string> String,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<string?> NullableString,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<DateTime> DateTime,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<DateTime?> NullableDateTime,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<UndefinableObject> Object,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<UndefinableObject?> NullableObject) : IUndefinableSample;

file record UndefinableSampleWithoutIgnore(
	string Demo,
	Undefinable<int> Integer,
	Undefinable<int?> NullableInteger,
	Undefinable<string> String,
	Undefinable<string?> NullableString,
	Undefinable<DateTime> DateTime,
	Undefinable<DateTime?> NullableDateTime,
	Undefinable<UndefinableObject> Object,
	Undefinable<UndefinableObject?> NullableObject) : IUndefinableSample;

file record UndefinableObject(Undefinable<int> Integer, Undefinable<int?> NullableInteger);
