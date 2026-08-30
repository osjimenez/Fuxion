using System;
using System.Net;
using System.Text.Json;
//using Fuxion;
using Fuxion.Union;
using Fuxion.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Test.AspNetCore.Service;
using Fuxion;
using Fuxion.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Test.AspNetCore.Union.Service.Endpoints;

public class TestEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder builder)
	{
		var minimalGroup = builder.MapGroup("minimal");

		#region FULL RESPONSE

		var responseGroup = minimalGroup.MapGroup("response");

		// SUCCESS
		responseGroup.MapGet("unit", Response<Unit> () =>
		{
			return Unit.Value;
		});

		responseGroup.MapGet("none", ResponseMaybe<Unit> () =>
		{
			return None.Value;
		});

		responseGroup.MapGet("string", Response<string> () =>
		{
			return "test";
		});

		responseGroup.MapGet("payload", Response<TestPayload> () =>
		{
			return TestPayload.Default;
		});

		responseGroup.MapGet("error-message", Response<Unit> () =>
		{
			return Error.Custom("test");
		});

		responseGroup.MapGet("error-type", Response<Unit> () =>
		{
			return Error.NotImplemented();
		});

		responseGroup.MapGet("error-payload", Response<Unit> () =>
		{
			return Error.Custom(payload: TestPayload.Default);
		});

		responseGroup.MapGet("error-exception", Response<Unit> () =>
		{
			try
			{
				new Level1().Throw();
				return Unit.Value;
			}
			catch (Exception ex)
			{
				return Error.Custom(exception: ex);
			}
		});

		responseGroup.MapGet("typed-error", Response<string, TestBusinessError> () =>
		{
			return TestBusinessError.Default;
		});

		#endregion

		#region IResult

		var resultGroup = minimalGroup.MapGroup("result");

		// Demo: subgroup overriding options to serialize full responses
		var specialGroup = minimalGroup.MapGroup("special").UseResponses(meta => meta.SerializeFullResponses = true);
		specialGroup.MapGet("payload", IResult () =>
		{
			Response<TestPayload> response = TestPayload.Default;
			return response.ToResult();
		});

		// SUCCESS
		resultGroup.MapGet("unit", () => Unit.Result);

		resultGroup.MapGet("none", () => None.Result);

		resultGroup.MapGet("string", () =>
		{
			Response<string> response = "test";
			return response.ToResult();
		});

		resultGroup.MapGet("payload", () =>
		{
			Response<TestPayload> response = TestPayload.Default;
			return response.ToResult();
		});

		resultGroup.MapGet("error-message", () =>
		{
			Response<Unit> response = Error.Custom("test");
			return response.ToResult();
		});

		resultGroup.MapGet("error-type", () =>
		{
			Response<Unit> response = Error.NotImplemented();
			return response.ToResult();
		});

		resultGroup.MapGet("error-payload", () =>
		{
			Response<Unit> response = Error.Custom(payload: TestPayload.Default);
			return response.ToResult();
		});

		resultGroup.MapGet("error-exception", () =>
		{
			try
			{
				new Level1().Throw();
				Response<Unit> response = Unit.Value;
				return response.ToResult();
			}
			catch (Exception ex)
			{
				Response<Unit> response = Error.Custom(exception: ex);
				return response.ToResult();
			}
		});
		#endregion

		// Caso 1 - NO especificamos el tipo de respuesta
		// Al menos un return tiene que ser Response para que el compilador infiera el tipo de respuesta
		resultGroup.MapGet("syntax-demo-1", () =>
		{
			if (Random.Shared.Next() > 0)
				return Error.Custom("test");
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return (ResponseMaybe<int>)123;
		});
		// Caso 2 - NO especificamos el tipo de respuesta
		// Al menos un return tiene que ser Response para que el compilador infiera el tipo de respuesta
		resultGroup.MapGet("syntax-demo-2", () =>
		{
			if (Random.Shared.Next() > 0)
				return Error.Custom("test");
			return (Response<int>)123;
		});
		// Caso 3 - SI especificamos el tipo de respuesta
		// Caso correcto
		resultGroup.MapGet("syntax-demo-3-1", ResponseMaybe<int> () =>
		{
			if (Random.Shared.Next() > 0)
				return Error.Custom("test");
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		resultGroup.MapGet("syntax-demo-3-2", async Task<ResponseMaybe<int>> () =>
		{
			await Task.Yield();
			if (Random.Shared.Next() > 0)
				return Error.Custom("test");
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		resultGroup.MapGet("syntax-demo-3-3", async ValueTask<ResponseMaybe<int>> () =>
		{
			await Task.Yield();
			if (Random.Shared.Next() > 0)
				return Error.Custom("test");
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		resultGroup.MapGet("syntax-demo-3-4", ResponseMaybe<int, string> () =>
		{
			if (Random.Shared.Next() > 0)
				return "error";
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		resultGroup.MapGet("syntax-demo-3-5", async Task<ResponseMaybe<int, string>> () =>
		{
			await Task.Yield();
			if (Random.Shared.Next() > 0)
				return "error";
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		resultGroup.MapGet("syntax-demo-3-6", async ValueTask<ResponseMaybe<int, string>> () =>
		{
			await Task.Yield();
			if (Random.Shared.Next() > 0)
				return "error";
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		// Caso 4 - SI especificamos el tipo de respuesta
		// Caso incorrecto y peligroso
		resultGroup.MapGet("syntax-demo-4", object () =>
		{
			if (Random.Shared.Next() > 0)
				return Error.Custom("test");
			else if (Random.Shared.Next() > 0)
				return None.Value;
			return 123;
		});
		// Caso 5 - NO especificamos el tipo de respuesta y respondemos solo con uno de los tipos de respuesta
		// Habrá que serializar Error directamente y convertirlo en IResult de forma adecuada (según config)
		resultGroup.MapGet("syntax-demo-5-1", () => Error.Custom("test"));
		// Habrá que serializar None directamente y convertirlo en IResult de forma adecuada (según config)
		resultGroup.MapGet("syntax-demo-5-2", () => None.Value);
		// Aqui no podemos hacer nada, el usuario devuelve el tipo que le da la gana, asi que se serializa lo que se devuelve y punto
		resultGroup.MapGet("syntax-demo-5-3", () => 123);
		// Caso 6 - Metodo directo
		// Es un caso perfecto, nada que objetar
		resultGroup.MapGet("syntax-demo-6-1", () => Do());
		resultGroup.MapGet("syntax-demo-6-2", () => DoMaybe());
		// Caso 7 - Metodo con tipo result
		// Habra que usar los métodos de extensión para convertir el Response en IResult
		// El compilador no puede inferirlo y podemos definir un conversor explícito porque el Response esta en Fuxion y no tiene los paquetes de AspNetCore
		// Una pena que no se puedan crear conversores explícitos mediante extensiones (Microsoft presentó algún diseño de esto, pero no se ha implementado finalmente)
		resultGroup.MapGet("syntax-demo-7-1", IResult () => Do().ToResult());
		resultGroup.MapGet("syntax-demo-7-2", IResult () => DoMaybe().ToResult());
		// Caso 8 - Metodos de extension para convertir cualqueir tipo en Response
		// Ayudaría a mitigar un poco la sintaxis de de demos 1 y 2
		//resultGroup.MapGet("syntax-demo-8-1", () => 123.ToResponse());
		//resultGroup.MapGet("syntax-demo-8-2", () => "".ToResponseMaybe());

		#region UNDEFINABLE

		// An undefined member must be absent from the payload, which is the JSON Merge Patch (RFC 7396)
		// convention, instead of being written with the marker object.
		var undefinableGroup = minimalGroup.MapGroup("undefinable");

		undefinableGroup.MapGet("partial", () => TestPatchPayload.PartiallyDefined);

		undefinableGroup.MapPost("echo", (TestPatchPayload payload) => new
		{
			nameDefined = payload.Name.IsDefined,
			ageDefined = payload.Age.IsDefined
		});

		#endregion
	}
	private Response<int> Do() => 123;
	private ResponseMaybe<int> DoMaybe() => None.Value;
}
//public static class Extensions
//{
//	extension<T>(T? me) where T : struct
//	{
//		public ResponseMaybe<T> ToResponseMaybe() => me is null ? None.Value : me.Value;
//	}
//	extension<T>(T? me) where T : class
//	{
//		public ResponseMaybe<T> ToResponseMaybe() => me is null ? None.Value : me;
//	}
//	extension<T>(T me) where T : notnull
//	{
//		public Response<T> ToResponse() => me;
//	}
//}
file class Level1
{
	public void Throw() => new Level2().Throw();
}
file class Level2
{
	public void Throw() => throw new NotImplementedException("Not implemented");
}
