using System.Text.Json.Serialization;

namespace Fuxion;

/// <summary>
/// Defines standardized error categories for Response operations.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration provides semantic categorization of errors independent of 
/// transport protocols. These categories represent common error scenarios in 
/// business logic and application flows.
/// </para>
/// <para>
/// The error types can be mapped to various protocols:
/// </para>
/// <list type="bullet">
/// <item><description>HTTP status codes (see <see cref="Fuxion.Net.Http.Extensions"/>)</description></item>
/// <item><description>gRPC status codes</description></item>
/// <item><description>Custom error codes in your domain</description></item>
/// </list>
/// <para>
/// The <see cref="JsonStringEnumConverter"/> ensures that these values are serialized
/// as strings in JSON (e.g., "NotFound" instead of 0), improving API clarity and version resilience.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Resource not found
/// if (!userExists)
/// {
///     return Response.Get.NotFound("User not found");
/// }
/// 
/// // Validation failure
/// if (!ModelState.IsValid)
/// {
///     return Response.Get.InvalidData("Invalid input data");
/// }
/// 
/// // Permission denied
/// if (!currentUser.HasPermission(Permission.Edit))
/// {
///     return Response.Get.PermissionDenied("You don't have permission to edit this resource");
/// }
/// 
/// // Conflict (e.g., duplicate email)
/// if (emailAlreadyExists)
/// {
///     return Response.Get.Conflict("Email address already in use");
/// }
/// 
/// // Unexpected error
/// try
/// {
///     await database.SaveChangesAsync();
/// }
/// catch (Exception ex)
/// {
///     return Response.Get.Critical("An unexpected error occurred", exception: ex);
/// }
/// </code>
/// </example>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
	/// <summary>
	/// The requested resource was not found.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>A resource with the specified identifier doesn't exist</description></item>
	/// <item><description>An endpoint is valid but the resource is missing</description></item>
	/// <item><description>A query returns no results when one was expected</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// var user = await userRepository.GetByIdAsync(userId);
	/// if (user is null)
	/// {
	///     return Response.Get.NotFound($"User with ID {userId} not found");
	/// }
	/// </code>
	/// </example>
	NotFound,

	/// <summary>
	/// The request lacks valid authentication credentials or the authenticated user lacks necessary permissions.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Authentication is required but not provided or invalid</description></item>
	/// <item><description>User is authenticated but lacks permission to access the resource</description></item>
	/// <item><description>Token has expired or is invalid</description></item>
	/// <item><description>Role-based access control denies the operation</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Authentication failure
	/// if (authToken is null)
	/// {
	///     return Response.Get.PermissionDenied("Authentication required");
	/// }
	/// 
	/// // Authorization failure
	/// if (!user.IsAdmin)
	/// {
	///     return Response.Get.PermissionDenied("Admin privileges required");
	/// }
	/// </code>
	/// </example>
	PermissionDenied,

	/// <summary>
	/// The request contains invalid or malformed data that fails validation.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Input validation fails (required fields, format, range)</description></item>
	/// <item><description>Request payload is malformed or unparseable</description></item>
	/// <item><description>Business rules validation fails</description></item>
	/// <item><description>Data type mismatches or constraint violations</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Field validation
	/// if (string.IsNullOrWhiteSpace(email))
	/// {
	///     return Response.Get.InvalidData("Email is required");
	/// }
	/// 
	/// // Format validation
	/// if (!emailRegex.IsMatch(email))
	/// {
	///     return Response.Get.InvalidData("Invalid email format");
	/// }
	/// 
	/// // Business rule validation
	/// if (age &lt; 18)
	/// {
	///     return Response.Get.InvalidData("Must be 18 or older");
	/// }
	/// </code>
	/// </example>
	InvalidData,

	/// <summary>
	/// The request conflicts with the current state of the resource or with another concurrent operation.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Duplicate key or unique constraint violations</description></item>
	/// <item><description>Optimistic concurrency conflicts (version mismatch)</description></item>
	/// <item><description>State transition is not allowed</description></item>
	/// <item><description>Resource dependencies prevent the operation</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Duplicate resource
	/// if (await userRepository.ExistsByEmailAsync(email))
	/// {
	///     return Response.Get.Conflict("Email address already in use");
	/// }
	/// 
	/// // Concurrency conflict
	/// if (entity.Version != expectedVersion)
	/// {
	///     return Response.Get.Conflict("The resource has been modified by another user");
	/// }
	/// 
	/// // Invalid state transition
	/// if (order.Status == OrderStatus.Completed)
	/// {
	///     return Response.Get.Conflict("Cannot modify a completed order");
	/// }
	/// </code>
	/// </example>
	Conflict,

	/// <summary>
	/// An unexpected or critical error occurred on the server.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Unhandled exceptions occur</description></item>
	/// <item><description>System-level errors prevent operation completion</description></item>
	/// <item><description>Database connection failures</description></item>
	/// <item><description>Configuration errors</description></item>
	/// <item><description>Infinite loops or recursion detected</description></item>
	/// </list>
	/// <para><strong>Important:</strong> Log these errors for diagnostics. Don't expose internal details to clients.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// try
	/// {
	///     await ProcessDataAsync(data);
	/// }
	/// catch (Exception ex)
	/// {
	///     logger.LogError(ex, "Failed to process data");
	///     return Response.Get.Critical(
	///         "An unexpected error occurred. Please try again later.",
	///         exception: ex);
	/// }
	/// </code>
	/// </example>
	Critical,

	/// <summary>
	/// The request uses an unsupported feature, protocol, or media type.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Unsupported Content-Type or media format</description></item>
	/// <item><description>Feature not implemented in current version</description></item>
	/// <item><description>Protocol upgrade required</description></item>
	/// <item><description>Deprecated API version accessed</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Unsupported content type
	/// if (contentType != "application/json")
	/// {
	///     return Response.Get.NotSupported("Only JSON content is supported");
	/// }
	/// 
	/// // Feature not implemented
	/// if (useExperimentalFeature)
	/// {
	///     return Response.Get.NotSupported("This feature is not yet available");
	/// }
	/// </code>
	/// </example>
	NotSupported,

	/// <summary>
	/// The service is temporarily unavailable due to maintenance, overload, or external dependency failure.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Service is under maintenance</description></item>
	/// <item><description>Server is overloaded</description></item>
	/// <item><description>External dependency (database, API) is unavailable</description></item>
	/// <item><description>Rate limiting is triggered</description></item>
	/// <item><description>Circuit breaker is open</description></item>
	/// </list>
	/// <para>Consider including retry information in response extensions.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // External service unavailable
	/// if (!await paymentService.IsAvailableAsync())
	/// {
	///     var response = Response.Get.Unavailable(
	///         "Payment service is temporarily unavailable. Please try again later.");
	///     response.Extensions["retry-after-seconds"] = 60;
	///     return response;
	/// }
	/// 
	/// // Rate limiting
	/// if (requestCount > rateLimit)
	/// {
	///     return Response.Get.Unavailable("Too many requests. Please slow down.");
	/// }
	/// </code>
	/// </example>
	Unavailable,

	/// <summary>
	/// The request or an intermediate operation timed out.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Client didn't send complete request in time</description></item>
	/// <item><description>Server operation exceeded time limit</description></item>
	/// <item><description>Upstream service didn't respond in time</description></item>
	/// <item><description>Database query timeout</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// try
	/// {
	///     var result = await longRunningOperation
	///         .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
	/// }
	/// catch (TimeoutException)
	/// {
	///     return Response.Get.Timeout("The operation took too long to complete");
	/// }
	/// 
	/// // Database timeout
	/// catch (SqlException ex) when (ex.Number == -2) // SQL timeout
	/// {
	///     return Response.Get.Timeout("Database query timed out", exception: ex);
	/// }
	/// </code>
	/// </example>
	Timeout,

	/// <summary>
	/// The operation resulted in multiple errors with different error categories.
	/// </summary>
	/// <remarks>
	/// <para>Use when:</para>
	/// <list type="bullet">
	/// <item><description>Multiple responses are aggregated and their <see cref="Response.ErrorType"/> values differ</description></item>
	/// <item><description>A batch or composite operation fails for more than one reason</description></item>
	/// <item><description>No single error category accurately represents the combined failure</description></item>
	/// </list>
	/// <para>
	/// This value is especially useful in response aggregation scenarios such as <c>CombineResponses</c>,
	/// where several individual errors must be represented as a single summarized error.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var responses = new[]
	/// {
	///     Response.Get.NotFound("User not found"),
	///     Response.Get.InvalidData("Email is required")
	/// };
	/// 
	/// var combined = responses.CombineResponses();
	/// // combined.ErrorType == ErrorType.Combined
	/// </code>
	/// </example>
	Combined
}