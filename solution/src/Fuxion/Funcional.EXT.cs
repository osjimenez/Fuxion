using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Fuxion;

/// <summary>
/// Provides functional programming extensions for C# objects, including side-effect operations (Tap)
/// and transformation operations (Map) that enable fluent, pipeline-style code similar to F# or LINQ.
/// </summary>
/// <remarks>
/// These extensions follow established functional programming patterns:
/// <list type="bullet">
/// <item><b>Tap</b>: Executes a side-effect and returns the original value unchanged (like Ruby's tap, RxJS's tap)</item>
/// <item><b>Map</b>: Transforms a value into a new value (like LINQ's Select, but for individual objects)</item>
/// <item><b>Do</b>: Executes a side-effect on each element of a collection (lazy evaluation)</item>
/// <item><b>ForEach</b>: Executes a side-effect on each element of a collection (eager evaluation)</item>
/// </list>
/// <para>
/// <b>Naming Convention:</b> Methods with async actions/functions have the <c>Async</c> suffix
/// for consistency and clarity (e.g., <c>ThenTapAsync</c>, <c>ThenMapAsync</c>, <c>DoAsync</c>).
/// </para>
/// </remarks>
public static class FunctionalExtensions
{
	#region Tap - Side-effect Operations
	extension<T>(T me)
	{
		/// <summary>
		/// Executes an action on the object and returns it unchanged (fluent API).
		/// </summary>
		/// <param name="action">The action to execute on the object.</param>
		/// <returns>The original object, unchanged.</returns>
		/// <remarks>
		/// <para>
		/// This method is commonly known as "tap" in functional programming. It allows you to perform
		/// side-effects (like logging, debugging, or initialization) without breaking the fluent API flow.
		/// </para>
		/// <para>
		/// The name "tap" comes from the idea of "tapping into" a pipeline to observe or modify state
		/// without changing the flow of data through the pipeline.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Logging in pipelines without breaking the flow
		/// var result = GetData()
		///     .Tap(d => logger.LogInformation("Processing {Count} items", d.Count))
		///     .ProcessData()
		///     .Tap(r => logger.LogInformation("Result: {Result}", r));
		/// 
		/// // Fluent object initialization
		/// var client = new HttpClient()
		///     .Tap(c => c.Timeout = TimeSpan.FromSeconds(30))
		///     .Tap(c => c.DefaultRequestHeaders.Add("Authorization", token));
		/// 
		/// // Debugging without intermediate variables
		/// var transformed = GetComplexData()
		///     .Select(x => x.Value)
		///     .Tap(v => Console.WriteLine($"Debug: {v}"))  // Inspect here
		///     .Where(v => v > 0);
		/// </code>
		/// </example>
		public T Tap(Action<T> action)
		{
			action(me);
			return me;
		}
	}

	extension<T>(T? me)
	{
		/// <summary>
		/// Executes an action on the object if it is not null, and returns it unchanged.
		/// </summary>
		/// <param name="action">The action to execute if the object is not null.</param>
		/// <returns>The original object, unchanged (may be null).</returns>
		/// <remarks>
		/// This is a null-safe version of <see cref="Tap{TSource}(TSource, Action{TSource})"/>.
		/// The action is only executed if the value is not null.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Safe logging of potentially null values
		/// string? value = GetNullableString()
		///     .TapIfNotNull(v => logger.LogInformation("Got value: {Value}", v));
		/// 
		/// // Only execute initialization if object exists
		/// var config = FindConfig()
		///     .TapIfNotNull(c => c.Initialize())
		///     .TapIfNotNull(c => logger.LogInformation("Config initialized"));
		/// </code>
		/// </example>
		public T? TapIfNotNull(Action<T> action)
		{
			if (me is not null) action(me);
			return me;
		}
	}

	extension<T>(Task<T> me)
	{
		/// <summary>
		/// Executes a synchronous action on the result of a <see cref="Task{TSource}"/> when it completes,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The synchronous action to execute on the task result.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original value after executing the action.</returns>
		/// <example>
		/// <code>
		/// var result = await GetDataAsync()
		///     .ThenTap(d => logger.LogInformation("Received {Count} items", d.Count))
		///     .ThenMap(d => ProcessData(d));
		/// </code>
		/// </example>
		public async Task<T> ThenTap(
			Action<T> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			action(result);
			return result;
		}

		/// <summary>
		/// Executes an async action on the result of a <see cref="Task{TSource}"/> when it completes,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The async action to execute on the task result.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original value after executing the action.</returns>
		/// <example>
		/// <code>
		/// var result = await GetDataAsync()
		///     .ThenTapAsync(async (d, ct) => await LogToRemoteAsync(d, ct))
		///     .ThenMap(d => ProcessData(d));
		/// </code>
		/// </example>
		public async Task<T> ThenTapAsync(
			Func<T, CancellationToken, Task> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			await action(result, ct).ConfigureAwait(false);
			return result;
		}
	}

	extension<T>(Task<T?> me)
	{
		/// <summary>
		/// Executes a synchronous action on the result of a <see cref="Task{TSource}"/> if it is not null,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The synchronous action to execute if the result is not null.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original value (may be null) after executing the action.</returns>
		public async Task<T?> ThenTapIfNotNull(
			Action<T> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			if (result is not null) action(result);
			return result;
		}

		/// <summary>
		/// Executes an async action on the result of a <see cref="Task{TSource}"/> if it is not null,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The async action to execute if the result is not null.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original value (may be null) after executing the action.</returns>
		public async Task<T?> ThenTapIfNotNullAsync(
			Func<T, CancellationToken, Task> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			if (result is not null) await action(result, ct).ConfigureAwait(false);
			return result;
		}
	}

	extension<T>(ValueTask<T> me)
	{
		/// <summary>
		/// Executes a synchronous action on the result of a <see cref="ValueTask{TSource}"/> when it completes,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The synchronous action to execute on the result.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the original value after executing the action.</returns>
		public async ValueTask<T> ThenTap(
			Action<T> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			action(result);
			return result;
		}

		/// <summary>
		/// Executes an async action on the result of a <see cref="ValueTask{TSource}"/> when it completes,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The async action to execute on the result.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the original value after executing the action.</returns>
		public async ValueTask<T> ThenTapAsync(
			Func<T, CancellationToken, ValueTask> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			await action(result, ct).ConfigureAwait(false);
			return result;
		}
	}

	extension<T>(ValueTask<T?> me)
	{
		/// <summary>
		/// Executes a synchronous action on the result of a <see cref="ValueTask{TSource}"/> if it is not null,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The synchronous action to execute if the result is not null.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the original value (may be null) after executing the action.</returns>
		public async ValueTask<T?> ThenTapIfNotNull(
			Action<T> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			if (result is not null) action(result);
			return result;
		}

		/// <summary>
		/// Executes an async action on the result of a <see cref="ValueTask{TSource}"/> if it is not null,
		/// and returns the original value.
		/// </summary>
		/// <param name="action">The async action to execute if the result is not null.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the original value (may be null) after executing the action.</returns>
		public async ValueTask<T?> ThenTapIfNotNullAsync(
			Func<T, CancellationToken, ValueTask> action,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			if (result is not null) await action(result, ct).ConfigureAwait(false);
			return result;
		}
	}
	#endregion

	#region Map - Transformation Operations

	extension<T>(T me)
	{
		/// <summary>
		/// Applies a transformation function to the value and returns the result.
		/// </summary>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="func">The transformation function.</param>
		/// <returns>The transformed value.</returns>
		/// <remarks>
		/// <para>
		/// This method is similar to LINQ's <c>Select</c>, but for individual objects instead of collections.
		/// It enables functional programming-style pipelines in C#, similar to F#'s pipe operator (|>).
		/// </para>
		/// <para>
		/// While this is technically just a wrapper around calling the function directly, it enables
		/// a more readable, linear flow of transformations instead of nested function calls.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Linear pipeline (easy to read and debug)
		/// var result = GetPerson()
		///     .Map(p => p.Address)
		///     .Map(a => a.City)
		///     .Map(c => c.ToUpper());
		/// 
		/// // vs. Nested calls (harder to read)
		/// var result = GetPerson().Address.City.ToUpper();
		/// 
		/// // Pipeline with mixed operations
		/// var city = GetPerson()
		///     .Tap(p => logger.LogDebug("Got person: {Name}", p.Name))
		///     .Map(p => p.Address)
		///     .Tap(a => logger.LogDebug("Got address: {Street}", a.Street))
		///     .Map(a => a.City);
		/// </code>
		/// </example>
		public TResult Map<TResult>(Func<T, TResult> func) => func(me);
	}

	extension<T>(T? me)
	{
		/// <summary>
		/// Applies a transformation function to the value if it is not null, otherwise returns default.
		/// </summary>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="func">The transformation function.</param>
		/// <returns>The transformed value, or default if the source was null.</returns>
		/// <remarks>
		/// This is a null-safe version of <see cref="Map{TSource, TResult}(TSource, Func{TSource, TResult})"/>.
		/// It provides a more explicit alternative to chaining null-conditional operators (?.?.?).
		/// </remarks>
		/// <example>
		/// <code>
		/// // Explicit null-safe pipeline
		/// var city = GetPerson()
		///     .MapIfNotNull(p => p.Address)
		///     .MapIfNotNull(a => a.City)
		///     .MapIfNotNull(c => c.ToUpper());
		/// 
		/// // vs. Null-conditional chain (less explicit about the Map operation)
		/// var city = GetPerson()?.Address?.City?.ToUpper();
		/// </code>
		/// </example>
		public TResult? MapIfNotNull<TResult>(Func<T, TResult> func) => me is not null ? func(me) : default;
	}

	extension<T>(Task<T> me)
	{
		/// <summary>
		/// Applies a synchronous transformation function to the result of a <see cref="Task{TSource}"/> when it completes.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The synchronous transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the transformed value.</returns>
		/// <example>
		/// <code>
		/// var cityUpper = await GetPersonAsync()
		///     .ThenMap(p => p.Address)
		///     .ThenMap(a => a.City)
		///     .ThenMap(c => c.ToUpper());
		/// </code>
		/// </example>
		public async Task<TResult> ThenMap<TResult>(
			Func<T, TResult> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return func(result);
		}

		/// <summary>
		/// Applies an async transformation function to the result of a <see cref="Task{TSource}"/> when it completes.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The async transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the transformed value.</returns>
		/// <example>
		/// <code>
		/// var processed = await GetPersonAsync()
		///     .ThenMapAsync(async (p, ct) => await ProcessPersonAsync(p, ct))
		///     .ThenMapAsync(async (r, ct) => await SaveResultAsync(r, ct));
		/// </code>
		/// </example>
		public async Task<TResult> ThenMapAsync<TResult>(
			Func<T, CancellationToken, Task<TResult>> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return await func(result, ct).ConfigureAwait(false);
		}
	}

	extension<T>(ValueTask<T> me)
	{
		/// <summary>
		/// Applies a synchronous transformation function to the result of a <see cref="ValueTask{TSource}"/> when it completes.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The synchronous transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the transformed value.</returns>
		public async ValueTask<TResult> ThenMap<TResult>(
			Func<T, TResult> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return func(result);
		}

		/// <summary>
		/// Applies an async transformation function to the result of a <see cref="ValueTask{TSource}"/> when it completes.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The async transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the transformed value.</returns>
		public async ValueTask<TResult> ThenMapAsync<TResult>(
			Func<T, CancellationToken, ValueTask<TResult>> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return await func(result, ct).ConfigureAwait(false);
		}
	}

	extension<T>(Task<T?> me)
	{
		/// <summary>
		/// Applies a synchronous transformation function to the result of a <see cref="Task{TSource}"/> if it is not null.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The synchronous transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the transformed value, or default if the source was null.</returns>
		public async Task<TResult?> ThenMapIfNotNull<TResult>(
			Func<T, TResult> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return result is not null ? func(result) : default;
		}

		/// <summary>
		/// Applies an async transformation function to the result of a <see cref="Task{TSource}"/> if it is not null.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The async transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the transformed value, or default if the source was null.</returns>
		public async Task<TResult?> ThenMapIfNotNullAsync<TResult>(
			Func<T, CancellationToken, Task<TResult>> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return result is not null ? await func(result, ct).ConfigureAwait(false) : default;
		}
	}

	extension<T>(ValueTask<T?> me)
	{
		/// <summary>
		/// Applies a synchronous transformation function to the result of a <see cref="ValueTask{TSource}"/> if it is not null.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The synchronous transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the transformed value, or default if the source was null.</returns>
		public async ValueTask<TResult?> ThenMapIfNotNull<TResult>(
			Func<T, TResult> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return result is not null ? func(result) : default;
		}

		/// <summary>
		/// Applies an async transformation function to the result of a <see cref="ValueTask{TSource}"/> if it is not null.
		/// </summary>
		/// <typeparam name="TResult">The type of the transformation result.</typeparam>
		/// <param name="func">The async transformation function.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A value task that completes with the transformed value, or default if the source was null.</returns>
		public async ValueTask<TResult?> ThenMapIfNotNullAsync<TResult>(
			Func<T, CancellationToken, ValueTask<TResult>> func,
			CancellationToken ct = default)
		{
			var result = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			return result is not null ? await func(result, ct).ConfigureAwait(false) : default;
		}
	}
	#endregion

	#region Do - Collection Side-effects (LINQ-style lazy evaluation)

	extension<T>(IEnumerable<T> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of the sequence as it is enumerated, and yields the elements unchanged.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <returns>
		/// A sequence that yields each element after executing the action on it.
		/// The enumeration is lazy - the action is only executed when elements are actually enumerated.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method maintains lazy evaluation, similar to LINQ's Select or Where.
		/// The action is executed element-by-element as the sequence is consumed, not all at once.
		/// </para>
		/// <para>
		/// This is useful for logging, debugging, or performing side-effects in LINQ pipelines
		/// without breaking the lazy evaluation chain.
		/// </para>
		/// <para>
		/// <b>Comparison with other methods:</b>
		/// <list type="bullet">
		/// <item><c>ForEach</c> (List only): Executes immediately, returns void</item>
		/// <item><c>Do</c> (this method): Executes lazily, returns IEnumerable for chaining</item>
		/// <item><c>Tap</c>: For single objects, not collections</item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Lazy evaluation - only processes what's needed
		/// var result = Enumerable.Range(1, 1_000_000)
		///     .Where(x => x % 2 == 0)
		///     .Do(x => logger.LogDebug("Processing {Number}", x))  // Only logs what's consumed
		///     .Take(10)
		///     .ToList();  // Only 10 items are processed and logged
		/// 
		/// // Multiple Do calls for debugging pipeline stages
		/// var cities = GetPeople()
		///     .Do(p => logger.LogDebug("Got person: {Name}", p.Name))
		///     .Where(p => p.IsActive)
		///     .Do(p => logger.LogDebug("Active person: {Name}", p.Name))
		///     .Select(p => p.Address)
		///     .Do(a => logger.LogDebug("Address: {City}", a.City))
		///     .Select(a => a.City)
		///     .ToList();
		/// 
		/// // Unlike ToList() + ForEach(), this maintains the pipeline
		/// var query = GetData()
		///     .Do(x => Console.WriteLine($"Item: {x}"))  // Lazy, not executed yet
		///     .Where(x => x.IsValid);
		/// 
		/// // Action only executes when enumerated
		/// foreach (var item in query) { }  // NOW the Do action runs
		/// </code>
		/// </example>
		public IEnumerable<T> Do(Action<T> action)
		{
			foreach (var item in me)
			{
				action(item);
				yield return item;
			}
		}
#if !STANDARD_OR_OLD_FRAMEWORKS
		/// <summary>
		/// Executes an async action on each element of the sequence as it is enumerated, and yields the elements unchanged.
		/// Returns an <see cref="IAsyncEnumerable{T}"/> for async streaming scenarios.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// An async sequence that yields each element after executing the async action on it.
		/// The enumeration is lazy - the action is only executed when elements are actually enumerated.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method enables lazy async side-effects in LINQ pipelines. It maintains the benefits
		/// of async/await while preserving lazy evaluation through <see cref="IAsyncEnumerable{T}"/>.
		/// </para>
		/// <para>
		/// <b>⚠️ .NET Core 3.0+ / .NET 5+ only:</b> This method uses <see cref="IAsyncEnumerable{T}"/>
		/// which is not available in .NET Framework or .NET Standard 2.0.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Async logging in a lazy pipeline
		/// await foreach (var person in GetPeople()
		///     .DoAsync(async (p, ct) => await LogToRemoteAsync(p, ct)))
		/// {
		///     ProcessPerson(person);
		/// }
		/// </code>
		/// </example>
		public async IAsyncEnumerable<T> DoAsync(
			Func<T, CancellationToken, Task> action,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
		{
			foreach (var item in me)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
				yield return item;
			}
		}
#endif
	}

	extension<T>(Task<IEnumerable<T>> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of a <see cref="Task{IEnumerable}"/> result as it is enumerated.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// A task that completes with a sequence that yields each element after executing the action on it.
		/// The enumeration is lazy - the action is only executed when elements are actually enumerated.
		/// </returns>
		/// <example>
		/// <code>
		/// // Get data async, then process with sync logging
		/// var result = await GetPeopleAsync()
		///     .ThenDo(p => logger.Log(p.Name))
		///     .Select(p => p.Process())
		///     .ToListAsync();
		/// </code>
		/// </example>
		public async Task<IEnumerable<T>> ThenDo(Action<T> action, CancellationToken ct = default)
		{
			var enumerable = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();

			return DoCore(enumerable, action);

			static IEnumerable<T> DoCore(IEnumerable<T> source, Action<T> act)
			{
				foreach (var item in source)
				{
					act(item);
					yield return item;
				}
			}
		}
#if !STANDARD_OR_OLD_FRAMEWORKS
		/// <summary>
		/// Executes an async action on each element of a <see cref="Task{IEnumerable}"/> result as it is enumerated.
		/// Returns an <see cref="IAsyncEnumerable{T}"/> for async streaming scenarios.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// An async sequence that yields each element after executing the async action on it.
		/// </returns>
		/// <example>
		/// <code>
		/// // Get data async, then process with async logging
		/// await foreach (var person in GetPeopleAsync()
		///     .ThenDoAsync(async (p, ct) => await LogAsync(p, ct)))
		/// {
		///     ProcessPerson(person);
		/// }
		/// </code>
		/// </example>
		public async IAsyncEnumerable<T> ThenDoAsync(
			Func<T, CancellationToken, Task> action,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
		{
			var enumerable = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();

			foreach (var item in enumerable)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
				yield return item;
			}
		}
#endif
	}

	extension<T>(ValueTask<IEnumerable<T>> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of a <see cref="ValueTask{IEnumerable}"/> result as it is enumerated.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// A value task that completes with a sequence that yields each element after executing the action on it.
		/// The enumeration is lazy - the action is only executed when elements are actually enumerated.
		/// </returns>
		public async ValueTask<IEnumerable<T>> ThenDo(
			Action<T> action,
			CancellationToken ct = default)
		{
			var enumerable = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();

			return DoCore(enumerable, action);

			static IEnumerable<T> DoCore(IEnumerable<T> source, Action<T> act)
			{
				foreach (var item in source)
				{
					act(item);
					yield return item;
				}
			}
		}
#if !STANDARD_OR_OLD_FRAMEWORKS
		/// <summary>
		/// Executes an async action on each element of a <see cref="ValueTask{IEnumerable}"/> result as it is enumerated.
		/// Returns an <see cref="IAsyncEnumerable{T}"/> for async streaming scenarios.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// An async sequence that yields each element after executing the async action on it.
		/// </returns>
		public async IAsyncEnumerable<T> ThenDoAsync(
			Func<T, CancellationToken, ValueTask> action,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
		{
			var enumerable = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();

			foreach (var item in enumerable)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
				yield return item;
			}
		}
#endif
	}

	extension<T>(IAsyncEnumerable<T> me)
	{
#if !STANDARD_OR_OLD_FRAMEWORKS
		/// <summary>
		/// Executes an async action on each element of an async sequence as it is enumerated, and yields the elements unchanged.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// An async sequence that yields each element after executing the async action on it.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This overload works with <see cref="IAsyncEnumerable{T}"/> sources, allowing you to
		/// chain async side-effects in fully async streaming pipelines.
		/// </para>
		/// <para>
		/// This is ideal for scenarios where your data source is already streaming async
		/// (e.g., Entity Framework Core async queries, gRPC streams, SignalR hubs).
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Fully async pipeline from database
		/// await foreach (var person in dbContext.People
		///     .AsAsyncEnumerable()
		///     .DoAsync(async (p, ct) => await LogAsync(p, ct))
		///     .Where(p => p.IsActive)
		///     .DoAsync(async (p, ct) => await EnrichAsync(p, ct)))
		/// {
		///     // Process enriched person
		/// }
		/// 
		/// // gRPC streaming with logging
		/// await foreach (var message in grpcStream
		///     .DoAsync(async (m, ct) => await AuditAsync(m, ct))
		///     .Where(m => m.IsValid))
		/// {
		///     ProcessMessage(message);
		/// }
		/// </code>
		/// </example>
		public async IAsyncEnumerable<T> DoAsync(
			Func<T, CancellationToken, Task> action,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
		{
			await foreach (var item in me.WithCancellation(ct).ConfigureAwait(false))
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
				yield return item;
			}
		}
#endif
	}
	#endregion

	#region ForEach - Eager Collection Side-effects

	extension<T>(IEnumerable<T> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element immediately (eager evaluation) and returns a List.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <returns>A materialized List containing all elements after executing the action.</returns>
		/// <remarks>
		/// <para>
		/// Unlike <see cref="Do"/>, this executes immediately and returns a materialized List.
		/// Use when you need eager evaluation and a concrete collection.
		/// </para>
		/// <para>
		/// <b>⚠️ Note:</b> If the source is already a <see cref="List{T}"/>, it will be returned as-is
		/// to avoid unnecessary allocations. Otherwise, the sequence is materialized into a new List.
		/// </para>
		/// <para>
		/// <b>Comparison with other methods:</b>
		/// <list type="bullet">
		/// <item><c>List.ForEach</c>: Instance method on List, returns void</item>
		/// <item><c>Do</c>: Lazy evaluation, returns IEnumerable</item>
		/// <item><c>ForEach</c> (this method): Eager evaluation, returns List for chaining</item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var people = GetPeople()
		///     .Where(p => p.IsActive)
		///     .ForEach(p => p.Status = "Processed"); // Returns List, executes immediately
		/// 
		/// // Chaining is still possible
		/// var count = GetPeople()
		///     .ForEach(p => logger.Log(p.Name))
		///     .Count;
		/// </code>
		/// </example>
		public List<T> ForEach(Action<T> action)
		{
			var list = me as List<T> ?? me.ToList();
			foreach (var item in list)
			{
				action(item);
			}
			return list;
		}

		/// <summary>
		/// Executes an async action on each element immediately (eager evaluation) and returns a List.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with a materialized List containing all elements after executing the async action.</returns>
		/// <remarks>
		/// <para>
		/// This is the async version of <see cref="ForEach"/>. It executes immediately and returns a materialized List.
		/// Use when you need eager evaluation with async operations.
		/// </para>
		/// <para>
		/// <b>⚠️ Note:</b> The source sequence is materialized into a List before executing the actions.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var people = await GetPeople()
		///     .Where(p => p.IsActive)
		///     .ForEachAsync(async (p, ct) => await ProcessAsync(p, ct), ct); // Returns Task of List
		/// 
		/// // Chaining is still possible
		/// var count = (await GetPeople()
		///     .ForEachAsync(async (p, ct) => await EnrichAsync(p, ct), ct))
		///     .Count;
		/// </code>
		/// </example>
		public async Task<List<T>> ForEachAsync(Func<T, CancellationToken, Task> action, CancellationToken ct = default)
		{
			var list = me as List<T> ?? me.ToList();
			
			foreach (var item in list)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
			}
			
			return list;
		}
	}

	extension<T>(Task<List<T>> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of a <see cref="Task{List}"/> result immediately (eager evaluation).
		/// Returns the original list after all actions have been executed.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// A task that completes with the original list after executing the action on all elements.
		/// This is an eager operation - all actions execute immediately after awaiting.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This is specifically designed for Entity Framework Core scenarios where you need to:
		/// <list type="bullet">
		/// <item>Modify tracked entities immediately</item>
		/// <item>Ensure all side-effects complete before SaveChanges</item>
		/// <item>Maintain the same List reference for tracking</item>
		/// </list>
		/// </para>
		/// <para>
		/// <b>Comparison:</b>
		/// <list type="bullet">
		/// <item><c>ThenDo</c>: Lazy (IEnumerable), deferred execution</item>
		/// <item><c>ThenForEach</c>: Eager (List), immediate execution, same reference</item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // EF Core change tracking - immediate execution
		/// var contracts = await db.Contratos
		///     .Where(c => c.IdCliente == clientId)
		///     .ToListAsync(ct)
		///     .ThenForEach(c => c.IdClientePago = newPaymentId, ct)
		///     .ConfigureAwait(false);
		/// 
		/// await db.SaveChangesAsync(ct); // ✅ Changes are tracked immediately
		/// </code>
		/// </example>
		public async Task<List<T>> ThenForEach(Action<T> action, CancellationToken ct = default)
		{
			var list = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (var item in list)
			{
				action(item);
			}
			
			return list;
		}

		/// <summary>
		/// Executes an async action on each element of a <see cref="Task{List}"/> result immediately (eager evaluation).
		/// Returns the original list after all async actions have been executed.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// A task that completes with the original list after executing all async actions.
		/// </returns>
		/// <example>
		/// <code>
		/// var contracts = await db.Contratos
		///     .ToListAsync(ct)
		///     .ThenForEachAsync(async (c, ct) => await EnrichAsync(c, ct), ct)
		///     .ConfigureAwait(false);
		/// </code>
		/// </example>
		public async Task<List<T>> ThenForEachAsync(Func<T, CancellationToken, Task> action, CancellationToken ct = default)
		{
			var list = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (var item in list)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
			}
			
			return list;
		}
	}

	extension<T>(Task<T[]> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of a Task of array result immediately (eager evaluation).
		/// Returns the original array after all actions have been executed.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original array after executing the action on all elements.</returns>
		public async Task<T[]> ThenForEach(Action<T> action, CancellationToken ct = default)
		{
			var array = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (var item in array)
			{
				action(item);
			}
			
			return array;
		}

		/// <summary>
		/// Executes an async action on each element of a Task of array result immediately (eager evaluation).
		/// Returns the original array after all async actions have been executed.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original array after executing all async actions.</returns>
		public async Task<T[]> ThenForEachAsync(Func<T, CancellationToken, Task> action, CancellationToken ct = default)
		{
			var array = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (var item in array)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
			}
			
			return array;
		}
	}

	extension<T>(Task<ICollection<T>> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of a <see cref="Task{ICollection}"/> result immediately (eager evaluation).
		/// Returns the original collection after all actions have been executed.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original collection after executing the action on all elements.</returns>
		public async Task<ICollection<T>> ThenForEach(Action<T> action, CancellationToken ct = default)
		{
			var collection = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (var item in collection)
			{
				action(item);
			}
			
			return collection;
		}

		/// <summary>
		/// Executes an async action on each element of a <see cref="Task{ICollection}"/> result immediately (eager evaluation).
		/// Returns the original collection after all async actions have been executed.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with the original collection after executing all async actions.</returns>
		public async Task<ICollection<T>> ThenForEachAsync(Func<T, CancellationToken, Task> action, CancellationToken ct = default)
		{
			var collection = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (var item in collection)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
			}
			
			return collection;
		}
	}

	extension<T>(Task<IEnumerable<T>> me)
	{
		/// <summary>
		/// Executes a synchronous action on each element of a <see cref="Task{IEnumerable}"/> result immediately (eager evaluation).
		/// Returns a List containing all elements after executing the action.
		/// </summary>
		/// <param name="action">The synchronous action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>
		/// A task that completes with a List after executing the action on all elements.
		/// The sequence is materialized into a List for eager execution.
		/// </returns>
		/// <remarks>
		/// This overload materializes the IEnumerable into a List for eager execution.
		/// If you're working with a specific collection type (List, Array), use the specific overload for better performance.
		/// </remarks>
		public async Task<List<T>> ThenForEach(Action<T> action, CancellationToken ct = default)
		{
			var enumerable = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			var list = enumerable as List<T> ?? enumerable.ToList();
			foreach (var item in list)
			{
				action(item);
			}
			
			return list;
		}

		/// <summary>
		/// Executes an async action on each element of a <see cref="Task{IEnumerable}"/> result immediately (eager evaluation).
		/// Returns a List containing all elements after executing all async actions.
		/// </summary>
		/// <param name="action">The async action to execute on each element.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task that completes with a List after executing all async actions.</returns>
		public async Task<List<T>> ThenForEachAsync(Func<T, CancellationToken, Task> action, CancellationToken ct = default)
		{
			var enumerable = await me.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			var list = enumerable as List<T> ?? enumerable.ToList();
			foreach (var item in list)
			{
				ct.ThrowIfCancellationRequested();
				await action(item, ct).ConfigureAwait(false);
			}
			
			return list;
		}
	}

	#endregion
}
