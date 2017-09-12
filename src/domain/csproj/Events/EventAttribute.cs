﻿using System;
namespace Fuxion.Domain.Events
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventAttribute : Attribute
    {
        public EventAttribute(Type aggregateType) { AggregateType = aggregateType; }
        public Type AggregateType { get; set; }
    }
}
