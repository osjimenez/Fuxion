using Microsoft.Extensions.Logging;
using Xunit;

namespace Fuxion.Xunit;

/// <summary>
///    Provides a logging provider that creates <see cref="XunitLogger"/> instances for xUnit test output.
/// </summary>
/// <remarks>
///    <para>
///       This class implements <see cref="ILoggerProvider"/> to integrate with Microsoft.Extensions.Logging
///       infrastructure, creating loggers that write to xUnit's <see cref="ITestOutputHelper"/>. It's designed
///       to be registered with <see cref="ILoggingBuilder"/> to enable logging to xUnit test output throughout
///       the application.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>ILoggerProvider implementation:</strong> Integrates with standard .NET logging infrastructure
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Single output helper:</strong> All loggers created share the same <see cref="ITestOutputHelper"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Category support:</strong> Creates loggers for different categories, all writing to the same output
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Simple lifecycle:</strong> No resources to dispose, empty <see cref="Dispose"/> implementation
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Test isolation:</strong> Each test gets its own provider instance with its own output helper
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Integrate application logging with xUnit test output</description>
///       </item>
///       <item>
///          <description>Enable logging for services under test</description>
///       </item>
///       <item>
///          <description>Debug Entity Framework Core, HTTP clients, and other logged components</description>
///       </item>
///       <item>
///          <description>Used internally by <see cref="BaseTest{TBaseTest}"/> for automatic logging setup</description>
///       </item>
///    </list>
///    <para>
///       <strong>Integration notes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             This provider is automatically registered by <see cref="BaseTest{TBaseTest}"/> constructor
///          </description>
///       </item>
///       <item>
///          <description>
///             All loggers created by this provider write to the same test output, ensuring all logs appear in test results
///          </description>
///       </item>
///       <item>
///          <description>
///             Category names are ignored by <see cref="XunitLogger"/>, but can be filtered via <see cref="ILoggingBuilder"/>
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Automatic usage via BaseTest:</strong>
///    <code>
/// public class MyTests : BaseTest&lt;MyTests&gt;
/// {
///     public MyTests(ITestOutputHelper output) : base(output)
///     {
///         // XunitLoggerProvider is automatically registered
///     }
///     
///     [Fact]
///     public void Test_WithLogging()
///     {
///         // All services can now log to test output
///         Logger.LogInformation("Test log");
///         var service = ServiceProvider.GetRequiredService&lt;IMyService&gt;();
///         service.DoWork(); // Service's logs will appear in test output
///     }
/// }
/// </code>
///    <strong>Manual registration with ILoggingBuilder:</strong>
///    <code>
/// public class CustomTests
/// {
///     private readonly ILogger&lt;CustomTests&gt; _logger;
///     private readonly IServiceProvider _serviceProvider;
///     
///     public CustomTests(ITestOutputHelper output)
///     {
///         var services = new ServiceCollection();
///         
///         // Register logging with XunitLoggerProvider
///         services.AddLogging(builder =&gt;
///         {
///             builder.AddProvider(new XunitLoggerProvider(output));
///             builder.SetMinimumLevel(LogLevel.Debug);
///         });
///         
///         // Register services
///         services.AddScoped&lt;IMyService, MyService&gt;();
///         
///         _serviceProvider = services.BuildServiceProvider();
///         _logger = _serviceProvider.GetRequiredService&lt;ILogger&lt;CustomTests&gt;&gt;();
///     }
///     
///     [Fact]
///     public void Test_Something()
///     {
///         _logger.LogInformation("Starting test");
///         var service = _serviceProvider.GetRequiredService&lt;IMyService&gt;();
///         service.Process();
///         _logger.LogInformation("Test completed");
///     }
/// }
/// </code>
///    <strong>With Entity Framework Core:</strong>
///    <code>
/// public class DatabaseTests
/// {
///     private readonly ITestOutputHelper _output;
///     private readonly IServiceProvider _serviceProvider;
///     
///     public DatabaseTests(ITestOutputHelper output)
///     {
///         _output = output;
///         
///         var services = new ServiceCollection();
///         
///         services.AddLogging(builder =&gt;
///         {
///             builder.AddProvider(new XunitLoggerProvider(output));
///             // Filter to only show EF Core SQL queries
///             builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
///         });
///         
///         services.AddDbContext&lt;MyDbContext&gt;(options =&gt;
///         {
///             options.UseInMemoryDatabase("TestDb");
///             options.EnableSensitiveDataLogging();
///         });
///         
///         _serviceProvider = services.BuildServiceProvider();
///     }
///     
///     [Fact]
///     public async Task Test_Query()
///     {
///         using var scope = _serviceProvider.CreateScope();
///         var db = scope.ServiceProvider.GetRequiredService&lt;MyDbContext&gt;();
///         
///         var users = await db.Users.Where(u =&gt; u.IsActive).ToListAsync();
///         // SQL query will be logged to test output
///     }
/// }
/// </code>
///    <strong>Multiple category loggers:</strong>
///    <code>
/// public class MultiCategoryTests
/// {
///     public MultiCategoryTests(ITestOutputHelper output)
///     {
///         var services = new ServiceCollection();
///         var provider = new XunitLoggerProvider(output);
///         
///         services.AddLogging(builder =&gt;
///         {
///             builder.AddProvider(provider);
///         });
///         
///         var sp = services.BuildServiceProvider();
///         
///         // Create loggers for different categories
///         var logger1 = sp.GetRequiredService&lt;ILogger&lt;MyService&gt;&gt;();
///         var logger2 = sp.GetRequiredService&lt;ILogger&lt;MyRepository&gt;&gt;();
///         
///         // Both write to the same test output
///         logger1.LogInformation("Service log");
///         logger2.LogInformation("Repository log");
///     }
/// }
/// </code>
///    <strong>Integration testing with WebApplicationFactory:</strong>
///    <code>
/// public class ApiIntegrationTests : IClassFixture&lt;WebApplicationFactory&lt;Program&gt;&gt;
/// {
///     private readonly WebApplicationFactory&lt;Program&gt; _factory;
///     private readonly ITestOutputHelper _output;
///     
///     public ApiIntegrationTests(
///         WebApplicationFactory&lt;Program&gt; factory,
///         ITestOutputHelper output)
///     {
///         _output = output;
///         _factory = factory.WithWebHostBuilder(builder =&gt;
///         {
///             builder.ConfigureLogging(logging =&gt;
///             {
///                 logging.ClearProviders();
///                 logging.AddProvider(new XunitLoggerProvider(output));
///             });
///         });
///     }
///     
///     [Fact]
///     public async Task Test_ApiEndpoint()
///     {
///         var client = _factory.CreateClient();
///         var response = await client.GetAsync("/api/users");
///         // All API logs appear in test output
///         response.EnsureSuccessStatusCode();
///     }
/// }
/// </code>
///    <strong>Custom filtering:</strong>
///    <code>
/// public class FilteredLogTests
/// {
///     public FilteredLogTests(ITestOutputHelper output)
///     {
///         var services = new ServiceCollection();
///         
///         services.AddLogging(builder =&gt;
///         {
///             builder.AddProvider(new XunitLoggerProvider(output));
///             
///             // Only show warnings and errors from Microsoft namespaces
///             builder.AddFilter("Microsoft", LogLevel.Warning);
///             
///             // Show all logs from application
///             builder.AddFilter("MyApp", LogLevel.Trace);
///             
///             // Hide specific noisy category
///             builder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.None);
///         });
///     }
/// }
/// </code>
/// </example>
public class XunitLoggerProvider : ILoggerProvider
{
	/// <summary>
	///    Initializes a new instance of the <see cref="XunitLoggerProvider"/> class.
	/// </summary>
	/// <param name="output">The xUnit test output helper to use for all created loggers.</param>
	/// <remarks>
	///    <para>
	///       All loggers created by this provider will share the same <paramref name="output"/> instance,
	///       ensuring all log messages from different categories appear in the same test output.
	///    </para>
	///    <para>
	///       The <paramref name="output"/> should be the <see cref="ITestOutputHelper"/> instance provided
	///       to the test class constructor by xUnit.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// public class MyTests
	/// {
	///     private readonly ILoggerFactory _loggerFactory;
	///     
	///     public MyTests(ITestOutputHelper output)
	///     {
	///         var provider = new XunitLoggerProvider(output);
	///         _loggerFactory = LoggerFactory.Create(builder =&gt;
	///         {
	///             builder.AddProvider(provider);
	///         });
	///     }
	/// }
	/// </code>
	/// </example>
	public XunitLoggerProvider(ITestOutputHelper output) => Output = output;

	/// <summary>
	///    Gets the xUnit test output helper used by all loggers created by this provider.
	/// </summary>
	/// <value>
	///    The <see cref="ITestOutputHelper"/> instance provided in the constructor.
	/// </value>
	/// <remarks>
	///    All loggers created by <see cref="CreateLogger"/> will write to this output helper.
	/// </remarks>
	public ITestOutputHelper Output { get; }

	/// <summary>
	///    Performs no cleanup as this provider has no resources to dispose.
	/// </summary>
	/// <remarks>
	///    <para>
	///       This implementation is empty because <see cref="XunitLoggerProvider"/> doesn't hold any
	///       unmanaged resources or subscriptions that need cleanup. The <see cref="ITestOutputHelper"/>
	///       is managed by the xUnit framework and shouldn't be disposed by this provider.
	///    </para>
	/// </remarks>
	public void Dispose() { }

	/// <summary>
	///    Creates a new <see cref="XunitLogger"/> instance for the specified category.
	/// </summary>
	/// <param name="categoryName">
	///    The category name for the logger. This is typically the fully qualified type name of the class
	///    requesting the logger, but is not used by <see cref="XunitLogger"/> itself.
	/// </param>
	/// <returns>
	///    A new <see cref="XunitLogger"/> instance that writes to <see cref="Output"/>.
	/// </returns>
	/// <remarks>
	///    <para>
	///       All loggers created by this method share the same <see cref="Output"/> instance, regardless
	///       of the <paramref name="categoryName"/>. The category name can still be used for filtering
	///       via <see cref="ILoggingBuilder"/> configuration.
	///    </para>
	///    <para>
	///       Each call to this method creates a new <see cref="XunitLogger"/> instance, but they all
	///       write to the same test output, so log messages from different categories will be interleaved
	///       in the order they're written.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// var provider = new XunitLoggerProvider(output);
	/// 
	/// // Create loggers for different categories
	/// var logger1 = provider.CreateLogger("MyApp.Services.UserService");
	/// var logger2 = provider.CreateLogger("MyApp.Repositories.UserRepository");
	/// 
	/// // Both write to the same test output
	/// logger1.LogInformation("Service message");
	/// logger2.LogInformation("Repository message");
	/// 
	/// // Output:
	/// // Information: Service message
	/// // Information: Repository message
	/// </code>
	/// </example>
	public ILogger CreateLogger(string categoryName) => new XunitLogger(Output);
}