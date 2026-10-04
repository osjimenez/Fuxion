using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fuxion.AspNetCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseResultExtensions
{
	extension(IResponse me)
	{
		/// <summary>Deferred: the effective options (scope + Accept) are resolved when the result executes.</summary>
		public IResult ToResult() => new DeferredResponseResult(me);

		/// <summary>Explicit options, for callers without an HTTP context. Accept is not consulted.</summary>
		public IResult ToResult(ResponseOptions options)
			=> new WireResult(ResponseWireMapper.Map(me, options));

		public IActionResult ToActionResult() => new DeferredResponseActionResult(me);

		public IActionResult ToActionResult(ResponseOptions options)
			=> new WireActionResult(ResponseWireMapper.Map(me, options));
	}

	sealed class DeferredResponseResult(IResponse response) : IResult
	{
		public Task ExecuteAsync(HttpContext httpContext)
			=> new WireResult(ResponseWireMapper.Map(response, ResponseOptionsResolver.ResolveEffective(httpContext))).ExecuteAsync(httpContext);
	}

	sealed class DeferredResponseActionResult(IResponse response) : IActionResult
	{
		public Task ExecuteResultAsync(ActionContext context)
			=> new WireActionResult(ResponseWireMapper.Map(response, ResponseOptionsResolver.ResolveEffective(context.HttpContext))).ExecuteResultAsync(context);
	}
}

/// <summary>Shortcuts to map the <see cref="Unit"/> value to an HTTP response.</summary>
public static class UnitResponseExtensions
{
	extension(Unit)
	{
		/// <summary>An <see cref="IResult"/> representing an operation that completed without a result.</summary>
		public static IResult Result => ((IResponse)(Response<Unit>)Unit.Value).ToResult();

		/// <summary>An <see cref="IActionResult"/> representing an operation that completed without a result.</summary>
		public static IActionResult ActionResult => ((IResponse)(Response<Unit>)Unit.Value).ToActionResult();
	}
}

/// <summary>Shortcuts to map the <see cref="None"/> value to an HTTP response.</summary>
public static class NoneResponseExtensions
{
	extension(None)
	{
		/// <summary>An <see cref="IResult"/> representing the absence of a result.</summary>
		public static IResult Result => ((IResponse)(ResponseMaybe<Unit>)None.Value).ToResult();

		/// <summary>An <see cref="IActionResult"/> representing the absence of a result.</summary>
		public static IActionResult ActionResult => ((IResponse)(ResponseMaybe<Unit>)None.Value).ToActionResult();
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
