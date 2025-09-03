using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class StringDao
{
	public string Property { get; set; } = string.Empty;
}

[FilterSchema(nameof(Fields))]
public partial class StringFilter
{
	public static readonly IFilterDescriptor<StringDao>[] Fields = FilterBuilder
		.For<StringDao>()
		.Property(d => d.Property, b => b.CaseInsensitive())
		.Build();
}

public class TestString
{
	IQueryable<StringDao> GetQueryable(params string[] values) => values.Select(v => new StringDao { Property = v }).AsQueryable();

	[Fact]
	public void Equal()
	{
		var q = GetQueryable("Alpha", "Beta", "Alpha");
		var f = new StringFilter();
		f.Property.Equal = "Alpha";
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"Alpha","Alpha"}, r);
	}

	[Fact]
	public void NotEqual()
	{
		var q = GetQueryable("Alpha", "Beta", "Gamma");
		var f = new StringFilter();
		f.Property.NotEqual = "Beta";
		var r = q.Filter(f).Select(x => x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"Alpha","Gamma"}, r);
	}

	[Fact]
	public void In()
	{
		var q = GetQueryable("A","B","C","D");
		var f = new StringFilter();
		f.Property.In = new[]{"B","D"};
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"B","D"}, r);
	}

	[Fact]
	public void Contains_CaseInsensitive()
	{
		var q = GetQueryable("Alpha","beta","ALPHANUM");
		var f = new StringFilter();
		f.Property.Contains = "alpha";
		f.Property.CaseInsensitive = true;
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"Alpha", "ALPHANUM" }, r);
	}

	[Fact]
	public void StartsWith()
	{
		var q = GetQueryable("main","MainRoad","road");
		var f = new StringFilter();
		f.Property.StartsWith = "Main";
		f.Property.CaseInsensitive = true;
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"main", "MainRoad" }, r);
	}

	[Fact]
	public void EndsWith()
	{
		var q = GetQueryable("test.cs","file.TXT","note.txt");
		var f = new StringFilter();
		f.Property.EndsWith = ".txt";
		f.Property.CaseInsensitive = true;
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"file.TXT","note.txt"}, r);
	}

	[Fact]
	public void Empty()
	{
		var q = GetQueryable("", "A", "");
		var f = new StringFilter();
		f.Property.Empty = true;
		var r = q.Filter(f).Select(x=>x.Property).ToList();
		Assert.Equal(new[]{"",""}, r);
	}

	[Fact]
	public void NotEmpty()
	{
		var q = GetQueryable("", "A", "B");
		var f = new StringFilter();
		f.Property.NotEmpty = true;
		var r = q.Filter(f).Select(x=>x.Property).OrderBy(x=>x).ToList();
		Assert.Equal(new[]{"A","B"}, r);
	}
}
