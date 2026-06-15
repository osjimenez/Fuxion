using Fuxion;
using Fuxion.Reflection;
using Fuxion.Text.Json;
using Fuxion.Union;
using Fuxion.Xunit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Test.Fuxion.Union;


public class ResponseTest(ITestOutputHelper output) : BaseTest<ResponseTest>(output)
{
   #region Static constructors

   void AssertStaticConstructor<TClass, TException>()
   {
      var ex = Assert.ThrowsAny<TypeInitializationException>(() =>
      {
         RuntimeHelpers.RunClassConstructor(typeof(TClass).TypeHandle);
      }, tiex => tiex.InnerException is TException ? null : $"InnerException isn't {typeof(TException).GetSignature()}");
      Output.WriteLine("Static constructor throw as expected: " + ex.InnerException!.Message);
   }
   [Fact]
   public void TSuccess_TError_CannotBeInitializedWithSameType()
   {
      AssertStaticConstructor<Response<string, string>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<string, string>, ResponseInitializationException>();
   }
   [Fact]
   public void TSuccess_CannotBeInitializedWith_None_Type()
   {
      AssertStaticConstructor<Response<None>, ResponseInitializationException>();
      AssertStaticConstructor<Response<None, string>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<None>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<None, string>, ResponseInitializationException>();
   }
   [Fact]
   public void TSuccess_CannotBeInitializedWith_Error_Type()
   {
      AssertStaticConstructor<Response<Error>, ResponseInitializationException>();
      AssertStaticConstructor<Response<Error, string>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<Error>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<Error, string>, ResponseInitializationException>();
   }
   [Fact]
   public void TError_CannotBeInitializedWith_None_Type()
   {
      AssertStaticConstructor<Response<string, None>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<string, None>, ResponseInitializationException>();
   }
   [Fact]
   public void TError_CannotBeInitializedWith_Error_Type()
   {
      AssertStaticConstructor<Response<string, Error>, ResponseInitializationException>();
      AssertStaticConstructor<ResponseMaybe<string, Error>, ResponseInitializationException>();
   }

   #endregion

   //[Fact]
   //public void Extensions_ExceptionJson_FromJsonElement()
   //{
   //   var json = """
   //   {
   //      "IsSuccess": false,
   //      "Error": {
   //         "Message": "error",
   //         "Type": "NotFound",
   //         "ExceptionJson": {
   //            "Message": "fromExt"
   //         }
   //      }
   //   }
   //   """;

   //   Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response.IsError);
   //   if (response is Error err)
   //   {
   //      // The extension key is 'ExceptionJson' but should be processed by the typed getter
   //      Assert.True(err.Extensions.ExceptionJson.IsDefined);
   //      var exJson = (ExceptionJson)err.Extensions.ExceptionJson;
   //      Assert.Equal("fromExt", exJson.Message);
   //   }
   //}
   //[Fact]
   //public void Extensions_ExceptionJson_AssignAndRemove()
   //{
   //   var exJson = new ExceptionJson { Message = "m" };
   //   var err = new Error("error", "type", null, null, null);
   //   var withExt = err with { Extensions = new ExtensionsDictionary<Error>() };
   //   withExt.Extensions.ExceptionJson = exJson;
   //   Assert.True(withExt.Extensions.ExceptionJson.IsDefined);
   //   Assert.Equal("m", ((ExceptionJson)withExt.Extensions.ExceptionJson).Message);

   //   withExt.Extensions.ExceptionJson = None.Value;
   //   Assert.False(withExt.Extensions.ExceptionJson.IsDefined);
   //}
   //[Fact]
   //public void Deserialization_RemoteException_Roundtrip()
   //{
   //   var json = """
   //   {
   //      "Error": {
   //         "Exception": {
   //            "Message": "ex",
   //            "ParamName": "userId"
   //         },
   //         "Message": "error"
   //      },
   //      "IsSuccess": false
   //   }
   //   """;

   //   Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response.IsError);
   //   string serialized = response.Fx.Json.Serialize(true).PayloadOrThrow();
   //   // The serialized should contain 'Exception' and not 'ExceptionJson' as top-level extension
   //   Assert.Contains("\"Exception\"", serialized);
   //   Assert.DoesNotContain("\"ExceptionJson\"", serialized);

   //   // Roundtrip back
   //   Response<Unit> response2 = serialized.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response2.IsError);
   //   if (response2 is Error error2)
   //   {
   //      Assert.True(error2.Extensions.ExceptionJson.IsDefined);
   //      var exJson = (ExceptionJson)error2.Extensions.ExceptionJson;
   //      Assert.Equal("ex", exJson.Message);
   //      Assert.Equal("userId", exJson.Extensions.GetAs<string>("ParamName"));
   //   }
   //}
   //[Fact]
   //public void Deserialization_RemoteException_WithExtraProperties()
   //{
   //   var json = """
   //   {
   //      "Error": {
   //         "Exception": {
   //            "Message": "ex",
   //            "ParamName": "userId"
   //         },
   //         "Message": "error"
   //      },
   //      "IsSuccess": false
   //   }
   //   """;

   //   Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response.IsError);
   //   if (response is Error error)
   //   {
   //      Assert.True(error.Extensions.ExceptionJson.IsDefined);
   //      var exJson = (ExceptionJson)error.Extensions.ExceptionJson;
   //      Assert.Equal("userId", exJson.Extensions.GetAs<string>("ParamName"));
   //   }
   //}
   //[Fact]
   //public void Deserialization_RemoteException_WithStackTrace()
   //{
   //   var json = """
   //   {
   //      "Error": {
   //         "Exception": {
   //            "Message": "ex",
   //            "StackTrace": [
   //               { "Method": "M()", "File": "F.cs", "Line": 10 }
   //            ]
   //         },
   //         "Message": "error"
   //      },
   //      "IsSuccess": false
   //   }
   //   """;

   //   Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response.IsError);
   //   if (response is Error error)
   //   {
   //      Assert.True(error.Extensions.ExceptionJson.IsDefined);
   //      var exJson = (ExceptionJson)error.Extensions.ExceptionJson;
   //      Assert.NotNull(exJson.StackTrace);
   //      Assert.Single(exJson.StackTrace!);
   //      Assert.Equal("M()", exJson.StackTrace![0].Method);
   //      var remote = Assert.IsType<RemoteException>(error.Exception);
   //      Assert.NotNull(remote.RemoteStackTrace);
   //      Assert.Equal("M()", remote.RemoteStackTrace![0].Method);
   //   }
   //}
   //[Fact]
   //public void Deserialization_RemoteException_WithInnerException()
   //{
   //   var json = """
   //   {
   //      "Error": {
   //         "Exception": {
   //            "Message": "outer",
   //            "InnerException": {
   //               "Message": "inner"
   //            }
   //         },
   //         "Message": "error"
   //      },
   //      "IsSuccess": false
   //   }
   //   """;

   //   Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response.IsError);
   //   if (response is Error error)
   //   {
   //      var remote = Assert.IsType<RemoteException>(error.Exception);
   //      Assert.NotNull(remote.InnerException);
   //      Assert.IsType<RemoteException>(remote.InnerException);
   //      var innerRemote = (RemoteException)remote.InnerException!;
   //      Assert.Equal("inner", innerRemote.Message);
   //      Assert.Equal("outer", remote.Message);
   //      Assert.True(error.Extensions.ExceptionJson.IsDefined);
   //      var exJson = (ExceptionJson)error.Extensions.ExceptionJson;
   //      Assert.Equal("outer", exJson.Message);
   //      Assert.Equal("inner", exJson.InnerException?.Message);
   //   }
   //}
   //[Fact]
   //public void Deserialization_RemoteException_IsThrowable()
   //{
   //   var json = """
   //   {
   //      "Error": {
   //         "Exception": {
   //            "Message": "remote",
   //            "HResult": 1
   //         },
   //         "Message": "error"
   //      },
   //      "IsSuccess": false
   //   }
   //   """;

   //   Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
   //   Assert.True(response.IsError);
   //   Assert.True(response is Error);
   //   if (response is Error error)
   //   {
   //      var remote = Assert.IsType<RemoteException>(error.Exception);
   //      // Throw and catch
   //      try
   //      {
   //         throw remote;
   //      }
   //      catch (RemoteException thrown)
   //      {
   //         Assert.Equal(remote.ExceptionJson.Message, thrown.ExceptionJson.Message);
   //      }
   //   }
   //}

   #region Serialization

   void AssertResponseJson(
      string json,
      bool isSuccess,
      bool? isNone = null,
      bool containsPayload = false,
      bool containsError = false,
      AssertJsonEntry[]? additionalAsserts = null)
   {
      List<AssertJsonEntry> asserts = [];
      if (isSuccess)
         asserts.Add(new([nameof(global::Fuxion.Union.IResponse.IsSuccess)] , true));
      else
         asserts.Add(new([nameof(global::Fuxion.Union.IResponse.IsSuccess)], false));

      if (isNone is null)
         asserts.Add(new([nameof(ResponseMaybe<>.IsNone)], IsPresent: false));
      else if(isNone is true)
         asserts.Add(new([nameof(ResponseMaybe<>.IsNone)], true));
      else
         asserts.Add(new([nameof(ResponseMaybe<>.IsNone)], false));

      if (containsPayload)
         asserts.Add(new([ResponseConstants.PayloadPropertyName]));
      else
         asserts.Add(new([ResponseConstants.PayloadPropertyName], IsPresent: false));

      if (containsError)
         asserts.Add(new([ResponseConstants.ErrorPropertyName]));
      else
         asserts.Add(new([ResponseConstants.ErrorPropertyName], IsPresent: false));

      asserts.Add(new([nameof(IUnion.Value)], IsPresent: false));

      if(additionalAsserts is not null)
         asserts.AddRange(additionalAsserts);

      AssertJson(json, asserts.ToArray());
   }
   [Fact]
   public void Serialization_Success()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         Response<User> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, containsPayload: true);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<User, string> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, containsPayload: true);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<User> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, isNone: false, containsPayload: true);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, string> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, isNone: false, containsPayload: true);
      }
   }
   [Fact]
   public void Serialization_None()
   {
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<User> response = None.Value;
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
         AssertResponseJson(json, true, isNone: true);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, string> response = None.Value;
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, isNone: true);
      }
   }
   [Fact]
   public void Serialization_Error()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         Response<User> response = Error.Custom("message");
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, false, containsError: true);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<User, string> response = "error";
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, false, containsError: true);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<User> response = Error.Custom("message");
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, false, isNone: false, containsError: true);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, string> response = "error";
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, false, isNone: false, containsError: true);
      }
   }

   [Fact]
   public void Serialization_CustomError()
   {
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<User, CustomError> response = new CustomError("message");
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, false, containsError: true, additionalAsserts: [new(["Error", "Message"], "message")]);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, CustomError> response = new CustomError("message");
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, false, isNone: false, containsError: true, additionalAsserts: [new(["Error", "Message"], "message")]);
      }
   }

   ExtensionsDictionary<global::Fuxion.Union.IResponse> defaultExtensions = new()
   {
      ["Ext1"] = "extension",
      ["Ext2"] = new
      {
         Message = "extension",
      }
   };

   [Fact]
   public void Serialization_Extensions()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         Response<Unit> response = new(Unit.Value) { Extensions = defaultExtensions };
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, additionalAsserts: [new(["Ext1"], "extension"), new(["Ext2", "Message"], "extension")]);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<Unit, string> response = new(Unit.Value) { Extensions = defaultExtensions };
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, additionalAsserts: [new(["Ext1"], "extension"), new(["Ext2", "Message"], "extension")]);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<Unit> response = new(Unit.Value) { Extensions = defaultExtensions };
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, isNone: false, additionalAsserts: [new(["Ext1"], "extension"), new(["Ext2", "Message"], "extension")]);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<Unit, string> response = new(Unit.Value) { Extensions = defaultExtensions };
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         AssertResponseJson(json, true, isNone: false, additionalAsserts: [new(["Ext1"], "extension"), new(["Ext2", "Message"], "extension")]);
      }
   }

   #endregion

   #region Deserialization

   [Fact]
   public void Deserialization_Success()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         var json = """
            {
               "IsSuccess": true,
               "Payload": {
                  "Age": 123,
                  "Name": "test"
               },
               "Ext1": "extension",
               "Ext2": {
                  "Message": "extension"
               }
            }
            """;
         Response<User> response = json.Fx.Json.Deserialize<Response<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is User);
         Assert.Equal("test", ((User)response).Name);
         Assert.Equal(123, ((User)response).Age);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         var json = """
            {
               "IsSuccess": true,
               "Payload": {
                  "Age": 123,
                  "Name": "test"
               }
            }
            """;
         Response<User, string> response = json.Fx.Json.Deserialize<Response<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is User);
         Assert.Equal("test", ((User)response).Name);
         Assert.Equal(123, ((User)response).Age);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         var json = """
            {
               "IsNone": false,
               "IsSuccess": true,
               "Payload": {
                  "Age": 123,
                  "Name": "test"
               }
            }
            """;
         ResponseMaybe<User> response = json.Fx.Json.Deserialize<ResponseMaybe<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is User);
         Assert.Equal("test", ((User)response).Name);
         Assert.Equal(123, ((User)response).Age);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         var json = """
            {
               "IsNone": false,
               "IsSuccess": true,
               "Payload": {
                  "Age": 123,
                  "Name": "test"
               }
            }
            """;
         ResponseMaybe<User, string> response = json.Fx.Json.Deserialize<ResponseMaybe<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is User);
         Assert.Equal("test", ((User)response).Name);
         Assert.Equal(123, ((User)response).Age);
      }
   }
   [Fact]
   public void Deserialization_None()
   {
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         var json = """
            {
               "IsNone": true,
               "IsSuccess": true
            }
            """;
         ResponseMaybe<User> response = json.Fx.Json.Deserialize<ResponseMaybe<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsNone);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is None);
      }
      {
         var json = """
            {
               "IsNone": true,
               "IsSuccess": true,
               "Payload": null
            }
            """;
         ResponseMaybe<User> response = json.Fx.Json.Deserialize<ResponseMaybe<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsNone);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is None);
      }
      {
         var json = """
            {
               "IsNone": true,
               "IsSuccess": true,
               "Payload": {}
            }
            """;
         ResponseMaybe<User> response = json.Fx.Json.Deserialize<ResponseMaybe<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsNone);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is None);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         var json = """
            {
               "IsNone": true,
               "IsSuccess": true
            }
            """;
         ResponseMaybe<User, string> response = json.Fx.Json.Deserialize<ResponseMaybe<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsNone);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is None);
      }
      {
         var json = """
            {
               "IsNone": true,
               "IsSuccess": true,
               "Payload": null
            }
            """;
         ResponseMaybe<User, string> response = json.Fx.Json.Deserialize<ResponseMaybe<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsNone);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is None);
      }
      {
         var json = """
            {
               "IsNone": true,
               "IsSuccess": true,
               "Payload": {}
            }
            """;
         ResponseMaybe<User, string> response = json.Fx.Json.Deserialize<ResponseMaybe<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsTrue(response.IsNone);
         IsTrue(response.IsSuccess);
         IsFalse(response.IsError);
         IsTrue(response is None);
      }
   }
   [Fact]
   public void Deserialization_Error()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         var json = """
            {
               "Error": {
                  "Exception": {
                     "Data": [],
                     "HelpLink": null,
                     "HResult": -2146233088,
                     "InnerException": null,
                     "Message": "exception",
                     "Source": null,
                     "StackTrace": null,
                     "TargetSite": null
                  },
                  "Message": "error",
                  "Payload": {
                     "Age": 123,
                     "Name": "test"
                  },
                  "Type": "NotFound"
               },
               "IsSuccess": false
            }
            """;
         Response<User> response = json.Fx.Json.Deserialize<Response<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsFalse(response.IsSuccess);
         IsTrue(response.IsError);
         IsTrue(response is Error);
         if (response is Error error)
         {
            Assert.Equal("error", error.Message);
            Assert.IsType<RemoteException>(error.Exception);
            var remote = (RemoteException)error.Exception;
            Assert.Equal("exception", remote.Message);
            Assert.Equal(ErrorType.NotFound, error.GetTypeAs<ErrorType>());
            Assert.Equal("test", error.GetPayloadAs<User>()?.Name);
            Assert.Equal(123, error.GetPayloadAs<User>()?.Age);
         }
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         var json = """
            {
               "IsSuccess": false,
               "Error": "error"
            }
            """;
         Response<User, string> response = json.Fx.Json.Deserialize<Response<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsFalse(response.IsSuccess);
         IsTrue(response.IsError);
         IsTrue(response is string);
         if (response is string error)
         {
            Assert.Equal("error", error);
         }
      }
      {
         var json = """
            {
               "IsSuccess": false,
               "Error": {
                  "Message": "error"
               }
            }
            """;
         Response<User, CustomError> response = json.Fx.Json.Deserialize<Response<User, CustomError>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsFalse(response.IsSuccess);
         IsTrue(response.IsError);
         IsTrue(response is CustomError);
         if (response is CustomError error)
            Assert.Equal("error", error.Message);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         var json = """
            {
               "IsSuccess": false,
               "IsNone": false,
               "Error": {
                  "Exception": null,
                   "Message": "error",
                   "Payload": null,
                   "Type": 123
               }
            }
            """;
         ResponseMaybe<User> response = json.Fx.Json.Deserialize<ResponseMaybe<User>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsFalse(response.IsSuccess);
         IsFalse(response.IsNone);
         IsTrue(response.IsError);
         IsTrue(response is Error);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         var json = """
            {
               "IsSuccess": false,
               "Error": "error"
            }
            """;
         ResponseMaybe<User, string> response = json.Fx.Json.Deserialize<ResponseMaybe<User, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         IsFalse(response.IsSuccess);
         IsFalse(response.IsNone);
         IsTrue(response.IsError);
         IsTrue(response is string);
         Assert.Equal("error", (string)response);
      }
   }
   [Fact]
   public void Deserialization_Extensions()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         var json = """
            {
               "IsSuccess": true,
               "Ext1": "extension",
               "Ext2": {
                  "Message": "extension"
               }
            }
            """;
         Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
         IsTrue(response is not null);
         Assert.Equal("extension", response.Extensions.GetAs<string>("Ext1"));
         Assert.Equal("extension", response.Extensions.GetAs<CustomError>("Ext2")?.Message);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         var json = """
            {
               "IsSuccess": true,
               "Ext1": "extension",
               "Ext2": {
                  "Message": "extension"
               }
            }
            """;
         Response<Unit, string> response = json.Fx.Json.Deserialize<Response<Unit, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         Assert.Equal("extension", response.Extensions.GetAs<string>("Ext1"));
         Assert.Equal("extension", response.Extensions.GetAs<CustomError>("Ext2")?.Message);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         var json = """
            {
               "IsSuccess": true,
               "Ext1": "extension",
               "Ext2": {
                  "Message": "extension"
               }
            }
            """;
         ResponseMaybe<Unit> response = json.Fx.Json.Deserialize<ResponseMaybe<Unit>>().PayloadOrThrow();
         IsTrue(response is not null);
         Assert.Equal("extension", response.Extensions.GetAs<string>("Ext1"));
         Assert.Equal("extension", response.Extensions.GetAs<CustomError>("Ext2")?.Message);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         var json = """
            {
               "IsSuccess": true,
               "Ext1": "extension",
               "Ext2": {
                  "Message": "extension"
               }
            }
            """;
         ResponseMaybe<Unit, string> response = json.Fx.Json.Deserialize<ResponseMaybe<Unit, string>>().PayloadOrThrow();
         IsTrue(response is not null);
         Assert.Equal("extension", response.Extensions.GetAs<string>("Ext1"));
         Assert.Equal("extension", response.Extensions.GetAs<CustomError>("Ext2")?.Message);
      }
   }
   [Fact]
   public void Deserialization_Error_Extensions()
   {
      var json = """
            {
               "IsSuccess": false,
               "Error": {
                  "Type": "NotFound",
                  "Ext1": "extension",
                  "Ext2": {
                     "Message": "extension"
                  }
               }
            }
            """;
      Response<Unit> response = json.Fx.Json.Deserialize<Response<Unit>>().PayloadOrThrow();
      IsTrue(response is not null);
      IsTrue(response is Error);
      if(response is Error error)
      {
         Assert.Equal("extension", error.Extensions.GetAs<string>("Ext1"));
         Assert.Equal("extension", error.Extensions.GetAs<CustomError>("Ext2")?.Message);
      }
   }

   #endregion

   #region Extensions

   [Fact]
   public void Extensions_ReservedKeys_AreDefined()
   {
      // Response
      Assert.Equal(3, ResponseConstants.ResponseExtensionsReservedKeys.Count);
      Assert.Contains(ResponseConstants.PayloadPropertyName, ResponseConstants.ResponseExtensionsReservedKeys);
      Assert.Contains(ResponseConstants.ErrorPropertyName, ResponseConstants.ResponseExtensionsReservedKeys);
      Assert.Contains(nameof(Response<>.IsSuccess), ResponseConstants.ResponseExtensionsReservedKeys);
      // ResponseMaybe
      Assert.Equal(4, ResponseConstants.ResponseMaybeExtensionsReservedKeys.Count);
      Assert.Contains(ResponseConstants.PayloadPropertyName, ResponseConstants.ResponseMaybeExtensionsReservedKeys);
      Assert.Contains(ResponseConstants.ErrorPropertyName, ResponseConstants.ResponseMaybeExtensionsReservedKeys);
      Assert.Contains(nameof(ResponseMaybe<>.IsSuccess), ResponseConstants.ResponseMaybeExtensionsReservedKeys);
      Assert.Contains(nameof(ResponseMaybe<>.IsNone), ResponseConstants.ResponseMaybeExtensionsReservedKeys);
   }
   [Fact]
   public void Extensions_ReservedKeys_ThrowOnInit()
   {
      // Response
      foreach (var key in ResponseConstants.ResponseExtensionsReservedKeys)
         Throws<ReservedKeyExtensionException>(() => new Response<Unit>(Unit.Value)
         {
            Extensions = new()
            {
               [key] = "reserved"
            }
         });

      // ResponseMaybe
      foreach (var key in ResponseConstants.ResponseMaybeExtensionsReservedKeys)
         Throws<ReservedKeyExtensionException>(() => new ResponseMaybe<Unit>(Unit.Value)
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
      // Response
      foreach (var key in ResponseConstants.ResponseExtensionsReservedKeys)
      {
         var response = new Response<Unit>(Unit.Value);
         Throws<ReservedKeyExtensionException>(() => response.Extensions.Add(key, "reserved"));
      }

      // ResponseMaybe
      foreach (var key in ResponseConstants.ResponseExtensionsReservedKeys)
      {
         var response = new Response<Unit>(Unit.Value);
         Throws<ReservedKeyExtensionException>(() => response.Extensions.Add(key, "reserved"));
      }
   }

   #endregion

   //[Fact]
   //public void GetUserTest()
   //{
   //   var r0 = GetUser(true, false);
   //   IsTrue(r0.IsSuccess);
   //   IsTrue(r0 is User);
   //   if (r0 is User user)
   //      Assert.Equal("test", user.Name);

   //   var r1 = GetUser(false, false);
   //   IsTrue(r1.IsError);
   //   IsTrue(r1 is Error);

   //   var r2 = GetUser(false, true);
   //   IsTrue(r2.IsError);
   //   IsTrue(r2 is Error);
   //}

   //[Fact]
   //public void FindUserTest()
   //{
   //   var r0 = FindUser(true, false);
   //   IsTrue(r0.IsSuccess);
   //   IsFalse(r0.IsNone);
   //   IsFalse(r0.IsError);
   //   IsTrue(r0 is User);
   //   if (r0 is User u0)
   //   {
   //      Assert.Equal("test", u0.Name);
   //   }

   //   var r1 = FindUser(false, false);
   //   IsTrue(r1.IsSuccess);
   //   IsTrue(r1.IsNone);
   //   IsFalse(r1.IsError);
   //   IsTrue(r1 is None);

   //   var r2 = FindUser(false, true);
   //   IsTrue(r2.IsError);
   //   IsTrue(r2 is Error);

   //   // Esto solo para probar sintaxis

   //   var f = FindUser(true, false);
   //   //if(f.TryError(out var aa, out var _, out var _)){
   //   //   return;
   //   //}
   //   //var oo = aa.Age;
   //   switch (f)
   //   {
   //      case Error:
   //         Output.WriteLine("Hubo un error al buscar el usuario");
   //         break;
   //      case None:
   //         Output.WriteLine("No se encontró el usuario");
   //         break;
   //      case User u:
   //         Output.WriteLine("Es un usuario" + u.Name);
   //         break;
   //   }
   //   if (f is Error)
   //      Output.WriteLine("Hubo un error al buscar el usuario");
   //   else if (f is None)
   //      Output.WriteLine("No se encontró el usuario");
   //   else
   //      Output.WriteLine("Es un usuario" + ((User)f).Name);

   //   Assert.NotNull(f);
   //   if (f is not null)
   //   {
   //      var x3 = f switch
   //      {
   //         Error => "Hubo un error al buscar el usuario",
   //         User => "Es un usuario",
   //         None => "No se encontró el usuario",
   //         //_ => throw new NotImplementedException(),
   //      };
         
   //   }
   //}

   //public Response<Unit> DoVoid(bool haveToFail)
   //   => haveToFail
   //   ? Error.NotFound()
   //   : new Response<Unit>(Unit.Value)
   //   {
   //      Extensions = new()
   //      {
   //         ["Info"] = "This is some additional info for the void response.",
   //         ["Object"] = new
   //         {
   //            One = "one",
   //            Two = 123
   //         }
   //      }
   //   };

   //public Response<Unit> ProcessUser(bool haveToFail)
   //{
   //   var res = GetUser(true, haveToFail);
   //   if (res is Error err)
   //      return err;
   //   var user = (User)res;

   //   // Process user here ...

   //   return Unit.Value;
   //}

   //Response<User> GetUser(bool haveToFindIt, bool haveToFail)
   //   => FindUser(haveToFindIt, haveToFail) switch
   //   {
   //      Error e => e,
   //      None => Error.NotFound(),
   //      User u => u
   //   };

   //private ResponseMaybe<User> FindUser(bool haveToFindIt, bool haveToFail)
   // => haveToFail
   //   ? Error.Critical()
   //   : haveToFindIt
   //      ? new User("test", 123)
   //      : None.Value;
}
file record User(string Name, int Age);
file record CustomError(string Message);