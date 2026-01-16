using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.Threading.Tasks;

/// <summary>
/// Provides extension methods for <see cref="Task"/> and <see cref="IEnumerable{T}"/> of <see cref="Task"/> 
/// to enhance task management capabilities with cancellation, continuation, and sleep functionality.
/// </summary>
/// <remarks>
/// <para>
/// This static class uses C# 14.0 extension syntax to add methods directly to <see cref="Task"/> and task collection types.
/// The extensions integrate tightly with <see cref="TaskManager"/> to provide advanced task control features not available
/// in standard TPL (Task Parallel Library).
/// </para>
/// <para>
/// <strong>Key features:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Cancellation control:</strong> Cancel, check cancellation state, and access cancellation tokens</description></item>
/// <item><description><strong>Fluent continuations:</strong> OnCancel, OnSuccess, OnFaulted methods for cleaner continuation syntax</description></item>
/// <item><description><strong>Cancellable sleep:</strong> Task.Delay alternative that respects task cancellation</description></item>
/// <item><description><strong>Batch operations:</strong> Cancel and wait for multiple tasks simultaneously</description></item>
/// </list>
/// <para>
/// <strong>TaskManager integration:</strong>
/// </para>
/// <para>
/// Most methods require tasks to be created through <see cref="TaskManager"/> rather than standard Task.Run or Task.Factory.
/// This is because TaskManager maintains additional metadata (cancellation tokens, entry tracking) that these extensions rely on.
/// </para>
/// <para>
/// <strong>Important:</strong> Unless otherwise noted, these extension methods only work with tasks created by <see cref="TaskManager"/>.
/// Using them with standard TPL tasks may throw exceptions or return default values.
/// </para>
/// </remarks>
public static class Extensions
{
	/// <summary>
	/// Extension methods for individual <see cref="Task"/> instances.
	/// </summary>
	extension(Task me)
	{
		/// <summary>
		/// Requests cancellation of the task if it was created by <see cref="TaskManager"/>.
		/// </summary>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an exception if the task is not managed by TaskManager.
		/// If <c>false</c>, silently ignores tasks not managed by TaskManager.
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="throwExceptionIfNotRunning"/> is <c>true</c> and the task 
		/// was not created through TaskManager.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method requests cancellation by accessing the task's <see cref="ITaskManagerEntry"/> and calling its
		/// Cancel method. It does not force immediate termination; the task must cooperatively check for cancellation
		/// via <see cref="CancellationToken.IsCancellationRequested"/> or similar mechanisms.
		/// </para>
		/// <para>
		/// <strong>Cancellation behavior:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>If the task is already completed, cancelled, or faulted, this method has no effect</description></item>
		/// <item><description>If the task is currently running, the cancellation token is signaled but execution continues until checked</description></item>
		/// <item><description>If the task is queued (sequential profile), it may be cancelled before starting</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     for (int i = 0; i &lt; 100; i++)
		///     {
		///         if (task.IsCancellationRequested()) break;
		///         DoWork();
		///     }
		/// });
		/// 
		/// // Request cancellation
		/// task.Cancel();
		/// 
		/// // Or ignore if not managed
		/// task.Cancel(throwExceptionIfNotRunning: false);
		/// </code>
		/// </example>
		public void Cancel(bool throwExceptionIfNotRunning = true)
			=> TaskManager.SearchEntry(me, throwExceptionIfNotRunning)?.Cancel();
		
		/// <summary>
		/// Cancels the task and waits for it to complete or reach the specified timeout.
		/// </summary>
		/// <param name="timeout">
		/// The maximum time to wait for the task to complete. Use <c>default(TimeSpan)</c> or <see cref="TimeSpan.Zero"/> 
		/// to wait indefinitely.
		/// </param>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an exception if the task is not managed by TaskManager.
		/// If <c>false</c>, only waits without cancellation for non-managed tasks.
		/// </param>
		/// <remarks>
		/// <para>
		/// This method combines cancellation request with synchronous waiting. It's useful when you need to
		/// ensure a task has stopped before proceeding. The method:
		/// </para>
		/// <list type="number">
		/// <item><description>Requests cancellation via <see cref="Cancel"/></description></item>
		/// <item><description>Waits for the task to complete using <see cref="Task.WaitAll(Task[], TimeSpan)"/></description></item>
		/// <item><description>Swallows <see cref="TaskCanceledException"/> (expected outcome)</description></item>
		/// </list>
		/// <para>
		/// <strong>Timeout behavior:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>If <paramref name="timeout"/> is <c>default</c> or <see cref="TimeSpan.Zero"/>, waits indefinitely</description></item>
		/// <item><description>If timeout expires, the method returns but the task may still be running</description></item>
		/// <item><description>Timeout does not force task termination</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(() => LongRunningOperation());
		/// 
		/// // Cancel and wait indefinitely
		/// task.CancelAndWait();
		/// 
		/// // Cancel and wait up to 5 seconds
		/// task.CancelAndWait(TimeSpan.FromSeconds(5));
		/// 
		/// // Check if still running after timeout
		/// if (!task.IsCompleted)
		/// {
		///     Console.WriteLine("Task didn't complete within timeout");
		/// }
		/// </code>
		/// </example>
		public void CancelAndWait(TimeSpan timeout = default, bool throwExceptionIfNotRunning = true)
			=> new[] { me }.CancelAndWait(timeout, throwExceptionIfNotRunning);

		/// <summary>
		/// Asynchronously cancels the task and waits for it to complete.
		/// </summary>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an exception if the task is not managed by TaskManager.
		/// If <c>false</c>, only waits without cancellation for non-managed tasks.
		/// </param>
		/// <returns>A task representing the asynchronous wait operation.</returns>
		/// <remarks>
		/// <para>
		/// This is the async version of <see cref="CancelAndWait(Task,TimeSpan,bool)"/>. It requests cancellation and then
		/// asynchronously waits for the task to complete without blocking the calling thread.
		/// </para>
		/// <para>
		/// <strong>Advantages over synchronous version:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Doesn't block the calling thread</description></item>
		/// <item><description>Better for UI applications and async contexts</description></item>
		/// <item><description>Allows other async operations to proceed</description></item>
		/// </list>
		/// <para>
		/// <strong>Exception handling:</strong> Like <see cref="CancelAndWait(Task,TimeSpan,bool)"/>, this method swallows
		/// <see cref="TaskCanceledException"/> as it's the expected result of cancellation.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(async () => await LongOperationAsync());
		/// 
		/// // Async cancel and wait
		/// await task.CancelAndWaitAsync();
		/// 
		/// // Use in UI event handler
		/// private async void CancelButton_Click(object sender, EventArgs e)
		/// {
		///     await _currentTask.CancelAndWaitAsync();
		///     UpdateUI("Operation cancelled");
		/// }
		/// </code>
		/// </example>
		public async Task CancelAndWaitAsync(bool throwExceptionIfNotRunning = true)
			=> await new[] { me }.CancelAndWaitAsync(throwExceptionIfNotRunning);
		
		/// <summary>
		/// Registers an action to be invoked when cancellation is requested for this task.
		/// </summary>
		/// <param name="action">The action to execute when cancellation is requested.</param>
		/// <remarks>
		/// <para>
		/// This method subscribes to the task entry's <see cref="ITaskManagerEntry.CancelRequested"/> event,
		/// allowing you to respond to cancellation requests before the task actually stops.
		/// </para>
		/// <para>
		/// <strong>Important differences:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description><see cref="OnCancelRequested"/>: Fires when <see cref="Cancel"/> is called (cancellation requested)</description></item>
		/// <item><description><see cref="OnCancel"/>: Fires when the task actually completes in cancelled state</description></item>
		/// </list>
		/// <para>
		/// <strong>Use cases:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Logging cancellation requests</description></item>
		/// <item><description>Updating UI to indicate cancellation in progress</description></item>
		/// <item><description>Cleaning up resources proactively</description></item>
		/// <item><description>Notifying dependent operations</description></item>
		/// </list>
		/// <para>
		/// <strong>Note:</strong> The action runs synchronously on the thread that called <see cref="Cancel"/>,
		/// so keep it lightweight to avoid blocking the cancellation request.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     while (!task.IsCancellationRequested())
		///     {
		///         ProcessItem();
		///     }
		/// });
		/// 
		/// task.OnCancelRequested(() => 
		/// {
		///     Console.WriteLine("Cancellation has been requested");
		///     UpdateProgressBar("Cancelling...");
		/// });
		/// 
		/// // Later...
		/// task.Cancel(); // Triggers the OnCancelRequested action
		/// </code>
		/// </example>
		public void OnCancelRequested(Action action)
			=> TaskManager.SearchEntry(me).CancelRequested += (s, e) => action();

		/// <summary>
		/// Creates a continuation task that executes only when the task completes in the <see cref="TaskStatus.Canceled"/> state.
		/// </summary>
		/// <param name="action">The action to execute if the task is cancelled.</param>
		/// <returns>A <see cref="Task"/> representing the continuation.</returns>
		/// <remarks>
		/// <para>
		/// This is a fluent wrapper around <see cref="Task"/>.ContinueWith with <see cref="TaskContinuationOptions.OnlyOnCanceled"/>.
		/// It provides cleaner syntax for cancellation-specific continuations.
		/// </para>
		/// <para>
		/// <strong>When it executes:</strong> The action runs only if the task enters the <see cref="TaskStatus.Canceled"/> state,
		/// which happens when the task acknowledges the cancellation request by throwing <see cref="OperationCanceledException"/>
		/// or checking cancellation tokens.
		/// </para>
		/// <para>
		/// <strong>Difference from <see cref="OnCancelRequested"/>:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description><see cref="OnCancelRequested"/>: Fires immediately when Cancel() is called</description></item>
		/// <item><description><see cref="OnCancel"/>: Fires only when the task actually completes in cancelled state</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(async () => 
		/// {
		///     for (int i = 0; i &lt; 100; i++)
		///     {
		///         token.ThrowIfCancellationRequested();
		///         await ProcessAsync(i);
		///     }
		/// });
		/// 
		/// task.OnCancel(() => 
		/// {
		///     Console.WriteLine("Task was successfully cancelled");
		///     CleanupResources();
		/// });
		/// 
		/// task.Cancel();
		/// await task; // OnCancel action runs after cancellation completes
		/// </code>
		/// </example>
		public Task OnCancel(Action action)
			=> me.ContinueWith(t => action(), TaskContinuationOptions.OnlyOnCanceled);

		/// <summary>
		/// Creates a continuation task that executes only when the task completes successfully with <see cref="TaskStatus.RanToCompletion"/> state.
		/// </summary>
		/// <param name="action">The action to execute if the task completes successfully.</param>
		/// <returns>A <see cref="Task"/> representing the continuation.</returns>
		/// <remarks>
		/// <para>
		/// This is a fluent wrapper around <see cref="Task"/>.ContinueWith with <see cref="TaskContinuationOptions.OnlyOnRanToCompletion"/>.
		/// The action executes only if the task completes without cancellation or exceptions.
		/// </para>
		/// <para>
		/// <strong>Success criteria:</strong> The task must reach <see cref="TaskStatus.RanToCompletion"/> state, meaning:
		/// </para>
		/// <list type="bullet">
		/// <item><description>No unhandled exceptions occurred</description></item>
		/// <item><description>The task was not cancelled</description></item>
		/// <item><description>The task's delegate completed execution</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     var result = PerformCalculation();
		///     SaveResult(result);
		/// });
		/// 
		/// task.OnSuccess(() => 
		/// {
		///     Console.WriteLine("Calculation completed successfully");
		///     NotifyUser("Operation complete");
		/// });
		/// 
		/// // Chain multiple continuations
		/// task.OnSuccess(() => UpdateUI())
		///     .OnFaulted(ex => LogError(ex))
		///     .OnCancel(() => RestoreState());
		/// </code>
		/// </example>
		public Task OnSuccess(Action action)
			=> me.ContinueWith(t => action(), TaskContinuationOptions.OnlyOnRanToCompletion);

		/// <summary>
		/// Creates a continuation task that executes only when the task completes in the <see cref="TaskStatus.Faulted"/> state.
		/// </summary>
		/// <param name="action">
		/// The action to execute if the task faults. Receives the <see cref="AggregateException"/> containing the task's exceptions,
		/// or <c>null</c> if somehow no exception is available.
		/// </param>
		/// <returns>A <see cref="Task"/> representing the continuation.</returns>
		/// <remarks>
		/// <para>
		/// This is a fluent wrapper around <see cref="Task"/>.ContinueWith with <see cref="TaskContinuationOptions.OnlyOnFaulted"/>.
		/// It provides access to the task's exception for error handling, logging, or recovery.
		/// </para>
		/// <para>
		/// <strong>Exception handling:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>The <paramref name="action"/> receives <see cref="Task.Exception"/> which is an <see cref="AggregateException"/></description></item>
		/// <item><description>Use <see cref="AggregateException.InnerExceptions"/> or <see cref="AggregateException.Flatten"/> to access individual exceptions</description></item>
		/// <item><description>The continuation itself marks the exception as "observed", preventing unobserved task exceptions</description></item>
		/// </list>
		/// <para>
		/// <strong>Important:</strong> The continuation doesn't suppress the exception from the original task;
		/// it only provides a hook for handling it. If the original task's exception is not observed elsewhere,
		/// it may still trigger the unobserved task exception handler.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     throw new InvalidOperationException("Something went wrong");
		/// });
		/// 
		/// task.OnFaulted(ex => 
		/// {
		///     Console.WriteLine($"Task failed: {ex?.Message}");
		///     
		///     if (ex != null)
		///     {
		///         foreach (var innerEx in ex.InnerExceptions)
		///         {
		///             Logger.LogError(innerEx);
		///         }
		///     }
		///     
		///     ShowErrorToUser();
		/// });
		/// 
		/// // Comprehensive error handling
		/// task.OnSuccess(() => Console.WriteLine("Success"))
		///     .OnFaulted(ex => HandleError(ex))
		///     .OnCancel(() => Console.WriteLine("Cancelled"));
		/// </code>
		/// </example>
		public Task OnFaulted(Action<AggregateException?> action)
			=> me.ContinueWith(t => action(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

		/// <summary>
		/// Determines whether cancellation has been requested for this task.
		/// </summary>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an <see cref="ArgumentException"/> if the task is not managed by TaskManager.
		/// If <c>false</c>, returns <c>false</c> for non-managed tasks.
		/// </param>
		/// <returns>
		/// <c>true</c> if cancellation has been requested; <c>false</c> if not requested or if the task is not managed
		/// and <paramref name="throwExceptionIfNotRunning"/> is <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="throwExceptionIfNotRunning"/> is <c>true</c> and the task was not created
		/// through TaskManager. The exception message includes the task's <see cref="Task.CreationOptions"/> for debugging.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method checks the <see cref="ITaskManagerEntry.IsCancellationRequested"/> property of the task's entry.
		/// It's meant to be called from within the task's execution to cooperatively respond to cancellation.
		/// </para>
		/// <para>
		/// <strong>Typical usage pattern:</strong>
		/// </para>
		/// <code>
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     while (!task.IsCancellationRequested())
		///     {
		///         ProcessNextItem();
		///     }
		///     
		///     CleanupOnCancellation();
		/// });
		/// </code>
		/// <para>
		/// <strong>Error message (Spanish):</strong> "IsCancellationRequested: La tarea no esta administrada por el TaskManager."
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     for (int i = 0; i &lt; 1000; i++)
		///     {
		///         // Check for cancellation periodically
		///         if (task.IsCancellationRequested())
		///         {
		///             Console.WriteLine("Cancellation requested, stopping...");
		///             return;
		///         }
		///         
		///         ExpensiveOperation(i);
		///     }
		/// });
		/// 
		/// // Safe check without exception for any task
		/// if (someTask.IsCancellationRequested(throwExceptionIfNotRunning: false))
		/// {
		///     Console.WriteLine("Task is being cancelled");
		/// }
		/// </code>
		/// </example>
		public bool IsCancellationRequested(bool throwExceptionIfNotRunning = false)
			=> TaskManager.SearchEntry(me, throwExceptionIfNotRunning)?.IsCancellationRequested
			   ?? (throwExceptionIfNotRunning
				   ? throw new ArgumentException($"IsCancellationRequested: La tarea no esta administrada por el TaskManager. {me.CreationOptions}")
				   : false);

		/// <summary>
		/// Gets the <see cref="CancellationToken"/> associated with this task if it was created by <see cref="TaskManager"/>.
		/// </summary>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an exception if the task is not managed by TaskManager.
		/// If <c>false</c>, returns <see cref="CancellationToken.None"/> for non-managed tasks.
		/// </param>
		/// <returns>
		/// The <see cref="CancellationToken"/> associated with the task, or <see cref="CancellationToken.None"/>
		/// if the task is not managed and <paramref name="throwExceptionIfNotRunning"/> is <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="throwExceptionIfNotRunning"/> is <c>true</c> and the task was not created
		/// through TaskManager.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method retrieves the cancellation token from the task's <see cref="ITaskManagerEntry.CancellationTokenSource"/>.
		/// The token can be used for cooperative cancellation checks or passed to other cancellable operations.
		/// </para>
		/// <para>
		/// <strong>Common uses:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Pass to async methods that accept <see cref="CancellationToken"/></description></item>
		/// <item><description>Check <see cref="CancellationToken.IsCancellationRequested"/> in loops</description></item>
		/// <item><description>Throw <see cref="OperationCanceledException"/> via <see cref="CancellationToken.ThrowIfCancellationRequested"/></description></item>
		/// <item><description>Register callbacks via <see cref="CancellationToken.Register(Action)"/></description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var task = TaskManager.StartNew(async () => 
		/// {
		///     var token = task.GetCancellationToken();
		///     
		///     // Use with async methods
		///     var data = await FetchDataAsync(token);
		///     await ProcessDataAsync(data, token);
		///     
		///     // Or check manually
		///     token.ThrowIfCancellationRequested();
		/// });
		/// 
		/// // Safe retrieval for any task
		/// var token = someTask.GetCancellationToken(throwExceptionIfNotRunning: false);
		/// if (token.CanBeCanceled)
		/// {
		///     Console.WriteLine("Task supports cancellation");
		/// }
		/// </code>
		/// </example>
		public CancellationToken GetCancellationToken(bool throwExceptionIfNotRunning = false)
			=> TaskManager.SearchEntry(me, throwExceptionIfNotRunning)?.CancellationTokenSource.Token ?? CancellationToken.None;

		/// <summary>
		/// Suspends the task execution for the specified duration, respecting the task's cancellation token.
		/// </summary>
		/// <param name="timeout">The duration to sleep.</param>
		/// <param name="rethrowException">
		/// If <c>true</c>, rethrows <see cref="TaskCanceledException"/> when the sleep is interrupted by cancellation.
		/// If <c>false</c>, returns <c>false</c> when cancelled without throwing.
		/// </param>
		/// <returns>
		/// <c>true</c> if the sleep completed normally (full duration elapsed);
		/// <c>false</c> if the sleep was interrupted by cancellation and <paramref name="rethrowException"/> is <c>false</c>.
		/// </returns>
		/// <exception cref="TaskCanceledException">
		/// Thrown when <paramref name="rethrowException"/> is <c>true</c> and the task's cancellation token is signaled
		/// during the sleep.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method provides a cancellation-aware alternative to <see cref="Thread.Sleep(TimeSpan)"/> or <see cref="Task.Delay(TimeSpan)"/>.
		/// It uses the task's cancellation token retrieved via <see cref="GetCancellationToken"/> to make the sleep interruptible.
		/// </para>
		/// <para>
		/// <strong>How it works:</strong>
		/// </para>
		/// <list type="number">
		/// <item><description>Gets the task's cancellation token (throws if task not managed by TaskManager)</description></item>
		/// <item><description>Calls <see cref="Task.Delay(TimeSpan, CancellationToken)"/> with the token</description></item>
		/// <item><description>Waits synchronously using <see cref="Task.Wait()"/></description></item>
		/// <item><description>Catches <see cref="TaskCanceledException"/> or <see cref="AggregateException"/> containing it</description></item>
		/// <item><description>Returns <c>true</c> if completed, <c>false</c> if cancelled (unless rethrowing)</description></item>
		/// </list>
		/// <para>
		/// <strong>Use cases:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Polling loops that need to be cancellable</description></item>
		/// <item><description>Rate limiting with cancellation support</description></item>
		/// <item><description>Retry delays that respect task cancellation</description></item>
		/// <item><description>Periodic operations that can be interrupted</description></item>
		/// </list>
		/// <para>
		/// <strong>Important:</strong> This method blocks the calling thread. For async scenarios, use
		/// <see cref="Task.Delay(TimeSpan, CancellationToken)"/> directly with an await.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Polling loop with cancellable sleep
		/// var task = TaskManager.StartNew(() => 
		/// {
		///     while (true)
		///     {
		///         CheckStatus();
		///         
		///         // Sleep for 1 second, can be cancelled
		///         if (!task.Sleep(TimeSpan.FromSeconds(1)))
		///         {
		///             Console.WriteLine("Sleep was cancelled");
		///             break;
		///         }
		///     }
		/// });
		/// 
		/// // Retry with cancellable delay
		/// var retryTask = TaskManager.StartNew(() => 
		/// {
		///     int attempts = 0;
		///     while (attempts++ &lt; 5)
		///     {
		///         try
		///         {
		///             PerformOperation();
		///             break;
		///         }
		///         catch
		///         {
		///             if (attempts &lt; 5)
		///             {
		///                 // Wait before retry, can be cancelled
		///                 task.Sleep(TimeSpan.FromSeconds(2 * attempts));
		///             }
		///         }
		///     }
		/// });
		/// 
		/// // With exception on cancellation
		/// try
		/// {
		///     task.Sleep(TimeSpan.FromMinutes(5), rethrowException: true);
		/// }
		/// catch (TaskCanceledException)
		/// {
		///     Console.WriteLine("Long sleep was interrupted");
		/// }
		/// </code>
		/// </example>
		public bool Sleep(TimeSpan timeout, bool rethrowException = false)
		{
			try
			{
				// Share the token with Delay method to break the operation if task will canceled
				Task.Delay(timeout, me.GetCancellationToken(true)).Wait();
				return true;
			}
			// If task was cancelled, nothing happens
			catch (Exception ex) when (ex is TaskCanceledException || ex is AggregateException aex && aex.Flatten().InnerException is TaskCanceledException)
			{
				if (rethrowException) throw ex is AggregateException aex2 && aex2.Flatten().InnerException is TaskCanceledException tce ? tce : ex;
				return false;
			}
		}
	}

	/// <summary>
	/// Extension methods for collections of <see cref="Task"/> instances.
	/// </summary>
	extension(IEnumerable<Task> me)
	{
		/// <summary>
		/// Cancels all tasks in the collection and synchronously waits for them to complete.
		/// </summary>
		/// <param name="timeout">
		/// The maximum time to wait for all tasks to complete. Use <c>default(TimeSpan)</c> or <see cref="TimeSpan.Zero"/>
		/// to wait indefinitely.
		/// </param>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an exception if any task is not managed by TaskManager.
		/// If <c>false</c>, only waits without cancellation for non-managed tasks.
		/// </param>
		/// <remarks>
		/// <para>
		/// This method provides bulk cancellation and synchronous waiting for multiple tasks. The process:
		/// </para>
		/// <list type="number">
		/// <item><description>Iterates through all tasks and calls <see cref="Extensions.Cancel"/> on each</description></item>
		/// <item><description>Filters out tasks that are already cancelled (to avoid waiting on them)</description></item>
		/// <item><description>Calls <see cref="Task.WaitAll(Task[])"/> or <see cref="Task.WaitAll(Task[], TimeSpan)"/> on remaining tasks</description></item>
		/// <item><description>Swallows <see cref="TaskCanceledException"/> (expected outcome of cancellation)</description></item>
		/// </list>
		/// <para>
		/// <strong>Timeout behavior:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>If <paramref name="timeout"/> is <c>default</c> or <see cref="TimeSpan.Zero"/>, waits indefinitely for all tasks</description></item>
		/// <item><description>If timeout expires, the method returns but tasks may still be running</description></item>
		/// <item><description>Check <see cref="Task.IsCompleted"/> on individual tasks after timeout to verify completion</description></item>
		/// </list>
		/// <para>
		/// <strong>Exception handling:</strong> Only <see cref="TaskCanceledException"/> and <see cref="AggregateException"/>
		/// containing it are suppressed. Other exceptions from tasks are not caught.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Create multiple tasks
		/// var tasks = new[]
		/// {
		///     TaskManager.StartNew(() => LongOperation1()),
		///     TaskManager.StartNew(() => LongOperation2()),
		///     TaskManager.StartNew(() => LongOperation3())
		/// };
		/// 
		/// // Cancel all and wait indefinitely
		/// tasks.CancelAndWait();
		/// 
		/// // Cancel all and wait with timeout
		/// tasks.CancelAndWait(TimeSpan.FromSeconds(10));
		/// 
		/// // Check for incomplete tasks after timeout
		/// var stillRunning = tasks.Where(t => !t.IsCompleted).ToList();
		/// if (stillRunning.Any())
		/// {
		///     Console.WriteLine($"{stillRunning.Count} tasks still running");
		/// }
		/// </code>
		/// </example>
		public void CancelAndWait(TimeSpan timeout = default, bool throwExceptionIfNotRunning = true)
		{
			foreach (var task in me) task.Cancel(throwExceptionIfNotRunning);
			try
			{
				if (timeout != default)
					Task.WaitAll(me.Where(t => t is { IsCanceled: false }).ToArray(), timeout);
				else
					Task.WaitAll(me.Where(t => t is { IsCanceled: false }).ToArray());
			}
			// If task was cancelled, nothing happens
			catch (Exception ex) when (ex is TaskCanceledException || ex is AggregateException aex && aex.Flatten().InnerException is TaskCanceledException) { }
		}

		/// <summary>
		/// Asynchronously cancels all tasks in the collection and waits for them to complete.
		/// </summary>
		/// <param name="throwExceptionIfNotRunning">
		/// If <c>true</c>, throws an exception if any task is not managed by TaskManager.
		/// If <c>false</c>, only waits without cancellation for non-managed tasks.
		/// </param>
		/// <returns>A <see cref="Task"/> representing the asynchronous wait operation.</returns>
		/// <remarks>
		/// <para>
		/// This is the async version of <see cref="CancelAndWait(Task,TimeSpan, bool)"/>. It provides non-blocking cancellation and waiting
		/// for multiple tasks. The process:
		/// </para>
		/// <list type="number">
		/// <item><description>Iterates through all tasks and calls <see cref="Extensions.Cancel"/> on each</description></item>
		/// <item><description>Uses <see cref="Task.WhenAll(Task[])"/> to asynchronously wait for all tasks</description></item>
		/// <item><description>Swallows <see cref="TaskCanceledException"/> and <see cref="AggregateException"/> containing it</description></item>
		/// </list>
		/// <para>
		/// <strong>Advantages over synchronous version:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Doesn't block the calling thread while waiting</description></item>
		/// <item><description>Better for UI applications (no freezing)</description></item>
		/// <item><description>Allows other async operations to proceed concurrently</description></item>
		/// <item><description>No timeout parameter needed (can use Task.WhenAny with a delay task if needed)</description></item>
		/// </list>
		/// <para>
		/// <strong>Exception handling:</strong> Like the synchronous version, only cancellation exceptions are suppressed.
		/// Other exceptions are propagated through the returned task.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Create multiple background tasks
		/// var tasks = Enumerable.Range(1, 10)
		///     .Select(i => TaskManager.StartNew(() => ProcessItem(i)))
		///     .ToList();
		/// 
		/// // Cancel all asynchronously
		/// await tasks.CancelAndWaitAsync();
		/// 
		/// // Use in UI shutdown
		/// private async void Window_Closing(object sender, CancelEventArgs e)
		/// {
		///     e.Cancel = true; // Prevent immediate close
		///     
		///     await _backgroundTasks.CancelAndWaitAsync();
		///     
		///     e.Cancel = false; // Allow close
		///     Close();
		/// }
		/// 
		/// // Cancel with timeout using Task.WhenAny
		/// var cancelTask = tasks.CancelAndWaitAsync();
		/// var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
		/// 
		/// var completedTask = await Task.WhenAny(cancelTask, timeoutTask);
		/// if (completedTask == timeoutTask)
		/// {
		///     Console.WriteLine("Cancellation timed out");
		/// }
		/// </code>
		/// </example>
		public async Task CancelAndWaitAsync(bool throwExceptionIfNotRunning = true)
		{
			foreach (var task in me) task.Cancel(throwExceptionIfNotRunning);
			try
			{
				await Task.WhenAll(me);
			}
			// If task was cancelled, nothing happens
			catch (Exception ex) when (ex is TaskCanceledException || ex is AggregateException aex && aex.Flatten().InnerException is TaskCanceledException) { }
		}
	}
}