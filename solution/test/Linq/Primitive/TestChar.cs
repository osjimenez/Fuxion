using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class CharDao
{
	public char Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class CharFilter
{
	public static readonly IFilterDescriptor<CharDao>[] Fields = FilterBuilder
		.For<CharDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestChar
{
	IQueryable<CharDao> GetQueryable(params char[] values) => values.Select(v => new CharDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable('a','b','a');
		var f = new CharFilter();
		f.Property.Equal = 'a';
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{'a','a'}, r);
	}
	[Fact]
	public void GreaterThan()
	{
		var q = GetQueryable('a','c','d');
		var f = new CharFilter();
		f.Property.GreaterThan = 'b';
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{'c','d'}, r);
	}
}
