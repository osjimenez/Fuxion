using System;
using System.Threading.Tasks;
using Fuxion.Reflection;
using Fuxion.Windows.Input;

namespace Fuxion.Windows.Threading;

/// <summary>
///    Defines a contract for objects that can be invoked through an <see cref="IInvoker"/> implementation.
/// </summary>
/// <remarks>
///    <para>
///       This interface is typically implemented by command classes or other types that need to control
///       whether their operations should be marshalled through an invoker (such as a dispatcher for UI thread).
///       The <see cref="UseInvoker"/> property allows runtime control of the invocation behavior.
///    </para>
///    <para>
///       Used extensively by <see cref="GenericCommand"/> and <see cref="GenericCommand{TParameter}"/> to
///       enable asynchronous and thread-safe command execution.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// public class MyCommand : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task ExecuteAsync()
///     {
///         // This will use the invoker if UseInvoker is true
///         await this.Invoke(() => Console.WriteLine("Executing"));
///     }
/// }
/// </code>
/// </example>
public interface IInvokable
{
	/// <summary>
	///    Gets or sets a value indicating whether to use the invoker for executing operations.
	/// </summary>
	/// <value>
	///    <c>true</c> to use the invoker for thread marshalling; <c>false</c> to execute directly
	///    on the calling thread. Default is typically <c>true</c>.
	/// </value>
	/// <remarks>
	///    When <c>true</c>, operations are routed through the registered <see cref="IInvoker"/> (typically
	///    a <see cref="DispatcherInvoker"/> in WPF applications) to ensure proper thread marshalling.
	///    When <c>false</c>, operations execute directly without any thread switching.
	/// </remarks>
	bool UseInvoker { get; set; }
	//IInvoker Invoker { get; }
}

/// <summary>
///    Defines a contract for invoking delegates, potentially with thread marshalling or other cross-cutting concerns.
/// </summary>
/// <remarks>
///    <para>
///       This interface provides a abstraction for executing delegates with optional thread marshalling.
///       Implementations can provide different invocation strategies such as synchronous execution
///       (<see cref="SynchronousInvoker"/>) or dispatcher-based execution (<see cref="DispatcherInvoker"/>).
///    </para>
///    <para>
///       The <see cref="DefaultSingletonInstanceAttribute"/> indicates that <see cref="SynchronousInvoker"/>
///       is the default implementation when no other invoker is registered with the singleton container.
///    </para>
///    <para>
///       <strong>Implementation strategies:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <see cref="SynchronousInvoker"/>: Executes delegates directly on the calling thread
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="DispatcherInvoker"/>: Marshals execution to a WPF dispatcher thread
///          </description>
///       </item>
///       <item>
///          <description>
///             Custom implementations: Could implement thread pool execution, task scheduling, etc.
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <code>
/// // Register a custom invoker
/// Singleton.Register&lt;IInvoker&gt;(new DispatcherInvoker(Application.Current.Dispatcher));
/// 
/// // Use the invoker
/// var invokable = new MyInvokableClass { UseInvoker = true };
/// var invoker = Singleton.Get&lt;IInvoker&gt;() ?? new SynchronousInvoker();
/// await invoker.InvokeActionDelegate(invokable, new Action(() => Console.WriteLine("Hello")));
/// </code>
/// </example>
[DefaultSingletonInstance(typeof(SynchronousInvoker))]
public interface IInvoker
{
	/// <summary>
	///    Invokes an action delegate asynchronously.
	/// </summary>
	/// <param name="invokable">
	///    The <see cref="IInvokable"/> instance that controls whether to use the invoker.
	/// </param>
	/// <param name="method">The delegate to invoke.</param>
	/// <param name="args">Optional arguments to pass to the delegate.</param>
	/// <returns>
	///    A <see cref="Task"/> representing the asynchronous invocation operation.
	/// </returns>
	/// <remarks>
	///    The implementation should respect the <paramref name="invokable"/>.<see cref="IInvokable.UseInvoker"/>
	///    property to determine whether to apply thread marshalling or execute directly.
	/// </remarks>
	Task InvokeActionDelegate(IInvokable invokable, Delegate method, params object?[] args);

	/// <summary>
	///    Invokes a function delegate asynchronously and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function delegate.</typeparam>
	/// <param name="invokable">
	///    The <see cref="IInvokable"/> instance that controls whether to use the invoker.
	/// </param>
	/// <param name="method">The function delegate to invoke.</param>
	/// <param name="args">Optional arguments to pass to the delegate.</param>
	/// <returns>
	///    A <see cref="Task{TResult}"/> representing the asynchronous invocation operation,
	///    containing the result returned by the delegate.
	/// </returns>
	/// <remarks>
	///    The implementation should respect the <paramref name="invokable"/>.<see cref="IInvokable.UseInvoker"/>
	///    property to determine whether to apply thread marshalling or execute directly.
	/// </remarks>
	Task<TResult> InvokeFuncDelegate<TResult>(IInvokable invokable, Delegate method, params object?[] args);
}

/// <summary>
///    Provides a synchronous implementation of <see cref="IInvoker"/> that executes delegates directly on the calling thread.
/// </summary>
/// <remarks>
///    <para>
///       This invoker performs no thread marshalling and executes all delegates synchronously on the calling thread.
///       It's the default implementation used when no other invoker is registered with the singleton container.
///    </para>
///    <para>
///       <strong>Key characteristics:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Synchronous execution:</strong> All delegates execute immediately on the calling thread
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>No marshalling:</strong> Does not switch threads or use dispatchers
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type safety:</strong> Validates return types and null handling for function delegates
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Default implementation:</strong> Used as fallback when no other invoker is configured
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Console applications where thread marshalling is not needed</description>
///       </item>
///       <item>
///          <description>Unit testing scenarios</description>
///       </item>
///       <item>
///          <description>Background operations that don't interact with UI</description>
///       </item>
///       <item>
///          <description>Default fallback when no dispatcher-based invoker is available</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <code>
/// var invoker = new SynchronousInvoker();
/// var invokable = new CustomInvokable { UseInvoker = true };
/// 
/// // Execute action
/// await invoker.InvokeActionDelegate(
///     invokable,
///     new Action(() => Console.WriteLine("Hello"))
/// );
/// 
/// // Execute function
/// var result = await invoker.InvokeFuncDelegate&lt;int&gt;(
///     invokable,
///     new Func&lt;int&gt;(() => 42)
/// );
/// Console.WriteLine(result); // Output: 42
/// </code>
/// </example>
public class SynchronousInvoker : IInvoker
{
	/// <summary>
	///    Invokes an action delegate synchronously by calling <see cref="Delegate.DynamicInvoke"/>.
	/// </summary>
	/// <param name="invokable">The invokable instance (not used in this implementation).</param>
	/// <param name="method">The delegate to invoke.</param>
	/// <param name="args">Arguments to pass to the delegate.</param>
	/// <returns>
	///    A completed <see cref="Task"/> wrapping the result of <see cref="Delegate.DynamicInvoke"/>.
	/// </returns>
	public Task InvokeActionDelegate(IInvokable invokable, Delegate method, params object?[] args) => Task.FromResult(method.DynamicInvoke(args));

	/// <summary>
	///    Invokes a function delegate synchronously and returns its result.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="invokable">The invokable instance (not used in this implementation).</param>
	/// <param name="method">The function delegate to invoke.</param>
	/// <param name="args">Arguments to pass to the delegate.</param>
	/// <returns>
	///    A completed <see cref="Task{TResult}"/> containing the function's return value.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///    Thrown when:
	///    <list type="bullet">
	///       <item>
	///          <description>The delegate returns <c>null</c> and <typeparamref name="TResult"/> is not nullable</description>
	///       </item>
	///       <item>
	///          <description>The delegate's return value cannot be cast to <typeparamref name="TResult"/></description>
	///       </item>
	///    </list>
	/// </exception>
	/// <remarks>
	///    This method performs type validation to ensure:
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If the return type is not nullable and the result is <c>null</c>, an exception is thrown
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             The return value can be safely cast to <typeparamref name="TResult"/>
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public Task<TResult> InvokeFuncDelegate<TResult>(IInvokable invokable, Delegate method, params object?[] args)
	{
		var res = method.DynamicInvoke(args);
		// NULLABLE - To test it
		if (!typeof(TResult).CanBeNull() && res == null)
			throw new InvalidOperationException($"Error in '{nameof(SynchronousInvoker)}', the invocation return null and the return type '{typeof(TResult).GetSignature()}' is not nullable.");
		if (res is not TResult) throw new InvalidOperationException($"Error in '{nameof(SynchronousInvoker)}', the invocation return value cannot be casted to type '{typeof(TResult).GetSignature()}'.");
		return Task.FromResult((TResult)res);
	}
}

/// <summary>
///    Provides extension methods for <see cref="IInvokable"/> to simplify delegate invocation through the registered <see cref="IInvoker"/>.
/// </summary>
/// <remarks>
///    <para>
///       This static class provides a fluent API for invoking actions and functions through the registered
///       <see cref="IInvoker"/> singleton. It automatically resolves the invoker from the singleton container
///       or falls back to <see cref="SynchronousInvoker"/> if no invoker is registered.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic invoker resolution:</strong> Retrieves invoker from <see cref="Singleton.Get{T}()"/> or uses <see cref="SynchronousInvoker"/> as fallback
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Strongly-typed overloads:</strong> Type-safe methods for 0-6 parameters for both actions and functions
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Action and Func support:</strong> Separate overloads for void delegates and returning delegates
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Async by default:</strong> All methods return <see cref="Task"/> or <see cref="Task{TResult}"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Clean syntax:</strong> Provides extension methods that eliminate boilerplate code
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Method coverage:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>7 overloads for <see cref="Action"/> delegates (0-6 parameters)</description>
///       </item>
///       <item>
///          <description>7 overloads for <see cref="Func{TResult}"/> delegates (0-6 parameters + return value)</description>
///       </item>
///    </list>
///    <para>
///       These extension methods are used extensively by <see cref="GenericCommand"/> and <see cref="GenericCommand{TParameter}"/>
///       to provide clean, readable command execution code with automatic thread marshalling.
///    </para>
/// </remarks>
/// <example>
///    <strong>Basic usage with IInvokable implementation:</strong>
///    <code>
/// public class MyViewModel : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task DoWorkAsync()
///     {
///         // Parameterless action
///         await this.Invoke(() => Console.WriteLine("Working"));
///         
///         // Action with parameter
///         await this.Invoke((string msg) => Console.WriteLine(msg), "Hello");
///         
///         // Function with return value
///         int result = await this.Invoke(() => 42);
///         
///         // Function with parameters
///         string text = await this.Invoke((int a, int b) => $"Sum: {a + b}", 10, 20);
///     }
/// }
/// </code>
///    <strong>With GenericCommand (internal usage):</strong>
///    <code>
/// public class CommandViewModel : INotifyPropertyChanged
/// {
///     public GenericCommand SaveCommand { get; }
///     
///     public CommandViewModel()
///     {
///         // GenericCommand uses these extension methods internally
///         SaveCommand = new GenericCommand(SaveData);
///     }
///     
///     private void SaveData()
///     {
///         // This gets invoked through the extension method
///         Console.WriteLine("Saving...");
///     }
/// }
/// </code>
///    <strong>Multiple parameters example:</strong>
///    <code>
/// public class CalculatorViewModel : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task&lt;double&gt; CalculateAsync()
///     {
///         // 1 parameter
///         await this.Invoke((int x) => Console.WriteLine($"Value: {x}"), 10);
///         
///         // 2 parameters
///         int sum = await this.Invoke((int a, int b) => a + b, 5, 3);
///         
///         // 3 parameters
///         int result = await this.Invoke(
///             (int a, int b, int c) => a + b + c, 
///             1, 2, 3
///         );
///         
///         // 4 parameters
///         string text = await this.Invoke(
///             (string a, string b, string c, string d) => $"{a} {b} {c} {d}",
///             "Hello", "from", "multiple", "params"
///         );
///         
///         return result;
///     }
/// }
/// </code>
///    <strong>Registering custom invoker:</strong>
///    <code>
/// // In App.xaml.cs or application startup
/// public partial class App : Application
/// {
///     protected override void OnStartup(StartupEventArgs e)
///     {
///         base.OnStartup(e);
///         
///         // Register DispatcherInvoker as the global invoker
///         Singleton.Register&lt;IInvoker&gt;(new DispatcherInvoker(Dispatcher));
///         
///         // Now all Invoke() calls will use DispatcherInvoker
///     }
/// }
/// 
/// // In ViewModel
/// public class MainViewModel : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task LoadDataAsync()
///     {
///         var data = await Task.Run(() => FetchData());
///         
///         // This will automatically marshal to UI thread via DispatcherInvoker
///         await this.Invoke(() =>
///         {
///             Items.Clear();
///             foreach (var item in data)
///                 Items.Add(item);
///         });
///     }
/// }
/// </code>
///    <strong>Conditional invocation:</strong>
///    <code>
/// public class ConfigurableViewModel : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task ProcessAsync(bool useDispatcher)
///     {
///         // Control whether to use invoker at runtime
///         UseInvoker = useDispatcher;
///         
///         // If UseInvoker is false, executes directly
///         // If true, uses registered invoker (e.g., DispatcherInvoker)
///         await this.Invoke(() => Console.WriteLine("Processing"));
///     }
/// }
/// </code>
///    <strong>All action overloads:</strong>
///    <code>
/// public class ActionExamples : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task DemonstrateActionsAsync()
///     {
///         // 0 parameters
///         await this.Invoke(() => Console.WriteLine("No params"));
///         
///         // 1 parameter
///         await this.Invoke((int x) => Console.WriteLine(x), 1);
///         
///         // 2 parameters
///         await this.Invoke((int x, int y) => Console.WriteLine($"{x}, {y}"), 1, 2);
///         
///         // 3 parameters
///         await this.Invoke((int x, int y, int z) => Console.WriteLine($"{x}, {y}, {z}"), 1, 2, 3);
///         
///         // 4 parameters
///         await this.Invoke((int a, int b, int c, int d) => 
///             Console.WriteLine($"{a}, {b}, {c}, {d}"), 1, 2, 3, 4);
///         
///         // 5 parameters
///         await this.Invoke((int a, int b, int c, int d, int e) => 
///             Console.WriteLine($"{a}, {b}, {c}, {d}, {e}"), 1, 2, 3, 4, 5);
///         
///         // 6 parameters
///         await this.Invoke((int a, int b, int c, int d, int e, int f) => 
///             Console.WriteLine($"{a}, {b}, {c}, {d}, {e}, {f}"), 1, 2, 3, 4, 5, 6);
///     }
/// }
/// </code>
///    <strong>All function overloads:</strong>
///    <code>
/// public class FunctionExamples : IInvokable
/// {
///     public bool UseInvoker { get; set; } = true;
///     
///     public async Task DemonstrateFunctionsAsync()
///     {
///         // 0 parameters
///         int result0 = await this.Invoke(() => 42);
///         
///         // 1 parameter
///         int result1 = await this.Invoke((int x) => x * 2, 10);
///         
///         // 2 parameters
///         int result2 = await this.Invoke((int x, int y) => x + y, 10, 20);
///         
///         // 3 parameters
///         int result3 = await this.Invoke((int x, int y, int z) => x + y + z, 1, 2, 3);
///         
///         // 4 parameters
///         int result4 = await this.Invoke((int a, int b, int c, int d) => 
///             a + b + c + d, 1, 2, 3, 4);
///         
///         // 5 parameters
///         int result5 = await this.Invoke((int a, int b, int c, int d, int e) => 
///             a + b + c + d + e, 1, 2, 3, 4, 5);
///         
///         // 6 parameters
///         int result6 = await this.Invoke((int a, int b, int c, int d, int e, int f) => 
///             a + b + c + d + e + f, 1, 2, 3, 4, 5, 6);
///     }
/// }
/// </code>
/// </example>
public static class IInvokerExtensions
{
	/// <summary>
	///    Invokes a parameterless action through the registered invoker.
	/// </summary>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	/// <remarks>
	///    Retrieves the invoker from <see cref="Singleton.Get{IInvoker}()"/> or uses <see cref="SynchronousInvoker"/> if none is registered.
	/// </remarks>
	public static Task Invoke(this IInvokable me, Action action) => (Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action);

	/// <summary>
	///    Invokes an action with one parameter through the registered invoker.
	/// </summary>
	/// <typeparam name="T">The type of the parameter.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <param name="param">The parameter to pass to the action.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static Task Invoke<T>(this IInvokable me, Action<T> action, T param) => (Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action, param);

	/// <summary>
	///    Invokes an action with two parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static Task Invoke<T1, T2>(this IInvokable me, Action<T1, T2> action, T1 param1, T2 param2) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action, param1, param2);

	/// <summary>
	///    Invokes an action with three parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static Task Invoke<T1, T2, T3>(this IInvokable me, Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action, param1, param2, param3);

	/// <summary>
	///    Invokes an action with four parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <param name="param4">The fourth parameter.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static Task Invoke<T1, T2, T3, T4>(this IInvokable me, Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action, param1, param2, param3, param4);

	/// <summary>
	///    Invokes an action with five parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth parameter.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <param name="param4">The fourth parameter.</param>
	/// <param name="param5">The fifth parameter.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static Task Invoke<T1, T2, T3, T4, T5>(this IInvokable me, Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action, param1, param2, param3, param4, param5);

	/// <summary>
	///    Invokes an action with six parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth parameter.</typeparam>
	/// <typeparam name="T6">The type of the sixth parameter.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="action">The action to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <param name="param4">The fourth parameter.</param>
	/// <param name="param5">The fifth parameter.</param>
	/// <param name="param6">The sixth parameter.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static Task Invoke<T1, T2, T3, T4, T5, T6>(this IInvokable me, Action<T1, T2, T3, T4, T5, T6> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeActionDelegate(me, action, param1, param2, param3, param4, param5, param6);

	/// <summary>
	///    Invokes a parameterless function through the registered invoker.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult> Invoke<TResult>(this IInvokable me, Func<TResult> func) => (Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func);

	/// <summary>
	///    Invokes a function with one parameter through the registered invoker.
	/// </summary>
	/// <typeparam name="T">The type of the parameter.</typeparam>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <param name="param">The parameter to pass to the function.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult> Invoke<T, TResult>(this IInvokable me, Func<T, TResult> func, T param) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func, param);

	/// <summary>
	///    Invokes a function with two parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult> Invoke<T1, T2, TResult>(this IInvokable me, Func<T1, T2, TResult> func, T1 param1, T2 param2) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func, param1, param2);

	/// <summary>
	///    Invokes a function with three parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult> Invoke<T1, T2, T3, TResult>(this IInvokable me, Func<T1, T2, T3, TResult> func, T1 param1, T2 param2, T3 param3) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func, param1, param2, param3);

	/// <summary>
	///    Invokes a function with four parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <param name="param4">The fourth parameter.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult> Invoke<T1, T2, T3, T4, TResult>(this IInvokable me, Func<T1, T2, T3, T4, TResult> func, T1 param1, T2 param2, T3 param3, T4 param4) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func, param1, param2, param3, param4);

	/// <summary>
	///    Invokes a function with five parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth parameter.</typeparam>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <param name="param4">The fourth parameter.</param>
	/// <param name="param5">The fifth parameter.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult> Invoke<T1, T2, T3, T4, T5, TResult>(this IInvokable me, Func<T1, T2, T3, T4, T5, TResult> func, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func, param1, param2, param3, param4, param5);

	/// <summary>
	///    Invokes a function with six parameters through the registered invoker.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter.</typeparam>
	/// <typeparam name="T2">The type of the second parameter.</typeparam>
	/// <typeparam name="T3">The type of the third parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth parameter.</typeparam>
	/// <typeparam name="T6">The type of the sixth parameter.</typeparam>
	/// <typeparam name="TResult">The return type of the function.</typeparam>
	/// <param name="me">The <see cref="IInvokable"/> instance.</param>
	/// <param name="func">The function to invoke.</param>
	/// <param name="param1">The first parameter.</param>
	/// <param name="param2">The second parameter.</param>
	/// <param name="param3">The third parameter.</param>
	/// <param name="param4">The fourth parameter.</param>
	/// <param name="param5">The fifth parameter.</param>
	/// <param name="param6">The sixth parameter.</param>
	/// <returns>A <see cref="Task{TResult}"/> containing the function's return value.</returns>
	public static Task<TResult>
		Invoke<T1, T2, T3, T4, T5, T6, TResult>(this IInvokable me, Func<T1, T2, T3, T4, T5, T6, TResult> func, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) =>
		(Singleton.Get<IInvoker>() ?? new SynchronousInvoker()).InvokeFuncDelegate<TResult>(me, func, param1, param2, param3, param4, param5, param6);
}