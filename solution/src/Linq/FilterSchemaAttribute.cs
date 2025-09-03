using System;

namespace Fuxion.Linq;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FilterSchemaAttribute(string member) : Attribute
{
	public string Member { get; } = member;
}