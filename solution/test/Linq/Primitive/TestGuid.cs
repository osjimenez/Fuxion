using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class GuidDao
{
	public Guid Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class GuidFilter
{
	public static readonly IFilterDescriptor<GuidDao>[] Fields = FilterBuilder
		.For<GuidDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestGuid
{
	IQueryable<GuidDao> GetQueryable(params Guid[] values) => values.Select(v => new GuidDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var a = Guid.NewGuid();
		var b = Guid.NewGuid();
		var c = a;
		var q = GetQueryable(a, b, c);
		var f = new GuidFilter();
		f.Property.Equal = a;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, x => Assert.Equal(a, x));
	}

	[Fact]
	public void In()
	{
		var a = Guid.NewGuid();
		var b = Guid.NewGuid();
		var c = Guid.NewGuid();
		var q = GetQueryable(a, b, c);
		var f = new GuidFilter();
		f.Property.In = new[] { b, c };
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{b,c}.OrderBy(x=>x), r);
	}
}
