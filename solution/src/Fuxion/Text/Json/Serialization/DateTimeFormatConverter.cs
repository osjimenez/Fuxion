using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Fuxion.Text.Json.Serialization;

/// <summary>
///    JSON converter for <see cref="DateTime" /> that supports custom read and write formats with culture-specific
///    formatting.
/// </summary>
/// <remarks>
///    <para>
///       This converter allows precise control over how <see cref="DateTime" /> values are serialized and deserialized in
///       JSON,
///       supporting multiple input formats for parsing and a single output format for writing.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Multiple read formats for flexible parsing (e.g., accept both "dd/MM/yyyy" and "yyyy-MM-dd")</description>
///       </item>
///       <item>
///          <description>Single write format for consistent output</description>
///       </item>
///       <item>
///          <description>Culture-specific formatting (or invariant culture)</description>
///       </item>
///       <item>
///          <description>Configurable <see cref="DateTimeStyles" /> for parsing behavior</description>
///       </item>
///    </list>
///    <para>
///       <strong>Default parsing behavior:</strong> Uses <see cref="DateTimeStyles.AssumeLocal" /> |
///       <see cref="DateTimeStyles.AllowWhiteSpaces" />,
///       which treats dates without timezone information as local time and allows whitespace in the input.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// // Register converter globally
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new DateTimeFormatJsonConverter(
///     readFormats: new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" },
///     writeFormat: "yyyy-MM-dd"
/// ));
/// 
/// // Serialization
/// var date = new DateTime(2024, 1, 15);
/// var json = JsonSerializer.Serialize(date, options);
/// // Result: "2024-01-15"
/// 
/// // Deserialization (accepts multiple formats)
/// var date1 = JsonSerializer.Deserialize&lt;DateTime&gt;("\"15/01/2024\"", options);  // dd/MM/yyyy
/// var date2 = JsonSerializer.Deserialize&lt;DateTime&gt;("\"2024-01-15\"", options);  // yyyy-MM-dd
/// var date3 = JsonSerializer.Deserialize&lt;DateTime&gt;("\"15-01-2024\"", options);  // dd-MM-yyyy
/// // All produce: DateTime(2024, 1, 15)
/// 
/// // With specific culture (French)
/// var frenchConverter = new DateTimeFormatJsonConverter(
///     readFormats: new[] { "dd/MM/yyyy" },
///     writeFormat: "dd/MM/yyyy",
///     culture: new CultureInfo("fr-FR")
/// );
/// 
/// // Using with attribute (see DateTimeFormatJsonConverterAttribute)
/// public class Event
/// {
///     [DateTimeFormatJsonConverter(new[] { "dd/MM/yyyy", "yyyy-MM-dd" }, "yyyy-MM-dd")]
///     public DateTime EventDate { get; set; }
/// }
/// </code>
/// </example>
/// <seealso cref="NullableDateTimeFormatJsonConverter" />
/// <seealso cref="DateTimeFormatJsonConverterAttribute" />
public sealed class DateTimeFormatJsonConverter(
	string[] readFormats,
	string writeFormat,
	CultureInfo? culture = null,
	DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces)
	: JsonConverter<DateTime>
{
	private readonly CultureInfo _culture = culture ?? CultureInfo.InvariantCulture;

	/// <summary>
	///    Reads and converts JSON to a <see cref="DateTime" /> using the configured read formats.
	/// </summary>
	/// <param name="reader">The reader to read JSON from.</param>
	/// <param name="typeToConvert">The type being converted (always <see cref="DateTime" />).</param>
	/// <param name="options">The serializer options.</param>
	/// <returns>The parsed <see cref="DateTime" /> value.</returns>
	/// <exception cref="JsonException">
	///    Thrown when the JSON token is not a string or when the string cannot be parsed using any of the configured read
	///    formats.
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method attempts to parse the JSON string value using each format in <c>readFormats</c> until one succeeds.
	///       The parsing behavior is controlled by the <see cref="DateTimeStyles" /> parameter provided in the constructor.
	///    </para>
	///    <para>
	///       <strong>Error handling:</strong> If parsing fails with all formats, a <see cref="JsonException" /> is thrown
	///       with the invalid value included in the error message for debugging purposes.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Accepts multiple input formats
	/// var converter = new DateTimeFormatJsonConverter(
	///     readFormats: new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM-dd-yyyy" },
	///     writeFormat: "yyyy-MM-dd"
	/// );
	/// 
	/// // All of these will parse successfully:
	/// // "15/01/2024" (dd/MM/yyyy)
	/// // "2024-01-15" (yyyy-MM-dd)
	/// // "01-15-2024" (MM-dd-yyyy)
	/// 
	/// // This will throw JsonException:
	/// // "2024/01/15" (format not in readFormats)
	/// </code>
	/// </example>
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("DateTime must be 'string'.");
		var s = reader.GetString();
		return DateTime.TryParseExact(s, readFormats, _culture, styles, out var dt)
			? dt
			: throw new JsonException($"DateTime invalid: '{s}'.");
	}

	/// <summary>
	///    Writes a <see cref="DateTime" /> value as a JSON string using the configured write format.
	/// </summary>
	/// <param name="writer">The writer to write JSON to.</param>
	/// <param name="value">The <see cref="DateTime" /> value to serialize.</param>
	/// <param name="options">The serializer options.</param>
	/// <remarks>
	///    The <see cref="DateTime" /> is formatted using the <c>writeFormat</c> string and the configured culture.
	///    This ensures consistent output format regardless of which read format was used during deserialization.
	/// </remarks>
	/// <example>
	///    <code>
	/// var converter = new DateTimeFormatJsonConverter(
	///     readFormats: new[] { "dd/MM/yyyy", "yyyy-MM-dd" },
	///     writeFormat: "yyyy-MM-dd"  // Always outputs in this format
	/// );
	/// 
	/// var date = new DateTime(2024, 1, 15);
	/// // Output: "2024-01-15" (regardless of input format)
	/// </code>
	/// </example>
	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString(writeFormat, _culture));
	}
}

/// <summary>
///    JSON converter for nullable <see cref="DateTime" /> (<c>DateTime?</c>) that supports custom read and write formats.
/// </summary>
/// <remarks>
///    <para>
///       This converter wraps <see cref="DateTimeFormatJsonConverter" /> to provide the same formatting capabilities
///       for nullable <see cref="DateTime" /> values, with proper handling of JSON <c>null</c> values.
///    </para>
///    <para>
///       <strong>Null handling:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Reads JSON <c>null</c> as <c>null</c> (not throwing an exception)</description>
///       </item>
///       <item>
///          <description>Writes <c>null</c> values as JSON <c>null</c></description>
///       </item>
///       <item>
///          <description>Delegates non-null values to the inner <see cref="DateTimeFormatJsonConverter" /></description>
///       </item>
///    </list>
///    <para>
///       All formatting behavior (read formats, write format, culture, styles) is identical to
///       <see cref="DateTimeFormatJsonConverter" />.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// // Register converter for nullable DateTime
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new NullableDateTimeFormatJsonConverter(
///     readFormats: new[] { "dd/MM/yyyy", "yyyy-MM-dd" },
///     writeFormat: "yyyy-MM-dd"
/// ));
/// 
/// // Serialization
/// DateTime? nullDate = null;
/// var json1 = JsonSerializer.Serialize(nullDate, options);
/// // Result: null
/// 
/// DateTime? date = new DateTime(2024, 1, 15);
/// var json2 = JsonSerializer.Serialize(date, options);
/// // Result: "2024-01-15"
/// 
/// // Deserialization
/// var result1 = JsonSerializer.Deserialize&lt;DateTime?&gt;("null", options);
/// // Result: null
/// 
/// var result2 = JsonSerializer.Deserialize&lt;DateTime?&gt;("\"15/01/2024\"", options);
/// // Result: DateTime(2024, 1, 15)
/// 
/// // Using with attribute
/// public class OptionalEvent
/// {
///     [DateTimeFormatJsonConverter(new[] { "dd/MM/yyyy" }, "yyyy-MM-dd")]
///     public DateTime? EventDate { get; set; }  // Can be null
/// }
/// </code>
/// </example>
/// <seealso cref="DateTimeFormatJsonConverter" />
/// <seealso cref="DateTimeFormatJsonConverterAttribute" />
public sealed class NullableDateTimeFormatJsonConverter(
	string[] readFormats,
	string writeFormat,
	CultureInfo? culture = null,
	DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces)
	: JsonConverter<DateTime?>
{
	private readonly DateTimeFormatJsonConverter _inner = new(readFormats, writeFormat, culture, styles);

	/// <summary>
	///    Reads and converts JSON to a nullable <see cref="DateTime" /> (<c>DateTime?</c>).
	/// </summary>
	/// <param name="reader">The reader to read JSON from.</param>
	/// <param name="typeToConvert">The type being converted (<c>DateTime?</c>).</param>
	/// <param name="options">The serializer options.</param>
	/// <returns>
	///    <c>null</c> if the JSON token is <see cref="JsonTokenType.Null" />;
	///    otherwise, the parsed <see cref="DateTime" /> value.
	/// </returns>
	/// <exception cref="JsonException">
	///    Thrown when the JSON token is not null and not a valid string, or when the string cannot be parsed.
	/// </exception>
	/// <remarks>
	///    This method delegates to <see cref="DateTimeFormatJsonConverter.Read" /> for non-null values,
	///    providing consistent parsing behavior.
	/// </remarks>
	public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType == JsonTokenType.Null ? null : _inner.Read(ref reader, typeof(DateTime), options);
	}

	/// <summary>
	///    Writes a nullable <see cref="DateTime" /> value as JSON.
	/// </summary>
	/// <param name="writer">The writer to write JSON to.</param>
	/// <param name="value">The nullable <see cref="DateTime" /> value to serialize.</param>
	/// <param name="options">The serializer options.</param>
	/// <remarks>
	///    <para>
	///       Writes JSON <c>null</c> if <paramref name="value" /> is <c>null</c>.
	///       Otherwise, delegates to <see cref="DateTimeFormatJsonConverter.Write" /> to format the date.
	///    </para>
	/// </remarks>
	public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
	{
		if (value is null)
			writer.WriteNullValue();
		else
			_inner.Write(writer, value.Value, options);
	}
}

/// <summary>
///    Attribute to apply custom <see cref="DateTime" /> formatting to individual properties or fields using JSON
///    converters.
/// </summary>
/// <remarks>
///    <para>
///       This attribute provides a declarative way to specify custom date formats for specific properties without
///       registering converters globally. It supports both <see cref="DateTime" /> and nullable <see cref="DateTime" />
///       (<c>DateTime?</c>) properties.
///    </para>
///    <para>
///       <strong>Parameters:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description><strong>readFormats:</strong> Array of format strings for parsing (allows multiple input formats)</description>
///       </item>
///       <item>
///          <description><strong>writeFormat:</strong> Single format string for serialization (ensures consistent output)</description>
///       </item>
///       <item>
///          <description>
///             <strong>culture:</strong> Culture name string (e.g., "en-US", "fr-FR") or <c>null</c> for current
///             thread culture
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>styles:</strong> <see cref="DateTimeStyles" /> flags for parsing behavior (default:
///             AssumeLocal | AllowWhiteSpaces)
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Important:</strong> At least one read format must be provided, and the write format cannot be null or
///       empty.
///    </para>
///    <para>
///       <strong>Culture handling:</strong> If no culture is specified (<c>null</c>), uses
///       <see cref="Thread.CurrentThread" />.<see cref="Thread.CurrentCulture" />.
///       For culture-independent behavior, explicitly specify <c>culture: ""</c> to use
///       <see cref="CultureInfo.InvariantCulture" />.
///    </para>
/// </remarks>
/// <example>
///    <code>
/// public class Event
/// {
///     // Property with multiple read formats, single write format
///     [DateTimeFormatJsonConverter(
///         readFormats: new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" },
///         writeFormat: "yyyy-MM-dd"
///     )]
///     public DateTime EventDate { get; set; }
///     
///     // Nullable DateTime with US culture
///     [DateTimeFormatJsonConverter(
///         readFormats: new[] { "MM/dd/yyyy", "yyyy-MM-dd" },
///         writeFormat: "MM/dd/yyyy",
///         culture: "en-US"
///     )]
///     public DateTime? OptionalDate { get; set; }
///     
///     // With specific DateTimeStyles
///     [DateTimeFormatJsonConverter(
///         readFormats: new[] { "yyyy-MM-ddTHH:mm:ss" },
///         writeFormat: "yyyy-MM-ddTHH:mm:ss",
///         styles: DateTimeStyles.AssumeUniversal
///     )]
///     public DateTime UtcTimestamp { get; set; }
/// }
/// 
/// // Usage
/// var json = """
/// {
///     "eventDate": "15/01/2024",
///     "optionalDate": null,
///     "utcTimestamp": "2024-01-15T14:30:00"
/// }
/// """;
/// 
/// var evt = JsonSerializer.Deserialize&lt;Event&gt;(json);
/// // eventDate: DateTime(2024, 1, 15)
/// // optionalDate: null
/// // utcTimestamp: DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc)
/// 
/// var outputJson = JsonSerializer.Serialize(evt);
/// // Result:
/// // {
/// //   "eventDate": "2024-01-15",
/// //   "optionalDate": null,
/// //   "utcTimestamp": "2024-01-15T14:30:00"
/// // }
/// </code>
/// </example>
/// <seealso cref="DateTimeFormatJsonConverter" />
/// <seealso cref="NullableDateTimeFormatJsonConverter" />
/// <seealso cref="JsonConverterAttribute" />
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DateTimeFormatJsonConverterAttribute : JsonConverterAttribute
{
	/// <summary>
	///    Initializes a new instance of the <see cref="DateTimeFormatJsonConverterAttribute" /> class.
	/// </summary>
	/// <param name="readFormats">
	///    Array of format strings for parsing dates during deserialization.
	///    Must contain at least one non-null, non-empty format string.
	///    Supports standard and custom <see cref="DateTime" /> format strings.
	/// </param>
	/// <param name="writeFormat">
	///    Format string for serializing dates to JSON.
	///    Must be a non-null, non-empty standard or custom <see cref="DateTime" /> format string.
	/// </param>
	/// <param name="culture">
	///    Culture name (e.g., "en-US", "fr-FR", "de-DE") for culture-specific formatting.
	///    When <c>null</c>, uses the current thread's culture.
	///    Use empty string <c>""</c> for <see cref="CultureInfo.InvariantCulture" />.
	/// </param>
	/// <param name="styles">
	///    <see cref="DateTimeStyles" /> flags that control parsing behavior.
	///    Common values:
	///    <list type="bullet">
	///       <item>
	///          <description><see cref="DateTimeStyles.AssumeLocal" />: Treats dates without timezone as local time</description>
	///       </item>
	///       <item>
	///          <description><see cref="DateTimeStyles.AssumeUniversal" />: Treats dates without timezone as UTC</description>
	///       </item>
	///       <item>
	///          <description><see cref="DateTimeStyles.AllowWhiteSpaces" />: Allows leading/trailing whitespace in input</description>
	///       </item>
	///       <item>
	///          <description><see cref="DateTimeStyles.AdjustToUniversal" />: Converts parsed dates to UTC</description>
	///       </item>
	///    </list>
	///    Default: <c>AssumeLocal | AllowWhiteSpaces</c>
	/// </param>
	/// <exception cref="ArgumentException">
	///    Thrown when:
	///    <list type="bullet">
	///       <item>
	///          <description><paramref name="readFormats" /> is null, empty, or contains null/empty elements</description>
	///       </item>
	///       <item>
	///          <description><paramref name="writeFormat" /> is null or empty</description>
	///       </item>
	///    </list>
	/// </exception>
	/// <remarks>
	///    <para>
	///       <strong>Format string reference:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description><c>"yyyy-MM-dd"</c>: ISO 8601 date (2024-01-15)</description>
	///       </item>
	///       <item>
	///          <description><c>"dd/MM/yyyy"</c>: European format (15/01/2024)</description>
	///       </item>
	///       <item>
	///          <description><c>"MM/dd/yyyy"</c>: US format (01/15/2024)</description>
	///       </item>
	///       <item>
	///          <description><c>"yyyy-MM-ddTHH:mm:ss"</c>: ISO 8601 with time (2024-01-15T14:30:00)</description>
	///       </item>
	///       <item>
	///          <description><c>"dd MMM yyyy"</c>: Culture-specific month names (15 Jan 2024)</description>
	///       </item>
	///    </list>
	///    <para>
	///       See
	///       <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">
	///          Custom
	///          date and time format strings
	///       </see>
	///       for complete format string reference.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Basic usage
	/// [DateTimeFormatJsonConverter(
	///     readFormats: new[] { "dd-MM-yyyy" },
	///     writeFormat: "dd/MM/yyyy"
	/// )]
	/// public DateTime BirthDate { get; set; }
	/// 
	/// // Multiple input formats
	/// [DateTimeFormatJsonConverter(
	///     readFormats: new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM-dd-yyyy" },
	///     writeFormat: "yyyy-MM-dd"
	/// )]
	/// public DateTime EventDate { get; set; }
	/// 
	/// // With specific culture
	/// [DateTimeFormatJsonConverter(
	///     readFormats: new[] { "dd MMMM yyyy" },  // "15 janvier 2024"
	///     writeFormat: "dd/MM/yyyy",
	///     culture: "fr-FR"
	/// )]
	/// public DateTime FrenchDate { get; set; }
	/// 
	/// // UTC timestamps
	/// [DateTimeFormatJsonConverter(
	///     readFormats: new[] { "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss" },
	///     writeFormat: "yyyy-MM-ddTHH:mm:ssZ",
	///     styles: DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
	/// )]
	/// public DateTime UtcTimestamp { get; set; }
	/// 
	/// // Invariant culture (culture-independent)
	/// [DateTimeFormatJsonConverter(
	///     readFormats: new[] { "yyyy-MM-dd" },
	///     writeFormat: "yyyy-MM-dd",
	///     culture: ""  // Empty string = InvariantCulture
	/// )]
	/// public DateTime StandardDate { get; set; }
	/// </code>
	/// </example>
	/// <seealso cref="DateTimeFormatJsonConverter" />
	/// <seealso cref="NullableDateTimeFormatJsonConverter" />
	/// <seealso cref="JsonConverterAttribute" />
	public DateTimeFormatJsonConverterAttribute(string[]? readFormats, string writeFormat, string? culture = null,
		DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces)
	{
		if (readFormats is null || readFormats.Length == 0 || readFormats.Any(s => s.IsNullOrWhiteSpace()))
			throw new ArgumentException(
				$"{nameof(readFormats)} must contains at least one element and cannot contains null or empty elements",
				nameof(readFormats));
		if (writeFormat.IsNullOrWhiteSpace())
			throw new ArgumentException($"{nameof(writeFormat)} cannot be null or empty", nameof(writeFormat));
		_readFormats = readFormats;
		_writeFormat = writeFormat;
		_culture = culture is not null ? new(culture) : Thread.CurrentThread.CurrentCulture;
		_styles = styles;
	}

	private readonly CultureInfo _culture;
	private readonly string[] _readFormats;
	private readonly DateTimeStyles _styles;
	private readonly string _writeFormat;

	/// <summary>
	///    Creates the appropriate JSON converter instance for the target property or field type.
	/// </summary>
	/// <param name="typeToConvert">
	///    The type of the property or field being converted.
	///    Must be either <see cref="DateTime" /> or <c>DateTime?</c>.
	/// </param>
	/// <returns>
	///    A <see cref="DateTimeFormatJsonConverter" /> if the type is <see cref="DateTime" />,
	///    or a <see cref="NullableDateTimeFormatJsonConverter" /> if the type is <c>DateTime?</c>.
	/// </returns>
	/// <exception cref="NotSupportedException">
	///    Thrown when <paramref name="typeToConvert" /> is neither <see cref="DateTime" /> nor <c>DateTime?</c>.
	/// </exception>
	/// <remarks>
	///    This method is called automatically by the JSON serialization infrastructure.
	///    It creates the converter with the formats, culture, and styles specified in the attribute constructor.
	/// </remarks>
	public override JsonConverter CreateConverter(Type typeToConvert)
	{
		if (typeToConvert == typeof(DateTime))
			return new DateTimeFormatJsonConverter(_readFormats, _writeFormat, _culture, _styles);

		if (typeToConvert == typeof(DateTime?))
			return new NullableDateTimeFormatJsonConverter(_readFormats, _writeFormat, _culture, _styles);

		throw new NotSupportedException($"DateTimeFormatsAttribute no soporta el tipo {typeToConvert}.");
	}
}