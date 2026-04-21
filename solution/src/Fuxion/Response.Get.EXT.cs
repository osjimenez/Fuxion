using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fuxion;

/// <summary>
/// Factory methods for creating IResponse objects (accessed via IResponse.Get).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Typed error checking methods (IsNotFound, IsInvalidData, etc.)</description></item>
/// </list>
/// </remarks>
public static partial class ResponseExtensions
{
	/// <summary>
	/// Factory methods for creating IResponse objects (accessed via IResponse.Get).
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class enables the fluent syntax: <c>IResponse.Get.Success()</c>, <c>IResponse.Get.NotFound()</c>, etc.
	/// All factory methods support optional extension data for attaching metadata to responses.
	/// </para>
	/// <para>
	/// The class provides three categories of factory methods:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Success methods:</strong> Success(), SuccessMessage(), SuccessPayload()</description></item>
	/// <item><description><strong>Generic error methods:</strong> ErrorMessage(), ErrorPayload(), Exception()</description></item>
	/// <item><description><strong>Typed error methods:</strong> NotFound(), InvalidData(), PermissionDenied(), etc.</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Simple success
	/// var response = IResponse.Get.Success();
	/// 
	/// // Success with metadata
	/// var successWithData = IResponse.Get.SuccessMessage(
	///     "User created",
	///     extensions: new[] { ("user-id", (object?)123), ("created-at", DateTime.UtcNow) }
	/// );
	/// 
	/// // Typed error with full details
	/// var notFound = IResponse.Get.NotFound(
	///     message: "User not found",
	///     exception: dbException,
	///     extensions: new[] { ("user-id", (object?)userId), ("search-timestamp", DateTime.UtcNow) }
	/// );
	/// </code>
	/// </example>
	public class ResponseGetExtensions;

	private static readonly ResponseGetExtensions Get = new();
	extension(Response)
	{
		/// <summary>
		/// Gets the IResponse factory methods for creating IResponse instances.
		/// </summary>
		/// <value>An instance providing access to static factory methods like Success(), Error(), etc.</value>
		/// <remarks>
		/// This property provides a fluent API for creating IResponse objects through the ResponseExtensions class.
		/// Used as: <c>IResponse.Get.Success()</c>, <c>IResponse.Get.Error()</c>, etc.
		/// </remarks>
		public static ResponseGetExtensions Get => Get;
	}
	/// <summary>
	/// Error type checking extension methods for IResponse.
	/// </summary>
	extension(IResponse me)
	{
		/// <summary>Checks if the response indicates a resource was not found.</summary>
		public bool IsNotFound
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.NotFound);
		}
		/// <summary>Checks if the response indicates a permission denial (authentication or authorization failure).</summary>
		public bool IsPermissionDenied
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.PermissionDenied);
		}
		/// <summary>Checks if the response indicates invalid or malformed data.</summary>
		public bool IsInvalidData
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.InvalidData);
		}
		/// <summary>Checks if the response indicates a resource conflict.</summary>
		public bool IsConflict
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.Conflict);
		}
		/// <summary>Checks if the response indicates a critical or unexpected error.</summary>
		public bool IsCritical
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.Critical);
		}
		/// <summary>Checks if the response indicates an unsupported operation or feature.</summary>
		public bool IsNotSupported
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.NotSupported);
		}
		/// <summary>Checks if the response indicates the service is temporarily unavailable.</summary>
		public bool IsUnavailable
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.Unavailable);
		}
		/// <summary>Checks if the response indicates a timeout occurred.</summary>
		public bool IsTimeout
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me.IsErrorType(ErrorType.Timeout);
		}
	}


	/// <summary>
	/// Instance wrapper providing access to IResponse factory methods.
	/// </summary>
	extension(ResponseGetExtensions me)
	{
		/// <summary>
		/// Creates a successful response without a message or payload.
		/// </summary>
		/// <param name="extensions">
		/// Optional collection of key-value pairs to add to the response's Extensions dictionary.
		/// Useful for attaching metadata such as correlation IDs, timestamps, or operation-specific data.
		/// </param>
		/// <returns>A successful IResponse with IsSuccess = true.</returns>
		/// <remarks>
		/// Use this when an operation succeeds but doesn't need to return data or a descriptive message.
		/// Common scenarios: DELETE operations, void commands, acknowledgments.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Simple success
		/// return IResponse.Get.Success();
		/// 
		/// // Success with correlation ID
		/// return IResponse.Get.Success(extensions: new[] 
		/// { 
		///     ("correlation-id", (object?)Guid.NewGuid()),
		///     ("processed-at", DateTime.UtcNow)
		/// });
		/// </code>
		/// </example>
		public IResponse Success(IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new ResponseBase(true)
			{
				Extensions = new(extensions)
			};

		/// <summary>
		/// Creates a successful response with a descriptive message.
		/// </summary>
		/// <param name="message">
		/// A human-readable message describing the successful operation.
		/// Should be clear and actionable for API consumers or UI display.
		/// </param>
		/// <param name="extensions">
		/// Optional collection of key-value pairs to add to the response's Extensions dictionary.
		/// </param>
		/// <returns>A successful IResponse with IsSuccess = true and the specified message.</returns>
		/// <remarks>
		/// Use this when you want to provide feedback about what succeeded.
		/// Common in APIs where confirmation messages are helpful for the client.
		/// </remarks>
		/// <example>
		/// <code>
		/// return IResponse.Get.SuccessMessage("User profile updated successfully");
		/// 
		/// // With tracking data
		/// return IResponse.Get.SuccessMessage(
		///     "Order processed",
		///     extensions: new[] 
		///     { 
		///         ("order-id", (object?)"ORD-12345"),
		///         ("processing-time-ms", 145)
		///     }
		/// );
		/// </code>
		/// </example>
		public IResponse SuccessMessage(string message, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new ResponseBase(true, message)
			{
				Extensions = new(extensions)
			};

		/// <summary>
		/// Creates a successful response with a typed payload containing the operation's result data.
		/// </summary>
		/// <typeparam name="TPayload">The type of the payload being returned.</typeparam>
		/// <param name="payload">
		/// The data to return. Must not be null for successful responses (enforced by IResponse&lt;T&gt; constructor).
		/// Can be any type except another IResponse to avoid nesting.
		/// </param>
		/// <param name="message">
		/// Optional human-readable message describing the success. 
		/// Can be null if the payload itself is self-explanatory.
		/// </param>
		/// <param name="extensions">
		/// Optional collection of key-value pairs to add to the response's Extensions dictionary.
		/// Commonly used for pagination metadata, cache information, or query performance metrics.
		/// </param>
		/// <returns>A successful IResponse&lt;TPayload&gt; with IsSuccess = true and the specified payload.</returns>
		/// <remarks>
		/// This is the most common success factory method for queries and operations that return data.
		/// The payload is guaranteed non-null when IsSuccess is true due to nullable reference type annotations.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Simple query result
		/// var user = await userRepository.GetByIdAsync(userId);
		/// return IResponse.Get.SuccessPayload(user);
		/// 
		/// // With descriptive message
		/// return IResponse.Get.SuccessPayload(
		///     users,
		///     message: "Retrieved 10 users"
		/// );
		/// 
		/// // With pagination metadata
		/// return IResponse.Get.SuccessPayload(
		///     users,
		///     message: $"Page {pageNumber} of {totalPages}",
		///     extensions: new[] 
		///     { 
		///         ("page", (object?)pageNumber),
		///         ("page-size", pageSize),
		///         ("total-count", totalCount),
		///         ("has-next-page", hasNextPage)
		///     }
		/// );
		/// </code>
		/// </example>
		public IResponse<TPayload> SuccessPayload<TPayload>(TPayload payload, string? message = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new ResponseBase<TPayload>(true, payload, message)
			{
				Extensions = new(extensions)
			};

		/// <summary>
		/// Creates a generic error response with a message and optional error type.
		/// </summary>
		/// <param name="message">
		/// A human-readable error message describing what went wrong.
		/// Should be clear enough for debugging but safe to show to end users.
		/// </param>
		/// <param name="type">
		/// Optional error type categorization (typically an ErrorType enum value).
		/// If null, the error is uncategorized. Use specific factory methods (NotFound, InvalidData, etc.) 
		/// for typed errors instead when possible.
		/// </param>
		/// <param name="exception">
		/// Optional exception that caused the error. Useful for logging and diagnostics.
		/// Automatically serialized in a safe way by the IResponse JSON converter.
		/// </param>
		/// <param name="extensions">
		/// Optional collection of key-value pairs to add to the response's Extensions dictionary.
		/// Commonly used for error codes, validation details, or trace information.
		/// </param>
		/// <returns>An error IResponse with IsSuccess = false and the specified error details.</returns>
		/// <remarks>
		/// Use this for generic errors when specific typed methods (NotFound, InvalidData, etc.) don't fit.
		/// For most scenarios, prefer using typed error methods for better error handling and HTTP status code mapping.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Generic error
		/// return IResponse.Get.ErrorMessage("An unexpected error occurred");
		/// 
		/// // With error type
		/// return IResponse.Get.ErrorMessage(
		///     "Payment gateway timeout",
		///     type: ErrorType.Timeout
		/// );
		/// 
		/// // With exception and tracking
		/// catch (Exception ex)
		/// {
		///     return IResponse.Get.ErrorMessage(
		///         "Failed to process order",
		///         type: ErrorType.Critical,
		///         exception: ex,
		///         extensions: new[] 
		///         { 
		///             ("order-id", (object?)orderId),
		///             ("timestamp", DateTime.UtcNow),
		///             ("correlation-id", correlationId)
		///         }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse ErrorMessage(string message, object? type = null, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new ResponseBase(false, message, type, exception)
			{
				Extensions = new(extensions)
			};

		/// <summary>
		/// Creates an error response with a payload (rare case for partial success or error details).
		/// </summary>
		/// <typeparam name="TPayload">The type of the error payload.</typeparam>
		/// <param name="payload">
		/// The error payload data. Can be validation errors, partial results, or error-specific information.
		/// Unlike success responses, this can be null for error responses.
		/// </param>
		/// <param name="message">
		/// Optional human-readable error message.
		/// </param>
		/// <param name="type">
		/// Optional error type categorization (typically an ErrorType enum value).
		/// </param>
		/// <param name="exception">
		/// Optional exception that caused the error.
		/// </param>
		/// <param name="extensions">
		/// Optional collection of key-value pairs to add to the response's Extensions dictionary.
		/// </param>
		/// <returns>An error IResponse&lt;TPayload&gt; with IsSuccess = false and the specified error details.</returns>
		/// <remarks>
		/// <para>
		/// This is an uncommon pattern. Most errors don't need a payload - use ErrorMessage() instead.
		/// </para>
		/// <para>Common valid use cases:</para>
		/// <list type="bullet">
		/// <item><description>Returning ValidationErrors collection for InvalidData responses</description></item>
		/// <item><description>Partial results when some operations succeeded but others failed</description></item>
		/// <item><description>Structured error details for complex error scenarios</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Validation errors
		/// var validationErrors = new ValidationErrors
		/// {
		///     Errors = new[] 
		///     {
		///         new ValidationError { Field = "Email", Message = "Invalid format" },
		///         new ValidationError { Field = "Age", Message = "Must be 18+" }
		///     }
		/// };
		/// return IResponse.Get.ErrorPayload(
		///     validationErrors,
		///     message: "Validation failed",
		///     type: ErrorType.InvalidData
		/// );
		/// 
		/// // Partial batch results
		/// var batchResult = new BatchResult
		/// {
		///     SuccessCount = 7,
		///     FailedCount = 3,
		///     FailedItems = new[] { item1, item8, item10 }
		/// };
		/// return IResponse.Get.ErrorPayload(
		///     batchResult,
		///     message: "Batch completed with errors",
		///     type: ErrorType.Conflict
		/// );
		/// </code>
		/// </example>
		public IResponse<TPayload> ErrorPayload<TPayload>(
			TPayload payload,
			string? message = null,
			object? type = null,
			Exception? exception = null,
			IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new ResponseBase<TPayload>(false, payload, message, type, exception)
			{
				Extensions = new(extensions)
			};

		/// <summary>
		/// Creates an error response directly from an exception, optionally overriding the message.
		/// </summary>
		/// <param name="exception">
		/// The exception to convert to a IResponse. The exception type and message will be used
		/// if no custom message is provided.
		/// </param>
		/// <param name="message">
		/// Optional custom error message. If null, uses "{ExceptionType}: {ExceptionMessage}" format.
		/// Use a custom message to provide more context or user-friendly text.
		/// </param>
		/// <returns>An error IResponse with IsSuccess = false containing the exception details.</returns>
		/// <remarks>
		/// <para>
		/// This is a convenience method for catch blocks where you want to convert an exception
		/// directly to a IResponse without additional processing.
		/// </para>
		/// <para>
		/// The exception is automatically attached to the IResponse for logging and diagnostics.
		/// By default, no ErrorType is set - consider using typed error methods if you know the error category.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// try
		/// {
		///     await database.SaveChangesAsync();
		///     return IResponse.Get.Success();
		/// }
		/// catch (Exception ex)
		/// {
		///     // Use exception message as-is
		///     return IResponse.Get.Exception(ex);
		///     // Message will be: "SqlException: Connection timeout expired"
		/// }
		/// 
		/// // With custom message for end users
		/// catch (DbUpdateException ex)
		/// {
		///     return IResponse.Get.Exception(
		///         ex,
		///         message: "Failed to save changes to the database"
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse Exception(Exception exception, string? message = null)
			=> new ResponseBase(false, message ?? $"{exception.GetType().Name}: {exception.Message}", exception: exception);

		// ==================== TYPED ERROR FACTORY METHODS ====================
		// Each ErrorType has two overloads: one without payload, one with payload

		/// <summary>
		/// Creates a NotFound error response indicating a requested resource doesn't exist.
		/// </summary>
		/// <param name="message">
		/// Error message describing what wasn't found. Default is "Not found".
		/// Be specific: "User with ID 123 not found" is better than just "Not found".
		/// </param>
		/// <param name="exception">
		/// Optional exception that led to the not found result (e.g., database query exception).
		/// </param>
		/// <param name="extensions">
		/// Optional metadata such as the resource ID that wasn't found, search criteria, etc.
		/// </param>
		/// <returns>An error IResponse with ErrorType.NotFound.</returns>
		/// <example>
		/// <code>
		/// var user = await db.Users.FindAsync(userId);
		/// if (user == null)
		/// {
		///     return IResponse.Get.NotFound(
		///         $"User with ID {userId} not found",
		///         extensions: new[] { ("user-id", (object?)userId) }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse NotFound(string message = "Not found", Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.NotFound, exception, extensions);

		/// <summary>
		/// Creates a NotFound error response with a payload containing additional details about what wasn't found.
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload containing search criteria or related information.</typeparam>
		/// <param name="message">Error message describing what wasn't found.</param>
		/// <param name="payload">Payload with search criteria, attempted filters, or related resources that do exist.</param>
		/// <param name="exception">Optional exception that led to the not found result.</param>
		/// <param name="extensions">Optional metadata.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.NotFound.</returns>
		public IResponse<TPayload> NotFound<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.NotFound, exception, extensions);

		/// <summary>
		/// Creates a PermissionDenied error response indicating authentication or authorization failure.
		/// </summary>
		/// <param name="message">
		/// Error message explaining why access was denied.
		/// Be security-conscious: don't reveal too much about system internals.
		/// Good: "You don't have permission to access this resource"
		/// Avoid: "Admin role required but you have User role"
		/// </param>
		/// <param name="exception">Optional exception from authentication/authorization logic.</param>
		/// <param name="extensions">
		/// Optional metadata such as required permissions, user ID, resource ID.
		/// Be careful not to leak sensitive information in production.
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.PermissionDenied.
		/// </returns>
		/// <example>
		/// <code>
		/// if (!User.IsAuthenticated)
		/// {
		///     return IResponse.Get.PermissionDenied("Authentication required");
		/// }
		/// 
		/// if (!await authService.HasPermissionAsync(userId, Permission.EditDocument))
		/// {
		///     return IResponse.Get.PermissionDenied(
		///         "You don't have permission to edit this document",
		///         extensions: new[] 
		///         { 
		///             ("required-permission", (object?)"EditDocument"),
		///             ("resource-id", documentId)
		///         }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse PermissionDenied(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.PermissionDenied, exception, extensions);

		/// <summary>
		/// Creates a PermissionDenied error response with a payload (rarely needed).
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload, typically containing permission details.</typeparam>
		/// <param name="message">Error message explaining why access was denied.</param>
		/// <param name="payload">Payload with permission details or alternative access information.</param>
		/// <param name="exception">Optional exception from authentication/authorization logic.</param>
		/// <param name="extensions">Optional metadata.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.PermissionDenied.</returns>
		public IResponse<TPayload> PermissionDenied<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.PermissionDenied, exception, extensions);

		/// <summary>
		/// Creates an InvalidData error response indicating validation failure or malformed input.
		/// </summary>
		/// <param name="message">
		/// Error message describing what data is invalid.
		/// Be specific about what's wrong: "Email format is invalid" vs "Invalid input".
		/// </param>
		/// <param name="exception">
		/// Optional exception from validation logic (e.g., JsonException for malformed JSON).
		/// </param>
		/// <param name="extensions">
		/// Optional validation details. Commonly includes field-level validation errors.
		/// Consider using ErrorPayload&lt;ValidationErrors&gt; for structured validation errors.
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.InvalidData.
		/// </returns>
		/// <example>
		/// <code>
		/// if (string.IsNullOrWhiteSpace(email))
		/// {
		///     return IResponse.Get.InvalidData("Email is required");
		/// }
		/// 
		/// if (!EmailRegex.IsMatch(email))
		/// {
		///     return IResponse.Get.InvalidData(
		///         "Invalid email format",
		///         extensions: new[] { ("field", (object?)"email"), ("value", email) }
		///     );
		/// }
		/// 
		/// if (age &lt; 18)
		/// {
		///     return IResponse.Get.InvalidData(
		///         "Must be 18 or older",
		///         extensions: new[] { ("field", (object?)"age"), ("minimum", 18), ("actual", age) }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse InvalidData(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.InvalidData, exception, extensions);

		/// <summary>
		/// Creates an InvalidData error response with a payload containing detailed validation errors.
		/// </summary>
		/// <typeparam name="TPayload">
		/// Type of the payload, typically a ValidationErrors collection or dictionary of field errors.
		/// </typeparam>
		/// <param name="message">High-level error message like "Validation failed".</param>
		/// <param name="payload">
		/// Structured validation errors, commonly a collection of field names and error messages.
		/// This is the preferred way to return multiple validation errors.
		/// </param>
		/// <param name="exception">Optional exception from validation logic.</param>
		/// <param name="extensions">Optional additional metadata.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.InvalidData.</returns>
		/// <example>
		/// <code>
		/// var errors = new Dictionary&lt;string, string[]&gt;
		/// {
		///     ["Email"] = new[] { "Email is required", "Email format is invalid" },
		///     ["Password"] = new[] { "Password must be at least 8 characters" },
		///     ["Age"] = new[] { "Must be 18 or older" }
		/// };
		/// 
		/// return IResponse.Get.InvalidData(
		///     "Validation failed",
		///     payload: errors
		/// );
		/// </code>
		/// </example>
		public IResponse<TPayload> InvalidData<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.InvalidData, exception, extensions);

		/// <summary>
		/// Creates a Conflict error response indicating a state conflict or duplicate resource.
		/// </summary>
		/// <param name="message">
		/// Error message describing the conflict.
		/// Examples: "Email address already in use", "Cannot delete resource with active dependencies",
		/// "Version mismatch - resource was modified by another user".
		/// </param>
		/// <param name="exception">
		/// Optional exception, commonly from database unique constraint violations or concurrency checks.
		/// </param>
		/// <param name="extensions">
		/// Optional conflict details such as existing resource ID, current version, conflicting value.
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.Conflict.
		/// </returns>
		/// <example>
		/// <code>
		/// // Duplicate check
		/// if (await db.Users.AnyAsync(u => u.Email == email))
		/// {
		///     return IResponse.Get.Conflict(
		///         "Email address already in use",
		///         extensions: new[] { ("email", (object?)email) }
		///     );
		/// }
		/// 
		/// // Optimistic concurrency
		/// if (entity.Version != expectedVersion)
		/// {
		///     return IResponse.Get.Conflict(
		///         "Resource was modified by another user",
		///         extensions: new[] 
		///         { 
		///             ("expected-version", (object?)expectedVersion),
		///             ("actual-version", entity.Version)
		///         }
		///     );
		/// }
		/// 
		/// // State transition conflict
		/// if (order.Status == OrderStatus.Completed)
		/// {
		///     return IResponse.Get.Conflict("Cannot modify a completed order");
		/// }
		/// </code>
		/// </example>
		public IResponse Conflict(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.Conflict, exception, extensions);

		/// <summary>
		/// Creates a Conflict error response with a payload containing conflict details.
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload, typically containing existing resource or conflict information.</typeparam>
		/// <param name="message">Error message describing the conflict.</param>
		/// <param name="payload">Conflict details such as the existing resource that caused the conflict.</param>
		/// <param name="exception">Optional exception from conflict detection.</param>
		/// <param name="extensions">Optional additional metadata.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.Conflict.</returns>
		public IResponse<TPayload> Conflict<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.Conflict, exception, extensions);

		/// <summary>
		/// Creates a Critical error response for unexpected server errors.
		/// </summary>
		/// <param name="message">
		/// User-safe error message. Avoid exposing internal details, stack traces, or sensitive information.
		/// Good: "An unexpected error occurred. Please try again later."
		/// Avoid: "NullReferenceException in UserService.cs line 42"
		/// </param>
		/// <param name="exception">
		/// The exception that caused the critical error. Should always be logged separately for diagnostics.
		/// </param>
		/// <param name="extensions">
		/// Optional tracking data like correlation ID, operation ID, timestamp.
		/// Useful for correlating with server logs when debugging.
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.Critical.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Important:</strong> Always log Critical errors server-side with full exception details.
		/// The message returned to clients should be sanitized to avoid security issues.
		/// </para>
		/// <para>
		/// Use Critical for truly unexpected errors. If you can anticipate and handle an error more specifically,
		/// use a different ErrorType (InvalidData, NotFound, etc.).
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// try
		/// {
		///     await database.SaveChangesAsync();
		///     return IResponse.Get.Success();
		/// }
		/// catch (Exception ex)
		/// {
		///     // Log with full details
		///     logger.LogError(ex, "Failed to save changes for user {UserId}", userId);
		///     
		///     // Return sanitized error
		///     return IResponse.Get.Critical(
		///         "An unexpected error occurred. Please try again later.",
		///         exception: ex,
		///         extensions: new[] 
		///         { 
		///             ("correlation-id", (object?)correlationId),
		///             ("timestamp", DateTime.UtcNow)
		///         }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse Critical(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.Critical, exception, extensions);

		/// <summary>
		/// Creates a Critical error response with a payload (rarely needed).
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload.</typeparam>
		/// <param name="message">User-safe error message.</param>
		/// <param name="payload">Error-specific data (use cautiously to avoid leaking sensitive information).</param>
		/// <param name="exception">The exception that caused the critical error.</param>
		/// <param name="extensions">Optional tracking metadata.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.Critical.</returns>
		public IResponse<TPayload> Critical<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.Critical, exception, extensions);

		/// <summary>
		/// Creates a NotSupported error response for unsupported operations, media types, or features.
		/// </summary>
		/// <param name="message">
		/// Error message explaining what is not supported.
		/// Be specific: "JSON format is not supported for this endpoint" vs "Not supported".
		/// </param>
		/// <param name="exception">Optional exception from attempting the unsupported operation.</param>
		/// <param name="extensions">
		/// Optional details about what was requested vs what is supported.
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.NotSupported.
		/// </returns>
		/// <example>
		/// <code>
		/// // Unsupported content type
		/// if (contentType != "application/json")
		/// {
		///     return IResponse.Get.NotSupported(
		///         "Only JSON content is supported",
		///         extensions: new[] 
		///         { 
		///             ("requested", (object?)contentType),
		///             ("supported", "application/json")
		///         }
		///     );
		/// }
		/// 
		/// // Feature not implemented
		/// if (request.UseExperimentalFeature)
		/// {
		///     return IResponse.Get.NotSupported(
		///         "Experimental features are not enabled on this server"
		///     );
		/// }
		/// 
		/// // Deprecated API version
		/// if (apiVersion &lt; 2.0)
		/// {
		///     return IResponse.Get.NotSupported(
		///         "API version 1.x is no longer supported. Please upgrade to v2.0 or later.",
		///         extensions: new[] 
		///         { 
		///             ("requested-version", (object?)apiVersion),
		///             ("minimum-version", 2.0)
		///         }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse NotSupported(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.NotSupported, exception, extensions);

		/// <summary>
		/// Creates a NotSupported error response with a payload (rarely needed).
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload.</typeparam>
		/// <param name="message">Error message explaining what is not supported.</param>
		/// <param name="payload">Details about supported alternatives or upgrade paths.</param>
		/// <param name="exception">Optional exception from attempting the unsupported operation.</param>
		/// <param name="extensions">Optional additional metadata.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.NotSupported.</returns>
		public IResponse<TPayload> NotSupported<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.NotSupported, exception, extensions);

		/// <summary>
		/// Creates an Unavailable error response for temporary service unavailability.
		/// </summary>
		/// <param name="message">
		/// Error message explaining why the service is unavailable.
		/// Good: "Payment service is temporarily unavailable", "Server is under maintenance".
		/// </param>
		/// <param name="exception">
		/// Optional exception, commonly from failed health checks or circuit breaker trips.
		/// </param>
		/// <param name="extensions">
		/// Optional retry information. Consider including:
		/// - "retry-after-seconds": When the client should retry
		/// - "maintenance-window": When maintenance will be complete
		/// - "affected-services": List of unavailable dependencies
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.Unavailable.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Use this for temporary conditions. The service should recover automatically.
		/// Common scenarios: maintenance windows, rate limiting, circuit breaker open, dependency outage.
		/// </para>
		/// <para>
		/// Consider adding retry-after information in extensions to help clients implement proper backoff.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // External service down
		/// if (!await paymentService.IsHealthyAsync())
		/// {
		///     return IResponse.Get.Unavailable(
		///         "Payment service is temporarily unavailable. Please try again later.",
		///         extensions: new[] 
		///         { 
		///             ("retry-after-seconds", (object?)60),
		///             ("service", "payment-gateway")
		///         }
		///     );
		/// }
		/// 
		/// // Rate limiting
		/// if (requestCount > rateLimit)
		/// {
		///     return IResponse.Get.Unavailable(
		///         "Rate limit exceeded. Please slow down.",
		///         extensions: new[] 
		///         { 
		///             ("retry-after-seconds", (object?)rateLimitResetSeconds),
		///             ("limit", rateLimit),
		///             ("reset-at", rateLimitResetTime)
		///         }
		///     );
		/// }
		/// 
		/// // Circuit breaker open
		/// if (circuitBreaker.State == CircuitState.Open)
		/// {
		///     return IResponse.Get.Unavailable(
		///         "Service temporarily unavailable due to repeated failures",
		///         extensions: new[] { ("retry-after-seconds", (object?)30) }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse Unavailable(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.Unavailable, exception, extensions);

		/// <summary>
		/// Creates an Unavailable error response with a payload (rarely needed).
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload, typically containing service status or alternatives.</typeparam>
		/// <param name="message">Error message explaining why the service is unavailable.</param>
		/// <param name="payload">Service status details, alternative endpoints, or partial results.</param>
		/// <param name="exception">Optional exception from availability check.</param>
		/// <param name="extensions">Optional retry information.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.Unavailable.</returns>
		public IResponse<TPayload> Unavailable<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.Unavailable, exception, extensions);

		/// <summary>
		/// Creates a Timeout error response for operations that exceeded time limits.
		/// </summary>
		/// <param name="message">
		/// Error message describing what timed out.
		/// Good: "Database query timed out", "Request took too long to complete".
		/// </param>
		/// <param name="exception">
		/// Optional TimeoutException or similar. Useful for logging actual timeout duration.
		/// </param>
		/// <param name="extensions">
		/// Optional timeout details such as:
		/// - "timeout-seconds": The timeout limit that was exceeded
		/// - "elapsed-seconds": How long the operation actually took
		/// - "operation": What operation timed out
		/// </param>
		/// <returns>
		/// An error IResponse with ErrorType.Timeout.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Use this when an operation exceeds its time budget. Common causes:
		/// - Database queries taking too long
		/// - External API calls not responding in time
		/// - Long-running computations exceeding limits
		/// - Client not sending complete request in time
		/// </para>
		/// <para>
		/// Clients may retry timeout errors, so ensure operations are idempotent when possible.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// try
		/// {
		///     var result = await operation
		///         .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
		///     return IResponse.Get.SuccessPayload(result);
		/// }
		/// catch (TimeoutException ex)
		/// {
		///     return IResponse.Get.Timeout(
		///         "Operation timed out after 30 seconds",
		///         exception: ex,
		///         extensions: new[] 
		///         { 
		///             ("timeout-seconds", (object?)30),
		///             ("operation", "data-export")
		///         }
		///     );
		/// }
		/// 
		/// // Database timeout
		/// catch (SqlException ex) when (ex.Number == -2)
		/// {
		///     return IResponse.Get.Timeout(
		///         "Database query took too long to execute",
		///         exception: ex,
		///         extensions: new[] 
		///         { 
		///             ("query-type", (object?)"complex-report"),
		///             ("timeout-seconds", (object?)commandTimeout)
		///         }
		///     );
		/// }
		/// 
		/// // Gateway timeout
		/// catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.GatewayTimeout)
		/// {
		///     return IResponse.Get.Timeout(
		///         "Upstream service did not respond in time",
		///         exception: ex,
		///         extensions: new[] 
		///         { 
		///             ("upstream-service", (object?)"payment-api"),
		///             ("timeout-seconds", (object?)httpTimeout)
		///         }
		///     );
		/// }
		/// </code>
		/// </example>
		public IResponse Timeout(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorMessage(message, ErrorType.Timeout, exception, extensions);

		/// <summary>
		/// Creates a Timeout error response with a payload (rarely needed).
		/// </summary>
		/// <typeparam name="TPayload">Type of the payload, typically containing partial results or timeout details.</typeparam>
		/// <param name="message">Error message describing what timed out.</param>
		/// <param name="payload">Partial results or timeout analysis data.</param>
		/// <param name="exception">Optional TimeoutException.</param>
		/// <param name="extensions">Optional timeout metrics.</param>
		/// <returns>An error IResponse&lt;TPayload&gt; with ErrorType.Timeout.</returns>
		public IResponse<TPayload> Timeout<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Get.ErrorPayload(payload, message, ErrorType.Timeout, exception, extensions);
	}
}