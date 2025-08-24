using Fuxion.Domain.Aggregates;
using Xunit;

namespace Fuxion.Domain.Test;

public class AggregateTest
{
	[Fact(DisplayName = "Aggregate - ApplyEvent")]
	public void ApplyEvent()
	{
		// var agg = new Mock<MockAggregate>();
		// var evt = new TestedEvent(agg.Object.Id);
		// Assert.Throws<AggregateFeatureNotFoundException>(() => agg.Object.ApplyEvent(evt));
		// agg.Object.Features().AttachEvents();
		// agg.Object.ApplyEvent(evt);
		// Assert.Throws<AggregateStateMismatchException>(() => agg.Object.ApplyEvent(new TestedEvent(Guid.NewGuid())));
		// Assert.Throws<AggregateApplyEventMethodMissingException>(() => agg.Object.ApplyEvent(new Mock<Event>(agg.Object.Id).Object));
		// agg.Verify(a => a.WhenTested(It.IsAny<TestedEvent>()), Times.Once);
		// Assert.Single(agg.Object.GetPendingEvents());
	}
}

public record TestedEvent : Fuxion.Domain.Event
{
	public TestedEvent(Guid aggregateId) : base(aggregateId) { }
}

public record MockAggregate :
#if OLD_FRAMEWORKS
	Featurizable<MockAggregate>,
#endif
	IAggregate
{
	[AggregateEventHandler]
	public virtual void WhenTested(TestedEvent @event) { }

	public Guid Id
	{
		get;
		init;
	}
#if OLD_FRAMEWORKS
		= Guid.NewGuid();
#endif
	IFeatureCollection<IAggregate> IFeaturizable<IAggregate>.Features { get; } =
#if OLD_FRAMEWORKS
		new FeatureCollection<IAggregate>();
#else
		IFeatureCollection<IAggregate>.Create();
#endif
}

public record User :
#if OLD_FRAMEWORKS
	Featurizable<User>,
#endif
	IAggregate
{
	
	public IFeatureCollection<IAggregate> Features { get; } =
#if OLD_FRAMEWORKS
		new FeatureCollection<IAggregate>();
#else
		IFeatureCollection<IAggregate>.Create();
#endif
	public Guid Id
	{
		get;
#if !OLD_FRAMEWORKS
		init;
#endif
	}
#if OLD_FRAMEWORKS
		= Guid.NewGuid();
#endif
	public DateTime BirthdayDate { get; private set; }
	
	public void ChangeName(string newName){}
	public void ChangeBirthdayDate(DateTime newBirthdayDate){}
}

public class ValidationUserFeature
{
	
}
