using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Fuxion.Xunit;
using Fuxion.Windows.Data;
using Xunit;

namespace Fuxion.Windows.Test.Data;

public class GenericConverterTest(ITestOutputHelper output) : BaseTest<GenericConverterTest>(output)
{
	[Fact(DisplayName = "GenericConverterTest - UnsetValues")]
	public void GenericMultiConverterTest_UnsetValues()
	{
		var c = new BooleanToVisibilityConverter {
			TrueValue = Visibility.Hidden
		};
		var res = new Func<Visibility>(() => (Visibility)((IValueConverter)c).Convert(DependencyProperty.UnsetValue, typeof(bool), null, CultureInfo.CurrentCulture));
		Assert.Throws<NotSupportedException>(() => res());
		c.AllowUnsetValue = true;
		Assert.Equal(Visibility.Visible, res());
		c.UnsetValue = Visibility.Hidden;
		Assert.Equal(Visibility.Hidden, res());
	}
}