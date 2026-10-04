using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fuxion.AspNetCore;

static class ResponseEndpointFilterFactory
{
	public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
	{
		// Build-time gate: endpoints that did not declare a union return type are never touched.
		if (!ResponseWireMapper.IsSupportedDeclaredResponseType(context.MethodInfo.ReturnType))
			return next;

		return async invocationContext =>
		{
			var value = await next(invocationContext);
			return ResponseHttpAdapter.TryMap(invocationContext.HttpContext, value, out var mapping)
				? new WireResult(mapping)
				: value;
		};
	}
}

sealed class ResponseActionFilter : IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context) { }

	public void OnActionExecuted(ActionExecutedContext context)
	{
		if (context.ActionDescriptor is not ControllerActionDescriptor action) return;
		if (!ResponseWireMapper.IsSupportedDeclaredResponseType(action.MethodInfo.ReturnType)) return;
		if (context.Result is not ObjectResult result) return;
		if (!ResponseHttpAdapter.TryMap(context.HttpContext, result.Value, out var mapping)) return;

		context.Result = new WireActionResult(mapping);
	}
}
