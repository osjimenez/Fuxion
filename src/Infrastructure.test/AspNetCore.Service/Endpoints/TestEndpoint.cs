using System.Net;

namespace Fuxion.AspNetCore.Service.Endpoints;

public class TestEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder builder)
	{
		// SUCCESS
		builder.MapGet("endpoint-test-empty-success", () => Response.Success().ToApiResult());
		builder.MapGet("endpoint-test-message-success", () => Response.SuccessMessage("Success message").ToApiResult());
		builder.MapGet("endpoint-test-payload-success", () => Response.SuccessPayload(new TestPayload
			{
				FirstName = "Test name",
				Age = 123
			})
			.ToApiResult());

		// ERROR
		builder.MapGet("endpoint-test-message-error", () => Response.ErrorMessage("Error message").ToApiResult());
		builder.MapGet("endpoint-test-payload-error", () => Response.ErrorPayload(new TestPayload
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
				return Response.Success().ToApiResult();
			} catch (Exception ex)
			{
				return Response.Exception(ex).ToApiResult();
			}
		});

		// BAD REQUEST
		builder.MapGet("endpoint-test-message-bad-request", () => Response.InvalidData("Error message").ToApiResult());
		builder.MapGet("endpoint-test-payload-bad-request", () => Response.InvalidData("Error message", new TestPayload
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