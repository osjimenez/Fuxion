using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class IntDao
{
	public int Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class IntFilter
{
	public static readonly IFilterDescriptor<IntDao>[] Fields = FilterBuilder
		.For<IntDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestInt
{
	IQueryable<IntDao> GetQueryable(params int[] values) => values.Select(v => new IntDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable(5, 7, 5);
		var f = new IntFilter();
		f.Property.Equal = 5;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 5, 5 }, r);
	}

	[Fact]
	public void NotEqual()
	{
		var q = GetQueryable(5, 7, 9);
		var f = new IntFilter();
		f.Property.NotEqual = 7;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 5, 9 }, r);
	}

	[Fact]
	public void In()
	{
		var q = GetQueryable(1, 2, 3, 4, 5);
		var f = new IntFilter();
		f.Property.In = new[] { 2, 4 };
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 2, 4 }, r);
	}

	[Fact]
	public void In_Empty_DoesNotFilter()
	{
		var q = GetQueryable(1, 2, 3);
		var f = new IntFilter();
		f.Property.In = Array.Empty<int>();
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 1, 2, 3 }, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(1, 5, 8);
		var f = new IntFilter();
		f.Property.GreaterThan = 5;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new[] { 8 }, r);
	}

	[Fact]
	public void GreaterOrEqual()
	{
		var q = GetQueryable(4, 5, 6);
		var f = new IntFilter();
		f.Property.GreaterOrEqual = 5;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 5, 6 }, r);
	}

	[Fact]
	public void LessThan()
	{
		var q = GetQueryable(2, 5, 9);
		var f = new IntFilter();
		f.Property.LessThan = 5;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new[] { 2 }, r);
	}

	[Fact]
	public void LessOrEqual()
	{
		var q = GetQueryable(2, 5, 9);
		var f = new IntFilter();
		f.Property.LessOrEqual = 5;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 2, 5 }, r);
	}

	[Fact]
	public void Between()
	{
		var q = GetQueryable(1, 5, 7, 10);
		var f = new IntFilter();
		f.Property.BetweenFrom = 5;
		f.Property.BetweenTo = 9;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 5, 7 }, r);
	}

	[Fact]
	public void Between_OpenUpper()
	{
		var q = GetQueryable(1, 5, 7, 10);
		var f = new IntFilter();
		f.Property.BetweenFrom = 7;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 7, 10 }, r);
	}

	[Fact]
	public void Between_OpenLower()
	{
		var q = GetQueryable(1, 5, 7, 10);
		var f = new IntFilter();
		f.Property.BetweenTo = 5;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 1, 5 }, r);
	}

	[Fact]
	public void Equal_WithOtherConstraints()
	{
		var q = GetQueryable(1, 2, 3, 2, 4);
		var f = new IntFilter();
		f.Property.Equal = 2;
		f.Property.GreaterThan = 1;
		f.Property.LessThan = 3;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 2, 2 }, r);
	}
}