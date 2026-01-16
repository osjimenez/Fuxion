using Fuxion.Windows.Data;
using Fuxion.Xunit;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Fuxion.Windows.Test.Data;

public class StringToCapitalizedStringConverterTest(ITestOutputHelper output)
	: BaseTest<StringToCapitalizedStringConverterTest>(output)
{
	public static IEnumerable<object?[]> GenerateTheoryParameters()
	{
		List<(string Input, StringCapitalization Capitalization, string Output)> inputs =
		[
			// Camel case
			("test string", StringCapitalization.ToCamelCase, "testString"),
			("test String", StringCapitalization.ToCamelCase, "testString"),
			("testString", StringCapitalization.ToCamelCase, "testString"),
			("TestString", StringCapitalization.ToCamelCase, "testString"),
			("TEST STRING", StringCapitalization.ToCamelCase, "testString"),
			// Pascal case
			("test string", StringCapitalization.ToPascalCase, "TestString"),
			("test String", StringCapitalization.ToPascalCase, "TestString"),
			("testString", StringCapitalization.ToPascalCase, "TestString"),
			("TestString", StringCapitalization.ToPascalCase, "TestString"),
			("TEST STRING", StringCapitalization.ToPascalCase, "TestString"),
			// Title case
			("test string", StringCapitalization.ToTitleCase, "Test String"),
			("test String", StringCapitalization.ToTitleCase, "Test String"),
			("testString", StringCapitalization.ToTitleCase, "Test String"),
			("TestString", StringCapitalization.ToTitleCase, "Test String"),
			("TEST STRING", StringCapitalization.ToTitleCase, "Test String"),
			// To upper
			("test string", StringCapitalization.ToUpper, "TEST STRING"),
			("test String", StringCapitalization.ToUpper, "TEST STRING"),
			("testString", StringCapitalization.ToUpper, "TESTSTRING"),
			("TestString", StringCapitalization.ToUpper, "TESTSTRING"),
			("TEST STRING", StringCapitalization.ToUpper, "TEST STRING"),
			// To lower
			("test string", StringCapitalization.ToLower, "test string"),
			("test String", StringCapitalization.ToLower, "test string"),
			("testString", StringCapitalization.ToLower, "teststring"),
			("TestString", StringCapitalization.ToLower, "teststring"),
			("TEST STRING", StringCapitalization.ToLower, "test string"),
		];
		return inputs.Select(i => new object?[] { i.Input, i.Capitalization, i.Output});
	}
	public record Params(string Input, StringCapitalization Capitalization);
	public record Asserts(string Output);
	[Theory]
	[MemberData(nameof(GenerateTheoryParameters))]
	public void StringToCapitalizedStringConverter(string input, StringCapitalization capitalization, string output)
	{
		var res = new StringToCapitalizedStringConverter
		{
			Capitalization = capitalization
		}.Convert(input, CultureInfo.CurrentCulture);
		Assert.Equal(output, res);
	}
}