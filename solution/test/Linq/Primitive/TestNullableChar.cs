using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableCharDao
{
	public char? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableCharFilter
{
	public static readonly IFilterDescriptor<NullableCharDao>[] Fields = FilterBuilder
		.For<NullableCharDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableChar
{
	IQueryable<NullableCharDao> GetQueryable(params char?[] values) => values.Select(v => new NullableCharDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var q = GetQueryable('a', null, 'b', null);
		var f = new NullableCharFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Null(v));
	}

	[Fact]
	public void Equal_Value()
	{
		var q = GetQueryable('a', null, 'a');
		var f = new NullableCharFilter();
		f.Property.Equal = 'a';
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new char?[]{'a','a'}, r);
	}
}
