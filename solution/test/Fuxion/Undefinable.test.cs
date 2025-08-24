namespace Fuxion.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class UndefinableTest(ITestOutputHelper output) : BaseTest<UndefinableTest>(output)
{
	[Fact(DisplayName = "Deserialization")]
	public void Deserialization()
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
		var undefinedJson = """
		{
		}
		""";

		var options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip
		};
		var definedSample = JsonSerializer.Deserialize<UndefinableSample>(definedJson, options);
		Assert.NotNull(definedSample);

		IsTrue(definedSample.Integer.IsDefined);
		Assert.Equal(123, definedSample.Integer.Value);

		IsTrue(definedSample.NullableInteger.IsDefined);
		Assert.Null(definedSample.NullableInteger.Value);

		IsTrue(definedSample.String.IsDefined);
		Assert.Equal("Hello", definedSample.String.Value);

		IsTrue(definedSample.NullableString.IsDefined);
		Assert.Null(definedSample.NullableString.Value);

		IsTrue(definedSample.DateTime.IsDefined);
		Assert.Equal(new DateTime(2021, 9, 1), definedSample.DateTime.Value);

		IsTrue(definedSample.NullableDateTime.IsDefined);
		Assert.Null(definedSample.NullableDateTime.Value);

		IsTrue(definedSample.Object.IsDefined);
		IsTrue(definedSample.Object.Value.Integer.IsDefined);
		Assert.Equal(123, definedSample.Object.Value.Integer.Value);
		IsTrue(definedSample.Object.Value.NullableInteger.IsDefined);
		Assert.Null(definedSample.Object.Value.NullableInteger.Value);

		IsTrue(definedSample.NullableObject.IsDefined);
		Assert.Null(definedSample.NullableObject.Value);

		var undefinedSample = JsonSerializer.Deserialize<UndefinableSample>(undefinedJson, options);
		Assert.NotNull(undefinedSample);

		IsTrue(undefinedSample.Integer.IsUndefined);
		IsTrue(undefinedSample.NullableInteger.IsUndefined);
		IsTrue(undefinedSample.String.IsUndefined);
		IsTrue(undefinedSample.NullableString.IsUndefined);
		IsTrue(undefinedSample.DateTime.IsUndefined);
		IsTrue(undefinedSample.NullableDateTime.IsUndefined);
		IsTrue(undefinedSample.Object.IsUndefined);
		IsTrue(undefinedSample.NullableObject.IsUndefined);
	}

	[Fact(DisplayName = "Serialization")]
	public void Serialization()
	{
		var definedSample = new UndefinableSample(
			"123",
			123,
			null,
			"Hello",
			null,
			new DateTime(2021, 9, 1),
			null,
			new UndefinableObject(123, null),
			null);

		var undefinedSample = new UndefinableSample(
			"123",
			Undefinable<int>.Undefined,
			Undefinable<int?>.Undefined,
			Undefinable<string>.Undefined,
			Undefinable<string?>.Undefined,
			Undefinable<DateTime>.Undefined,
			Undefinable<DateTime?>.Undefined,
			Undefinable<UndefinableObject>.Undefined,
			Undefinable<UndefinableObject?>.Undefined);

		var options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			WriteIndented = true
		};
		var definedJson = JsonSerializer.Serialize(definedSample, options);
		var undefinedJson = JsonSerializer.Serialize(undefinedSample, options);

		Output.WriteLine(definedJson);
		Output.WriteLine(undefinedJson);
	}

	[Fact(DisplayName = "Nullables")]
	public void Nullables()
	{
		var json = """
			{
				"Demo": "",
				"Integer": 0,
				//"NullableInteger": null,
				"String": null,
				"NullableString": null,
			}
			""";

		var options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			RespectNullableAnnotations = true
		};

		var sample = JsonSerializer.Deserialize<UndefinableSample>(json, options);

		Assert.NotNull(sample);

		IsTrue(sample.Integer.IsDefined);
		Assert.Equal(0, sample.Integer.Value);

		IsTrue(sample.String.IsDefined);
		Assert.Null(sample.String.Value);
	}
}

file record UndefinableSample(
	string Demo,
	Undefinable<int> Integer,
	Undefinable<int?> NullableInteger,
	Undefinable<string> String,
	Undefinable<string?> NullableString,
	Undefinable<DateTime> DateTime,
	Undefinable<DateTime?> NullableDateTime,
	Undefinable<UndefinableObject> Object,
	Undefinable<UndefinableObject?> NullableObject);
file record UndefinableObject(
	Undefinable<int> Integer,
	Undefinable<int?> NullableInteger);