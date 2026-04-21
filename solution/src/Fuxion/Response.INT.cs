using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Fuxion.Text.Json.Serialization;

namespace Fuxion;

/// <summary>
/// Defines the contract for a Response object representing the result of an operation.
/// </summary>
/// <remarks>
/// <para>
/// This interface establishes the core structure of the Result pattern (Railway Oriented Programming)
/// in Fuxion, providing a standardized way to handle operation outcomes without relying on exceptions
/// for flow control.
/// </para>
/// <para>
/// The interface supports:
/// </para>
/// <list type="bullet">
/// <item><description>Success/failure state tracking</description></item>
/// <item><description>Descriptive error messages</description></item>
/// <item><description>Typed error categorization (ErrorType enum or custom types)</description></item>
/// <item><description>Exception capturing for diagnostics</description></item>
/// <item><description>Extensible metadata dictionary for arbitrary data</description></item>
/// <item><description>JSON serialization with custom converters</description></item>
/// </list>
/// <para>
/// <strong>Implementation note:</strong> This interface uses <see cref="InterfaceSerializerConverter{T}"/>
/// to enable proper JSON serialization/deserialization of interface types to their concrete implementations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Method returning IResponse
/// public IResponse DeleteUser(int userId)
/// {
///     try
///     {
///         userRepository.Delete(userId);
///         return Response.Get.Success();
///     }
///     catch (NotFoundException)
///     {
///         return Response.Get.NotFound($"User {userId} not found");
///     }
///     catch (Exception ex)
///     {
///         return Response.Get.Critical("Failed to delete user", exception: ex);
///     }
/// }
/// 
/// // Using the response
/// var result = DeleteUser(123);
/// if (result.IsSuccess)
/// {
///     Console.WriteLine("User deleted successfully");
/// }
/// else
/// {
///     Console.WriteLine($"Error: {result.Message}");
///     if (result.Exception != null)
///     {
///         logger.LogError(result.Exception, "Delete operation failed");
///     }
/// }
/// </code>
/// </example>
[JsonConverter(typeof(InterfaceSerializerConverter<IResponse>))]
public interface IResponse
{
	/// <summary>
	/// Gets a value indicating whether the operation was successful.
	/// </summary>
	/// <value>true if the operation succeeded; otherwise, false.</value>
	/// <remarks>
	/// This is the primary property for determining the outcome of an operation.
	/// When true, the operation completed successfully and any payload data (in IResponse&lt;T&gt;) is valid.
	/// </remarks>
	bool IsSuccess { get; }

	/// <summary>
	/// Gets a value indicating whether the operation failed.
	/// </summary>
	/// <value>true if the operation failed; otherwise, false.</value>
	/// <remarks>
	/// This is the inverse of <see cref="IsSuccess"/> and is provided for convenience and code readability.
	/// It is excluded from JSON serialization to avoid redundancy.
	/// </remarks>
	[JsonIgnore]
	bool IsError { get; }

	/// <summary>
	/// Gets the message describing the result of the operation.
	/// </summary>
	/// <value>A human-readable message, or null if no message was provided.</value>
	/// <remarks>
	/// This property should contain user-friendly text suitable for display in UIs or API responses.
	/// For success responses, it may describe what was accomplished. For error responses, it should
	/// explain what went wrong in clear, actionable terms without exposing sensitive system details.
	/// This property is omitted from JSON when null.
	/// </remarks>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	string? Message { get; }

	/// <summary>
	/// Gets the error type categorization for failed operations.
	/// </summary>
	/// <value>
	/// An object representing the error type (commonly an <see cref="ErrorType"/> enum value),
	/// or null for successful operations or uncategorized errors.
	/// </value>
	/// <remarks>
	/// <para>
	/// This property enables semantic error handling by categorizing failures into standard types
	/// such as NotFound, InvalidData, PermissionDenied, etc. The type is intentionally defined as
	/// <see cref="object"/> to allow flexibility:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Use the standard <see cref="ErrorType"/> enum for common scenarios</description></item>
	/// <item><description>Use custom enum types for domain-specific error categorization</description></item>
	/// <item><description>Use string codes or other types if needed for specific use cases</description></item>
	/// </list>
	/// <para>
	/// When mapping to transport protocols (HTTP, gRPC, etc.), use extension methods in the
	/// appropriate namespace (e.g., <see cref="Fuxion.Net.Http.Extensions"/> for HTTP status code mapping).
	/// </para>
	/// <para>This property is omitted from JSON when null.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Using standard ErrorType
	/// if (response.ErrorType is ErrorType type)
	/// {
	///     switch (type)
	///     {
	///         case ErrorType.NotFound:
	///             return NotFound(response.Message);
	///         case ErrorType.InvalidData:
	///             return BadRequest(response.Message);
	///         default:
	///             return StatusCode(500, response.Message);
	///     }
	/// }
	/// 
	/// // Using custom error type
	/// if (response.ErrorType is MyCustomErrorType customType)
	/// {
	///     HandleCustomError(customType);
	/// }
	/// </code>
	/// </example>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	object? ErrorType { get; }

	/// <summary>
	/// Gets the exception that caused the operation to fail, if any.
	/// </summary>
	/// <value>The exception that occurred, or null if no exception was captured.</value>
	/// <remarks>
	/// <para>
	/// This property captures the original exception for logging, diagnostics, and debugging purposes
	/// while keeping the error information encapsulated in the Response object. This supports the
	/// Result pattern by avoiding exception-based flow control.
	/// </para>
	/// <para>
	/// Uses <see cref="ExceptionConverter"/> for JSON serialization, which handles exception
	/// serialization safely by excluding non-serializable properties and capturing essential
	/// information like type, message, and stack trace.
	/// </para>
	/// <para>
	/// <strong>Important:</strong> When exposing responses through APIs, ensure exception details
	/// are not leaked to end users in production environments. Log the full exception server-side
	/// but return sanitized messages to clients.
	/// </para>
	/// <para>This property is omitted from JSON when null or default.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// try
	/// {
	///     await database.SaveChangesAsync();
	///     return Response.Get.Success();
	/// }
	/// catch (Exception ex)
	/// {
	///     // Log with full exception details
	///     logger.LogError(ex, "Database operation failed");
	///     
	///     // Return response with exception for diagnostics
	///     return Response.Get.Critical(
	///         "An unexpected error occurred",
	///         exception: ex
	///     );
	/// }
	/// </code>
	/// </example>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonConverter(typeof(ExceptionConverter))]
	Exception? Exception { get; }

	/// <summary>
	/// Gets the dictionary of extension data for attaching arbitrary metadata to the response.
	/// </summary>
	/// <value>
	/// A dictionary with case-sensitive string keys containing additional response metadata.
	/// Never null - initialized to an empty dictionary if no extensions are provided.
	/// </value>
	/// <remarks>
	/// <para>
	/// This dictionary is serialized as JSON extension data using <see cref="JsonExtensionDataAttribute"/>,
	/// meaning the key-value pairs are serialized as properties at the root level of the JSON object
	/// rather than nested under an "Extensions" property.
	/// </para>
	/// <para>
	/// Common use cases for extensions include:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>HTTP integration:</strong> status-code, reason-phrase, content-type, headers</description></item>
	/// <item><description><strong>RFC 7807 Problem Details:</strong> inner-problem, detail, instance, type</description></item>
	/// <item><description><strong>Validation errors:</strong> Field-level error details for InvalidData responses</description></item>
	/// <item><description><strong>Correlation/tracing:</strong> correlation-id, trace-id, span-id</description></item>
	/// <item><description><strong>Performance metrics:</strong> processing-time-ms, query-count, cache-hit</description></item>
	/// <item><description><strong>Pagination:</strong> page, page-size, total-count, has-next-page</description></item>
	/// <item><description><strong>Custom metadata:</strong> Any domain-specific data relevant to the response</description></item>
	/// </list>
	/// <para>
	/// Extension keys defined in <see cref="Fuxion.Net.Http.Extensions"/>:
	/// InnerProblemKey, JsonContentKey, JsonErrorKey, StringContentKey, PayloadKey, ExceptionKey,
	/// StatusCodeKey, ReasonPhraseKey, ContentLengthKey, ContentTypeKey, FileNameKey
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Adding HTTP metadata
	/// var response = Response.Get.Success();
	/// response.Extensions["status-code"] = 200;
	/// response.Extensions["correlation-id"] = Guid.NewGuid();
	/// 
	/// // Adding validation errors
	/// var validationResponse = Response.Get.InvalidData("Validation failed");
	/// validationResponse.Extensions["errors"] = new Dictionary&lt;string, string[]&gt;
	/// {
	///     ["Email"] = new[] { "Required", "Invalid format" },
	///     ["Age"] = new[] { "Must be 18 or older" }
	/// };
	/// 
	/// // Adding pagination metadata
	/// var paginatedResponse = Response.Get.SuccessPayload(users);
	/// paginatedResponse.Extensions["page"] = pageNumber;
	/// paginatedResponse.Extensions["page-size"] = pageSize;
	/// paginatedResponse.Extensions["total-count"] = totalCount;
	/// paginatedResponse.Extensions["has-next-page"] = hasNextPage;
	/// 
	/// // Accessing extensions
	/// if (response.Extensions.TryGetValue("status-code", out var statusCode))
	/// {
	///     Console.WriteLine($"HTTP Status: {statusCode}");
	/// }
	/// </code>
	/// </example>
	[JsonExtensionData]
	ResponseExtensionsDictionary Extensions { get; }

	/// <summary>
	/// PEND DOC
	/// </summary>
	/// <param name="payload"></param>
	/// <returns></returns>
	bool TryGetPayload([NotNullWhen(true)] out object? payload);
}

/// <summary>
/// Defines the contract for a Response object with a typed payload containing operation result data.
/// </summary>
/// <typeparam name="TPayload">The type of the payload value returned on success.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IResponse"/> to add strongly-typed payload support. The payload
/// represents the data returned by a successful operation (query results, created entities, etc.).
/// </para>
/// <para>
/// <strong>Key behaviors:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>When <see cref="IsSuccess"/> is true, <see cref="Payload"/> is guaranteed non-null (via <see cref="MemberNotNullWhenAttribute"/>)</description></item>
/// <item><description>When <see cref="IsSuccess"/> is false, <see cref="Payload"/> is typically null or default</description></item>
/// <item><description>The payload cannot be another Response type to prevent confusing nested structures</description></item>
/// <item><description>Nullable reference type annotations provide compile-time safety for payload access</description></item>
/// </list>
/// <para>
/// <strong>Framework compatibility:</strong> The interface has different implementations for modern .NET
/// (8+) vs older frameworks (.NET Standard 2.0, .NET Framework 4.7.2) due to default interface member limitations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query returning typed payload
/// public async Task&lt;IResponse&lt;User&gt;&gt; GetUserByIdAsync(int userId)
/// {
///     var user = await userRepository.GetByIdAsync(userId);
///     if (user == null)
///     {
///         return Response.Get.NotFound($"User {userId} not found").AsPayload&lt;User&gt;();
///     }
///     return Response.Get.SuccessPayload(user);
/// }
/// 
/// // Using the response
/// var response = await GetUserByIdAsync(123);
/// if (response.IsSuccess)
/// {
///     // Compiler knows Payload is not null here
///     var user = response.Payload;
///     Console.WriteLine($"Found user: {user.Name}");
/// }
/// else
/// {
///     Console.WriteLine($"Error: {response.Message}");
/// }
/// 
/// // Pattern matching
/// var result = response.Match(
///     success: r => $"User: {r.Payload.Name}",
///     error: r => $"Error: {r.Message}"
/// );
/// </code>
/// </example>
public interface IResponse<out TPayload> : IResponse
{
#if !STANDARD_OR_OLD_FRAMEWORKS
	/// <summary>
	/// Gets a value indicating whether the operation was successful.
	/// </summary>
	/// <value>true if the operation succeeded (and Payload is not null); otherwise, false.</value>
	/// <remarks>
	/// <para>
	/// This property overrides <see cref="IResponse.IsSuccess"/> to add nullable analysis via
	/// <see cref="MemberNotNullWhenAttribute"/>, informing the compiler that when this returns true,
	/// <see cref="Payload"/> is guaranteed not to be null.
	/// </para>
	/// <para>
	/// This provides compile-time safety when accessing the payload after checking success:
	/// </para>
	/// <code>
	/// if (response.IsSuccess)
	/// {
	///     // No null check needed - compiler knows Payload is not null
	///     Console.WriteLine(response.Payload.ToString());
	/// }
	/// </code>
	/// <para>
	/// <strong>Note:</strong> This is a default interface member available in .NET 8+ and C# 8+.
	/// For older frameworks, see the #else section which requires concrete implementation.
	/// </para>
	/// </remarks>
	[MemberNotNullWhen(true, nameof(Payload))]
	new bool IsSuccess => (this as IResponse).IsSuccess;

	/// <summary>
	/// Gets a value indicating whether the operation failed.
	/// </summary>
	/// <value>true if the operation failed (and Payload might be null); otherwise, false.</value>
	/// <remarks>
	/// This property overrides <see cref="IResponse.IsError"/> to add nullable analysis with
	/// <see cref="MemberNotNullWhenAttribute"/>, indicating the relationship between failure
	/// and potentially null payload. Excluded from JSON serialization.
	/// </remarks>
	[MemberNotNullWhen(false, nameof(Payload))]
	new bool IsError => (this as IResponse).IsError;
#else
	/// <summary>
	/// Gets a value indicating whether the operation was successful.
	/// </summary>
	/// <value>true if the operation succeeded (and Payload is not null); otherwise, false.</value>
	/// <remarks>
	/// <para>
	/// This property override is required for older frameworks (.NET Standard 2.0, .NET Framework 4.7.2)
	/// that don't support default interface members. Concrete implementations must provide this property.
	/// </para>
	/// <para>
	/// The <see cref="MemberNotNullWhenAttribute"/> provides nullable analysis, informing the compiler
	/// that when this returns true, <see cref="Payload"/> is guaranteed not to be null.
	/// </para>
	/// </remarks>
	[MemberNotNullWhen(true, nameof(Payload))]
	new bool IsSuccess { get; }
	
	/// <summary>
	/// Gets a value indicating whether the operation failed.
	/// </summary>
	/// <value>true if the operation failed (and Payload might be null); otherwise, false.</value>
	/// <remarks>
	/// This property override is required for older frameworks. The <see cref="MemberNotNullWhenAttribute"/>
	/// indicates the relationship between failure and potentially null payload. Excluded from JSON serialization.
	/// </remarks>
	[MemberNotNullWhen(false, nameof(Payload))]
	[JsonIgnore]
	new bool IsError { get; }
#endif

	/// <summary>
	/// Gets the payload value containing the operation's result data.
	/// </summary>
	/// <value>
	/// The result data when successful; typically null or default when the operation failed.
	/// </value>
	/// <remarks>
	/// <para>
	/// <strong>Nullability contract:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>When <see cref="IsSuccess"/> is true: Payload is guaranteed non-null (enforced by <see cref="MemberNotNullWhenAttribute"/> on IsSuccess)</description></item>
	/// <item><description>When <see cref="IsSuccess"/> is false: Payload is typically null or default(TPayload)</description></item>
	/// </list>
	/// <para>
	/// The compiler uses nullable reference type analysis to enforce safe access patterns:
	/// </para>
	/// <code>
	/// // Safe - compiler knows Payload is not null
	/// if (response.IsSuccess)
	/// {
	///     var data = response.Payload;
	///     ProcessData(data); // No null check needed
	/// }
	/// 
	/// // Compiler warning - Payload might be null
	/// var unsafeData = response.Payload;
	/// ProcessData(unsafeData); // ⚠ Potential null reference
	/// </code>
	/// <para>
	/// <strong>Implementation restriction:</strong> Concrete implementations (e.g., <see cref="ResponseBase{TPayload}"/>)
	/// validate that TPayload is not another Response type to prevent confusing nested Response structures.
	/// </para>
	/// <para>This property is omitted from JSON when null or default.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Successful operation with payload
	/// IResponse&lt;List&lt;User&gt;&gt; response = Response.Get.SuccessPayload(users);
	/// if (response.IsSuccess)
	/// {
	///     // Payload is guaranteed non-null
	///     foreach (var user in response.Payload)
	///     {
	///         Console.WriteLine(user.Name);
	///     }
	/// }
	/// 
	/// // Error operation - payload is null
	/// IResponse&lt;User&gt; errorResponse = Response.Get.NotFound("User not found").AsPayload&lt;User&gt;();
	/// if (errorResponse.IsError)
	/// {
	///     Console.WriteLine(errorResponse.Message);
	///     // errorResponse.Payload is null here
	/// }
	/// 
	/// // Using with pattern matching
	/// var result = response.Match(
	///     success: r => r.Payload.Count, // Safe access - compiler knows Payload is not null
	///     error: r => 0
	/// );
	/// </code>
	/// </example>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	TPayload? Payload { get; }

	/// <summary>
	/// PEND DOC
	/// </summary>
	/// <returns></returns>
	TPayload? PayloadOrDefault();
}