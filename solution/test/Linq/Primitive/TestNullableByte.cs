using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableByteDao
{
	public byte? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableByteFilter
{
	public static readonly IFilterDescriptor<NullableByteDao>[] Fields = FilterBuilder
		.For<NullableByteDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableByte
{
	IQueryable<NullableByteDao> GetQueryable(params byte?[] values) => values.Select(v => new NullableByteDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable((byte)1, null, (byte)2, null);
		var f = new NullableByteFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}
}
