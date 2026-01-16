using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Fuxion.Windows.Documents;
using Fuxion.Windows.Input;

namespace Fuxion.Windows.Controls;

/// <summary>
///    Provides a window for displaying unhandled exceptions with options to close, restart, or send error reports.
/// </summary>
/// <remarks>
///    <para>
///       This WPF window is designed to present unhandled exceptions to users in a user-friendly manner,
///       offering various actions such as closing the application, restarting it, or sending an error report.
///       It automatically formats exception details using <see cref="FlowDocument"/> for easy reading.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Exception display:</strong> Shows exception type and formatted details in a FlowDocument
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible actions:</strong> Configurable buttons for closing or restarting the application
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Details toggle:</strong> Optional expandable section to show full exception details
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Error reporting:</strong> Optional callback to send error reports to a server or logging service
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Adaptive UI:</strong> Changes window style and size based on whether details are shown
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Display global unhandled exceptions from Application.DispatcherUnhandledException</description>
///       </item>
///       <item>
///          <description>Show task exceptions from async operations</description>
///       </item>
///       <item>
///          <description>Provide user-friendly error dialogs with recovery options</description>
///       </item>
///       <item>
///          <description>Collect and send error reports for debugging</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic usage in App.xaml.cs:</strong>
///    <code>
/// public partial class App : Application
/// {
///     protected override void OnStartup(StartupEventArgs e)
///     {
///         base.OnStartup(e);
///         
///         // Handle unhandled exceptions
///         DispatcherUnhandledException += App_DispatcherUnhandledException;
///     }
///     
///     private void App_DispatcherUnhandledException(object sender, 
///         DispatcherUnhandledExceptionEventArgs e)
///     {
///         // Show exception window
///         var window = new UnhandledExceptionWindow(
///             e.Exception,
///             showDetails: true,
///             sendReportFunc: null
///         )
///         {
///             Buttons = UnhandledExceptionWindowButtons.CloseWindowAndRestartApplication,
///             Message = "An unexpected error occurred. You can restart the application."
///         };
///         
///         window.ShowDialog();
///         
///         // Mark as handled to prevent application crash
///         e.Handled = true;
///     }
/// }
/// </code>
///    <strong>With error reporting:</strong>
///    <code>
/// private void App_DispatcherUnhandledException(object sender, 
///     DispatcherUnhandledExceptionEventArgs e)
/// {
///     var window = new UnhandledExceptionWindow(
///         e.Exception,
///         showDetails: true,
///         sendReportFunc: async () =>
///         {
///             // Send error report to server
///             var client = new HttpClient();
///             var report = new
///             {
///                 Exception = e.Exception.ToString(),
///                 MachineName = Environment.MachineName,
///                 Timestamp = DateTime.UtcNow
///             };
///             
///             await client.PostAsJsonAsync(
///                 "https://api.example.com/error-reports", 
///                 report
///             );
///         }
///     )
///     {
///         Buttons = UnhandledExceptionWindowButtons.CloseWindow,
///         Message = "An error occurred. Would you like to send a report?"
///     };
///     
///     window.ShowDialog();
///     e.Handled = true;
/// }
/// </code>
///    <strong>Task exception handling:</strong>
///    <code>
/// TaskScheduler.UnobservedTaskException += (sender, e) =>
/// {
///     Application.Current.Dispatcher.Invoke(() =>
///     {
///         var window = new UnhandledExceptionWindow(
///             e.Exception,
///             showDetails: true
///         )
///         {
///             Buttons = UnhandledExceptionWindowButtons.CloseWindow,
///             Message = "A background task encountered an error."
///         };
///         
///         window.ShowDialog();
///     });
///     
///     e.SetObserved();
/// };
/// </code>
///    <strong>Custom buttons configuration:</strong>
///    <code>
/// // Only show close button
/// var window1 = new UnhandledExceptionWindow(exception, true)
/// {
///     Buttons = UnhandledExceptionWindowButtons.CloseWindow
/// };
/// 
/// // Only show restart button
/// var window2 = new UnhandledExceptionWindow(exception, true)
/// {
///     Buttons = UnhandledExceptionWindowButtons.RestartApplication
/// };
/// 
/// // Show both buttons
/// var window3 = new UnhandledExceptionWindow(exception, true)
/// {
///     Buttons = UnhandledExceptionWindowButtons.CloseWindowAndRestartApplication
/// };
/// </code>
///    <strong>Hide details option:</strong>
///    <code>
/// // Show simplified error without details toggle
/// var window = new UnhandledExceptionWindow(
///     exception,
///     showDetails: false  // User cannot see full exception details
/// )
/// {
///     Message = "An error occurred. Please contact support."
/// };
/// 
/// window.ShowDialog();
/// </code>
/// </example>
public partial class UnhandledExceptionWindow : Window, INotifyPropertyChanged
{
	/// <summary>
	///    Initializes a new instance of the <see cref="UnhandledExceptionWindow"/> class.
	/// </summary>
	/// <param name="ex">The exception to display.</param>
	/// <param name="showDetails">
	///    <c>true</c> to allow users to toggle detailed exception view; <c>false</c> to hide the details option.
	/// </param>
	/// <param name="sendReportFunc">
	///    Optional asynchronous function to send an error report. When provided, a "Send Report" button is shown.
	///    If <c>null</c>, the send report functionality is disabled.
	/// </param>
	/// <remarks>
	///    <para>
	///       The constructor initializes all commands and populates the <see cref="Document"/> with formatted
	///       exception information using the <see cref="FlowDocumentExtensions.ToBlocks"/> extension method.
	///    </para>
	///    <para>
	///       <strong>Commands initialized:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="IgnoreCommand"/>: Closes the window</description>
	///       </item>
	///       <item>
	///          <description><see cref="CloseConsoleCommand"/>: Shuts down the application</description>
	///       </item>
	///       <item>
	///          <description><see cref="RestartApplicationCommand"/>: Restarts the application</description>
	///       </item>
	///       <item>
	///          <description><see cref="ShowDetailsCommand"/>: Toggles exception details visibility</description>
	///       </item>
	///       <item>
	///          <description><see cref="SendReportCommand"/>: Sends error report (if function provided)</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <exception cref="Exception">
	///    May throw exceptions from <paramref name="sendReportFunc"/> which are caught and displayed to the user.
	/// </exception>
	public UnhandledExceptionWindow(Exception ex, bool showDetails, Func<Task>? sendReportFunc = null)
	{
		_CanShowDetails = showDetails;
		this.ex = ex;
		this.sendReportFunc = sendReportFunc;
		// Create commands
		IgnoreCommand = new(() => Close(), () => Buttons.HasFlag(UnhandledExceptionWindowButtons.CloseWindow));
		CloseConsoleCommand = new(() => Application.Current.Shutdown(), () => Buttons.HasFlag(UnhandledExceptionWindowButtons.CloseWindow));
		RestartApplicationCommand = new(() => {
			Process.Start(Application.ResourceAssembly.Location);
			Application.Current.Shutdown();
		}, () => Buttons.HasFlag(UnhandledExceptionWindowButtons.RestartApplication));
		ShowDetailsCommand = new(() => ShowDetails = !ShowDetails, () => CanShowDetails);
		SendReportCommand = new(async () => {
			try
			{
				SendingReport = true;
				if (sendReportFunc != null) await sendReportFunc();
				sendReportFunc = null;
				SendReportCommand?.RaiseCanExecuteChanged();
			} catch (Exception ex2)
			{
				MessageBox.Show(this, ex2.Message, "Error al enviar el mensaje", MessageBoxButton.OK, MessageBoxImage.Error);
				Debug.WriteLine("");
			} finally
			{
				SendingReport = false;
			}
		}, () => sendReportFunc != null);
		// Inicializar interfaz
		InitializeComponent();
		// Populate Document
		Document.Blocks.AddRange(ex.ToBlocks());
	}
	readonly Exception ex;
	bool _CanShowDetails;
	UnhandledExceptionWindowButtons _Commands;
	string? _Message;
	bool _SendingReport;
	Func<Task>? sendReportFunc;

	/// <summary>
	///    Gets the full type name of the exception being displayed.
	/// </summary>
	/// <value>
	///    The fully qualified type name of the exception (e.g., "System.InvalidOperationException").
	/// </value>
	/// <exception cref="InvalidProgramException">
	///    Thrown if the exception type doesn't have a <see cref="Type.FullName"/>.
	/// </exception>
	public string ExceptionType => ex.GetType().FullName ?? throw new InvalidProgramException($"Type '{ex.GetType().Name}' hasn't FullName");

	/// <summary>
	///    Gets or sets a custom message to display to the user.
	/// </summary>
	/// <value>
	///    A user-friendly message explaining the error or suggesting actions. Can be <c>null</c>.
	/// </value>
	/// <remarks>
	///    This property raises <see cref="PropertyChanged"/> when set. Use this to provide context-specific
	///    messages like "The database connection failed" or "Unable to save your changes."
	/// </remarks>
	public string? Message
	{
		get => _Message;
		set
		{
			_Message = value;
			PropertyChanged?.Invoke(this, new(nameof(Message)));
		}
	}

	/// <summary>
	///    Gets or sets a value indicating whether an error report is currently being sent.
	/// </summary>
	/// <value>
	///    <c>true</c> if a report is being sent; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	///    This property is used to show a loading indicator during report submission and raises
	///    <see cref="PropertyChanged"/> when set.
	/// </remarks>
	public bool SendingReport
	{
		get => _SendingReport;
		set
		{
			_SendingReport = value;
			PropertyChanged?.Invoke(this, new(nameof(SendingReport)));
		}
	}

	/// <summary>
	///    Gets or sets which action buttons are available in the window.
	/// </summary>
	/// <value>
	///    A <see cref="UnhandledExceptionWindowButtons"/> flags value indicating which buttons to show.
	/// </value>
	/// <remarks>
	///    <para>
	///       When this property is set, it triggers <see cref="GenericCommand.RaiseCanExecuteChanged"/> on all
	///       action commands to update button visibility states.
	///    </para>
	///    <para>
	///       Common configurations:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="UnhandledExceptionWindowButtons.CloseWindow"/>: Only show close button
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="UnhandledExceptionWindowButtons.RestartApplication"/>: Only show restart button
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="UnhandledExceptionWindowButtons.CloseWindowAndRestartApplication"/>: Show both buttons
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public UnhandledExceptionWindowButtons Buttons
	{
		get => _Commands;
		set
		{
			_Commands = value;
			IgnoreCommand.RaiseCanExecuteChanged();
			RestartApplicationCommand.RaiseCanExecuteChanged();
			CloseConsoleCommand.RaiseCanExecuteChanged();
		}
	}

	/// <summary>
	///    Gets or sets a value indicating whether users can toggle the detailed exception view.
	/// </summary>
	/// <value>
	///    <c>true</c> to allow showing/hiding details; <c>false</c> to disable the details toggle.
	/// </value>
	/// <remarks>
	///    When set, this property triggers <see cref="GenericCommand.RaiseCanExecuteChanged"/> on
	///    <see cref="ShowDetailsCommand"/> to update the details button state.
	/// </remarks>
	public bool CanShowDetails
	{
		get => _CanShowDetails;
		set
		{
			_CanShowDetails = value;
			ShowDetailsCommand.RaiseCanExecuteChanged();
		}
	}

	/// <summary>
	///    Gets or sets a value indicating whether the detailed exception view is currently shown.
	/// </summary>
	/// <value>
	///    <c>true</c> if details are shown; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	///    <para>
	///       When details are shown, the window:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Changes <see cref="Window.ResizeMode"/> to <see cref="ResizeMode.CanResizeWithGrip"/></description>
	///       </item>
	///       <item>
	///          <description>Sets <see cref="Window.SizeToContent"/> to <see cref="SizeToContent.Manual"/></description>
	///       </item>
	///       <item>
	///          <description>Sets <see cref="Window.WindowStyle"/> to <see cref="WindowStyle.SingleBorderWindow"/></description>
	///       </item>
	///       <item>
	///          <description>Maximizes the window (<see cref="Window.WindowState"/> = <see cref="WindowState.Maximized"/>)</description>
	///       </item>
	///    </list>
	///    <para>
	///       When details are hidden, the window:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>Changes <see cref="Window.ResizeMode"/> to <see cref="ResizeMode.NoResize"/></description>
	///       </item>
	///       <item>
	///          <description>Sets <see cref="Window.SizeToContent"/> to <see cref="SizeToContent.WidthAndHeight"/></description>
	///       </item>
	///       <item>
	///          <description>Sets <see cref="Window.WindowStyle"/> to <see cref="WindowStyle.None"/></description>
	///       </item>
	///       <item>
	///          <description>Normalizes the window (<see cref="Window.WindowState"/> = <see cref="WindowState.Normal"/>)</description>
	///       </item>
	///    </list>
	/// </remarks>
	public bool ShowDetails
	{
		get => ResizeMode == ResizeMode.CanResizeWithGrip;
		set
		{
			ResizeMode = value ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;
			SizeToContent = value ? SizeToContent.Manual : SizeToContent.WidthAndHeight;
			WindowStyle = value ? WindowStyle.SingleBorderWindow : WindowStyle.None;
			WindowState = value ? WindowState.Maximized : WindowState.Normal;
			PropertyChanged?.Invoke(this, new(nameof(ShowDetails)));
		}
	}

	/// <summary>
	///    Gets the command that sends an error report.
	/// </summary>
	/// <value>
	///    A <see cref="GenericCommand"/> that executes the <c>sendReportFunc</c> provided in the constructor.
	/// </value>
	/// <remarks>
	///    <para>
	///       This command is only enabled when a <c>sendReportFunc</c> was provided in the constructor.
	///       After successfully sending a report, the function is set to <c>null</c> and the command
	///       is disabled to prevent duplicate reports.
	///    </para>
	///    <para>
	///       If an exception occurs during report sending, a <see cref="MessageBox"/> is shown with the error.
	///    </para>
	/// </remarks>
	public GenericCommand SendReportCommand { get; }

	/// <summary>
	///    Gets the command that toggles the visibility of exception details.
	/// </summary>
	/// <value>
	///    A <see cref="GenericCommand"/> that toggles <see cref="ShowDetails"/>.
	/// </value>
	/// <remarks>
	///    This command is only enabled when <see cref="CanShowDetails"/> is <c>true</c>.
	/// </remarks>
	public GenericCommand ShowDetailsCommand { get; }

	/// <summary>
	///    Gets the command that closes the window without shutting down the application.
	/// </summary>
	/// <value>
	///    A <see cref="GenericCommand"/> that calls <see cref="Window.Close"/>.
	/// </value>
	/// <remarks>
	///    This command is enabled when <see cref="Buttons"/> has the <see cref="UnhandledExceptionWindowButtons.CloseWindow"/> flag.
	/// </remarks>
	public GenericCommand IgnoreCommand { get; }

	/// <summary>
	///    Gets the command that shuts down the entire application.
	/// </summary>
	/// <value>
	///    A <see cref="GenericCommand"/> that calls <see cref="Application.Shutdown()"/>.
	/// </value>
	/// <remarks>
	///    This command is enabled when <see cref="Buttons"/> has the <see cref="UnhandledExceptionWindowButtons.CloseWindow"/> flag.
	/// </remarks>
	public GenericCommand CloseConsoleCommand { get; }

	/// <summary>
	///    Gets the command that restarts the application.
	/// </summary>
	/// <value>
	///    A <see cref="GenericCommand"/> that starts a new instance of the application and shuts down the current one.
	/// </value>
	/// <remarks>
	///    <para>
	///       This command:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Starts a new process using <see cref="Application.ResourceAssembly"/>.Location</description>
	///       </item>
	///       <item>
	///          <description>Calls <see cref="Application.Shutdown()"/> on the current application</description>
	///       </item>
	///    </list>
	///    <para>
	///       This command is enabled when <see cref="Buttons"/> has the <see cref="UnhandledExceptionWindowButtons.RestartApplication"/> flag.
	///    </para>
	/// </remarks>
	public GenericCommand RestartApplicationCommand { get; }

	/// <summary>
	///    Gets or sets the <see cref="FlowDocument"/> containing the formatted exception details.
	/// </summary>
	/// <value>
	///    A <see cref="FlowDocument"/> with exception information formatted using <see cref="FlowDocumentExtensions.ToBlocks"/>.
	/// </value>
	/// <remarks>
	///    The document is automatically populated in the constructor and uses Segoe UI font with 10px padding.
	/// </remarks>
	public FlowDocument Document { get; set; } = new() {
		TextAlignment = TextAlignment.Left, PagePadding = new(10), FontFamily = new("Segoe UI")
	};

	/// <summary>
	///    Occurs when a property value changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	///    Handles the close button click event by opening the close buttons popup.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">Event data.</param>
	/// <remarks>
	///    This event handler opens the <c>CloseButtonsPopUp</c> to show close/restart options.
	/// </remarks>
	void ButtonClose_Click(object sender, RoutedEventArgs e) => CloseButtonsPopUp.IsOpen = true;
}

/// <summary>
///    Specifies which action buttons are available in the <see cref="UnhandledExceptionWindow"/>.
/// </summary>
/// <remarks>
///    This enum uses the <see cref="FlagsAttribute"/> to allow combining multiple button options
///    using bitwise operations.
/// </remarks>
[Flags]
public enum UnhandledExceptionWindowButtons
{
	/// <summary>
	///    Shows the "Close Window" button, allowing the user to close the error window without shutting down the application.
	/// </summary>
	CloseWindow = 1,

	/// <summary>
	///    Shows the "Restart Application" button, allowing the user to restart the application.
	/// </summary>
	RestartApplication = 2,

	/// <summary>
	///    Shows both "Close Window" and "Restart Application" buttons.
	/// </summary>
	/// <remarks>
	///    This is a combination of <see cref="CloseWindow"/> and <see cref="RestartApplication"/> flags.
	/// </remarks>
	CloseWindowAndRestartApplication = 3
}