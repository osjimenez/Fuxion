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
		builder.MapGet("endpoint-test-empty-success", () => ResponseExt.Get.Success().ToApiResult());
		builder.MapGet("endpoint-test-message-success", () => ResponseExt.Get.SuccessMessage("Success message").ToApiResult());
		builder.MapGet("endpoint-test-payload-success", () => ResponseExt.Get.SuccessPayload(new TestPayload
			{
				FirstName = "Test name",
				Age = 123
			})
			.ToApiResult());

		// ERROR
		builder.MapGet("endpoint-test-message-error", () => ResponseExt.Get.ErrorMessage("Error message").ToApiResult());
		builder.MapGet("endpoint-test-payload-error", () => ResponseExt.Get.ErrorPayload(new TestPayload
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
				return ResponseExt.Get.Success().ToApiResult();
			} catch (Exception ex)
			{
				return ResponseExt.Get.Exception(ex).ToApiResult();
			}
		});

		// BAD REQUEST
		builder.MapGet("endpoint-test-message-bad-request", () => ResponseExt.Get.InvalidData("Error message").ToApiResult());
		builder.MapGet("endpoint-test-payload-bad-request", () => ResponseExt.Get.InvalidData("Error message", new TestPayload
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