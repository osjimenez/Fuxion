using System;

namespace Fuxion.EntityFramework;

[AttributeUsage(AttributeTargets.Property)]
public class CascadeDeleteAttribute : Attribute { }