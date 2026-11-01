using System;

namespace Fuxion.Types;

/// <summary>
/// The key and the exact revision of a type: the generator of Fuxion writes its <see cref="TypeDefinition"/> at compile
/// time, in the class marked with <see cref="TypeDefinitionsAttribute"/>, and checks there what this attribute says.
/// </summary>
/// <param name="key">The key of the type, a <see cref="UriKey"/> without arguments in its canonical form: <c>https://meta.fuxion.nexus/erp/invoice/v1</c>.</param>
/// <param name="revision">The exact revision of the type, a semantic version of the same major version as its key: <c>1.3.0</c>.</param>
/// <remarks>
/// A generic type has the arity of its declaration; its base and its interfaces come from the type itself. Nothing reads
/// this attribute at run time: the definitions are generated, without reflection.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false)]
public sealed class UriKeyAttribute(string key, string revision) : Attribute
{
	/// <summary>The key of the type.</summary>
	public string Key { get; } = key;

	/// <summary>The exact revision of the type.</summary>
	public string Revision { get; } = revision;
}

/// <summary>
/// Marks a <c>static partial</c> class that the generator of Fuxion fills with the definitions of the types of its
/// assembly that have a <see cref="UriKeyAttribute"/>: their <c>Definitions</c>, and a <c>Register(TypeRegistry)</c>
/// method that registers each type with its definition, without reflection.
/// </summary>
/// <example>
/// <code>
/// [TypeDefinitions]
/// internal static partial class AppTypes;
///
/// var registry = new TypeRegistry();
/// AppTypes.Register(registry);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TypeDefinitionsAttribute : Attribute;
