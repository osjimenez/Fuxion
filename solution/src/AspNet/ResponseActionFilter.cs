using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Fuxion.AspNet;

/// <summary>
/// Maps union return values of Web API 2 actions to the wire contract. Only actions whose declared
/// return type is a union shape are touched; everything else keeps the framework's behaviour.
/// </summary>
sealed class ResponseActionFilter : ActionFilterAttribute
{
	public override void OnActionExecuted(HttpActionExecutedContext context)
	{
		if (context.Exception is not null || context.Response is null) return;
		// ReflectedHttpActionDescriptor.ReturnType is null for void and non-generic Task actions: there is
		// no declared union shape to check, so this is the same as "not supported", not a null to dereference.
		if (context.ActionContext.ActionDescriptor.ReturnType is not { } declared || !ResponseWireMapper.IsSupportedDeclaredResponseType(declared)) return;

		var original = context.Response;
		if (!original.TryGetContentValue<object>(out var value)) return;

		var request = context.Request;
		if (!ResponseWireMapper.TryMap(value, ResponseOptionsResolver.ResolveEffective(request), out var mapping)) return;

		// Build the replacement before disposing the original: Materialize/Write can still read from
		// the extracted value, and the original's ObjectContent must not be dropped without disposal.
		var replacement = ResponseHttpWriter.Write(request, mapping, ResponseOptionsResolver.ResolveJsonOptions(request));
		original.Dispose();
		context.Response = replacement;
	}
}
