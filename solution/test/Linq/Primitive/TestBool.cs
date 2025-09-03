using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class BoolDao
{
	public bool Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class BoolFilter
{
	public static readonly IFilterDescriptor<BoolDao>[] Fields = FilterBuilder
		.For<BoolDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestBool
{
	IQueryable<BoolDao> GetQueryable(params bool[] values) => values.Select(v => new BoolDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable(true, false, true);
		var f = new BoolFilter();
		f.Property.Equal = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.True(v));
	}

	[Fact]
	public void NotEqual()
	{
		var q = GetQueryable(true, false, true);
		var f = new BoolFilter();
		f.Property.NotEqual = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Single(r);
		Assert.False(r[0]);
	}

	[Fact]
	public void In()
	{
		var q = GetQueryable(true, false, true);
		var f = new BoolFilter();
		f.Property.In = new[] { false };
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Single(r);
		Assert.False(r[0]);
	}

	[Fact]
	public void In_Empty_DoesNotFilter()
	{
		var q = GetQueryable(true, false);
		var f = new BoolFilter();
		f.Property.In = System.Array.Empty<bool>();
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{false,true}, r);
	}
}
