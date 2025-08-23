using Fuxion.Pods;
using Fuxion.Reflection;

namespace Fuxion.Domain;

[UriKey(UriKey.FuxionBaseUri + "domain/event/1.0.0")]
public abstract record Event(Guid aggregateId) :
#if STANDARD_OR_OLD_FRAMEWORKS
	Featurizable<Event>,
#endif
	IFeaturizable<Event>
{
	public Guid AggregateId { get; set; } = aggregateId;
	IFeatureCollection<Event> IFeaturizable<Event>.Features { get; } = new FeatureCollection<Event>();
}

public static class EventExtensions
{
	public static IFeaturizable<Event> Features(this Event me) => me.Features<Event>();
}