using System.Collections.Concurrent;
using System.Reflection;

namespace Fuxion.Domain.Aggregates;

public class EventsAggregateFeature : IFeature<IAggregate>
{
	static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> aggregateEventHandlerCache = new();

	// Pending events
	readonly ConcurrentStack<Event> pendingEvents = new();
	IAggregate? _aggregate;
	// Event handlers
	Dictionary<Type, MethodInfo> eventHandlerCache = new();
	public void OnAttach(IAggregate aggregate)
	{
		_aggregate = aggregate;
		// Setup internal event handlers
		var aggregateType = aggregate.GetType();
		aggregateEventHandlerCache.AddOrUpdate(aggregateType,
			type => type.GetRuntimeMethods()
				.Where(m => m.ReturnType == typeof(void) && m.GetCustomAttribute<AggregateEventHandlerAttribute>(true) != null && m.GetParameters().Count() == 1
					&& typeof(Event).IsAssignableFrom(m.GetParameters().First().ParameterType)).ToDictionary(m => m.GetParameters().First().ParameterType), (_, __) => __);
		eventHandlerCache = aggregateEventHandlerCache[aggregateType].ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}
	#if NETSTANDARD2_0 || NET472
	public void OnDetach(IAggregate aggregate) { }
	#endif
	public event EventHandler<EventArgs<Event>>? Applying;
	public event EventHandler<EventArgs<Event>>? Validated;
	public event EventHandler<EventArgs<Event>>? Handled;
	public event EventHandler<EventArgs<Event>>? Pendent;

	// Events appling
	public void ApplyEvent(Event @event)
	{
		Applying?.Invoke(this, new(@event));
		Validate(@event);
		Validated?.Invoke(this, new(@event));
		Handle(@event);
		Handled?.Invoke(this, new(@event));
		pendingEvents.Push(@event);
		Pendent?.Invoke(this, new(@event));
	}
	internal void Validate(Event @event)
	{
		if (_aggregate?.Id != @event.AggregateId) throw new AggregateStateMismatchException($"Aggregate Id is '{_aggregate?.Id}' and event.AggregateId is '{@event.AggregateId}'");
	}
	internal void Handle(Event @event)
	{
		if (eventHandlerCache.ContainsKey(@event.GetType()))
			eventHandlerCache[@event.GetType()].Invoke(_aggregate, new object[] {
				@event
			});
		else
			throw new AggregateApplyEventMethodMissingException($"No event handler specified for '{@event.GetType()}' on '{GetType()}'");
	}
	public bool HasPendingEvents() => !pendingEvents.IsEmpty;
	public IEnumerable<Event> GetPendingEvents() => pendingEvents.ToArray();
	public void ClearPendingEvents() => pendingEvents.Clear();
}