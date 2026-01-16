using System;
using System.Threading.Tasks;

namespace Fuxion;

/// <summary>
/// Provides a disposable wrapper around an object that executes custom logic when disposed.
/// Supports both synchronous and asynchronous disposal patterns.
/// </summary>
/// <typeparam name="T">The type of the wrapped value. Must be a non-nullable type.</typeparam>
/// <remarks>
/// <para>
/// This class is useful when you need to wrap an object with automatic cleanup logic that should execute
/// when the wrapper is disposed. It implements both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>
/// to support different disposal scenarios.
/// </para>
/// <para>
/// Common use cases include:
/// </para>
/// <list type="bullet">
/// <item><description>Automatic resource cleanup when exiting a scope</description></item>
/// <item><description>Logging or tracking when objects are disposed</description></item>
/// <item><description>Executing custom teardown logic without modifying the wrapped type</description></item>
/// <item><description>Managing temporary state or connections</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Simple synchronous cleanup
/// using (var envelope = new DisposableEnvelope&lt;DatabaseConnection&gt;(
///     connection,
///     conn => conn.Close()))
/// {
///     envelope.Value.ExecuteQuery("SELECT * FROM Users");
/// } // Connection automatically closed here
/// 
/// // Async cleanup with logging
/// await using (var envelope = new DisposableEnvelope&lt;HttpClient&gt;(
///     client,
///     async c => 
///     {
///         await c.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/logout"));
///         Console.WriteLine("Client logged out");
///     }))
/// {
///     var response = await envelope.Value.GetAsync("/api/data");
/// } // Logout request sent automatically
/// 
/// // Without cleanup action (just disposal tracking)
/// using var envelope = new DisposableEnvelope&lt;Resource&gt;(myResource);
/// envelope.Value.DoWork();
/// 
/// // Using implicit conversion
/// DisposableEnvelope&lt;string&gt; envelope = new("Hello", s => Console.WriteLine($"Disposing: {s}"));
/// string value = envelope; // Implicit conversion to T
/// Console.WriteLine(value); // "Hello"
/// </code>
/// </example>
public class DisposableEnvelope<T> : IDisposable, IAsyncDisposable where T : notnull
{
	/// <summary>
	/// Gets a pre-disposed instance that can be used as a sentinel value.
	/// </summary>
	/// <remarks>
	/// This property uses a field-backed property pattern (C# 13) to cache a single disposed instance.
	/// The instance has no value and is already marked as disposed, making it safe to use as a default or null-object pattern.
	/// </remarks>
	public static DisposableEnvelope<T> Disposed
	{
		get
		{
			if (field is not null) return field;
			field = new(true);
			return field;
		}
	}

	/// <summary>
	/// Initializes a new disposed instance (private constructor for the Disposed property).
	/// </summary>
	/// <param name="disposed">Always true for this constructor.</param>
	private DisposableEnvelope(bool disposed)
	{
		_disposed = disposed;
		Value = default!;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="DisposableEnvelope{T}"/> class with a synchronous disposal action.
	/// </summary>
	/// <param name="obj">The object to wrap.</param>
	/// <param name="actionOnDispose">Optional action to execute when the envelope is disposed. The action receives the wrapped value as a parameter.</param>
	/// <example>
	/// <code>
	/// var envelope = new DisposableEnvelope&lt;FileStream&gt;(
	///     fileStream,
	///     fs => Console.WriteLine($"Closing file: {fs.Name}"));
	/// </code>
	/// </example>
	public DisposableEnvelope(T obj, Action<T>? actionOnDispose = null)
	{
		Action = actionOnDispose;
		Value = obj;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="DisposableEnvelope{T}"/> class with an asynchronous disposal function.
	/// </summary>
	/// <param name="obj">The object to wrap.</param>
	/// <param name="functionOnDispose">Optional async function to execute when the envelope is disposed. The function receives the wrapped value as a parameter.</param>
	/// <example>
	/// <code>
	/// var envelope = new DisposableEnvelope&lt;DbContext&gt;(
	///     context,
	///     async ctx => await ctx.SaveChangesAsync());
	/// </code>
	/// </example>
	public DisposableEnvelope(T obj, Func<T, ValueTask>? functionOnDispose = null)
	{
		Function = functionOnDispose;
		Value = obj;
	}

	private bool _disposed;

	/// <summary>
	/// Gets or sets the wrapped value.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown when attempting to set the value after the envelope has been disposed.</exception>
	/// <remarks>
	/// The getter always succeeds, but the setter will throw if the envelope has been disposed.
	/// Uses the field-backed property pattern (C# 13) for automatic backing field management.
	/// </remarks>
	public T Value
	{
		get;
		set
		{
			if (_disposed) throw new ObjectDisposedException(nameof(Value));
			field = value;
		}
	}

	/// <summary>
	/// Gets or sets the synchronous action to execute on disposal.
	/// </summary>
	protected Action<T>? Action { get; set; }
	
	/// <summary>
	/// Gets or sets the asynchronous function to execute on disposal.
	/// </summary>
	protected Func<T, ValueTask>? Function { get; set; }

	/// <summary>
	/// Disposes the envelope synchronously, executing any registered disposal actions or functions.
	/// </summary>
	void IDisposable.Dispose() => OnDispose();
	
	/// <summary>
	/// Disposes the envelope asynchronously, executing any registered disposal actions or functions.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
	ValueTask IAsyncDisposable.DisposeAsync() => OnDisposeAsync();
	
	/// <summary>
	/// Executes the synchronous disposal logic. Can be overridden in derived classes.
	/// </summary>
	/// <remarks>
	/// <para>The disposal process:</para>
	/// <list type="number">
	/// <item><description>Marks the envelope as disposed</description></item>
	/// <item><description>Executes the synchronous action if present</description></item>
	/// <item><description>Executes the async function synchronously (blocking) if present</description></item>
	/// </list>
	/// <para>
	/// Note: If an async function is registered, it will be executed synchronously using <c>.Wait()</c>.
	/// For proper async disposal, use <c>await using</c> instead of <c>using</c>.
	/// </para>
	/// </remarks>
	protected virtual void OnDispose()
	{
		_disposed = true;
		Action?.Invoke(Value);
		Function?.Invoke(Value).AsTask().Wait();
	}
	
	/// <summary>
	/// Executes the asynchronous disposal logic. Can be overridden in derived classes.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
	/// <remarks>
	/// <para>The disposal process:</para>
	/// <list type="number">
	/// <item><description>Marks the envelope as disposed</description></item>
	/// <item><description>Executes the synchronous action if present</description></item>
	/// <item><description>Awaits the async function if present</description></item>
	/// </list>
	/// </remarks>
	protected virtual async ValueTask OnDisposeAsync()
	{
		_disposed = true;
		Action?.Invoke(Value);
		if (Function is not null)
			await Function.Invoke(Value);
	}

	/// <summary>
	/// Implicitly converts a <see cref="DisposableEnvelope{T}"/> to its wrapped value.
	/// </summary>
	/// <param name="dis">The envelope to unwrap.</param>
	/// <returns>The wrapped value.</returns>
	/// <example>
	/// <code>
	/// DisposableEnvelope&lt;int&gt; envelope = new(42);
	/// int value = envelope; // Implicit conversion
	/// Console.WriteLine(value); // 42
	/// </code>
	/// </example>
	public static implicit operator T(DisposableEnvelope<T> dis) => dis.Value;
}