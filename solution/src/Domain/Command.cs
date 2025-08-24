namespace Fuxion.Domain;

public abstract record Command(Guid aggregateId) :
#if STANDARD_OR_OLD_FRAMEWORKS
	Featurizable<Command>,
#endif
	IFeaturizable<Command>
{
	public Guid AggregateId { get; set; } = aggregateId;
	IFeatureCollection<Command> IFeaturizable<Command>.Features { get; } = new FeatureCollection<Command>();
}
public static class CommandExtensions
{
	public static IFeaturizable<Command> Features(this Command me) => me.Features<Command>();
}