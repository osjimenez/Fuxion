namespace Fuxion.Types;

// The types of the system and their keys, under https://meta.fuxion.nexus/system/: the
// primitives with their C# names, the other values in kebab-case, and the collections. The same file is compiled into
// Fuxion (SystemTypes, the registry) and into its generator of type definitions (Fuxion.Analyzers), so both name a type
// of the system the same way.
static class SystemTypeKeys
{
	internal const string Root = "https://meta.fuxion.nexus/system/";
	internal const string Revision = "1.0.0";

	// The one-dimensional arrays (T[]): a construction of it, as https://meta.fuxion.nexus/system/array/v1[…/int/v1]
	internal const string Array = Root + "array/v1";

	// The full name in metadata of each .NET type of the system (null for the arrays, which have none), its key and its
	// arity
	internal static readonly (string? ClrName, string Key, int Arity)[] All =
	[
		("System.Boolean", Root + "bool/v1", 0),
		("System.Byte", Root + "byte/v1", 0),
		("System.SByte", Root + "sbyte/v1", 0),
		("System.Int16", Root + "short/v1", 0),
		("System.UInt16", Root + "ushort/v1", 0),
		("System.Int32", Root + "int/v1", 0),
		("System.UInt32", Root + "uint/v1", 0),
		("System.Int64", Root + "long/v1", 0),
		("System.UInt64", Root + "ulong/v1", 0),
		("System.Single", Root + "float/v1", 0),
		("System.Double", Root + "double/v1", 0),
		("System.Decimal", Root + "decimal/v1", 0),
		("System.Char", Root + "char/v1", 0),
		("System.String", Root + "string/v1", 0),
		("System.Object", Root + "object/v1", 0),
		("System.DateTime", Root + "date-time/v1", 0),
		("System.DateTimeOffset", Root + "date-time-offset/v1", 0),
		("System.DateOnly", Root + "date-only/v1", 0),
		("System.TimeOnly", Root + "time-only/v1", 0),
		("System.TimeSpan", Root + "time-span/v1", 0),
		("System.Guid", Root + "guid/v1", 0),
		("System.Uri", Root + "uri/v1", 0),
		(null, Array, 1),
		("System.Collections.Generic.List`1", Root + "list/v1", 1),
		("System.Collections.Generic.Dictionary`2", Root + "dictionary/v1", 2),
	];
}
