using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class FloatDao
{
	public float Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class FloatFilter
{
	public static readonly IFilterDescriptor<FloatDao>[] Fields = FilterBuilder
		.For<FloatDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestFloat
{
	IQueryable<FloatDao> GetQueryable(params float[] values) => values.Select(v => new FloatDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable(1f, 2f, 1f);
		var f = new FloatFilter();
		f.Property.Equal = 1f;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new float[] { 1f, 1f }, r);
	}

	[Fact]
	public void NotEqual()
	{
		var q = GetQueryable(1f, 2f, 3f);
		var f = new FloatFilter();
		f.Property.NotEqual = 2f;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new float[] { 1f, 3f }, r);
	}

	[Fact]
	public void In()
	{
		var q = GetQueryable(1f, 2f, 3f, 4f);
		var f = new FloatFilter();
		f.Property.In = new float[] { 2f, 4f };
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new float[] { 2f, 4f }, r);
	}

	[Fact]
	public void Between()
	{
		var q = GetQueryable(0.5f, 1f, 1.5f, 2f);
		var f = new FloatFilter();
		f.Property.BetweenFrom = 1f;
		f.Property.BetweenTo = 2f;
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x => x).ToList();
		Assert.Equal(new float[] { 1f, 1.5f, 2f }, r);
	}
}
