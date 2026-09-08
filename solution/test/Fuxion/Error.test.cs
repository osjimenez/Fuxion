using System;
using System.Net;
using System.Text.Json;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

public class ErrorTest(ITestOutputHelper output) : BaseTest<ResponseTest>(output)
{
   #region Serialization

   //[Fact]
   //public string Serialization_OnlySource()
   //{
   //   var json = Error.Custom().Fx.Json.Serialize(true).PayloadOrThrow();

   //   AssertJson(json, [
   //      new([nameof(Error.Message)], IsPresent: false),
   //      new([nameof(Error.Source)]),
   //      new([nameof(Error.Source), nameof(ErrorSource.MethodName)], nameof(Serialization_OnlySource)),
   //      new([nameof(Error.Payload)], IsPresent: false),
   //      new([nameof(Error.Type)], IsPresent: false),
   //      new([nameof(Error.Exception)], IsPresent: false),
   //      ]);

   //   return json;
   //}
   //[Fact]
   //public string Serialization_NoSource()
   //{
   //   ErrorJsonConverter.WriteSourceInformation = false;
   //   var json = Error.Custom().Fx.Json.Serialize(true).PayloadOrThrow();
   //   ErrorJsonConverter.WriteSourceInformation = true;

   //   AssertJson(json, [
   //      new([nameof(Error.Message)], IsPresent: false),
   //      new([nameof(Error.Source)], IsPresent: false),
   //      new([nameof(Error.Payload)], IsPresent: false),
   //      new([nameof(Error.Type)], IsPresent: false),
   //      new([nameof(Error.Exception)], IsPresent: false),
   //      ]);

   //   return json;
   //}
   [Fact]
   public string Serialization_Message()
   {
      var json = Error.Custom("message").Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], "message"),
         new([nameof(Error.Source)]),
         new([nameof(Error.Payload)], IsPresent: false),
         new([nameof(Error.Type)], IsPresent: false),
         new([nameof(Error.Exception)], IsPresent: false),
         ]);

      return json;
   }
   [Fact]
   public string Serialization_Type()
   {
      var json = Error.Custom(type: CustomInfo.Default).Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], IsPresent: false),
         new([nameof(Error.Source)]),
         new([nameof(Error.Payload)], IsPresent: false),
         new([nameof(Error.Type)], CustomInfo.Default),
         new([nameof(Error.Type), nameof(CustomInfo.Message)], CustomInfo.Default.Message),
         new([nameof(Error.Type), nameof(CustomInfo.Code)], CustomInfo.Default.Code),
         new([nameof(Error.Exception)], IsPresent: false),
      ]);

      return json;
   }
   [Fact]
   public string Serialization_Type_HttpStatusCode()
   {
      var json = Error.NotImplemented().Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], IsPresent: false),
         new([nameof(Error.Source)]),
         new([nameof(Error.Payload)], IsPresent: false),
         new([nameof(Error.Type), "$type"], nameof(HttpStatusCode)),
         new([nameof(Error.Type), "Name"], HttpStatusCode.NotImplemented.ToString()),
         new([nameof(Error.Type), "Code"], (int)HttpStatusCode.NotImplemented),
         new([nameof(Error.Exception)], IsPresent: false),
      ]);

      return json;
   }
   [Fact]
   public string Serialization_Payload()
   {
      var json = Error.Custom(payload: CustomInfo.Default).Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], IsPresent: false),
         new([nameof(Error.Source)]),
         new([nameof(Error.Payload)], CustomInfo.Default),
         new([nameof(Error.Type)], IsPresent: false),
         new([nameof(Error.Exception)], IsPresent: false),
      ]);

      return json;
   }
   [Fact]
   public string Serialization_Extensions()
   {
      var json = Error.Custom(extensions: new()
      {
         ["Ext1"] = "extension",
         ["Ext2"] = new
         {
            Message = "message",
         }
      }).Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], IsPresent: false),
         new([nameof(Error.Source)]),
         new([nameof(Error.Payload)], IsPresent: false),
         new([nameof(Error.Type)], IsPresent: false),
         new([nameof(Error.Exception)], IsPresent: false),
         new(["Ext1"],"extension"),
         new(["Ext2","Message"],"message"),
         ]);

      return json;
   }
   [Fact]
   public string Serialization_Exception()
   {
      var json = Error.Custom(exception: new Exception("message")).Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], IsPresent: false),
         new([nameof(Error.Source)]),
         new([nameof(Error.Payload)], IsPresent: false),
         new([nameof(Error.Type)], IsPresent: false),
         new([nameof(Error.Exception)]),
         ]);

      return json;
   }
   [Fact]
   public string Serialization_Wrap()
   {
      var json = Error.NotImplemented(
            "message",
            CustomInfo.Default,
            new Exception(),
            callerMemberName: "test")
         .Wrap().Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Message)], "message"), // Must be copied from the inner error
         new([nameof(Error.Source), nameof(ErrorSource.MethodName)], nameof(Serialization_Wrap)), // Different from the inner error
         new([nameof(Error.Payload)], IsPresent: false), // Mustn't be copied from the inner error
         new([nameof(Error.Type),"Code"], (int)HttpStatusCode.NotImplemented), // Must be copied from the inner error
         new([nameof(Error.Exception)], IsPresent: false), // Mustn't be copied from the inner error

         new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Message)], "message"),
         new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotImplemented),
         new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Payload)], CustomInfo.Default),
         ]);

      return json;
   }
   [Fact]
   public string Serialization_InnerErrors()
   {
      var json = Error.NotImplemented(
         innerErrors: [
            Error.NotFound(),
            Error.Unauthorized(),
            Error.Forbidden(),
            ])
         .Fx.Json.Serialize(true).SuccessOrThrow();

      AssertJson(json, [
         new([nameof(Error.Type),"Code"], (int)HttpStatusCode.NotImplemented),
         new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
         new([nameof(Error.InnerErrors) + "[1]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Unauthorized),
         new([nameof(Error.InnerErrors) + "[2]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Forbidden),
         ]);

      return json;
   }
   [Fact]
   public string Serialization_Aggregate()
   {
      {
         var json = Error.Aggregate(
               Error.NotFound("message"),
               Error.NotFound())
            .Fx.Json.Serialize(true).SuccessOrThrow();

         AssertJson(json, [
            new([nameof(Error.Message)], "One or more errors occurred. (message)"),
            new([nameof(Error.Type),"Code"], (int)HttpStatusCode.NotFound), // When all inner errors have the same type, the aggregate error takes that type
            new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
            new([nameof(Error.InnerErrors) + "[1]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
         ]);
      }
      {
         var json = Error.Aggregate(
               Error.NotFound(),
               Error.Unauthorized(),
               Error.Forbidden())
            .Fx.Json.Serialize(true).SuccessOrThrow();

         AssertJson(json, [
            new([nameof(Error.Type),"Code"], (int)HttpStatusCode.InternalServerError),
         new([nameof(Error.InnerErrors) + "[0]",nameof(Error.Type), "Code"], (int)HttpStatusCode.NotFound),
         new([nameof(Error.InnerErrors) + "[1]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Unauthorized),
         new([nameof(Error.InnerErrors) + "[2]",nameof(Error.Type), "Code"], (int)HttpStatusCode.Forbidden),
         ]);

         return json;
      }
   }

   #endregion

   #region Deserialization

   //[Fact]
   //public void Deserialization_NoSource()
   //{
   //   Error error = Serialization_NoSource().Fx.Json.Deserialize<Error>().PayloadOrThrow();
   //   Assert.Null(error.Message);
   //   Assert.Null(error.Payload);
   //   Assert.Null(error.Type);
   //   Assert.Null(error.Exception);
   //   Assert.Null(error.Source);
   //}
   //[Fact]
   //public void Deserialization_OnlySource()
   //{
   //   Error error = Serialization_OnlySource().Fx.Json.Deserialize<Error>().PayloadOrThrow();
   //   Assert.Null(error.Message);
   //   Assert.Null(error.Payload);
   //   Assert.Null(error.Type);
   //   Assert.Null(error.Exception);
   //   Assert.NotNull(error.Source);

   //   Assert.Equal(nameof(Serialization_OnlySource), error.Source?.MethodName);
   //}
   [Fact]
   public void Deserialization_Message()
   {
      Error error = Serialization_Message().Fx.Json.DeserializeErrorOrFail();
      Assert.NotNull(error.Message);
      Assert.Null(error.Payload);
      Assert.Null(error.Type);
      Assert.Null(error.Exception);
      Assert.NotNull(error.Source);

      Assert.Equal("message", error.Message);
   }
   [Fact]
   public void Deserialization_Type()
   {
      Error error = Serialization_Type().Fx.Json.DeserializeErrorOrFail();
      Assert.Null(error.Message);
      Assert.Null(error.Payload);
      Assert.NotNull(error.Type);
      Assert.Null(error.Exception);
      Assert.NotNull(error.Source);

      var info = error.GetTypeAs<CustomInfo>();
      Assert.NotNull(info);
      Assert.Equal(CustomInfo.Default.Message, info.Message);
      Assert.Equal(CustomInfo.Default.Code, info.Code);
   }
   [Fact]
   public void Deserialization_Type_HttpStatusCode()
   {
      // OK
      {
         Error error = Serialization_Type_HttpStatusCode().Fx.Json.DeserializeErrorOrFail();
         Assert.Null(error.Message);
         Assert.Null(error.Payload);
         Assert.NotNull(error.Type);
         Assert.Null(error.Exception);

         if (error.Type is HttpStatusCode code)
            Assert.Equal(HttpStatusCode.NotImplemented, code);
         else
            Assert.Fail("Expected HttpStatusCode type");
      }
      // Not OK because 'Code' is not present (could be '$type' or 'Name' too)
      {
         var json = """
         {
            "Type": {
               "$type": "HttpStatusCode",
               "Name": "NotImplemented"
            }
         }
         """;
         PrintVariable(json, false);
         Error error = json.Fx.Json.DeserializeErrorOrFail();
         Assert.Null(error.Message);
         Assert.Null(error.Payload);
         Assert.NotNull(error.Type);
         Assert.Null(error.Exception);
         Assert.Null(error.Source);

         if (error.Type is not JsonElement element)
            Assert.Fail("Expected JsonElement");
      }
      // Not OK because 'Extra' is not expected
      {
         var json = """
         {
            "Type": {
               "$type": "HttpStatusCode",
               "Code": 501,
               "Name": "NotImplemented",
               "Extra": true
            }
         }
         """;
         PrintVariable(json, false);
         Error error = json.Fx.Json.DeserializeErrorOrFail();
         Assert.Null(error.Message);
         Assert.Null(error.Payload);
         Assert.NotNull(error.Type);
         Assert.Null(error.Exception);
         Assert.Null(error.Source);

         if (error.Type is not JsonElement element)
            Assert.Fail("Expected JsonElement");
      }
      // Not OK because the value of 'Code' not match with the value of 'Name'
      {
         var json = """
         {
            "Type": {
               "$type": "HttpStatusCode",
               "Code": 400,
               "Name": "NotImplemented"
            }
         }
         """;
         PrintVariable(json, false);
         Error error = json.Fx.Json.DeserializeErrorOrFail();
         Assert.Null(error.Message);
         Assert.Null(error.Payload);
         Assert.NotNull(error.Type);
         Assert.Null(error.Exception);
         Assert.Null(error.Source);

         if (error.Type is not JsonElement element)
            Assert.Fail("Expected JsonElement");
      }
   }
   [Fact]
   public void Deserialization_Payload()
   {
      Error error = Serialization_Payload().Fx.Json.DeserializeErrorOrFail();
      Assert.Null(error.Message);
      Assert.NotNull(error.Payload);
      Assert.Null(error.Type);
      Assert.Null(error.Exception);
      Assert.NotNull(error.Source);

      var info = error.GetPayloadAs<CustomInfo>();
      Assert.NotNull(info);
      Assert.Equal(CustomInfo.Default.Message, info.Message);
      Assert.Equal(CustomInfo.Default.Code, info.Code);
   }
   [Fact]
   public void Deserialization_Extensions()
   {
      Error error = Serialization_Extensions().Fx.Json.DeserializeErrorOrFail();
      Assert.Null(error.Message);
      Assert.Null(error.Payload);
      Assert.Null(error.Type);
      Assert.Null(error.Exception);
      Assert.NotNull(error.Source);

      Assert.Equal("extension", error.Extensions.GetAs<string>("Ext1"));
      Assert.Equal("message", error.Extensions.GetAs<CustomInfo>("Ext2")?.Message);
   }
   [Fact]
   public void Deserialization_Exception()
   {
      Error error = Serialization_Exception().Fx.Json.DeserializeErrorOrFail();
      Assert.Null(error.Message);
      Assert.Null(error.Payload);
      Assert.Null(error.Type);
      Assert.NotNull(error.Exception);
      Assert.NotNull(error.Source);

      var json2 = error.Fx.Json.Serialize(true).SuccessOrThrow();
      Error error2 = json2.Fx.Json.DeserializeErrorOrFail();
      Assert.Null(error2.Message);
      Assert.Null(error2.Payload);
      Assert.Null(error2.Type);
      Assert.NotNull(error2.Exception);
      Assert.NotNull(error2.Source);
   }
   [Fact]
   public void Deserialization_Wrap()
   {
      Error error = Serialization_Wrap().Fx.Json.DeserializeErrorOrFail();
      Assert.NotNull(error.Message);
      Assert.Null(error.Payload);
      Assert.NotNull(error.Type);
      Assert.Null(error.Exception);
      Assert.NotNull(error.Source);

      Assert.NotNull(error.InnerErrors);
      Assert.NotEmpty(error.InnerErrors);
      var inner = error.InnerErrors[0];
      Assert.NotNull(inner.Message);
      Assert.NotNull(inner.Payload);
      Assert.NotNull(inner.Type);
      Assert.NotNull(inner.Exception);
      Assert.NotNull(inner.Source);
   }
   [Fact]
   public void Deserialization_InnerErrors()
   {
      Error error = Serialization_InnerErrors().Fx.Json.DeserializeErrorOrFail();
      Assert.Equal(error.Type, HttpStatusCode.NotImplemented);
      Assert.NotNull(error.InnerErrors);
      Assert.NotEmpty(error.InnerErrors);
      Assert.Equal(3, error.InnerErrors.Length);
      Assert.Equal(error.InnerErrors[0].Type, HttpStatusCode.NotFound);
      Assert.Equal(error.InnerErrors[1].Type, HttpStatusCode.Unauthorized);
      Assert.Equal(error.InnerErrors[2].Type, HttpStatusCode.Forbidden);
   }
   [Fact]
   public void Deserialization_Aggregate()
   {
      Error error = Serialization_Aggregate().Fx.Json.DeserializeErrorOrFail();
      Assert.Equal(error.Type, HttpStatusCode.InternalServerError);
      Assert.NotNull(error.InnerErrors);
      Assert.NotEmpty(error.InnerErrors);
      Assert.Equal(3, error.InnerErrors.Length);
      Assert.Equal(error.InnerErrors[0].Type, HttpStatusCode.NotFound);
      Assert.Equal(error.InnerErrors[1].Type, HttpStatusCode.Unauthorized);
      Assert.Equal(error.InnerErrors[2].Type, HttpStatusCode.Forbidden);
   }
   #endregion

   #region Extensions

   [Fact]
   public void Extensions_ReservedKeys_AreDefined()
   {
      Assert.Equal(6, ErrorConstants.ErrorExtensionsReservedKeys.Count);
      Assert.Contains(nameof(Error.Payload), ErrorConstants.ErrorExtensionsReservedKeys);
      Assert.Contains(nameof(Error.Message), ErrorConstants.ErrorExtensionsReservedKeys);
      Assert.Contains(nameof(Error.Type), ErrorConstants.ErrorExtensionsReservedKeys);
      Assert.Contains(nameof(Error.Exception), ErrorConstants.ErrorExtensionsReservedKeys);
      Assert.Contains(nameof(Error.InnerErrors), ErrorConstants.ErrorExtensionsReservedKeys);
      Assert.Contains(nameof(Error.Source), ErrorConstants.ErrorExtensionsReservedKeys);
   }
   [Fact]
   public void Extensions_ReservedKeys_ThrowOnInit()
   {
      foreach (var key in ErrorConstants.ErrorExtensionsReservedKeys)
         Throws<ReservedKeyExtensionException>(() => new Error(extensions: new()
         {
            [key] = "reserved"
         }));
      foreach (var key in ErrorConstants.ErrorExtensionsReservedKeys)
         Throws<ReservedKeyExtensionException>(() => new Error()
         {
            Extensions = new()
            {
               [key] = "reserved"
            }
         });
   }
   [Fact]
   public void Extensions_ReservedKeys_ThrowOnAdd()
   {
      foreach (var key in ErrorConstants.ErrorExtensionsReservedKeys)
      {
         var error = new Error();
         Throws<ReservedKeyExtensionException>(() => error.Extensions.Add(key, "reserved"));
      }
   }

   #endregion

   #region InnerErrors

   [Fact(DisplayName = "An empty innerErrors array is stored as null")]
   public void InnerErrors_Empty_IsNull()
   {
      Assert.Null(Error.Custom("x", innerErrors: []).InnerErrors);
      Assert.Single(Error.Custom("x", innerErrors: [Error.Custom("inner")]).InnerErrors!);
   }
   [Fact(DisplayName = "Every factory accepts innerErrors and keeps them")]
   public void Factories_KeepInnerErrors()
   {
      Error[] inner = [Error.Custom("inner")];
      Error[] all =
      [
         Error.Custom("m", innerErrors: inner), Error.NotFound("m", innerErrors: inner), Error.Forbidden("m", innerErrors: inner),
         Error.Unauthorized("m", innerErrors: inner), Error.InvalidData("m", innerErrors: inner), Error.Conflict("m", innerErrors: inner),
         Error.Critical("m", innerErrors: inner), Error.NotImplemented("m", innerErrors: inner), Error.Unavailable("m", innerErrors: inner),
         Error.Timeout("m", innerErrors: inner)
      ];
      Assert.All(all, e => Assert.Equal("inner", Assert.Single(e.InnerErrors!).Message));
   }

   #endregion
}
file record CustomInfo(string Message, int Code)
{
   public static CustomInfo Default { get; } = new CustomInfo("test", 123);
}
// Error cannot be the success type of a Response<T>, so bare errors are read through TryDeserializeError; this
// keeps the deserialization tests as one-liners and fails with the deserialization failure's message.
file static class ErrorJsonTestExtensions
{
   extension(JsonExtensions<string?> me)
   {
      public Error DeserializeErrorOrFail()
      {
         if (!me.TryDeserializeError(out var error, out var failure))
            Assert.Fail(failure.Message ?? "The JSON could not be read as an Error.");
         return error;
      }
   }
}