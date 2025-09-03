using System;

namespace Fuxion.Linq;

[Flags]
public enum OperationKind
{
	None = 0,
	Equal = 1 << 0,
	NotEqual = 1 << 1,
	In = 1 << 2,
	GreaterThan = 1 << 3,
	GreaterOrEqual = 1 << 4,
	LessThan = 1 << 5,
	LessOrEqual = 1 << 6,
	Between = 1 << 7,
	Contains = 1 << 8,
	StartsWith = 1 << 9,
	EndsWith = 1 << 10,
	IsNull = 1 << 11,
	IsNotNull = 1 << 12,
	Empty = 1 << 13,
	NotEmpty = 1 << 14,
	HasFlag = 1 << 15
}