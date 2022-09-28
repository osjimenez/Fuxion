﻿namespace Fuxion.Testing;

using Xunit.Abstractions;

public class XunitLogger : ILogger
{
	public XunitLogger(ITestOutputHelper output) => this.output = output;

	private readonly ITestOutputHelper output;

	public IDisposable BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();
	public bool IsEnabled(LogLevel logLevel) => true;
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		=> output.WriteLine(state?.ToString());
}