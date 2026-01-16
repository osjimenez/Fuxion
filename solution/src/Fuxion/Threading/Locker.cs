using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fuxion.Threading;

/// <summary>
/// Provides thread-safe access to an object using reader-writer lock semantics with support for both synchronous and asynchronous operations.
/// </summary>
/// <typeparam name="TObjectLocked">The type of object to protect with locking.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Locker{TObjectLocked}"/> wraps <see cref="ReaderWriterLockSlim"/> to provide a cleaner API for
/// protecting shared resources with read/write locks. It supports:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Read locks:</strong> Allow multiple concurrent readers</description></item>
/// <item><description><strong>Write locks:</strong> Provide exclusive access for modifications</description></item>
/// <item><description><strong>Upgradeable read locks:</strong> Can upgrade to write lock without releasing the read lock</description></item>
/// <item><description><strong>Synchronous operations:</strong> Execute actions/functions synchronously within locks</description></item>
/// <item><description><strong>Asynchronous operations:</strong> Execute operations asynchronously using <see cref="TaskManager"/></description></item>
/// <item><description><strong>Exception logging:</strong> Optional logging integration via <see cref="ILogger"/></description></item>
/// <item><description><strong>Lock recursion:</strong> Configurable via <see cref="LockRecursionPolicy"/> (default: supports recursion)</description></item>
/// </list>
/// <para>
/// <strong>Lock types:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Read:</strong> Multiple threads can hold read locks simultaneously for read-only operations</description></item>
/// <item><description><strong>ReadUpgradeable:</strong> One thread can hold an upgradeable read lock, which can be elevated to a write lock</description></item>
/// <item><description><strong>Write:</strong> Only one thread can hold a write lock for exclusive modification access</description></item>
/// </list>
/// <para>
/// <strong>Thread-safety guarantees:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Read operations are safe for concurrent access</description></item>
/// <item><description>Write operations have exclusive access</description></item>
/// <item><description>All operations properly acquire/release locks even on exception</description></item>
/// <item><description>Exceptions are logged (if logger configured) and re-thrown</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Creating a thread-safe list
/// var safeList = new Locker&lt;List&lt;string&gt;&gt;(new List&lt;string&gt;());
/// 
/// // Synchronous read operation
/// safeList.Read(list =&gt; 
/// {
///     Console.WriteLine($"Count: {list.Count}");
///     foreach (var item in list)
///         Console.WriteLine(item);
/// });
/// 
/// // Synchronous read with return value
/// var count = safeList.Read(list =&gt; list.Count);
/// 
/// // Synchronous write operation
/// safeList.Write(list =&gt; 
/// {
///     list.Add("New Item");
///     list.Sort();
/// });
/// 
/// // Asynchronous operations
/// await safeList.ReadAsync(list =&gt; Console.WriteLine($"Count: {list.Count}"));
/// await safeList.WriteAsync(list =&gt; list.Add("Async Item"));
/// 
/// // With logging
/// var logger = loggerFactory.CreateLogger&lt;Locker&lt;List&lt;string&gt;&gt;&gt;();
/// safeList.Logger = logger;
/// 
/// // Upgradeable read lock (can upgrade to write lock)
/// safeList.ReadUpgradeable(list =&gt; 
/// {
///     if (list.Count &gt; 100)
///     {
///         // Upgrade to write lock to clear
///         safeList.Write(l =&gt; l.Clear());
///     }
/// });
/// 
/// // Replace entire object
/// safeList.WriteObject(new List&lt;string&gt; { "Item1", "Item2" });
/// 
/// // Don't forget to dispose
/// safeList.Dispose();
/// </code>
/// </example>
public class Locker<TObjectLocked>(TObjectLocked objectLocked, LockRecursionPolicy recursionPolicy = LockRecursionPolicy.SupportsRecursion) : IDisposable
{
	readonly ReaderWriterLockSlim readerWriterLockSlim = new(recursionPolicy);
	
	/// <summary>
	/// Gets or sets the logger used for logging exceptions that occur during lock operations.
	/// </summary>
	/// <value>An <see cref="ILogger"/> instance, or <c>null</c> if logging is not configured.</value>
	/// <remarks>
	/// When set, all exceptions caught during Read/Write operations are logged before being re-thrown.
	/// This is useful for diagnostics and debugging thread-safety issues.
	/// </remarks>
	public ILogger? Logger { get; set; }
	
	/// <summary>
	/// Disposes the underlying <see cref="ReaderWriterLockSlim"/> and releases all resources.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Important:</strong> Call this method when the <see cref="Locker{TObjectLocked}"/> is no longer needed.
	/// Failing to dispose can lead to resource leaks.
	/// </para>
	/// <para>
	/// After disposal, any attempt to use the locker will result in <see cref="ObjectDisposedException"/>.
	/// </para>
	/// </remarks>
	public void Dispose() => readerWriterLockSlim.Dispose();
	
	/// <summary>
	/// Executes an action within a read lock, allowing multiple concurrent readers.
	/// </summary>
	/// <param name="action">The action to execute with the locked object.</param>
	/// <remarks>
	/// <para>
	/// Multiple threads can execute read operations concurrently. Use this for read-only operations
	/// that don't modify the protected object.
	/// </para>
	/// <para>
	/// The lock is automatically released when the action completes, even if an exception is thrown.
	/// Exceptions are logged (if <see cref="Logger"/> is set) and re-thrown.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var safeDict = new Locker&lt;Dictionary&lt;string, int&gt;&gt;(new Dictionary&lt;string, int&gt;());
	/// 
	/// // Read operation - multiple threads can do this simultaneously
	/// safeDict.Read(dict =&gt; 
	/// {
	///     if (dict.TryGetValue("key", out var value))
	///         Console.WriteLine($"Value: {value}");
	/// });
	/// </code>
	/// </example>
	public void Read(Action<TObjectLocked> action)
	{
		readerWriterLockSlim.EnterReadLock();
		try
		{
			action.Invoke(objectLocked);
		} catch (Exception ex)
		{
			Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
			throw;
		} finally
		{
			readerWriterLockSlim.ExitReadLock();
		}
	}
	
	/// <summary>
	/// Executes an action within an upgradeable read lock, which can later be upgraded to a write lock.
	/// </summary>
	/// <param name="action">The action to execute with the locked object.</param>
	/// <remarks>
	/// <para>
	/// An upgradeable read lock allows one thread to hold a read lock that can be upgraded to a write lock
	/// without releasing the lock. This is useful for scenarios where you need to read data and conditionally
	/// modify it based on the read values.
	/// </para>
	/// <para>
	/// <strong>Important:</strong> Only one thread can hold an upgradeable read lock at a time, but other threads
	/// can hold regular read locks simultaneously.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var safeCache = new Locker&lt;Dictionary&lt;string, object&gt;&gt;(new Dictionary&lt;string, object&gt;());
	/// 
	/// safeCache.ReadUpgradeable(cache =&gt; 
	/// {
	///     if (!cache.ContainsKey("key"))
	///     {
	///         // Upgrade to write lock to add item
	///         safeCache.Write(c =&gt; c["key"] = ComputeValue());
	///     }
	/// });
	/// </code>
	/// </example>
	public void ReadUpgradeable(Action<TObjectLocked> action)
	{
		readerWriterLockSlim.EnterUpgradeableReadLock();
		try
		{
			action.Invoke(objectLocked);
		} catch (Exception ex)
		{
			Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
			throw;
		} finally
		{
			readerWriterLockSlim.ExitUpgradeableReadLock();
		}
	}
	
	/// <summary>
	/// Executes a function within a read lock and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="func">The function to execute with the locked object.</param>
	/// <returns>The result returned by the function.</returns>
	/// <remarks>
	/// Multiple threads can execute read operations concurrently. The lock is held only for the duration
	/// of the function execution.
	/// </remarks>
	/// <example>
	/// <code>
	/// var safeList = new Locker&lt;List&lt;string&gt;&gt;(new List&lt;string&gt;());
	/// 
	/// // Get count safely
	/// int count = safeList.Read(list =&gt; list.Count);
	/// 
	/// // Get first item or default
	/// string? first = safeList.Read(list =&gt; list.FirstOrDefault());
	/// </code>
	/// </example>
	public TResult Read<TResult>(Func<TObjectLocked, TResult> func)
	{
		readerWriterLockSlim.EnterReadLock();
		try
		{
			var res = func.Invoke(objectLocked);
			return res;
		} catch (Exception ex)
		{
			Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
			throw;
		} finally
		{
			readerWriterLockSlim.ExitReadLock();
		}
	}
	
	/// <summary>
	/// Executes a function within an upgradeable read lock and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="func">The function to execute with the locked object.</param>
	/// <returns>The result returned by the function.</returns>
	public TResult ReadUpgradeable<TResult>(Func<TObjectLocked, TResult> func)
	{
		readerWriterLockSlim.EnterUpgradeableReadLock();
		try
		{
			var res = func.Invoke(objectLocked);
			return res;
		} catch (Exception ex)
		{
			Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
			throw;
		} finally
		{
			readerWriterLockSlim.ExitUpgradeableReadLock();
		}
	}
	
	/// <summary>
	/// Executes an action within a write lock, providing exclusive access to the protected object.
	/// </summary>
	/// <param name="action">The action to execute with exclusive access to the locked object.</param>
	/// <remarks>
	/// <para>
	/// Write locks provide exclusive access - no other thread can hold a read or write lock while
	/// a write lock is active. Use this for operations that modify the protected object.
	/// </para>
	/// <para>
	/// The lock is automatically released when the action completes, even if an exception is thrown.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var safeList = new Locker&lt;List&lt;int&gt;&gt;(new List&lt;int&gt;());
	/// 
	/// // Exclusive write operation
	/// safeList.Write(list =&gt; 
	/// {
	///     list.Add(42);
	///     list.Sort();
	///     list.RemoveAt(0);
	/// });
	/// </code>
	/// </example>
	public void Write(Action<TObjectLocked> action)
	{
		readerWriterLockSlim.EnterWriteLock();
		try
		{
			action.Invoke(objectLocked);
		} catch (Exception ex)
		{
			Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Write: {ex.Message}");
			throw;
		} finally
		{
			readerWriterLockSlim.ExitWriteLock();
		}
	}
	
	/// <summary>
	/// Executes a function within a write lock and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="func">The function to execute with exclusive access to the locked object.</param>
	/// <returns>The result returned by the function.</returns>
	/// <example>
	/// <code>
	/// var safeDict = new Locker&lt;Dictionary&lt;string, int&gt;&gt;(new Dictionary&lt;string, int&gt;());
	/// 
	/// // Add and return previous count
	/// int previousCount = safeDict.Write(dict =&gt; 
	/// {
	///     var count = dict.Count;
	///     dict["newKey"] = 42;
	///     return count;
	/// });
	/// </code>
	/// </example>
	public TResult Write<TResult>(Func<TObjectLocked, TResult> func)
	{
		readerWriterLockSlim.EnterWriteLock();
		try
		{
			var res = func.Invoke(objectLocked);
			return res;
		} catch (Exception ex)
		{
			Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Write: {ex.Message}");
			throw;
		} finally
		{
			readerWriterLockSlim.ExitWriteLock();
		}
	}
	
	/// <summary>
	/// Replaces the entire protected object with a new value within a write lock.
	/// </summary>
	/// <param name="value">The new object value to set.</param>
	/// <remarks>
	/// <para>
	/// This method acquires a write lock and replaces the entire object reference.
	/// Use this when you need to swap out the entire protected object.
	/// </para>
	/// <para>
	/// <strong>Note:</strong> This operation is atomic from the perspective of other threads accessing
	/// the locker, but doesn't dispose or cleanup the old object.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var safeList = new Locker&lt;List&lt;string&gt;&gt;(new List&lt;string&gt;());
	/// 
	/// // Replace with completely new list
	/// safeList.WriteObject(new List&lt;string&gt; { "A", "B", "C" });
	/// </code>
	/// </example>
	public void WriteObject(TObjectLocked value)
	{
		readerWriterLockSlim.EnterWriteLock();
		objectLocked = value;
		readerWriterLockSlim.ExitWriteLock();
	}

	#region Async delegates
	/// <summary>
	/// Internal helper for executing delegates asynchronously within a read lock.
	/// </summary>
	Task DelegateReadAsync(Delegate del, params object?[] pars) =>
		TaskManager.StartNew((d, ps) => {
			readerWriterLockSlim.EnterReadLock();
			try
			{
				var p = new object?[] {
					objectLocked
				}.ToList();
				p.AddRange(ps);
				d.DynamicInvoke(p.ToArray());
			} catch (Exception ex)
			{
				Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
				throw;
			} finally
			{
				readerWriterLockSlim.ExitReadLock();
			}
		}, del, pars);
	
	/// <summary>
	/// Internal helper for executing delegates asynchronously within a read lock and returning a non-null result.
	/// </summary>
	Task<TResult> DelegateReadAsync<TResult>(Delegate del, params object?[] pars) where TResult : notnull =>
		TaskManager.StartNew<Delegate, object?[], TResult>((d, ps) => {
			readerWriterLockSlim.EnterReadLock();
			try
			{
				var p = new object?[] {
					objectLocked
				}.ToList();
				p.AddRange(ps);
				var res = d.DynamicInvoke(p.ToArray());
				if (res == null) throw new ArgumentNullException("Dynamic invocation cannot return null");
				return (TResult)res;
			} catch (Exception ex)
			{
				Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
				throw;
			} finally
			{
				readerWriterLockSlim.ExitReadLock();
			}
		}, del, pars);
	
	/// <summary>
	/// Internal helper for executing delegates asynchronously within a read lock and returning a nullable result.
	/// </summary>
	Task<TResult?> DelegateReadNullableAsync<TResult>(Delegate del, params object?[] pars) =>
		TaskManager.StartNew<Delegate, object?[], TResult?>((d, ps) => {
			readerWriterLockSlim.EnterReadLock();
			try
			{
				var p = new object?[] {
					objectLocked
				}.ToList();
				p.AddRange(ps);
				var res = d.DynamicInvoke(p.ToArray());
				return (TResult?)res;
			} catch (Exception ex)
			{
				Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Read: {ex.Message}");
				throw;
			} finally
			{
				readerWriterLockSlim.ExitReadLock();
			}
		}, del, pars);
	
	/// <summary>
	/// Internal helper for executing delegates asynchronously within a write lock.
	/// </summary>
	Task DelegateWriteAsync(Delegate del, params object?[] pars) =>
		TaskManager.StartNew((d, ps) => {
			readerWriterLockSlim.EnterWriteLock();
			try
			{
				var p = new object?[] {
					objectLocked
				}.ToList();
				p.AddRange(ps);
				d.DynamicInvoke(p.ToArray());
			} catch (Exception ex)
			{
				Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Write: {ex.Message}");
				throw;
			} finally
			{
				readerWriterLockSlim.ExitWriteLock();
			}
		}, del, pars);
	
	/// <summary>
	/// Internal helper for executing delegates asynchronously within a write lock and returning a result.
	/// </summary>
	Task<TResult> DelegateWriteAsync<TResult>(Delegate del, params object?[] pars) =>
		TaskManager.StartNew((d, ps) => {
			readerWriterLockSlim.EnterWriteLock();
			try
			{
				var p = new object?[] {
					objectLocked
				}.ToList();
				p.AddRange(ps);
				var res = d.DynamicInvoke(p.ToArray());
				if (res == null) throw new ArgumentNullException("Dynamic invocation cannot return null");
				return (TResult)res;
			} catch (Exception ex)
			{
				Logger?.LogError(ex, $"Error '{ex.GetType().Name}' in Locker.Write: {ex.Message}");
				throw;
			} finally
			{
				readerWriterLockSlim.ExitWriteLock();
			}
		}, del, pars);
	#endregion

	#region Async methods
	/// <summary>
	/// Asynchronously executes an action within a read lock.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// The action is executed on a thread pool thread via <see cref="TaskManager"/>.StartNew
	/// The read lock is acquired on the worker thread.
	/// </remarks>
	/// <example>
	/// <code>
	/// await safeList.ReadAsync(list =&gt; Console.WriteLine($"Count: {list.Count}"));
	/// </code>
	/// </example>
	public Task ReadAsync(Action<TObjectLocked> action) => DelegateReadAsync(action);
	
	/// <summary>
	/// Asynchronously executes an action with one parameter within a read lock.
	/// </summary>
	public Task ReadAsync<T>(Action<TObjectLocked, T> action, T param) => DelegateReadAsync(action, param);
	
	/// <summary>
	/// Asynchronously executes an action with two parameters within a read lock.
	/// </summary>
	public Task ReadAsync<T1, T2>(Action<TObjectLocked, T1, T2> action, T1 param1, T2 param2) => DelegateReadAsync(action, param1, param2);
	
	/// <summary>
	/// Asynchronously executes an action with three parameters within a read lock.
	/// </summary>
	public Task ReadAsync<T1, T2, T3>(Action<TObjectLocked, T1, T2, T3> action, T1 param1, T2 param2, T3 param3) => DelegateReadAsync(action, param1, param2, param3);
	
	/// <summary>
	/// Asynchronously executes a function within a read lock and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result (must be non-null).</typeparam>
	/// <param name="func">The function to execute.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	/// <example>
	/// <code>
	/// int count = await safeList.ReadAsync(list =&gt; list.Count);
	/// </code>
	/// </example>
	public Task<TResult> ReadAsync<TResult>(Func<TObjectLocked, TResult> func) where TResult : notnull => DelegateReadAsync<TResult>(func);
	
	/// <summary>
	/// Asynchronously executes a function with one parameter within a read lock and returns its result.
	/// </summary>
	public Task<TResult> ReadAsync<T, TResult>(Func<TObjectLocked, T, TResult> func, T param) where TResult : notnull => DelegateReadAsync<TResult>(func, param);
	
	/// <summary>
	/// Asynchronously executes a function with two parameters within a read lock and returns its result.
	/// </summary>
	public Task<TResult> ReadAsync<T1, T2, TResult>(Func<TObjectLocked, T1, T2, TResult> func, T1 param1, T2 param2) where TResult : notnull => DelegateReadAsync<TResult>(func, param1, param2);
	
	/// <summary>
	/// Asynchronously executes a function with three parameters within a read lock and returns its result.
	/// </summary>
	public Task<TResult> ReadAsync<T1, T2, T3, TResult>(Func<TObjectLocked, T1, T2, T3, TResult> func, T1 param1, T2 param2, T3 param3) where TResult : notnull =>
		DelegateReadAsync<TResult>(func, param1, param2, param3);
	
	/// <summary>
	/// Asynchronously executes a function within a read lock and returns a nullable result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result (can be null).</typeparam>
	/// <param name="func">The function to execute.</param>
	/// <returns>A task representing the asynchronous operation with the nullable result.</returns>
	public Task<TResult?> ReadNullableAsync<TResult>(Func<TObjectLocked, TResult> func) => DelegateReadNullableAsync<TResult>(func);
	
	/// <summary>
	/// Asynchronously executes a function with one parameter within a read lock and returns a nullable result.
	/// </summary>
	public Task<TResult?> ReadNullableAsync<T, TResult>(Func<TObjectLocked, T, TResult> func, T param) => DelegateReadNullableAsync<TResult>(func, param);
	
	/// <summary>
	/// Asynchronously executes a function with two parameters within a read lock and returns a nullable result.
	/// </summary>
	public Task<TResult?> ReadNullableAsync<T1, T2, TResult>(Func<TObjectLocked, T1, T2, TResult> func, T1 param1, T2 param2) => DelegateReadNullableAsync<TResult>(func, param1, param2);
	
	/// <summary>
	/// Asynchronously executes a function with three parameters within a read lock and returns a nullable result.
	/// </summary>
	public Task<TResult?> ReadNullableAsync<T1, T2, T3, TResult>(Func<TObjectLocked, T1, T2, T3, TResult> func, T1 param1, T2 param2, T3 param3) =>
		DelegateReadNullableAsync<TResult>(func, param1, param2, param3);

	/// <summary>
	/// Asynchronously executes an action within a write lock.
	/// </summary>
	/// <param name="action">The action to execute with exclusive access.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <example>
	/// <code>
	/// await safeList.WriteAsync(list =&gt; list.Add("New Item"));
	/// </code>
	/// </example>
	public Task WriteAsync(Action<TObjectLocked> action) => DelegateWriteAsync(action);
	
	/// <summary>
	/// Asynchronously executes an async function within a write lock.
	/// </summary>
	public Task WriteAsync(Func<TObjectLocked, Task> action) => DelegateWriteAsync(action);
	
	/// <summary>
	/// Asynchronously executes an action with one parameter within a write lock.
	/// </summary>
	public Task WriteAsync<T>(Action<TObjectLocked, T> action, T param) => DelegateWriteAsync(action, param);
	
	/// <summary>
	/// Asynchronously executes an async function with one parameter within a write lock.
	/// </summary>
	public Task WriteAsync<T>(Func<TObjectLocked, T, Task> action, T param) => DelegateWriteAsync(action, param);
	
	/// <summary>
	/// Asynchronously executes an action with two parameters within a write lock.
	/// </summary>
	public Task WriteAsync<T1, T2>(Action<TObjectLocked, T1, T2> action, T1 param1, T2 param2) => DelegateWriteAsync(action, param1, param2);
	
	/// <summary>
	/// Asynchronously executes an async function with two parameters within a write lock.
	/// </summary>
	public Task WriteAsync<T1, T2>(Func<TObjectLocked, T1, T2, Task> action, T1 param1, T2 param2) => DelegateWriteAsync(action, param1, param2);
	
	/// <summary>
	/// Asynchronously executes an action with three parameters within a write lock.
	/// </summary>
	public Task WriteAsync<T1, T2, T3>(Action<TObjectLocked, T1, T2, T3> action, T1 param1, T2 param2, T3 param3) => DelegateWriteAsync(action, param1, param2, param3);
	
	/// <summary>
	/// Asynchronously executes an async function with three parameters within a write lock.
	/// </summary>
	public Task WriteAsync<T1, T2, T3>(Func<TObjectLocked, T1, T2, T3, Task> action, T1 param1, T2 param2, T3 param3) => DelegateWriteAsync(action, param1, param2, param3);

	/// <summary>
	/// Asynchronously executes a function within a write lock and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="func">The function to execute with exclusive access.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	/// <example>
	/// <code>
	/// int newCount = await safeList.WriteAsync(list =&gt; 
	/// {
	///     list.Add("Item");
	///     return list.Count;
	/// });
	/// </code>
	/// </example>
	public Task<TResult> WriteAsync<TResult>(Func<TObjectLocked, TResult> func) => DelegateWriteAsync<TResult>(func);
	
	/// <summary>
	/// Asynchronously executes an async function within a write lock and returns its result.
	/// </summary>
	public Task<TResult> WriteAsync<TResult>(Func<TObjectLocked, Task<TResult>> func) => DelegateWriteAsync<TResult>(func);
	
	/// <summary>
	/// Asynchronously executes an action with one parameter within a write lock and returns a result.
	/// </summary>
	public Task<TResult> WriteAsync<T, TResult>(Action<TObjectLocked, T, TResult> func, T param) => DelegateWriteAsync<TResult>(func, param);
	
	/// <summary>
	/// Asynchronously executes an async action with one parameter within a write lock and returns a result.
	/// </summary>
	public Task<TResult> WriteAsync<T, TResult>(Action<TObjectLocked, T, Task<TResult>> func, T param) => DelegateWriteAsync<TResult>(func, param);
	
	/// <summary>
	/// Asynchronously executes an action with two parameters within a write lock and returns a result.
	/// </summary>
	public Task<TResult> WriteAsync<T1, T2, TResult>(Action<TObjectLocked, T1, T2, TResult> func, T1 param1, T2 param2) => DelegateWriteAsync<TResult>(func, param1, param2);
	
	/// <summary>
	/// Asynchronously executes an async action with two parameters within a write lock and returns a result.
	/// </summary>
	public Task<TResult> WriteAsync<T1, T2, TResult>(Action<TObjectLocked, T1, T2, Task<TResult>> func, T1 param1, T2 param2) => DelegateWriteAsync<TResult>(func, param1, param2);
	
	/// <summary>
	/// Asynchronously executes an action with three parameters within a write lock and returns a result.
	/// </summary>
	public Task<TResult> WriteAsync<T1, T2, T3, TResult>(Action<TObjectLocked, T1, T2, T3, TResult> func, T1 param1, T2 param2, T3 param3) => DelegateWriteAsync<TResult>(func, param1, param2, param3);
	
	/// <summary>
	/// Asynchronously executes an async action with three parameters within a write lock and returns a result.
	/// </summary>
	public Task<TResult> WriteAsync<T1, T2, T3, TResult>(Action<TObjectLocked, T1, T2, T3, Task<TResult>> func, T1 param1, T2 param2, T3 param3) =>
		DelegateWriteAsync<TResult>(func, param1, param2, param3);
	#endregion
}