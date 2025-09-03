using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class SByteDao
{
	public sbyte Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class SByteFilter
{
	public static readonly IFilterDescriptor<SByteDao>[] Fields = FilterBuilder
		.For<SByteDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestSByte
{
	IQueryable<SByteDao> GetQueryable(params sbyte[] values) => values.Select(v => new SByteDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable((sbyte)-1,(sbyte)2,(sbyte)-1);
		var f = new SByteFilter();
		f.Property.Equal = -1;
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new sbyte[]{-1,-1}, r);
	}
}
