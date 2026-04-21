namespace Fuxion;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides extension methods for mathematical operations on numeric types.
/// Includes specialized division operations that return both quotient and remainder,
/// and optimized division by powers of two using bit shifting.
/// </summary>
/// <remarks>
/// This class uses the extension mechanism to add mathematical functionality to numeric types
/// (<see cref="int"/>, <see cref="long"/>, and <see cref="byte"/> arrays) through the Fuxion extensions framework.
/// All operations return <see cref="IResponse{T}"/> objects to enable proper error handling.
/// <para>
/// The class provides operations for:
/// <list type="bullet">
/// <item><description>Division with remainder calculation for <see cref="int"/> and <see cref="long"/> types</description></item>
/// <item><description>Optimized division by powers of two using bit shift operations</description></item>
/// <item><description>Conversion of byte arrays to numeric values for mathematical operations</description></item>
/// </list>
/// </para>
/// </remarks>
public static class MathExtensions
{
	extension(FuxionExtensions<long> me)
	{
		/// <summary>
		/// Provides math-related extension operations for this <see cref="long"/> value.
		/// </summary>
		public MathExtensions<long> Math
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(FuxionExtensions<long?> me)
	{
		/// <summary>
		/// Provides math-related extension operations for this <see cref="long"/> value.
		/// </summary>
		public MathExtensions<long?> Math
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(FuxionExtensions<byte[]?> me)
	{
		/// <summary>
		/// Provides math-related extension operations for this <see cref="byte"/> value.
		/// </summary>
		public MathExtensions<byte[]?> Math
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(FuxionExtensions<int> me)
	{
		/// <summary>
		/// Provides math-related extension operations for this <see cref="int"/> value.
		/// </summary>
		public MathExtensions<int> Math
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	extension(FuxionExtensions<int?> me)
	{
		/// <summary>
		/// Provides math-related extension operations for this <see cref="int"/> value.
		/// </summary>
		public MathExtensions<int?> Math
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension(MathExtensions<long> me)
	{
		/// <summary>
		/// Divides this value by <paramref name="divisor"/> and returns both quotient and remainder.
		/// </summary>
		/// <param name="divisor">The divisor used for the division.</param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="divisor"/> is not zero; otherwise, an error response indicating that the divisor is invalid.
		/// </returns>
		public IResponse<(long Quotient, long Remainder)> DivisionAndRemainder(long divisor)
		{
			if (divisor == 0)
				return ResponseExt.Get.InvalidData($"Argument '{nameof(divisor)}' cannot be zero.")
					.AsPayload<(long Quotient, long Remainder)>();

			var quotient = Math.DivRem(me.Value, divisor, out var remainder);
			return ResponseExt.Get.SuccessPayload((quotient, remainder));
		}

		/// <summary>
		/// Divides this value by <c>2^bitCount</c> and returns both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="bitCount"/> is in range; otherwise, an error response describing the problem.
		/// </returns>
		public IResponse<(long Quotient, long Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (bitCount is < 0 or > 62)
				return ResponseExt.Get.InvalidData($"Argument '{nameof(bitCount)}' must be between 0 and 62 (inclusive).")
					.AsPayload<(long Quotient, long Remainder)>();

			// 2^bitCount without going through double
			var divisor = 1L << bitCount;

			return me.DivisionAndRemainder(divisor);
		}
	}

	extension(MathExtensions<long?> me)
	{
		/// <summary>
		/// Divides the underlying <see cref="long"/> value by <paramref name="divisor"/> and returns both quotient and remainder.
		/// </summary>
		/// <param name="divisor">The divisor used for the division.</param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="divisor"/> is not zero;
		/// otherwise, an error response indicating that the source value is <c>null</c> or the divisor is invalid.
		/// </returns>
		public IResponse<(long Quotient, long Remainder)> DivisionAndRemainder(long divisor)
		{
			if (me.Value is null)
				return ResponseExt.Get.InvalidData("Source long is null.")
					.AsPayload<(long Quotient, long Remainder)>();

			return me.Value.Value.Fx.Math.DivisionAndRemainder(divisor);
		}

		/// <summary>
		/// Divides the underlying <see cref="long"/> value by <c>2^bitCount</c> and returns both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="bitCount"/> is in range;
		/// otherwise, an error response describing the problem.
		/// </returns>
		public IResponse<(long Quotient, long Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (me.Value is null)
				return ResponseExt.Get.InvalidData("Source long is null.")
					.AsPayload<(long Quotient, long Remainder)>();

			return me.Value.Value.Fx.Math.DivisionByPowerOfTwo(bitCount);
		}
	}
	extension(MathExtensions<int> me)
	{
		/// <summary>
		/// Divides this value by <paramref name="divisor"/> and returns both quotient and remainder.
		/// </summary>
		/// <param name="divisor">The divisor used for the division.</param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="divisor"/> is not zero; otherwise, an error response indicating that the divisor is invalid.
		/// </returns>
		public IResponse<(int Quotient, int Remainder)> DivisionAndRemainder(int divisor)
		{
			if (divisor == 0)
				return ResponseExt.Get.InvalidData($"Argument '{nameof(divisor)}' cannot be zero.")
					.AsPayload<(int Quotient, int Remainder)>();

			var quotient = Math.DivRem(me.Value, divisor, out var remainder);
			return ResponseExt.Get.SuccessPayload((quotient, remainder));
		}

		/// <summary>
		/// Divides this value by <c>2^bitCount</c> and returns both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="bitCount"/> is in range; otherwise, an error response describing the problem.
		/// </returns>
		public IResponse<(int Quotient, int Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (bitCount is < 0 or > 62)
				return ResponseExt.Get.InvalidData($"Argument '{nameof(bitCount)}' must be between 0 and 62 (inclusive).")
					.AsPayload<(int Quotient, int Remainder)>();

			// 2^bitCount without going through double
			var divisor = 1 << bitCount;

			return me.DivisionAndRemainder(divisor);
		}
	}
	extension(MathExtensions<int?> me)
	{
		/// <summary>
		/// Divides the underlying <see cref="int"/> value by <paramref name="divisor"/> and returns both quotient and remainder.
		/// </summary>
		/// <param name="divisor">The divisor used for the division.</param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="divisor"/> is not zero;
		/// otherwise, an error response indicating that the source value is <c>null</c> or the divisor is invalid.
		/// </returns>
		public IResponse<(int Quotient, int Remainder)> DivisionAndRemainder(int divisor)
		{
			if (me.Value is null)
				return ResponseExt.Get.InvalidData("Source int is null.")
					.AsPayload<(int Quotient, int Remainder)>();

			return me.Value.Value.Fx.Math.DivisionAndRemainder(divisor);
		}

		/// <summary>
		/// Divides the underlying <see cref="int"/> value by <c>2^bitCount</c> and returns both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="bitCount"/> is in range;
		/// otherwise, an error response describing the problem.
		/// </returns>
		public IResponse<(int Quotient, int Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (me.Value is null)
				return ResponseExt.Get.InvalidData("Source long is null.")
					.AsPayload<(int Quotient, int Remainder)>();

			return me.Value.Value.Fx.Math.DivisionByPowerOfTwo(bitCount);
		}
	}
	extension(MathExtensions<byte[]?> me)
	{
		/// <summary>
		/// Interprets the underlying byte array as a 64-bit integer and divides it by <c>2^bitCount</c>,
		/// returning both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <param name="isLittleEndian">
		/// Indicates whether the underlying byte array is encoded in little-endian (<c>true</c>) or big-endian (<c>false</c>) form.
		/// When <c>true</c>, the first byte is treated as the least significant byte.
		/// When <c>false</c>, the first byte is treated as the most significant byte.
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying byte array is not <c>null</c>, has a length between 1 and 8 bytes (inclusive),
		/// and <paramref name="bitCount"/> is in range; otherwise, an error response describing the problem.
		/// </returns>
		public IResponse<(long Quotient, long Remainder)> DivisionByPowerOfTwo(int bitCount, bool isLittleEndian = true)
		{
			if (me.Value is null)
				return ResponseExt.Get.InvalidData("Source byte array is null.")
					.AsPayload<(long Quotient, long Remainder)>();

			if (me.Value.Length is < 1 or > 8)
				return ResponseExt.Get.InvalidData("Length must be between 1 and 8 bytes.")
					.AsPayload<(long Quotient, long Remainder)>();

			if (bitCount is < 0 or > 62)
				return ResponseExt.Get.InvalidData($"Argument '{nameof(bitCount)}' must be between 0 and 62 (inclusive).")
					.AsPayload<(long Quotient, long Remainder)>();

			long value = 0;

			if (isLittleEndian)
			{
				// Little-endian: bytes[0] is the least significant byte.
				for (var i = 0; i < me.Value.Length; i++)
					value |= (long)me.Value[i] << (8 * i);
			}
			else
			{
				// Big-endian: bytes[0] is the most significant byte.
				// For lengths < 8, the value is left-padded with zeros on the most significant side.
				for (var i = 0; i < me.Value.Length; i++)
				{
					value <<= 8;
					value |= me.Value[i];
				}
			}

			return value.Fx.Math.DivisionByPowerOfTwo(bitCount);
		}
	}
}

/// <summary>
/// Generic wrapper class that provides mathematical extension methods for values of type <typeparamref name="T"/>.
/// This class is used internally by the Fuxion extensions framework to enable fluent API syntax for math operations.
/// </summary>
/// <typeparam name="T">
/// The type of the value being wrapped, typically <see cref="int"/>, <see cref="long"/>, 
/// or <see cref="byte"/> array for numeric operations.
/// </typeparam>
/// <remarks>
/// This class inherits from <see cref="Extensions{T}"/> and serves as a specialized container for mathematical operations.
/// Users typically access math functionality through extension methods rather than instantiating this class directly.
/// The primary operations provided are division operations that return both quotient and remainder,
/// with specialized optimizations for division by powers of two.
/// </remarks>
/// <example>
/// <code>
/// // Accessed through extension syntax
/// long number = 100;
/// var result = number.Fx.Math.DivisionAndRemainder(7);
/// Console.WriteLine($"Quotient: {result.Payload.Quotient}, Remainder: {result.Payload.Remainder}");
/// 
/// // Division by power of two
/// int value = 1024;
/// var powerResult = value.Fx.Math.DivisionByPowerOfTwo(3); // Divide by 2^3 = 8
/// 
/// // Byte array operations
/// byte[] bytes = new byte[] { 0xFF, 0x00, 0x00, 0x00 };
/// var byteResult = bytes.Fx.Math.DivisionByPowerOfTwo(4, isLittleEndian: true);
/// </code>
/// </example>
public class MathExtensions<T>(T value) : Extensions<T>(value);

