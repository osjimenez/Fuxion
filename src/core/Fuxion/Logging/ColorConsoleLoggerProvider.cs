﻿namespace Fuxion.Logging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

public sealed class ColorConsoleLoggerProvider : ILoggerProvider
{
	private readonly IDisposable _onChangeToken;
	private ColorConsoleLoggerConfiguration _currentConfig;
	private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers = new();

	public ColorConsoleLoggerProvider(
		IOptionsMonitor<ColorConsoleLoggerConfiguration> config)
	{
		_currentConfig = config.CurrentValue;
		_onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
	}

	public ILogger CreateLogger(string categoryName) =>
		_loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, _currentConfig));

	public void Dispose()
	{
		_loggers.Clear();
		_onChangeToken.Dispose();
	}
}