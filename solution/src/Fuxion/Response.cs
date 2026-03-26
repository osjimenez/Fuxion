using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Fuxion.Collections.Generic;
using Fuxion.Reflection;
using Fuxion.Text.Json.Serialization;

namespace Fuxion;

/// <summary>
///    Represents the result of an operation with success/failure information and optional metadata.
/// </summary>
/// <param name="isSuccess">Indicates whether the operation succeeded.</param>
/// <param name="message">Optional message describing the result.</param>
/// <param name="errorType">Optional error type categorization (e.g., ErrorType enum).</param>
/// <param name="exception">Optional exception that caused the failure.</param>
/// <remarks>
///    <para>
///       This class implements the Result pattern (also known as Railway Oriented Programming) to provide
///       a standardized way to handle operation outcomes without relying on exceptions for flow control.
///    </para>
///    <para>
///       Key benefits:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Explicit success/failure handling without exceptions</description>
///       </item>
///       <item>
///          <description>Rich metadata through Extensions dictionary</description>
///       </item>
///       <item>
///          <description>Type-safe error categorization</description>
///       </item>
///       <item>
///          <description>Implicit conversion to bool for easy checking</description>
///       </item>
///       <item>
///          <description>JSON serialization support with conditional properties</description>
///       </item>
///    </list>
///    <para>
///       The Extensions dictionary allows attaching arbitrary metadata to responses, useful for:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>HTTP status codes and headers</description>
///       </item>
///       <item>
///          <description>Validation errors</description>
///       </item>
///       <item>
///          <description>Correlation IDs</description>
///       </item>
///       <item>
///          <description>Performance metrics</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <code>
/// // Success response
/// var success = new Response(true, "Operation completed successfully");
/// if (success)
/// {
///     Console.WriteLine(success.Message);
/// }
/// 
/// // Error response with exception
/// try
/// {
///     // Some operation
///     throw new InvalidOperationException("Something went wrong");
/// }
/// catch (Exception ex)
/// {
///     var error = new Response(false, "Operation failed", ErrorType.Critical, ex);
///     Console.WriteLine($"Error: {error.Message}");
///     Console.WriteLine($"Type: {error.ErrorType}");
/// }
/// 
/// // Using extensions for metadata
/// var response = new Response(true, "User created");
/// response.Extensions["user-id"] = 12345;
/// response.Extensions["created-at"] = DateTime.UtcNow;
/// 
/// // Implicit bool conversion
/// Response result = PerformOperation();
/// if (result) // Automatically checks IsSuccess
/// {
///     Console.WriteLine("Success!");
/// }
/// </code>
/// </example>
public class Response(bool isSuccess, string? message = null, object? errorType = null, Exception? exception = null)
{
	/// <summary>
	///    Gets a value indicating whether the operation was successful.
	/// </summary>
	/// <value>true if the operation succeeded; otherwise, false.</value>
	public bool IsSuccess { get; protected init; } = isSuccess;

	/// <summary>
	///    Gets a value indicating whether the operation failed.
	/// </summary>
	/// <value>true if the operation failed; otherwise, false.</value>
	/// <remarks>
	///    This property is the inverse of <see cref="IsSuccess" /> and is provided for convenience.
	///    It is excluded from JSON serialization.
	/// </remarks>
	[JsonIgnore]
	public bool IsError => !IsSuccess;

	/// <summary>
	///    Gets the message describing the result of the operation.
	/// </summary>
	/// <value>A message describing success or failure, or null if no message was provided.</value>
	/// <remarks>
	///    This property is omitted from JSON when null.
	/// </remarks>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; init; } = message;

	/// <summary>
	///    Gets the error type categorization for failed operations.
	/// </summary>
	/// <value>
	///    An object representing the error type (typically an <see cref="ErrorType" /> enum value),
	///    or null for successful operations.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property is used to categorize errors for proper handling. Common error types include:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>InvalidData - Client-side validation errors</description>
	///       </item>
	///       <item>
	///          <description>NotFound - Resource not found</description>
	///       </item>
	///       <item>
	///          <description>PermissionDenied - Authorization failures</description>
	///       </item>
	///       <item>
	///          <description>Conflict - State conflicts</description>
	///       </item>
	///       <item>
	///          <description>Critical - Unexpected server errors</description>
	///       </item>
	///    </list>
	///    <para>This property is omitted from JSON when null.</para>
	/// </remarks>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public object? ErrorType { get; init; } = errorType;

	/// <summary>
	///    Gets the exception that caused the operation to fail, if any.
	/// </summary>
	/// <value>The exception that occurred during the operation, or null if no exception occurred.</value>
	/// <remarks>
	///    <para>
	///       This property captures the original exception for logging and diagnostics while keeping
	///       the failure information in the Response object.
	///    </para>
	///    <para>
	///       Uses a custom <see cref="ExceptionConverter" /> for JSON serialization to handle exception
	///       serialization safely (excluding non-serializable properties).
	///    </para>
	///    <para>This property is omitted from JSON when default/null.</para>
	/// </remarks>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonConverter(typeof(ExceptionConverter))]
	public Exception? Exception { get; init; } = exception;

	/// <summary>
	///    Gets the dictionary of extension data for attaching arbitrary metadata to the response.
	/// </summary>
	/// <value>A dictionary with case-sensitive string keys containing additional response metadata.</value>
	/// <remarks>
	///    <para>
	///       This dictionary is serialized as JSON extension data (properties at the root level).
	///       It's commonly used for:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>HTTP metadata (status codes, headers)</description>
	///       </item>
	///       <item>
	///          <description>Validation error details</description>
	///       </item>
	///       <item>
	///          <description>RFC 7807 Problem Details</description>
	///       </item>
	///       <item>
	///          <description>Correlation/trace IDs</description>
	///       </item>
	///       <item>
	///          <description>Performance metrics</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var response = new Response(false, "Validation failed");
	/// response.Extensions["status-code"] = 400;
	/// response.Extensions["validation-errors"] = new[] 
	/// { 
	///     "Email is required", 
	///     "Password must be at least 8 characters" 
	/// };
	/// </code>
	/// </example>
	[JsonExtensionData]
	public ResponseExtensionsDictionary Extensions { get; init; } = new(StringComparer.Ordinal);

	/// <summary>
	///    Implicitly converts a Response to a boolean value.
	/// </summary>
	/// <param name="response">The response to convert.</param>
	/// <returns>true if the response indicates success; otherwise, false.</returns>
	/// <remarks>
	///    This conversion allows using Response objects directly in conditional statements.
	/// </remarks>
	/// <example>
	///    <code>
	/// Response result = PerformOperation();
	/// if (result) // Implicitly checks result.IsSuccess
	/// {
	///     Console.WriteLine("Success!");
	/// }
	/// </code>
	/// </example>
	public static implicit operator bool(Response response)
		=> response.IsSuccess;

	/// <summary>
	///    Attempts to extract a payload from the current response.
	/// </summary>
	/// <param name="payload">
	///    When this method returns <see langword="true" />, contains the payload value associated with the response.
	///    When this method returns <see langword="false" />, contains <see langword="null" />.
	/// </param>
	/// <returns>
	///    <see langword="true" /> if the response contains a payload; otherwise, <see langword="false" />.
	/// </returns>
	/// <remarks>
	///    The base <see cref="Response" /> type does not carry a payload, so this implementation always returns
	///    <see langword="false" /> and sets <paramref name="payload" /> to <see langword="null" />.
	///    Derived types such as <see cref="Response{TPayload}" /> override this method to expose their payload value.
	/// </remarks>
	public virtual bool TryGetPayload([NotNullWhen(true)] out object? payload)
	{
		payload = null;
		return false;
	}

	/// <summary>
	///    Returns a human-readable string representation of the response.
	/// </summary>
	/// <returns>
	///    A string in the format <c>"Success"</c>, <c>"Success - Message"</c>,
	///    <c>"ErrorType"</c>, or <c>"ErrorType - Message"</c> depending on the response state.
	/// </returns>
	public override string ToString()
	{
		var status = IsSuccess ? "Success" : ErrorType?.ToString() ?? "Error";
		var messagePart = string.IsNullOrWhiteSpace(Message) ? null : Message;
		return string.Join(" - ", new[] { status, messagePart }.Where(p => p is not null));
	}
}

/// <summary>
///    Represents the result of an operation with a typed payload value.
/// </summary>
/// <typeparam name="TPayload">The type of the payload value.</typeparam>
/// <param name="isSuccess">Indicates whether the operation succeeded.</param>
/// <param name="payload">The payload value (required for success, typically null/default for failure).</param>
/// <param name="message">Optional message describing the result.</param>
/// <param name="type">Optional error type categorization.</param>
/// <param name="exception">Optional exception that caused the failure.</param>
/// <remarks>
///    <para>
///       Extends <see cref="Response" /> to include a strongly-typed payload value. The payload is:
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Required (non-null) when <see cref="Response.IsSuccess" /> is true</description>
///       </item>
///       <item>
///          <description>Typically null/default when <see cref="Response.IsSuccess" /> is false</description>
///       </item>
///    </list>
///    <para>
///       The <see cref="MemberNotNullWhenAttribute" /> attributes ensure nullable reference type safety:
///       When <c>IsSuccess</c> is true, the compiler knows <c>Payload</c> is not null.
///    </para>
///    <para>
///       <strong>Important restriction:</strong> Payload cannot be another Response type to prevent
///       nested Response objects which would be confusing and violate the pattern's design.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// // Success with payload
/// var successResult = new Response&lt;User&gt;(true, new User { Id = 1, Name = "John" });
/// if (successResult.IsSuccess)
/// {
///     var user = successResult.Payload; // Compiler knows this is not null
///     Console.WriteLine(user.Name);
/// }
/// 
/// // Error without payload
/// var errorResult = new Response&lt;User&gt;(false, null, "User not found", ErrorType.NotFound);
/// if (errorResult.IsError)
/// {
///     Console.WriteLine(errorResult.Message);
/// }
/// 
/// // Implicit conversion to payload
/// Response&lt;int&gt; calcResult = Calculate();
/// int value = calcResult; // Implicitly extracts Payload
/// 
/// // Implicit conversion from payload
/// Response&lt;string&gt; result = "Success!"; // Creates success response with payload
/// </code>
/// </example>
public class Response<TPayload>(bool isSuccess, TPayload payload, string? message = null, object? type = null, Exception? exception = null)
	: Response(isSuccess, message, type, exception)
{
	/// <summary>
	///    Gets a value indicating whether the operation was successful.
	/// </summary>
	/// <value>true if the operation succeeded (and Payload is not null); otherwise, false.</value>
	/// <remarks>
	///    This override adds nullable analysis with <see cref="MemberNotNullWhenAttribute" /> to inform
	///    the compiler that when this returns true, <see cref="Payload" /> is guaranteed not to be null.
	/// </remarks>
	[MemberNotNullWhen(true, nameof(Payload))]
	public new bool IsSuccess
	{
		get => base.IsSuccess;
		protected init => base.IsSuccess = value;
	}

	/// <summary>
	///    Gets a value indicating whether the operation failed.
	/// </summary>
	/// <value>true if the operation failed (and Payload might be null); otherwise, false.</value>
	/// <remarks>
	///    This override adds nullable analysis with <see cref="MemberNotNullWhenAttribute" /> to inform
	///    the compiler about the relationship between failure and potentially null payload.
	///    It is excluded from JSON serialization.
	/// </remarks>
	[MemberNotNullWhen(false, nameof(Payload))]
	[JsonIgnore]
	public new bool IsError => base.IsError;

	/// <summary>
	///    Gets the payload value containing the operation's result data.
	/// </summary>
	/// <value>
	///    The result data when successful; typically null or default when the operation failed.
	/// </value>
	/// <remarks>
	///    <para>
	///       When <see cref="IsSuccess" /> is true, the compiler (via nullable reference types and
	///       <see cref="MemberNotNullWhenAttribute" />) knows this value is not null, enabling safe access
	///       without null checks.
	///    </para>
	///    <para>
	///       The setter validates that the payload is not another Response type, preventing
	///       confusing nested Response structures.
	///    </para>
	///    <para>This property is omitted from JSON when default/null.</para>
	/// </remarks>
	/// <exception cref="ArgumentException">
	///    Thrown when attempting to set a payload that derives from Response&lt;T&gt;.
	/// </exception>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public TPayload? Payload
	{
		get;
		init
			=> field = value?.GetType()
				.IsSubclassOfGenericDefinition(typeof(Response<>)) ?? false // PEND Change typeof(Response<>) by is IResponse<> - Make test to try it
				? throw new ArgumentException(
					$"Payload is '{value.GetType().GetSignature()}' type, but can't be derived from '{typeof(Response<>).GetSignature()}' to avoid nested responses.",
					nameof(Payload))
				: value;
	} = payload;

	/// <summary>
	///    Implicitly converts a Response&lt;TPayload&gt; to its payload value.
	/// </summary>
	/// <param name="response">The response to extract the payload from.</param>
	/// <returns>The payload value from the response.</returns>
	/// <remarks>
	///    <para>
	///       This conversion allows using Response&lt;TPayload&gt; objects directly as their payload type.
	///    </para>
	///    <para>
	///       <strong>Warning:</strong> This will return null/default if the response is a failure.
	///       Always check <see cref="IsSuccess" /> before relying on implicit conversion in critical code paths.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// Response&lt;int&gt; Calculate() => new(true, 42);
	/// 
	/// int result = Calculate(); // Implicitly extracts the 42
	/// Console.WriteLine(result); // 42
	/// </code>
	/// </example>
	public static implicit operator TPayload?(Response<TPayload> response)
		=> response.Payload;

	/// <summary>
	///    Implicitly converts a payload value to a successful Response&lt;TPayload&gt;.
	/// </summary>
	/// <param name="payload">The payload value to wrap in a response.</param>
	/// <returns>A successful Response&lt;TPayload&gt; containing the payload.</returns>
	/// <remarks>
	///    This conversion allows returning payload values directly from methods that return Response&lt;TPayload&gt;,
	///    automatically wrapping them in a success response.
	/// </remarks>
	/// <example>
	///    <code>
	/// Response&lt;string&gt; GetGreeting()
	/// {
	///     return "Hello, World!"; // Implicitly creates successful response
	/// }
	/// 
	/// var response = GetGreeting();
	/// // response.IsSuccess == true
	/// // response.Payload == "Hello, World!"
	/// </code>
	/// </example>
	public static implicit operator Response<TPayload>(TPayload payload)
		=> new(true, payload);

	/// <summary>
	///    Attempts to extract the typed payload from the current response.
	/// </summary>
	/// <param name="payload">
	///    When this method returns <see langword="true" />, contains the current <see cref="Payload" /> value boxed as
	///    <see cref="object" />.
	///    When this method returns <see langword="false" />, contains <see langword="null" />.
	/// </param>
	/// <returns>
	///    <see langword="true" /> if <see cref="Payload" /> is not <see langword="null" />; otherwise,
	///    <see langword="false" />.
	/// </returns>
	/// <remarks>
	///    This override allows payload-aware code to access the underlying value without knowing
	///    <typeparamref name="TPayload" />
	///    at compile time. A <see langword="null" /> payload is treated as the absence of payload.
	/// </remarks>
	public override bool TryGetPayload([NotNullWhen(true)] out object? payload)
	{
		if (Payload is null)
		{
			payload = null;
			return false;
		}

		payload = Payload;
		return true;
	}

	/// <summary>
	/// Returns the payload if successful; otherwise computes a fallback value from the error response.
	/// </summary>
	/// <param name="fallback">Function that receives the error response and returns an alternative payload value.</param>
	/// <returns>The response payload when successful; otherwise the value returned by <paramref name="fallback"/>.</returns>
	/// <remarks>
	/// This method is useful when you want to recover from errors with context-aware logic.
	/// If you just need the type default value on error, use <see cref="PayloadOrDefault"/>.
	/// </remarks>
	/// <example>
	/// <code>
	/// Response&lt;User&gt; response = GetUser(id);
	/// var user = response.PayloadOrFallback(err => new User { Name = "Guest" });
	/// // Always returns a User, either from response or default Guest
	/// </code>
	/// </example>
	public TPayload PayloadOrFallback(Func<Response<TPayload>, TPayload> fallback) => IsSuccess ? Payload : fallback(this);

	/// <summary>
	/// Returns the payload if successful; otherwise returns the default value of <typeparamref name="TPayload"/>.
	/// </summary>
	/// <returns>
	/// The response payload when successful; otherwise <c>default</c>.
	/// For reference types, this is <c>null</c>.
	/// </returns>
	/// <remarks>
	/// Use this when a simple default-on-error behavior is enough and no error-specific fallback logic is required.
	/// </remarks>
	/// <example>
	/// <code>
	/// Response&lt;int&gt; quantityResponse = GetQuantity();
	/// int quantity = quantityResponse.PayloadOrDefault();
	/// 
	/// Response&lt;User&gt; userResponse = GetUser(id);
	/// User? user = userResponse.PayloadOrDefault();
	/// </code>
	/// </example>
	public TPayload? PayloadOrDefault() => IsSuccess ? Payload : default;

	/// <summary>
	///    Returns a human-readable string representation of the response, including the payload type when present.
	/// </summary>
	/// <returns>
	///    A string combining status, optional payload type signature, and optional message, separated by <c>" - "</c>.
	///    Examples: <c>"Success"</c>, <c>"Success - User"</c>, <c>"Success - User - Created"</c>,
	///    <c>"NotFound"</c>, <c>"NotFound - User - User not found"</c>.
	/// </returns>
	/// <remarks>
	///    The payload type signature is obtained via <c>typeof(TPayload).GetSignature()</c> for a compact, readable representation.
	///    It is only included when <see cref="Payload"/> is not <see langword="null"/>.
	/// </remarks>
	public override string ToString()
	{
		var status = IsSuccess ? "Success" : ErrorType?.ToString() ?? "Error";
		var payloadPart = Payload is not null ? typeof(TPayload).GetSignature() : null;
		var messagePart = string.IsNullOrWhiteSpace(Message) ? null : Message;
		return string.Join(" - ", new[] { status, payloadPart, messagePart }.Where(p => p is not null));
	}
}

/// <summary>
/// Represents a specialized dictionary for storing response extension metadata.
/// </summary>
/// <param name="comparer">The string comparer used to compare extension keys.</param>
/// <remarks>
/// This type is used by <see cref="Response.Extensions"/> to store arbitrary metadata associated with a response,
/// such as correlation identifiers, validation details, transport-specific information, or other custom values.
/// </remarks>
public class ResponseExtensionsDictionary(IEqualityComparer<string> comparer) : Dictionary<string, object?>(comparer)
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ResponseExtensionsDictionary"/> class from an enumerable sequence of key/value pairs.
	/// </summary>
	/// <param name="extensions">The extension entries to copy into the dictionary.</param>
	/// <param name="comparer">The string comparer used to compare extension keys.</param>
	public ResponseExtensionsDictionary(IEnumerable<(string Property, object? Value)>? extensions = null, IEqualityComparer<string>? comparer = null)
		: this(comparer ?? StringComparer.Ordinal)
	{
		if (extensions is not null)
			foreach (var item in extensions)
				Add(item.Property, item.Value);
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="ResponseExtensionsDictionary"/> class from an existing dictionary.
	/// </summary>
	/// <param name="dictionary">The dictionary whose entries will be copied into the new instance.</param>
	/// <param name="comparer">The string comparer used to compare extension keys.</param>
	public ResponseExtensionsDictionary(IDictionary<string, object?> dictionary, IEqualityComparer<string>? comparer = null) 
		: this(comparer ?? StringComparer.Ordinal)
	{
		foreach (var item in dictionary)
			Add(item.Key, item.Value);
	}
	/// <summary>
	/// Converts the dictionary contents to an enumerable sequence of tuples.
	/// </summary>
	/// <returns>An enumerable sequence containing each extension entry as a <c>(Property, Value)</c> tuple.</returns>
	public IEnumerable<(string Property, object? Value)> ToEnumerable()
		=> this.Select(kvp => (kvp.Key, kvp.Value));
}