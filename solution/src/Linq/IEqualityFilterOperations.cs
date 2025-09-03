namespace Fuxion.Linq;

public interface IEqualityFilterOperations<T>
{
	T? Equal { get; set; }
	T? NotEqual { get; set; }
}