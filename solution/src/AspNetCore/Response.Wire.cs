using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Fuxion.AspNetCore;

/// <summary>
/// The ASP.NET Core side of the wire contract: resolves the effective options (scope defaults + the
/// request's Accept), delegates the decision to <see cref="ResponseWireMapper"/> and writes the result.
/// </summary>
static class ResponseHttpAdapter
{
	public static bool TryMap(HttpContext httpContext, object? value, out ResponseWireMapping mapping)
	{
		// Already materialized results are never touched.
		if (value is IResult || value is IActionResult)
		{
			mapping = null!;
			return false;
		}

		return ResponseWireMapper.TryMap(value, ResponseOptionsResolver.ResolveEffective(httpContext), out mapping);
	}

	// The shape depends on Accept, so shared caches must key on it. StringValues.Contains is an exact
	// element match, so a pre-existing "Vary: Accept, Origin" needs a comma-separated-value comparison
	// (case-insensitively) instead, or Accept would be appended a second time.
	public static void MarkVaryByAccept(HttpResponse response)
	{
		var existing = response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
		foreach (var value in existing)
			if (string.Equals(value, HeaderNames.Accept, StringComparison.OrdinalIgnoreCase))
				return;

		response.Headers.Append(HeaderNames.Vary, HeaderNames.Accept);
	}

	public static JsonSerializerOptions ResolveHttpJsonOptions(HttpContext httpContext)
		=> httpContext.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions
			?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

	public static JsonSerializerOptions ResolveMvcJsonOptions(HttpContext httpContext)
		=> httpContext.RequestServices.GetService<IOptions<MvcJsonOptions>>()?.Value.JsonSerializerOptions
			?? httpContext.RequestServices.GetService<IOptions<HttpJsonOptions>>()?.Value.SerializerOptions
			?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

	public static EntityTagHeaderValue? ParseETag(string? etag)
		=> !string.IsNullOrWhiteSpace(etag) && EntityTagHeaderValue.TryParse(etag, out var parsed) ? parsed : null;
}

sealed class WireResult(ResponseWireMapping mapping) : IResult
{
	public Task ExecuteAsync(HttpContext httpContext)
	{
		if (mapping.Shape == ResponseWireShape.Binary)
			return BinaryResult((FileContent)mapping.Value!).ExecuteAsync(httpContext);

		ResponseHttpAdapter.MarkVaryByAccept(httpContext.Response);

		if (!mapping.HasBody)
			return Results.StatusCode(mapping.StatusCode).ExecuteAsync(httpContext);

		var jsonOptions = ResponseHttpAdapter.ResolveHttpJsonOptions(httpContext);
		var (contentType, body, serializerOptions) = mapping.Materialize(jsonOptions);
		return Results.Json(body, serializerOptions ?? jsonOptions, contentType, mapping.StatusCode).ExecuteAsync(httpContext);
	}

	// The framework writes the stream (and disposes it), sets Content-Disposition, Content-Length,
	// ETag/Last-Modified and serves Range requests. No Vary: a file's shape does not depend on Accept.
	static IResult BinaryResult(FileContent file)
		=> Results.File(file.Stream, file.ContentType, file.FileName, file.LastModified, ResponseHttpAdapter.ParseETag(file.ETag), file.EnableRangeProcessing);
}

sealed class WireActionResult(ResponseWireMapping mapping) : IActionResult
{
	public Task ExecuteResultAsync(ActionContext context)
	{
		var http = context.HttpContext;

		if (mapping.Shape == ResponseWireShape.Binary)
		{
			var file = (FileContent)mapping.Value!;
			return new FileStreamResult(file.Stream, file.ContentType)
			{
				FileDownloadName = file.FileName,
				LastModified = file.LastModified,
				EntityTag = ResponseHttpAdapter.ParseETag(file.ETag),
				EnableRangeProcessing = file.EnableRangeProcessing
			}.ExecuteResultAsync(context);
		}

		ResponseHttpAdapter.MarkVaryByAccept(http.Response);
		http.Response.StatusCode = mapping.StatusCode;

		if (!mapping.HasBody)
			return Task.CompletedTask;

		var jsonOptions = ResponseHttpAdapter.ResolveMvcJsonOptions(http);
		var (contentType, body, serializerOptions) = mapping.Materialize(jsonOptions);
		return http.Response.WriteAsJsonAsync(body, body!.GetType(), serializerOptions ?? jsonOptions, contentType, http.RequestAborted);
	}
}
