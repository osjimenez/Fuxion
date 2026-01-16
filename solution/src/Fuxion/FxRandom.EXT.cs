using System.Linq;
using System.Security.Cryptography;

namespace Fuxion;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Provides extension methods for generating random strings using custom character sets.
/// </summary>
/// <remarks>
/// This class extends <see cref="string"/> to enable random string generation using the string itself as a charset.
/// On .NET 8.0 and greater, uses <see cref="RandomNumberGenerator"/> for cryptographically strong random generation.
/// On older frameworks, uses <see cref="Random"/> with a GUID-based seed for pseudo-random generation.
/// </remarks>
public static class RandomExtensions
{
	extension(string me)
	{
#pragma warning disable CS1573, CS1572 // Parameter 'ran' conditionally exists based on target framework
		/// <summary>
		/// Generates a random string of the specified length using the characters in the current string as the charset.
		/// If the current string is <c>null</c> or whitespace, uses the default charset "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".
		/// </summary>
		/// <param name="length">The length of the random string to generate.</param>
		/// <param name="ran">
		/// (Legacy frameworks only) Optional <see cref="Random"/> instance to use for random number generation. 
		/// If <c>null</c>, a new instance with a GUID-based seed will be created.
		/// This parameter is only available on frameworks prior to .NET 8.0.
		/// </param>
		/// <returns>A random string composed of characters from the charset.</returns>
		/// <remarks>
		/// This method uses <see cref="RandomNumberGenerator"/> on .NET 8.0 or greater for cryptographically strong random generation.
		/// On older frameworks, it uses <see cref="Random"/> with a seed based on <see cref="Guid.NewGuid"/>.
		/// </remarks>
		/// <example>
		/// <code>
		/// // Generate password with custom charset
		/// var charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%";
		/// var password = charset.RandomString(12);  // e.g., "A3#9X@M2K5P!"
		/// 
		/// // Generate hexadecimal string
		/// var hex = "0123456789ABCDEF".RandomString(8);  // e.g., "3FA8C12D"
		/// 
		/// // Empty/null uses default charset
		/// var random = "".RandomString(10);  // Uses "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
		/// </code>
		/// </example>
		public string RandomString(
			int length
#if !NET8_0_OR_GREATER
			, Random? ran = null
#endif
		)
#pragma warning restore CS1573, CS1572
		{
			const string defaultStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
#if !NET8_0_OR_GREATER
			ran ??= new(Guid.NewGuid().GetHashCode());
			var str = string.IsNullOrWhiteSpace(me) ? defaultStr : me;
			return new(Enumerable.Repeat(str, length)
				.Select(s => s[ran!.Next(s.Length)])
				.ToArray());
#else
			return RandomNumberGenerator.GetString((string.IsNullOrWhiteSpace(me) ? defaultStr : me).AsSpan(), length);
#endif
		}
	}
}
