﻿using System.Text.Json.Serialization;
using Fuxion.Domain;

namespace Fuxion.Application.Snapshots;

public abstract class Snapshot
{
	internal Snapshot() { }
	[JsonInclude]
	public Guid Id { get; internal set; } = Guid.NewGuid();
	[JsonInclude]
	public Guid AggregateId { get; internal set; }
	[JsonInclude]
	public int Version { get; internal set; }
	internal abstract void Load(IAggregate aggregate);
}