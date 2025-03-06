using Fuxion.Domain;

namespace Fuxion.Application.Events;

public class PublicationEventFeature : IFeature<Event>
{
	public DateTime Timestamp { get; internal set; }
#if NETSTANDARD2_0 || NET472
	public void OnAttach(Event featurizable) { }
	public void OnDetach(Event featurizable) { }
#endif
}