using System;

namespace Fuxion.Analyzers;

/// <summary>
/// Indicates that a property or method requires a non-null value and will throw
/// an exception (typically <see cref="ArgumentNullException"/>) if the value is null.
/// </summary>
/// <remarks>
/// This attribute is used by Roslyn analyzers to provide compile-time diagnostics
/// when accessing members that enforce non-null constraints at runtime.
/// 
/// <para>
/// When a property or method is marked with this attribute, the analyzer will:
/// <list type="bullet">
/// <item>Report an error when accessed on a nullable type without null-conditional operator</item>
/// <item>Suggest using the null-conditional operator (?) to safely handle null values</item>
/// </list>
/// </para>
/// 
/// <example>
/// <code>
/// using Fuxion.Analyzers;
/// 
/// extension&lt;T&gt;(FuxionExtensions&lt;T?&gt; me) where T : notnull
/// {
///     [RequiresNotNull]
///     public PodExtensions&lt;T&gt; Pod
///     {
///         get
///         {
///             ArgumentNullException.ThrowIfNull(me.Value);
///             return new(me.Value);
///         }
///     }
/// }
/// 
/// // Usage:
/// string? nullable = GetNullableString();
/// nullable?.Fx.Pod.BuildUriKeyPod(resolver); // ? Correct: uses null-conditional
/// nullable.Fx.Pod.BuildUriKeyPod(resolver);  // ? Error: FX001
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresNotNullAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RequiresNotNullAttribute"/> class.
	/// </summary>
	public RequiresNotNullAttribute()
	{
	}

	/// <summary>
	/// Gets or sets an optional custom error message to display when the analyzer
	/// detects unsafe access to this member.
	/// </summary>
	/// <remarks>
	/// If not specified, a default message will be generated based on the member type.
	/// </remarks>
	public string? CustomMessage { get; set; }
}
