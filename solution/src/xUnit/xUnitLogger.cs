using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Fuxion.Xunit;

// https://stackoverflow.com/questions/43680174/entity-framework-core-log-queries-for-a-single-db-context-instance

/// <summary>
///    Provides an <see cref="ILogger"/> implementation that writes log messages to xUnit test output.
/// </summary>
/// <remarks>
///    <para>
///       This logger implementation bridges Microsoft.Extensions.Logging with xUnit's test output system,
///       allowing log messages from services, Entity Framework, and other logging sources to appear in
///       xUnit test results. This is particularly useful for debugging tests and understanding what's
///       happening during test execution.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>xUnit integration:</strong> Writes all log messages to <see cref="ITestOutputHelper"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Log level formatting:</strong> Prefixes each line with the log level
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multiline support:</strong> Properly formats multiline log messages with consistent prefixes
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Exception support:</strong> Includes full exception details in test output
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>No filtering:</strong> All log levels are enabled by default (can be customized via <see cref="XunitLoggerProvider"/>)
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Debug Entity Framework Core SQL queries in tests</description>
///       </item>
///       <item>
///          <description>View application logging during integration tests</description>
///       </item>
///       <item>
///          <description>Troubleshoot service behavior in unit tests</description>
///       </item>
///       <item>
///          <description>Monitor dependency injection and service lifetime events</description>
///       </item>
///    </list>
///    <para>
///       <strong>Output format:</strong>
///    </para>
///    <para>
///       Log messages are formatted as: <c>{LogLevel}: {Message}</c>
///    </para>
///    <para>
///       Multiline messages preserve the log level prefix on each line, and exceptions are written
///       on separate lines with full stack traces.
///    </para>
/// </remarks>
/// <example>
///    <strong>Basic usage with BaseTest:</strong>
///    <code>
/// public class MyTests : BaseTest&lt;MyTests&gt;
/// {
///     public MyTests(ITestOutputHelper output) : base(output)
///     {
///         // XunitLogger is automatically configured
///     }
///     
///     [Fact]
///     public void Test_Logging()
///     {
///         Logger.LogInformation("Starting test");
///         Logger.LogDebug("Debug information");
///         Logger.LogWarning("Warning message");
///         
///         // Output in test results:
///         // Information: Starting test
///         // Debug: Debug information
///         // Warning: Warning message
///     }
/// }
/// </code>
///    <strong>Direct usage with ITestOutputHelper:</strong>
///    <code>
/// public class CustomTests
/// {
///     private readonly ILogger _logger;
///     
///     public CustomTests(ITestOutputHelper output)
///     {
///         _logger = new XunitLogger(output);
///     }
///     
///     [Fact]
///     public void Test_Something()
///     {
///         _logger.LogInformation("Test started");
///         // Perform test...
///         _logger.LogInformation("Test completed");
///     }
/// }
/// </code>
///    <strong>Entity Framework Core query logging:</strong>
///    <code>
/// public class DatabaseTests : BaseTest&lt;DatabaseTests&gt;
/// {
///     public DatabaseTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     protected override void OnConfigureServices(IServiceCollection serviceCollection)
///     {
///         serviceCollection.AddDbContext&lt;MyDbContext&gt;(options =&gt;
///         {
///             options.UseInMemoryDatabase("TestDb");
///             // EF Core logs will appear in test output
///             options.EnableSensitiveDataLogging();
///         });
///     }
///     
///     [Fact]
///     public async Task Test_Query()
///     {
///         await Scoped&lt;MyDbContext&gt;(async db =&gt;
///         {
///             var users = await db.Users.Where(u =&gt; u.IsActive).ToListAsync();
///             // Output will show the SQL query executed
///         });
///     }
/// }
/// </code>
///    <strong>With custom logging configuration:</strong>
///    <code>
/// public class FilteredLogTests : BaseTest&lt;FilteredLogTests&gt;
/// {
///     public FilteredLogTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     protected override void OnLoggingBuild(ILoggingBuilder loggingBuilder)
///     {
///         // Only log warnings and errors from Entity Framework
///         loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
///         
///         // Log everything from your application
///         loggingBuilder.AddFilter("MyApp", LogLevel.Debug);
///     }
/// }
/// </code>
///    <strong>Multiline and exception logging:</strong>
///    <code>
/// [Fact]
/// public void Test_MultilineAndExceptions()
/// {
///     // Multiline message
///     Logger.LogInformation($"Processing items:\n- Item 1\n- Item 2\n- Item 3");
///     // Output:
///     // Information: Processing items:
///     // Information: - Item 1
///     // Information: - Item 2
///     // Information: - Item 3
///     
///     try
///     {
///         throw new InvalidOperationException("Something went wrong");
///     }
///     catch (Exception ex)
///     {
///         Logger.LogError(ex, "An error occurred");
///         // Output:
///         // Error: An error occurred
///         // System.InvalidOperationException: Something went wrong
///         //    at ...stack trace...
///     }
/// }
/// </code>
///    <strong>Integration test logging:</strong>
///    <code>
/// public class ApiIntegrationTests : BaseTest&lt;ApiIntegrationTests&gt;
/// {
///     public ApiIntegrationTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     protected override void OnConfigureServices(IServiceCollection serviceCollection)
///     {
///         serviceCollection.AddHttpClient&lt;IMyApiClient, MyApiClient&gt;()
///             .AddHttpMessageHandler&lt;LoggingHandler&gt;();
///     }
///     
///     [Fact]
///     public async Task Test_ApiCall()
///     {
///         var client = ServiceProvider.GetRequiredService&lt;IMyApiClient&gt;();
///         
///         Logger.LogInformation("Making API call");
///         var result = await client.GetDataAsync();
///         // All HTTP request/response logs appear in test output
///         
///         IsTrue(result.IsSuccess);
///     }
/// }
/// </code>
/// </example>
public class XunitLogger : ILogger
{
	/// <summary>
	///    Initializes a new instance of the <see cref="XunitLogger"/> class.
	/// </summary>
	/// <param name="output">The xUnit test output helper to write log messages to.</param>
	/// <remarks>
	///    The logger will write all log messages to the provided <see cref="ITestOutputHelper"/>,
	///    making them visible in xUnit test results and output windows.
	/// </remarks>
	/// <example>
	///    <code>
	/// public class MyTests
	/// {
	///     private readonly ILogger _logger;
	///     
	///     public MyTests(ITestOutputHelper output)
	///     {
	///         _logger = new XunitLogger(output);
	///     }
	/// }
	/// </code>
	/// </example>
	public XunitLogger(ITestOutputHelper output) => Output = output;

	/// <summary>
	///    Gets the xUnit test output helper used for writing log messages.
	/// </summary>
	/// <value>
	///    The <see cref="ITestOutputHelper"/> instance provided in the constructor.
	/// </value>
	public ITestOutputHelper Output { get; }

	/// <summary>
	///    Writes a log entry to the xUnit test output.
	/// </summary>
	/// <typeparam name="TState">The type of the state object.</typeparam>
	/// <param name="logLevel">The log level for this entry.</param>
	/// <param name="eventId">The event identifier associated with the log entry.</param>
	/// <param name="state">The state object to log.</param>
	/// <param name="exception">The exception to log, or <c>null</c> if no exception.</param>
	/// <param name="formatter">Function to format the state and exception into a log message.</param>
	/// <exception cref="ArgumentNullException">
	///    Thrown when <paramref name="formatter"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	///    <para>
	///       The log entry is formatted with the following rules:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>If the log level is not enabled, the entry is ignored</description>
	///       </item>
	///       <item>
	///          <description>The formatter is used to create the message string</description>
	///       </item>
	///       <item>
	///          <description>Empty messages without exceptions are ignored</description>
	///       </item>
	///       <item>
	///          <description>Each line of the message is prefixed with the log level</description>
	///       </item>
	///       <item>
	///          <description>Exceptions are written with full stack traces on separate lines</description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Output format:</strong>
	///    </para>
	///    <para>
	///       Single line: <c>{LogLevel}: {Message}</c>
	///    </para>
	///    <para>
	///       Multiple lines: Each line is prefixed with <c>{LogLevel}: </c>
	///    </para>
	///    <para>
	///       With exception: Message line(s) followed by full exception.ToString()
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // This is typically called by the logging framework, not directly
	/// logger.Log(
	///     LogLevel.Information, 
	///     new EventId(1), 
	///     "User logged in", 
	///     null, 
	///     (state, ex) => state.ToString()
	/// );
	/// // Output: Information: User logged in
	/// 
	/// logger.Log(
	///     LogLevel.Error, 
	///     new EventId(2), 
	///     "Error processing request", 
	///     new InvalidOperationException("Bad state"), 
	///     (state, ex) => $"{state}: {ex?.Message}"
	/// );
	/// // Output: 
	/// // Error: Error processing request: Bad state
	/// // System.InvalidOperationException: Bad state
	/// //    at ...
	/// </code>
	/// </example>
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel)) return;
		if (formatter == null) throw new ArgumentNullException(nameof(formatter));
		var message = formatter(state, exception);
		if (string.IsNullOrEmpty(message) && exception == null) return;
		var line = $"{logLevel}: {message.Replace("\n", $"\n{logLevel}: ")}";
		Output.WriteLine(line);
		if (exception != null) Output.WriteLine(exception.ToString());
	}

	/// <summary>
	///    Checks if the given log level is enabled.
	/// </summary>
	/// <param name="logLevel">The log level to check.</param>
	/// <returns>
	///    Always returns <c>true</c>. All log levels are enabled by default.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This implementation always returns <c>true</c>, meaning all log levels are enabled.
	///       To filter log messages by level or category, use <see cref="ILoggingBuilder"/> configuration
	///       in <see cref="BaseTest{TBaseTest}.OnLoggingBuild"/> or when setting up the logger provider.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// if (logger.IsEnabled(LogLevel.Debug))
	/// {
	///     logger.LogDebug("Expensive debug operation result: {Result}", GetDebugInfo());
	/// }
	/// // Since this returns true, the LogDebug will always execute
	/// // To actually filter, configure logging in OnLoggingBuild:
	/// 
	/// protected override void OnLoggingBuild(ILoggingBuilder loggingBuilder)
	/// {
	///     loggingBuilder.SetMinimumLevel(LogLevel.Information);
	/// }
	/// </code>
	/// </example>
	public bool IsEnabled(LogLevel logLevel) => true;

	/// <summary>
	///    Begins a logical operation scope.
	/// </summary>
	/// <typeparam name="TState">The type of the state object.</typeparam>
	/// <param name="state">The state object for the scope.</param>
	/// <returns>
	///    An <see cref="IDisposable"/> that ends the logical operation scope when disposed.
	///    Always returns a new instance of <see cref="XunitScope"/>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This implementation returns a simple scope object that does nothing when disposed.
	///       Log scopes are typically used to add context to log messages, but this xUnit logger
	///       doesn't maintain scope state.
	///    </para>
	///    <para>
	///       For more advanced scope tracking in tests, consider wrapping this logger or using
	///       a different logging provider that supports scopes.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// using (logger.BeginScope("Processing order {OrderId}", orderId))
	/// {
	///     logger.LogInformation("Validating order");
	///     logger.LogInformation("Saving order");
	/// }
	/// // Note: The scope information is not included in the output
	/// // with this basic implementation
	/// </code>
	/// </example>
	public IDisposable BeginScope<TState>(TState state) where TState : notnull => new XunitScope();
}