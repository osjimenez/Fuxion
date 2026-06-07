using Fuxion;
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

   #region Generic types initialization
   void CheckTypeInitialization(Type type)
   {
      var ex = Assert.ThrowsAny<TypeInitializationException>(() =>
      {
         RuntimeHelpers.RunClassConstructor(type.TypeHandle);
      }, tiex => tiex.InnerException is InvalidOperationException ? null : "InnerException isn't InvalidOperationException");
      Output.WriteLine("Method throw as expected: " + ex.InnerException!.Message);
   }
   [Fact]
   public void TSuccess_TError_CannotBeInitializedWithSameType()
   {
      CheckTypeInitialization(typeof(Response<string, string>));
      CheckTypeInitialization(typeof(ResponseMaybe<string, string>));
   }
   [Fact]
   public void TSuccess_CannotBeInitializedWith_None_Type()
   {
      CheckTypeInitialization(typeof(Response<None>));
      CheckTypeInitialization(typeof(Response<None, string>));
      CheckTypeInitialization(typeof(ResponseMaybe<None>));
      CheckTypeInitialization(typeof(ResponseMaybe<None, string>));
   }
   [Fact]
   public void TSuccess_CannotBeInitializedWith_Error_Type()
   {
      CheckTypeInitialization(typeof(Response<Error>));
      CheckTypeInitialization(typeof(Response<Error, string>));
      CheckTypeInitialization(typeof(ResponseMaybe<Error>));
      CheckTypeInitialization(typeof(ResponseMaybe<Error, string>));
   }
   [Fact]
   public void TError_CannotBeInitializedWith_None_Type()
   {
      CheckTypeInitialization(typeof(Response<string, None>));
      CheckTypeInitialization(typeof(ResponseMaybe<string, None>));
   }
   [Fact]
   public void TError_CannotBeInitializedWith_Error_Type()
   {
      CheckTypeInitialization(typeof(Response<string, Error>));
      CheckTypeInitialization(typeof(ResponseMaybe<string, Error>));
   }
   #endregion

   #region Serialization

   [Fact]
   public void Serialization_Success()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         Response<User> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
         Assert.Contains($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.DoesNotContain($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<User, string> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
         Assert.Contains($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.DoesNotContain($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<User> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
         Assert.Contains($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.DoesNotContain($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, string> response = new User("test", 123);
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
         Assert.Contains($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.DoesNotContain($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
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
         Assert.DoesNotContain($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.DoesNotContain($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, string> response = None.Value;
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
         Assert.DoesNotContain($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.DoesNotContain($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
   }
   [Fact]
   public void Serialization_Error()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         Response<User> response = Error.NotFound();
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
         Assert.Contains("\"Error\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      {
         var ex = new Exception("exception message");
         Response<User> response = Error.NotFound("error", new User("test", 123), ex);
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
         //Assert.DoesNotContain($"\"{ResponseConstants.PayloadPropertyName}\"", json); // INFO: Contiene 'Payload' por el Error
         Assert.Contains($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<User, string> response = "error";
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
         Assert.DoesNotContain($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.Contains($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<User> response = Error.NotFound();
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
         Assert.DoesNotContain($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.Contains($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<User, string> response = "error";
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
         Assert.DoesNotContain($"\"{ResponseConstants.PayloadPropertyName}\"", json);
         Assert.Contains($"\"{ResponseConstants.ErrorPropertyName}\"", json);
         Assert.DoesNotContain("\"Value\"", json);
      }
   }
   [Fact]
   public void Serialization_Extensions()
   {
      Output.WriteLine(" - Response<TSuccess>:");
      {
         Response<Unit> response = new Response<Unit>(Unit.Value)
         {
            Extensions = new()
            {
               ["Ext1"] = "extension",
               ["Ext2"] = new
               {
                  Message = "extension",
               }
            }
         };
         var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
         PrintVariable(json, false);
      }
      Output.WriteLine(" - Response<TSuccess, TError>:");
      {
         Response<Unit, string> response = new Response<Unit, string>(Unit.Value)
         {
            Extensions = new()
            {
               ["Ext1"] = "extension",
               ["Ext2"] = new
               {
                  Message = "extension",
               }
            }
         };
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess>:");
      {
         ResponseMaybe<Unit> response = new ResponseMaybe<Unit>(Unit.Value)
         {
            Extensions = new()
            {
               ["Ext1"] = "extension",
               ["Ext2"] = new
               {
                  Message = "extension",
               }
            }
         };
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
      }
      Output.WriteLine(" - ResponseMaybe<TSuccess, TError>:");
      {
         ResponseMaybe<Unit, string> response = new ResponseMaybe<Unit, string>(Unit.Value)
         {
            Extensions = new()
            {
               ["Ext1"] = "extension",
               ["Ext2"] = new
               {
                  Message = "extension",
               }
            }
         };
         var json = response.Fx.Json.Serialize(true).PayloadOrFallback(res => res.Exception is not null ? throw res.Exception : throw new Exception(res.Message));
         PrintVariable(json, false);
      }
   }
   [Fact]
   public void Serialization_Error_Extensions()
   {
      Response<Unit> response = new Response<Unit>(Error.NotFound(
         extensions: new()
         {
            ["Ext1"] = "extension",
            ["Ext2"] = new
            {
               Message = "extension",
            }
         }));
      var json = response.Fx.Json.Serialize(true).PayloadOrThrow();
      PrintVariable(json, false);
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
               "IsSuccess": false,
               "Error": {
                  "Exception": {
                     "Data": "this data will be ignored by deserializer"
                  },
                   "Message": "error",
                   "Payload": {
                     "Name": "test",
                     "Age": 123
                  },
                   "Type": "NotFound"
               }
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
            Assert.Null(error.Exception);
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
         {
            Assert.Equal("error", error.Message);
         }
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
   public void Extensions_ReservedKeys_ThrowOnInit()
   {
      Assert.Throws<ReservedKeyExtensionException>(() => new Response<Unit>(Unit.Value)
      {
         Extensions = new()
         {
            [nameof(Response<>.IsSuccess)] = "reserved"
         }
      });

      Assert.Throws<ReservedKeyExtensionException>(() => new Response<Unit, string>(Unit.Value)
      {
         Extensions = new()
         {
            [nameof(Response<,>.IsSuccess)] = "reserved"
         }
      });

      Assert.Throws<ReservedKeyExtensionException>(() => new ResponseMaybe<Unit>(Unit.Value)
      {
         Extensions = new()
         {
            [nameof(ResponseMaybe<>.IsNone)] = "reserved"
         }
      });

      Assert.Throws<ReservedKeyExtensionException>(() => new ResponseMaybe<Unit, string>(Unit.Value)
      {
         Extensions = new()
         {
            [nameof(ResponseMaybe<,>.IsNone)] = "reserved"
         }
      });
   }

   [Fact]
   public void MaybeExtensions_UseMaybeReservedKeys_AfterInit()
   {
      var response = new ResponseMaybe<Unit>(Unit.Value)
      {
         Extensions = new()
      };

      Assert.Throws<ReservedKeyExtensionException>(() => response.Extensions.Add(nameof(ResponseMaybe<>.IsNone), "reserved"));
   }

   #endregion

   [Fact]
   public void GetUserTest()
   {
      var r0 = GetUser(true, false);
      IsTrue(r0.IsSuccess);
      IsTrue(r0 is User);
      if (r0 is User user)
         Assert.Equal("test", user.Name);

      var r1 = GetUser(false, false);
      IsTrue(r1.IsError);
      IsTrue(r1 is Error);

      var r2 = GetUser(false, true);
      IsTrue(r2.IsError);
      IsTrue(r2 is Error);
   }

   [Fact]
   public void FindUserTest()
   {
      var r0 = FindUser(true, false);
      IsTrue(r0.IsSuccess);
      IsFalse(r0.IsNone);
      IsFalse(r0.IsError);
      IsTrue(r0 is User);
      if (r0 is User u0)
      {
         Assert.Equal("test", u0.Name);
      }

      var r1 = FindUser(false, false);
      IsTrue(r1.IsSuccess);
      IsTrue(r1.IsNone);
      IsFalse(r1.IsError);
      IsTrue(r1 is None);

      var r2 = FindUser(false, true);
      IsTrue(r2.IsError);
      IsTrue(r2 is Error);

      // Esto solo para probar sintaxis

      var f = FindUser(true, false);
      switch (f)
      {
         case Error:
            Output.WriteLine("Hubo un error al buscar el usuario");
            break;
         case None:
            Output.WriteLine("No se encontró el usuario");
            break;
         case User u:
            Output.WriteLine("Es un usuario" + u.Name);
            break;
      }
      if (f is Error)
         Output.WriteLine("Hubo un error al buscar el usuario");
      else if (f is None)
         Output.WriteLine("No se encontró el usuario");
      else
         Output.WriteLine("Es un usuario" + ((User)f).Name);

      Assert.NotNull(f);
      if (f is not null)
      {
         var x3 = f switch
         {
            Error => "Hubo un error al buscar el usuario",
            User => "Es un usuario",
            None => "No se encontró el usuario",
            //_ => throw new NotImplementedException(),
         };
      }
   }

   public Response<Unit> DoVoid(bool haveToFail)
      => haveToFail
      ? Error.NotFound()
      : new Response<Unit>(Unit.Value)
      {
         Extensions = new()
         {
            ["Info"] = "This is some additional info for the void response.",
            ["Object"] = new
            {
               One = "one",
               Two = 123
            }
         }
      };

   public Response<Unit> ProcessUser(bool haveToFail)
   {
      var res = GetUser(true, haveToFail);
      if (res is Error err)
         return err;
      var user = (User)res;

      // Process user here ...

      return Unit.Value;
   }

   public Response<User> GetUser(bool haveToFindIt, bool haveToFail)
      => FindUser(haveToFindIt, haveToFail) switch
      {
         Error e => e,
         None => Error.NotFound(),
         User u => u,
         null => Error.Critical("Invalid FindUser response state.")
      };
   public ResponseMaybe<User> FindUser(bool haveToFindIt, bool haveToFail)
    => haveToFail
      ? Error.Critical()
      : haveToFindIt
         ? new User("test", 123)
         : None.Value;
}
public record User(string Name, int Age);
public record CustomError(string Message);