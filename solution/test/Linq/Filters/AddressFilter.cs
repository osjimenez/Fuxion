using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.Filters;

[FilterSchema(nameof(Fields))]
public partial class AddressFilter
{
	public static readonly IFilterDescriptor<AddressDao>[] Fields = FilterBuilder
		.For<AddressDao>()
		.Property(a => a.Street)
		.Property(a => a.Number)
		.Build();
}