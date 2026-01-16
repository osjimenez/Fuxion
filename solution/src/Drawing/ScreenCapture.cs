using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Fuxion.Drawing;

/// <summary>
///    Provides functionality to capture screenshots of the desktop, active windows, or specific windows.
/// </summary>
/// <remarks>
///    <para>
///       This class uses Windows API (user32.dll) to capture screen content. It provides methods
///       to capture the entire desktop, the currently active window, or any specific window by handle.
///       Optionally, it can draw a crosshair indicator at the current cursor position.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Desktop capture:</strong> Capture the entire desktop screen
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Active window capture:</strong> Capture only the currently active/foreground window
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Specific window capture:</strong> Capture any window by its handle
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Cursor visualization:</strong> Optionally draw a crosshair at the cursor position
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Platform support:</strong> This class requires Windows and is designed for
///       .NET Framework 4.7.2 and .NET 8+. It uses P/Invoke to call native Windows APIs.
///    </para>
/// </remarks>
/// <example>
///    <strong>Capture entire desktop:</strong>
///    <code>
/// var desktopScreenshot = ScreenCapture.CaptureDesktop();
/// desktopScreenshot?.Save("desktop.png", ImageFormat.Png);
/// </code>
///    <strong>Capture active window with cursor:</strong>
///    <code>
/// var activeWindowScreenshot = ScreenCapture.CaptureActiveWindow(drawMousePoint: true);
/// activeWindowScreenshot?.Save("active-window.png", ImageFormat.Png);
/// </code>
///    <strong>Capture specific window by handle:</strong>
///    <code>
/// IntPtr windowHandle = /* get window handle */;
/// var windowScreenshot = ScreenCapture.CaptureWindow(windowHandle);
/// if (windowScreenshot != null)
/// {
///     windowScreenshot.Save("window.png", ImageFormat.Png);
///     windowScreenshot.Dispose();
/// }
/// </code>
/// </example>
public partial class ScreenCapture
{
	/// <summary>
	///    Retrieves a handle to the foreground window (the window with which the user is currently working).
	/// </summary>
	/// <returns>
	///    A handle to the foreground window. The foreground window can be <see cref="IntPtr.Zero" />
	///    in certain circumstances, such as when a window is losing activation.
	/// </returns>
#if STANDARD_OR_OLD_FRAMEWORKS
	[DllImport("user32.dll")]
#else
	[LibraryImport("user32.dll")]
#endif
	private static
#if STANDARD_OR_OLD_FRAMEWORKS
	extern
#else
	partial
#endif
	IntPtr GetForegroundWindow();

	/// <summary>
	///    Retrieves a handle to the desktop window.
	/// </summary>
	/// <returns>A handle to the desktop window.</returns>
	/// <remarks>
	///    The desktop window covers the entire screen. The desktop window is the area on top of which
	///    other windows are painted.
	/// </remarks>
#if STANDARD_OR_OLD_FRAMEWORKS
	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
#else
	[LibraryImport("user32.dll")]
#endif
	private static
#if STANDARD_OR_OLD_FRAMEWORKS
	extern
#else
		partial
#endif
	IntPtr GetDesktopWindow();

	/// <summary>
	///    Retrieves the dimensions of the bounding rectangle of the specified window.
	/// </summary>
	/// <param name="hWnd">A handle to the window.</param>
	/// <param name="rect">
	///    A reference to a <see cref="Rect" /> structure that receives the screen coordinates
	///    of the upper-left and lower-right corners of the window.
	/// </param>
	/// <returns>
	///    A handle to the window rectangle. The actual rectangle coordinates are returned in the
	///    <paramref name="rect" /> parameter.
	/// </returns>
#if STANDARD_OR_OLD_FRAMEWORKS
	[DllImport("user32.dll")]
#else
	[LibraryImport("user32.dll")]
#endif
	private static
#if STANDARD_OR_OLD_FRAMEWORKS
	extern
#else
		partial
#endif
	IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

	/// <summary>
	///    Retrieves the position of the mouse cursor in screen coordinates.
	/// </summary>
	/// <param name="lpPoint">
	///    An output parameter that receives a <see cref="POINT" /> structure containing
	///    the screen coordinates of the cursor.
	/// </param>
	/// <returns>
	///    <c>true</c> if the function succeeds; otherwise, <c>false</c>.
	/// </returns>
#if STANDARD_OR_OLD_FRAMEWORKS
	[DllImport("user32.dll")]
#else
	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
#endif
	private static
#if STANDARD_OR_OLD_FRAMEWORKS
	extern
#else
	partial
#endif
	bool GetCursorPos(out POINT lpPoint);

	/// <summary>
	///    Captures a screenshot of the entire desktop.
	/// </summary>
	/// <param name="drawMousePoint">
	///    If <c>true</c>, draws a red crosshair indicator at the current cursor position
	///    on the captured image. Default is <c>false</c>.
	/// </param>
	/// <returns>
	///    An <see cref="Image" /> containing the desktop screenshot, or <c>null</c> if the
	///    desktop window handle is invalid.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method captures all visible content on all monitors in a multi-monitor setup.
	///       The resulting image dimensions correspond to the virtual screen bounds.
	///    </para>
	///    <para>
	///       <strong>Cursor visualization:</strong> When <paramref name="drawMousePoint" /> is <c>true</c>,
	///       a crosshair is drawn with:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>A red filled circle (12px diameter) as the outer marker</description>
	///       </item>
	///       <item>
	///          <description>White horizontal and vertical lines forming a cross</description>
	///       </item>
	///       <item>
	///          <description>A smaller white filled circle (4px diameter) at the center</description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// using var screenshot = ScreenCapture.CaptureDesktop(drawMousePoint: true);
	/// screenshot?.Save("desktop-with-cursor.png", ImageFormat.Png);
	/// </code>
	/// </example>
	public static Image? CaptureDesktop(bool drawMousePoint = false)
	{
		return CaptureWindow(GetDesktopWindow(), drawMousePoint);
	}

	/// <summary>
	///    Captures a screenshot of the currently active (foreground) window.
	/// </summary>
	/// <param name="drawMousePoint">
	///    If <c>true</c>, draws a red crosshair indicator at the current cursor position
	///    on the captured image. Default is <c>false</c>.
	/// </param>
	/// <returns>
	///    A <see cref="Bitmap" /> containing the active window screenshot, or <c>null</c> if
	///    there is no active window or the window handle is invalid.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The active window is the window that currently has keyboard focus. This method
	///       captures only the client and non-client areas of that specific window, excluding
	///       any overlapping windows.
	///    </para>
	///    <para>
	///       If the cursor is outside the active window bounds and <paramref name="drawMousePoint" />
	///       is <c>true</c>, the crosshair will not be visible in the resulting image.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Capture the active window without cursor
	/// using var screenshot = ScreenCapture.CaptureActiveWindow();
	/// if (screenshot != null)
	/// {
	///     screenshot.Save("active-window.png", ImageFormat.Png);
	/// }
	/// </code>
	/// </example>
	public static Bitmap? CaptureActiveWindow(bool drawMousePoint = false)
	{
		return CaptureWindow(GetForegroundWindow(), drawMousePoint);
	}

	/// <summary>
	///    Captures a screenshot of a specific window identified by its handle.
	/// </summary>
	/// <param name="handle">
	///    A handle (<see cref="IntPtr" />) to the window to capture. This can be obtained
	///    from methods like <see cref="GetForegroundWindow" /> or <see cref="GetDesktopWindow" />,
	///    or through other Windows API calls.
	/// </param>
	/// <param name="drawMousePoint">
	///    If <c>true</c>, draws a red crosshair indicator at the current cursor position
	///    on the captured image. Default is <c>false</c>.
	/// </param>
	/// <returns>
	///    A <see cref="Bitmap" /> containing the window screenshot, or <c>null</c> if the
	///    <paramref name="handle" /> is <see cref="IntPtr.Zero" />.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This is the core capture method used by both <see cref="CaptureDesktop" /> and
	///       <see cref="CaptureActiveWindow" />. It uses <see cref="Graphics.CopyFromScreen(Point, Point, Size)" />
	///       to capture the screen content at the window's coordinates.
	///    </para>
	///    <para>
	///       <strong>Cursor crosshair details:</strong> When <paramref name="drawMousePoint" /> is <c>true</c>:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>A red filled ellipse (12px diameter) is drawn centered on the cursor</description>
	///       </item>
	///       <item>
	///          <description>White horizontal and vertical lines (12px length) form a crosshair</description>
	///       </item>
	///       <item>
	///          <description>A white filled ellipse (4px diameter) marks the exact cursor position</description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Important:</strong> The caller is responsible for disposing the returned
	///       <see cref="Bitmap" /> to free unmanaged resources.
	///    </para>
	/// </remarks>
	/// <example>
	///    <strong>Capture a specific window:</strong>
	///    <code>
	/// IntPtr windowHandle = GetWindowHandleSomehow();
	/// using var screenshot = ScreenCapture.CaptureWindow(windowHandle, drawMousePoint: true);
	/// if (screenshot != null)
	/// {
	///     screenshot.Save($"window-{DateTime.Now:yyyyMMdd-HHmmss}.png", ImageFormat.Png);
	/// }
	/// </code>
	/// </example>
	public static Bitmap? CaptureWindow(IntPtr handle, bool drawMousePoint = false)
	{
		if (handle == IntPtr.Zero) return null;
		var rect = new Rect();
		GetWindowRect(handle, ref rect);
		var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
		var bitmap = new Bitmap(bounds.Width, bounds.Height);
		using (var graphics = Graphics.FromImage(bitmap))
		{
			graphics.CopyFromScreen(new(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
			if (drawMousePoint)
			{
				var cursorCrosshairSize = 12;
				GetCursorPos(out var po);
				graphics.FillEllipse(Brushes.Red,
					new(po.X - bounds.Left - cursorCrosshairSize / 2, po.Y - bounds.Top - cursorCrosshairSize / 2,
						cursorCrosshairSize, cursorCrosshairSize));
				// Horizontal line
				graphics.DrawLine(Pens.White, new(po.X - bounds.Left - cursorCrosshairSize / 2, po.Y - bounds.Top),
					new(po.X - bounds.Left + cursorCrosshairSize / 2, po.Y - bounds.Top));
				// Vertical line
				graphics.DrawLine(Pens.White, new(po.X - bounds.Left, po.Y - bounds.Top - cursorCrosshairSize / 2),
					new(po.X - bounds.Left, po.Y - bounds.Top + cursorCrosshairSize / 2));
				graphics.FillEllipse(Brushes.White,
					new(po.X - bounds.Left - cursorCrosshairSize / 6, po.Y - bounds.Top - cursorCrosshairSize / 6,
						cursorCrosshairSize / 3, cursorCrosshairSize / 3));
			}
		}

		return bitmap;
	}

	/// <summary>
	///    Represents a rectangle defined by its left, top, right, and bottom coordinates.
	/// </summary>
	/// <remarks>
	///    This structure is used for interop with the Windows API <c>GetWindowRect</c> function.
	///    It represents screen coordinates in pixels.
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	private readonly struct Rect
	{
		/// <summary>
		///    The x-coordinate of the upper-left corner of the rectangle.
		/// </summary>
		public readonly int Left;

		/// <summary>
		///    The y-coordinate of the upper-left corner of the rectangle.
		/// </summary>
		public readonly int Top;

		/// <summary>
		///    The x-coordinate of the lower-right corner of the rectangle.
		/// </summary>
		public readonly int Right;

		/// <summary>
		///    The y-coordinate of the lower-right corner of the rectangle.
		/// </summary>
		public readonly int Bottom;
	}

	/// <summary>
	///    Represents a point in 2D space with X and Y coordinates.
	/// </summary>
	/// <remarks>
	///    This structure is used for interop with Windows API functions that return cursor positions.
	///    It can be implicitly converted to <see cref="System.Drawing.Point" />.
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		/// <summary>
		///    The x-coordinate of the point.
		/// </summary>
		public int X;

		/// <summary>
		///    The y-coordinate of the point.
		/// </summary>
		public int Y;

		/// <summary>
		///    Implicitly converts a <see cref="POINT" /> to a <see cref="System.Drawing.Point" />.
		/// </summary>
		/// <param name="point">The <see cref="POINT" /> to convert.</param>
		/// <returns>A <see cref="System.Drawing.Point" /> with the same coordinates.</returns>
		public static implicit operator Point(POINT point)
		{
			return new(point.X, point.Y);
		}
	}
}