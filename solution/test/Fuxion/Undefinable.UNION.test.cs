using Fuxion.Xunit;
using Xunit;
using Fuxion;
using Fuxion.Union;
using Fuxion.Text.Json;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace Test.Fuxion.Union;

public class UndefinableTest(ITestOutputHelper output) : BaseTest<UndefinableTest>(output)
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
      CheckTypeInitialization(typeof(global::Fuxion.Union.Undefinable<None>));
   }

   [Fact]
	public void ImplicitConversion_ValueType()
	{
		{
         global::Fuxion.Union.Undefinable<int> u = 123;
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
         global::Fuxion.Union.Undefinable<int> u = default;
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
         global::Fuxion.Union.Undefinable<int> u = None.Value;
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
         global::Fuxion.Union.Undefinable<int?> u = 123;
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
         global::Fuxion.Union.Undefinable<int?> u = null;
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
         global::Fuxion.Union.Undefinable<int?> u = default;
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
         global::Fuxion.Union.Undefinable<int?> u = None.Value;
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
         global::Fuxion.Union.Undefinable<string> u = "test";
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
         global::Fuxion.Union.Undefinable<string> u = null;
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
         global::Fuxion.Union.Undefinable<string> u = default;
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
         global::Fuxion.Union.Undefinable<string> u = None.Value;
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
         global::Fuxion.Union.Undefinable<string?> u = "test";
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
         global::Fuxion.Union.Undefinable<string?> u = null;
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
         global::Fuxion.Union.Undefinable<string?> u = default;
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
         global::Fuxion.Union.Undefinable<string?> u = None.Value;
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
      Throws<global::Fuxion.UndefinedException>(() =>
      {
         global::Fuxion.Union.Undefinable<int> u = None.Value;
         var val = (int)u;
      });
      Throws<global::Fuxion.UndefinedException>(() =>
      {
         global::Fuxion.Union.Undefinable<int> u = 1;
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
      var definedJson = definedSample.Fx.Json.Serialize(true).PayloadOrThrow();
      var undefinedJson = undefinedSample.Fx.Json.Serialize(true).PayloadOrThrow();

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

      var definedSample = definedJson.Fx.Json.Deserialize<UndefinableSampleWithIgnore>(true).PayloadOrThrow();
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
		var undefinedMarker = global::Fuxion.Union.UndefinableConverterFactory.UndefinedMarkerPropertyName;
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

		var undefinedSample = undefinedJson.Fx.Json.Deserialize<UndefinableSampleWithIgnore>(true).PayloadOrThrow();
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
      var definedJson = definedSample.Fx.Json.Serialize(true).PayloadOrThrow();
      var undefinedJson = undefinedSample.Fx.Json.Serialize(true).PayloadOrThrow();

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

      var definedSample = definedJson.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true).PayloadOrThrow();
      Assert.NotNull(definedSample);

      IsTrue(definedSample.Integer.IsDefined);
      IsTrue(definedSample.Integer is int);
      if (definedSample.Integer is int integer)
         Assert.Equal(123, integer);

      IsTrue(definedSample.NullableInteger.IsDefined);
      if(definedSample.NullableInteger.TryGetValue(out int? nullableInteger))
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
      if(definedSample.Object is UndefinableObjectWithoutIgnore @object){
         IsTrue(@object.Integer.IsDefined);
         Assert.Equal(123, @object.Integer.Value);
         IsTrue(@object.NullableInteger.IsDefined);
         IsTrue(@object.NullableInteger == null);
      }

      IsTrue(definedSample.NullableObject.IsDefined);
      IsTrue(definedSample.NullableObject == null);

		// Undefined values are represented by the absence of the property. The marker object is only used
		// where omission is not possible, but it is still accepted on properties, so Integer covers that path.
		var undefinedMarker = global::Fuxion.Union.UndefinableConverterFactory.UndefinedMarkerPropertyName;
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

		var undefinedSample = undefinedJson.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true).PayloadOrThrow();
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

      var json = sample.Fx.Json.Serialize(true).PayloadOrThrow();
      PrintVariable(json);

      Assert.NotNull(json);
      Assert.DoesNotContain("\"Integer\"", json);
      Assert.DoesNotContain("\"String\"", json);
      Assert.DoesNotContain(global::Fuxion.Union.UndefinableConverterFactory.UndefinedMarkerPropertyName, json);
      Assert.Contains("\"NullableInteger\"", json);

      var back = json.Fx.Json.Deserialize<UndefinableSampleWithoutIgnore>(true).PayloadOrThrow();
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
      global::Fuxion.Union.Undefinable<int> root = None.Value;
      var rootJson = root.Fx.Json.Serialize(true).PayloadOrThrow();
      PrintVariable(rootJson);
      Assert.Contains(global::Fuxion.Union.UndefinableConverterFactory.UndefinedMarkerPropertyName, rootJson!);
      IsTrue(rootJson.Fx.Json.Deserialize<global::Fuxion.Union.Undefinable<int>>(true).PayloadOrThrow().IsUndefined);

      var array = new global::Fuxion.Union.Undefinable<int>[] { new(1), None.Value, new(3) };
      var arrayJson = array.Fx.Json.Serialize(true).PayloadOrThrow();
      PrintVariable(arrayJson);
      Assert.Contains(global::Fuxion.Union.UndefinableConverterFactory.UndefinedMarkerPropertyName, arrayJson!);

      var backArray = arrayJson.Fx.Json.Deserialize<global::Fuxion.Union.Undefinable<int>[]>(true).PayloadOrThrow();
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
      global::Fuxion.Union.Undefinable<string> value = "--undefined--";

      var json = value.Fx.Json.Serialize(true).PayloadOrThrow();
      PrintVariable(json);

      var back = json.Fx.Json.Deserialize<global::Fuxion.Union.Undefinable<string>>(true).PayloadOrThrow();
      IsTrue(back.IsDefined);
      Assert.Equal("--undefined--", back.Value);
   }

   [Fact(DisplayName = "The default value of the struct is the undefined state")]
   public void DefaultValue_IsUndefined()
   {
      // Reading an omitted property relies on this, because the converter is never invoked when the
      // property is absent and the struct is left at its default value.
      IsTrue(default(global::Fuxion.Union.Undefinable<int>).IsUndefined);
      IsTrue(default(global::Fuxion.Union.Undefinable<string>).IsUndefined);
      IsTrue(global::Fuxion.Union.Undefinable<int>.Undefined.IsUndefined);
      IsTrue(!default(global::Fuxion.Union.Undefinable<int>).IsDefined);
   }
}

file record UndefinableSampleWithIgnore(
   string Demo,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<int> Integer,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<int?> NullableInteger,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<string> String,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<string?> NullableString,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<DateTime> DateTime,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<DateTime?> NullableDateTime,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<UndefinableObjectWithIgnore> Object,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<UndefinableObjectWithIgnore?> NullableObject);
file record UndefinableObjectWithIgnore(
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<int> Integer,
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] global::Fuxion.Union.Undefinable<int?> NullableInteger);


file record UndefinableSampleWithoutIgnore(
   string Demo,
   global::Fuxion.Union.Undefinable<int> Integer,
   global::Fuxion.Union.Undefinable<int?> NullableInteger,
   global::Fuxion.Union.Undefinable<string> String,
   global::Fuxion.Union.Undefinable<string?> NullableString,
   global::Fuxion.Union.Undefinable<DateTime> DateTime,
   global::Fuxion.Union.Undefinable<DateTime?> NullableDateTime,
   global::Fuxion.Union.Undefinable<UndefinableObjectWithoutIgnore> Object,
   global::Fuxion.Union.Undefinable<UndefinableObjectWithoutIgnore?> NullableObject);
file record UndefinableObjectWithoutIgnore(
   global::Fuxion.Union.Undefinable<int> Integer,
   global::Fuxion.Union.Undefinable<int?> NullableInteger);