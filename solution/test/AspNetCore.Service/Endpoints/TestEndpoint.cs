using System;
using System.Net;
using System.Text.Json;
using Fuxion;
using Fuxion.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Test.AspNetCore.Service.Endpoints;

public class TestEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder builder)
	{
		// SUCCESS
		builder.MapGet("endpoint-test-empty-success", () => Response.Get.Success().ToApiResult());
		builder.MapGet("endpoint-test-message-success", () => Response.Get.SuccessMessage("Success message").ToApiResult());
		builder.MapGet("endpoint-test-payload-success", () => Response.Get.SuccessPayload(new TestPayload
			{
				FirstName = "Test name",
				Age = 123
			})
			.ToApiResult());

		// ERROR
		builder.MapGet("endpoint-test-message-error", () => Response.Get.ErrorMessage("Error message").ToApiResult());
		builder.MapGet("endpoint-test-payload-error", () => Response.Get.ErrorPayload(new TestPayload
			{
				FirstName = "Test name",
				Age = 123
			}, "Error message")
			.ToApiResult());

		builder.MapGet("endpoint-test-message-exception", () =>
		{
			try
			{
				new Level1().Throw();
				return Response.Get.Success().ToApiResult();
			} catch (Exception ex)
			{
				return Response.Get.Exception(ex).ToApiResult();
			}
		});

		// BAD REQUEST
		builder.MapGet("endpoint-test-message-bad-request", () => Response.Get.InvalidData("Error message").ToApiResult());
		builder.MapGet("endpoint-test-payload-bad-request", () => Response.Get.InvalidData("Error message", new TestPayload
			{
				FirstName = "Test name",
				Age = 123
			})
			.ToApiResult());
	}
}
public class Level1
{
	public void Throw() => new Level2().Throw();
}
public class Level2
{
	public void Throw() => throw new NotImplementedException("Not implemented");
}