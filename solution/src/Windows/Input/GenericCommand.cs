using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Fuxion.Reflection;
using Fuxion.Windows.Threading;

namespace Fuxion.Windows.Input;

/// <summary>
///    Provides a generic implementation of <see cref="ICommand"/> for parameterless commands with async support.
/// </summary>
/// <remarks>
///    <para>
///       This class implements the command pattern for WPF applications with built-in support for asynchronous
///       execution via the <see cref="IInvokable"/> interface. It's designed for commands that don't require
///       parameters and provides optional <c>CanExecute</c> logic.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Async support:</strong> Both synchronous and asynchronous execution methods
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Optional CanExecute:</strong> Specify a function to determine if the command can execute
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Invoker integration:</strong> Uses <see cref="IInvokable"/> for dispatcher-aware execution
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>CanExecuteChanged notification:</strong> Manual triggering via <see cref="RaiseCanExecuteChanged"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Simple construction:</strong> Easy setup with action delegates
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Simple button commands without parameters</description>
///       </item>
///       <item>
///          <description>Menu commands and toolbar actions</description>
///       </item>
///       <item>
///          <description>Asynchronous operations triggered by UI elements</description>
///       </item>
///       <item>
///          <description>Commands with conditional execution logic</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Simple command without CanExecute:</strong>
///    <code>
/// public class MainViewModel : INotifyPropertyChanged
/// {
///     public ICommand SaveCommand { get; }
///     
///     public MainViewModel()
///     {
///         SaveCommand = new GenericCommand(SaveData);
///     }
///     
///     private void SaveData()
///     {
///         // Save logic here
///         Console.WriteLine("Data saved");
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <Button Content="Save" Command="{Binding SaveCommand}"/>
/// ]]></code>
///    <strong>Command with CanExecute logic:</strong>
///    <code>
/// public class EditorViewModel : INotifyPropertyChanged
/// {
///     private bool _isDirty;
///     
///     public bool IsDirty
///     {
///         get => _isDirty;
///         set
///         {
///             _isDirty = value;
///             OnPropertyChanged();
///             SaveCommand.RaiseCanExecuteChanged();
///         }
///     }
///     
///     public GenericCommand SaveCommand { get; }
///     
///     public EditorViewModel()
///     {
///         SaveCommand = new GenericCommand(
///             action: Save,
///             canExecute: () => IsDirty
///         );
///     }
///     
///     private void Save()
///     {
///         // Save logic
///         IsDirty = false;
///     }
/// }
/// </code>
///    <strong>Async command:</strong>
///    <code>
/// public class DataViewModel : INotifyPropertyChanged
/// {
///     public GenericCommand LoadDataCommand { get; }
///     
///     public DataViewModel()
///     {
///         LoadDataCommand = new GenericCommand(LoadDataAsync);
///     }
///     
///     private async void LoadDataAsync()
///     {
///         await Task.Delay(1000); // Simulate loading
///         // Load data logic
///     }
/// }
/// </code>
/// </example>
/// <remarks>
///    Initializes a new instance of the <see cref="GenericCommand"/> class.
/// </remarks>
/// <param name="action">The action to execute when the command is invoked.</param>
/// <param name="canExecute">
///    Optional function to determine if the command can execute. If <c>null</c>, the command can always execute.
/// </param>
/// <remarks>
///    The <paramref name="action"/> is executed via the <see cref="IInvokable"/> interface, which ensures
///    it runs on the appropriate thread (typically the UI thread in WPF applications).
/// </remarks>
/// <example>
///    <code>
/// // Simple command
/// var command = new GenericCommand(() => Console.WriteLine("Executed"));
/// 
/// // Command with CanExecute
/// var conditionalCommand = new GenericCommand(
///     action: () => Console.WriteLine("Executed"),
///     canExecute: () => someCondition
/// );
/// </code>
/// </example>
public class GenericCommand(Action action, Func<bool>? canExecute = null) : ICommand, IInvokable
{

   /// <summary>
   ///    Occurs when changes occur that affect whether or not the command should execute.
   /// </summary>
   /// <remarks>
   ///    This event must be manually raised by calling <see cref="RaiseCanExecuteChanged"/> when
   ///    conditions that affect <c>CanExecute</c> change. Unlike some command implementations,
   ///    this does not automatically hook into <see cref="System.Windows.Input.CommandManager.RequerySuggested"/>.
   /// </remarks>
   public event EventHandler? CanExecuteChanged;

	/// <summary>
	///    Defines the method that determines whether the command can execute in its current state.
	/// </summary>
	/// <param name="parameter">
	///    Data used by the command. This parameter is ignored for parameterless commands.
	/// </param>
	/// <returns>
	///    <c>true</c> if the command can execute; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	///    This is the explicit implementation of <see cref="ICommand.CanExecute(object)"/>.
	///    It ignores the parameter and delegates to the synchronous <see cref="CanExecute()"/> method.
	/// </remarks>
	bool ICommand.CanExecute(object? parameter) => CanExecute();

	/// <summary>
	///    Defines the method to be called when the command is invoked.
	/// </summary>
	/// <param name="parameter">
	///    Data used by the command. This parameter is ignored for parameterless commands.
	/// </param>
	/// <remarks>
	///    This is the explicit implementation of <see cref="ICommand.Execute(object)"/>.
	///    It ignores the parameter and delegates to the synchronous <see cref="Execute()"/> method.
	/// </remarks>
	void ICommand.Execute(object? parameter) => Execute();

	/// <summary>
	///    Gets or sets whether to use the invoker for dispatching execution to the UI thread.
	/// </summary>
	/// <value>
	///    <c>true</c> to use the invoker (default); <c>false</c> to execute directly without dispatching.
	/// </value>
	/// <remarks>
	///    When <c>true</c>, the command execution and CanExecute checks are dispatched via the
	///    <see cref="IInvokable"/> interface, ensuring they run on the appropriate thread (usually the UI thread).
	/// </remarks>
	bool IInvokable.UseInvoker { get; set; } = true;

	/// <summary>
	///    Asynchronously determines whether the command can execute.
	/// </summary>
	/// <returns>
	///    A task that represents the asynchronous operation. The task result contains <c>true</c> if
	///    the command can execute; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	///    If no <c>canExecute</c> function was provided in the constructor, this method returns <c>true</c>.
	///    Otherwise, it invokes the <c>canExecute</c> function via the <see cref="IInvokable"/> interface.
	/// </remarks>
	/// <example>
	///    <code>
	/// var command = new GenericCommand(() => { }, () => someCondition);
	/// bool canExecute = await command.CanExecuteAsync();
	/// </code>
	/// </example>
	public Task<bool> CanExecuteAsync() => canExecute != null ? this.Invoke(canExecute) : Task.FromResult(true);

	/// <summary>
	///    Synchronously determines whether the command can execute.
	/// </summary>
	/// <returns>
	///    <c>true</c> if the command can execute; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	///    This method blocks until <see cref="CanExecuteAsync"/> completes and returns its result.
	/// </remarks>
	public bool CanExecute() => CanExecuteAsync().Result;

	/// <summary>
	///    Asynchronously raises the <see cref="CanExecuteChanged"/> event.
	/// </summary>
	/// <returns>
	///    A task that represents the asynchronous operation.
	/// </returns>
	/// <remarks>
	///    This method invokes the event via the <see cref="IInvokable"/> interface to ensure it's
	///    raised on the UI thread.
	/// </remarks>
	public Task RaiseCanExecuteChangedAsync() => this.Invoke(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));

	/// <summary>
	///    Synchronously raises the <see cref="CanExecuteChanged"/> event.
	/// </summary>
	/// <remarks>
	///    Call this method when properties or conditions change that affect the result of <c>CanExecute</c>.
	///    This method blocks until <see cref="RaiseCanExecuteChangedAsync"/> completes.
	/// </remarks>
	/// <example>
	///    <code>
	/// public bool IsDataLoaded
	/// {
	///     get => _isDataLoaded;
	///     set
	///     {
	///         _isDataLoaded = value;
	///         OnPropertyChanged();
	///         SaveCommand.RaiseCanExecuteChanged(); // Update command state
	///     }
	/// }
	/// </code>
	/// </example>
	public void RaiseCanExecuteChanged() => RaiseCanExecuteChangedAsync().Wait();

	/// <summary>
	///    Asynchronously executes the command.
	/// </summary>
	/// <returns>
	///    A task that represents the asynchronous execution operation.
	/// </returns>
	/// <remarks>
	///    The action is invoked via the <see cref="IInvokable"/> interface to ensure it executes
	///    on the appropriate thread.
	/// </remarks>
	public Task ExecuteAsync() => this.Invoke(action);

	/// <summary>
	///    Synchronously executes the command.
	/// </summary>
	/// <remarks>
	///    This method blocks until <see cref="ExecuteAsync"/> completes.
	/// </remarks>
	public void Execute() => ExecuteAsync().Wait();
}

/// <summary>
///    Provides a generic implementation of <see cref="ICommand"/> for commands with a typed parameter and async support.
/// </summary>
/// <typeparam name="TParameter">The type of the command parameter.</typeparam>
/// <remarks>
///    <para>
///       This class implements the command pattern for WPF applications with built-in support for strongly-typed
///       parameters and asynchronous execution via the <see cref="IInvokable"/> interface. It provides type safety
///       for command parameters and includes optional <c>CanExecute</c> logic.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Type-safe parameters:</strong> Strongly-typed command parameter with automatic casting and validation
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Async support:</strong> Both synchronous and asynchronous execution methods
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Optional CanExecute:</strong> Specify a function to determine if the command can execute based on parameter
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Automatic validation and support for nullable parameter types
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type conversion safety:</strong> Throws descriptive exceptions for invalid parameter types
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Commands bound to items in collections (e.g., Delete, Edit commands)</description>
///       </item>
///       <item>
///          <description>Context menu commands with specific data</description>
///       </item>
///       <item>
///          <description>Commands that need information from the UI element</description>
///       </item>
///       <item>
///          <description>Type-safe command parameter passing in MVVM patterns</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Simple parameterized command:</strong>
///    <code>
/// public class ItemViewModel : INotifyPropertyChanged
/// {
///     public ObservableCollection&lt;Item&gt; Items { get; } = new();
///     public ICommand DeleteItemCommand { get; }
///     
///     public ItemViewModel()
///     {
///         DeleteItemCommand = new GenericCommand&lt;Item&gt;(DeleteItem);
///     }
///     
///     private void DeleteItem(Item item)
///     {
///         Items.Remove(item);
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <ListBox ItemsSource="{Binding Items}">
///     <ListBox.ItemTemplate>
///         <DataTemplate>
///             <StackPanel Orientation="Horizontal">
///                 <TextBlock Text="{Binding Name}"/>
///                 <Button Content="Delete" 
///                         Command="{Binding DataContext.DeleteItemCommand, RelativeSource={RelativeSource AncestorType=ListBox}}"
///                         CommandParameter="{Binding}"/>
///             </StackPanel>
///         </DataTemplate>
///     </ListBox.ItemTemplate>
/// </ListBox>
/// ]]></code>
///    <strong>Command with CanExecute based on parameter:</strong>
///    <code>
/// public class DocumentViewModel
/// {
///     public ICommand OpenDocumentCommand { get; }
///     
///     public DocumentViewModel()
///     {
///         OpenDocumentCommand = new GenericCommand&lt;string&gt;(
///             action: OpenDocument,
///             canExecute: path => !string.IsNullOrEmpty(path) &amp;&amp; File.Exists(path)
///         );
///     }
///     
///     private void OpenDocument(string path)
///     {
///         // Open document logic
///     }
/// }
/// </code>
/// </example>
public class GenericCommand<TParameter>(Action<TParameter> action, Func<TParameter, bool>? canExecute = null) : ICommand, IInvokable
{
	/// <summary>
	///    Occurs when changes occur that affect whether or not the command should execute.
	/// </summary>
	public event EventHandler? CanExecuteChanged;

	/// <summary>
	///    Defines the method that determines whether the command can execute in its current state.
	/// </summary>
	/// <param name="parameter">Data used by the command.</param>
	/// <returns>
	///    <c>true</c> if the command can execute; otherwise, <c>false</c>.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///    Thrown when <paramref name="parameter"/> is <c>null</c> and <typeparamref name="TParameter"/> is not nullable.
	/// </exception>
	/// <exception cref="InvalidCastException">
	///    Thrown when <paramref name="parameter"/> cannot be cast to <typeparamref name="TParameter"/>.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method performs type checking and conversion:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>If parameter is null and <typeparamref name="TParameter"/> is nullable, passes <c>default</c></description>
	///       </item>
	///       <item>
	///          <description>If parameter is null and <typeparamref name="TParameter"/> is not nullable, throws <see cref="InvalidOperationException"/></description>
	///       </item>
	///       <item>
	///          <description>If parameter can be cast to <typeparamref name="TParameter"/>, performs the cast</description>
	///       </item>
	///       <item>
	///          <description>Otherwise, throws <see cref="InvalidCastException"/> with descriptive message</description>
	///       </item>
	///    </list>
	/// </remarks>
	bool ICommand.CanExecute(object? parameter)
	{
		if (parameter is null) throw new InvalidOperationException($"The parameter '{nameof(parameter)}' cannot be null");
		if (parameter is TParameter par) return CanExecute(par);
		if (typeof(TParameter).CanBeNull()) return CanExecute(default!);
		throw new InvalidCastException($"The parameter of type '{parameter.GetType().Name}' couldn't casted to '{typeof(TParameter).Name}' as was declared for command parameter.");
	}

	/// <summary>
	///    Defines the method to be called when the command is invoked.
	/// </summary>
	/// <param name="parameter">Data used by the command.</param>
	/// <exception cref="InvalidOperationException">
	///    Thrown when <paramref name="parameter"/> is <c>null</c> and <typeparamref name="TParameter"/> is not nullable.
	/// </exception>
	/// <exception cref="InvalidCastException">
	///    Thrown when <paramref name="parameter"/> cannot be cast to <typeparamref name="TParameter"/>.
	/// </exception>
	/// <remarks>
	///    This method performs the same type checking and conversion as <see cref="ICommand.CanExecute(object)"/>
	///    before invoking the command action.
	/// </remarks>
	void ICommand.Execute(object? parameter)
	{
		if (parameter is null) throw new InvalidOperationException($"The parameter '{nameof(parameter)}' cannot be null");
		if (parameter is TParameter par)
			Execute(par);
		else if (typeof(TParameter).CanBeNull() && parameter == null)
			Execute(default!);
		else
			throw new InvalidCastException($"The parameter of type '{parameter.GetType().Name}' couldn't casted to '{typeof(TParameter).Name}' as was declared for command parameter.");
	}

	/// <summary>
	///    Gets or sets whether to use the invoker for dispatching execution to the UI thread.
	/// </summary>
	bool IInvokable.UseInvoker { get; set; } = true;

	/// <summary>
	///    Asynchronously determines whether the command can execute with the specified parameter.
	/// </summary>
	/// <param name="parameter">The typed parameter to evaluate.</param>
	/// <returns>
	///    A task that represents the asynchronous operation. The task result contains <c>true</c> if
	///    the command can execute; otherwise, <c>false</c>.
	/// </returns>
	public Task<bool> CanExecuteAsync(TParameter parameter) => canExecute != null ? this.Invoke(canExecute, parameter) : Task.FromResult(true);

	/// <summary>
	///    Synchronously determines whether the command can execute with the specified parameter.
	/// </summary>
	/// <param name="parameter">The typed parameter to evaluate.</param>
	/// <returns>
	///    <c>true</c> if the command can execute; otherwise, <c>false</c>.
	/// </returns>
	public bool CanExecute(TParameter parameter) => CanExecuteAsync(parameter).Result;

	/// <summary>
	///    Asynchronously raises the <see cref="CanExecuteChanged"/> event.
	/// </summary>
	/// <returns>
	///    A task that represents the asynchronous operation.
	/// </returns>
	public Task RaiseCanExecuteChangedAsync() => this.Invoke(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));

	/// <summary>
	///    Synchronously raises the <see cref="CanExecuteChanged"/> event.
	/// </summary>
	/// <remarks>
	///    Call this method when properties or conditions change that affect the result of <c>CanExecute</c>.
	/// </remarks>
	public void RaiseCanExecuteChanged() => RaiseCanExecuteChangedAsync().Wait();

	/// <summary>
	///    Asynchronously executes the command with the specified parameter.
	/// </summary>
	/// <param name="parameter">The typed parameter to pass to the command action.</param>
	/// <returns>
	///    A task that represents the asynchronous execution operation.
	/// </returns>
	public Task ExecuteAsync(TParameter parameter) => this.Invoke(action, parameter);

	/// <summary>
	///    Synchronously executes the command with the specified parameter.
	/// </summary>
	/// <param name="parameter">The typed parameter to pass to the command action.</param>
	public void Execute(TParameter parameter) => ExecuteAsync(parameter).Wait();
}