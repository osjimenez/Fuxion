using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Fuxion;

/// <summary>
/// Provides extension methods for encoding and decoding operations on byte arrays and strings.
/// Supports hexadecimal, Base64, and Base64Url encoding formats with various conversion options.
/// </summary>
/// <remarks>
/// This class uses the extension mechanism to add encoding-related functionality to <see cref="byte" /> and <see cref="string" /> types
/// through the Fuxion extensions framework. All operations return <see cref="Response{T}" /> objects to enable proper error handling.
/// </remarks>
public static class EncodingExtensions
{
	extension(FuxionExtensions<byte[]?> me)
	{
		/// <summary>
		/// Provides encoding-related extension operations for this byte[] value.
		/// </summary>
		public EncodingExtensions<byte[]?> Encoding
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(FuxionExtensions<string?> me)
	{
		/// <summary>
		/// Provides encoding-related extension operations for this string value.
		/// </summary>
		public EncodingExtensions<string?> Encoding
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(EncodingExtensions<byte[]?> me)
	{
		/// <summary>
		/// Converts a byte array value to a hexadecimal string.
		/// </summary>
		/// <param name="separatorChar">
		/// Optional separator character between bytes (for example <c>'-'</c> or <c>':'</c>).
		/// If <c>null</c>, no separator is used.
		/// </param>
		/// <param name="asBigEndian">
		/// When <c>true</c>, the underlying byte array is processed in reverse order so that the resulting
		/// hex string represents the value in big-endian order. When <c>false</c>, the bytes
		/// are encoded in their original order.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the hexadecimal string representation of the underlying byte array
		/// when the operation succeeds, or an error response when the underlying value is <c>null</c>.
		/// </returns>
		public Response<string> ToHexString(char? separatorChar = null, bool asBigEndian = false)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source byte array is null").AsPayload<string>();

			if (me.Value.Length == 0)
				return string.Empty;

			var byteCount = me.Value.Length;
			var useSeparator = separatorChar.HasValue;

			// Each byte -> 2 hex chars + optional separator.
			var charsPerByte = useSeparator ? 3 : 2;
			var builder = new StringBuilder(byteCount * charsPerByte);

			if (asBigEndian)
				for (var i = byteCount - 1; i >= 0; i--)
					AppendByte(me.Value[i], i == 0);
			else
				for (var i = 0; i < byteCount; i++)
					AppendByte(me.Value[i], i == byteCount - 1);

			return builder.ToString();

			void AppendByte(byte b, bool isLast)
			{
				builder.Append(b.ToString("X2"));
				if (useSeparator && !isLast)
					builder.Append(separatorChar!.Value);
			}
		}

		/// <summary>
		/// Converts a byte array value to a regular Base64 string.
		/// </summary>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the Base64-encoded string when the operation succeeds,
		/// or an error response when the underlying value is <c>null</c>.
		/// </returns>
		public Response<string> ToBase64String()
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source byte array is null").AsPayload<string>();

			return Convert.ToBase64String(me.Value);
		}

		/// <summary>
		/// Converts a byte array value to a Base64Url-encoded string.
		/// </summary>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the Base64Url-encoded string when the operation succeeds,
		/// or an error response when the underlying value is <c>null</c>.
		/// </returns>
		public Response<string> ToBase64UrlString()
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source byte array is null").AsPayload<string>();

#if NET9_0_OR_GREATER
			// System.Buffers.Text.Base64Url
			return System.Buffers.Text.Base64Url.EncodeToString(me.Value);
#else
			// Regular Base64
			var s = Convert.ToBase64String(me.Value);

			// Convert to Base64Url:
			s = s.TrimEnd('='); // remove padding
			s = s.Replace('+', '-');
			s = s.Replace('/', '_');

			return s;
#endif
		}
	}

	extension(EncodingExtensions<string?> me)
	{
		/// <summary>
		/// Converts a hexadecimal string value to a byte array.
		/// </summary>
		/// <param name="separatorChar">
		/// Optional separator character used in the hexadecimal string (for example <c>'-'</c> or <c>':'</c>).
		/// If not <c>null</c>, all occurrences of this character are removed before parsing.
		/// </param>
		/// <param name="isBigEndian">
		/// When <c>true</c>, the parsed byte array is reversed so that the resulting bytes represent
		/// the original value in little-endian order (assuming the hex string represents big-endian).
		/// When <c>false</c>, the bytes are returned in the same order as they appear in the hex string.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the decoded byte array when the operation succeeds.
		/// If the underlying string is <c>null</c> or has an invalid length (odd number of hex characters),
		/// an error response is returned instead.
		/// </returns>
		public Response<byte[]> ToBytesFromHexString(char? separatorChar = null, bool isBigEndian = false)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source string is null").AsPayload<byte[]>();

			var hex = me.Value;
			if (separatorChar.HasValue)
				hex = hex.Replace(separatorChar.Value.ToString(), string.Empty);

			if (hex.Length == 0)
				return Array.Empty<byte>();

			if (hex.Length % 2 != 0)
				return Response.Get.Critical("Hex string must have an even number of characters.").AsPayload<byte[]>();

			var byteCount = hex.Length / 2;
			var bytes = new byte[byteCount];

			for (var i = 0; i < byteCount; i++)
			{
				var byteString = hex.Substring(i * 2, 2);
				try
				{
					bytes[i] = Convert.ToByte(byteString, 16);
				}
				catch (Exception ex)
				{
					return Response.Get.Critical($"Error converting hexadecimal string '{me.Value}' to byte array. Byte '{byteString}' is not valid.", exception: ex).AsPayload<byte[]>();
				}
			}

			if (isBigEndian)
				Array.Reverse(bytes);

			return bytes;
		}

		/// <summary>
		/// Decodes a regular Base64 string value into a byte array.
		/// </summary>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the decoded byte array when the operation succeeds,
		/// or an error response when the underlying string is <c>null</c> or conversion fails.
		/// </returns>
		public Response<byte[]> ToBytesFromBase64String()
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source string is null").AsPayload<byte[]>();
			try
			{
				return Convert.FromBase64String(me.Value);
			}
			catch (Exception ex)
			{
				return Response.Get.Critical($"Error converting Base64 string '{me.Value}' to byte array.", exception: ex).AsPayload<byte[]>();
			}
		}

		/// <summary>
		/// Decodes a Base64-encoded string into a text string using the specified character encoding.
		/// This is a convenience method that combines Base64 decoding with text decoding in a single operation.
		/// </summary>
		/// <param name="encoding">
		/// The character encoding to use for converting the decoded bytes to a string.
		/// When <c>null</c>, defaults to <see cref="Encoding.UTF8"/>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the decoded text string when the operation succeeds,
		/// or an error response when the underlying string is <c>null</c>, is not valid Base64, or decoding fails.
		/// </returns>
		/// <remarks>
		/// This method first decodes the Base64 string to bytes using <see cref="ToBytesFromBase64String"/>, 
		/// then converts those bytes to a text string using the specified <paramref name="encoding"/>.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Encode and decode with UTF-8 (default)
		/// var original = "Hello, World!";
		/// var base64 = original.Fx.Encoding.ToBase64String();  // "SGVsbG8sIFRvcmxkIQ=="
		/// var decoded = base64.Payload.Fx.Encoding.DecodeFromBase64();
		/// Console.WriteLine(decoded.Payload);  // Output: "Hello, World!"
		/// 
		/// // Using a different encoding
		/// var base64Latin1 = "SGVsbG8sIFRvcmxkIQ==";
		/// var decodedLatin1 = base64Latin1.Fx.Encoding.DecodeFromBase64(Encoding.Latin1);
		/// </code>
		/// </example>
		public Response<string> DecodeFromBase64(Encoding? encoding = null)
		{
			var bytesResponse = me.ToBytesFromBase64String();
			if (bytesResponse.IsError)
				return bytesResponse.AsPayload<string>();
			return (encoding ?? Encoding.UTF8).GetString(bytesResponse.Payload);
		}

		/// <summary>
		/// Encodes a text string to a Base64-encoded string using the specified character encoding.
		/// This is a convenience method that combines text encoding and Base64 encoding in a single operation.
		/// </summary>
		/// <param name="encoding">
		/// The character encoding to use for converting the string to bytes before Base64 encoding.
		/// When <c>null</c>, defaults to <see cref="Encoding.UTF8"/>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the Base64-encoded string when the operation succeeds,
		/// or an error response when the underlying string is <c>null</c> or encoding fails.
		/// </returns>
		/// <remarks>
		/// This method first converts the text string to bytes using the specified <paramref name="encoding"/>, 
		/// then encodes those bytes to Base64.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Encode with UTF-8 (default)
		/// var text = "Hello, World!";
		/// var response = text.Fx.Encoding.ToBase64String();
		/// Console.WriteLine(response.Payload);  // Output: "SGVsbG8sIFRvcmxkIQ=="
		/// 
		/// // Encode with a different encoding
		/// var textLatin1 = "Café";
		/// var responseLatin1 = textLatin1.Fx.Encoding.ToBase64String(Encoding.Latin1);
		/// 
		/// // Round-trip example
		/// var original = "Test data 123";
		/// var encoded = original.Fx.Encoding.ToBase64String();
		/// var decoded = encoded.Payload.Fx.Encoding.DecodeFromBase64();
		/// Console.WriteLine(decoded.Payload);  // Output: "Test data 123"
		/// </code>
		/// </example>
		public Response<string> ToBase64String(Encoding? encoding = null)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source string is null").AsPayload<string>();

			var bytes = (encoding ?? Encoding.UTF8).GetBytes(me.Value);
			return bytes.Fx.Encoding.ToBase64String();
		}

		/// <summary>
		/// Decodes the underlying Base64Url-encoded string into a byte array.
		/// </summary>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the decoded byte array when the operation succeeds,
		/// or an error response when the underlying string is <c>null</c> or conversion fails.
		/// </returns>
		public Response<byte[]> ToBytesFromBase64UrlString()
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source string is null").AsPayload<byte[]>();

#if NET9_0_OR_GREATER
			try
			{
				return System.Buffers.Text.Base64Url.DecodeFromChars(me.Value);
			}
			catch (Exception ex)
			{
				return Response.Get.Critical(
						$"Error converting Base64Url string '{me.Value}' to byte array.",
						exception: ex)
					.AsPayload<byte[]>();
			}
#else
			var s = me.Value
				.Replace('-', '+')
				.Replace('_', '/');

			switch (s.Length % 4)
			{
				case 0:
					break; // INFO No padding needed
				case 2:
					s += "==";
					break;
				case 3:
					s += "=";
					break;
				default:
					return Response.Get.InvalidData($"Source string '{me.Value}' has invalid Base64Url string length.")
						.AsPayload<byte[]>();
			}

			try
			{
				return Convert.FromBase64String(s);
			}
			catch (Exception ex)
			{
				return Response.Get.Critical($"Error converting Base64Url string '{me.Value}' to byte array.", exception: ex).AsPayload<byte[]>();
			}
#endif
		}

		/// <summary>
		/// Decodes a Base64Url-encoded string into a text string using the specified character encoding.
		/// This is a convenience method that combines Base64Url decoding with text decoding in a single operation.
		/// </summary>
		/// <param name="encoding">
		/// The character encoding to use for converting the decoded bytes to a string.
		/// When <c>null</c>, defaults to <see cref="Encoding.UTF8"/>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the decoded text string when the operation succeeds,
		/// or an error response when the underlying string is <c>null</c>, is not valid Base64Url, or decoding fails.
		/// </returns>
		/// <remarks>
		/// This method first decodes the Base64Url string to bytes using <see cref="ToBytesFromBase64UrlString"/>, 
		/// then converts those bytes to a text string using the specified <paramref name="encoding"/>.
		/// Base64Url is a URL-safe variant of Base64 that uses <c>'-'</c> instead of <c>'+'</c> and <c>'_'</c> instead of <c>'/'</c>, 
		/// and omits padding characters.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Encode and decode with UTF-8 (default)
		/// var original = "Hello, World!";
		/// var base64Url = original.Fx.Encoding.ToBase64UrlString();  // "SGVsbG8sIFRvcmxkIQ" (no padding)
		/// var decoded = base64Url.Payload.Fx.Encoding.DecodeFromBase64Url();
		/// Console.WriteLine(decoded.Payload);  // Output: "Hello, World!"
		/// 
		/// // URL-safe characters example
		/// var urlData = "test+data/value";
		/// var encoded = urlData.Fx.Encoding.ToBase64UrlString();  // Uses '-' and '_' instead of '+' and '/'
		/// var decodedUrl = encoded.Payload.Fx.Encoding.DecodeFromBase64Url();
		/// Console.WriteLine(decodedUrl.Payload);  // Output: "test+data/value"
		/// 
		/// // Using a different encoding
		/// var base64UrlLatin1 = "SGVsbG8";
		/// var decodedLatin1 = base64UrlLatin1.Fx.Encoding.DecodeFromBase64Url(Encoding.Latin1);
		/// </code>
		/// </example>
		public Response<string> DecodeFromBase64Url(Encoding? encoding = null)
		{
			var bytesResponse = me.ToBytesFromBase64UrlString();
			if (bytesResponse.IsError)
				return bytesResponse.AsPayload<string>();
			return (encoding ?? Encoding.UTF8).GetString(bytesResponse.Payload);
		}

		/// <summary>
		/// Encodes a text string to a Base64Url-encoded string using the specified character encoding.
		/// This is a convenience method that combines text encoding and Base64Url encoding in a single operation.
		/// </summary>
		/// <param name="encoding">
		/// The character encoding to use for converting the string to bytes before Base64Url encoding.
		/// When <c>null</c>, defaults to <see cref="Encoding.UTF8"/>.
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is the Base64Url-encoded string when the operation succeeds,
		/// or an error response when the underlying string is <c>null</c> or encoding fails.
		/// </returns>
		/// <remarks>
		/// This method first converts the text string to bytes using the specified <paramref name="encoding"/>, 
		/// then encodes those bytes to Base64Url.
		/// Base64Url is a URL-safe variant of Base64 that uses <c>'-'</c> instead of <c>'+'</c> and <c>'_'</c> instead of <c>'/'</c>, 
		/// and omits padding characters, making it safe for use in URLs and file names.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Encode with UTF-8 (default)
		/// var text = "Hello, World!";
		/// var response = text.Fx.Encoding.ToBase64UrlString();
		/// Console.WriteLine(response.Payload);  // Output: "SGVsbG8sIFRvcmxkIQ" (no padding)
		/// 
		/// // Compare with regular Base64
		/// var base64 = text.Fx.Encoding.ToBase64String().Payload;        // "SGVsbG8sIFRvcmxkIQ=="
		/// var base64Url = text.Fx.Encoding.ToBase64UrlString().Payload;  // "SGVsbG8sIFRvcmxkIQ"
		/// 
		/// // URL-safe usage
		/// var urlData = "user+id/session";
		/// var urlSafe = urlData.Fx.Encoding.ToBase64UrlString();
		/// // Can be safely used in URLs: https://example.com/api?token={urlSafe.Payload}
		/// 
		/// // Round-trip example
		/// var original = "Test data 123";
		/// var encoded = original.Fx.Encoding.ToBase64UrlString();
		/// var decoded = encoded.Payload.Fx.Encoding.DecodeFromBase64Url();
		/// Console.WriteLine(decoded.Payload);  // Output: "Test data 123"
		/// </code>
		/// </example>
		public Response<string> ToBase64UrlString(Encoding? encoding = null)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source string is null").AsPayload<string>();

			var bytes = (encoding ?? Encoding.UTF8).GetBytes(me.Value);
			return bytes.Fx.Encoding.ToBase64UrlString();
		}
	}
}

/// <summary>
/// Generic wrapper class that provides encoding extension methods for values of type <typeparamref name="T"/>.
/// This class is used internally by the Fuxion extensions framework to enable fluent API syntax for encoding operations.
/// </summary>
/// <typeparam name="T">The type of the value being wrapped, typically <see cref="byte"/> or <see cref="string"/>.</typeparam>
/// <remarks>
/// This class inherits from <see cref="Extensions{T}"/> and serves as a specialized container for encoding-related operations.
/// Users typically access encoding functionality through extension methods rather than instantiating this class directly.
/// </remarks>
/// <example>
/// <code>
/// // Accessed through extension syntax
/// byte[] data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
/// var hexString = data.Fx.Encoding.ToHexString();
/// 
/// string text = "Hello";
/// var base64 = text.Fx.Encoding.ToBase64String();
/// </code>
/// </example>
public class EncodingExtensions<T>(T me) : Extensions<T>(me);