using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fuxion.Threading.Tasks;

public static partial class TaskManager
{
	#region Result<TResult>
	/// <summary>
	/// Creates an internal task entry for a parameterless function that returns a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The function to execute that returns a result.</param>
	/// <param name="scheduler">Optional task scheduler. If <c>null</c>, uses the default scheduler.</param>
	/// <param name="options">Task creation options controlling behavior.</param>
	/// <param name="concurrencyProfile">Concurrency control profile for the task.</param>
	/// <param name="delegate">Optional reference to the original delegate for metadata/debugging.</param>
	/// <returns>The created task manager entry containing the task and metadata.</returns>
	/// <remarks>
	/// This is an internal helper method that wraps the function in a <see cref="FuncTaskManagerEntry{TResult}"/>
	/// and registers it with the task tracking system via <see cref="AddEntry"/>.
	/// </remarks>
	static FuncTaskManagerEntry<TResult> CreateEntry<TResult>(Func<TResult> func,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(func, scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes the specified function and returns a result, without starting it.
	/// </summary>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The synchronous function to execute.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task{TResult}"/> that can be started manually.</returns>
	/// <remarks>
	/// The returned task is not started automatically. To start it, retrieve the entry using
	/// <see cref="SearchEntry(Task)"/> and call its Start() method.
	/// </remarks>
	/// <example>
	/// <code>
	/// var task = TaskManager.Create(() => 42);
	/// // Task is created but not running yet
	/// TaskManager.SearchEntry(task).Start();
	/// int result = await task; // result = 42
	/// </code>
	/// </example>
	public static Task<TResult> Create<TResult>(Func<TResult> func, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes the specified asynchronous function and returns a result, without starting it.
	/// </summary>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The asynchronous function returning a <see cref="Task{TResult}"/>.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task{TResult}"/> that can be started manually.</returns>
	/// <remarks>
	/// The async function is wrapped with .Result to block until completion. The original delegate
	/// is preserved for metadata purposes.
	/// </remarks>
	public static Task<TResult> Create<TResult>(Func<Task<TResult>> func, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(() => func().Result, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes the specified function and returns a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The synchronous function to execute.</param>
	/// <param name="scheduler">Optional scheduler to run the task on. If <c>null</c>, uses the default scheduler.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task{TResult}"/>.</returns>
	/// <remarks>
	/// This method creates the task and immediately starts it. It's equivalent to calling
	/// <see cref="Create{TResult}(Func{TResult}, TaskCreationOptions, ConcurrencyProfile)"/> followed by Start().
	/// </remarks>
	/// <example>
	/// <code>
	/// var task = TaskManager.StartNew(() => 
	/// {
	///     Thread.Sleep(1000);
	///     return DateTime.Now;
	/// });
	/// DateTime result = await task;
	/// Console.WriteLine($"Computed at: {result}");
	/// </code>
	/// </example>
	public static Task<TResult> StartNew<TResult>(Func<TResult> func, TaskScheduler? scheduler = null, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, scheduler, options, concurrencyProfile).Task;
		;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes the specified asynchronous function and returns a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The asynchronous function returning a <see cref="Task{TResult}"/>.</param>
	/// <param name="scheduler">Optional scheduler to run the task on. If <c>null</c>, uses the default scheduler.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task{TResult}"/>.</returns>
	/// <remarks>
	/// The async function is wrapped with .Result to block until completion.
	/// </remarks>
	public static Task<TResult> StartNew<TResult>(Func<Task<TResult>> func, TaskScheduler? scheduler = null, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(() => func().Result, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with one parameter that returns a result.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The function to execute with one parameter.</param>
	/// <param name="param">The parameter value to pass to the function.</param>
	/// <param name="scheduler">Optional task scheduler.</param>
	/// <param name="options">Task creation options.</param>
	/// <param name="concurrencyProfile">Concurrency control profile.</param>
	/// <param name="delegate">Optional reference to the original delegate.</param>
	/// <returns>The created task manager entry.</returns>
	/// <remarks>
	/// Uses <see cref="ValueTuple{T}"/> internally to efficiently pass the single parameter.
	/// </remarks>
	static FuncTaskManagerEntry<TResult> CreateEntry<T, TResult>(Func<T, TResult> func,
		T param,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null) where T : notnull
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => func((T)o), param, scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes the specified function with one parameter and returns a result, without starting it.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The synchronous function to execute.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task{TResult}"/> that can be started manually.</returns>
	/// <example>
	/// <code>
	/// var task = TaskManager.Create((int x) => x * 2, 21);
	/// TaskManager.SearchEntry(task).Start();
	/// int result = await task; // result = 42
	/// </code>
	/// </example>
	public static Task<TResult> Create<T, TResult>(Func<T, TResult> func, T param, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) where T : notnull =>
		(Task<TResult>)CreateEntry(func, param, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes the specified asynchronous function with one parameter and returns a result, without starting it.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task{TResult}"/> that can be started manually.</returns>
	public static Task<TResult> Create<T, TResult>(Func<T, Task<TResult>> func, T param, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) where T : notnull =>
		(Task<TResult>)CreateEntry(p => func(p).Result, param, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes the specified function with one parameter and returns a result.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The synchronous function to execute.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task{TResult}"/>.</returns>
	/// <example>
	/// <code>
	/// var task = TaskManager.StartNew(
	///     (string name) => $"Hello, {name}!",
	///     "World"
	/// );
	/// string result = await task; // result = "Hello, World!"
	/// </code>
	/// </example>
	public static Task<TResult> StartNew<T, TResult>(Func<T, TResult> func,
		T param,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) where T : notnull
	{
		var task = (Task<TResult>)CreateEntry(func, param, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes the specified asynchronous function with one parameter and returns a result.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task{TResult}"/>.</returns>
	public static Task<TResult> StartNew<T, TResult>(Func<T, Task<TResult>> func,
		T param,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) where T : notnull
	{
		var task = (Task<TResult>)CreateEntry(p => func(p).Result, param, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with two parameters that returns a result.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The function to execute with two parameters.</param>
	/// <param name="param1">The first parameter value.</param>
	/// <param name="param2">The second parameter value.</param>
	/// <param name="scheduler">Optional task scheduler.</param>
	/// <param name="options">Task creation options.</param>
	/// <param name="concurrencyProfile">Concurrency control profile.</param>
	/// <param name="delegate">Optional reference to the original delegate.</param>
	/// <returns>The created task manager entry.</returns>
	/// <remarks>
	/// Uses <see cref="ValueTuple{T1, T2}"/> internally to efficiently pass multiple parameters.
	/// </remarks>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, TResult>(Func<T1, T2, TResult> func,
		T1 param1,
		T2 param2,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2) = (ValueTuple<T1, T2>)o;
			return func(p1, p2);
		}, (param1, param2), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with two parameters and returns a result, without starting it.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The synchronous function to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task{TResult}"/> that can be started manually.</returns>
	public static Task<TResult> Create<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 param1, T2 param2, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with two parameters and returns a result, without starting it.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task{TResult}"/> that can be started manually.</returns>
	public static Task<TResult>
		Create<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func, T1 param1, T2 param2, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2) => func(p1, p2).Result, param1, param2, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with two parameters and returns a result.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The synchronous function to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task{TResult}"/>.</returns>
	/// <example>
	/// <code>
	/// var task = TaskManager.StartNew(
	///     (int a, int b) => a + b,
	///     10,
	///     32
	/// );
	/// int result = await task; // result = 42
	/// </code>
	/// </example>
	public static Task<TResult> StartNew<T1, T2, TResult>(Func<T1, T2, TResult> func,
		T1 param1,
		T2 param2,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) where T1 : notnull where T2 : notnull
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with two parameters and returns a result.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task{TResult}"/>.</returns>
	public static Task<TResult> StartNew<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func,
		T1 param1,
		T2 param2,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) where T1 : notnull where T2 : notnull
	{
		var task = (Task<TResult>)CreateEntry((p1, p2) => func(p1, p2).Result, param1, param2, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with three parameters that returns a result.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="ValueTuple{T1, T2, T3}"/> internally for parameter passing.
	/// </remarks>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3) = (ValueTuple<T1, T2, T3>)o;
			return func(p1, p2, p3);
		}, (param1, param2, param3), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with three parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with three parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3) => func(p1, p2, p3).Result, param1, param2, param3, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with three parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with three parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3) => func(p1, p2, p3).Result, param1, param2, param3, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, T4, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with four parameters that returns a result.
	/// </summary>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3, p4) = (ValueTuple<T1, T2, T3, T4>)o;
			return func(p1, p2, p3, p4);
		}, (param1, param2, param3, param4), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with four parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, param4, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with four parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3, p4) => func(p1, p2, p3, p4).Result, param1, param2, param3, param4, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with four parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, param4, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with four parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3, p4) => func(p1, p2, p3, p4).Result, param1, param2, param3, param4, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, T4, T5, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with five parameters that returns a result.
	/// </summary>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3, p4, p5) = (ValueTuple<T1, T2, T3, T4, T5>)o;
			return func(p1, p2, p3, p4, p5);
		}, (param1, param2, param3, param4, param5), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with five parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with five parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3, p4, p5) => func(p1, p2, p3, p4, p5).Result, param1, param2, param3, param4, param5, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with five parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with five parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3, p4, p5) => func(p1, p2, p3, p4, p5).Result, param1, param2, param3, param4, param5, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, T4, T5, T6, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with six parameters that returns a result.
	/// </summary>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3, p4, p5, p6) = (ValueTuple<T1, T2, T3, T4, T5, T6>)o;
			return func(p1, p2, p3, p4, p5, p6);
		}, (param1, param2, param3, param4, param5, param6), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with six parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with six parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6) => func(p1, p2, p3, p4, p5, p6).Result, param1, param2, param3, param4, param5, param6, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with six parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with six parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6) => func(p1, p2, p3, p4, p5, p6).Result, param1, param2, param3, param4, param5, param6, scheduler, options, concurrencyProfile,
			func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, T4, T5, T6, T7, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with seven parameters that returns a result.
	/// </summary>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3, p4, p5, p6, p7) = (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)o;
			return func(p1, p2, p3, p4, p5, p6, p7);
		}, (param1, param2, param3, param4, param5, param6, param7), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with seven parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, param7, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with seven parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6, p7) => func(p1, p2, p3, p4, p5, p6, p7).Result, param1, param2, param3, param4, param5, param6, param7, null, options, concurrencyProfile,
			func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with seven parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, param7, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with seven parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6, p7) => func(p1, p2, p3, p4, p5, p6, p7).Result, param1, param2, param3, param4, param5, param6, param7, scheduler, options,
			concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, T4, T5, T6, T7, T8, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with eight parameters that returns a result.
	/// </summary>
	/// <remarks>
	/// Note: Uses nested <see cref="ValueTuple"/> (ValueTuple&lt;T1-T7, ValueTuple&lt;T8&gt;&gt;) to support 8 parameters.
	/// </remarks>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3, p4, p5, p6, p7, p8) = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>)o;
			return func(p1, p2, p3, p4, p5, p6, p7, p8);
		}, (param1, param2, param3, param4, param5, param6, param7, param8), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with eight parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, param7, param8, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with eight parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8) => func(p1, p2, p3, p4, p5, p6, p7, p8).Result, param1, param2, param3, param4, param5, param6, param7, param8, null, options,
			concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with eight parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, param7, param8, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with eight parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8) => func(p1, p2, p3, p4, p5, p6, p7, p8).Result, param1, param2, param3, param4, param5, param6, param7, param8, scheduler,
			options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Result<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>
	/// <summary>
	/// Creates an internal task entry for a function with nine parameters that returns a result.
	/// </summary>
	/// <remarks>
	/// Note: Uses nested <see cref="ValueTuple"/> (ValueTuple&lt;T1-T7, ValueTuple&lt;T8, T9&gt;&gt;) to support 9 parameters.
	/// </remarks>
	static FuncTaskManagerEntry<TResult> CreateEntry<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		T9 param9,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new FuncTaskManagerEntry<TResult>(o => {
			var (p1, p2, p3, p4, p5, p6, p7, p8, p9) = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>)o;
			return func(p1, p2, p3, p4, p5, p6, p7, p8, p9);
		}, (param1, param2, param3, param4, param5, param6, param7, param8, param9), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes a function with nine parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		T9 param9,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, param7, param8, param9, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with nine parameters and returns a result, without starting it.
	/// </summary>
	public static Task<TResult> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		T9 param9,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		(Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8, p9) => func(p1, p2, p3, p4, p5, p6, p7, p8, p9).Result, param1, param2, param3, param4, param5, param6, param7, param8, param9, null,
			options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes a function with nine parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		T9 param9,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry(func, param1, param2, param3, param4, param5, param6, param7, param8, param9, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with nine parameters and returns a result.
	/// </summary>
	public static Task<TResult> StartNew<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Task<TResult>> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		T8 param8,
		T9 param9,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = (Task<TResult>)CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8, p9) => func(p1, p2, p3, p4, p5, p6, p7, p8, p9).Result, param1, param2, param3, param4, param5, param6, param7, param8,
			param9, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion
}