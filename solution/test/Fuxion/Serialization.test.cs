using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

using Fuxion;
using Fuxion.Text.Json;
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
			_ => throw new ArgumentException("input.Method no tiene un valor soportado"),
		};
		Assert.Equal(asserts.IsSuccess, res.IsSuccess);
		// The error, if any, lives inside the concrete Response<T>; the interface only exposes the kind.
		Error? failure = res switch
		{
			Response<JsonNode> r when r.TryGetValue(out Error e) => e,
			Response<JsonElement> r when r.TryGetValue(out Error e) => e,
			Response<string> r when r.TryGetValue(out Error e) => e,
			_ => null
		};
		Assert.Equal(asserts.ExceptionIsNull, failure?.Exception is null);
		if (res is Response<JsonNode> resNode)
		{
			if (asserts.PayloadIsNull)
			{
				if(resNode is not Error error)
					Assert.Fail("res is not of type Error");
				else
					PrintVariable(error);
			}
			else
			{
				PrintVariable(resNode);
			}
			//Assert.Equal(asserts.PayloadIsNull, resNode.Payload is null);
			//if (!asserts.PayloadIsNull)
			//{
			//	PrintVariable(resNode.Payload);

			//}
		}
		else if (res is Response<JsonElement> resEle)
		{
			if (asserts.PayloadIsNull)
			{
				if (resEle is not Error error)
					Assert.Fail("res is not of type Error");
				else
					PrintVariable(error);
			}
			else
			{
				PrintVariable(resEle);
			}
			//Assert.Equal(asserts.PayloadIsNull, resEle.Payload.ValueKind == JsonValueKind.Undefined);
			//if (!asserts.PayloadIsNull)
			//	PrintVariable(resEle.Payload);
		}
		else if (res is Response<string> resObj)
		{
			if (asserts.PayloadIsNull)
			{
				if (resObj is not Error error)
					Assert.Fail("res is not of type Error");
				else
					PrintVariable(error);
			}
			else
			{
				if (resObj is not string json)
					Assert.Fail("res is not of type Response<string>");
				else
				{
					PrintVariable(json);
					if (input.Object is null)
						Assert.Equal("null", json);
					else
					{
						if (input.Formatted)
							Assert.Equal("{\r\n\t\"", json.Substring(0, 5));
						else
							Assert.Equal("{\"", json.Substring(0, 2));
					}
				}
			}
			//Assert.Equal(asserts.PayloadIsNull, resObj.Payload is null);
			//if (!asserts.PayloadIsNull && input.Object is not null)
			//{
			//	PrintVariable(resObj.Payload);
			//	if (input.Formatted)
			//		Assert.Equal("{\r\n\t\"", resObj.Payload!.Substring(0, 5));
			//	else
			//		Assert.Equal("{\"", resObj.Payload!.Substring(0, 2));
			//}
		}
		else Assert.Fail("res has an unexpected type");

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
		if (res is not TestClass payload)
			Assert.Fail("res is not of type TestClass");
		else
			PrintVariable(payload);
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
		if(res is not TestClass payload)
			Assert.Fail("res is not of type TestClass");
		else
			PrintVariable(payload);
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
		if(res is not TestClass payload)
			Assert.Fail("res is not of type TestClass");
		else
			PrintVariable(payload);
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
		if (res is not TestClass payload)
			Assert.Fail("res is not of type TestClass");
		else
			PrintVariable(payload);
	}

	[Fact]
	public void Deserialize_BadJson_Typed()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
	}

	[Fact]
	public void Deserialize_BadJson_Untyped()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
	}

	[Fact]
	public void Deserialize_NullValue_Typed()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error.Message);
	}

	[Fact]
	public void Deserialize_NullValue_Untyped()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error.Message);
	}

	[Fact]
	public void Deserialize_NullString_Typed()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.Deserialize<TestClass>();

		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error.Message);
	}

	[Fact]
	public void Deserialize_NullString_Untyped()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.Deserialize(typeof(TestClass));

		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error.Message);
	}

	[Fact]
	public void DeserializeMaybe_Ok_Typed()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30
			}
			""";
		var res = json.Fx.Json.DeserializeMaybe<TestClass>();
		Assert.True(res.IsSuccess);
		if (res is not TestClass payload)
			Assert.Fail("res is not of type TestClass");
		else
			PrintVariable(payload);
	}

	[Fact]
	public void DeserializeMaybe_BadJson_Typed()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.DeserializeMaybe<TestClass>();

		Assert.NotNull(res);
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
	}

	[Fact]
	public void DeserializeMaybe_NullValue_Typed()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.DeserializeMaybe<TestClass>();

		if(res is not None none)
			Assert.Fail("res is not of type None");
	}

	[Fact]
	public void DeserializeMaybe_NullString_Typed()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.DeserializeMaybe<TestClass>();

		if (res is not None none)
			Assert.Fail("res is not of type None");
	}
	[Fact]
	public void DeserializeMaybe_NullValue_Untyped()
	{
		string? nullValue = null;
		var res = nullValue.Fx.Json.DeserializeMaybe(typeof(TestClass));

		if (res is not None none)
			Assert.Fail("res is not of type None");
	}

	[Fact]
	public void DeserializeMaybe_NullString_Untyped()
	{
		string nullString = "null";
		var res = nullString.Fx.Json.DeserializeMaybe(typeof(TestClass));

		if (res is not None none)
			Assert.Fail("res is not of type None");
	}
	[Fact]
	public void DeserializeMaybe_Ok_Untyped()
	{
		const string json = """
			{
				"Name": "Bob",
				"Age": 30
			}
			""";
		var res = json.Fx.Json.DeserializeMaybe(typeof(TestClass));

		Assert.True(res.IsSuccess);
		if (res is not TestClass payload)
			Assert.Fail("res is not of type TestClass");
		else
			PrintVariable(payload);
	}

	[Fact]
	public void DeserializeMaybe_BadJson_Untyped()
	{
		var badJson = "--";
		var res = badJson.Fx.Json.DeserializeMaybe(typeof(TestClass));

		Assert.NotNull(res);
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
	}

	[Fact]
	public void DeserializeMaybe_Null_Untyped()
	{
		string? nullJson = null;
		var res = nullJson.Fx.Json.DeserializeMaybe(typeof(TestClass));

		if (res is not None none)
			Assert.Fail("res is not of type None");
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
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
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
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
	}

	[Fact]
	public void Serialize_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.Serialize();
		Assert.True(res.IsSuccess);
		if(res is not string value)
			Assert.Fail("res is not of type string");
		else
			Assert.Equal("null", value);
	}
	[Fact]
	public void Serialize_ErrorIfNull_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.Serialize(errorIfNull:true);
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error.Message);
	}
	[Fact]
	public void SerializeToNode_Ok()
	{
		var obj = new TestClass("Bob") { Age = 30 };
		var res = obj.Fx.Json.SerializeToNode();
		Assert.True(res.IsSuccess);
		if (res is not JsonNode node)
			Assert.Fail("res is not of type JsonNode");
		else
		{
			PrintVariable(node);
			Assert.Equal("Bob", node[nameof(TestClass.FullName)]?.GetValue<string>());
			Assert.Equal(30, node[nameof(TestClass.Age)]?.GetValue<int>());
		}
	}
	[Fact]
	public void SerializeToNode_Throw()
	{
		var obj = new TestClass("Bob") { Age = 30, Throw = true };
		var res = obj.Fx.Json.SerializeToNode();
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
		{
			Assert.NotNull(error.Exception);
			Assert.NotNull(error.Exception.StackTrace);
			PrintVariable(error.Message);
			PrintVariable(error.Exception.StackTrace);
		}
	}
	[Fact]
	public void SerializeToNode_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.SerializeToNode();
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error);
	}
	[Fact]
	public void SerializeToElement_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.SerializeToElement();
		Assert.True(res.IsSuccess);
		if (res is not JsonElement element)
			Assert.Fail("res is not of type string");
		else
			Assert.Equal(JsonValueKind.Null, element.ValueKind);
	}
	[Fact]
	public void SerializeToElement_ErrorIfNull_Null()
	{
		object? obj = null;
		var res = obj.Fx.Json.SerializeToElement(errorIfNull:true);
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error);
	}
	[Fact]
	public void SerializeException_ToString_Null()
	{
		Exception? obj = null;
		var res = obj.Fx.Json.Serialize();
		Assert.True(res.IsSuccess);
		// PEND - Uncomment the logic again
		if (res is not string value)
			Assert.Fail("res is not of type string");
		else
			Assert.Equal("null", value);
	}
	[Fact]
	public void SerializeException_ToString_ErrorIfNull_Null()
	{
		Exception? obj = null;
		var res = obj.Fx.Json.Serialize(errorIfNull:true);
		Assert.True(res.IsError);
		if (res is not Error error)
			Assert.Fail("res is not of type Error");
		else
			PrintVariable(error.Message);
	}
}

public class FuxionFormattedTypeInfoResolverTest
{
	[Fact]
	public void GetTypeInfo_OrderPropertiesAlphabetically_OrdersPropertiesEvenWhenCreateObjectExists()
	{
		var resolver = new JsonExtensions.FuxionFormattedTypeInfoResolver();
		var info = resolver.GetTypeInfo(typeof(ResolverPublicCtorType), new JsonSerializerOptions());

		Assert.Equal([nameof(ResolverPublicCtorType.Apple), nameof(ResolverPublicCtorType.Zebra)], info.Properties.OrderBy(p => p.Order).Select(p => p.Name));
	}

	[Fact]
	public void GetTypeInfo_AllowPrivateConstructorsFalse_DoesNotAssignCreateObject()
	{
		var resolver = new JsonExtensions.FuxionFormattedTypeInfoResolver
		{
			AllowPrivateConstructors = false
		};
		var info = resolver.GetTypeInfo(typeof(ResolverPrivateCtorType), new JsonSerializerOptions());

		Assert.Null(info.CreateObject);
	}

	[Fact]
	public void GetTypeInfo_OrderPropertiesAlphabeticallyFalse_DoesNotAssignPropertyOrder()
	{
		var resolver = new JsonExtensions.FuxionFormattedTypeInfoResolver
		{
			OrderPropertiesAlphabetically = false
		};
		var info = resolver.GetTypeInfo(typeof(ResolverPublicCtorType), new JsonSerializerOptions());

		Assert.All(info.Properties, p => Assert.Equal(0, p.Order));
	}
}

// Error and None cannot be the success type of Response<T>/ResponseMaybe<T>: the generic deserializers must say so
// up front, and bare errors are read through TryDeserializeError.
public class DeserializeUnionCasesTest(ITestOutputHelper output) : BaseTest<DeserializeUnionCasesTest>(output)
{
	[Fact(DisplayName = "Deserialize<Error> and DeserializeMaybe<Error> fail fast with a message pointing to TryDeserializeError")]
	public void Deserialize_Error_IsNotSupported()
	{
		var json = Error.NotFound("missing").Fx.Json.Serialize().SuccessOrThrow();
		var ex = Assert.Throws<NotSupportedException>(() => json.Fx.Json.Deserialize<Error>());
		Assert.Contains("TryDeserializeError", ex.Message);
		Assert.Throws<NotSupportedException>(() => json.Fx.Json.DeserializeMaybe<Error>());
	}

	[Fact(DisplayName = "Deserialize<None> and DeserializeMaybe<None> fail fast with a clear message")]
	public void Deserialize_None_IsNotSupported()
	{
		var ex = Assert.Throws<NotSupportedException>(() => "null".Fx.Json.Deserialize<None>());
		Assert.Contains(nameof(None), ex.Message);
		Assert.Throws<NotSupportedException>(() => "null".Fx.Json.DeserializeMaybe<None>());
	}

	[Fact(DisplayName = "TryDeserializeError reads a bare error and reports malformed or empty input as a failure")]
	public void TryDeserializeError_Paths()
	{
		var json = Error.NotFound("missing").Fx.Json.Serialize().SuccessOrThrow();
		Assert.True(json.Fx.Json.TryDeserializeError(out var error, out _));
		Assert.Equal(System.Net.HttpStatusCode.NotFound, error.Type);
		Assert.Equal("missing", error.Message);

		Assert.False("{ not json".Fx.Json.TryDeserializeError(out _, out var malformed));
		IsTrue(malformed.IsInternalServerError);
		Assert.NotNull(malformed.Exception);

		Assert.False("".Fx.Json.TryDeserializeError(out _, out var empty));
		IsTrue(empty.IsInternalServerError);
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

file class ResolverPublicCtorType
{
	public string Zebra { get; set; } = string.Empty;
	public string Apple { get; set; } = string.Empty;
}

file class ResolverPrivateCtorType
{
	private ResolverPrivateCtorType()
	{
	}

	public string Zebra { get; set; } = string.Empty;
	public string Apple { get; set; } = string.Empty;
}
