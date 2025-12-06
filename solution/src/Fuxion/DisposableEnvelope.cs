using System;
using System.Threading.Tasks;

namespace Fuxion;

public class DisposableEnvelope<T> : IDisposable, IAsyncDisposable where T : notnull
{
	public static DisposableEnvelope<T> Disposed
	{
		get
		{
			if (field is not null) return field;
			field = new (true);
			return field;
		}
	}

	private DisposableEnvelope(bool disposed)
	{
		_disposed = disposed;
		Value = default!;
	}
	public DisposableEnvelope(T obj, Action<T>? actionOnDispose = null)
	{
		Action = actionOnDispose;
		Value = obj;
	}
	public DisposableEnvelope(T obj, Func<T, ValueTask>? functionOnDispose = null)
	{
		Function = functionOnDispose;
		Value = obj;
	}

	private bool _disposed;

	public T Value
	{
		get;
		set
		{
			if (_disposed) throw new ObjectDisposedException(nameof(Value));
			field = value;
		}
	}

	protected Action<T>? Action { get; set; }
	protected Func<T, ValueTask>? Function { get; set; }
	
	void IDisposable.Dispose() => OnDispose();
	ValueTask IAsyncDisposable.DisposeAsync() => OnDisposeAsync();
	protected virtual void OnDispose()
	{
		_disposed = true;
		Action?.Invoke(Value);
		Function?.Invoke(Value).AsTask().Wait();
	}
	protected virtual async ValueTask OnDisposeAsync()
	{
		_disposed = true;
		Action?.Invoke(Value);
		if(Function is not null)
			await Function.Invoke(Value);
	}

	public static implicit operator T(DisposableEnvelope<T> dis) => dis.Value;
}