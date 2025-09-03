using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class TimeSpanDao
{
	public TimeSpan Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class TimeSpanFilter
{
	public static readonly IFilterDescriptor<TimeSpanDao>[] Fields = FilterBuilder
		.For<TimeSpanDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestTimeSpan
{
	IQueryable<TimeSpanDao> GetQueryable(params TimeSpan[] values) => values.Select(v => new TimeSpanDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
		var f = new TimeSpanFilter();
		f.Property.Equal = TimeSpan.FromSeconds(1);
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)}, r);
	}

	[Fact]
	public void NotEqual()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
		var f = new TimeSpanFilter();
		f.Property.NotEqual = TimeSpan.FromSeconds(2);
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)}, r);
	}

	[Fact]
	public void In()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
		var f = new TimeSpanFilter();
		f.Property.In = new[]{ TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3) };
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)}, r);
	}

	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8));
		var f = new TimeSpanFilter();
		f.Property.GreaterThan = TimeSpan.FromSeconds(5);
		var r = q.Filter(f).Select(x=>x.Property).ToList();
		Assert.Equal(new[]{TimeSpan.FromSeconds(8)}, r);
	}

	[Fact]
	public void Between()
	{
		var q = GetQueryable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
		var f = new TimeSpanFilter();
		f.Property.BetweenFrom = TimeSpan.FromSeconds(5);
		f.Property.BetweenTo = TimeSpan.FromSeconds(9);
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7)}, r);
	}
}
