﻿namespace Fuxion.Threading.Tasks;

public static class Extensions
{
	public static void Cancel(this Task task, bool throwExceptionIfNotRunning = true)
	{
		var entry = TaskManager.SearchEntry(task, throwExceptionIfNotRunning);
		entry?.Cancel();
	}
	public static void CancelAndWait(this Task task, TimeSpan timeout = default, bool throwExceptionIfNotRunning = true) =>
		new[] {
			task
		}.CancelAndWait(timeout, throwExceptionIfNotRunning);
	public static async Task CancelAndWaitAsync(this Task task, bool throwExceptionIfNotRunning = true) =>
		await new[] {
			task
		}.CancelAndWaitAsync(throwExceptionIfNotRunning);
	public static void CancelAndWait(this IEnumerable<Task> me, TimeSpan timeout = default, bool throwExceptionIfNotRunning = true)
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
	public static async Task CancelAndWaitAsync(this IEnumerable<Task> me, bool throwExceptionIfNotRunning = true)
	{
		foreach (var task in me) task.Cancel(throwExceptionIfNotRunning);
		try
		{
			await Task.WhenAll(me);
		}
		// If task was cancelled, nothing happens
		catch (Exception ex) when (ex is TaskCanceledException || ex is AggregateException aex && aex.Flatten().InnerException is TaskCanceledException) { }
	}
	public static void OnCancelRequested(this Task task, Action action) => TaskManager.SearchEntry(task).CancelRequested += (s, e) => action();
	public static Task OnCancel(this Task task, Action action) => task.ContinueWith(t => action(), TaskContinuationOptions.OnlyOnCanceled);
	public static Task OnSuccess(this Task task, Action action) => task.ContinueWith(t => action(), TaskContinuationOptions.OnlyOnRanToCompletion);
	public static Task OnFaulted(this Task task, Action<AggregateException?> action) => task.ContinueWith(t => action(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
	public static bool IsCancellationRequested(this Task task, bool throwExceptionIfNotRunning = false)
	{
		var entry = TaskManager.SearchEntry(task, throwExceptionIfNotRunning);
		if (entry != null) return entry.IsCancellationRequested;
		if (throwExceptionIfNotRunning) throw new ArgumentException("IsCancellationRequested: La tarea no esta administrada por el TaskManager." + task.CreationOptions);
		return false;
	}
	public static CancellationToken GetCancellationToken(this Task task, bool throwExceptionIfNotRunning = false)
	{
		var entry = TaskManager.SearchEntry(task, throwExceptionIfNotRunning);
		return entry?.CancellationTokenSource.Token ?? CancellationToken.None;
	}
	public static bool Sleep(this Task task, TimeSpan timeout, bool rethrowException = false)
	{
		try
		{
			// Share the token with Delay method to break the operation if task will canceled
			Task.Delay(timeout, task.GetCancellationToken(true)).Wait();
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