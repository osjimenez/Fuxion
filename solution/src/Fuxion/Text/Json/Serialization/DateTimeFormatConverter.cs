using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Fuxion.Text.Json.Serialization;

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class DateTimeFormatJsonConverter(
	string[] readFormats,
	string writeFormat,
	CultureInfo? culture = null,
	DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces)
	: JsonConverter<DateTime>
{
	readonly CultureInfo _culture = culture ?? CultureInfo.InvariantCulture;

	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("DateTime must be 'string'.");
		var s = reader.GetString();
		return DateTime.TryParseExact(s, readFormats, _culture, styles, out var dt)
			? dt
			: throw new JsonException($"DateTime invalid: '{s}'.");
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		 => writer.WriteStringValue(value.ToString(writeFormat, _culture));
}

public sealed class NullableDateTimeFormatJsonConverter(
	string[] readFormats,
	string writeFormat,
	CultureInfo? culture = null,
	DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces)
	: JsonConverter<DateTime?>
{
	readonly DateTimeFormatJsonConverter _inner = new(readFormats, writeFormat, culture, styles);

	public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		 => reader.TokenType == JsonTokenType.Null ? null : _inner.Read(ref reader, typeof(DateTime), options);

	public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
	{
		if (value is null)
			writer.WriteNullValue();
		else
			_inner.Write(writer, value.Value, options);
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DateTimeFormatJsonConverterAttribute : JsonConverterAttribute
{
	readonly string[] _readFormats;
	readonly string _writeFormat;
	readonly CultureInfo _culture;
	readonly DateTimeStyles _styles;

	// Use: [DateTimeFormats(["dd-MM-yyyy"], "dd/MM/yyyy")]
	public DateTimeFormatJsonConverterAttribute(string[]? readFormats, string writeFormat, string? culture = null, DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces)
	{
		if (readFormats is null || readFormats.Length == 0 || readFormats.Any(s => s.IsNullOrWhiteSpace()))
			throw new ArgumentException($"{nameof(readFormats)} must contains at least one element and cannot contains null or empty elements", nameof(readFormats));
		if(writeFormat.IsNullOrWhiteSpace())
			throw new ArgumentException($"{nameof(writeFormat)} cannot be null or empty", nameof(writeFormat));
		_readFormats = readFormats;
		_writeFormat = writeFormat;
		_culture = culture is not null ? new CultureInfo(culture) : Thread.CurrentThread.CurrentCulture;
		_styles = styles;
	}

	public override JsonConverter CreateConverter(Type typeToConvert)
	{
		if (typeToConvert == typeof(DateTime))
			return new DateTimeFormatJsonConverter(_readFormats, _writeFormat, _culture, _styles);

		if (typeToConvert == typeof(DateTime?))
			return new NullableDateTimeFormatJsonConverter(_readFormats, _writeFormat, _culture, _styles);

		throw new NotSupportedException($"DateTimeFormatsAttribute no soporta el tipo {typeToConvert}.");
	}
}


