namespace Fuxion.Linq;

public interface INullabilityFilterOperations
{
	bool? IsNull { get; set; }
	bool? IsNotNull { get; set; }
}