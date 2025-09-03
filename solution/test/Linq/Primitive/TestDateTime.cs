using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class DateTimeDao
{
	public DateTime Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class DateTimeFilter
{
	public static readonly IFilterDescriptor<DateTimeDao>[] Fields = FilterBuilder
		.For<DateTimeDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestDateTime
{
	IQueryable<DateTimeDao> GetQueryable(params DateTime[] values) => values.Select(v => new DateTimeDao { Property = v }).AsQueryable();

	[Fact]
	public void GreaterOrEqual()
	{
		var baseDay = new DateTime(2024, 01, 01);
		var q = GetQueryable(baseDay, baseDay.AddDays(1), baseDay.AddDays(2));
		var f = new DateTimeFilter();
		f.Property.GreaterOrEqual = baseDay.AddDays(1);
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{baseDay.AddDays(1), baseDay.AddDays(2)}, r);
	}

	[Fact]
	public void Between()
	{
		var baseDay = new DateTime(2024, 01, 01);
		var q = GetQueryable(baseDay, baseDay.AddDays(1), baseDay.AddDays(2), baseDay.AddDays(3));
		var f = new DateTimeFilter();
		f.Property.BetweenFrom = baseDay.AddDays(1);
		f.Property.BetweenTo = baseDay.AddDays(2);
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{baseDay.AddDays(1), baseDay.AddDays(2)}, r);
	}
}
