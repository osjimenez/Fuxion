using System;

namespace Fuxion;

/// <summary>
/// Provides extension methods for <see cref="Exception"/> to enhance exception handling and analysis.
/// </summary>
public static class ExceptionExtensions
{
	/// <summary>
	/// Extension methods for exception traversal and analysis.
	/// </summary>
	extension(Exception me)
	{
		/// <summary>
		/// Traverses the exception chain to find the deepest (innermost) exception.
		/// </summary>
		/// <returns>
		/// The innermost exception in the exception chain. If there are no inner exceptions, returns the exception itself.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method follows the <see cref="Exception.InnerException"/> chain until it reaches an exception
		/// that has no inner exception, which is typically the root cause of the problem.
		/// </para>
		/// <para>
		/// This is particularly useful when dealing with wrapped exceptions where the actual root cause
		/// is buried several layers deep in the exception hierarchy.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// try
		/// {
		///     try
		///     {
		///         try
		///         {
		///             throw new InvalidOperationException("Root cause: Database connection failed");
		///         }
		///         catch (Exception ex)
		///         {
		///             throw new ApplicationException("Data layer error", ex);
		///         }
		///     }
		///     catch (Exception ex)
		///     {
		///         throw new Exception("Business logic error", ex);
		///     }
		/// }
		/// catch (Exception ex)
		/// {
		///     var rootCause = ex.GetDeeperInnerException();
		///     Console.WriteLine(rootCause.Message); // "Root cause: Database connection failed"
		///     
		///     // Useful for logging the actual error
		///     logger.Error($"Root cause: {rootCause.GetType().Name} - {rootCause.Message}");
		/// }
		/// 
		/// // Real-world example: Async web API error handling
		/// [HttpGet("data")]
		/// public async Task&lt;IActionResult&gt; GetData()
		/// {
		///     try
		///     {
		///         return Ok(await dataService.GetDataAsync());
		///     }
		///     catch (Exception ex)
		///     {
		///         var rootException = ex.GetDeeperInnerException();
		///         
		///         // Log the root cause for diagnostics
		///         logger.LogError(rootException, "Failed to retrieve data");
		///         
		///         // Return user-friendly message with technical details
		///         return StatusCode(500, new 
		///         { 
		///             Message = "An error occurred while retrieving data",
		///             TechnicalDetails = rootException.Message
		///         });
		///     }
		/// }
		/// 
		/// // Example with AggregateException
		/// try
		/// {
		///     Task.WaitAll(
		///         Task.Run(() => throw new InvalidOperationException("Task 1 failed")),
		///         Task.Run(() => throw new ArgumentException("Task 2 failed"))
		///     );
		/// }
		/// catch (AggregateException aggEx)
		/// {
		///     foreach (var innerEx in aggEx.InnerExceptions)
		///     {
		///         var deepest = innerEx.GetDeeperInnerException();
		///         Console.WriteLine($"Root cause: {deepest.Message}");
		///     }
		/// }
		/// </code>
		/// </example>
		public Exception GetDeeperInnerException()
		{
			var inner = me;
			while (inner.InnerException is not null) inner = inner.InnerException;
			return inner;
		}
	}
}