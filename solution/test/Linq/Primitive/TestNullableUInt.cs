using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableUIntDao
{
	public uint? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableUIntFilter
{
	public static readonly IFilterDescriptor<NullableUIntDao>[] Fields = FilterBuilder
		.For<NullableUIntDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableUInt
{
	IQueryable<NullableUIntDao> GetQueryable(params uint?[] values) => values.Select(v => new NullableUIntDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable(5u, null, 7u, null);
		var f = new NullableUIntFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void IsNotNull()
	{
		var q = GetQueryable(5u, null, 7u, null);
		var f = new NullableUIntFilter();
		f.Property.IsNotNull = true;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new uint?[] { 5u, 7u }, r);
	}

	[Fact]
	public void Equal_Null_Alias_IsNull()
	{
		var q = GetQueryable(5u, null, 7u);
		var f = new NullableUIntFilter();
		f.Property.Equal = null;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Single(r);
		Assert.Null(r[0]);
	}

	[Fact]
	public void Equal_Value()
	{
		var q = GetQueryable(5u, null, 7u, 5u);
		var f = new NullableUIntFilter();
		f.Property.Equal = 5u;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new uint?[] { 5u, 5u }, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(1u, null, 5u, 8u);
		var f = new NullableUIntFilter();
		f.Property.GreaterThan = 5u;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new uint?[] { 8u }, r);
	}

	[Fact]
	public void Between_WithNullsIgnored()
	{
		var q = GetQueryable(null, 1u, 5u, 7u, 10u, null);
		var f = new NullableUIntFilter();
		f.Property.BetweenFrom = 5u;
		f.Property.BetweenTo = 9u;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new uint?[] { 5u, 7u }, r);
	}

	[Fact]
	public void Between_OpenLower_WithNulls()
	{
		var q = GetQueryable(null, 1u, 5u, 7u, 10u);
		var f = new NullableUIntFilter();
		f.Property.BetweenTo = 5u;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new uint?[] { 1u, 5u }, r);
	}

	[Fact]
	public void NullAndValueConstraint()
	{
		var q = GetQueryable(null, 2u, 3u);
		var f = new NullableUIntFilter();
		f.Property.IsNull = true;
		f.Property.GreaterThan = 1u;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Empty(r);
	}

	[Fact]
	public void Contradictory_IsNull_And_IsNotNull()
	{
		var q = GetQueryable(null, 2u);
		var f = new NullableUIntFilter();
		f.Property.IsNull = true;
		f.Property.IsNotNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Empty(r);
	}
}
