using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Fuxion.Text.Json;
using Fuxion.Text.Json.Serialization;
using static Fuxion.Net.Http.Extensions;

namespace Fuxion.AspNet;

/// <summary>
///    Provides extension methods for converting <see cref="Response" /> and <see cref="Response{T}" /> objects
///    to ASP.NET Web API <see cref="IHttpActionResult" /> instances with support for various payload types
///    including streams, byte arrays, strings, and complex objects.
/// </summary>
/// <remarks>
///    <para>
///       This class enables seamless integration between Fuxion's Response pattern and ASP.NET Web API controllers
///       by converting responses into appropriate HTTP action results with proper status codes, content types,
///       and problem details formatting (RFC 7807).
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic status code mapping:</strong> Maps <see cref="ErrorType" /> to appropriate HTTP status
///             codes
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multiple payload types:</strong> Handles Stream, byte arrays, strings, and complex objects
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>RFC 7807 Problem Details:</strong> Generates standardized error responses with extensions
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>File download support:</strong> Specialized methods for file streams and byte arrays with content
///             disposition
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>JSON serialization:</strong> Configurable System.Text.Json serialization with custom options
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Exception handling:</strong> Optional exception details in error responses
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Async/await support:</strong> Extension methods for both synchronous and asynchronous responses
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>ErrorType to HTTP Status Code mapping:</strong>
///    </para>
///    <list type="table">
///       <listheader>
///          <term>ErrorType</term>
///          <description>HTTP Status Code</description>
///       </listheader>
///       <item>
///          <term>
///             <see cref="ErrorType.NotFound" />
///          </term>
///          <description>404 Not Found</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.PermissionDenied" />
///          </term>
///          <description>403 Forbidden</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.InvalidData" />
///          </term>
///          <description>400 Bad Request</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.Conflict" />
///          </term>
///          <description>409 Conflict</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.Critical" />
///          </term>
///          <description>500 Internal Server Error</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.NotSupported" />
///          </term>
///          <description>501 Not Implemented</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.Unavailable" />
///          </term>
///          <description>503 Service Unavailable</description>
///       </item>
///       <item>
///          <term>
///             <see cref="ErrorType.Timeout" />
///          </term>
///          <description>408 Request Timeout</description>
///       </item>
///    </list>
///    <para>
///       <strong>Configuration:</strong>
///    </para>
///    <para>
///       The class provides static configuration properties:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <see cref="IncludeException" />: Controls whether exception details are included in error responses
///             (default: true)
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="JsonSerializerOptions" />: Custom JSON serialization options (default: null, uses default
///             options)
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage with generic response:</strong>
///    <code>
/// public class UsersController : ApiController
/// {
///     [HttpGet]
///     public async Task&lt;IHttpActionResult&gt; GetUser(int id)
///     {
///         var response = await _userService.GetUserAsync(id);
///         return await response.ToApiResultAsync();
///     }
/// }
/// // Success: 200 OK with JSON body
/// // NotFound: 404 Not Found with problem details
/// </code>
///    <strong>File download with stream:</strong>
///    <code>
/// [HttpGet]
/// public async Task&lt;IHttpActionResult&gt; DownloadReport(int id)
/// {
///     var response = await _reportService.GenerateReportStreamAsync(id);
///     return await response.ToApiFileStreamResultAsync(
///         contentType: "application/pdf",
///         fileDownloadName: "report.pdf");
/// }
/// // Success: 200 OK with Content-Disposition: attachment; filename="report.pdf"
/// </code>
///    <strong>File download with byte array:</strong>
///    <code>
/// [HttpGet]
/// public async Task&lt;IHttpActionResult&gt; DownloadImage(int id)
/// {
///     var response = await _imageService.GetImageBytesAsync(id);
///     return await response.ToApiFileBytesResultAsync(
///         contentType: "image/png",
///         fileDownloadName: "photo.png");
/// }
/// </code>
///    <strong>Full response serialization (includes all Response properties):</strong>
///    <code>
/// [HttpPost]
/// public async Task&lt;IHttpActionResult&gt; ProcessData(DataRequest request)
/// {
///     var response = await _dataService.ProcessAsync(request);
///     return await response.ToApiResultAsync(fullSerialization: true);
/// }
/// // Returns complete Response object with IsSuccess, ErrorType, Message, etc.
/// </code>
///    <strong>Custom JSON options:</strong>
///    <code>
/// // In Startup or Global.asax
/// ResponseExtensions.JsonSerializerOptions = new JsonSerializerOptions
/// {
///     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
///     WriteIndented = true
/// };
/// ResponseExtensions.IncludeException = false; // Don't expose exceptions in production
/// </code>
///    <strong>Error handling example:</strong>
///    <code>
/// [HttpDelete]
/// public async Task&lt;IHttpActionResult&gt; DeleteUser(int id)
/// {
///     var response = await _userService.DeleteUserAsync(id);
///     return await response.ToApiResultAsync();
/// }
/// // If user not found:
/// // 404 Not Found with body:
/// // {
/// //   "type": "https://httpwg.org/specs/rfc9110.html#section-15.5.5",
/// //   "status": 404,
/// //   "title": "Not found",
/// //   "detail": "User with id 123 not found",
/// //   "exception": { /* exception details if IncludeException is true */ }
/// // }
/// </code>
/// </example>
public static class ResponseExtensions
{
	/// <summary>
	///    Gets or sets a value indicating whether exception details should be included in error responses.
	/// </summary>
	/// <value>
	///    <c>true</c> to include exception details in error responses; otherwise, <c>false</c>.
	///    Default is <c>true</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       When set to <c>true</c>, exceptions are serialized using <see cref="ExceptionConverter" />
	///       and included in the "exception" extension field of the problem details response.
	///    </para>
	///    <para>
	///       <strong>Security consideration:</strong> In production environments, consider setting this to
	///       <c>false</c> to avoid exposing sensitive implementation details through stack traces and
	///       exception messages.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // In Application_Start or Startup
	/// #if DEBUG
	///     ResponseExtensions.IncludeException = true;
	/// #else
	///     ResponseExtensions.IncludeException = false;
	/// #endif
	/// </code>
	/// </example>
	public static bool IncludeException { get; set; } = true;

	/// <summary>
	///    Gets or sets the <see cref="JsonSerializerOptions" /> to use for JSON serialization.
	/// </summary>
	/// <value>
	///    Custom JSON serializer options, or <c>null</c> to use default options.
	///    Default is <c>null</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       These options are used for serializing response payloads and problem details.
	///       The options are cloned before use to avoid modification issues.
	///    </para>
	///    <para>
	///       If <see cref="IncludeException" /> is <c>true</c>, an <see cref="ExceptionConverter" />
	///       is automatically added to the converters when serializing error responses.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// ResponseExtensions.JsonSerializerOptions = new JsonSerializerOptions
	/// {
	///     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	///     WriteIndented = true,
	///     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	///     Converters = { new JsonStringEnumConverter() }
	/// };
	/// </code>
	/// </example>
	public static JsonSerializerOptions? JsonSerializerOptions { get; set; }

	// Core helper used by all extensions
	/// <summary>
	///    Core implementation method that converts an <see cref="Response" /> to an <see cref="IHttpActionResult" />.
	/// </summary>
	/// <param name="me">The response to convert.</param>
	/// <param name="contentType">The content type for the response. If null, uses value from response extensions or defaults.</param>
	/// <param name="fileDownloadName">The file name for Content-Disposition header when returning files.</param>
	/// <param name="fullSerialization">
	///    If <c>true</c>, serializes the entire Response object including metadata.
	///    If <c>false</c>, only serializes the payload or message.
	/// </param>
	/// <returns>An <see cref="IHttpActionResult" /> representing the HTTP response.</returns>
	/// <remarks>
	///    <para>
	///       This method handles the following scenarios:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             <strong>Success with Stream payload:</strong> Returns 200 OK with StreamContent and
	///             Content-Disposition attachment header
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Success with byte array payload:</strong> Returns 200 OK with ByteArrayContent and
	///             Content-Disposition attachment header
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Success with string payload:</strong> Returns 200 OK with StringContent,
	///             optionally serializing the full Response object
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Success with object payload:</strong> Returns 200 OK with JSON serialized payload or full Response
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Success without payload:</strong> Returns 204 No Content or 200 OK with message
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Error:</strong> Returns appropriate status code with RFC 7807 Problem Details
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Problem Details structure:</strong> Error responses follow RFC 7807 format with:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><c>type</c>: RFC 9110 reference URL for the status code</description>
	///       </item>
	///       <item>
	///          <description><c>status</c>: HTTP status code (integer)</description>
	///       </item>
	///       <item>
	///          <description><c>title</c>: Human-readable summary</description>
	///       </item>
	///       <item>
	///          <description><c>detail</c>: Detailed error message from Response.Message</description>
	///       </item>
	///       <item>
	///          <description>
	///             <c>extensions</c>: Additional data including payload and exception (if enabled)
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	private static IHttpActionResult ToApiResultCore(
		Response me,
		string? contentType,
		string? fileDownloadName,
		bool fullSerialization)
	{
		if (me.IsSuccess)
		{
			if (me.TryGetPayload(out var payload))
			{
				if (payload is Stream stream)
					return Factory.Ok(new StreamContent(stream)
					{
						Headers =
						{
							ContentDisposition = new("attachment")
							{
								FileName = fileDownloadName ??
								           (me.Extensions.TryGetValue(FileNameKey, out var fileNameExt) &&
								            fileNameExt is string fn
									           ? fn
									           : null)
							},
							ContentType = new(contentType ??
							                  (me.Extensions.TryGetValue(ContentTypeKey, out var contentTypeExt) &&
							                   contentTypeExt is string con
								                  ? con
								                  : null)),
							ContentLength =
								me.Extensions.TryGetValue(ContentLengthKey, out var extension) && extension is long length
									? length
									: -1
						}
					});
				if (payload is IEnumerable<byte> bytes)
					return Factory.Ok(new ByteArrayContent(bytes.ToArray())
					{
						Headers =
						{
							ContentDisposition = new("attachment")
							{
								FileName = fileDownloadName ??
								           (me.Extensions.TryGetValue(FileNameKey, out var fileNameExt) &&
								            fileNameExt is string fn
									           ? fn
									           : null)
							},
							ContentType = new(contentType ??
							                  (me.Extensions.TryGetValue(ContentTypeKey, out var contentTypeExt) &&
							                   contentTypeExt is string con
								                  ? con
								                  : null)),
							ContentLength =
								me.Extensions.TryGetValue(ContentLengthKey, out var extension) && extension is long length
									? length
									: -1
						}
					});
				if (payload is string str)
					return fullSerialization
						? me.Fx.Json.Serialize(options: JsonSerializerOptions != null ? new(JsonSerializerOptions) : null)
							.Match(
								r => Factory.Ok(new StringContent(r.Payload, Encoding.UTF8, "application/json")),
								r => throw new JsonException("Error serializing response", r.Exception))
						: Factory.Ok(new StringContent(str, Encoding.UTF8, contentType ?? "text/plain"));
				return fullSerialization
					? me.Fx.Json.Serialize(options: JsonSerializerOptions != null ? new(JsonSerializerOptions) : null)
						.Match(
							r => Factory.Ok(new StringContent(r.Payload, Encoding.UTF8, "application/json")),
							r => throw new JsonException("Error serializing response", r.Exception))
					: payload.Fx.Json
						.Serialize(options: JsonSerializerOptions != null ? new(JsonSerializerOptions) : null).Match(
							r => Factory.Ok(new StringContent(r.Payload, Encoding.UTF8, "application/json")),
							r => throw new JsonException("Error serializing response", r.Exception));
			}

			if (me.Message is not null || fullSerialization)
				return fullSerialization
					? me.Fx.Json.Serialize(options: JsonSerializerOptions != null ? new(JsonSerializerOptions) : null).Match(
						r => Factory.Ok(new StringContent(r.Payload, Encoding.UTF8, "application/json")),
						r => throw new JsonException("Error serializing response", r.Exception))
					: Factory.Ok(new StringContent(me.Message, Encoding.UTF8, contentType ?? "text/plain"));
			return Factory.NoContent();
		}

		var extensions = me.Extensions.ToDictionary(e => e.Key, e => e.Value);
		extensions.Remove(StatusCodeKey);
		extensions.Remove(ReasonPhraseKey);

		if (me.TryGetPayload(out var payload2) && payload2 is not Stream) extensions[PayloadKey] = payload2;
		if (IncludeException && me.Exception is not null)
		{
			var jsonOptions = JsonSerializerOptions is null
				? new()
				{
					Converters =
					{
						new ExceptionConverter()
					}
				}
				: JsonSerializerOptions.Map(o =>
				{
					var res = new JsonSerializerOptions(o);
					res.Converters.Add(new ExceptionConverter());
					return res;
				});
			extensions[ExceptionKey] = JsonSerializer.SerializeToElement(me.Exception, jsonOptions);
		}

		return me.ErrorType switch
		{
			ErrorType.NotFound => Factory.Problem(me.Message, HttpStatusCode.NotFound, "Not found", extensions),
			ErrorType.PermissionDenied => Factory.Problem(me.Message, HttpStatusCode.Forbidden, "Forbidden", extensions),
			ErrorType.InvalidData => Factory.Problem(me.Message, HttpStatusCode.BadRequest, "Bad request", extensions),
			ErrorType.Conflict => Factory.Problem(me.Message, HttpStatusCode.Conflict, "Conflict", extensions),
			ErrorType.Critical => Factory.Problem(me.Message, HttpStatusCode.InternalServerError, "Internal server error",
				extensions),
			ErrorType.NotSupported => Factory.Problem(me.Message, HttpStatusCode.NotImplemented, "Not implemented",
				extensions),
			ErrorType.Unavailable => Factory.Problem(me.Message, HttpStatusCode.ServiceUnavailable, "Service unavailable",
				extensions),
			ErrorType.Timeout => Factory.Problem(me.Message, HttpStatusCode.RequestTimeout, "Request timeout", extensions),
			_ => Factory.Problem(me.Message, HttpStatusCode.InternalServerError, "Internal server error", extensions)
		};
	}

	// Task<IResponse<TPayload>> receivers (general)
	// Task<Response<TPayload>> receivers (general)
	extension<TPayload>(Task<Response<TPayload>> me)
	{
		/// <summary>
		///    Converts a <see cref="Task{Response}" /> to a <see cref="Task{IHttpActionResult}" />.
		/// </summary>
		/// <param name="fullSerialization">
		///    If <c>true</c>, serializes the entire Response object including metadata.
		///    If <c>false</c>, only serializes the payload value or message.
		/// </param>
		/// <returns>
		///    A task that represents the asynchronous operation. The task result contains an
		///    <see cref="IHttpActionResult" /> representing the HTTP response.
		/// </returns>
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, fullSerialization);
	}

	extension(Task<Response<string>> me)
	{
		/// <summary>
		///    Converts a <see cref="Task{Response}" /> with string payload to a <see cref="Task{IHttpActionResult}" />.
		/// </summary>
		/// <param name="fullSerialization">
		///    If <c>true</c>, serializes the entire Response object.
		///    If <c>false</c>, returns the string payload directly.
		/// </param>
		/// <param name="contentType">
		///    The content type for the response. Default is "text/plain".
		/// </param>
		/// <returns>
		///    A task that represents the asynchronous operation. The task result contains an
		///    <see cref="IHttpActionResult" /> with the string content.
		/// </returns>
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false,
			string contentType = "text/plain ")
			=> ToApiResultCore(await me, contentType, null, fullSerialization);
	}

	// Stream payload specializations
	extension<TPayload>(Task<Response<TPayload>> me) where TPayload : Stream
	{
		/// <summary>
		///    Converts a <see cref="Task{Response}" /> with <see cref="Stream" /> payload to a file download result.
		/// </summary>
		/// <param name="contentType">The MIME content type for the file.</param>
		/// <param name="fileDownloadName">The filename for the Content-Disposition header.</param>
		/// <returns>
		///    A task that represents the asynchronous operation. The task result contains an
		///    <see cref="IHttpActionResult" /> with the stream as downloadable content.
		/// </returns>
		public async Task<IHttpActionResult> ToApiFileStreamResultAsync(string? contentType = null,
			string? fileDownloadName = null)
			=> ToApiResultCore(await me, contentType, fileDownloadName, false);
	}

	extension<TPayload>(Response<TPayload> me) where TPayload : Stream
	{
		/// <summary>
		///    Converts an <see cref="Response" /> with <see cref="Stream" /> payload to a file download result.
		/// </summary>
		/// <param name="contentType">The MIME content type for the file.</param>
		/// <param name="fileDownloadName">The filename for the Content-Disposition header.</param>
		/// <returns>An <see cref="IHttpActionResult" /> with the stream as downloadable content.</returns>
		public IHttpActionResult ToApiFileStreamResult(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(me, contentType, fileDownloadName, false);
	}

	// Bytes payload specializations
	extension<TPayload>(Task<Response<TPayload>> me) where TPayload : IEnumerable<byte>
	{
		/// <summary>
		///    Converts a <see cref="Task{Response}" /> with byte array payload to a file download result.
		/// </summary>
		/// <param name="contentType">The MIME content type for the file.</param>
		/// <param name="fileDownloadName">The filename for the Content-Disposition header.</param>
		/// <returns>
		///    A task that represents the asynchronous operation. The task result contains an
		///    <see cref="IHttpActionResult" /> with the bytes as downloadable content.
		/// </returns>
		public async Task<IHttpActionResult> ToApiFileBytesResultAsync(string? contentType = null,
			string? fileDownloadName = null)
			=> ToApiResultCore(await me, contentType, fileDownloadName, false);
	}

	extension<TPayload>(Response<TPayload> me) where TPayload : IEnumerable<byte>
	{
		/// <summary>
		///    Converts an <see cref="Response" /> with byte array payload to a file download result.
		/// </summary>
		/// <param name="contentType">The MIME content type for the file.</param>
		/// <param name="fileDownloadName">The filename for the Content-Disposition header.</param>
		/// <returns>An <see cref="IHttpActionResult" /> with the bytes as downloadable content.</returns>
		public IHttpActionResult ToApiFileBytesResult(string? contentType = null, string? fileDownloadName = null)
			=> ToApiResultCore(me, contentType, fileDownloadName, false);
	}

	extension(Task<Response> me)
	{
		/// <summary>
		///    Converts a <see cref="Task{Response}" /> (non-generic) to a <see cref="Task{IHttpActionResult}" />.
		/// </summary>
		/// <param name="fullSerialization">
		///    If <c>true</c>, serializes the entire Response object.
		///    If <c>false</c>, returns the message only or 204 No Content if no message.
		/// </param>
		/// <returns>
		///    A task that represents the asynchronous operation. The task result contains an
		///    <see cref="IHttpActionResult" />.
		/// </returns>
		public async Task<IHttpActionResult> ToApiResultAsync(bool fullSerialization = false)
			=> ToApiResultCore(await me, null, null, fullSerialization);
	}

	extension(Response me)
	{
		/// <summary>
		///    Converts an <see cref="Response" /> (non-generic) to an <see cref="IHttpActionResult" />.
		/// </summary>
		/// <param name="fullSerialization">
		///    If <c>true</c>, serializes the entire Response object.
		///    If <c>false</c>, returns the message only or 204 No Content if no message.
		/// </param>
		/// <returns>An <see cref="IHttpActionResult" /> representing the HTTP response.</returns>
		public IHttpActionResult ToApiResult(bool fullSerialization = false)
			=> ToApiResultCore(me, null, null, fullSerialization);
	}
}

/// <summary>
///    Internal implementation of <see cref="IHttpActionResult" /> that uses a factory function
///    to generate <see cref="HttpResponseMessage" /> instances.
/// </summary>
/// <param name="func">
///    A function that takes a <see cref="CancellationToken" /> and returns a
///    <see cref="Task{HttpResponseMessage}" />.
/// </param>
/// <remarks>
///    This file-scoped class provides a lightweight wrapper for creating custom HTTP action results
///    without the overhead of creating dedicated classes for each response type.
/// </remarks>
file class FuncHttpActionResult(Func<CancellationToken, Task<HttpResponseMessage>> func) : IHttpActionResult
{
	Task<HttpResponseMessage> IHttpActionResult.ExecuteAsync(CancellationToken cancellationToken)
		=> func(cancellationToken);
}

/// <summary>
///    Factory class for creating <see cref="IHttpActionResult" /> instances with various HTTP status codes
///    and content types, including RFC 7807 Problem Details support.
/// </summary>
/// <param name="func">
///    A function that generates the <see cref="HttpResponseMessage" /> asynchronously.
/// </param>
/// <remarks>
///    <para>
///       This file-scoped factory class provides static methods for creating common HTTP responses:
///    </para>
///    <list type="bullet">
///       <item>
///          <description><see cref="Ok" />: Creates 200 OK responses with optional content</description>
///       </item>
///       <item>
///          <description><see cref="NoContent" />: Creates 204 No Content responses</description>
///       </item>
///       <item>
///          <description>
///             <see cref="Problem" />: Creates RFC 7807 Problem Details responses with custom status codes
///          </description>
///       </item>
///    </list>
///    <para>
///       The factory also includes comprehensive HTTP status code to RFC 9110 URL mapping via
///       <see cref="GetTypeFromStatusCode" />.
///    </para>
/// </remarks>
file class Factory(Func<CancellationToken, Task<HttpResponseMessage>> func) : IHttpActionResult
{
	/// <summary>
	///    Creates a 200 OK response with optional content.
	/// </summary>
	/// <param name="content">The HTTP content to include in the response, or <c>null</c> for no content.</param>
	/// <returns>A <see cref="Factory" /> instance representing the OK response.</returns>
	public static Factory Ok(HttpContent? content = null) => Create(HttpStatusCode.OK, content);

	/// <summary>
	///    Creates a 204 No Content response.
	/// </summary>
	/// <returns>A <see cref="Factory" /> instance representing the No Content response.</returns>
	public static Factory NoContent() => Create(HttpStatusCode.NoContent);

	/// <summary>
	///    Creates an RFC 7807 Problem Details response with the specified status code and details.
	/// </summary>
	/// <param name="detail">A human-readable explanation specific to this occurrence of the problem.</param>
	/// <param name="statusCode">The HTTP status code for the problem.</param>
	/// <param name="title">A short, human-readable summary of the problem type.</param>
	/// <param name="extensions">
	///    Additional members to include in the problem details, such as validation errors or exception details.
	/// </param>
	/// <returns>
	///    A <see cref="Factory" /> instance representing the problem details response with
	///    Content-Type: application/problem+json.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The problem details response includes:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><c>type</c>: A URI reference from RFC 9110 identifying the problem type</description>
	///       </item>
	///       <item>
	///          <description><c>status</c>: The HTTP status code</description>
	///       </item>
	///       <item>
	///          <description><c>title</c>: A short summary (e.g., "Not found")</description>
	///       </item>
	///       <item>
	///          <description><c>detail</c>: Detailed explanation of the error</description>
	///       </item>
	///       <item>
	///          <description>Additional extension members from the <paramref name="extensions" /> dictionary</description>
	///       </item>
	///    </list>
	///    <para>
	///       The response is serialized using the configured <see cref="ResponseExtensions.JsonSerializerOptions" />.
	///    </para>
	/// </remarks>
	public static Factory Problem(string? detail, HttpStatusCode statusCode, string title,
		Dictionary<string, object?>? extensions)
		=> Create(statusCode, new StringContent(new ResponseProblemDetails
				{
					Type = GetTypeFromStatusCode(statusCode),
					Status = (int)statusCode,
					Title = title,
					Detail = detail,
					Extensions = extensions ?? new(StringComparer.Ordinal)
				}.Fx.Json
				.Serialize(options: ResponseExtensions.JsonSerializerOptions != null
					? new(ResponseExtensions.JsonSerializerOptions)
					: null)
				.PayloadOrFallback(r => throw new JsonException("Error serializing response problem", r.Exception)),
			Encoding.UTF8,
			"application/problem+json"));

	/// <summary>
	///    Creates a basic <see cref="Factory" /> instance with the specified status code and optional content.
	/// </summary>
	/// <param name="status">The HTTP status code for the response.</param>
	/// <param name="content">The HTTP content to include in the response, or <c>null</c> for no content.</param>
	/// <returns>A <see cref="Factory" /> instance representing the response.</returns>
	public static Factory Create(HttpStatusCode status, HttpContent? content = null)
		=> new(_ =>
		{
			var msg = new HttpResponseMessage(status);
			if (content is not null) msg.Content = content;
			return Task.FromResult(msg);
		});

	/// <summary>
	///    Maps an HTTP status code to its corresponding RFC 9110 specification URL.
	/// </summary>
	/// <param name="status">The HTTP status code to map.</param>
	/// <returns>
	///    A string containing the RFC 9110 URL for the status code
	///    (e.g., "https://httpwg.org/specs/rfc9110.html#section-15.5.5" for 404).
	/// </returns>
	/// <exception cref="NotImplementedException">
	///    Thrown when the status code is not explicitly supported in the mapping.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method provides comprehensive mapping for standard HTTP status codes defined in RFC 9110:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>1xx: Informational responses (Continue, Switching Protocols)</description>
	///       </item>
	///       <item>
	///          <description>2xx: Successful responses (OK, Created, Accepted, No Content, etc.)</description>
	///       </item>
	///       <item>
	///          <description>3xx: Redirection messages (Multiple Choices, Moved Permanently, etc.)</description>
	///       </item>
	///       <item>
	///          <description>
	///             4xx: Client error responses (Bad Request, Unauthorized, Forbidden, Not Found, etc.)
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             5xx: Server error responses (Internal Server Error, Not Implemented, Service Unavailable, etc.)
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       The URLs point to the official HTTP Semantics specification (RFC 9110) hosted at httpwg.org.
	///    </para>
	/// </remarks>
	private static string GetTypeFromStatusCode(HttpStatusCode status)
		=> status switch
		{
			HttpStatusCode.Continue => "https://httpwg.org/specs/rfc9110.html#section-15.2.1", // 100
			HttpStatusCode.SwitchingProtocols => "https://httpwg.org/specs/rfc9110.html#section-15.2.2", // 101

			HttpStatusCode.OK => "https://httpwg.org/specs/rfc9110.html#section-15.3.1", // 200
			HttpStatusCode.Created => "https://httpwg.org/specs/rfc9110.html#section-15.3.2", // 201
			HttpStatusCode.Accepted => "https://httpwg.org/specs/rfc9110.html#section-15.3.3", // 202
			HttpStatusCode.NonAuthoritativeInformation => "https://httpwg.org/specs/rfc9110.html#section-15.3.4", // 203
			HttpStatusCode.NoContent => "https://httpwg.org/specs/rfc9110.html#section-15.3.5", // 204
			HttpStatusCode.ResetContent => "https://httpwg.org/specs/rfc9110.html#section-15.3.6", // 205
			HttpStatusCode.PartialContent => "https://httpwg.org/specs/rfc9110.html#section-15.3.7", // 206

			HttpStatusCode.MultipleChoices => "https://httpwg.org/specs/rfc9110.html#section-15.4.1", // 300
			HttpStatusCode.MovedPermanently => "https://httpwg.org/specs/rfc9110.html#section-15.4.2", // 301
			HttpStatusCode.Found => "https://httpwg.org/specs/rfc9110.html#section-15.4.3", // 302
			HttpStatusCode.SeeOther => "https://httpwg.org/specs/rfc9110.html#section-15.4.4", // 303
			HttpStatusCode.NotModified => "https://httpwg.org/specs/rfc9110.html#section-15.4.5", // 304
			HttpStatusCode.UseProxy => "https://httpwg.org/specs/rfc9110.html#section-15.4.6", // 305
			HttpStatusCode.Unused => "https://httpwg.org/specs/rfc9110.html#section-15.4.7", // 306
			HttpStatusCode.TemporaryRedirect => "https://httpwg.org/specs/rfc9110.html#section-15.4.8", // 307

			HttpStatusCode.BadRequest => "https://httpwg.org/specs/rfc9110.html#section-15.5.1", // 400
			HttpStatusCode.Unauthorized => "https://httpwg.org/specs/rfc9110.html#section-15.5.2", // 401
			HttpStatusCode.PaymentRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.3", // 402
			HttpStatusCode.Forbidden => "https://httpwg.org/specs/rfc9110.html#section-15.5.4", // 403
			HttpStatusCode.NotFound => "https://httpwg.org/specs/rfc9110.html#section-15.5.5", // 404
			HttpStatusCode.MethodNotAllowed => "https://httpwg.org/specs/rfc9110.html#section-15.5.6", // 405
			HttpStatusCode.NotAcceptable => "https://httpwg.org/specs/rfc9110.html#section-15.5.7", // 406
			HttpStatusCode.ProxyAuthenticationRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.8", // 407
			HttpStatusCode.RequestTimeout => "https://httpwg.org/specs/rfc9110.html#section-15.5.9", // 408
			HttpStatusCode.Conflict => "https://httpwg.org/specs/rfc9110.html#section-15.5.10", // 409
			HttpStatusCode.Gone => "https://httpwg.org/specs/rfc9110.html#section-15.5.11", // 410
			HttpStatusCode.LengthRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.12", // 411
			HttpStatusCode.PreconditionFailed => "https://httpwg.org/specs/rfc9110.html#section-15.5.13", // 412
			HttpStatusCode.RequestEntityTooLarge => "https://httpwg.org/specs/rfc9110.html#section-15.5.14", // 413
			HttpStatusCode.RequestUriTooLong => "https://httpwg.org/specs/rfc9110.html#section-15.5.15", // 414
			HttpStatusCode.UnsupportedMediaType => "https://httpwg.org/specs/rfc9110.html#section-15.5.16", // 415
			HttpStatusCode.RequestedRangeNotSatisfiable => "https://httpwg.org/specs/rfc9110.html#section-15.5.17", // 416
			HttpStatusCode.ExpectationFailed => "https://httpwg.org/specs/rfc9110.html#section-15.5.18", // 417
			HttpStatusCode.UpgradeRequired => "https://httpwg.org/specs/rfc9110.html#section-15.5.19", // 426

			HttpStatusCode.InternalServerError => "https://httpwg.org/specs/rfc9110.html#section-15.6.1", // 500
			HttpStatusCode.NotImplemented => "https://httpwg.org/specs/rfc9110.html#section-15.6.2", // 501
			HttpStatusCode.BadGateway => "https://httpwg.org/specs/rfc9110.html#section-15.6.3", // 502
			HttpStatusCode.ServiceUnavailable => "https://httpwg.org/specs/rfc9110.html#section-15.6.4", // 503
			HttpStatusCode.GatewayTimeout => "https://httpwg.org/specs/rfc9110.html#section-15.6.5", // 504
			HttpStatusCode.HttpVersionNotSupported => "https://httpwg.org/specs/rfc9110.html#section-15.6.6", // 505

			_ => throw new NotImplementedException($"Status code '{status}' is not supported")
		};

	Task<HttpResponseMessage> IHttpActionResult.ExecuteAsync(CancellationToken cancellationToken)
		=> func(cancellationToken);
}