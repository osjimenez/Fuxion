using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

using global::Fuxion;
using global::Fuxion.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SerializeTest(ITestOutputHelper output) : BaseTest<SerializeTest>(output)
{
	public static IEnumerable<object?[]> GenerateTheoryParameters()
	{
		List<(Params, Asserts)> inputs =
		[
			// Serialize normal object unformatted without options
			(new("OBJ", "STRING", new TestClass("Bob") { Age = 30 }, false, false, false), new(true, false, true)),
			(new("OBJ", "NODE", new TestClass("Bob") { Age = 30 }, false, false, false), new(true, false, true)),
			(new("OBJ", "ELEMENT", new TestClass("Bob") { Age = 30 }, false, false, false), new(true, false, true)),
			// Serialize normal object unformatted with options
			(new("OBJ", "STRING", new TestClass("Bob") { Age = 30 }, false, true, false), new(true, false, true)),
			(new("OBJ", "NODE", new TestClass("Bob") { Age = 30 }, false, true, false), new(true, false, true)),
			(new("OBJ", "ELEMENT", new TestClass("Bob") { Age = 30 }, false, true, false), new(true, false, true)),
			// Serialize normal object formatted without options
			(new("OBJ", "STRING", new TestClass("Bob") { Age = 30 }, true, false, false), new(true, false, true)),
			(new("OBJ", "NODE", new TestClass("Bob") { Age = 30 }, true, false, false), new(true, false, true)),
			(new("OBJ", "ELEMENT", new TestClass("Bob") { Age = 30 }, true, false, false), new(true, false, true)),
			// Serialize normal object formatted with options
			(new("OBJ", "STRING", new TestClass("Bob") { Age = 30 }, true, true, false), new(true, false, true)),
			(new("OBJ", "NODE", new TestClass("Bob") { Age = 30 }, true, true, false), new(true, false, true)),
			(new("OBJ", "ELEMENT", new TestClass("Bob") { Age = 30 }, true, true, false), new(true, false, true)),
			// Serialize throw object
			(new("OBJ", "STRING", new TestClass("Bob") { Age = 30, Throw = true }, false, false, false), new(false, true, false)),
			(new("OBJ", "NODE", new TestClass("Bob") { Age = 30, Throw = true }, false, false, false), new(false, true, false)),
			(new("OBJ", "ELEMENT", new TestClass("Bob") { Age = 30, Throw = true }, false, false, false),
				new(false, true, false)),
			// Serialize null value without errorIfNull
			(new("OBJ", "STRING", null, false, false, false), new(true, false, true)),
			//(new("NODE",null, false, false, false),new(true, false, true)), // This case doesn't apply for NODE because SerializeToNode always returns error on null
			(new("OBJ", "ELEMENT", null, false, false, false), new(true, false, true)),
			// Serialize null value with errorIfNull
			(new("OBJ", "STRING", null, false, false, true), new(false, true, true)),
			(new("OBJ", "NODE", null, false, false, true), new(false, true, true)),
			(new("OBJ", "ELEMENT", null, false, false, true), new(false, true, true)),
			// Serialize Exception object unformatted
			(new("EX", "STRING", new InvalidOperationException("Test exception"), false, false, false), new(true, false, true)),
			(new("EX", "NODE", new InvalidOperationException("Test exception"), false, false, false), new(true, false, true)),
			(new("EX", "ELEMENT", new InvalidOperationException("Test exception"), false, false, false), new(true, false, true)),
			// Serialize Exception object formatted
			(new("EX", "STRING", new InvalidOperationException("Test exception"), true, false, false), new(true, false, true)),
			(new("EX", "NODE", new InvalidOperationException("Test exception"), true, false, false), new(true, false, true)),
			(new("EX", "ELEMENT", new InvalidOperationException("Test exception"), true, false, false), new(true, false, true)),
			// Serialize Exception null object without errorIfNull
			(new("EX", "STRING", null, false, false, false), new(true, false, true)),
			//(new("EX","NODE",null, false, null, false),new(true,false,true)),
			(new("EX", "ELEMENT", null, false, false, false), new(true, false, true)),
			// Serialize Exception null object with errorIfNull
			(new("EX", "STRING", null, false, false, true), new(false, true, true)),
			(new("EX", "NODE", null, false, false, true), new(false, true, true)),
			(new("EX", "ELEMENT", null, false, false, true), new(false, true, true)),
		];
		return inputs.Select(i => new object?[] { i.Item1, i.Item2 });
	}

	public record Params(string Type, string Method, object? Object, bool Formatted, bool HasOptions, bool ErrorIfNull);
	public record Asserts(bool IsSuccess, bool PayloadIsNull, bool ExceptionIsNull);
	[Theory]
	[MemberData(nameof(GenerateTheoryParameters))]
	public void Serialize(Params input, Asserts asserts)
	{
		PrintVariable(input);
		PrintVariable(asserts);
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
		};
		IResponse res = (input.Type, input.Method) switch
		{
			("OBJ","STRING") => input.Object.Fx.Json.Serialize(input.Formatted,input.HasOptions ? options : null, input.ErrorIfNull),
			("OBJ", "NODE") => input.Object.Fx.Json.SerializeToNode(input.Formatted, input.HasOptions ? options : null),
			("OBJ", "ELEMENT") => input.Object.Fx.Json.SerializeToElement(input.Formatted, input.HasOptions ? options : null, input.ErrorIfNull),
			("EX", "STRING") => ((Exception?)input.Object).Fx.Json.Serialize(input.Formatted, input.HasOptions ? options : null, input.ErrorIfNull),
			("EX", "NODE") => ((Exception?)input.Object).Fx.Json.SerializeToNode(input.Formatted, input.HasOptions ? options : null),
			("EX", "ELEMENT") => ((Exception?)input.Object).Fx.Json.SerializeToElement(input.Formatted, input.HasOptions ? options : null, input.ErrorIfNull),
			_ => throw new ArgumentException("input.Metho no tiene un valor soportado"),
		};
		PrintVariable(res.Message);
		Assert.Equal(asserts.IsSuccess, res.IsSuccess);
		if (res is IResponse<JsonNode> resNode)
		{
			Assert.Equal(asserts.PayloadIsNull, resNode.Payload is null);
			if (!asserts.PayloadIsNull)
			{
				PrintVariable(resNode.Payload);
				
			}
		}
		else if (res is IResponse<JsonElement> resEle)
		{
			Assert.Equal(asserts.PayloadIsNull, resEle.Payload.ValueKind == JsonValueKind.Undefined);
			if (!asserts.PayloadIsNull)
				PrintVariable(resEle.Payload);
		}
		else if (res is IResponse<string> resObj)
		{
			Assert.Equal(asserts.PayloadIsNull, resObj.Payload is null);
			if (!asserts.PayloadIsNull && input.Object is not null)
			{
				PrintVariable(resObj.Payload);
				if (input.Formatted)
					Assert.Equal("{\r\n\t\"", resObj.Payload!.Substring(0,5));
				else
					Assert.Equal("{\"", resObj.Payload!.Substring(0, 2));
			}
		}else Assert.Fail("res has an unexpected type");
		
		Assert.Equal(asserts.ExceptionIsNull, res.Exception is null);
	}
}

public class SerializationTest(ITestOutputHelper output) : BaseTest<SerializationTest>(output)
{
	[Fact]
	public void Deserialize_Ok_Typed()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30
			}
			""";
		var res = json.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsSuccess);
		PrintVariable(res.Payload);
	}
	[Fact]
	public void Deserialize_Ok_Untyped()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30
			}
			""";
		var res = json.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsSuccess);
		PrintVariable(res.Payload);
	}

	[Fact]
	public void Deserialize_Ok_Formatted_Typed()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30, // Allow trailing comma and comments
			}
			""";
		Assert.True(json.Fx.Json.Deserialize<TestClass>().IsError);
		var res = json.Fx.Json.Deserialize<TestClass>(true);

		Assert.True(res.IsSuccess);
		PrintVariable(res.Payload);
	}

	[Fact]
	public void Deserialize_Ok_Formatted_Untyped()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30, // Allow trailing comma and comments
			}
			""";
		Assert.True(json.Fx.Json.Deserialize(typeof(TestClass)).IsError);
		var res = json.Fx.Json.Deserialize(typeof(TestClass), true);

		Assert.True(res.IsSuccess);
		PrintVariable(res.Payload);
	}

	[Fact]
	public void Deserialize_BadJson_Typed()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsError);
		Assert.NotNull(res.Exception);
		Assert.NotNull(res.Exception.StackTrace);
		PrintVariable(res.Message);
		PrintVariable(res.Exception.StackTrace);
	}

	[Fact]
	public void Deserialize_BadJson_Untyped()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsError);
		Assert.NotNull(res.Exception);
		Assert.NotNull(res.Exception.StackTrace);
		PrintVariable(res.Message);
		PrintVariable(res.Exception.StackTrace);
	}

	[Fact]
	public void Deserialize_NullValue_Typed()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsError);
		Assert.Null(res.Payload);
	}

	[Fact]
	public void Deserialize_NullValue_Untyped()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsError);
		Assert.Null(res.Payload);
	}

	[Fact]
	public void Deserialize_NullString_Typed()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsError);
		Assert.Null(res.Payload);
	}

	[Fact]
	public void Deserialize_NullString_Untyped()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsError);
		Assert.Null(res.Payload);
	}

	[Fact]
	public void Deserialize_Nullable_Ok_Typed()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30
			}
			""";
		var res = json.Fx.Json.Deserialize<TestClass>();
		PrintVariable(res.Message);
		Assert.True(res.IsSuccess);
		Assert.NotNull(res.Payload);
		PrintVariable(res.Payload);
	}

	[Fact]
	public void Deserialize_Nullable_BadJson_Typed()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.DeserializeNullable<TestClass>();

		Assert.NotNull(res);
		Assert.True(res.IsError);
		Assert.NotNull(res.Exception);
		Assert.NotNull(res.Exception.StackTrace);
		PrintVariable(res.Message);
		PrintVariable(res.Exception.StackTrace);
	}

	[Fact]
	public void Deserialize_Nullable_NullValue_Typed()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.DeserializeNullable<TestClass>();

		Assert.Null(res);
	}

	[Fact]
	public void Deserialize_Nullable_NullString_Typed()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.DeserializeNullable<TestClass>();

		Assert.Null(res);
	}
	[Fact]
	public void Deserialize_Nullable_NullValue_Untyped()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.DeserializeNullable(typeof(TestClass));

		Assert.Null(res);
	}

	[Fact]
	public void Deserialize_Nullable_NullString_Untyped()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.DeserializeNullable(typeof(TestClass));

		Assert.Null(res);
	}
	[Fact]
	public void Deserialize_Nullable_Ok_Untyped()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30
			}
			""";
		var res = json.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsSuccess);
		Assert.NotNull(res.Payload);
		PrintVariable(res.Payload);
	}

	[Fact]
	public void Deserialize_Nullable_BadJson_Untyped()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.DeserializeNullable(typeof(TestClass));

		Assert.NotNull(res);
		Assert.True(res.IsError);
		Assert.NotNull(res.Exception);
		Assert.NotNull(res.Exception.StackTrace);
		PrintVariable(res.Message);
		PrintVariable(res.Exception.StackTrace);
	}

	[Fact]
	public void Deserialize_Nullable_Null_Untyped()
	{
		string? nullJson = null;
		var res = nullJson.Fx.Json.DeserializeNullable(typeof(TestClass));

		Assert.Null(res);
	}

	[Fact]
	public void Deserialize_Exception_Typed()
	{
		var json = """
			{
				"Name": "Bob"
			}
			""";
		var res = json.Fx.Json.Deserialize<TestClass>();
		Assert.True(res.IsError);
		Assert.NotNull(res.Exception);
		Assert.NotNull(res.Exception.StackTrace);
		PrintVariable(res.Message);
	}

	[Fact]
	public void Deserialize_Exception_Untyped()
	{
		var json = """
			{
				"Name": "Bob"
			}
			""";
		var res = json.Fx.Json.Deserialize(typeof(TestClass));
		Assert.True(res.IsError);
		Assert.NotNull(res.Exception);
		Assert.NotNull(res.Exception.StackTrace);
		PrintVariable(res.Message);
	}

	[Fact]
	public void Serialize_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.Serialize();
		Assert.True(res.IsSuccess);
		Assert.Equal("null", res.Payload);
	}
	[Fact]
	public void Serialize_ErrorIfNull_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.Serialize(errorIfNull:true);
		Assert.True(res.IsError);
		Assert.Null(res.Payload);
	}
	[Fact]
	public void SerializeToNode_Ok()
	{
		var obj = new TestClass("Bob") { Age = 30 };
		var res = obj.Fx.Json.SerializeToNode();
		Assert.True(res.IsSuccess);
		Assert.Equal("Bob", res.Payload[nameof(TestClass.FullName)]?.GetValue<string>());
		Assert.Equal(30, res.Payload[nameof(TestClass.Age)]?.GetValue<int>());
	}
	[Fact]
	public void SerializeToNode_Throw()
	{
		var obj = new TestClass("Bob") { Age = 30, Throw = true };
		var res = obj.Fx.Json.SerializeToNode();
		Assert.True(res.IsError);
		Assert.Null(res.Payload);
		Assert.NotNull(res.Exception);
	}
	[Fact]
	public void SerializeToNode_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.SerializeToNode();
		Assert.True(res.IsError);
		Assert.Null(res.Payload);
	}
	[Fact]
	public void SerializeToElement_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.SerializeToElement();
		Assert.True(res.IsSuccess);
		Assert.Equal(JsonValueKind.Null, res.Payload.ValueKind);
	}
	[Fact]
	public void SerializeToElement_ErrorIfNull_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.SerializeToElement(errorIfNull:true);
		Assert.True(res.IsError);
		Assert.Equal(JsonValueKind.Undefined, res.Payload.ValueKind);
		Assert.Equal(default, res.Payload.ValueKind);
	}
	[Fact]
	public void SerializeException_ToString_Null()
	{
		Exception? obj = null;
		var res = obj.Fx.Json.Serialize();
		Assert.True(res.IsSuccess);
		Assert.Equal("null", res.Payload);
	}
	[Fact]
	public void SerializeException_ToString_ErrorIfNull_Null()
	{
		Exception? obj = null;
		var res = obj.Fx.Json.Serialize(errorIfNull:true);
		Assert.True(res.IsError);
		PrintVariable(res.Message);
		Assert.Null(res.Payload);
	}
}

file class TestClass(string fullName)
{
	private string _fullName = fullName;

	public string FullName
	{
		get => Throw ? throw new InvalidOperationException("Name property exception") : _fullName;
		set => _fullName = Throw ? throw new InvalidOperationException("Name property exception") : value;
	}

	public required int Age { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool IsAdmin { get; set; } = false;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Role { get; set; }

	[JsonIgnore]
	public bool Throw { get; set; }
	public override string ToString() => Throw ? "THROW" : $"{FullName} - {Age}";
}
