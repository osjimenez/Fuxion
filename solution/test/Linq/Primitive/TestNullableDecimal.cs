using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableDecimalDao
{
	public decimal? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableDecimalFilter
{
	public static readonly IFilterDescriptor<NullableDecimalDao>[] Fields = FilterBuilder
		.For<NullableDecimalDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableDecimal
{
	IQueryable<NullableDecimalDao> GetQueryable(params decimal?[] values) => values.Select(v => new NullableDecimalDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable(1m, null, 2m, null);
		var f = new NullableDecimalFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void Equal_Null_Alias_IsNull()
	{
		var q = GetQueryable(1m, null, 2m);
		var f = new NullableDecimalFilter();
		f.Property.Equal = null;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Single(r);
		Assert.Null(r[0]);
	}

	[Fact]
	public void Equal_Value()
	{
		var q = GetQueryable(1m, null, 2m, 1m);
		var f = new NullableDecimalFilter();
		f.Property.Equal = 1m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal?[] { 1m, 1m }, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(1m, null, 2m, 3m);
		var f = new NullableDecimalFilter();
		f.Property.GreaterThan = 2m;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new decimal?[] { 3m }, r);
	}

	[Fact]
	public void Between_WithNullsIgnored()
	{
		var q = GetQueryable(null, 0.5m, 1m, 1.5m, 2m, null);
		var f = new NullableDecimalFilter();
		f.Property.BetweenFrom = 1m;
		f.Property.BetweenTo = 2m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal?[] { 1m, 1.5m, 2m }, r);
	}

	[Fact]
	public void NullAndValueConstraint()
	{
		var q = GetQueryable(null, 2m);
		var f = new NullableDecimalFilter();
		f.Property.IsNull = true;
		f.Property.GreaterThan = 1m;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Empty(r);
	}
}
