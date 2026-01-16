using System.Threading.Tasks;

namespace Fuxion.Threading.Tasks;

/// <summary>
/// Represents an exception that is thrown when a task is canceled due to concurrency control mechanisms.
/// </summary>
/// <remarks>
/// This exception is a specialized version of <see cref="TaskCanceledException"/> used to indicate
/// that a task was canceled specifically because of concurrency management logic, rather than a general
/// cancellation request. This distinction allows for more precise exception handling and debugging
/// in scenarios where concurrent task execution needs to be controlled or limited.
/// <para>
/// Common scenarios where this exception might be thrown include:
/// <list type="bullet">
/// <item><description>Rate limiting or throttling mechanisms that prevent too many concurrent operations</description></item>
/// <item><description>Resource pool exhaustion where only a limited number of concurrent tasks are allowed</description></item>
/// <item><description>Mutex or semaphore-based concurrency control that cancels tasks when access is denied</description></item>
/// <item><description>Task deduplication where duplicate concurrent operations are canceled</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ConcurrentTaskManager
/// {
///     private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5); // Max 5 concurrent tasks
///     
///     public async Task ExecuteWithConcurrencyControlAsync(Func&lt;Task&gt; work, CancellationToken ct = default)
///     {
///         if (!await _semaphore.WaitAsync(0, ct)) // Don't wait, fail immediately
///         {
///             throw new TaskCanceledByConcurrencyException();
///         }
///         
///         try
///         {
///             await work();
///         }
///         finally
///         {
///             _semaphore.Release();
///         }
///     }
/// }
/// 
/// // Usage
/// try
/// {
///     await manager.ExecuteWithConcurrencyControlAsync(async () => 
///     {
///         await ProcessDataAsync();
///     });
/// }
/// catch (TaskCanceledByConcurrencyException)
/// {
///     Console.WriteLine("Task was canceled due to concurrency limits");
///     // Handle concurrency-specific cancellation
/// }
/// catch (TaskCanceledException)
/// {
///     Console.WriteLine("Task was canceled for other reasons");
///     // Handle general cancellation
/// }
/// </code>
/// </example>
public class TaskCanceledByConcurrencyException : TaskCanceledException { }