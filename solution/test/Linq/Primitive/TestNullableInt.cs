using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableIntDao
{
	public int? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableIntFilter
{
	public static readonly IFilterDescriptor<NullableIntDao>[] Fields = FilterBuilder
		.For<NullableIntDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableInt
{
	IQueryable<NullableIntDao> GetQueryable(params int?[] values) => values.Select(v => new NullableIntDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable(5, null, 7, null);
		var f = new NullableIntFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void IsNotNull()
	{
		var q = GetQueryable(5, null, 7, null);
		var f = new NullableIntFilter();
		f.Property.IsNotNull = true;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new int?[] { 5, 7 }, r);
	}

	[Fact]
	public void Equal_Null_Alias_IsNull()
	{
		var q = GetQueryable(5, null, 7);
		var f = new NullableIntFilter();
		f.Property.Equal = null;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Single(r);
		Assert.Null(r[0]);
	}

	[Fact]
	public void Equal_Value()
	{
		var q = GetQueryable(5, null, 7, 5);
		var f = new NullableIntFilter();
		f.Property.Equal = 5;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new int?[] { 5, 5 }, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(1, null, 5, 8);
		var f = new NullableIntFilter();
		f.Property.GreaterThan = 5;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new int?[] { 8 }, r);
	}

	[Fact]
	public void Between_WithNullsIgnored()
	{
		var q = GetQueryable(null, 1, 5, 7, 10, null);
		var f = new NullableIntFilter();
		f.Property.BetweenFrom = 5;
		f.Property.BetweenTo = 9;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new int?[] { 5, 7 }, r);
	}

	[Fact]
	public void Between_OpenLower_WithNulls()
	{
		var q = GetQueryable(null, 1, 5, 7, 10);
		var f = new NullableIntFilter();
		f.Property.BetweenTo = 5;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new int?[] { 1, 5 }, r);
	}

	[Fact]
	public void NullAndValueConstraint()
	{
		var q = GetQueryable(null, 2, 3);
		var f = new NullableIntFilter();
		f.Property.IsNull = true;
		f.Property.GreaterThan = 1;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		// Conjunción lógica: (x == null) AND (x.HasValue && x.Value > 1) => nunca cierto => vacío
		Assert.Empty(r);
	}

	[Fact]
	public void Contradictory_IsNull_And_IsNotNull()
	{
		var q = GetQueryable(null, 2);
		var f = new NullableIntFilter();
		f.Property.IsNull = true;
		f.Property.IsNotNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Empty(r);
	}
}