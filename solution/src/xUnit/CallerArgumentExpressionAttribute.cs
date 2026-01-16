// This is class is a substitute for legacy frameworks
// https://stackoverflow.com/a/70034587/3459458

#if OLD_FRAMEWORKS
#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

/// <summary>
///    Provides a polyfill attribute for legacy .NET frameworks that allows capturing the expression passed to a parameter.
/// </summary>
/// <remarks>
///    <para>
///       This attribute is a substitute implementation for frameworks that don't natively support
///       <see cref="CallerArgumentExpressionAttribute"/>, which was introduced in C# 10 and .NET 6.
///       It enables features like automatic expression capturing for improved diagnostics and logging
///       in older framework versions.
///    </para>
///    <para>
///       <strong>Framework support:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Native support:</strong> .NET 6+, .NET Core 3.1+, .NET Standard 2.1+
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Polyfill needed:</strong> .NET Framework 4.x, .NET Standard 2.0, older .NET Core versions
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Expression capture:</strong> Automatically captures the expression text passed as an argument
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Compile-time feature:</strong> Works at compile time, no runtime overhead
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Conditional compilation:</strong> Only compiled for legacy frameworks via <c>OLD_FRAMEWORKS</c> symbol
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Seamless integration:</strong> Provides same API as native implementation
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Assertion methods that print variable names and values</description>
///       </item>
///       <item>
///          <description>Logging methods that include expression context</description>
///       </item>
///       <item>
///          <description>Validation methods with detailed error messages</description>
///       </item>
///       <item>
///          <description>Diagnostic utilities that show code context</description>
///       </item>
///    </list>
///    <para>
///       <strong>Important notes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             This polyfill is only compiled when <c>OLD_FRAMEWORKS</c> is defined
///          </description>
///       </item>
///       <item>
///          <description>
///             The attribute must be in the <c>System.Runtime.CompilerServices</c> namespace
///          </description>
///       </item>
///       <item>
///          <description>
///             Requires C# 10+ compiler even when targeting older frameworks
///          </description>
///       </item>
///       <item>
///          <description>
///             Modern frameworks use the built-in implementation from the BCL
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic assertion method:</strong>
///    <code>
/// public static void IsTrue(bool condition, 
///     [CallerArgumentExpression(nameof(condition))] string? expression = null)
/// {
///     if (!condition)
///     {
///         throw new AssertionException($"Assertion failed: {expression}");
///     }
///     Console.WriteLine($"Pass: {expression}");
/// }
/// 
/// // Usage
/// int count = GetCount();
/// IsTrue(count &gt; 0);
/// // Output: Pass: count &gt; 0
/// 
/// IsTrue(user.IsActive);
/// // Output: Pass: user.IsActive
/// </code>
///    <strong>Logging with context:</strong>
///    <code>
/// public static void LogValue&lt;T&gt;(T value,
///     [CallerArgumentExpression(nameof(value))] string? expression = null)
/// {
///     Console.WriteLine($"{expression} = {value}");
/// }
/// 
/// // Usage
/// var result = CalculateSum(10, 20);
/// LogValue(result);
/// // Output: result = 30
/// 
/// LogValue(DateTime.Now);
/// // Output: DateTime.Now = 2024-01-15 10:30:00
/// </code>
///    <strong>Validation with descriptive errors:</strong>
///    <code>
/// public static void NotNull&lt;T&gt;(T? value,
///     [CallerArgumentExpression(nameof(value))] string? expression = null)
///     where T : class
/// {
///     if (value is null)
///     {
///         throw new ArgumentNullException(expression, $"'{expression}' cannot be null");
///     }
/// }
/// 
/// // Usage
/// NotNull(user);
/// // Throws: ArgumentNullException: 'user' cannot be null
/// 
/// NotNull(repository.GetById(id));
/// // Throws: ArgumentNullException: 'repository.GetById(id)' cannot be null
/// </code>
///    <strong>xUnit test assertions (as in BaseTest):</strong>
///    <code>
/// public void PrintVariable(object? value,
///     [CallerArgumentExpression(nameof(value))] string? name = null)
/// {
///     Output.WriteLine($"{name} = {value}");
/// }
/// 
/// public void IsTrue(bool? value,
///     [CallerArgumentExpression(nameof(value))] string? name = null)
/// {
///     Assert.True(value);
///     PrintVariable(value, name);
/// }
/// 
/// // Usage in test
/// [Fact]
/// public void Test_Example()
/// {
///     var result = service.Process();
///     IsTrue(result.IsSuccess);
///     // Output: result.IsSuccess = True
/// }
/// </code>
///    <strong>Debug helpers:</strong>
///    <code>
/// public static class Debug
/// {
///     public static T Inspect&lt;T&gt;(T value,
///         [CallerArgumentExpression(nameof(value))] string? expression = null)
///     {
///         Console.WriteLine($"[DEBUG] {expression} = {value}");
///         return value;
///     }
/// }
/// 
/// // Usage - can be inserted inline
/// var total = Debug.Inspect(items.Sum(x =&gt; x.Price));
/// // Output: [DEBUG] items.Sum(x =&gt; x.Price) = 150.50
/// 
/// return Debug.Inspect(repository.Query().Where(x =&gt; x.IsActive).ToList());
/// // Output: [DEBUG] repository.Query().Where(x =&gt; x.IsActive).ToList() = [...]
/// </code>
///    <strong>Conditional compilation setup:</strong>
///    <code><![CDATA[
/// <!-- In .csproj for .NET Framework 4.7.2 -->
/// <PropertyGroup>
///     <TargetFramework>net472</TargetFramework>
///     <LangVersion>10.0</LangVersion>
///     <DefineConstants>$(DefineConstants);OLD_FRAMEWORKS</DefineConstants>
/// </PropertyGroup>
/// 
/// <!-- In .csproj for .NET 8 - no OLD_FRAMEWORKS needed -->
/// <PropertyGroup>
///     <TargetFramework>net8.0</TargetFramework>
///     <LangVersion>12.0</LangVersion>
/// </PropertyGroup>
/// ]]></code>
///    <strong>Multi-parameter expression capture:</strong>
///    <code>
/// public static void AreEqual&lt;T&gt;(T expected, T actual,
///     [CallerArgumentExpression(nameof(expected))] string? expectedExpr = null,
///     [CallerArgumentExpression(nameof(actual))] string? actualExpr = null)
/// {
///     if (!EqualityComparer&lt;T&gt;.Default.Equals(expected, actual))
///     {
///         throw new AssertionException(
///             $"Expected {expectedExpr} ({expected}) to equal {actualExpr} ({actual})");
///     }
/// }
/// 
/// // Usage
/// AreEqual(expectedCount, result.Count);
/// // Error: Expected expectedCount (10) to equal result.Count (5)
/// </code>
///    <strong>Guard clauses:</strong>
///    <code>
/// public static class Guard
/// {
///     public static void IsNotEmpty(string value,
///         [CallerArgumentExpression(nameof(value))] string? paramName = null)
///     {
///         if (string.IsNullOrEmpty(value))
///         {
///             throw new ArgumentException($"'{paramName}' cannot be null or empty", paramName);
///         }
///     }
///     
///     public static void IsInRange(int value, int min, int max,
///         [CallerArgumentExpression(nameof(value))] string? paramName = null)
///     {
///         if (value &lt; min || value &gt; max)
///         {
///             throw new ArgumentOutOfRangeException(paramName,
///                 $"'{paramName}' ({value}) must be between {min} and {max}");
///         }
///     }
/// }
/// 
/// // Usage
/// public void ProcessUser(string username, int age)
/// {
///     Guard.IsNotEmpty(username);
///     Guard.IsInRange(age, 0, 120);
///     // ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
	/// <summary>
	///    Gets the name of the parameter whose expression should be captured.
	/// </summary>
	/// <value>
	///    The name of the parameter in the same method whose argument expression will be captured.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property specifies which parameter's expression should be captured and passed to
	///       the parameter decorated with this attribute. The value must match the name of another
	///       parameter in the same method signature.
	///    </para>
	///    <para>
	///       When the compiler encounters this attribute, it automatically captures the textual
	///       representation of the expression passed to the specified parameter and provides it
	///       as the default value for the decorated parameter.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Method signature
	/// public void AssertTrue(bool condition,
	///     [CallerArgumentExpression(nameof(condition))] string? expr = null)
	/// {
	///     // ParameterName is "condition"
	///     // When called with: AssertTrue(x &gt; 5)
	///     // expr will be: "x &gt; 5"
	/// }
	/// </code>
	/// </example>
	public string ParameterName { get; } = parameterName;
}
#endif