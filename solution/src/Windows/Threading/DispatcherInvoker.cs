using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Fuxion.Windows.Input;

namespace Fuxion.Windows.Threading;

/// <summary>
///    Provides a WPF <see cref="Dispatcher"/>-based implementation of <see cref="IInvoker"/> for marshalling calls to the UI thread.
/// </summary>
/// <remarks>
///    <para>
///       This class implements the <see cref="IInvoker"/> interface to provide automatic thread marshalling
///       for WPF applications. It ensures that operations are executed on the correct dispatcher thread,
///       typically the UI thread, which is required for updating WPF UI elements.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic thread marshalling:</strong> Detects if already on dispatcher thread and executes directly or marshals
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Async support:</strong> Provides asynchronous execution via <see cref="Task"/> and <see cref="Task{TResult}"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Shutdown handling:</strong> Gracefully handles dispatcher shutdown scenarios
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Optional invocation:</strong> Respects <see cref="IInvokable.UseInvoker"/> flag for conditional marshalling
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible dispatcher:</strong> Can use provided dispatcher or current dispatcher
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Execute UI updates from background threads in WPF applications</description>
///       </item>
///       <item>
///          <description>Implement commands that need to interact with UI elements</description>
///       </item>
///       <item>
///          <description>Ensure thread-safe property updates in ViewModels</description>
///       </item>
///       <item>
///          <description>Marshal async operations back to the UI thread</description>
///       </item>
///       <item>
///          <description>Used by <see cref="GenericCommand"/> for async command execution</description>
///       </item>
///    </list>
///    <para>
///       <strong>Thread safety:</strong>
///    </para>
///    <para>
///       This class is thread-safe. It uses <see cref="Dispatcher.CheckAccess"/> to determine if the calling
///       thread is the dispatcher thread and marshals the call using <see cref="Dispatcher.InvokeAsync(Action)"/> when necessary.
///    </para>
/// </remarks>
/// <example>
///    <strong>Basic usage with GenericCommand:</strong>
///    <code>
/// public class MainViewModel : INotifyPropertyChanged
/// {
///     private readonly DispatcherInvoker _invoker;
///     
///     public MainViewModel()
///     {
///         _invoker = new DispatcherInvoker();
///         
///         // Commands automatically use the invoker
///         LoadDataCommand = new GenericCommand(LoadDataAsync);
///     }
///     
///     public ICommand LoadDataCommand { get; }
///     
///     private async void LoadDataAsync()
///     {
///         // This runs on background thread
///         var data = await FetchDataFromServerAsync();
///         
///         // This is automatically marshalled to UI thread by DispatcherInvoker
///         Items.Clear();
///         foreach (var item in data)
///         {
///             Items.Add(item);
///         }
///     }
/// }
/// </code>
///    <strong>Custom usage with IInvokable:</strong>
///    <code>
/// public class CustomInvokable : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
/// }
/// 
/// public class Worker
/// {
///     private readonly DispatcherInvoker _invoker;
///     private readonly CustomInvokable _invokable;
///     
///     public Worker(Dispatcher dispatcher)
///     {
///         _invoker = new DispatcherInvoker(dispatcher);
///         _invokable = new CustomInvokable();
///     }
///     
///     public async Task ProcessAsync()
///     {
///         // Create a delegate to execute
///         Action updateUI = () =>
///         {
///             // UI update code here
///             Console.WriteLine("Updating UI");
///         };
///         
///         // Invoke on UI thread
///         await _invoker.InvokeActionDelegate(_invokable, updateUI);
///     }
/// }
/// </code>
///    <strong>Using with current dispatcher:</strong>
///    <code>
/// // In a WPF Window or UserControl
/// public partial class MainWindow : Window
/// {
///     private readonly DispatcherInvoker _invoker;
///     
///     public MainWindow()
///     {
///         InitializeComponent();
///         
///         // Uses Dispatcher.CurrentDispatcher
///         _invoker = new DispatcherInvoker();
///     }
///     
///     private async Task UpdateUIFromBackgroundAsync()
///     {
///         // Do background work
///         await Task.Delay(1000);
///         
///         // Marshal back to UI thread
///         var invokable = new CustomInvokable();
///         await _invoker.InvokeActionDelegate(
///             invokable,
///             new Action(() => StatusLabel.Content = "Complete")
///         );
///     }
/// }
/// </code>
///    <strong>Function delegate with return value:</strong>
///    <code>
/// public class DataService
/// {
///     private readonly DispatcherInvoker _invoker;
///     
///     public DataService(Dispatcher dispatcher)
///     {
///         _invoker = new DispatcherInvoker(dispatcher);
///     }
///     
///     public async Task&lt;string&gt; GetUserInputAsync()
///     {
///         var invokable = new CustomInvokable();
///         
///         // Get input from UI thread
///         Func&lt;string&gt; getInput = () =>
///         {
///             var dialog = new InputDialog();
///             dialog.ShowDialog();
///             return dialog.InputText;
///         };
///         
///         return await _invoker.InvokeFuncDelegate&lt;string&gt;(
///             invokable,
///             getInput
///         );
///     }
/// }
/// </code>
///    <strong>Handling dispatcher shutdown:</strong>
///    <code>
/// public class ShutdownAwareService
/// {
///     private readonly DispatcherInvoker _invoker;
///     
///     public ShutdownAwareService(Dispatcher dispatcher)
///     {
///         _invoker = new DispatcherInvoker(dispatcher);
///     }
///     
///     public async Task TryUpdateUIAsync()
///     {
///         var invokable = new CustomInvokable();
///         
///         try
///         {
///             await _invoker.InvokeActionDelegate(
///                 invokable,
///                 new Action(() => Console.WriteLine("UI Update"))
///             );
///         }
///         catch (InvalidOperationException)
///         {
///             // Dispatcher has shut down
///             Console.WriteLine("Cannot update UI - dispatcher shut down");
///         }
///     }
/// }
/// </code>
///    <strong>Conditional invocation based on UseInvoker:</strong>
///    <code>
/// public class ConditionalWorker
/// {
///     private readonly DispatcherInvoker _invoker;
///     
///     public ConditionalWorker()
///     {
///         _invoker = new DispatcherInvoker();
///     }
///     
///     public async Task ProcessWithConditionAsync(bool useDispatcher)
///     {
///         var invokable = new CustomInvokable
///         {
///             UseInvoker = useDispatcher
///         };
///         
///         Action work = () => Console.WriteLine("Work done");
///         
///         // If UseInvoker is false, executes directly without marshalling
///         // If true, marshals to dispatcher thread
///         await _invoker.InvokeActionDelegate(invokable, work);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
///    Initializes a new instance of the <see cref="DispatcherInvoker"/> class.
/// </remarks>
/// <param name="dispatcher">
///    The <see cref="Dispatcher"/> to use for marshalling calls. If <c>null</c>, uses <see cref="Dispatcher.CurrentDispatcher"/>.
/// </param>
/// <remarks>
///    <para>
///       If <paramref name="dispatcher"/> is not provided, the invoker will use <see cref="Dispatcher.CurrentDispatcher"/>,
///       which is the dispatcher associated with the thread that creates this instance.
///    </para>
///    <para>
///       <strong>Important:</strong> When using <see cref="Dispatcher.CurrentDispatcher"/>, ensure this instance is created
///       on the UI thread to get the correct dispatcher reference.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// // Use current dispatcher (must be called from UI thread)
/// var invoker1 = new DispatcherInvoker();
/// 
/// // Use specific dispatcher
/// var invoker2 = new DispatcherInvoker(Application.Current.Dispatcher);
/// 
/// // Use window's dispatcher
/// var window = new MainWindow();
/// var invoker3 = new DispatcherInvoker(window.Dispatcher);
/// </code>
/// </example>
public class DispatcherInvoker(Dispatcher? dispatcher = null) : IInvoker
{
	private readonly Dispatcher _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

	/// <summary>
	///    Invokes an action delegate, optionally marshalling to the dispatcher thread.
	/// </summary>
	/// <param name="invokable">
	///    The <see cref="IInvokable"/> instance that controls whether to use the invoker via its <see cref="IInvokable.UseInvoker"/> property.
	/// </param>
	/// <param name="method">The delegate to invoke.</param>
	/// <param name="args">Optional arguments to pass to the delegate.</param>
	/// <returns>
	///    A <see cref="Task"/> representing the asynchronous operation. The task completes when the delegate has executed.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The invocation behavior depends on several conditions:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             If <paramref name="invokable"/>.<see cref="IInvokable.UseInvoker"/> is <c>false</c>, executes directly without marshalling
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If dispatcher is <c>null</c> or <see cref="Dispatcher.CheckAccess"/> returns <c>true</c> (already on dispatcher thread),
	///             executes directly
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="Dispatcher.HasShutdownStarted"/> is <c>true</c>, returns a completed task without executing
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Otherwise, marshals the call to the dispatcher thread using <see cref="Dispatcher.InvokeAsync(Action)"/>
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var invoker = new DispatcherInvoker();
	/// var invokable = new CustomInvokable { UseInvoker = true };
	/// 
	/// Action updateUI = () =>
	/// {
	///     // This will run on UI thread
	///     myTextBlock.Text = "Updated";
	/// };
	/// 
	/// await invoker.InvokeActionDelegate(invokable, updateUI);
	/// </code>
	/// </example>
	public Task InvokeActionDelegate(IInvokable invokable, Delegate method, params object?[] args)
	{
		if (!invokable.UseInvoker || _dispatcher == null || _dispatcher.CheckAccess()) return Task.FromResult(method.DynamicInvoke(args));
		if (!_dispatcher.HasShutdownStarted) return _dispatcher.InvokeAsync(() => method.DynamicInvoke(args)).Task;
		return Task.CompletedTask;
	}

	/// <summary>
	///    Invokes a function delegate that returns a value, optionally marshalling to the dispatcher thread.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function delegate.</typeparam>
	/// <param name="invokable">
	///    The <see cref="IInvokable"/> instance that controls whether to use the invoker via its <see cref="IInvokable.UseInvoker"/> property.
	/// </param>
	/// <param name="method">The function delegate to invoke.</param>
	/// <param name="args">Optional arguments to pass to the delegate.</param>
	/// <returns>
	///    A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the value
	///    returned by the delegate.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///    Thrown when:
	///    <list type="bullet">
	///       <item>
	///          <description>The delegate returns <c>null</c></description>
	///       </item>
	///       <item>
	///          <description>The dispatcher has started shutdown</description>
	///       </item>
	///    </list>
	/// </exception>
	/// <remarks>
	///    <para>
	///       The invocation behavior follows the same rules as <see cref="InvokeActionDelegate"/>, with additional
	///       handling for return values:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If the delegate returns <c>null</c>, throws <see cref="InvalidOperationException"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If the dispatcher has shut down, throws <see cref="InvalidOperationException"/> instead of returning a completed task
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Otherwise, returns the delegate's result wrapped in a <see cref="Task{TResult}"/>
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// var invoker = new DispatcherInvoker();
	/// var invokable = new CustomInvokable { UseInvoker = true };
	/// 
	/// Func&lt;string&gt; getUserInput = () =>
	/// {
	///     // This will run on UI thread
	///     var dialog = new InputDialog();
	///     dialog.ShowDialog();
	///     return dialog.Result;
	/// };
	/// 
	/// string result = await invoker.InvokeFuncDelegate&lt;string&gt;(invokable, getUserInput);
	/// Console.WriteLine($"User entered: {result}");
	/// </code>
	/// </example>
	public Task<TResult> InvokeFuncDelegate<TResult>(IInvokable invokable, Delegate method, params object?[] args)
	{
		if (!invokable.UseInvoker || _dispatcher == null || _dispatcher.CheckAccess())
		{
			var r = method.DynamicInvoke(args);
			return r is null
				? throw new InvalidOperationException() // Task.FromResult(default(TResult))
				: Task.FromResult((TResult)r);
		}
		if (!_dispatcher.HasShutdownStarted)
			return _dispatcher.InvokeAsync(() => {
				var r = method.DynamicInvoke(args);
				return r == null
					? throw new InvalidOperationException() //default
					: (TResult)r;
			}).Task;
		throw new InvalidOperationException();
		//return Task.FromResult(default(TResult));
	}
}