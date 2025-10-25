using Fuxion.Linq;
using Test.Dataset.Daos;

namespace Test.Linq.Filters;

[FilterSchema(nameof(Fields))]
public partial class AddressFilter
{
	public static readonly IFilterDescriptor<AddressDao>[] Fields = FilterBuilder
		.For<AddressDao>("Address", "Addresses")
		.Property(a => a.Street)
		.Property(a => a.Number)
		.Build();
}