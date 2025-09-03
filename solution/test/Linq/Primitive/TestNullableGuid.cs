using System;
using System.Linq;
using Xunit;
using Fuxion.Linq;

namespace Fuxion.Linq.Test.Primitive;

public class NullableGuidDao
{
	public Guid? Property { get; set; }
}

[FilterSchema(nameof(Fields))]
public partial class NullableGuidFilter
{
	public static readonly IFilterDescriptor<NullableGuidDao>[] Fields = FilterBuilder
		.For<NullableGuidDao>()
		.Property(d => d.Property)
		.Build();
}

public class TestNullableGuid
{
	IQueryable<NullableGuidDao> GetQueryable(params Guid?[] values) => values.Select(v => new NullableGuidDao { Property = v }).AsQueryable();

	[Fact]
	public void IsNull()
	{
		var a = Guid.NewGuid();
		var b = Guid.NewGuid();
		var q = GetQueryable(a, null, b, null);
		var f = new NullableGuidFilter();
		f.Property.IsNull = true;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, x => Assert.Null(x));
	}

	[Fact]
	public void Equal_Value()
	{
		var a = Guid.NewGuid();
		var q = GetQueryable(a, null, a);
		var f = new NullableGuidFilter();
		f.Property.Equal = a;
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Equal(2, r.Count);
		Assert.All(r, v => Assert.Equal(a, v));
	}

	[Fact]
	public void Equal_Null_Alias_IsNull()
	{
		var a = Guid.NewGuid();
		var q = GetQueryable(a, null);
		var f = new NullableGuidFilter();
		f.Property.Equal = null; // expect only nulls
		var r = q.Filter(f).Select(x => x.Property).ToList();
		Assert.Single(r);
		Assert.Null(r[0]);
	}
}
