using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class ByteDao
{
	public byte Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class ByteFilter
{
	public static readonly IFilterDescriptor<ByteDao>[] Fields = FilterBuilder
		.For<ByteDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestByte
{
	IQueryable<ByteDao> GetQueryable(params byte[] values) => values.Select(v => new ByteDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable((byte)1,(byte)2,(byte)1);
		var f = new ByteFilter();
		f.Property.Equal = 1;
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new byte[]{1,1}, r);
	}
}
