using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class DecimalDao
{
	public decimal Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class DecimalFilter
{
	public static readonly IFilterDescriptor<DecimalDao>[] Fields = FilterBuilder
		.For<DecimalDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestDecimal
{
	IQueryable<DecimalDao> GetQueryable(params decimal[] values) => values.Select(v => new DecimalDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable(1.1m, 2.2m, 1.1m);
		var f = new DecimalFilter();
		f.Property.Equal = 1.1m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal[] { 1.1m, 1.1m }, r);
	}

	[Fact]
	public void NotEqual()
	{
		var q = GetQueryable(1.1m, 2.2m, 3.3m);
		var f = new DecimalFilter();
		f.Property.NotEqual = 2.2m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal[] { 1.1m, 3.3m }, r);
	}

	[Fact]
	public void In()
	{
		var q = GetQueryable(1m, 2m, 3m, 4m);
		var f = new DecimalFilter();
		f.Property.In = new decimal[] { 2m, 4m };
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal[] { 2m, 4m }, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(1m, 2m, 3m);
		var f = new DecimalFilter();
		f.Property.GreaterThan = 2m;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new decimal[] { 3m }, r);
	}

	[Fact]
	public void LessOrEqual()
	{
		var q = GetQueryable(1m, 2m, 3m);
		var f = new DecimalFilter();
		f.Property.LessOrEqual = 2m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal[] { 1m, 2m }, r);
	}

	[Fact]
	public void Between()
	{
		var q = GetQueryable(0.5m, 1m, 1.5m, 2m, 3m);
		var f = new DecimalFilter();
		f.Property.BetweenFrom = 1m;
		f.Property.BetweenTo = 2m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal[] { 1m, 1.5m, 2m }, r);
	}

	[Fact]
	public void Equal_WithOtherConstraints()
	{
		var q = GetQueryable(1m, 2m, 3m, 2m);
		var f = new DecimalFilter();
		f.Property.Equal = 2m;
		f.Property.GreaterThan = 1m;
		f.Property.LessThan = 3m;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new decimal[] { 2m, 2m }, r);
	}
}
