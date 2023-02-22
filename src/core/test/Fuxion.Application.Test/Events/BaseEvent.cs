﻿using Fuxion.Domain;
using Fuxion.Reflection;

namespace Fuxion.Application.Test.Events;

[TypeKey(nameof(BaseEvent))]
public record BaseEvent(Guid AggregateId, string? Name, int Age) : Event(AggregateId);