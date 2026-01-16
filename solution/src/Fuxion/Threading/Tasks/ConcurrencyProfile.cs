namespace Fuxion.Threading.Tasks;

/// <summary>
/// Defines a configuration profile that controls the concurrency behavior and execution patterns of tasks managed by <see cref="TaskManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConcurrencyProfile"/> is a struct that encapsulates various flags and settings to control how tasks are executed,
/// queued, and managed. It provides predefined profiles for common scenarios and allows custom configuration through property
/// combination.
/// </para>
/// <para>
/// <strong>Key concepts:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>Sequential execution:</strong> Tasks are queued and executed one after another, not concurrently</description></item>
/// <item><description><strong>Cancellation:</strong> Previous tasks can be cancelled when new ones are queued</description></item>
/// <item><description><strong>Last-only execution:</strong> Only the most recent task is executed, skipping queued ones</description></item>
/// <item><description><strong>Instance-based:</strong> Concurrency control can be applied per instance rather than globally</description></item>
/// </list>
/// <para>
/// <strong>Predefined profiles:</strong>
/// </para>
/// <para>
/// The struct provides five static readonly profiles that cover common use cases:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Default"/>: No special concurrency control (parallel execution)</description></item>
/// <item><description><see cref="RunSequentially"/>: Execute tasks one at a time in order</description></item>
/// <item><description><see cref="RunSequentiallyAndCancelPrevious"/>: Sequential execution with cancellation of pending tasks</description></item>
/// <item><description><see cref="RunSequentiallyAndExecuteOnlyLast"/>: Sequential execution but skip to the last queued task</description></item>
/// <item><description><see cref="RunSequentiallyCancelPreviousAndExecuteOnlyLast"/>: Combination of cancellation and last-only execution</description></item>
/// </list>
/// <para>
/// <strong>Use cases by profile:</strong>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Profile</term>
/// <description>Use Case</description>
/// </listheader>
/// <item>
/// <term><see cref="Default"/></term>
/// <description>Standard parallel task execution, no restrictions</description>
/// </item>
/// <item>
/// <term><see cref="RunSequentially"/></term>
/// <description>Database transactions, file operations requiring order, state machines</description>
/// </item>
/// <item>
/// <term><see cref="RunSequentiallyAndCancelPrevious"/></term>
/// <description>UI updates where only the latest state matters, cancellable long-running operations</description>
/// </item>
/// <item>
/// <term><see cref="RunSequentiallyAndExecuteOnlyLast"/></term>
/// <description>Search-as-you-type, live data filtering, throttled API calls</description>
/// </item>
/// <item>
/// <term><see cref="RunSequentiallyCancelPreviousAndExecuteOnlyLast"/></term>
/// <description>Real-time preview generation, aggressive throttling scenarios</description>
/// </item>
/// </list>
/// <para>
/// <strong>Custom profiles:</strong>
/// </para>
/// <para>
/// You can create custom profiles by setting individual properties. For example, combining <see cref="ByInstance"/> with
/// other flags allows per-object concurrency control while sharing the same profile configuration.
/// </para>
/// </remarks>
/// <example>
/// <strong>Using predefined profiles:</strong>
/// <code>
/// // Default parallel execution
/// var task1 = TaskManager.StartNew(
///     () => Console.WriteLine("Running in parallel"),
///     concurrencyProfile: ConcurrencyProfile.Default
/// );
/// 
/// // Sequential execution - tasks run one after another
/// var task2 = TaskManager.StartNew(
///     () => ProcessOrder(),
///     concurrencyProfile: ConcurrencyProfile.RunSequentially
/// );
/// 
/// // Sequential with cancellation - new tasks cancel previous ones
/// var task3 = TaskManager.StartNew(
///     () => UpdateUI(),
///     concurrencyProfile: ConcurrencyProfile.RunSequentiallyAndCancelPrevious
/// );
/// 
/// // Execute only the last queued task
/// var task4 = TaskManager.StartNew(
///     () => PerformSearch(searchText),
///     concurrencyProfile: ConcurrencyProfile.RunSequentiallyAndExecuteOnlyLast
/// );
/// </code>
/// 
/// <strong>Creating custom profiles:</strong>
/// <code>
/// // Custom profile with specific settings
/// var customProfile = new ConcurrencyProfile
/// {
///     Name = "Custom Sequential",
///     Sequentially = true,
///     CancelPrevious = false,
///     ExecuteOnlyLast = false,
///     ByInstance = true
/// };
/// 
/// var task = TaskManager.StartNew(
///     () => DoWork(),
///     concurrencyProfile: customProfile
/// );
/// </code>
/// 
/// <strong>Search-as-you-type scenario:</strong>
/// <code>
/// // Each keystroke creates a new task, but only the last one executes
/// private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
/// {
///     var searchText = SearchBox.Text;
///     
///     TaskManager.StartNew(
///         async () => 
///         {
///             var results = await SearchService.SearchAsync(searchText);
///             await Dispatcher.InvokeAsync(() => DisplayResults(results));
///         },
///         concurrencyProfile: ConcurrencyProfile.RunSequentiallyAndExecuteOnlyLast
///     );
/// }
/// </code>
/// 
/// <strong>UI update with cancellation:</strong>
/// <code>
/// // Update preview - cancel previous generation if still running
/// private void GeneratePreview()
/// {
///     TaskManager.StartNew(
///         async () => 
///         {
///             var preview = await GenerateComplexPreview();
///             UpdatePreviewImage(preview);
///         },
///         concurrencyProfile: ConcurrencyProfile.RunSequentiallyAndCancelPrevious
///     );
/// }
/// </code>
/// 
/// <strong>Per-instance concurrency control:</strong>
/// <code>
/// public class DataProcessor
/// {
///     private readonly ConcurrencyProfile _profile = new()
///     {
///         Sequentially = true,
///         ByInstance = true // Each processor instance has its own queue
///     };
///     
///     public void ProcessData(string data)
///     {
///         TaskManager.StartNew(
///             () => DoProcessing(data),
///             concurrencyProfile: _profile
///         );
///     }
/// }
/// 
/// // Each processor maintains independent sequential execution
/// var processor1 = new DataProcessor();
/// var processor2 = new DataProcessor();
/// processor1.ProcessData("A"); // Sequential within processor1
/// processor2.ProcessData("B"); // Can run parallel to processor1
/// </code>
/// </example>
public struct ConcurrencyProfile
{
	/// <summary>
	/// Gets the default concurrency profile with no special restrictions, allowing parallel task execution.
	/// </summary>
	/// <value>A profile with all flags set to <c>false</c>.</value>
	/// <remarks>
	/// This profile imposes no concurrency restrictions. Tasks created with this profile execute in parallel
	/// according to the .NET thread pool's default behavior. This is the standard behavior for most task scenarios.
	/// </remarks>
	public static readonly ConcurrencyProfile Default = new();
	
	/// <summary>
	/// Gets a profile that executes tasks sequentially, one at a time in the order they were queued.
	/// </summary>
	/// <value>A profile with <see cref="Sequentially"/> set to <c>true</c>.</value>
	/// <remarks>
	/// <para>
	/// Tasks using this profile are queued and executed one after another. The next task doesn't start until
	/// the previous one completes. This ensures predictable ordering and prevents concurrent execution.
	/// </para>
	/// <para>
	/// <strong>Use this when:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Order of execution matters (e.g., database transactions)</description></item>
	/// <item><description>Resource access must be serialized (e.g., file operations)</description></item>
	/// <item><description>State transitions must occur sequentially (e.g., state machines)</description></item>
	/// </list>
	/// </remarks>
	public static readonly ConcurrencyProfile RunSequentially = new() {
		Sequentially = true
	};
	
	/// <summary>
	/// Gets a profile that executes tasks sequentially and cancels any pending tasks when a new one is queued.
	/// </summary>
	/// <value>A profile with <see cref="Sequentially"/> and <see cref="CancelPrevious"/> set to <c>true</c>.</value>
	/// <remarks>
	/// <para>
	/// This profile combines sequential execution with aggressive cancellation. When a new task is queued,
	/// all pending (not yet started) tasks are cancelled, and only the new task executes after the current one completes.
	/// </para>
	/// <para>
	/// <strong>Use this when:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Only the most recent operation matters (e.g., UI state updates)</description></item>
	/// <item><description>Long-running operations should be cancelled if superseded</description></item>
	/// <item><description>Resource cleanup is required when operations are abandoned</description></item>
	/// </list>
	/// <para>
	/// <strong>Note:</strong> Currently running tasks are NOT cancelled, only pending ones in the queue.
	/// </para>
	/// </remarks>
	public static readonly ConcurrencyProfile RunSequentiallyAndCancelPrevious = new() {
		Sequentially = true, CancelPrevious = true
	};
	
	/// <summary>
	/// Gets a profile that executes tasks sequentially but skips intermediate tasks to execute only the last queued one.
	/// </summary>
	/// <value>A profile with <see cref="Sequentially"/> and <see cref="ExecuteOnlyLast"/> set to <c>true</c>.</value>
	/// <remarks>
	/// <para>
	/// This profile queues tasks sequentially but implements a "skip to latest" behavior. When multiple tasks are queued,
	/// only the most recently added task executes after the current one completes. Intermediate tasks are skipped entirely.
	/// </para>
	/// <para>
	/// <strong>Use this when:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Rapid successive operations where intermediate results aren't needed (e.g., search-as-you-type)</description></item>
	/// <item><description>Real-time data updates where only the latest value matters</description></item>
	/// <item><description>Throttling without explicit delays (natural throttling through queue skipping)</description></item>
	/// </list>
	/// <para>
	/// <strong>Difference from <see cref="RunSequentiallyAndCancelPrevious"/>:</strong>
	/// Tasks aren't cancelled but simply never started. This is useful when cancellation overhead isn't desired
	/// or when the task delegate doesn't support cancellation.
	/// </para>
	/// </remarks>
	public static readonly ConcurrencyProfile RunSequentiallyAndExecuteOnlyLast = new() {
		Sequentially = true, ExecuteOnlyLast = true
	};
	
	/// <summary>
	/// Gets a profile that combines sequential execution, cancellation of previous tasks, and execution of only the last task.
	/// </summary>
	/// <value>A profile with <see cref="Sequentially"/>, <see cref="CancelPrevious"/>, and <see cref="ExecuteOnlyLast"/> all set to <c>true</c>.</value>
	/// <remarks>
	/// <para>
	/// This is the most aggressive concurrency control profile. It combines:
	/// </para>
	/// <list type="bullet">
	/// <item><description>Sequential execution (tasks run one at a time)</description></item>
	/// <item><description>Cancellation of pending tasks</description></item>
	/// <item><description>Skip-to-last behavior (only most recent task executes)</description></item>
	/// </list>
	/// <para>
	/// <strong>Use this when:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description>Maximum responsiveness to the latest request is critical</description></item>
	/// <item><description>Resource cleanup via cancellation is important</description></item>
	/// <item><description>Intermediate operations are completely unnecessary</description></item>
	/// <item><description>Heavy throttling with cancellation support is needed</description></item>
	/// </list>
	/// <para>
	/// <strong>Example scenarios:</strong>
	/// Real-time preview generation where generating previews for intermediate states is wasteful,
	/// or live search with expensive backend queries that should be cancelled if superseded.
	/// </para>
	/// </remarks>
	public static readonly ConcurrencyProfile RunSequentiallyCancelPreviousAndExecuteOnlyLast = new() {
		Sequentially = true, CancelPrevious = true, ExecuteOnlyLast = true
	};
	
	/// <summary>
	/// Gets or sets an optional name for the concurrency profile for debugging and identification purposes.
	/// </summary>
	/// <value>A string identifying the profile, or <c>null</c> if not set.</value>
	/// <remarks>
	/// This property is purely for documentation and debugging. It doesn't affect the profile's behavior
	/// but can be useful when logging, profiling, or diagnosing concurrency issues.
	/// </remarks>
	public string Name { get; set; }
	
	/// <summary>
	/// Gets or sets a value indicating whether concurrency control should be applied per instance rather than globally.
	/// </summary>
	/// <value><c>true</c> if concurrency control is instance-specific; otherwise, <c>false</c>.</value>
	/// <remarks>
	/// <para>
	/// When <c>true</c>, the concurrency profile's restrictions (sequencing, cancellation, etc.) apply independently
	/// to each instance or context that uses the profile. When <c>false</c>, restrictions apply globally across all
	/// tasks using the same profile.
	/// </para>
	/// <para>
	/// <strong>Example:</strong>
	/// </para>
	/// <code>
	/// // Global sequential execution (all tasks using this profile share one queue)
	/// var globalProfile = new ConcurrencyProfile { Sequentially = true, ByInstance = false };
	/// 
	/// // Per-instance sequential execution (each object instance has its own queue)
	/// var instanceProfile = new ConcurrencyProfile { Sequentially = true, ByInstance = true };
	/// </code>
	/// <para>
	/// <strong>Note:</strong> The exact behavior of this flag depends on the <see cref="TaskManager"/> implementation
	/// and how it tracks and groups task instances.
	/// </para>
	/// </remarks>
	public bool ByInstance { get; set; }
	
	/// <summary>
	/// Gets or sets a value indicating whether tasks should be executed sequentially rather than in parallel.
	/// </summary>
	/// <value><c>true</c> if tasks should execute one at a time; otherwise, <c>false</c> for parallel execution.</value>
	/// <remarks>
	/// <para>
	/// When <c>true</c>, tasks are queued and executed in FIFO (First-In-First-Out) order. The next task waits
	/// until the previous task completes before starting.
	/// </para>
	/// <para>
	/// This is the foundation flag for all sequential execution profiles. It's typically combined with other flags
	/// like <see cref="CancelPrevious"/> or <see cref="ExecuteOnlyLast"/> to achieve specific behaviors.
	/// </para>
	/// <para>
	/// <strong>Performance consideration:</strong> Sequential execution may reduce throughput compared to parallel
	/// execution but ensures predictable ordering and can simplify reasoning about task dependencies and state.
	/// </para>
	/// </remarks>
	public bool Sequentially { get; set; }
	
	/// <summary>
	/// Gets or sets a value indicating whether only the last queued task should be executed, skipping intermediate tasks.
	/// </summary>
	/// <value><c>true</c> to execute only the most recent task; otherwise, <c>false</c> to execute all queued tasks.</value>
	/// <remarks>
	/// <para>
	/// When <c>true</c> and combined with <see cref="Sequentially"/>, the task queue implements a "skip-to-latest"
	/// behavior. As tasks are queued, intermediate tasks are marked to be skipped, and only the most recently
	/// added task will execute once the current task completes.
	/// </para>
	/// <para>
	/// <strong>Typical usage:</strong> Search-as-you-type, live filtering, auto-save debouncing, real-time calculations
	/// where only the final result matters.
	/// </para>
	/// <para>
	/// <strong>Important:</strong> This flag typically requires <see cref="Sequentially"/> to be <c>true</c> to be
	/// meaningful. Without sequential execution, there's no queue from which to skip tasks.
	/// </para>
	/// </remarks>
	public bool ExecuteOnlyLast { get; set; }
	
	/// <summary>
	/// Gets or sets a value indicating whether pending tasks should be cancelled when a new task is queued.
	/// </summary>
	/// <value><c>true</c> to cancel pending tasks when new ones arrive; otherwise, <c>false</c>.</value>
	/// <remarks>
	/// <para>
	/// When <c>true</c>, any tasks that are queued but not yet started are cancelled when a new task is added to the queue.
	/// This is useful for scenarios where only the most recent operation is relevant and older operations can be safely
	/// abandoned.
	/// </para>
	/// <para>
	/// <strong>Cancellation scope:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Cancelled:</strong> Tasks that are queued but haven't started executing yet</description></item>
	/// <item><description><strong>NOT cancelled:</strong> The currently executing task (it runs to completion or until it checks cancellation tokens)</description></item>
	/// </list>
	/// <para>
	/// <strong>Use with caution:</strong> Ensure your task delegates properly handle cancellation if they allocate resources
	/// or perform operations that need cleanup. Use <see cref="System.Threading.CancellationToken"/> parameters in your
	/// task delegates for proper cancellation support.
	/// </para>
	/// <para>
	/// <strong>Difference from <see cref="ExecuteOnlyLast"/>:</strong>
	/// </para>
	/// <list type="bullet">
	/// <item><description><see cref="CancelPrevious"/>: Tasks are actively cancelled (cancellation tokens are triggered)</description></item>
	/// <item><description><see cref="ExecuteOnlyLast"/>: Tasks are simply skipped without cancellation notification</description></item>
	/// </list>
	/// </remarks>
	public bool CancelPrevious { get; set; }
}