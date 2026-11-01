using System.Collections.Generic;
using System.Linq;

namespace Fuxion.Types;

/// <summary>
/// The definitions of the types of the system, under <c>https://meta.fuxion.nexus/system/</c>, revision 1.0.0: the
/// primitives, other values and the collections. Every <see cref="TypeRegistry"/> has them.
/// </summary>
public static class SystemTypes
{
	static readonly Dictionary<string, TypeDefinition> byKey = SystemTypeKeys.All.ToDictionary(
		entry => entry.Key,
		entry => new TypeDefinition(UriKey.Parse(entry.Key), new(SystemTypeKeys.Revision), entry.Arity));

	/// <summary>Every type of the system.</summary>
	public static IReadOnlyList<TypeDefinition> All { get; } = SystemTypeKeys.All.Select(entry => byKey[entry.Key]).ToArray();

	static TypeDefinition Named(string name) => byKey[SystemTypeKeys.Root + name + "/v1"];

	/// <summary><c>bool</c>.</summary>
	public static TypeDefinition Bool { get; } = Named("bool");

	/// <summary><c>byte</c>.</summary>
	public static TypeDefinition Byte { get; } = Named("byte");

	/// <summary><c>sbyte</c>.</summary>
	public static TypeDefinition SByte { get; } = Named("sbyte");

	/// <summary><c>short</c>.</summary>
	public static TypeDefinition Short { get; } = Named("short");

	/// <summary><c>ushort</c>.</summary>
	public static TypeDefinition UShort { get; } = Named("ushort");

	/// <summary><c>int</c>.</summary>
	public static TypeDefinition Int { get; } = Named("int");

	/// <summary><c>uint</c>.</summary>
	public static TypeDefinition UInt { get; } = Named("uint");

	/// <summary><c>long</c>.</summary>
	public static TypeDefinition Long { get; } = Named("long");

	/// <summary><c>ulong</c>.</summary>
	public static TypeDefinition ULong { get; } = Named("ulong");

	/// <summary><c>float</c>.</summary>
	public static TypeDefinition Float { get; } = Named("float");

	/// <summary><c>double</c>.</summary>
	public static TypeDefinition Double { get; } = Named("double");

	/// <summary><c>decimal</c>.</summary>
	public static TypeDefinition Decimal { get; } = Named("decimal");

	/// <summary><c>char</c>.</summary>
	public static TypeDefinition Char { get; } = Named("char");

	/// <summary><c>string</c>.</summary>
	public static TypeDefinition String { get; } = Named("string");

	/// <summary><c>object</c>: any value.</summary>
	public static TypeDefinition Object { get; } = Named("object");

	/// <summary><c>System.DateTime</c>.</summary>
	public static TypeDefinition DateTime { get; } = Named("date-time");

	/// <summary><c>System.DateTimeOffset</c>.</summary>
	public static TypeDefinition DateTimeOffset { get; } = Named("date-time-offset");

	/// <summary><c>System.DateOnly</c>.</summary>
	public static TypeDefinition DateOnly { get; } = Named("date-only");

	/// <summary><c>System.TimeOnly</c>.</summary>
	public static TypeDefinition TimeOnly { get; } = Named("time-only");

	/// <summary><c>System.TimeSpan</c>.</summary>
	public static TypeDefinition TimeSpan { get; } = Named("time-span");

	/// <summary><c>System.Guid</c>.</summary>
	public static TypeDefinition Guid { get; } = Named("guid");

	/// <summary><c>System.Uri</c>.</summary>
	public static TypeDefinition Uri { get; } = Named("uri");

	/// <summary>The one-dimensional arrays, <c>T[]</c>: <c>int[]</c> is <c>https://meta.fuxion.nexus/system/array/v1[https://meta.fuxion.nexus/system/int/v1]</c>.</summary>
	public static TypeDefinition Array { get; } = byKey[SystemTypeKeys.Array];

	/// <summary><c>System.Collections.Generic.List&lt;T&gt;</c>.</summary>
	public static TypeDefinition List { get; } = Named("list");

	/// <summary><c>System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;</c>.</summary>
	public static TypeDefinition Dictionary { get; } = Named("dictionary");
}
