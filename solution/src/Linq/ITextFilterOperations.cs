namespace Fuxion.Linq;

public interface ITextFilterOperations<T>
{
	T? Contains { get; set; }
	T? StartsWith { get; set; }
	T? EndsWith { get; set; }
	bool? Empty { get; set; }
	bool? NotEmpty { get; set; }
	bool? CaseInsensitive { get; set; }
}