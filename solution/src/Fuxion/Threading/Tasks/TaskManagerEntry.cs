using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fuxion.Threading.Tasks;

internal abstract class TaskManagerEntry(
	Delegate? @delegate,
	TaskScheduler? scheduler,
	TaskCreationOptions options,
	ConcurrencyProfile concurrencyProfile)
	: ITaskManagerEntry
{
	private ITaskManagerEntry? _next;
	private ITaskManagerEntry? _previous;
	public ILogger? Logger { get; set; }
	public ITaskManagerEntry? Previous
	{
		get => _previous;
		set
		{
			_previous = value;
			if (value != null) ((TaskManagerEntry)value)._next = this;
		}
	}
	public ITaskManagerEntry? Next
	{
		get => _next;
		set
		{
			_next = value;
			if (value != null) ((TaskManagerEntry)value)._previous = this;
		}
	}
	public ConcurrencyProfile ConcurrencyProfile { get; set; } = concurrencyProfile;

	public Task Task
	{
		get => field ?? throw new InvalidProgramException();
		set
		{
			field = value;
			field.ContinueWith(t => {
				var toLog = "La tarea finalizó con " + t.Exception?.InnerExceptions.Count + " errores.";
				Exception? ex = t.Exception;
				if (t.Exception?.InnerExceptions.Count == 1)
				{
					ex = t.Exception.InnerExceptions[0];
					toLog += " Error '" + ex.GetType().Name + "': " + ex.Message;
				}
				if (ex is TaskCanceledByConcurrencyException tccex)
					Logger?.LogDebug(tccex, toLog);
				else
					Logger?.LogError(ex, toLog);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}
	}
	public TaskScheduler TaskScheduler { get; } = scheduler ?? TaskScheduler.Default;
	public TaskCreationOptions TaskCreationOptions { get; } = options;
	public void Start() => Task.Start(TaskScheduler);
	public event EventHandler? CancelRequested;
	public bool IsCancellationRequested => CancellationTokenSource.IsCancellationRequested;
	public CancellationTokenSource CancellationTokenSource { get; } = new();
	public Delegate? Delegate { get; } = @delegate;

	public void Cancel()
	{
		CancellationTokenSource.Cancel();
		CancelRequested?.Invoke(this, EventArgs.Empty);
	}
	public void DoConcurrency()
	{
		var allPrevious = TaskManager.Tasks.Read(l => l.Take(l.IndexOf(this)).Where(e => string.IsNullOrWhiteSpace(ConcurrencyProfile.Name)
			//? e.Delegate.Method == Delegate.Method && e.Delegate.Target.GetType() == Delegate.Target.GetType()
			? ConcurrencyProfile.ByInstance
				? e.Delegate?.Method == Delegate?.Method && e.Delegate?.Target == Delegate?.Target
				: e.Delegate?.Method == Delegate?.Method && e.Delegate?.Target?.GetType() == Delegate?.Target?.GetType()
			: e.ConcurrencyProfile.Name == ConcurrencyProfile.Name).ToList());
		Previous = allPrevious.LastOrDefault();
		if (ConcurrencyProfile.CancelPrevious)
			foreach (var entry in allPrevious)
				entry.Cancel();
		if (ConcurrencyProfile.Sequentially)
			if (Previous != null)
				try
				{
					Previous.Task.Wait();
				}
				// If task was cancelled, nothing happens
				catch (Exception ex) when (ex is TaskCanceledException || ex is AggregateException aex && aex.Flatten().InnerException is TaskCanceledException)
				{
					Debug.WriteLine("Previous entry was canceled");
				}
		if (ConcurrencyProfile.ExecuteOnlyLast)
			if (Next != null)
				throw new TaskCanceledByConcurrencyException();
	}
}