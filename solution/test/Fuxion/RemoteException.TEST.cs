using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Threading.Tasks;
using Fuxion.Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Test.Fuxion;

public class RemoteException_Test(ITestOutputHelper output) : BaseTest<RemoteException_Test>(output)
{
   // Builds the JSON the deserialization tests read back: a test never calls another test.
   static string SerializeExceptionJson() => ExceptionGenerator.GetException().ToExceptionJson().Fx.Json.Serialize(true).SuccessOrThrow();

   [Fact]
   public void Serialization()
   {
      var json = SerializeExceptionJson();

      AssertJson(json, [
         new([nameof(ExceptionJson.Message)],"message"),
         new([nameof(ExceptionJson.Source)],"Test.Fuxion"),
         new(["$type"],nameof(ExceptionWithData))]);
   }
   // Builds the JSON the deserialization tests read back: a test never calls another test.
   static string SerializeWithInnerJson() => ExceptionGenerator.GetExceptionWithInner().ToExceptionJson().Fx.Json.Serialize(true).SuccessOrThrow();

   [Fact]
   public void Serialization_WithInner()
   {
      var json = SerializeWithInnerJson();

      AssertJson(json, [
         new([nameof(ExceptionJson.Message)],"inner-message"),
         new(["$type"],nameof(ExceptionWithInner)),
         new([nameof(Exception.InnerException),nameof(ExceptionJson.Message)],"message"),
         new([nameof(Exception.InnerException),"$type"],nameof(ExceptionWithData)),
         new([nameof(Exception.InnerException),nameof(ExceptionJson.Source)],"Test.Fuxion")]);
   }
   // Builds the JSON the deserialization tests read back: a test never calls another test.
   static string SerializeWithLoopJson() => ExceptionGenerator
      .GetExceptionWithLoop()
      .ToExceptionJson()
      .Fx.Json.Serialize(true)
      .SuccessOrThrow();

   [Fact]
   public void Serialization_WithLoop()
   {
      var json = SerializeWithLoopJson();

      AssertJson(json, [
         new([nameof(ExceptionJson.Message)],"message")]);
   }
   // Builds the JSON the deserialization tests read back: a test never calls another test.
   static string SerializeAggregateJson() => ExceptionGenerator.GetAggregateException().ToExceptionJson().Fx.Json.Serialize(true).SuccessOrThrow();

   [Fact]
   public void Serialization_Aggregate()
   {
      var json = SerializeAggregateJson();

      AssertJson(json, [
         new([nameof(ExceptionJson.Message)],"One or more errors occurred. (message) (message)")]);
   }

   [Fact]
   public void Deserialization()
   {
      var rex = SerializeExceptionJson().Fx.Json.Deserialize<ExceptionJson>().SuccessOrThrow().ToRemoteException();

      {
         Assert.Equal("message", rex.Message);
         Assert.Equal("Test.Fuxion", rex.Source);
         Assert.Equal(nameof(ExceptionWithData), rex.RemoteType);
         Assert.NotNull(rex.RemoteStackTrace);
         var stackFrame = rex.RemoteStackTrace.First();
         Assert.NotNull(stackFrame.File);
         IsTrue(stackFrame.File.EndsWith("solution\\test\\Fuxion\\RemoteException.TEST.cs"));
         IsTrue(stackFrame.Line > 0);
         Assert.Equal("private static void Test.Fuxion.ExceptionGenerator.FailWithData()", stackFrame.Method);
      }
      {
         var exceptionJson = rex.ExceptionJson;
         Assert.Equal("message", exceptionJson.Message);
         Assert.Equal("Test.Fuxion", exceptionJson.Source);
         Assert.Equal(nameof(ExceptionWithData), exceptionJson.Type);

         Assert.NotNull(exceptionJson.Data);
         Assert.Equal(1, ((JsonElement?)exceptionJson.Data.FirstOrDefault(kvp => kvp.Key == "Key1").Value)!.Value.GetInt32());

         Assert.NotNull(exceptionJson.StackTrace);
         var stackFrame = exceptionJson.StackTrace.First();
         Assert.NotNull(stackFrame.File);
         IsTrue(stackFrame.File.EndsWith("solution\\test\\Fuxion\\RemoteException.TEST.cs"));
         IsTrue(stackFrame.Line > 0);
         Assert.Equal("private static void Test.Fuxion.ExceptionGenerator.FailWithData()", stackFrame.Method);
      }
   }
   [Fact]
   public void Deserialization_WithInner()
   {
      var rex = SerializeWithInnerJson().Fx.Json.Deserialize<ExceptionJson>().SuccessOrThrow().ToRemoteException();

      Assert.Equal("inner-message", rex.Message);
      Assert.Null(rex.Source);
      Assert.Equal(nameof(ExceptionWithInner), rex.RemoteType);
      IsTrue(rex.InnerException is RemoteException);
      if (rex.InnerException is RemoteException innerRex)
      {
         Assert.Equal("message", innerRex.Message);
         Assert.Equal("Test.Fuxion", innerRex.Source);
         Assert.Equal(nameof(ExceptionWithData), innerRex.RemoteType);
         Assert.NotNull(innerRex.RemoteStackTrace);
         var stackFrame = innerRex.RemoteStackTrace.First();
         Assert.NotNull(stackFrame.File);
         IsTrue(stackFrame.File.EndsWith("solution\\test\\Fuxion\\RemoteException.TEST.cs"));
         IsTrue(stackFrame.Line > 0);
         Assert.Equal("private static void Test.Fuxion.ExceptionGenerator.FailWithData()", stackFrame.Method);
      }
   }
   [Fact]
   public void Deserialization_WithLoop()
   {
      var rex = SerializeWithLoopJson().Fx.Json.Deserialize<ExceptionJson>().SuccessOrThrow().ToRemoteException();
      IsTrue(rex is RemoteException);
      Assert.Equal("message", rex.Message);
      Assert.Equal("Test.Fuxion", rex.Source);
      Assert.Equal(nameof(ExceptionWithLoop), rex.RemoteType);

      var loop = rex.ExceptionJson.Extensions.GetAs<Loop>("Loop");
      Assert.NotNull(loop);
      Assert.Equal("loop", loop.Name);
   }
   [Fact]
   public void Deserialization_Aggregate()
   {
      var rex = SerializeAggregateJson().Fx.Json.Deserialize<ExceptionJson>().SuccessOrThrow().ToRemoteException();

      Assert.Equal("One or more errors occurred. (message) (message)", rex.Message);
      IsTrue(rex.Source is "System.Private.CoreLib" or "mscorlib");
      Assert.Equal(nameof(AggregateException), rex.RemoteType);
      var arex = Assert.IsType<RemoteAggregateException>(rex);
      Assert.Equal(2, arex.RemoteInnerExceptions.Count);
   }

   #region Extensions

   [Fact]
   public void Extensions_ReservedKeys_AreDefined()
   {
      Assert.Equal(10, ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys.Count);
      Assert.Contains(nameof(ExceptionJson.Type), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.Message), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.Source), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.StackTrace), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.HelpLink), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.HResult), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.InnerException), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.InnerExceptions), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.Data), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
      Assert.Contains(nameof(ExceptionJson.TargetSite), ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys);
   }
   [Fact]
   public void Extensions_ReservedKeys_ThrowOnInit()
   {
      foreach (var key in ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys)
         Throws<ReservedKeyExtensionException>(() => new ExceptionJson()
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
      foreach (var key in ExceptionJsonConstants.ExceptionJsonExtensionsReservedKeys)
      {
         var exceptionJson = new ExceptionJson();
         Throws<ReservedKeyExtensionException>(() => exceptionJson.Extensions.Add(key, "reserved"));
      }
   }

   #endregion
}
file class ExceptionGenerator
{
   public static Exception GetException()
   {
      try
      {
         FailWithData();
         throw new ApplicationException("This should never be thrown");
      }
      catch (Exception ex)
      {
         return ex;
      }
   }
   public static Exception GetExceptionWithInner()
   {
      try
      {
         FailWithData();
         throw new ApplicationException("This should never be thrown");
      }
      catch (Exception ex)
      {
         return new ExceptionWithInner("inner-message", ex);
      }
   }
   public static Exception GetExceptionWithLoop()
   {
      try
      {
         FailWithLoop();
         throw new ApplicationException("This should never be thrown");
      }
      catch (Exception ex)
      {
         return ex;
      }
   }
   public static Exception GetAggregateException()
   {
      try
      {
         var t1 = TaskManager.StartNew(() => FailWithData());
         var t2 = TaskManager.StartNew(() => FailWithLoop());
         Task.WhenAll(t1, t2).Wait();
         throw new ApplicationException("This should never be thrown");
      }
      catch (Exception ex)
      {
         return ex;
      }
   }
   static void FailWithData() => throw new ExceptionWithData("message");
   static void FailWithLoop()
   {
      Loop loop = new("loop");
      loop.Data = loop;
      ExceptionWithLoop lex = new("message")
      {
         Loop = loop
      };
      throw lex;
   }
}

file class ExceptionWithData(string message) : Exception(message)
{
   public override IDictionary Data => new Dictionary<string, int>()
   {
      ["Key1"] = 1,
      ["Key2"] = 2,
   };
}
file class ExceptionWithInner(string message, Exception innerException) : Exception(message, innerException);
file class ExceptionWithLoop : Exception
{
   public ExceptionWithLoop(string message) : base(message) { }
   public ExceptionWithLoop(string message, Exception innerException) : base(message, innerException) { }
   public Loop? Loop { get; init; }
}
file record Loop(string Name)
{
   public Loop? Data { get; set; }
}