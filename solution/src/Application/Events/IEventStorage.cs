﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fuxion.Domain;

namespace Fuxion.Application.Events;

public interface IEventStorage
{
	Task<IQueryable<Event>> GetEventsAsync(Guid aggregateId, int start, int count);
	Task<Event?> GetLastEventAsync(Guid aggregateId);
	Task CommitAsync(Guid aggregateId, IEnumerable<Event> events);
}

public interface IEventStorage<TAggregate> : IEventStorage where TAggregate : IAggregate { }