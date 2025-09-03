using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableDoubleDao
{
	public double? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableDoubleFilter
{
	public static readonly IFilterDescriptor<NullableDoubleDao>[] Fields = FilterBuilder
		.For<NullableDoubleDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableDouble
{
	IQueryable<NullableDoubleDao> GetQueryable(params double?[] values) => values.Select(v => new NullableDoubleDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable(1d, null, 2d, null);
		var f = new NullableDoubleFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void Equal_Value()
	{
		var q = GetQueryable(1d, null, 1d);
		var f = new NullableDoubleFilter();
		f.Property.Equal = 1d;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new double?[]{1d,1d}, r);
	}

	[Fact]
	public void Between_WithNullsIgnored()
	{
		var q = GetQueryable(null,0.5d,1d,1.5d,2d,null);
		var f = new NullableDoubleFilter();
		f.Property.BetweenFrom = 1d;
		f.Property.BetweenTo = 2d;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new double?[]{1d,1.5d,2d}, r);
	}
}
