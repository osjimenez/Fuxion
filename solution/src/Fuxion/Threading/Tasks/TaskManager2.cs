using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fuxion.Threading.Tasks;

public static partial class TaskManager
{
	#region Void
	/// <summary>
	/// Creates an internal task entry for a parameterless action.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="scheduler">Optional task scheduler. If <c>null</c>, uses the default scheduler.</param>
	/// <param name="options">Task creation options controlling behavior.</param>
	/// <param name="concurrencyProfile">Concurrency control profile for the task.</param>
	/// <param name="delegate">Optional reference to the original delegate for metadata/debugging.</param>
	/// <returns>The created task manager entry containing the task and metadata.</returns>
	/// <remarks>
	/// This is an internal helper method that wraps the action in an <see cref="ActionTaskManagerEntry"/>
	/// and registers it with the task tracking system via <see cref="AddEntry"/>.
	/// </remarks>
	static ActionTaskManagerEntry CreateEntry(Action action,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new ActionTaskManagerEntry(action, scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes the specified action without starting it.
	/// </summary>
	/// <param name="action">The synchronous action to execute.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task"/> that can be started manually.</returns>
	/// <remarks>
	/// The returned task is not started automatically. To start it, retrieve the entry using
	/// <see cref="SearchEntry(Task)"/> and call its Start() method.
	/// </remarks>
	/// <example>
	/// <code>
	/// var task = TaskManager.Create(() => Console.WriteLine("Hello"));
	/// // Task is created but not running yet
	/// TaskManager.SearchEntry(task).Start();
	/// await task;
	/// </code>
	/// </example>
	public static Task Create(Action action, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) => CreateEntry(action, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes the specified asynchronous function without starting it.
	/// </summary>
	/// <param name="func">The asynchronous function returning a <see cref="Task"/>.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task"/> that can be started manually.</returns>
	/// <remarks>
	/// The async function is wrapped with .Wait() to block until completion. The original delegate
	/// is preserved for metadata purposes.
	/// </remarks>
	public static Task Create(Func<Task> func, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(() => func().Wait(), null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes the specified action.
	/// </summary>
	/// <param name="action">The synchronous action to execute.</param>
	/// <param name="scheduler">Optional scheduler to run the task on. If <c>null</c>, uses the default scheduler.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	/// <remarks>
	/// This method creates the task and immediately starts it. It's equivalent to calling
	/// <see cref="Create(Action, TaskCreationOptions, ConcurrencyProfile)"/> followed by Start().
	/// </remarks>
	/// <example>
	/// <code>
	/// var task = TaskManager.StartNew(() => 
	/// {
	///     Console.WriteLine("Task is running");
	///     Thread.Sleep(1000);
	/// });
	/// await task;
	/// </code>
	/// </example>
	public static Task StartNew(Action action, TaskScheduler? scheduler = null, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry(action, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes the specified asynchronous function.
	/// </summary>
	/// <param name="func">The asynchronous function returning a <see cref="Task"/>.</param>
	/// <param name="scheduler">Optional scheduler to run the task on. If <c>null</c>, uses the default scheduler.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	/// <remarks>
	/// The async function is wrapped with .Wait() to block until completion.
	/// </remarks>
	public static Task StartNew(Func<Task> func, TaskScheduler? scheduler = null, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry(() => func().Wait(), scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T>
	/// <summary>
	/// Creates an internal task entry for an action with one parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <param name="action">The action to execute with one parameter.</param>
	/// <param name="param">The parameter value to pass to the action.</param>
	/// <param name="scheduler">Optional task scheduler.</param>
	/// <param name="options">Task creation options.</param>
	/// <param name="concurrencyProfile">Concurrency control profile.</param>
	/// <param name="delegate">Optional reference to the original delegate.</param>
	/// <returns>The created task manager entry.</returns>
	/// <remarks>
	/// Uses <see cref="ValueTuple{T1, T2}"/> internally to efficiently pass multiple parameters.
	/// </remarks>
	static ActionTaskManagerEntry CreateEntry<T>(Action<T> action,
		T param,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null) where T : notnull
	{
		var entry = new ActionTaskManagerEntry(o => action((T)o), param, scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes the specified action with one parameter, without starting it.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <param name="action">The synchronous action to execute.</param>
	/// <param name="param">The parameter to pass to the action.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task"/> that can be started manually.</returns>
	/// <example>
	/// <code>
	/// var task = TaskManager.Create((string msg) => Console.WriteLine(msg), "Hello World");
	/// TaskManager.SearchEntry(task).Start();
	/// await task;
	/// </code>
	/// </example>
	public static Task Create<T>(Action<T> action, T param, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) where T : notnull =>
		CreateEntry(action, param, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes the specified asynchronous function with one parameter, without starting it.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task"/> that can be started manually.</returns>
	public static Task Create<T>(Func<T, Task> func, T param, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) where T : notnull =>
		CreateEntry(p => func(p).Wait(), param, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes the specified action with one parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <param name="action">The synchronous action to execute.</param>
	/// <param name="param">The parameter to pass to the action.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	/// <example>
	/// <code>
	/// var task = TaskManager.StartNew(
	///     (int x) => Console.WriteLine($"Number: {x}"), 
	///     42
	/// );
	/// await task;
	/// </code>
	/// </example>
	public static Task StartNew<T>(Action<T> action, T param, TaskScheduler? scheduler = null, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) where T : notnull
	{
		var task = CreateEntry(action, param, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes the specified asynchronous function with one parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter (must be non-null).</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	public static Task StartNew<T>(Func<T, Task> func, T param, TaskScheduler? scheduler = null, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default)
		where T : notnull
	{
		var task = CreateEntry(p => func(p).Wait(), param, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1, T2>
	/// <summary>
	/// Creates an internal task entry for an action with two parameters.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <param name="action">The action to execute with two parameters.</param>
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
	static ActionTaskManagerEntry CreateEntry<T1, T2>(Action<T1, T2> action,
		T1 param1,
		T2 param2,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2) = (ValueTuple<T1, T2>)o;
			action.Invoke(p1, p2);
		}, (param1, param2), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes the specified action with two parameters, without starting it.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <param name="action">The synchronous action to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task"/> that can be started manually.</returns>
	public static Task Create<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(action, param1, param2, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes the specified asynchronous function with two parameters, without starting it.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A <see cref="Task"/> that can be started manually.</returns>
	public static Task Create<T1, T2>(Func<T1, T2, Task> func, T1 param1, T2 param2, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry((p1, p2) => func(p1, p2).Wait(), param1, param2, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes the specified action with two parameters.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <param name="action">The synchronous action to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	/// <example>
	/// <code>
	/// var task = TaskManager.StartNew(
	///     (string name, int age) => Console.WriteLine($"{name} is {age}"),
	///     "Alice", 
	///     30
	/// );
	/// await task;
	/// </code>
	/// </example>
	public static Task StartNew<T1, T2>(Action<T1, T2> action,
		T1 param1,
		T2 param2,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry(action, param1, param2, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes the specified asynchronous function with two parameters.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="scheduler">Optional scheduler to run the task on.</param>
	/// <param name="options">Options controlling the task's behavior.</param>
	/// <param name="concurrencyProfile">Profile controlling concurrency behavior.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	public static Task StartNew<T1, T2>(Func<T1, T2, Task> func,
		T1 param1,
		T2 param2,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry((p1, p2) => func(p1, p2).Wait(), param1, param2, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3>
	/// <summary>
	/// Creates an internal task entry for an action with three parameters.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="ValueTuple{T1, T2, T3}"/> internally for parameter passing.
	/// </remarks>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3>(Action<T1, T2, T3> action,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3) = (ValueTuple<T1, T2, T3>)o;
			action.Invoke(p1, p2, p3);
		}, (param1, param2, param3), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with three parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(action, param1, param2, param3, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with three parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3>(Func<T1, T2, T3, Task> func, T1 param1, T2 param2, T3 param3, TaskCreationOptions options = default, ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry((p1, p2, p3) => func(p1, p2, p3).Wait(), param1, param2, param3, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with three parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3>(Action<T1, T2, T3> action,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry(action, param1, param2, param3, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with three parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3>(Func<T1, T2, T3, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry((p1, p2, p3) => func(p1, p2, p3).Wait(), param1, param2, param3, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3, T4>
	/// <summary>
	/// Creates an internal task entry for an action with four parameters.
	/// </summary>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default,
		Delegate? @delegate = null)
	{
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3, p4) = (ValueTuple<T1, T2, T3, T4>)o;
			action.Invoke(p1, p2, p3, p4);
		}, (param1, param2, param3, param4), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with four parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(action, param1, param2, param3, param4, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with four parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry((p1, p2, p3, p4) => func(p1, p2, p3, p4).Wait(), param1, param2, param3, param4, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with four parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry(action, param1, param2, param3, param4, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with four parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry((p1, p2, p3, p4) => func(p1, p2, p3, p4).Wait(), param1, param2, param3, param4, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3, T4, T5>
	/// <summary>
	/// Creates an internal task entry for an action with five parameters.
	/// </summary>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action,
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
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3, p4, p5) = (ValueTuple<T1, T2, T3, T4, T5>)o;
			action.Invoke(p1, p2, p3, p4, p5);
		}, (param1, param2, param3, param4, param5), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with five parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(action, param1, param2, param3, param4, param5, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with five parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry((p1, p2, p3, p4, p5) => func(p1, p2, p3, p4, p5).Wait(), param1, param2, param3, param4, param5, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with five parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry(action, param1, param2, param3, param4, param5, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with five parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		TaskScheduler? scheduler = null,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default)
	{
		var task = CreateEntry((p1, p2, p3, p4, p5) => func(p1, p2, p3, p4, p5).Wait(), param1, param2, param3, param4, param5, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3, T4, T5, T6>
	/// <summary>
	/// Creates an internal task entry for an action with six parameters.
	/// </summary>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action,
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
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3, p4, p5, p6) = (ValueTuple<T1, T2, T3, T4, T5, T6>)o;
			action.Invoke(p1, p2, p3, p4, p5, p6);
		}, (param1, param2, param3, param4, param5, param6), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with six parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(action, param1, param2, param3, param4, param5, param6, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with six parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry((p1, p2, p3, p4, p5, p6) => func(p1, p2, p3, p4, p5, p6).Wait(), param1, param2, param3, param4, param5, param6, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with six parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action,
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
		var task = CreateEntry(action, param1, param2, param3, param4, param5, param6, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with six parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> func,
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
		var task = CreateEntry((p1, p2, p3, p4, p5, p6) => func(p1, p2, p3, p4, p5, p6).Wait(), param1, param2, param3, param4, param5, param6, scheduler, options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3, T4, T5, T6, T7>
	/// <summary>
	/// Creates an internal task entry for an action with seven parameters.
	/// </summary>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action,
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
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3, p4, p5, p6, p7) = (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)o;
			action.Invoke(p1, p2, p3, p4, p5, p6, p7);
		}, (param1, param2, param3, param4, param5, param6, param7), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with seven parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry(action, param1, param2, param3, param4, param5, param6, param7, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with seven parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> func,
		T1 param1,
		T2 param2,
		T3 param3,
		T4 param4,
		T5 param5,
		T6 param6,
		T7 param7,
		TaskCreationOptions options = default,
		ConcurrencyProfile concurrencyProfile = default) =>
		CreateEntry((p1, p2, p3, p4, p5, p6, p7) => func(p1, p2, p3, p4, p5, p6, p7).Wait(), param1, param2, param3, param4, param5, param6, param7, null, options, concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with seven parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action,
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
		var task = CreateEntry(action, param1, param2, param3, param4, param5, param6, param7, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with seven parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> func,
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
		var task = CreateEntry((p1, p2, p3, p4, p5, p6, p7) => func(p1, p2, p3, p4, p5, p6, p7).Wait(), param1, param2, param3, param4, param5, param6, param7, scheduler, options, concurrencyProfile,
			func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3, T4, T5, T6, T7, T8>
	/// <summary>
	/// Creates an internal task entry for an action with eight parameters.
	/// </summary>
	/// <remarks>
	/// Note: Uses nested <see cref="ValueTuple"/> (ValueTuple&lt;T1-T7, ValueTuple&lt;T8&gt;&gt;) to support 8 parameters.
	/// </remarks>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action,
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
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3, p4, p5, p6, p7, p8) = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>)o;
			action.Invoke(p1, p2, p3, p4, p5, p6, p7, p8);
		}, (param1, param2, param3, param4, param5, param6, param7, param8), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with eight parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action,
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
		CreateEntry(action, param1, param2, param3, param4, param5, param6, param7, param8, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with eight parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> func,
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
		CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8) => func(p1, p2, p3, p4, p5, p6, p7, p8).Wait(), param1, param2, param3, param4, param5, param6, param7, param8, null, options, concurrencyProfile,
			func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with eight parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action,
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
		var task = CreateEntry(action, param1, param2, param3, param4, param5, param6, param7, param8, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with eight parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> func,
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
		var task = CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8) => func(p1, p2, p3, p4, p5, p6, p7, p8).Wait(), param1, param2, param3, param4, param5, param6, param7, param8, scheduler, options,
			concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion

	#region Void<T1,T2, T3, T4, T5, T6, T7, T8, T9>
	/// <summary>
	/// Creates an internal task entry for an action with nine parameters.
	/// </summary>
	/// <remarks>
	/// Note: Uses nested <see cref="ValueTuple"/> (ValueTuple&lt;T1-T7, ValueTuple&lt;T8, T9&gt;&gt;) to support 9 parameters.
	/// </remarks>
	static ActionTaskManagerEntry CreateEntry<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action,
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
		var entry = new ActionTaskManagerEntry(o => {
			var (p1, p2, p3, p4, p5, p6, p7, p8, p9) = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>)o;
			action.Invoke(p1, p2, p3, p4, p5, p6, p7, p8, p9);
		}, (param1, param2, param3, param4, param5, param6, param7, param8, param9), scheduler, options, concurrencyProfile, @delegate);
		AddEntry(entry);
		return entry;
	}
	
	/// <summary>
	/// Creates a new task that executes an action with nine parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action,
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
		CreateEntry(action, param1, param2, param3, param4, param5, param6, param7, param8, param9, null, options, concurrencyProfile).Task;
	
	/// <summary>
	/// Creates a new task that executes an asynchronous function with nine parameters, without starting it.
	/// </summary>
	public static Task Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Task> func,
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
		CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8, p9) => func(p1, p2, p3, p4, p5, p6, p7, p8, p9).Wait(), param1, param2, param3, param4, param5, param6, param7, param8, param9, null, options,
			concurrencyProfile, func).Task;
	
	/// <summary>
	/// Creates and starts a new task that executes an action with nine parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action,
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
		var task = CreateEntry(action, param1, param2, param3, param4, param5, param6, param7, param8, param9, scheduler, options, concurrencyProfile).Task;
		SearchEntry(task).Start();
		return task;
	}
	
	/// <summary>
	/// Creates and starts a new task that executes an asynchronous function with nine parameters.
	/// </summary>
	public static Task StartNew<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Task> func,
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
		var task = CreateEntry((p1, p2, p3, p4, p5, p6, p7, p8, p9) => func(p1, p2, p3, p4, p5, p6, p7, p8, p9).Wait(), param1, param2, param3, param4, param5, param6, param7, param8, param9, scheduler,
			options, concurrencyProfile, func).Task;
		SearchEntry(task).Start();
		return task;
	}
	#endregion
}