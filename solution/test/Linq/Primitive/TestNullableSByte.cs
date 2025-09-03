using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableSByteDao
{
	public sbyte? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableSByteFilter
{
	public static readonly IFilterDescriptor<NullableSByteDao>[] Fields = FilterBuilder
		.For<NullableSByteDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableSByte
{
	IQueryable<NullableSByteDao> GetQueryable(params sbyte?[] values) => values.Select(v => new NullableSByteDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable((sbyte)-1, null, (sbyte)2, null);
		var f = new NullableSByteFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}
}
