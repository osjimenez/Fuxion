using System;
using Fuxion.Domain;

namespace Fuxion.Application.Events;

public class PublicationEventFeature : IFeature<Event>
{
	public DateTime Timestamp { get; internal set; }
#if STANDARD_OR_OLD_FRAMEWORKS
	public void OnAttach(Event featurizable) { }
	public void OnDetach(Event featurizable) { }
#endif
}