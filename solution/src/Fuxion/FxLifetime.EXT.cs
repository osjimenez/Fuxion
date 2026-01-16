using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fuxion;

/// <summary>
///    Provides extension methods for lifetime management of objects, enabling wrapping values
///    in disposable envelopes with custom cleanup logic.
/// </summary>
/// <remarks>
///    This class uses the extension mechanism to add lifetime management functionality to any type
///    through the Fuxion extensions framework. It allows objects that don't implement <see cref="IDisposable" />
///    to have disposal behavior by wrapping them in a <see cref="DisposableEnvelope{T}" />.
/// </remarks>
public static class LifetimeExtensions
{
	extension<T>(FuxionExtensions<T?> me)
	{
		/// <summary>
		///    Provides lifetime management extension operations for this value.
		/// </summary>
		public LifetimeExtensions<T?> Lifetime
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension<T>(LifetimeExtensions<T?> me) where T : notnull
	{
		/// <summary>
		///    Wraps the underlying value in a <see cref="DisposableEnvelope{T}" /> that executes a synchronous action when
		///    disposed.
		/// </summary>
		/// <param name="actionOnDispose">
		///    An optional <see cref="Action{T}" /> to execute when the envelope is disposed.
		///    The action receives the wrapped value as its parameter.
		///    When <c>null</c>, no action is executed on disposal.
		/// </param>
		/// <returns>
		///    A <see cref="DisposableEnvelope{T}" /> wrapping the underlying value.
		///    When the underlying value is <c>null</c>, returns <see cref="DisposableEnvelope{T}.Disposed" />,
		///    a pre-disposed singleton instance.
		/// </returns>
		/// <remarks>
		///    This method is useful for managing the lifetime of objects that don't implement <see cref="IDisposable" />
		///    but require cleanup logic when they go out of scope. The envelope implements <see cref="IDisposable" />
		///    and <see cref="IAsyncDisposable" />, allowing it to be used with <c>using</c> statements.
		///    <para>
		///       The <see cref="DisposableEnvelope{T}" /> provides an implicit conversion operator to <typeparamref name="T" />,
		///       allowing the envelope to be used directly as the wrapped value without explicitly accessing the <c>Value</c>
		///       property.
		///    </para>
		/// </remarks>
		/// <example>
		///    <code>
		/// var connection = CreateConnection();
		/// using var envelope = connection.Fx.Lifetime.AsDisposable(conn => conn.Close());
		/// 
		/// // Access wrapped value explicitly via .Value property
		/// envelope.Value.SendData("test");
		/// 
		/// // Or use implicit conversion (envelope is automatically converted to Connection)
		/// envelope.SendData("test");  // Calls SendData directly on the wrapped connection
		/// 
		/// // Connection will be closed automatically when the using block exits
		/// 
		/// // Example with null value
		/// Connection? nullConnection = null;
		/// using var nullEnvelope = nullConnection.Fx.Lifetime.AsDisposable(conn => conn.Close());
		/// // Returns DisposableEnvelope&lt;Connection&gt;.Disposed (already disposed, no action executed)
		/// </code>
		/// </example>
		public DisposableEnvelope<T> AsDisposable(Action<T>? actionOnDispose = null)
		{
			return me.Value is null ? DisposableEnvelope<T>.Disposed : new(me.Value, actionOnDispose);
		}

		/// <summary>
		///    Wraps the underlying value in a <see cref="DisposableEnvelope{T}" /> that executes an asynchronous function when
		///    disposed.
		/// </summary>
		/// <param name="functionOnDispose">
		///    A <see cref="Func{T, TResult}" /> that returns a <see cref="ValueTask" /> to execute when the envelope is disposed.
		///    The function receives the wrapped value as its parameter.
		/// </param>
		/// <returns>
		///    A <see cref="DisposableEnvelope{T}" /> wrapping the underlying value.
		///    When the underlying value is <c>null</c>, returns <see cref="DisposableEnvelope{T}.Disposed" />,
		///    a pre-disposed singleton instance.
		/// </returns>
		/// <remarks>
		///    This method is useful for managing the lifetime of objects that require asynchronous cleanup logic.
		///    The envelope implements <see cref="IAsyncDisposable" />, allowing it to be used with <c>await using</c> statements.
		///    If disposed synchronously (via <see cref="IDisposable.Dispose" />), the asynchronous function is executed
		///    synchronously by blocking with <c>.Wait()</c>.
		///    <para>
		///       The <see cref="DisposableEnvelope{T}" /> provides an implicit conversion operator to <typeparamref name="T" />,
		///       allowing the envelope to be used directly as the wrapped value without explicitly accessing the <c>Value</c>
		///       property.
		///    </para>
		/// </remarks>
		/// <example>
		///    <code>
		/// var stream = OpenStreamAsync();
		/// await using var envelope = stream.Fx.Lifetime.AsDisposableAsync(async s => await s.FlushAsync());
		/// 
		/// // Access wrapped value explicitly via .Value property
		/// await envelope.Value.WriteAsync(data);
		/// 
		/// // Or use implicit conversion (envelope is automatically converted to Stream)
		/// await envelope.WriteAsync(data);  // Calls WriteAsync directly on the wrapped stream
		/// 
		/// // Stream will be flushed asynchronously when the await using block exits
		/// 
		/// // Example with database connection
		/// var dbConnection = await GetDatabaseConnectionAsync();
		/// await using var dbEnvelope = dbConnection.Fx.Lifetime.AsDisposableAsync(async conn => 
		/// {
		///     await conn.CommitTransactionAsync();
		///     await conn.CloseAsync();
		/// });
		/// 
		/// // Use implicit conversion for cleaner code
		/// var result = await dbEnvelope.ExecuteQueryAsync("SELECT * FROM Users");
		/// 
		/// // Database transaction committed and connection closed asynchronously on disposal
		/// 
		/// // Example with null value
		/// Stream? nullStream = null;
		/// await using var nullEnvelope = nullStream.Fx.Lifetime.AsDisposableAsync(async s => await s.FlushAsync());
		/// // Returns DisposableEnvelope&lt;Stream&gt;.Disposed (already disposed, no function executed)
		/// </code>
		/// </example>
		public DisposableEnvelope<T> AsDisposableAsync(Func<T, ValueTask> functionOnDispose)
		{
			return me.Value is null ? DisposableEnvelope<T>.Disposed : new(me.Value, functionOnDispose);
		}
	}
}

/// <summary>
///    Generic wrapper class that provides lifetime management extension methods for values of type
///    <typeparamref name="T" />.
///    This class is used internally by the Fuxion extensions framework to enable fluent API syntax for lifetime
///    operations.
/// </summary>
/// <typeparam name="T">The type of the value being wrapped.</typeparam>
/// <remarks>
///    This class inherits from <see cref="Extensions{T}" /> and serves as a specialized container for lifetime-related
///    operations.
///    Users typically access lifetime management functionality through extension methods rather than instantiating this
///    class directly.
///    The primary operations provided are wrapping values in disposable envelopes with custom cleanup logic.
/// </remarks>
/// <example>
///    <code>
/// // Accessed through extension syntax
/// var resource = GetResource();
/// 
/// // Synchronous disposal
/// using var envelope = resource.Fx.Lifetime.AsDisposable(r => r.Close());
/// envelope.DoWork();
/// 
/// // Asynchronous disposal
/// await using var asyncEnvelope = resource.Fx.Lifetime.AsDisposableAsync(async r => await r.CloseAsync());
/// await asyncEnvelope.DoWorkAsync();
/// </code>
/// </example>
public class LifetimeExtensions<T>(T me) : Extensions<T>(me);