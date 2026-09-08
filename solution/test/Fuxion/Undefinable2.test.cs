using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class UndefinableTest2(ITestOutputHelper output) : BaseTest<UndefinableTest>(output)
{
	void CheckTypeInitialization(Type type)
	{
		var ex = Assert.ThrowsAny<TypeInitializationException>(() =>
		{
			RuntimeHelpers.RunClassConstructor(type.TypeHandle);
		}, tiex => tiex.InnerException is InvalidOperationException ? null : "InnerException isn't InvalidOperationException");
		Output.WriteLine("Method throw as expected: " + ex.InnerException!.Message);
	}
	[Fact]
	public void CannotBeInitializedWithNoneType()
	{
		CheckTypeInitialization(typeof(Undefinable<None>));
	}

	[Fact]
	public void ImplicitConversion_ValueType()
	{
		{
			Undefinable<int> u = 123;
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			IsTrue(u is int);
			IsFalse(u is None);
			// values
			IsTrue(u == 123);
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<int> u = default;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is int);
			IsTrue(u is None);
			// values
			IsFalse(u == 123);
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<int> u = None.Value;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is int);
			IsTrue(u is None);
			// values
			IsFalse(u == 123);
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
	}
	[Fact]
	public void ImplicitConversion_ValueNullableType()
	{
		{
			Undefinable<int?> u = 123;
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			IsTrue(u is int);
			IsFalse(u is None);
			// values
			IsTrue(u == 123);
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsFalse(u == null);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<int?> u = null;
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			//IsTrue(u is int?); // IMPOSIBLE to do. Doesn't compile.
			//IsTrue(u is int); // IMPOSIBLE to do. null value cannot be boxed as int.
			if (u.TryGetValue(out int? val))
				Assert.Null(val);
			else
				Assert.Fail("val must be int?");
			IsFalse(u is None);
			var t = (int?)u;
			IsFalse(u is None);
			// values
			IsFalse(u == 123);
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsTrue(u == null);
			//IsTrue(u is null); // IMPOSIBLE to do. The 'is' operator checks if a reference value is null but Undefinable<int?> is a struct and cannot be null.
		}
		{
			Undefinable<int?> u = default;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is int);
			IsTrue(u is None);
			// values
			IsFalse(u == 123);
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u == null);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<int?> u = None.Value;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is int);
			IsTrue(u is None);
			// values
			IsFalse(u == 123);
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u == null);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
	}
	[Fact]
	public void ImplicitConversion_ReferenceType()
	{
		{
			Undefinable<string> u = "test";
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			IsTrue(u is string);
			IsFalse(u is None);
			// values
			IsTrue(u == "test");
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
#nullable disable
			Undefinable<string> u = null;
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			IsTrue(u is string);
			IsFalse(u is None);
			// values
			IsFalse(u == "test");
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsTrue(u == null);
			//IsTrue(u is null); // IMPOSIBLE to do. The 'is' operator checks if a reference value is null but Undefinable<int?> is a struct and cannot be null.
#nullable enable
		}
		{
			Undefinable<string> u = default;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is string);
			IsTrue(u is None);
			// values
			IsFalse(u == "test");
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<string> u = None.Value;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is string);
			IsTrue(u is None);
			// values
			IsFalse(u == "test");
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
	}
	[Fact]
	public void ImplicitConversion_ReferenceNullableType()
	{
		{
			Undefinable<string?> u = "test";
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			IsTrue(u is string);
			IsFalse(u is None);
			// values
			IsTrue(u == "test");
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsFalse(u == null);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<string?> u = null;
			// definition
			IsTrue(u.IsDefined);
			IsFalse(u.IsUndefined);
			// types
			//IsTrue(u is string?); // IMPOSIBLE to do. Doesn't compile.
			IsTrue(u is string);
			if (u.TryGetValue(out string? val))
				Assert.Null(val);
			else
				Assert.Fail("val must be string?");
			IsFalse(u is None);
			var t = (string?)u;
			IsFalse(u is None);
			// values
			IsFalse(u == "test");
			IsFalse(u == default);
			IsFalse(u == None.Value);
			IsTrue(u == null);
			//IsTrue(u is null); // IMPOSIBLE to do. The 'is' operator checks if a reference value is null but Undefinable<int?> is a struct and cannot be null.
		}
		{
			Undefinable<string?> u = default;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is string);
			IsTrue(u is None);
			// values
			IsFalse(u == "test");
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u == null);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
		{
			Undefinable<string?> u = None.Value;
			// definition
			IsFalse(u.IsDefined);
			IsTrue(u.IsUndefined);
			// types
			IsFalse(u is string);
			IsTrue(u is None);
			// values
			IsFalse(u == "test");
			IsTrue(u == default);
			IsTrue(u == None.Value);
			IsFalse(u == null);
			IsFalse(u is null); // Pass test, but 'is' operator is not the correct way (see bellow). 'u is null' always will be true because Undefinable<string> is a struct and cannot be null.
		}
	}
	[Fact]
	public void ImplicitConversion_Exception()
	{
		Throws<UndefinedException>(() =>
		{
			Undefinable<int> u = None.Value;
			var val = (int)u;
		});
		Throws<UndefinedException>(() =>
		{
			Undefinable<int> u = 1;
			var val = (None)u; // This line should throw UndefinedException because u is undefined.
		});
	}

	[Fact]
	public void Serialization_WithIgnore()
	{
		var definedSample = new UndefinableSampleWithIgnore(
			"123",
			123,
			null,
			"Hello",
			null,
			new DateTime(2021, 9, 1),
			null,
			new UndefinableObjectWithIgnore(123, null),
			null);

		var undefinedSample = new UndefinableSampleWithIgnore(
			"123",
			default,
			default,
			default,
			null,
			default,
			default,
			default,
			default);

		var options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			IndentCharacter = '\t',
			IndentSize = 1,
			WriteIndented = true
		};
		var definedJson = definedSample.Fx.Json.Serialize(true).SuccessOrThrow();
		var undefinedJson = undefinedSample.Fx.Json.Serialize(true).SuccessOrThrow();

		Output.WriteLine(definedJson ?? "null");
		PrintVariable(undefinedJson);
	}

	[Fact]
	public void Deserialization_WithIgnore()
	{
		var definedJson = """
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
		PrintVariable(definedJson, false);

		var definedSample = definedJson.Fx.Json.Deserialize<UndefinableSampleWithIgnore>(true).SuccessOrThrow();
		Assert.NotNull(definedSample);

		IsTrue(definedSample.Integer.IsDefined);
		IsTrue(definedSample.Integer is int);
		if (definedSample.Integer is int integer)
			Assert.Equal(123, integer);

		IsTrue(definedSample.NullableInteger.IsDefined);
		if (definedSample.NullableInteger.TryGetValue(out int? nullableInteger))
			Assert.Null(nullableInteger);
		else
			Assert.Fail("nullableInteger must be int?");

		IsTrue(definedSample.String.IsDefined);
		Assert.Equal("Hello", definedSample.String);

		IsTrue(definedSample.NullableString.IsDefined);
		IsTrue(definedSample.NullableString == null);

		IsTrue(definedSample.DateTime.IsDefined);
		Assert.Equal(new DateTime(2021, 9, 1), definedSample.DateTime);

		IsTrue(definedSample.NullableDateTime.IsDefined);
		IsTrue(definedSample.NullableDateTime == null);

		IsTrue(definedSample.Object.IsDefined);
		if (definedSample.Object is UndefinableObjectWithIgnore @object)
		{
			IsTrue(@object.Integer.IsDefined);
			Assert.Equal(123, @object.Integer.Value);
			IsTrue(@object.NullableInteger.IsDefined);
			IsTrue(@object.NullableInteger == null);
		}

		IsTrue(definedSample.NullableObject.IsDefined);
		IsTrue(definedSample.NullableObject == null);

		// Undefined values are represented by the absence of the property. The marker object is only used
		// where omission is not possible, but it is still accepted on properties, so Integer covers that path.
		var undefinedMarker = UndefinableConverterFactory.UndefinedMarkerPropertyName;
		var undefinedJson = $$"""
		{
			"Demo": "test",

			"Integer": { "{{undefinedMarker}}": true },
			"NullableString": null,
			"Object": {
				"Integer": 123,
				"NullableInteger": null
			}
		}
		""";
		PrintVariable(undefinedJson, false);

		var undefinedSample = undefinedJson.Fx.Json.Deserialize<UndefinableSampleWithIgnore>(true).SuccessOrThrow();
		Assert.NotNull(undefinedSample);

		IsTrue(undefinedSample.Integer.IsUndefined);
		IsTrue(undefinedSample.NullableInteger.IsUndefined);
		IsTrue(undefinedSample.String.IsUndefined);
		//IsTrue(undefinedSample.NullableString.IsUndefined);
		IsTrue(undefinedSample.DateTime.IsUndefined);
		IsTrue(undefinedSample.NullableDateTime.IsUndefined);
		//IsTrue(undefinedSample.Object.IsUndefined);
		IsTrue(undefinedSample.NullableObject.IsUndefined);
	}

	[Fact]
	public void Serialization_WithoutIgnore()
	{
		var definedSample = new UndefinableSampleWithoutIgnore(
			"123",
			123,
			null,
			"Hello",
			null,
			new DateTime(2021, 9, 1),
			null,
			new UndefinableObjectWithoutIgnore(123, null),
			null);

		var undefinedSample = new UndefinableSampleWithoutIgnore(
			"123",
			default,
			default,
			default,
			null,
			default,
			default,
			default,
			default);

		var options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			IndentCharacter = '\t',
			IndentSize = 1,
			WriteIndented = true
		};
		var definedJson = definedSample.Fx.Json.Serialize(true).SuccessOrThrow();
		var undefinedJson = undefinedSample.Fx.Json.Serialize(true).SuccessOrThrow();

		Output.WriteLine(definedJson ?? "null");
		PrintVariable(undefinedJson);
	}

	[Fact]
	public void Deserialization_WithoutIgnore()
	{
		var definedJson = """
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
		PrintVariable(definedJson, false);

		//var definedSample = definedJson.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true).PayloadOrThrow();
		var desRes = definedJson.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true);
		if (desRes is not UndefinableSampleWithoutIgnore definedSample)
			Assert.Fail("Deserialization fails");
		else
		{
			Assert.NotNull(definedSample);

			IsTrue(definedSample.Integer.IsDefined);
			IsTrue(definedSample.Integer is int);
			if (definedSample.Integer is int integer)
				Assert.Equal(123, integer);

			IsTrue(definedSample.NullableInteger.IsDefined);
			if (definedSample.NullableInteger.TryGetValue(out int? nullableInteger))
				Assert.Null(nullableInteger);
			else
				Assert.Fail("nullableInteger must be int?");

			IsTrue(definedSample.String.IsDefined);
			Assert.Equal("Hello", definedSample.String);

			IsTrue(definedSample.NullableString.IsDefined);
			IsTrue(definedSample.NullableString == null);

			IsTrue(definedSample.DateTime.IsDefined);
			Assert.Equal(new DateTime(2021, 9, 1), definedSample.DateTime);

			IsTrue(definedSample.NullableDateTime.IsDefined);
			IsTrue(definedSample.NullableDateTime == null);

			IsTrue(definedSample.Object.IsDefined);
			if (definedSample.Object is UndefinableObjectWithoutIgnore @object)
			{
				IsTrue(@object.Integer.IsDefined);
				Assert.Equal(123, @object.Integer.Value);
				IsTrue(@object.NullableInteger.IsDefined);
				IsTrue(@object.NullableInteger == null);
			}

			IsTrue(definedSample.NullableObject.IsDefined);
			IsTrue(definedSample.NullableObject == null);
		}

		// Undefined values are represented by the absence of the property. The marker object is only used
		// where omission is not possible, but it is still accepted on properties, so Integer covers that path.
		var undefinedMarker = UndefinableConverterFactory.UndefinedMarkerPropertyName;
		var undefinedJson = $$"""
		{
			"Demo": "test",

			"Integer": { "{{undefinedMarker}}": true },
			"NullableString": null,
			"Object": {
				"Integer": 123,
				"NullableInteger": null
			}
		}
		""";
		PrintVariable(undefinedJson, false);

		var undefinedSample = undefinedJson.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true).SuccessOrThrow();
		Assert.NotNull(undefinedSample);

		IsTrue(undefinedSample.Integer.IsUndefined);
		IsTrue(undefinedSample.NullableInteger.IsUndefined);
		IsTrue(undefinedSample.String.IsUndefined);
		//IsTrue(undefinedSample.NullableString.IsUndefined);
		IsTrue(undefinedSample.DateTime.IsUndefined);
		IsTrue(undefinedSample.NullableDateTime.IsUndefined);
		//IsTrue(undefinedSample.Object.IsUndefined);
		IsTrue(undefinedSample.NullableObject.IsUndefined);
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
		Assert.DoesNotContain("\"Integer\"", json);
		Assert.DoesNotContain("\"String\"", json);
		Assert.DoesNotContain(UndefinableConverterFactory.UndefinedMarkerPropertyName, json);
		Assert.Contains("\"NullableInteger\"", json);

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
		Assert.Contains(UndefinableConverterFactory.UndefinedMarkerPropertyName, rootJson!);
		IsTrue(rootJson.Fx.Json.Deserialize<Undefinable<int>>(true).SuccessOrThrow().IsUndefined);

		var array = new Undefinable<int>[] { new(1), None.Value, new(3) };
		var arrayJson = array.Fx.Json.Serialize(true).SuccessOrThrow();
		PrintVariable(arrayJson);
		Assert.Contains(UndefinableConverterFactory.UndefinedMarkerPropertyName, arrayJson!);

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

	[Fact(DisplayName = "The default value of the struct is the undefined state")]
	public void DefaultValue_IsUndefined()
	{
		// Reading an omitted property relies on this, because the converter is never invoked when the
		// property is absent and the struct is left at its default value.
		IsTrue(default(Undefinable<int>).IsUndefined);
		IsTrue(default(Undefinable<string>).IsUndefined);
		IsTrue(Undefinable<int>.Undefined.IsUndefined);
		IsTrue(!default(Undefinable<int>).IsDefined);
	}
}

file record UndefinableSampleWithIgnore(
	string Demo,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<int> Integer,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<int?> NullableInteger,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<string> String,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<string?> NullableString,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<DateTime> DateTime,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<DateTime?> NullableDateTime,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<UndefinableObjectWithIgnore> Object,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<UndefinableObjectWithIgnore?> NullableObject);
file record UndefinableObjectWithIgnore(
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<int> Integer,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] Undefinable<int?> NullableInteger);


file record UndefinableSampleWithoutIgnore(
	string Demo,
	Undefinable<int> Integer,
	Undefinable<int?> NullableInteger,
	Undefinable<string> String,
	Undefinable<string?> NullableString,
	Undefinable<DateTime> DateTime,
	Undefinable<DateTime?> NullableDateTime,
	Undefinable<UndefinableObjectWithoutIgnore> Object,
	Undefinable<UndefinableObjectWithoutIgnore?> NullableObject);
file record UndefinableObjectWithoutIgnore(
	Undefinable<int> Integer,
	Undefinable<int?> NullableInteger);