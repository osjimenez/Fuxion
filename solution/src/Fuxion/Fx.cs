using Fuxion;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
///    Provides the entry point for all Fuxion extension methods through the <c>.Fx</c> property.
/// </summary>
/// <remarks>
///    <para>
///       This class implements a fluent extension pattern that allows accessing Fuxion-specific functionality
///       on any object through the <c>.Fx</c> property. This design pattern provides better IntelliSense discoverability
///       and namespace organization for extension methods.
///    </para>
///    <para>
///       Instead of having hundreds of extension methods appearing directly on every object, Fuxion groups them
///       under the <c>.Fx</c> property, making the API cleaner and more organized.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// // Instead of: myObject.SomeFuxionMethod()
/// // Use: myObject.Fx.SomeFuxionMethod()
/// 
/// // Example with string
/// string text = "Hello";
/// var result = text.Fx.SomeExtension();
/// 
/// // Example with any type
/// var myObject = new MyClass();
/// myObject.Fx.DoSomething();
/// 
/// // The .Fx property provides access to all Fuxion extensions
/// // while keeping the base object's API clean
/// </code>
/// </example>
public static class FuxionExtensions
{
	/// <summary>
	///    Extension methods that add the <c>.Fx</c> property to all types.
	/// </summary>
	extension<T>(T me)
	{
		/// <summary>
		///    Gets the Fuxion extensions container for this object.
		/// </summary>
		/// <value>
		///    A <see cref="FuxionExtensions{T}" /> instance that provides access to all Fuxion-specific extension methods.
		/// </value>
		/// <remarks>
		///    <para>
		///       This property is the entry point for all Fuxion functionality. It wraps the current object
		///       in a <see cref="FuxionExtensions{T}" /> container that exposes Fuxion-specific extension methods.
		///    </para>
		///    <para>
		///       The method is marked with <see cref="MethodImplAttribute" /> using
		///       <see cref="MethodImplOptions.AggressiveInlining" />
		///       for optimal performance, as this property is called frequently throughout Fuxion usage.
		///    </para>
		/// </remarks>
		/// <example>
		///    <code>
		/// // Access Fuxion extensions on any object
		/// var person = new Person { Name = "John" };
		/// 
		/// // The .Fx property gives access to all Fuxion extensions
		/// person.Fx.DoSomething();
		/// 
		/// // Works with built-in types too
		/// int number = 42;
		/// number.Fx.SomeExtension();
		/// 
		/// // Even with nullable types
		/// string? nullableText = "test";
		/// nullableText.Fx.AnotherExtension();
		/// </code>
		/// </example>
		public FuxionExtensions<T?> Fx
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me);
		}
	}
}

namespace Fuxion
{
	/// <summary>
	///    Container for Fuxion-specific extension methods.
	///    This class wraps an object and provides access to Fuxion functionality.
	/// </summary>
	/// <typeparam name="T">The type of the wrapped object.</typeparam>
	/// <param name="me">The object being wrapped.</param>
	/// <remarks>
	///    <para>
	///       This class serves as an extension point where Fuxion-specific functionality can be added
	///       through additional extension methods. It inherits from <see cref="Extensions{T}" /> which
	///       provides the base infrastructure.
	///    </para>
	///    <para>
	///       Developers can extend Fuxion functionality by creating extension methods on this type:
	///    </para>
	///    <code>
	/// public static class MyFuxionExtensions
	/// {
	///     public static string MyCustomExtension&lt;T&gt;(this FuxionExtensions&lt;T&gt; fx)
	///     {
	///         var value = fx.Value;
	///         // Custom logic here
	///         return $"Processed: {value}";
	///     }
	/// }
	/// 
	/// // Usage:
	/// var obj = new MyClass();
	/// var result = obj.Fx.MyCustomExtension();
	/// </code>
	/// </remarks>
	public class FuxionExtensions<T>(T me) : Extensions<T>(me);

	/// <summary>
	///    Base class for extension containers that wrap an object and provide extensibility.
	/// </summary>
	/// <typeparam name="T">The type of the wrapped object.</typeparam>
	/// <param name="me">The object being wrapped.</param>
	/// <remarks>
	///    <para>
	///       This abstract class provides the foundation for the Fuxion extension pattern.
	///       It stores the wrapped object and provides access to it through the <see cref="Value" /> property.
	///    </para>
	///    <para>
	///       The class is marked as abstract to prevent direct instantiation, encouraging the use of
	///       derived classes like <see cref="FuxionExtensions{T}" />.
	///    </para>
	/// </remarks>
	public abstract class Extensions<T>(T me)
	{
		/// <summary>
		///    Gets the wrapped object.
		/// </summary>
		/// <value>The original object that was wrapped by this extension container.</value>
		/// <remarks>
		///    <para>
		///       This property allows extension methods to access the original object being extended.
		///       It is marked with <see cref="EditorBrowsableAttribute" /> set to <see cref="EditorBrowsableState.Never" />
		///       to hide it from IntelliSense, keeping the API surface clean for consumers.
		///    </para>
		///    <para>
		///       The getter is aggressively inlined for performance optimization since it's frequently accessed
		///       by extension methods.
		///    </para>
		/// </remarks>
		/// <example>
		///    <code>
		/// // Inside a custom extension method:
		/// public static string ToUpperExtension(this FuxionExtensions&lt;string&gt; fx)
		/// {
		///     string original = fx.Value; // Access the wrapped string
		///     return original.ToUpper();
		/// }
		/// 
		/// // Usage:
		/// string text = "hello";
		/// string upper = text.Fx.ToUpperExtension(); // "HELLO"
		/// </code>
		/// </example>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public T Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => me;
		}
	}
}