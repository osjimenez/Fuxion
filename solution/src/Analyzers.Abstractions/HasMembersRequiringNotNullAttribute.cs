using System;

namespace Fuxion.Analyzers.Abstractions;

/// <summary>
/// Indicates that specific members of this type require non-null values and will throw
/// exceptions when accessed with null. This attribute is primarily used as a workaround
/// for extension members where individual member attributes are not visible to Roslyn analyzers.
/// </summary>
/// <remarks>
/// <para>
/// This attribute should be applied to the containing type (usually the type being extended)
/// and lists the names of members that enforce non-null constraints.
/// </para>
/// <para>
/// This is a temporary workaround for C# 14 extension members. Once Roslyn properly exposes
/// attributes on extension member symbols, this attribute can be removed in favor of applying
/// <see cref="RequiresNotNullAttribute"/> directly to individual members.
/// </para>
/// <example>
/// <code>
/// using Fuxion.Analyzers;
/// 
/// [HasMembersRequiringNotNull("Pod", "Json")]
/// public class FuxionExtensions&lt;T&gt; where T : notnull
/// {
///     public T Value { get; }
/// }
/// 
/// extension&lt;T&gt;(FuxionExtensions&lt;T?&gt; me) where T : notnull
/// {
///     // This property requires non-null, indicated by the class attribute
///     public PodExtensions&lt;T&gt; Pod
///     {
///         get
///         {
///             ArgumentNullException.ThrowIfNull(me.Value);
///             return new PodExtensions&lt;T&gt;(me.Value);
///         }
///     }
/// }
/// 
/// // Usage:
/// string? nullable = GetString();
/// nullable?.Fx.Pod.BuildUriKeyPod(resolver); // ? Correct
/// nullable.Fx.Pod.BuildUriKeyPod(resolver);  // ? Error FX001
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class HasMembersRequiringNotNullAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HasMembersRequiringNotNullAttribute"/> class
	/// with the specified member names that require non-null values.
	/// </summary>
	/// <param name="memberNames">
	/// The names of properties or methods in this type (or its extension members) that require non-null values.
	/// </param>
	public HasMembersRequiringNotNullAttribute(params string[] memberNames)
	{
		MemberNames = memberNames ?? Array.Empty<string>();
	}

	/// <summary>
	/// Gets the names of members that require non-null values.
	/// </summary>
	public string[] MemberNames { get; }
}
