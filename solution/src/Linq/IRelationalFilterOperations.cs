namespace Fuxion.Linq;

public interface IRelationalFilterOperations<T>
{
	T? GreaterThan { get; set; }
	T? GreaterOrEqual { get; set; }
	T? LessThan { get; set; }
	T? LessOrEqual { get; set; }
	T? BetweenFrom { get; set; }
	T? BetweenTo { get; set; }
}