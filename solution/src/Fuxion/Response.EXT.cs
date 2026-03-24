using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fuxion.Collections.Generic;

namespace Fuxion;

/// <summary>
/// Provides extension methods and factory methods for creating and manipulating Response objects.
/// </summary>
/// <remarks>
/// <para>
/// This class contains the complete API for working with Response objects, including:
/// </para>
/// <list type="bullet">
/// <item><description>Factory methods for creating success and error responses</description></item>
/// <item><description>Match pattern methods for functional-style response handling</description></item>
/// <item><description>Conversion methods between Response and Response&lt;TPayload&gt;</description></item>
/// <item><description>Response combination methods for aggregating multiple results</description></item>
/// </list>
/// </remarks>
public static class ResponseExtensions
{
	/// <summary>
	/// Extension methods for IResponse interface.
	/// </summary>
	extension(IResponse me)
	{
		/// <summary>
		/// Converts a non-payload Response to a Response with a typed payload.
		/// </summary>
		/// <typeparam name="T">The payload type for the new response.</typeparam>
		/// <returns>A Response&lt;T&gt; with the same status and metadata.</returns>
		/// <exception cref="InvalidOperationException">Thrown when attempting to convert a success response (payload mismatch).</exception>
		/// <remarks>
		/// This method is primarily used to convert error responses to a typed Response&lt;T&gt; format.
		/// Success responses cannot be converted as they would require an actual payload value.
		/// </remarks>
		/// <example>
		/// <code>
		/// Response errorResponse = Response.Get.NotFound("User not found");
		/// Response&lt;User&gt; typedError = errorResponse.AsPayload&lt;User&gt;();
		/// // typedError.IsError == true, typedError.Payload == null
		/// </code>
		/// </example>
		public IResponse<T> AsPayload<T>()
		{
			if (me is IResponse<T> r) return r;
			if (me.IsSuccess) throw new InvalidOperationException("Can't convert a success response to a different payload type.");
			return new Response<T>(me.IsSuccess, default!, me.Message, me.ErrorType, me.Exception)
			{
				Extensions = me.Extensions
			};
		}

		/// <summary>
		/// Executes one of two functions based on the response state (success or error).
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Function to execute if the response is successful.</param>
		/// <param name="error">Function to execute if the response is an error.</param>
		/// <returns>The result of the executed function.</returns>
		/// <remarks>
		/// This method implements the Match pattern from functional programming, providing a clean
		/// way to handle both success and error cases without explicit if/else statements.
		/// </remarks>
		/// <example>
		/// <code>
		/// Response response = PerformOperation();
		/// var message = response.Match(
		///     success: r => "Operation completed successfully",
		///     error: r => $"Operation failed: {r.Message}"
		/// );
		/// </code>
		/// </example>
		public T Match<T>(Func<IResponse, T> success, Func<IResponse, T> error) => me.IsSuccess ? success(me) : error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for success case.
		/// </summary>
		public async Task<T> MatchAsync<T>(Func<IResponse, Task<T>> success, Func<IResponse, T> error) => me.IsSuccess ? await success(me) : error(me);
		
		/// <summary>
		/// Executes one of two async functions based on the response state.
		/// </summary>
		public async Task<T> MatchAsync<T>(Func<IResponse, Task<T>> success, Func<IResponse, Task<T>> error) => me.IsSuccess ? await success(me) : await error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for error case.
		/// </summary>
		public async Task<T> MatchAsync<T>(Func<IResponse, T> success, Func<IResponse, Task<T>> error) => me.IsSuccess ? success(me) : await error(me);
	}

	/// <summary>
	/// Extension methods for IResponse&lt;TPayload&gt; interface.
	/// </summary>
	extension<TPayload>(IResponse<TPayload> me)
	{
		/// <summary>
		/// Executes one of two functions based on the response state (success or error).
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Function to execute if the response is successful. Receives the response with payload.</param>
		/// <param name="error">Function to execute if the response is an error.</param>
		/// <returns>The result of the executed function.</returns>
		/// <remarks>
		/// This is the typed version of Match for responses with payloads. The success function receives
		/// a response with a guaranteed non-null Payload when IsSuccess is true.
		/// </remarks>
		/// <example>
		/// <code>
		/// Response&lt;User&gt; userResponse = GetUser(userId);
		/// var displayName = userResponse.Match(
		///     success: r => $"Hello, {r.Payload.Name}",
		///     error: r => "User not found"
		/// );
		/// </code>
		/// </example>
		public T Match<T>(Func<IResponse<TPayload>, T> success, Func<IResponse<TPayload>, T> error) => me.IsSuccess ? success(me) : error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for success case.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Async function to execute if the response is successful.</param>
		/// <param name="error">Sync function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		public async Task<T> MatchAsync<T>(Func<IResponse<TPayload>, Task<T>> success, Func<IResponse<TPayload>, T> error) => me.IsSuccess ? await success(me) : error(me);
		
		/// <summary>
		/// Executes one of two async functions based on the response state.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Async function to execute if the response is successful.</param>
		/// <param name="error">Async function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		public async Task<T> MatchAsync<T>(Func<IResponse<TPayload>, Task<T>> success, Func<IResponse<TPayload>, Task<T>> error) => me.IsSuccess ? await success(me) : await error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for error case.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Sync function to execute if the response is successful.</param>
		/// <param name="error">Async function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		public async Task<T> MatchAsync<T>(Func<IResponse<TPayload>, T> success, Func<IResponse<TPayload>, Task<T>> error) => me.IsSuccess ? success(me) : await error(me);
	}
	
	/// <summary>
	/// Extension methods for Response class.
	/// </summary>
	extension(Response me)
	{
		/// <summary>
		/// Converts a non-payload Response to a Response with a typed payload.
		/// </summary>
		/// <typeparam name="T">The payload type for the new response.</typeparam>
		/// <returns>A Response&lt;T&gt; with the same status and metadata.</returns>
		/// <exception cref="InvalidOperationException">Thrown when attempting to convert a success response.</exception>
		public Response<T> AsPayload<T>()
		{
			if (me is Response<T> r) return r;
			if (me.IsSuccess)
				// PEND Ver si hacer algo distinto aquí en vez de lanza exception
				throw new InvalidOperationException("Can't convert a success response to a different payload type.");
			return new Response<T>(me.IsSuccess, default!, me.Message, me.ErrorType, me.Exception)
			{
				Extensions = me.Extensions
			};
		}

		/// <summary>
		/// Adds or updates an extension value in the response's Extensions dictionary.
		/// </summary>
		/// <param name="key">The key for the extension data.</param>
		/// <param name="value">The value to store.</param>
		/// <returns>The same Response instance for method chaining.</returns>
		/// <example>
		/// <code>
		/// var response = Response.Get.Success()
		///     .AddOrUpdateExtension("correlation-id", Guid.NewGuid())
		///     .AddOrUpdateExtension("timestamp", DateTime.UtcNow);
		/// </code>
		/// </example>
		public Response AddOrUpdateExtension(string key, object? value)
		{
			if(me.Extensions.TryGetValue(key,out var val)) 
				me.Extensions[key] = value;
			else
				me.Extensions.Add(key, value);
			return me;
		}
		
		/// <summary>
		/// Executes one of two functions based on the response state (success or error).
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Function to execute if the response is successful.</param>
		/// <param name="error">Function to execute if the response is an error.</param>
		/// <returns>The result of the executed function.</returns>
		/// <remarks>
		/// This is the concrete Response class version of Match. Use this when working directly
		/// with Response instances rather than the IResponse interface.
		/// </remarks>
		/// <example>
		/// <code>
		/// Response result = DeleteUser(userId);
		/// var message = result.Match(
		///     success: r => "User deleted successfully",
		///     error: r => $"Failed to delete user: {r.Message}"
		/// );
		/// </code>
		/// </example>
		public T Match<T>(Func<Response, T> success, Func<Response, T> error) => me.IsSuccess ? success(me) : error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for success case.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Async function to execute if the response is successful.</param>
		/// <param name="error">Sync function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		public async Task<T> MatchAsync<T>(Func<Response, Task<T>> success, Func<Response, T> error) => me.IsSuccess ? await success(me) : error(me);
		
		/// <summary>
		/// Executes one of two async functions based on the response state.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Async function to execute if the response is successful.</param>
		/// <param name="error">Async function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		public async Task<T> MatchAsync<T>(Func<Response, Task<T>> success, Func<Response, Task<T>> error) => me.IsSuccess ? await success(me) : await error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for error case.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Sync function to execute if the response is successful.</param>
		/// <param name="error">Async function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		public async Task<T> MatchAsync<T>(Func<Response, T> success, Func<Response, Task<T>> error) => me.IsSuccess ? success(me) : await error(me);
	}

	/// <summary>
	/// Extension methods for Response&lt;TPayload&gt; class.
	/// </summary>
	extension<TPayload>(Response<TPayload> me)
	{
		/// <summary>
		/// Executes one of two functions based on the response state (success or error).
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Function to execute if the response is successful. Has access to the typed Payload.</param>
		/// <param name="error">Function to execute if the response is an error.</param>
		/// <returns>The result of the executed function.</returns>
		/// <remarks>
		/// This is the concrete Response&lt;TPayload&gt; class version of Match. The Payload is guaranteed
		/// non-null in the success function due to nullable reference type annotations.
		/// </remarks>
		/// <example>
		/// <code>
		/// Response&lt;User&gt; result = await GetUserAsync(userId);
		/// var html = result.Match(
		///     success: r => $"&lt;h1&gt;{r.Payload.Name}&lt;/h1&gt;&lt;p&gt;{r.Payload.Email}&lt;/p&gt;",
		///     error: r => $"&lt;div class='error'&gt;{r.Message}&lt;/div&gt;"
		/// );
		/// </code>
		/// </example>
		public T Match<T>(Func<Response<TPayload>, T> success, Func<Response<TPayload>, T> error) => me.IsSuccess ? success(me) : error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for success case.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Async function to execute if the response is successful.</param>
		/// <param name="error">Sync function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		/// <example>
		/// <code>
		/// Response&lt;User&gt; userResponse = GetUser(userId);
		/// var notification = await userResponse.MatchAsync(
		///     success: async r => await SendWelcomeEmailAsync(r.Payload),
		///     error: r => Task.FromResult("Failed to send welcome email")
		/// );
		/// </code>
		/// </example>
		public async Task<T> MatchAsync<T>(Func<Response<TPayload>, Task<T>> success, Func<Response<TPayload>, T> error) => me.IsSuccess ? await success(me) : error(me);
		
		/// <summary>
		/// Executes one of two async functions based on the response state.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Async function to execute if the response is successful.</param>
		/// <param name="error">Async function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		/// <example>
		/// <code>
		/// Response&lt;Order&gt; orderResponse = await CreateOrderAsync(orderData);
		/// var result = await orderResponse.MatchAsync(
		///     success: async r => await ProcessPaymentAsync(r.Payload),
		///     error: async r => await LogErrorAsync(r.Message)
		/// );
		/// </code>
		/// </example>
		public async Task<T> MatchAsync<T>(Func<Response<TPayload>, Task<T>> success, Func<Response<TPayload>, Task<T>> error) => me.IsSuccess ? await success(me) : await error(me);
		
		/// <summary>
		/// Executes one of two functions based on the response state, with async support for error case.
		/// </summary>
		/// <typeparam name="T">The return type of both functions.</typeparam>
		/// <param name="success">Sync function to execute if the response is successful.</param>
		/// <param name="error">Async function to execute if the response is an error.</param>
		/// <returns>A task containing the result of the executed function.</returns>
		/// <example>
		/// <code>
		/// Response&lt;string&gt; fileResponse = ReadFile(path);
		/// var content = await fileResponse.MatchAsync(
		///     success: r => r.Payload,
		///     error: async r => await GetDefaultContentAsync()
		/// );
		/// </code>
		/// </example>
		public async Task<T> MatchAsync<T>(Func<Response<TPayload>, T> success, Func<Response<TPayload>, Task<T>> error) => me.IsSuccess ? success(me) : await error(me);
		
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
		public TPayload PayloadOrFallback(Func<Response<TPayload>, TPayload> fallback) => me.IsSuccess ? me.Payload : fallback(me);

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
		public TPayload? PayloadOrDefault() => me.IsSuccess ? me.Payload : default;
	}

	/// <summary>
	/// Extension methods for collections of IResponse.
	/// </summary>
	extension(IEnumerable<IResponse> me)
	{
		/// <summary>
		/// Combines multiple responses into a single payload response while preserving error semantics.
		/// </summary>
		/// <param name="successMessage">Optional message used when all responses are successful.</param>
		/// <returns>
		/// A <see cref="Response{TPayload}"/> whose payload is always the original input sequence.
		/// If all responses are successful, returns <c>SuccessPayload</c> with <paramref name="successMessage"/>.
		/// If exactly one response fails, returns that error (including type/exception/extensions) converted to payload form.
		/// If multiple responses fail, returns a generic error payload with concatenated error messages.
		/// </returns>
		/// <example>
		/// <code>
		/// var responses = new[]
		/// {
		///     ValidateEmail(email),
		///     ValidatePassword(password),
		///     ValidateAge(age)
		/// };
		/// 
		/// var combinedResult = responses.CombineResponses("Validation completed");
		/// if (combinedResult.IsError)
		/// {
		///     foreach (var item in combinedResult.Payload)
		///     {
		///         if (item.IsError) Console.WriteLine(item.Message);
		///     }
		/// }
		/// </code>
		/// </example>
		public Response<IEnumerable<IResponse>> CombineResponses(string? successMessage = null)
		{
			var errors = me.Where(r => r.IsError).ToList();
			return errors.Count switch
			{
				0 => Response.Get.SuccessPayload(me, successMessage),
				1 => errors[0] is Response res
					? res.AsPayload<IEnumerable<IResponse>>()
					: new(
						errors[0].IsSuccess,
						me,
						errors[0].Message,
						errors[0].ErrorType,
						errors[0].Exception)
					{
						Extensions = errors[0].Extensions
					},
				_ => Response.Get.ErrorPayload(me, string.Join("\r\n", errors.Select(r => r.Message)))
			};
		}
	}

	/// <summary>
	/// Error type checking extension methods for IResponse.
	/// </summary>
	extension(IResponse me)
	{
		/// <summary>
		/// Checks if the response has a specific error type.
		/// </summary>
		/// <param name="type">The error type to check (typically an ErrorType enum value).</param>
		/// <returns>true if the response's ErrorType equals the specified type; otherwise, false.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsErrorType(object type) => me.ErrorType?.Equals(type) == true;
	}
}