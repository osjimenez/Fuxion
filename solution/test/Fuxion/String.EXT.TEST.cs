using Fuxion.Xunit;
using System;
using Xunit;

namespace Fuxion.Test;

public class StringExtensionsTest(ITestOutputHelper output) : BaseTest<StringExtensionsTest>(output)
{
	#region RemoveChar
	[Fact]
	public void RemoveChar_RemovesAllOccurrences()
	{
		var result = "hello-world-test".RemoveChar('-');
		PrintVariable(result);
		Assert.Equal("helloworldtest", result);
	}

	[Fact]
	public void RemoveChar_CharNotFound_ReturnsOriginal()
	{
		var result = "hello".RemoveChar('x');
		PrintVariable(result);
		Assert.Equal("hello", result);
	}

	[Fact]
	public void RemoveChar_EmptyString_ReturnsEmpty()
	{
		var result = "".RemoveChar('a');
		PrintVariable(result);
		Assert.Equal("", result);
	}
	#endregion

	#region SplitInLines
	[Fact]
	public void SplitInLines_HandlesWindowsLineEndings()
	{
		var text = "Line 1\r\nLine 2\r\nLine 3";
		var lines = text.SplitInLines();
		PrintVariable(lines);
		Assert.Equal(3, lines.Length);
		Assert.Equal("Line 1", lines[0]);
		Assert.Equal("Line 2", lines[1]);
		Assert.Equal("Line 3", lines[2]);
	}

	[Fact]
	public void SplitInLines_HandlesUnixLineEndings()
	{
		var text = "Line 1\nLine 2\nLine 3";
		var lines = text.SplitInLines();
		PrintVariable(lines);
		Assert.Equal(3, lines.Length);
	}

	[Fact]
	public void SplitInLines_HandlesMixedLineEndings()
	{
		var text = "Line 1\r\nLine 2\nLine 3\rLine 4";
		var lines = text.SplitInLines();
		PrintVariable(lines);
		Assert.Equal(4, lines.Length);
	}

	[Fact]
	public void SplitInLines_RemoveEmptyLines()
	{
		var text = "Line 1\r\n\r\nLine 3";
		var lines = text.SplitInLines(removeEmptyLines: true);
		PrintVariable(lines);
		Assert.Equal(2, lines.Length);
		Assert.Equal("Line 1", lines[0]);
		Assert.Equal("Line 3", lines[1]);
	}

	[Fact]
	public void SplitInLines_TrimEachLine()
	{
		var text = "  Line 1  \n  Line 2  ";
		var lines = text.SplitInLines(trimEachLine: true);
		PrintVariable(lines);
		Assert.Equal("Line 1", lines[0]);
		Assert.Equal("Line 2", lines[1]);
	}

	[Fact]
	public void SplitInLines_RemoveEmptyAndTrim()
	{
		var text = "  Line 1  \n\n  Line 3  ";
		var lines = text.SplitInLines(removeEmptyLines: true, trimEachLine: true);
		PrintVariable(lines);
		Assert.Equal(2, lines.Length);
		Assert.Equal("Line 1", lines[0]);
		Assert.Equal("Line 3", lines[1]);
	}
	#endregion

	#region EnsureEndsWith
	[Fact]
	public void EnsureEndsWith_AlreadyEnds_ReturnsOriginal()
	{
		var result = "hello".EnsureEndsWith("lo");
		PrintVariable(result);
		Assert.Equal("hello", result);
	}

	[Fact]
	public void EnsureEndsWith_PartialMatch_AppendsMinimal()
	{
		var result = "hel".EnsureEndsWith("llo");
		PrintVariable(result);
		Assert.Equal("hello", result);
	}

	[Fact]
	public void EnsureEndsWith_NoMatch_AppendsFull()
	{
		var result = "hello".EnsureEndsWith(" world");
		PrintVariable(result);
		Assert.Equal("hello world", result);
	}

	[Fact]
	public void EnsureEndsWith_PathExample()
	{
		var result = "path/to/dir".EnsureEndsWith("/");
		PrintVariable(result);
		Assert.Equal("path/to/dir/", result);
	}

	[Fact]
	public void EnsureEndsWith_PathAlreadyEnds()
	{
		var result = "path/to/dir/".EnsureEndsWith("/");
		PrintVariable(result);
		Assert.Equal("path/to/dir/", result);
	}

	[Fact]
	public void EnsureEndsWith_NullEnding_ReturnsOriginal()
	{
		var result = "hello".EnsureEndsWith(null!);
		PrintVariable(result);
		Assert.Equal("hello", result);
	}

	[Fact]
	public void EnsureEndsWith_EmptyEnding_ReturnsOriginal()
	{
		var result = "hello".EnsureEndsWith("");
		PrintVariable(result);
		Assert.Equal("hello", result);
	}
	#endregion

	#region SubstringFromEnd
	[Fact]
	public void SubstringFromEnd_TakesLastCharacters()
	{
		var result = "Hello World".SubstringFromEnd(5);
		PrintVariable(result);
		Assert.Equal("World", result);
	}

	[Fact]
	public void SubstringFromEnd_LengthGreaterThanString_ReturnsWhole()
	{
		var result = "Hello".SubstringFromEnd(10);
		PrintVariable(result);
		Assert.Equal("Hello", result);
	}

	[Fact]
	public void SubstringFromEnd_LengthEqualsString_ReturnsWhole()
	{
		var result = "Hello".SubstringFromEnd(5);
		PrintVariable(result);
		Assert.Equal("Hello", result);
	}

	[Fact]
	public void SubstringFromEnd_FileExtension()
	{
		var result = "file.txt".SubstringFromEnd(4);
		PrintVariable(result);
		Assert.Equal(".txt", result);
	}

	[Fact]
	public void SubstringFromEnd_NegativeLength_ThrowsException()
	{
		Throws<ArgumentOutOfRangeException>(() => "test".SubstringFromEnd(-1));
	}
	#endregion

	#region ToTitleCase
	[Fact]
	public void ToTitleCase_FromLowerCase()
	{
		var result = "hello world".ToTitleCase();
		PrintVariable(result);
		Assert.Equal("Hello World", result);
	}

	[Fact]
	public void ToTitleCase_FromCamelCase()
	{
		var result = "myVariableName".ToTitleCase();
		PrintVariable(result);
		Assert.Equal("My Variable Name", result);
	}

	[Fact]
	public void ToTitleCase_FromSnakeCase()
	{
		var result = "my_constant_name".ToTitleCase();
		PrintVariable(result);
		Assert.Equal("My Constant Name", result);
	}

	[Fact]
	public void ToTitleCase_FromKebabCase()
	{
		var result = "kebab-case-string".ToTitleCase();
		PrintVariable(result);
		Assert.Equal("Kebab Case String", result);
	}

	[Fact]
	public void ToTitleCase_WithAcronym()
	{
		var result = "XMLParser".ToTitleCase();
		PrintVariable(result);
		Assert.Equal("Xml Parser", result);
	}
	#endregion

	#region ToCamelCase
	[Fact]
	public void ToCamelCase_FromTitleCase()
	{
		var result = "Hello World".ToCamelCase();
		PrintVariable(result);
		Assert.Equal("helloWorld", result);
	}

	[Fact]
	public void ToCamelCase_FromPascalCase()
	{
		var result = "MyVariableName".ToCamelCase();
		PrintVariable(result);
		Assert.Equal("myVariableName", result);
	}

	[Fact]
	public void ToCamelCase_FromSnakeCase()
	{
		var result = "MY_CONSTANT_NAME".ToCamelCase();
		PrintVariable(result);
		Assert.Equal("myConstantName", result);
	}

	[Fact]
	public void ToCamelCase_FromKebabCase()
	{
		var result = "kebab-case-string".ToCamelCase();
		PrintVariable(result);
		Assert.Equal("kebabCaseString", result);
	}

	[Fact]
	public void ToCamelCase_WithAcronym()
	{
		var result = "XMLParser".ToCamelCase();
		PrintVariable(result);
		Assert.Equal("xmlParser", result);
	}

	[Fact]
	public void ToCamelCase_AlreadyCamelCase()
	{
		var result = "alreadyCamelCase".ToCamelCase();
		PrintVariable(result);
		Assert.Equal("alreadyCamelCase", result);
	}
	#endregion

	#region ToPascalCase
	[Fact]
	public void ToPascalCase_FromTitleCase()
	{
		var result = "hello world".ToPascalCase();
		PrintVariable(result);
		Assert.Equal("HelloWorld", result);
	}

	[Fact]
	public void ToPascalCase_FromCamelCase()
	{
		var result = "myVariableName".ToPascalCase();
		PrintVariable(result);
		Assert.Equal("MyVariableName", result);
	}

	[Fact]
	public void ToPascalCase_FromSnakeCase()
	{
		var result = "MY_CONSTANT_NAME".ToPascalCase();
		PrintVariable(result);
		Assert.Equal("MyConstantName", result);
	}

	[Fact]
	public void ToPascalCase_FromKebabCase()
	{
		var result = "kebab-case-string".ToPascalCase();
		PrintVariable(result);
		Assert.Equal("KebabCaseString", result);
	}

	[Fact]
	public void ToPascalCase_WithAcronym()
	{
		var result = "XMLParser".ToPascalCase();
		PrintVariable(result);
		Assert.Equal("XmlParser", result);
	}

	[Fact]
	public void ToPascalCase_AlreadyPascalCase()
	{
		var result = "AlreadyPascalCase".ToPascalCase();
		PrintVariable(result);
		Assert.Equal("AlreadyPascalCase", result);
	}
	#endregion

	#region ToSnakeCase
	[Fact]
	public void ToSnakeCase_FromTitleCase()
	{
		var result = "Hello World".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("hello_world", result);
	}

	[Fact]
	public void ToSnakeCase_FromCamelCase()
	{
		var result = "myVariableName".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("my_variable_name", result);
	}

	[Fact]
	public void ToSnakeCase_FromPascalCase()
	{
		var result = "MyVariableName".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("my_variable_name", result);
	}

	[Fact]
	public void ToSnakeCase_FromKebabCase()
	{
		var result = "kebab-case-string".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("kebab_case_string", result);
	}

	[Fact]
	public void ToSnakeCase_WithAcronym()
	{
		var result = "XMLParser".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("xml_parser", result);
	}

	[Fact]
	public void ToSnakeCase_AlreadySnakeCase()
	{
		var result = "already_snake_case".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("already_snake_case", result);
	}

	[Fact]
	public void ToSnakeCase_FromUpperCase()
	{
		var result = "MY_CONSTANT_NAME".ToSnakeCaseLower();
		PrintVariable(result);
		Assert.Equal("my_constant_name", result);
	}
	#endregion

	#region ToKebabCase
	[Fact]
	public void ToKebabCase_FromTitleCase()
	{
		var result = "Hello World".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("hello-world", result);
	}

	[Fact]
	public void ToKebabCase_FromCamelCase()
	{
		var result = "myVariableName".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("my-variable-name", result);
	}

	[Fact]
	public void ToKebabCase_FromPascalCase()
	{
		var result = "MyVariableName".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("my-variable-name", result);
	}

	[Fact]
	public void ToKebabCase_FromSnakeCase()
	{
		var result = "snake_case_string".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("snake-case-string", result);
	}

	[Fact]
	public void ToKebabCase_WithAcronym()
	{
		var result = "XMLParser".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("xml-parser", result);
	}

	[Fact]
	public void ToKebabCase_AlreadyKebabCase()
	{
		var result = "already-kebab-case".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("already-kebab-case", result);
	}

	[Fact]
	public void ToKebabCase_FromUpperCase()
	{
		var result = "MY_CONSTANT_NAME".ToKebabCaseLower();
		PrintVariable(result);
		Assert.Equal("my-constant-name", result);
	}
	#endregion

	#region AllIndexesOf
	[Fact]
	public void AllIndexesOf_FindsMultipleOccurrences()
	{
		var text = "hello world, hello universe";
		var response = text.AllIndexesOf("hello");
		
		PrintVariable(response.IsSuccess);
		IsTrue(response.IsSuccess);
		
		Assert.NotNull(response.Payload);
		var indexes = response.Payload;
		PrintVariable(indexes);
		Assert.Equal(2, indexes.Length);
		Assert.Equal(0, indexes[0]);
		Assert.Equal(13, indexes[1]);
	}

	[Fact]
	public void AllIndexesOf_CaseInsensitive()
	{
		var text = "Hello HELLO hello";
		var response = text.AllIndexesOf("hello", StringComparison.OrdinalIgnoreCase);
		
		PrintVariable(response.IsSuccess);
		IsTrue(response.IsSuccess);
		
		Assert.NotNull(response.Payload);
		var indexes = response.Payload;
		PrintVariable(indexes);
		Assert.Equal(3, indexes.Length);
		Assert.Equal(0, indexes[0]);
		Assert.Equal(6, indexes[1]);
		Assert.Equal(12, indexes[2]);
	}

	[Fact]
	public void AllIndexesOf_NoMatches_ReturnsEmptyArray()
	{
		var text = "hello world";
		var response = text.AllIndexesOf("xyz");
		
		PrintVariable(response.IsSuccess);
		IsTrue(response.IsSuccess);
		
		Assert.NotNull(response.Payload);
		var indexes = response.Payload;
		PrintVariable(indexes);
		Assert.Empty(indexes);
	}

	[Fact]
	public void AllIndexesOf_EmptySearchValue_ReturnsError()
	{
		var text = "hello world";
		var response = text.AllIndexesOf("");
		
		PrintVariable(response.IsError);
		IsTrue(response.IsError);
		PrintVariable(response);
	}

	[Fact]
	public void AllIndexesOf_NullSearchValue_ReturnsError()
	{
		var text = "hello world";
		var response = text.AllIndexesOf(null!);
		
		PrintVariable(response.IsError);
		IsTrue(response.IsError);
		PrintVariable(response);
	}

	[Fact]
	public void AllIndexesOf_OverlappingMatches()
	{
		var text = "aaa";
		var response = text.AllIndexesOf("aa");
		
		PrintVariable(response.IsSuccess);
		IsTrue(response.IsSuccess);
		
		Assert.NotNull(response.Payload);
		var indexes = response.Payload;
		PrintVariable(indexes);
		// Should find "aa" at position 0, then skip to position 2 (no overlapping)
		Assert.Single(indexes);
		Assert.Equal(0, indexes[0]);
	}
	#endregion

	#region Cross-format conversion tests
	[Fact]
	public void CaseConversions_RoundTrip_CamelToSnakeToCamel()
	{
		var original = "myVariableName";
		var snake = original.ToSnakeCaseLower();
		PrintVariable(snake);
		Assert.Equal("my_variable_name", snake);
		
		var backToCamel = snake.ToCamelCase();
		PrintVariable(backToCamel);
		Assert.Equal("myVariableName", backToCamel);
	}

	[Fact]
	public void CaseConversions_RoundTrip_PascalToKebabToPascal()
	{
		var original = "MyClassName";
		var kebab = original.ToKebabCaseLower();
		PrintVariable(kebab);
		Assert.Equal("my-class-name", kebab);
		
		var backToPascal = kebab.ToPascalCase();
		PrintVariable(backToPascal);
		Assert.Equal("MyClassName", backToPascal);
	}

	[Fact]
	public void CaseConversions_ComplexAcronym()
	{
		var original = "HTTPSConnection";
		
		var camel = original.ToCamelCase();
		PrintVariable(camel);
		Assert.Equal("httpsConnection", camel);
		
		var snake = original.ToSnakeCaseLower();
		PrintVariable(snake);
		Assert.Equal("https_connection", snake);
		
		var kebab = original.ToKebabCaseLower();
		PrintVariable(kebab);
		Assert.Equal("https-connection", kebab);
	}
	#endregion

	#region IsNullOrEmpty / IsNeitherNullNorEmpty
	[Fact]
	public void IsNullOrEmpty_NullSafety()
	{
		var text = GetNullableString();
		if (!text.IsNullOrEmpty())
		{
			// Compiler should know 'text' is not null here
			var length = text.Length;  // No warning expected
			PrintVariable(length);
			Assert.True(length >= 0);
		}
		
		text = GetNullableString();
		if (text.IsNullOrEmpty())
			return;

		// Compiler should know 'text' is not null here
		var upper = text.ToUpper();  // No warning expected
		PrintVariable(upper);
		Assert.NotNull(upper);
	}

	[Fact]
	public void IsNeitherNullNorEmpty_NullSafety()
	{
		var text = GetNullableString();
		if (text.IsNeitherNullNorEmpty())
		{
			// Compiler should know 'text' is not null here (NotNullWhen(true))
			var trimmed = text.Trim();  // No warning expected
			PrintVariable(trimmed);
			Assert.NotNull(trimmed);
		}

		text = GetNullableString();
		if (!text.IsNeitherNullNorEmpty())
			return;

		// Compiler should know 'text' is not null here
		var upper = text.ToUpper();  // No warning expected
		PrintVariable(upper);
		Assert.NotNull(upper);
	}
	#endregion

	#region IsNullOrWhiteSpace / IsNeitherNullNorWhiteSpace
	[Fact]
	public void IsNullOrWhiteSpace_NullSafety()
	{
		var text = GetNullableString();
		if (!text.IsNullOrWhiteSpace())
		{
			// Compiler should know 'text' is not null here
			var upper = text.ToUpper();  // No warning expected
			PrintVariable(upper);
			Assert.NotNull(upper);
		}

		text = GetNullableString();
		if (text.IsNullOrWhiteSpace())
			return;
		// Compiler should know 'text' is not null here
		var trimmed = text.Trim();  // No warning expected
		PrintVariable(trimmed);
		Assert.NotNull(trimmed);
	}

	[Fact]
	public void IsNeitherNullNorWhiteSpace_NullSafety()
	{
		var text = GetNullableString();
		if (text.IsNeitherNullNorWhiteSpace())
		{
			// Compiler should know 'text' is not null here (NotNullWhen(true))
			var lower = text.ToLower();  // No warning expected
			PrintVariable(lower);
			Assert.NotNull(lower);
		}
		text = GetNullableString();
		if (!text.IsNeitherNullNorWhiteSpace())
			return;
		// Compiler should know 'text' is not null here
		var upper = text.ToUpper();  // No warning expected
		PrintVariable(upper);
		Assert.NotNull(upper);
	}
	#endregion

	// Helper method that returns nullable string
	private static string? GetNullableString() => "test";
}