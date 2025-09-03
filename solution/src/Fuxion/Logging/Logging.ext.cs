using System;
using Microsoft.Extensions.Logging;

namespace Fuxion.Logging;

public static class Logging
{
	/// <summary>
	///     Logs an informational message if the logger is enabled for the <see cref="LogLevel.Information" /> level.
	/// </summary>
	/// <param name="me">The logger instance that will log the message. Can be <c>null</c>.</param>
	/// <param name="func">
	///     A function that generates the message to log. The function is only invoked if logging is enabled
	///     for the <see cref="LogLevel.Information" /> level.
	/// </param>
	public static void LogInformation(this ILogger? me, Func<string> func)
	{
		if (me?.IsEnabled(LogLevel.Information) ?? false)
			me.LogInformation(func());
	}
	/// <summary>
	///     Logs a critical message if the logger is enabled for the <see cref="LogLevel.Critical" /> level.
	/// </summary>
	/// <param name="me">The logger instance that will log the message. Can be <c>null</c>.</param>
	/// <param name="func">
	///     A function that generates the message to log. The function is only invoked if logging is enabled
	///     for the <see cref="LogLevel.Critical" /> level.
	/// </param>
	public static void LogCritical(this ILogger? me, Func<string> func)
	{
		if (me?.IsEnabled(LogLevel.Critical) ?? false)
			me.LogCritical(func());
	}
	/// <summary>
	///     Logs a warning message if the logger is enabled for the <see cref="LogLevel.Warning" /> level.
	/// </summary>
	/// <param name="me">The logger instance that will log the message. Can be <c>null</c>.</param>
	/// <param name="func">
	///     A function that generates the message to log. The function is only invoked if logging is enabled
	///     for the <see cref="LogLevel.Warning" /> level.
	/// </param>
	public static void LogWarning(this ILogger? me, Func<string> func)
	{
		if (me?.IsEnabled(LogLevel.Warning) ?? false) me.LogWarning(func());
	}
	/// <summary>
	///     Logs a debug message if the logger is enabled for the <see cref="LogLevel.Debug" /> level.
	/// </summary>
	/// <param name="me">The logger instance that will log the message. Can be <c>null</c>.</param>
	/// <param name="func">
	///     A function that generates the message to log. The function is only invoked if logging is enabled
	///     for the <see cref="LogLevel.Debug" /> level.
	/// </param>
	public static void LogDebug(this ILogger? me, Func<string> func)
	{
		if (me?.IsEnabled(LogLevel.Debug) ?? false) me.LogDebug(func());
	}
	/// <summary>
	///     Logs an error message if the logger is enabled for the <see cref="LogLevel.Error" /> level.
	/// </summary>
	/// <param name="me">The logger instance that will log the message. Can be <c>null</c>.</param>
	/// <param name="func">
	///     A function that generates the message to log. The function is only invoked if logging is enabled
	///     for the <see cref="LogLevel.Error" /> level.
	/// </param>
	public static void LogError(this ILogger? me, Func<string> func)
	{
		if (me?.IsEnabled(LogLevel.Error) ?? false) me.LogError(func());
	}
	/// <summary>
	///     Logs a trace message if the logger is enabled for the <see cref="LogLevel.Trace" /> level.
	/// </summary>
	/// <param name="me">The logger instance that will log the message. Can be <c>null</c>.</param>
	/// <param name="func">
	///     A function that generates the message to log. The function is only invoked if logging is enabled
	///     for the <see cref="LogLevel.Trace" /> level.
	/// </param>
	public static void LogTrace(this ILogger? me, Func<string> func)
	{
		if (me?.IsEnabled(LogLevel.Trace) ?? false) me.LogTrace(func());
	}
}