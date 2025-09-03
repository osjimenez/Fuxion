using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class DoubleDao
{
	public double Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class DoubleFilter
{
	public static readonly IFilterDescriptor<DoubleDao>[] Fields = FilterBuilder
		.For<DoubleDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestDouble
{
	IQueryable<DoubleDao> GetQueryable(params double[] values) => values.Select(v => new DoubleDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable(1d, 2d, 1d);
		var f = new DoubleFilter();
		f.Property.Equal = 1d;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new double[]{1d,1d}, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(0.5d,2d,3d);
		var f = new DoubleFilter();
		f.Property.GreaterThan = 2d;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new double[]{3d}, r);
	}

	[Fact]
	public void Between()
	{
		var q = GetQueryable(0.5d,1d,1.5d,2d);
		var f = new DoubleFilter();
		f.Property.BetweenFrom = 1d;
		f.Property.BetweenTo = 2d;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new double[]{1d,1.5d,2d}, r);
	}
}
