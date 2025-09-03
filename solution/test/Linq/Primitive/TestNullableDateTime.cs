using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableDateTimeDao
{
	public DateTime? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableDateTimeFilter
{
	public static readonly IFilterDescriptor<NullableDateTimeDao>[] Fields = FilterBuilder
		.For<NullableDateTimeDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableDateTime
{
	IQueryable<NullableDateTimeDao> GetQueryable(params DateTime?[] values) => values.Select(v => new NullableDateTimeDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var d = new DateTime(2024,1,1);
		var q = GetQueryable(d, null, d.AddDays(1), null);
		var f = new NullableDateTimeFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, x => Assert.Null(x));
	}

	[Fact]
	public void GreaterThan()
	{
		var d = new DateTime(2024,1,1);
		var q = GetQueryable(d, null, d.AddDays(1), d.AddDays(2));
		var f = new NullableDateTimeFilter();
		f.Property.GreaterThan = d.AddDays(1);
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new DateTime?[]{ d.AddDays(2) }, r);
	}
}
