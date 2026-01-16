using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fuxion.Threading.Tasks;

/// <summary>
/// Provides advanced task management capabilities with tracking, concurrency control, and enhanced creation options.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TaskManager"/> is a static utility class that extends the standard <see cref="Task"/> functionality
/// by providing:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Task Tracking:</strong> All tasks created through TaskManager are tracked and can be queried</description></item>
/// <item><description><strong>Concurrency Profiles:</strong> Built-in concurrency control through <see cref="ConcurrencyProfile"/></description></item>
/// <item><description><strong>Current Task Access:</strong> Easy access to the currently executing managed task via <see cref="Current"/></description></item>
/// <item><description><strong>Overload Support:</strong> Methods for 0-9 parameters with both synchronous and asynchronous delegates</description></item>
/// <item><description><strong>Return Value Support:</strong> Both void and result-returning task creation</description></item>
/// <item><description><strong>Thread-Safe Operations:</strong> All internal task tracking is thread-safe via <see cref="Locker{T}"/></description></item>
/// </list>
/// <para>
/// The class is implemented as a partial class split across three files:
/// </para>
/// <list type="bullet">
/// <item><description><strong>TaskManager.cs:</strong> Core infrastructure, task storage, and entry management</description></item>
/// <item><description><strong>TaskManager2.cs:</strong> Void-returning task creation methods (Action delegates)</description></item>
/// <item><description><strong>TaskManager3.cs:</strong> Result-returning task creation methods (Func delegates)</description></item>
/// </list>
/// <para>
/// <strong>Key differences from standard Task.Factory.StartNew:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Automatic task tracking and lifecycle management</description></item>
/// <item><description>Concurrency profile support for controlling execution patterns</description></item>
/// <item><description>Access to task metadata through <see cref="ITaskManagerEntry"/></description></item>
/// <item><description>Automatic cleanup when tasks complete</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Create and start a simple task
/// var task = TaskManager.StartNew(() => Console.WriteLine("Hello"));
/// 
/// // Create a task with parameters
/// var task2 = TaskManager.StartNew(
///     (name, age) => Console.WriteLine($"{name} is {age}"), 
///     "John", 
///     30
/// );
/// 
/// // Create a task with return value
/// var resultTask = TaskManager.StartNew(() => 42);
/// int result = await resultTask;
/// 
/// // Access current task from within a managed task
/// TaskManager.StartNew(() => 
/// {
///     var current = TaskManager.Current; // Gets the current task
///     Console.WriteLine($"Task ID: {current?.Id}");
/// });
/// 
/// // Create task with concurrency profile
/// var task3 = TaskManager.StartNew(
///     () => DoWork(),
///     concurrencyProfile: ConcurrencyProfile.MaxConcurrency(4)
/// );
/// 
/// // Create without starting
/// var task4 = TaskManager.Create(() => Console.WriteLine("Not started yet"));
/// // Start later
/// TaskManager.SearchEntry(task4).Start();
/// </code>
/// </example>
public static partial class TaskManager
{
	/// <summary>
	/// Thread-safe storage for all managed tasks with their associated metadata.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This locker protects a list of <see cref="ITaskManagerEntry"/> instances, where each entry contains:
	/// </para>
	/// <list type="bullet">
	/// <item><description>The actual <see cref="Task"/> instance</description></item>
	/// <item><description>Metadata about task creation (scheduler, options, concurrency profile)</description></item>
	/// <item><description>Optional reference to the original delegate for debugging</description></item>
	/// </list>
	/// <para>
	/// Entries are automatically added when tasks are created and removed when tasks complete.
	/// All access to this collection is synchronized through the <see cref="Locker{T}"/> wrapper.
	/// </para>
	/// </remarks>
	internal static readonly Locker<List<ITaskManagerEntry>> Tasks = new([]);
	
	/// <summary>
	/// Gets the current managed task, if the calling code is executing within a task created by <see cref="TaskManager"/>.
	/// </summary>
	/// <value>
	/// The <see cref="Task"/> instance of the currently executing managed task, or <c>null</c> if:
	/// <list type="bullet">
	/// <item><description>The current code is not executing within a TaskManager-created task</description></item>
	/// <item><description>The task was not created through TaskManager</description></item>
	/// <item><description><see cref="Task.CurrentId"/> is <c>null</c></description></item>
	/// </list>
	/// </value>
	/// <remarks>
	/// <para>
	/// This property uses <see cref="Task.CurrentId"/> to identify the calling task and looks it up
	/// in the managed task collection. This is useful for:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Accessing task metadata from within task execution</description></item>
	/// <item><description>Debugging and logging task information</description></item>
	/// <item><description>Implementing task-local storage patterns</description></item>
	/// </list>
	/// <para>
	/// <strong>Performance note:</strong> This property performs a thread-safe search through the managed
	/// task list on each access. Consider caching the result if accessed frequently within a task.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// TaskManager.StartNew(() => 
	/// {
	///     var current = TaskManager.Current;
	///     if (current != null)
	///     {
	///         Console.WriteLine($"Running in task {current.Id}");
	///         Console.WriteLine($"Status: {current.Status}");
	///     }
	/// });
	/// </code>
	/// </example>
	public static Task? Current => CurrentEntry?.Task;

	/// <summary>
	/// Gets the current managed task entry, if the calling code is executing within a task created by <see cref="TaskManager"/>.
	/// </summary>
	/// <value>
	/// The <see cref="ITaskManagerEntry"/> instance containing the current task and its metadata,
	/// or <c>null</c> if not executing within a managed task.
	/// </value>
	/// <remarks>
	/// <para>
	/// This property provides access to the full task entry, which includes:
	/// </para>
	/// <list type="bullet">
	/// <item><description>The <see cref="Task"/> instance via <see cref="ITaskManagerEntry.Task"/></description></item>
	/// <item><description>Creation options via <see cref="ITaskManagerEntry"/>.Options</description></item>
	/// <item><description>Concurrency profile via <see cref="ITaskManagerEntry"/>.ConcurrencyProfile</description></item>
	/// <item><description>Optional task scheduler via <see cref="ITaskManagerEntry"/>.Scheduler</description></item>
	/// </list>
	/// <para>
	/// The lookup is performed by matching <see cref="Task.CurrentId"/> against tracked tasks.
	/// </para>
	/// </remarks>
	internal static ITaskManagerEntry? CurrentEntry => Tasks.Read(l => l.FirstOrDefault(e => Task.CurrentId.HasValue && e.Task.Id == Task.CurrentId.Value));

	/// <summary>
	/// Adds a task entry to the managed task collection and sets up automatic cleanup on completion.
	/// </summary>
	/// <param name="entry">The task entry to add to the tracking system.</param>
	/// <remarks>
	/// <para>
	/// This method performs two key operations:
	/// </para>
	/// <list type="number">
	/// <item><description>
	/// Registers a continuation on the task that will automatically remove the entry from the
	/// collection when the task completes (regardless of outcome: success, fault, or cancellation).
	/// </description></item>
	/// <item><description>
	/// Adds the entry to the thread-safe <see cref="Tasks"/> collection for tracking.
	/// </description></item>
	/// </list>
	/// <para>
	/// <strong>Automatic Cleanup:</strong> The continuation ensures that completed tasks don't
	/// accumulate in memory. The cleanup happens asynchronously after task completion.
	/// </para>
	/// <para>
	/// <strong>Thread Safety:</strong> Both the continuation registration and the collection
	/// modification are thread-safe operations.
	/// </para>
	/// </remarks>
	private static void AddEntry(ITaskManagerEntry entry)
	{
		entry.Task.ContinueWith(t => Tasks.Write(l => { l.Remove(entry); }));
		Tasks.Write(l => { l.Add(entry); });
	}

	/// <summary>
	/// Searches for a task entry in the managed collection and throws an exception if not found.
	/// </summary>
	/// <param name="task">The task to search for.</param>
	/// <returns>The <see cref="ITaskManagerEntry"/> associated with the specified task.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the task is not found in the managed collection, indicating it wasn't created
	/// through <see cref="TaskManager"/>.
	/// </exception>
	/// <remarks>
	/// <para>
	/// This method is used internally by TaskManager to retrieve task metadata for tasks that
	/// were created via 'Create' methods but need to be started later.
	/// </para>
	/// <para>
	/// <strong>Search behavior:</strong> The search compares task instances by reference equality.
	/// </para>
	/// <para>
	/// <strong>Common causes of ArgumentException:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Task was created via standard <see cref="Task.Factory"/>.StartNew or <see cref="Task"/>.Run</description></item>
	/// <item><description>Task has already completed and been removed from tracking</description></item>
	/// <item><description>Task object is from a different TaskManager instance (if multiple contexts exist)</description></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// // This will work - task created through TaskManager
	/// var task = TaskManager.Create(() => Console.WriteLine("Hello"));
	/// var entry = TaskManager.SearchEntry(task);
	/// entry.Start();
	/// 
	/// // This will throw - task not created through TaskManager
	/// var standardTask = Task.Run(() => Console.WriteLine("Hello"));
	/// try 
	/// {
	///     var entry = TaskManager.SearchEntry(standardTask); // ArgumentException
	/// }
	/// catch (ArgumentException ex)
	/// {
	///     Console.WriteLine(ex.Message); // "Task wasn't created with TaskManager."
	/// }
	/// </code>
	/// </example>
	internal static ITaskManagerEntry SearchEntry(Task task)
	{
		//Busco entre las tareas administradas
		var res = Tasks.Read(l => l.FirstOrDefault(e => e.Task == task));
		//Compruebo si se encontró y si debo lanzar una excepción
		return res ?? throw new ArgumentException("Task wasn't created with TaskManager.");
	}
	
	/// <summary>
	/// Searches for a task entry in the managed collection with optional exception throwing.
	/// </summary>
	/// <param name="task">The task to search for.</param>
	/// <param name="throwExceptionIfNotFound">
	/// If <c>true</c>, throws an <see cref="ArgumentException"/> when the task is not found.
	/// If <c>false</c>, returns <c>null</c> when the task is not found.
	/// </param>
	/// <returns>
	/// The <see cref="ITaskManagerEntry"/> associated with the specified task, or <c>null</c>
	/// if not found and <paramref name="throwExceptionIfNotFound"/> is <c>false</c>.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the task is not found and <paramref name="throwExceptionIfNotFound"/> is <c>true</c>.
	/// </exception>
	/// <remarks>
	/// <para>
	/// This overload provides flexibility for callers that want to handle missing tasks gracefully
	/// without exception handling overhead.
	/// </para>
	/// <para>
	/// <strong>Use cases:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>throwExceptionIfNotFound = true:</strong> When task must exist (internal operations)</description></item>
	/// <item><description><strong>throwExceptionIfNotFound = false:</strong> When checking if a task is managed (validation, diagnostics)</description></item>
	/// </list>
	/// <para>
	/// The search is performed thread-safely using the <see cref="Tasks"/> locker.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var task = Task.Run(() => Console.WriteLine("Standard task"));
	/// 
	/// // Safe check without exception
	/// var entry = TaskManager.SearchEntry(task, throwExceptionIfNotFound: false);
	/// if (entry == null)
	/// {
	///     Console.WriteLine("Task is not managed by TaskManager");
	/// }
	/// 
	/// // Will throw if not found
	/// try
	/// {
	///     var entry2 = TaskManager.SearchEntry(task, throwExceptionIfNotFound: true);
	/// }
	/// catch (ArgumentException)
	/// {
	///     Console.WriteLine("Task not found in TaskManager");
	/// }
	/// </code>
	/// </example>
	internal static ITaskManagerEntry? SearchEntry(Task task, bool throwExceptionIfNotFound)
	{
		//Busco entre las tareas administradas
		var res = Tasks.Read(l => l.FirstOrDefault(e => e.Task == task));
		//Compruebo si se encontró y si debo lanzar una excepción
		if (res == null && throwExceptionIfNotFound) throw new ArgumentException("Task wasn't created with TaskManager.");
		return res;
	}
}