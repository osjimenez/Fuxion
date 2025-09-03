using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableFloatDao
{
	public float? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableFloatFilter
{
	public static readonly IFilterDescriptor<NullableFloatDao>[] Fields = FilterBuilder
		.For<NullableFloatDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableFloat
{
	IQueryable<NullableFloatDao> GetQueryable(params float?[] values) => values.Select(v => new NullableFloatDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable(1f, null, 2f, null);
		var f = new NullableFloatFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void Equal_Value()
	{
		var q = GetQueryable(1f, null, 1f);
		var f = new NullableFloatFilter();
		f.Property.Equal = 1f;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new float?[]{1f,1f}, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(0.5f, null, 2f, 3f);
		var f = new NullableFloatFilter();
		f.Property.GreaterThan = 2f;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new float?[]{3f}, r);
	}

	[Fact]
	public void Between_WithNullsIgnored()
	{
		var q = GetQueryable(null, 0.5f, 1f, 1.5f, 2f, null);
		var f = new NullableFloatFilter();
		f.Property.BetweenFrom = 1f;
		f.Property.BetweenTo = 2f;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new float?[]{1f,1.5f,2f}, r);
	}
}
