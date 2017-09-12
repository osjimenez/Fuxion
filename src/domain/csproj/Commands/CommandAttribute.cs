﻿using System;
namespace Fuxion.Domain.Commands
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(Type aggregateType) { AggregateType = aggregateType; }
        public Type AggregateType { get; set; }
    }
}
