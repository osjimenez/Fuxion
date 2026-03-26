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
	/// Extension methods for Response class.
	/// </summary>
	extension(Response me)
	{
		/// <summary>
		/// Checks if the response has a specific error type.
		/// </summary>
		/// <param name="type">The error type to check (typically an ErrorType enum value).</param>
		/// <returns>true if the response's ErrorType equals the specified type; otherwise, false.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsErrorType(object type) => me.ErrorType?.Equals(type) == true;

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

			T returnPayload = default!;
			if (me.TryGetPayload(out var payload) && payload is T t)
				returnPayload = t;
			return new(me.IsSuccess, returnPayload, me.Message, me.ErrorType, me.Exception)
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
			if (me.Extensions.TryGetValue(key, out var val))
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
	}

	/// <summary>
	/// Extension methods for collections of IResponse.
	/// </summary>
	extension(IEnumerable<Response> me)
	{
		/// <summary>
		/// Combines multiple responses into a single response whose payload contains either all input responses or only the error responses.
		/// </summary>
		/// <param name="successMessage">Optional message used when all responses are successful.</param>
		/// <param name="includeSucessResponses">
		/// When <see langword="true"/>, the payload includes all input responses.
		/// When <see langword="false"/>, the payload includes only the responses in error state.
		/// </param>
		/// <returns>
		/// A <see cref="Response{TPayload}"/> whose payload contains either the original input sequence or only the failing responses,
		/// depending on <paramref name="includeSucessResponses"/>.
		/// If all responses are successful, returns <c>SuccessPayload</c> with <paramref name="successMessage"/>.
		/// If exactly one response fails, returns an error response preserving that response's message, type, exception, and extensions.
		/// If multiple responses fail, returns an error payload with concatenated error messages and an error type equal to the shared
		/// error type when all errors match, or <see cref="ErrorType.Combined"/> when they differ.
		/// </returns>
		/// <remarks>
		/// This method aggregates a set of <see cref="Response"/> instances while preserving the semantics of the error cases.
		/// The payload content is controlled by <paramref name="includeSucessResponses"/>:
		/// include everything for contextual inspection, or only the failing responses for focused error handling.
		/// </remarks>
		/// <example>
		/// <code>
		/// var responses = new[]
		/// {
		///     ValidateEmail(email),
		///     ValidatePassword(password),
		///     ValidateAge(age)
		/// };
		/// 
		/// var combinedResult = responses.CombineResponses("Validation completed", includeSucessResponses: false);
		/// if (combinedResult.IsError)
		/// {
		///     foreach (var item in combinedResult.Payload)
		///     {
		///         if (item.IsError) Console.WriteLine(item.Message);
		///     }
		/// }
		/// 
		/// // Include successful responses too
		/// var fullResult = responses.CombineResponses(includeSucessResponses: true);
		/// </code>
		/// </example>
		public Response<IEnumerable<Response>> CombineResponses(string? successMessage = null, bool includeSucessResponses = false)
		{
			var errors = me.Where(r => r.IsError).ToList();
			return errors.Count switch
			{
				0 => Response.Get.SuccessPayload(me, successMessage),
				1 => new(
						errors[0].IsSuccess,
						includeSucessResponses ? me : errors,
						errors[0].Message,
						errors[0].ErrorType,
						errors[0].Exception)
				{
					Extensions = errors[0].Extensions
				},
				_ => Response.Get.ErrorPayload(
					includeSucessResponses ? me : errors,
					string.Join("\r\n", errors.Select(r => r.Message)),
					errors.GroupBy(e => e.ErrorType).Count() == 1
						? errors[0].ErrorType
						: ErrorType.Combined)
			};
		}
	}
}