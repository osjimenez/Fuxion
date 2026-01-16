using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace Fuxion;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Provides extension methods for string manipulation, case conversion, and text processing.
/// </summary>
/// <remarks>
/// This class contains a comprehensive set of string utilities organized into several categories:
/// <list type="bullet">
/// <item><description><strong>Character and line manipulation:</strong> RemoveChar, SplitInLines, EnsureEndsWith</description></item>
/// <item><description><strong>Case conversion:</strong> ToTitleCase, ToCamelCase, ToPascalCase, ToSnakeCase, ToKebabCase</description></item>
/// <item><description><strong>Substring operations:</strong> SubstringFromEnd, AllIndexesOf</description></item>
/// <item><description><strong>Null checking:</strong> IsNullOrEmpty, IsNullOrWhiteSpace, IsNeither* variants</description></item>
/// <item><description><strong>Formatting:</strong> Format with fluent syntax</description></item>
/// <item><description><strong>Search operations:</strong> AllIndexesOf, SearchTextInElements</description></item>
/// </list>
/// </remarks>
public static class StringExtensions
{
	extension(string me)
	{
		/// <summary>
		/// Removes all occurrences of the specified character from the string.
		/// </summary>
		/// <param name="c">The character to remove from the string.</param>
		/// <returns>
		/// A new string with all occurrences of <paramref name="c"/> removed.
		/// If the character is not found, returns a copy of the original string.
		/// </returns>
		/// <example>
		/// <code>
		/// "hello-world".RemoveChar('-')  // returns "helloworld"
		/// "abc123".RemoveChar('x')       // returns "abc123"
		/// </code>
		/// </example>
		public string RemoveChar(char c) => new(me.Where(x => x != c).ToArray());

		/// <summary>
		/// Splits the string into lines using common line break sequences.
		/// Handles Windows (CRLF), Unix (LF), and classic Mac (CR) line endings.
		/// </summary>
		/// <param name="removeEmptyLines">
		/// When <c>true</c>, removes empty lines from the result.
		/// When <c>false</c>, preserves empty lines.
		/// </param>
		/// <param name="trimEachLine">
		/// When <c>true</c>, trims leading and trailing whitespace from each line.
		/// When <c>false</c>, preserves whitespace in each line.
		/// </param>
		/// <returns>
		/// An array of strings, where each element represents a line from the original string.
		/// </returns>
		/// <example>
		/// <code>
		/// var text = "Line 1\r\nLine 2\r\n\r\nLine 4";
		/// var lines1 = text.SplitInLines();                        // ["Line 1", "Line 2", "", "Line 4"]
		/// var lines2 = text.SplitInLines(removeEmptyLines: true);  // ["Line 1", "Line 2", "Line 4"]
		/// 
		/// var textWithSpaces = "  Line 1  \n  Line 2  ";
		/// var lines3 = textWithSpaces.SplitInLines(trimEachLine: true);  // ["Line 1", "Line 2"]
		/// </code>
		/// </example>
		public string[] SplitInLines(bool removeEmptyLines = false, bool trimEachLine = false)
		{
#if !STANDARD_OR_OLD_FRAMEWORKS
			return me.Split(["\r\n", "\r", "\n"],
				(removeEmptyLines
					? StringSplitOptions.RemoveEmptyEntries
					: StringSplitOptions.None)
				| (trimEachLine
					? StringSplitOptions.TrimEntries
					: StringSplitOptions.None));
#else
			var res = me.Split(["\r\n", "\r", "\n"],
				removeEmptyLines
					? StringSplitOptions.RemoveEmptyEntries
					: StringSplitOptions.None);
			if (trimEachLine)
				res = res.Select(l => l.Trim()).ToArray();
			return res;
#endif
		}

		/// <summary>
		/// Ensures the string ends with the specified suffix by appending only the necessary characters.
		/// If the string already ends with the suffix (or part of it), only appends what's missing.
		/// </summary>
		/// <param name="ending">The suffix to ensure at the end of the string.</param>
		/// <returns>
		/// The original string if <paramref name="ending"/> is <c>null</c> or empty;
		/// otherwise, the string with the minimal concatenation needed to satisfy <c>EndsWith(ending)</c>.
		/// </returns>
		/// <remarks>
		/// This method is more efficient than simply appending the suffix, as it checks for partial matches
		/// at the end of the string. For example, if the string is "hel" and the ending is "llo", 
		/// it only appends "lo" to create "hello" instead of "helllo".
		/// </remarks>
		/// <example>
		/// <code>
		/// "hel".EnsureEndsWith("llo")          // returns "hello" (appends "lo")
		/// "hell".EnsureEndsWith("llo")         // returns "hello" (appends "o")
		/// "hello".EnsureEndsWith("llo")        // returns "hello" (nothing to append)
		/// "hello world".EnsureEndsWith("!")    // returns "hello world!"
		/// "path/to/dir".EnsureEndsWith("/")    // returns "path/to/dir/"
		/// "path/to/dir/".EnsureEndsWith("/")   // returns "path/to/dir/" (already ends with /)
		/// </code>
		/// </example>
		public string EnsureEndsWith(string ending)
		{
			if (string.IsNullOrEmpty(ending)) return me;

			for (var i = 0; i <= ending.Length; i++)
			{
				var tmp = me + ending.SubstringFromEnd(i);
				if (tmp.EndsWith(ending)) return tmp;
			}
			return me;
		}

		/// <summary>
		/// Returns a substring containing the last <paramref name="length"/> characters from the string.
		/// If <paramref name="length"/> is greater than or equal to the string's length, returns the entire string.
		/// </summary>
		/// <param name="length">The number of characters to extract from the end of the string.</param>
		/// <returns>
		/// A substring containing the last <paramref name="length"/> characters.
		/// If <paramref name="length"/> is greater than or equal to the string's length, returns the entire string.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="length"/> is negative.
		/// </exception>
		/// <remarks>
		/// This method is similar to <see cref="string.Substring(int, int)"/> but extracts characters from the end
		/// rather than from the beginning. It's particularly useful for getting file extensions, suffixes, or
		/// the last N characters of identifiers.
		/// </remarks>
		/// <example>
		/// <code>
		/// "Hello World".SubstringFromEnd(5)    // returns "World"
		/// "Hello".SubstringFromEnd(10)         // returns "Hello" (length >= string length)
		/// "abc123".SubstringFromEnd(3)         // returns "123"
		/// "file.txt".SubstringFromEnd(4)       // returns ".txt"
		/// 
		/// // Throws ArgumentOutOfRangeException
		/// "test".SubstringFromEnd(-1)
		/// </code>
		/// </example>
		public string SubstringFromEnd(int length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, "Length cannot be negative.");

			return length < me.Length ? me.Substring(me.Length - length) : me;
		}

		/// <summary>
		/// Splits a string into individual words from various naming conventions.
		/// Handles camelCase, PascalCase, snake_case, kebab-case, spaces, and acronyms.
		/// </summary>
		/// <param name="input">The string to split into words.</param>
		/// <returns>
		/// An enumerable sequence of words extracted from the input string.
		/// Returns empty if the input is null or empty.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This is a helper method used internally by case conversion methods (ToTitleCase, ToCamelCase, etc.).
		/// It intelligently handles various naming conventions and edge cases:
		/// </para>
		/// <list type="bullet">
		/// <item><description><strong>Separators:</strong> Splits on space, hyphen (-), and underscore (_)</description></item>
		/// <item><description><strong>camelCase/PascalCase:</strong> Splits when transitioning from lowercase to uppercase</description></item>
		/// <item><description><strong>Acronyms:</strong> Handles sequences like "XMLParser" → ["XML", "Parser"]</description></item>
		/// <item><description><strong>Mixed cases:</strong> "HTTPSConnection" → ["HTTPS", "Connection"]</description></item>
		/// </list>
		/// <para>
		/// <strong>Acronym detection logic:</strong> When encountering consecutive uppercase letters followed by
		/// a lowercase letter, the last uppercase letter is considered the start of a new word.
		/// For example: "XMLHttp" splits before 'H' → ["XML", "Http"].
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Internal usage examples (not directly accessible)
		/// SplitIntoWords("myVariableName")     // yields: ["my", "Variable", "Name"]
		/// SplitIntoWords("MY_CONSTANT_NAME")   // yields: ["MY", "CONSTANT", "NAME"]
		/// SplitIntoWords("kebab-case-string")  // yields: ["kebab", "case", "string"]
		/// SplitIntoWords("XMLParser")          // yields: ["XML", "Parser"]
		/// SplitIntoWords("HTTPSConnection")    // yields: ["HTTPS", "Connection"]
		/// SplitIntoWords("IOError")            // yields: ["IO", "Error"]
		/// SplitIntoWords("Hello World")        // yields: ["Hello", "World"]
		/// </code>
		/// </example>
		private static IEnumerable<string> SplitIntoWords(string input)
		{
			if (string.IsNullOrEmpty(input)) yield break;

			var currentWord = new StringBuilder();
			var previousWasUpper = false;
			var previousWasLower = false;

			for (var i = 0; i < input.Length; i++)
			{
				var c = input[i];

				// Separators: space, hyphen, underscore
				if (c == ' ' || c == '-' || c == '_')
				{
					if (currentWord.Length > 0)
					{
						yield return currentWord.ToString();
						currentWord.Clear();
						previousWasUpper = false;
						previousWasLower = false;
					}
					continue;
				}

				var isUpper = char.IsUpper(c);
				var isLower = char.IsLower(c);

				// Detect word boundaries in camelCase/PascalCase
				if (isUpper && currentWord.Length > 0)
				{
					// Handle acronyms: "XMLParser" -> ["XML", "Parser"]
					// "HTTPSConnection" -> ["HTTPS", "Connection"]
					// If previous was uppercase and next is lowercase, current char starts a new word
					if (previousWasUpper && i + 1 < input.Length && char.IsLower(input[i + 1]))
					{
						// Current uppercase char starts a new word
						// Yield what we have accumulated so far
						yield return currentWord.ToString();
						currentWord.Clear();
					}
					else if (previousWasLower)
					{
						// Normal camelCase boundary: "myVariable" -> ["my", "Variable"]
						yield return currentWord.ToString();
						currentWord.Clear();
					}
				}

				currentWord.Append(c);
				previousWasUpper = isUpper;
				previousWasLower = isLower;
			}

			if (currentWord.Length > 0)
				yield return currentWord.ToString();
		}

		/// <summary>
		/// Converts the string to Title Case, where the first letter of each word is capitalized.
		/// Automatically detects and handles various naming conventions (camelCase, PascalCase, snake_case, kebab-case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in Title Case format with spaces between words.
		/// </returns>
		/// <example>
		/// <code>
		/// "hello world".ToTitleCase()           // returns "Hello World"
		/// "myVariableName".ToTitleCase()        // returns "My Variable Name"
		/// "MY_CONSTANT_NAME".ToTitleCase()      // returns "My Constant Name"
		/// "kebab-case-string".ToTitleCase()     // returns "Kebab Case String"
		/// "XMLParser".ToTitleCase()             // returns "Xml Parser"
		/// </code>
		/// </example>
		public string ToTitleCase(CultureInfo? culture = null) =>
			string.Join(" ", SplitIntoWords(me).Select(w =>
				char.ToUpper(w[0], culture ?? CultureInfo.CurrentCulture) + w[1..].ToLower(culture ?? CultureInfo.CurrentCulture)));

		/// <summary>
		/// Converts the string to camelCase, where the first word is lowercase and subsequent words
		/// start with an uppercase letter, with no separators between words.
		/// Automatically detects and handles various naming conventions (PascalCase, snake_case, kebab-case, Title Case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in camelCase format.
		/// </returns>
		/// <example>
		/// <code>
		/// "Hello World".ToCamelCase()           // returns "helloWorld"
		/// "MyVariableName".ToCamelCase()        // returns "myVariableName"
		/// "MY_CONSTANT_NAME".ToCamelCase()      // returns "myConstantName"
		/// "kebab-case-string".ToCamelCase()     // returns "kebabCaseString"
		/// "XMLParser".ToCamelCase()             // returns "xmlParser"
		/// "already_camelCase".ToCamelCase()     // returns "alreadyCamelCase"
		/// </code>
		/// </example>
		public string ToCamelCase(CultureInfo? culture = null)
		{
			var words = SplitIntoWords(me).ToArray();
			if (words.Length == 0) return me;

			culture ??= CultureInfo.CurrentCulture;

			var first = words[0].ToLower(culture);
			var rest = words.Skip(1).Select(w =>
				char.ToUpper(w[0], culture) + w[1..].ToLower(culture));

			return first + string.Concat(rest);
		}

		/// <summary>
		/// Converts the string to PascalCase, where each word starts with an uppercase letter
		/// and there are no separators between words.
		/// Automatically detects and handles various naming conventions (camelCase, snake_case, kebab-case, Title Case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in PascalCase format.
		/// </returns>
		/// <example>
		/// <code>
		/// "hello world".ToPascalCase()           // returns "HelloWorld"
		/// "myVariableName".ToPascalCase()        // returns "MyVariableName"
		/// "MY_CONSTANT_NAME".ToPascalCase()      // returns "MyConstantName"
		/// "kebab-case-string".ToPascalCase()     // returns "KebabCaseString"
		/// "XMLParser".ToPascalCase()             // returns "XmlParser"
		/// "alreadyPascalCase".ToPascalCase()     // returns "AlreadyPascalCase"
		/// </code>
		/// </example>
		public string ToPascalCase(CultureInfo? culture = null) =>
			string.Concat(SplitIntoWords(me).Select(w =>
				char.ToUpper(w[0], culture ?? CultureInfo.CurrentCulture) + w[1..].ToLower(culture ?? CultureInfo.CurrentCulture)));

		/// <summary>
		/// Converts the string to lowercase snake_case, where words are lowercase and separated by underscores.
		/// Automatically detects and handles various naming conventions (camelCase, PascalCase, kebab-case, Title Case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in lowercase snake_case format.
		/// </returns>
		/// <remarks>
		/// This method was renamed from <c>ToSnakeCase</c> to <c>ToSnakeCaseLower</c> to distinguish it from
		/// the uppercase variant <see cref="ToSnakeCaseUpper"/>. Both methods are provided to support different
		/// naming conventions (e.g., variable names vs. constant names).
		/// </remarks>
		/// <example>
		/// <code>
		/// "Hello World".ToSnakeCaseLower()            // returns "hello_world"
		/// "MyVariableName".ToSnakeCaseLower()         // returns "my_variable_name"
		/// "MY_CONSTANT_NAME".ToSnakeCaseLower()       // returns "my_constant_name"
		/// "kebab-case-string".ToSnakeCaseLower()      // returns "kebab_case_string"
		/// "XMLParser".ToSnakeCaseLower()              // returns "xml_parser"
		/// "already_snake_case".ToSnakeCaseLower()     // returns "already_snake_case"
		/// </code>
		/// </example>
		public string ToSnakeCaseLower(CultureInfo? culture = null)
			=> string.Join("_", SplitIntoWords(me).Select(w => w.ToLower(culture ?? CultureInfo.CurrentCulture)));

		/// <summary>
		/// Converts the string to uppercase SNAKE_CASE, where words are uppercase and separated by underscores.
		/// Automatically detects and handles various naming conventions (camelCase, PascalCase, kebab-case, Title Case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in uppercase SNAKE_CASE format, commonly used for constants.
		/// </returns>
		/// <remarks>
		/// This format is typically used for constant names in many programming languages (e.g., C, C++, Java, C#).
		/// Use <see cref="ToSnakeCaseLower"/> for lowercase snake_case (commonly used for variable names in Python, Ruby).
		/// </remarks>
		/// <example>
		/// <code>
		/// "Hello World".ToSnakeCaseUpper()            // returns "HELLO_WORLD"
		/// "MyVariableName".ToSnakeCaseUpper()         // returns "MY_VARIABLE_NAME"
		/// "myConstantName".ToSnakeCaseUpper()         // returns "MY_CONSTANT_NAME"
		/// "kebab-case-string".ToSnakeCaseUpper()      // returns "KEBAB_CASE_STRING"
		/// "XMLParser".ToSnakeCaseUpper()              // returns "XML_PARSER"
		/// "already_snake_case".ToSnakeCaseUpper()     // returns "ALREADY_SNAKE_CASE"
		/// 
		/// // Common use case: constant naming
		/// const string API_KEY = "my-api-key".ToSnakeCaseUpper();  // "MY_API_KEY"
		/// </code>
		/// </example>
		public string ToSnakeCaseUpper(CultureInfo? culture = null)
			=> string.Join("_", SplitIntoWords(me).Select(w => w.ToUpper(culture ?? CultureInfo.CurrentCulture)));

		/// <summary>
		/// Converts the string to lowercase kebab-case, where words are lowercase and separated by hyphens.
		/// Automatically detects and handles various naming conventions (camelCase, PascalCase, snake_case, Title Case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in lowercase kebab-case format, commonly used in URLs, CSS classes, and HTML attributes.
		/// </returns>
		/// <remarks>
		/// This method was renamed from <c>ToKebabCase</c> to <c>ToKebabCaseLower</c> to distinguish it from
		/// the uppercase variant <see cref="ToKebabCaseUpper"/>. Lowercase kebab-case is the standard format
		/// for URLs, CSS class names, and HTML IDs.
		/// </remarks>
		/// <example>
		/// <code>
		/// "Hello World".ToKebabCaseLower()            // returns "hello-world"
		/// "MyVariableName".ToKebabCaseLower()         // returns "my-variable-name"
		/// "MY_CONSTANT_NAME".ToKebabCaseLower()       // returns "my-constant-name"
		/// "snake_case_string".ToKebabCaseLower()      // returns "snake-case-string"
		/// "XMLParser".ToKebabCaseLower()              // returns "xml-parser"
		/// "already-kebab-case".ToKebabCaseLower()     // returns "already-kebab-case"
		/// 
		/// // Common use cases
		/// var cssClass = "MyButton".ToKebabCaseLower();      // "my-button"
		/// var url = "UserProfile".ToKebabCaseLower();        // "user-profile"
		/// var htmlId = "MainContainer".ToKebabCaseLower();   // "main-container"
		/// </code>
		/// </example>
		public string ToKebabCaseLower(CultureInfo? culture = null)
			=> string.Join("-", SplitIntoWords(me).Select(w => w.ToLower(culture ?? CultureInfo.CurrentCulture)));

		/// <summary>
		/// Converts the string to uppercase KEBAB-CASE, where words are uppercase and separated by hyphens.
		/// Automatically detects and handles various naming conventions (camelCase, PascalCase, snake_case, Title Case).
		/// </summary>
		/// <param name="culture">
		/// The culture to use for casing. When <c>null</c>, uses <see cref="CultureInfo.CurrentCulture"/>.
		/// </param>
		/// <returns>
		/// A string in uppercase KEBAB-CASE format.
		/// </returns>
		/// <remarks>
		/// This format is less common than lowercase kebab-case but may be used for emphasis in headers,
		/// titles, or specific styling requirements. For standard kebab-case (lowercase), use
		/// <see cref="ToKebabCaseLower"/>.
		/// </remarks>
		/// <example>
		/// <code>
		/// "Hello World".ToKebabCaseUpper()            // returns "HELLO-WORLD"
		/// "MyVariableName".ToKebabCaseUpper()         // returns "MY-VARIABLE-NAME"
		/// "myConstantName".ToKebabCaseUpper()         // returns "MY-CONSTANT-NAME"
		/// "snake_case_string".ToKebabCaseUpper()      // returns "SNAKE-CASE-STRING"
		/// "XMLParser".ToKebabCaseUpper()              // returns "XML-PARSER"
		/// "already-kebab-case".ToKebabCaseUpper()     // returns "ALREADY-KEBAB-CASE"
		/// 
		/// // Use case: emphasized headers or titles
		/// var header = "Important Notice".ToKebabCaseUpper();  // "IMPORTANT-NOTICE"
		/// </code>
		/// </example>
		public string ToKebabCaseUpper(CultureInfo? culture = null)
			=> string.Join("-", SplitIntoWords(me).Select(w => w.ToUpper(culture ?? CultureInfo.CurrentCulture)));

		/// <summary>
		/// Finds all starting indexes of occurrences of a specified substring within the string.
		/// </summary>
		/// <param name="value">The substring to search for.</param>
		/// <param name="comparisonType">
		/// The string comparison rules to use. Defaults to <see cref="StringComparison.Ordinal"/>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload contains an array of all starting indexes where <paramref name="value"/> occurs,
		/// or an error response when <paramref name="value"/> is <c>null</c> or empty.
		/// If no occurrences are found, returns an empty array.
		/// </returns>
		/// <example>
		/// <code>
		/// var text = "hello world, hello universe";
		/// var response = text.AllIndexesOf("hello");
		/// if (response.IsSuccess)
		/// {
		///     var indexes = response.Payload;  // [0, 13]
		/// }
		/// 
		/// // Case-insensitive search
		/// var text2 = "Hello HELLO hello";
		/// var response2 = text2.AllIndexesOf("hello", StringComparison.OrdinalIgnoreCase);
		/// // Returns: [0, 6, 12]
		/// 
		/// // No matches
		/// var response3 = "test".AllIndexesOf("xyz");  // Returns: []
		/// 
		/// // Error case
		/// var response4 = "test".AllIndexesOf("");  // Returns error response
		/// </code>
		/// </example>
		public Response<int[]> AllIndexesOf(string value, StringComparison comparisonType = StringComparison.Ordinal)
		{
			if (string.IsNullOrEmpty(value))
				return Response.Get.InvalidData("Source string is null or empty.").AsPayload<int[]>();

			var indexes = new List<int>();
			for (var index = 0; ; index += value.Length)
			{
				index = me.IndexOf(value, index, comparisonType);
				if (index == -1) break;
				indexes.Add(index);
			}

			return indexes.ToArray();
		}

		/// <summary>
		/// Formats the string using the specified parameters.
		/// This is a convenience wrapper around <see cref="string.Format(string, object[])"/>.
		/// </summary>
		/// <param name="params">
		/// An array of objects to format into the string. The string should contain format placeholders
		/// like <c>{0}</c>, <c>{1}</c>, etc., which will be replaced by the corresponding parameter values.
		/// </param>
		/// <returns>
		/// A formatted string with all placeholders replaced by their corresponding parameter values.
		/// </returns>
		/// <exception cref="FormatException">
		/// Thrown when the format string is invalid or when there's a mismatch between format items and parameters.
		/// </exception>
		/// <remarks>
		/// This extension method provides a more fluent syntax for string formatting by allowing
		/// the format string to be the subject of the operation rather than a parameter.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Traditional approach
		/// var message1 = string.Format("Hello {0}, you have {1} messages", "John", 5);
		/// 
		/// // Extension method approach (more fluent)
		/// var message2 = "Hello {0}, you have {1} messages".Format("John", 5);
		/// // Returns: "Hello John, you have 5 messages"
		/// 
		/// // With multiple parameters
		/// var path = "C:\\{0}\\{1}\\{2}.txt".Format("Users", "Documents", "file");
		/// // Returns: "C:\Users\Documents\file.txt"
		/// 
		/// // With format specifiers
		/// var price = "Total: {0:C}".Format(123.45);
		/// // Returns: "Total: $123.45" (depends on current culture)
		/// 
		/// var date = "Today is {0:yyyy-MM-dd}".Format(DateTime.Now);
		/// // Returns: "Today is 2024-01-15"
		/// </code>
		/// </example>
		public string Format(params object?[] @params) => string.Format(me, @params);
	}

	extension([NotNullWhen(false)] string? me)
	{
		/// <summary>
		/// Determines whether the string is <c>null</c> or empty.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the string is <c>null</c> or an empty string (""); otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method is a convenience wrapper around <see cref="string.IsNullOrEmpty(string)"/>.
		/// The <c>NotNullWhen</c> attribute ensures that when this method returns <c>false</c>,
		/// the compiler knows the string is not <c>null</c>.
		/// </remarks>
		/// <example>
		/// <code>
		/// string? text = null;
		/// text.IsNullOrEmpty()     // returns true
		/// 
		/// text = "";
		/// text.IsNullOrEmpty()     // returns true
		/// 
		/// text = "hello";
		/// text.IsNullOrEmpty()     // returns false
		/// 
		/// // Null-safety with pattern matching
		/// if (!text.IsNullOrEmpty())
		/// {
		///     // Compiler knows 'text' is not null here
		///     Console.WriteLine(text.ToUpper());
		/// }
		/// </code>
		/// </example>
		public bool IsNullOrEmpty() => string.IsNullOrEmpty(me);

		/// <summary>
		/// Determines whether the string is <c>null</c>, empty, or consists only of white-space characters.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the string is <c>null</c>, empty, or contains only white-space characters; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method is a convenience wrapper around <see cref="string.IsNullOrWhiteSpace(string)"/>.
		/// The <c>NotNullWhen</c> attribute ensures that when this method returns <c>false</c>,
		/// the compiler knows the string is not <c>null</c>.
		/// White-space characters include space, tab, newline, carriage return, and other Unicode whitespace characters.
		/// </remarks>
		/// <example>
		/// <code>
		/// string? text = null;
		/// text.IsNullOrWhiteSpace()        // returns true
		/// 
		/// text = "";
		/// text.IsNullOrWhiteSpace()        // returns true
		/// 
		/// text = "   ";
		/// text.IsNullOrWhiteSpace()        // returns true
		/// 
		/// text = "hello";
		/// text.IsNullOrWhiteSpace()        // returns false
		/// 
		/// // Null-safety with pattern matching
		/// if (!text.IsNullOrWhiteSpace())
		/// {
		///     // Compiler knows 'text' is not null here
		///     Console.WriteLine(text.Trim());
		/// }
		/// </code>
		/// </example>
		public bool IsNullOrWhiteSpace() => string.IsNullOrWhiteSpace(me);
	}
	extension([NotNullWhen(true)] string? me)
	{
		/// <summary>
		/// Determines whether the string is neither <c>null</c> nor empty.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the string is not <c>null</c> and not empty; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method is the logical inverse of <see cref="string.IsNullOrEmpty(string)"/>.
		/// The <c>NotNullWhen</c> attribute ensures that when this method returns <c>true</c>,
		/// the compiler knows the string is not <c>null</c>.
		/// </remarks>
		/// <example>
		/// <code>
		/// string? text = null;
		/// text.IsNeitherNullNorEmpty()     // returns false
		/// 
		/// text = "";
		/// text.IsNeitherNullNorEmpty()     // returns false
		/// 
		/// text = "hello";
		/// text.IsNeitherNullNorEmpty()     // returns true
		/// 
		/// // Null-safety with pattern matching
		/// if (text.IsNeitherNullNorEmpty())
		/// {
		///     // Compiler knows 'text' is not null here
		///     Console.WriteLine(text.Length);
		/// }
		/// </code>
		/// </example>
		public bool IsNeitherNullNorEmpty() => !string.IsNullOrEmpty(me);

		/// <summary>
		/// Determines whether the string is neither <c>null</c>, empty, nor consists only of white-space characters.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the string is not <c>null</c>, not empty, and contains at least one non-white-space character; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method is the logical inverse of <see cref="string.IsNullOrWhiteSpace(string)"/>.
		/// The <c>NotNullWhen</c> attribute ensures that when this method returns <c>true</c>,
		/// the compiler knows the string is not <c>null</c>
		/// </remarks>
		/// <example>
		/// <code>
		/// string? text = null;
		/// text.IsNeitherNullNorWhiteSpace()        // returns false
		/// 
		/// text = "";
		/// text.IsNeitherNullNorWhiteSpace()        // returns false
		/// 
		/// text = "   ";
		/// text.IsNeitherNullNorWhiteSpace()        // returns false
		/// 
		/// text = "hello";
		/// text.IsNeitherNullNorWhiteSpace()        // returns true
		/// 
		/// // Null-safety with pattern matching
		/// if (text.IsNeitherNullNorWhiteSpace())
		/// {
		///     // Compiler knows 'text' is not null here
		///     var trimmed = text.Trim();
		///     Console.WriteLine(trimmed);
		/// }
		/// </code>
		/// </example>
		public bool IsNeitherNullNorWhiteSpace() => !string.IsNullOrWhiteSpace(me);
	}

	/// <summary>
	/// Extension methods for string arrays to perform advanced text search operations.
	/// </summary>
	extension(string[] me)
	{
		/// <summary>
		/// Searches for all occurrences of a text pattern within an array of strings and returns their positions.
		/// The search treats the array as a continuous text stream, handling matches that span multiple array elements.
		/// </summary>
		/// <param name="text">The text pattern to search for in the array elements.</param>
		/// <param name="comparisonType">
		/// The string comparison rules to use. Affects case sensitivity and culture-specific comparison.
		/// </param>
		/// <returns>
		/// A list of tuples where each tuple contains:
		/// <list type="bullet">
		/// <item><description><strong>Start:</strong> (ItemIndex, PositionIndex) - The array index and character position where the match begins</description></item>
		/// <item><description><strong>End:</strong> (ItemIndex, PositionIndex) - The array index and character position where the match ends</description></item>
		/// </list>
		/// Returns an empty list if no matches are found or if the search text is null/empty.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method is particularly useful for searching text that has been split into logical segments
		/// (like lines, paragraphs, or tokens) but where matches may cross segment boundaries.
		/// </para>
		/// <para>
		/// <strong>Algorithm:</strong>
		/// </para>
		/// <list type="number">
		/// <item><description>Concatenates all array elements into a single string</description></item>
		/// <item><description>Finds all occurrences of the search text using <see cref="AllIndexesOf"/></description></item>
		/// <item><description>Maps each occurrence back to the original array structure, identifying start and end positions</description></item>
		/// <item><description>Handles matches that span multiple array elements</description></item>
		/// </list>
		/// <para>
		/// <strong>Position indexing:</strong> PositionIndex is 0-based and refers to the character position
		/// within the specific array element (ItemIndex).
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var lines = new[]
		/// {
		///     "Hello wor",
		///     "ld! This is",
		///     " a test."
		/// };
		/// 
		/// // Search for "world" which spans elements 0 and 1
		/// var results = lines.SearchTextInElements("world", StringComparison.Ordinal);
		/// // Results:
		/// // [
		/// //   Start: (ItemIndex: 0, PositionIndex: 6),  // "wor" starts at index 6 in element 0
		/// //   End:   (ItemIndex: 1, PositionIndex: 1)   // "ld" ends at index 1 in element 1
		/// // ]
		/// 
		/// // Search within single element
		/// var results2 = lines.SearchTextInElements("test", StringComparison.Ordinal);
		/// // Results:
		/// // [
		/// //   Start: (ItemIndex: 2, PositionIndex: 3),  // starts at index 3 in element 2
		/// //   End:   (ItemIndex: 2, PositionIndex: 6)   // ends at index 6 in element 2
		/// // ]
		/// 
		/// // Case-insensitive search
		/// var results3 = lines.SearchTextInElements("HELLO", StringComparison.OrdinalIgnoreCase);
		/// // Results:
		/// // [
		/// //   Start: (ItemIndex: 0, PositionIndex: 0),
		/// //   End:   (ItemIndex: 0, PositionIndex: 4)
		/// // ]
		/// 
		/// // No matches
		/// var results4 = lines.SearchTextInElements("xyz", StringComparison.Ordinal);
		/// // Returns: empty list []
		/// 
		/// // Practical use case: highlighting search results in a multi-line text editor
		/// foreach (var match in results)
		/// {
		///     Console.WriteLine($"Found from element {match.Start.ItemIndex} " +
		///                       $"position {match.Start.PositionIndex} " +
		///                       $"to element {match.End.ItemIndex} " +
		///                       $"position {match.End.PositionIndex}");
		/// }
		/// </code>
		/// </example>
		public List<((int ItemIndex, int PositionIndex) Start, (int ItemIndex, int PositionIndex) End)> SearchTextInElements(string text, StringComparison comparisonType)
		{
			// Concateno el texto de todos los elementos
			var allText = me.Aggregate("", (a, c) => a + c);
			// Busco todas las apariciones del texto buscado
			var indexesResponse = allText.AllIndexesOf(text, comparisonType);
			if (indexesResponse.IsError)
				return [];

			var indexes = indexesResponse.Payload;
			List<((int ItemIndex, int PositionIndex) Start, (int ItemIndex, int PositionIndex) End)> res = [];
			foreach (var index in indexes)
			{
				var counter = 0;
				var startItemIndex = 0;
				var startIndexInItem = 0;
				for (; startItemIndex < me.Length; startItemIndex++)
				{
					counter += me[startItemIndex].Length;
					if (counter > index)
					{
						startIndexInItem = me[startItemIndex].Length - (counter - index);
						break;
					}
				}
				var endItemIndex = startItemIndex;
				var endIndexInItem = 0;
				for (; endItemIndex < me.Length; endItemIndex++)
				{
					if (endItemIndex != startItemIndex) counter += me[endItemIndex].Length;
					if (counter >= index + text.Length)
					{
						endIndexInItem = me[endItemIndex].Length - 1 - (counter - (index + text.Length));
						break;
					}
				}
				res.Add(((startItemIndex, startIndexInItem), (endItemIndex, endIndexInItem)));
			}
			return res;
		}
	}
}
