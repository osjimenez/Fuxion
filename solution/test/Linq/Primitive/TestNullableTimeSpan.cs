using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableTimeSpanDao
{
	public TimeSpan? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableTimeSpanFilter
{
	public static readonly IFilterDescriptor<NullableTimeSpanDao>[] Fields = FilterBuilder
		.For<NullableTimeSpanDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableTimeSpan
{
	IQueryable<NullableTimeSpanDao> GetQueryable(params TimeSpan?[] values) => values.Select(v => new NullableTimeSpanDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), null, TimeSpan.FromSeconds(2), null);
		var f = new NullableTimeSpanFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void GreaterOrEqual()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), null, TimeSpan.FromSeconds(2));
		var f = new NullableTimeSpanFilter();
		f.Property.GreaterOrEqual = TimeSpan.FromSeconds(2);
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(new TimeSpan?[]{TimeSpan.FromSeconds(2)}, r);
	}
}
