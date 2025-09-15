﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;
using Fuxion.Xunit;
using Fuxion.Windows.Data;
using Xunit;

namespace Fuxion.Windows.Test.Data;

public class EnumToNamedEnumValueConverterTest(ITestOutputHelper output)
	: BaseTest<EnumToNamedEnumValueConverterTest>(output)
{
	[Fact(DisplayName = "EnumToNamedEnumValueConverter - Display value")]
	public void EnumToNamedEnumValueConverter_DisplayValue()
	{
		var res = new EnumToNamedEnumValueConverter().Convert(EnumTest.One, CultureInfo.CurrentCulture);
		Assert.Equal("One value", res.ToString());
	}
	[Fact(DisplayName = "EnumToNamedEnumValueConverter - Null value")]
	public void EnumToNamedEnumValueConverter_NullValue()
	{
		Assert.Throws<NotSupportedException>(() => { ((IValueConverter)new EnumToNamedEnumValueConverter()).Convert(null, typeof(EnumTest), null, CultureInfo.CurrentCulture); });
		var res = ((IValueConverter)new NullableEnumToNamedEnumValueConverter()).Convert(null, typeof(EnumTest), null, CultureInfo.CurrentCulture);
		Assert.Null(res?.ToString());
	}
}

public enum EnumTest
{
	[Display(Name = "One value")]
	One,
	[Display(Name = "Two value")]
	Two
}