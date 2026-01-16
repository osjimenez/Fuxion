using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
//using Xunit.Abstractions;

namespace Fuxion.Xunit;

/// <summary>
///    Provides a base class for xUnit test classes with integrated dependency injection, logging, and enhanced assertion methods.
/// </summary>
/// <typeparam name="TBaseTest">The type of the derived test class, used for typed logger creation.</typeparam>
/// <remarks>
///    <para>
///       This abstract base class simplifies xUnit test creation by providing built-in dependency injection,
///       logging infrastructure via <see cref="XunitLoggerProvider"/>, and enhanced assertion methods that
///       automatically output values to the test output. It follows the generic self-referencing pattern to
///       ensure the logger is typed for the actual test class.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Dependency injection:</strong> Built-in <see cref="IServiceProvider"/> configured per test instance
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Logging integration:</strong> Automatic setup of <see cref="ILogger{TCategoryName}"/> with xUnit output
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Enhanced assertions:</strong> Assert methods that automatically print values using CallerArgumentExpressionAttribute
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Pattern matching:</strong> String pattern matching with placeholder support for formatted strings
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Scoped execution:</strong> Helper method for executing code within DI scopes
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Extensibility:</strong> Virtual methods for customizing service and logging configuration
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Unit tests that require dependency injection</description>
///       </item>
///       <item>
///          <description>Integration tests with logging to test output</description>
///       </item>
///       <item>
///          <description>Tests that need to verify formatted string patterns</description>
///       </item>
///       <item>
///          <description>Tests requiring scoped service lifetimes</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic test class:</strong>
///    <code>
/// public class MyServiceTests : BaseTest&lt;MyServiceTests&gt;
/// {
///     public MyServiceTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     [Fact]
///     public void Test_ServiceWorks()
///     {
///         // Arrange
///         var service = ServiceProvider.GetRequiredService&lt;IMyService&gt;();
///         
///         // Act
///         var result = service.DoSomething();
///         
///         // Assert
///         IsTrue(result, "Service should return true");
///     }
/// }
/// </code>
///    <strong>With custom service configuration:</strong>
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
///             options.UseInMemoryDatabase("TestDb"));
///         serviceCollection.AddScoped&lt;IRepository, Repository&gt;();
///     }
///     
///     [Fact]
///     public async Task Test_DatabaseOperation()
///     {
///         await Scoped&lt;IRepository&gt;(async repo =&gt;
///         {
///             // Act
///             await repo.AddAsync(new Entity { Name = "Test" });
///             var count = await repo.CountAsync();
///             
///             // Assert
///             IsTrue(count == 1, "Should have one entity");
///         });
///     }
/// }
/// </code>
///    <strong>With custom logging configuration:</strong>
///    <code>
/// public class LoggingTests : BaseTest&lt;LoggingTests&gt;
/// {
///     public LoggingTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     protected override void OnLoggingBuild(ILoggingBuilder loggingBuilder)
///     {
///         loggingBuilder.SetMinimumLevel(LogLevel.Debug);
///         loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
///     }
///     
///     [Fact]
///     public void Test_Logging()
///     {
///         Logger.LogInformation("This will appear in test output");
///         IsTrue(true);
///     }
/// }
/// </code>
///    <strong>Exception testing:</strong>
///    <code>
/// public class ExceptionTests : BaseTest&lt;ExceptionTests&gt;
/// {
///     public ExceptionTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     [Fact]
///     public void Test_ThrowsException()
///     {
///         Throws&lt;InvalidOperationException&gt;(() =&gt;
///         {
///             throw new InvalidOperationException("Test exception");
///         });
///     }
///     
///     [Fact]
///     public async Task Test_ThrowsExceptionAsync()
///     {
///         await ThrowsAsync&lt;ArgumentNullException&gt;(async () =&gt;
///         {
///             await Task.Delay(10);
///             throw new ArgumentNullException("param");
///         });
///     }
/// }
/// </code>
///    <strong>Pattern matching:</strong>
///    <code>
/// public class StringTests : BaseTest&lt;StringTests&gt;
/// {
///     public StringTests(ITestOutputHelper output) : base(output)
///     {
///     }
///     
///     [Fact]
///     public void Test_FormattedString()
///     {
///         var message = string.Format("User {0} logged in at {1}", "John", DateTime.Now);
///         
///         // Matches any formatted string with two placeholders
///         MatchFormattedString("User {0} logged in at {1}", message);
///     }
/// }
/// </code>
/// </example>
public abstract class BaseTest<TBaseTest> where TBaseTest : BaseTest<TBaseTest>
{
	/// <summary>
	///    Initializes a new instance of the <see cref="BaseTest{TBaseTest}"/> class.
	/// </summary>
	/// <param name="output">The xUnit test output helper for writing to test output.</param>
	/// <remarks>
	///    <para>
	///       The constructor performs the following initialization:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Stores the <paramref name="output"/> for use in assertion methods</description>
	///       </item>
	///       <item>
	///          <description>Creates a <see cref="ServiceCollection"/> and configures logging with <see cref="XunitLoggerProvider"/></description>
	///       </item>
	///       <item>
	///          <description>Calls <see cref="OnLoggingBuild"/> to allow derived classes to customize logging</description>
	///       </item>
	///       <item>
	///          <description>Calls <see cref="OnConfigureServices"/> to allow derived classes to register services</description>
	///       </item>
	///       <item>
	///          <description>Builds the <see cref="ServiceProvider"/></description>
	///       </item>
	///       <item>
	///          <description>Resolves a typed <see cref="ILogger{TCategoryName}"/> for the test class</description>
	///       </item>
	///    </list>
	/// </remarks>
	protected BaseTest(ITestOutputHelper output)
	{
		Output = output;
		var serviceCollection = new ServiceCollection().AddLogging(o => {
			o.AddProvider(new XunitLoggerProvider(output));
			OnLoggingBuild(o);
		});
		OnConfigureServices(serviceCollection);
		ServiceProvider = serviceCollection.BuildServiceProvider();
		Logger = ServiceProvider.GetRequiredService<ILogger<TBaseTest>>();
	}

	/// <summary>
	///    Prints a variable's name and value to the test output.
	/// </summary>
	/// <param name="value">The value to print.</param>
	/// <param name="name">
	///    The name of the variable (automatically captured using CallerArgumentExpressionAttribute).
	/// </param>
	/// <remarks>
	///    This method uses C# 10's CallerArgumentExpressionAttribute to automatically capture
	///    the expression passed as the <paramref name="value"/> parameter, providing a clean syntax for
	///    printing variable names and values.
	/// </remarks>
	/// <example>
	///    <code>
	/// int count = 42;
	/// PrintVariable(count);
	/// // Output: count = 42
	/// 
	/// var user = new User { Name = "John" };
	/// PrintVariable(user.Name);
	/// // Output: user.Name = John
	/// </code>
	/// </example>
	protected void PrintVariable(object? value, [CallerArgumentExpression(nameof(value))] string? name = null) 
		=> Output.WriteLine($"{name} = {value}");

	/// <summary>
	///    Asserts that a condition is <c>true</c> and prints the result to test output.
	/// </summary>
	/// <param name="value">The condition to evaluate.</param>
	/// <param name="userMessage">Optional custom message to display if the assertion fails.</param>
	/// <param name="name">
	///    The name of the condition expression (automatically captured using CallerArgumentExpressionAttribute).
	/// </param>
	/// <remarks>
	///    This method combines Assert.True(bool?, string) with automatic output logging.
	///    The condition expression is automatically captured and printed along with its value.
	/// </remarks>
	/// <example>
	///    <code>
	/// int result = service.Calculate(5, 3);
	/// IsTrue(result == 8);
	/// // If true, outputs: result == 8 = True
	/// 
	/// IsTrue(user.IsActive, "User must be active");
	/// // If false, throws with message "User must be active"
	/// </code>
	/// </example>
	protected void IsTrue(bool? value, string? userMessage = null, [CallerArgumentExpression(nameof(value))] string? name = null)
	{
		Assert.True(value, userMessage);
		PrintVariable(value, name);
	}

	/// <summary>
	///    Asserts that a condition is <c>false</c> and prints the result to test output.
	/// </summary>
	/// <param name="value">The condition to evaluate.</param>
	/// <param name="userMessage">Optional custom message to display if the assertion fails.</param>
	/// <param name="name">
	///    The name of the condition expression (automatically captured using CallerArgumentExpressionAttribute).
	/// </param>
	/// <remarks>
	///    This method combines Assert.False(bool, string) with automatic output logging.
	/// </remarks>
	/// <example>
	///    <code>
	/// IsFalse(user.IsDeleted);
	/// // If false, outputs: user.IsDeleted = False
	/// 
	/// IsFalse(result &gt; 100, "Result should not exceed 100");
	/// </code>
	/// </example>
	protected void IsFalse(bool value, string? userMessage = null, [CallerArgumentExpression(nameof(value))] string? name = null)
	{
		Assert.False(value, userMessage);
		PrintVariable(value, name);
	}

	/// <summary>
	///    Asserts that a delegate throws a specific exception type and prints exception details to test output.
	/// </summary>
	/// <typeparam name="TException">The expected exception type.</typeparam>
	/// <param name="testCode">The delegate that should throw the exception.</param>
	/// <param name="name">
	///    The name of the test code expression (automatically captured using CallerArgumentExpressionAttribute).
	/// </param>
	/// <remarks>
	///    This method wraps Assert.Throws{T}(Action) and automatically prints the exception
	///    type name and message to the test output.
	/// </remarks>
	/// <example>
	///    <code>
	/// Throws&lt;ArgumentNullException&gt;(() =&gt; service.Process(null));
	/// // Output: () =&gt; service.Process(null) = Throws 'ArgumentNullException' =&gt; Value cannot be null.
	/// 
	/// Throws&lt;InvalidOperationException&gt;(() =&gt; 
	/// {
	///     var obj = new MyClass();
	///     obj.MethodThatThrows();
	/// });
	/// </code>
	/// </example>
	protected void Throws<TException>(Action testCode, [CallerArgumentExpression(nameof(testCode))] string? name = null)
		where TException: Exception
	{
		var ex = Assert.Throws<TException>(testCode);
		PrintVariable($"Throws '{ex.GetType().Name}' => {ex.Message}", name);
	}

	/// <summary>
	///    Asserts that an asynchronous delegate throws a specific exception type and prints exception details to test output.
	/// </summary>
	/// <typeparam name="TException">The expected exception type.</typeparam>
	/// <param name="testCode">The asynchronous delegate that should throw the exception.</param>
	/// <param name="name">
	///    The name of the test code expression (automatically captured using CallerArgumentExpressionAttribute).
	/// </param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	///    This method wraps Assert.ThrowsAsync{T}(Func{Task}) and automatically prints the
	///    exception type name and message to the test output.
	/// </remarks>
	/// <example>
	///    <code>
	/// await ThrowsAsync&lt;HttpRequestException&gt;(async () =&gt; 
	///     await client.GetAsync("https://invalid-url"));
	/// 
	/// await ThrowsAsync&lt;TaskCanceledException&gt;(async () =&gt;
	/// {
	///     using var cts = new CancellationTokenSource();
	///     cts.Cancel();
	///     await service.ProcessAsync(cts.Token);
	/// });
	/// </code>
	/// </example>
	protected async Task ThrowsAsync<TException>(Func<Task> testCode, [CallerArgumentExpressionAttribute(nameof(testCode))] string? name = null)
		where TException: Exception
	{
		var ex = await Assert.ThrowsAsync<TException>(testCode);
		PrintVariable($"Throws '{ex.GetType().Name}' => {ex.Message}", name);
	}

	/// <summary>
	///    Asserts that a string matches a pattern with placeholder support.
	/// </summary>
	/// <param name="pattern">
	///    The pattern to match, with placeholders in the format <c>{0}</c>, <c>{1}</c>, etc.
	///    Each placeholder is treated as a wildcard that matches any text.
	/// </param>
	/// <param name="input">The input string to validate against the pattern.</param>
	/// <remarks>
	///    <para>
	///       This method converts a formatted string pattern into a regular expression by replacing
	///       placeholders (<c>{0}</c>, <c>{1}</c>, etc.) with <c>.*</c> regex patterns. This is useful
	///       for verifying formatted strings where the exact placeholder values are unknown or variable.
	///    </para>
	///    <para>
	///       The pattern matching:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Replaces <c>{0}</c>, <c>{1}</c>, ... with <c>.*</c> (matches any characters)</description>
	///       </item>
	///       <item>
	///          <description>Adds <c>$</c> at the end to ensure complete match</description>
	///       </item>
	///       <item>
	///          <description>Uses <see cref="Regex"/> for matching</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Test formatted log messages
	/// var logMessage = $"User {userId} logged in at {timestamp}";
	/// MatchFormattedString("User {0} logged in at {1}", logMessage);
	/// // Output: String 'User 123 logged in at 2024-01-15' match pattern 'User {0} logged in at {1}'
	/// 
	/// // Test exception messages
	/// var exception = new ArgumentException("Parameter 'count' must be positive", "count");
	/// MatchFormattedString("Parameter '{0}' must be positive", exception.Message);
	/// 
	/// // Multiple placeholders
	/// var message = string.Format("Order {0} for customer {1} was {2}", orderId, customerId, status);
	/// MatchFormattedString("Order {0} for customer {1} was {2}", message);
	/// </code>
	/// </example>
	protected void MatchFormattedString(string pattern, string input)
	{
		var regexPattern = pattern;
		for (var i = 0;; i++)
		{
			if (regexPattern.Contains($"{i}"))
				regexPattern = regexPattern.Replace($"{{{i}}}", ".*");
			else
				break;
		}
		regexPattern += "$";
		Assert.True(new Regex(regexPattern).IsMatch(input),$"String '{input}' doesn't match pattern '{pattern}'");
		Output.WriteLine($"String '{input}' match pattern '{pattern}'");
	}

	/// <summary>
	///    Gets the xUnit test output helper for writing to test output.
	/// </summary>
	/// <value>
	///    The <see cref="ITestOutputHelper"/> instance passed to the constructor.
	/// </value>
	/// <remarks>
	///    Use this property to write additional output to the test results. All output written
	///    through this helper will appear in the test results window and test logs.
	/// </remarks>
	protected internal ITestOutputHelper Output { get; }

	/// <summary>
	///    Gets the dependency injection service provider for this test instance.
	/// </summary>
	/// <value>
	///    The <see cref="IServiceProvider"/> built from services configured in <see cref="OnConfigureServices"/>.
	/// </value>
	/// <remarks>
	///    Use this property to resolve services registered during test setup. The service provider
	///    is created per test instance, ensuring isolation between tests.
	/// </remarks>
	/// <example>
	///    <code>
	/// var service = ServiceProvider.GetRequiredService&lt;IMyService&gt;();
	/// var repository = ServiceProvider.GetService&lt;IRepository&gt;();
	/// </code>
	/// </example>
	protected internal IServiceProvider ServiceProvider { get; }

	/// <summary>
	///    Gets the typed logger for this test class.
	/// </summary>
	/// <value>
	///    An <see cref="ILogger{TCategoryName}"/> instance configured to write to xUnit test output.
	/// </value>
	/// <remarks>
	///    The logger category is automatically set to the derived test class type (<typeparamref name="TBaseTest"/>).
	///    All log output is directed to the xUnit test output via <see cref="XunitLoggerProvider"/>.
	/// </remarks>
	/// <example>
	///    <code>
	/// Logger.LogInformation("Starting test");
	/// Logger.LogDebug("Processing item {ItemId}", itemId);
	/// Logger.LogWarning("Unexpected condition detected");
	/// </code>
	/// </example>
	protected internal ILogger<TBaseTest> Logger { get; }

	/// <summary>
	///    Called during construction to allow derived classes to configure logging.
	/// </summary>
	/// <param name="loggingBuilder">The <see cref="ILoggingBuilder"/> to configure.</param>
	/// <remarks>
	///    <para>
	///       Override this method in derived classes to customize logging configuration such as:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Setting minimum log levels</description>
	///       </item>
	///       <item>
	///          <description>Adding additional logging providers</description>
	///       </item>
	///       <item>
	///          <description>Configuring log filters for specific categories</description>
	///       </item>
	///       <item>
	///          <description>Customizing log output format</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// protected override void OnLoggingBuild(ILoggingBuilder loggingBuilder)
	/// {
	///     // Set minimum level
	///     loggingBuilder.SetMinimumLevel(LogLevel.Debug);
	///     
	///     // Filter out noisy categories
	///     loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
	///     
	///     // Add console logging for local debugging
	///     loggingBuilder.AddConsole();
	/// }
	/// </code>
	/// </example>
	protected virtual void OnLoggingBuild(ILoggingBuilder loggingBuilder) { }

	/// <summary>
	///    Called during construction to allow derived classes to register services in the dependency injection container.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
	/// <remarks>
	///    <para>
	///       Override this method in derived classes to register services required by your tests. Common registrations include:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Test doubles (mocks, stubs, fakes)</description>
	///       </item>
	///       <item>
	///          <description>Services under test</description>
	///       </item>
	///       <item>
	///          <description>DbContext with in-memory database</description>
	///       </item>
	///       <item>
	///          <description>Configuration objects</description>
	///       </item>
	///       <item>
	///          <description>HTTP clients and factories</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// protected override void OnConfigureServices(IServiceCollection serviceCollection)
	/// {
	///     // Register services
	///     serviceCollection.AddScoped&lt;IMyService, MyService&gt;();
	///     
	///     // Register DbContext with in-memory database
	///     serviceCollection.AddDbContext&lt;MyDbContext&gt;(options =&gt;
	///         options.UseInMemoryDatabase("TestDatabase"));
	///     
	///     // Register configuration
	///     serviceCollection.Configure&lt;MyOptions&gt;(options =&gt;
	///     {
	///         options.ConnectionString = "test-connection";
	///     });
	///     
	///     // Register mock
	///     var mockRepo = new Mock&lt;IRepository&gt;();
	///     serviceCollection.AddSingleton(mockRepo.Object);
	/// }
	/// </code>
	/// </example>
	protected virtual void OnConfigureServices(IServiceCollection serviceCollection) { }

	/// <summary>
	///    Executes an action within a new dependency injection scope, resolving a required service for the action.
	/// </summary>
	/// <typeparam name="T">The type of service to resolve for the action.</typeparam>
	/// <param name="action">The asynchronous action to execute with the resolved service.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	///    <para>
	///       This method creates a new <see cref="IServiceScope"/> using <see cref="ServiceProvider"/>,
	///       resolves the required service of type <typeparamref name="T"/>, executes the action, and
	///       disposes the scope when complete.
	///    </para>
	///    <para>
	///       This is particularly useful for testing scoped services like Entity Framework DbContext,
	///       ensuring proper scope lifetime management.
	///    </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	///    Thrown if the service of type <typeparamref name="T"/> is not registered.
	/// </exception>
	/// <example>
	///    <code>
	/// [Fact]
	/// public async Task Test_DatabaseOperations()
	/// {
	///     await Scoped&lt;MyDbContext&gt;(async db =&gt;
	///     {
	///         // Arrange
	///         db.Users.Add(new User { Name = "Test" });
	///         await db.SaveChangesAsync();
	///         
	///         // Act
	///         var count = await db.Users.CountAsync();
	///         
	///         // Assert
	///         IsTrue(count == 1);
	///     });
	/// }
	/// 
	/// [Fact]
	/// public async Task Test_ScopedService()
	/// {
	///     await Scoped&lt;IMyService&gt;(async service =&gt;
	///     {
	///         var result = await service.ProcessAsync();
	///         IsTrue(result.IsSuccess);
	///     });
	/// }
	/// 
	/// // Multiple scoped operations
	/// [Fact]
	/// public async Task Test_MultipleScopes()
	/// {
	///     // First scope
	///     await Scoped&lt;IRepository&gt;(async repo =&gt;
	///     {
	///         await repo.AddAsync(new Entity());
	///     });
	///     
	///     // Second scope - ensures fresh instance
	///     await Scoped&lt;IRepository&gt;(async repo =&gt;
	///     {
	///         var count = await repo.CountAsync();
	///         IsTrue(count == 1);
	///     });
	/// }
	/// </code>
	/// </example>
	protected async Task Scoped<T>(Func<T, Task> action) where T : notnull
	{
		await using var scope = ServiceProvider.CreateAsyncScope();
		await action(scope.ServiceProvider.GetRequiredService<T>());
	}
}