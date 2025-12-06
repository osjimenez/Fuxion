namespace Fuxion;

using System;
using System.Runtime.CompilerServices;

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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="divisor"/> is not zero; otherwise, an error response indicating that the divisor is invalid.
		/// </returns>
		public Response<(long Quotient, long Remainder)> DivisionAndRemainder(long divisor)
		{
			if (divisor == 0)
				return Response.Get.InvalidData($"Argument '{nameof(divisor)}' cannot be zero.")
					.AsPayload<(long Quotient, long Remainder)>();

			var quotient = System.Math.DivRem(me.Value, divisor, out var remainder);
			return (quotient, remainder);
		}

		/// <summary>
		/// Divides this value by <c>2^bitCount</c> and returns both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="bitCount"/> is in range; otherwise, an error response describing the problem.
		/// </returns>
		public Response<(long Quotient, long Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (bitCount is < 0 or > 62)
				return Response.Get.InvalidData($"Argument '{nameof(bitCount)}' must be between 0 and 62 (inclusive).")
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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="divisor"/> is not zero;
		/// otherwise, an error response indicating that the source value is <c>null</c> or the divisor is invalid.
		/// </returns>
		public Response<(long Quotient, long Remainder)> DivisionAndRemainder(long divisor)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source long is null.")
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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="bitCount"/> is in range;
		/// otherwise, an error response describing the problem.
		/// </returns>
		public Response<(long Quotient, long Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source long is null.")
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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="divisor"/> is not zero; otherwise, an error response indicating that the divisor is invalid.
		/// </returns>
		public Response<(int Quotient, int Remainder)> DivisionAndRemainder(int divisor)
		{
			if (divisor == 0)
				return Response.Get.InvalidData($"Argument '{nameof(divisor)}' cannot be zero.")
					.AsPayload<(int Quotient, int Remainder)>();

			var quotient = System.Math.DivRem(me.Value, divisor, out var remainder);
			return (quotient, remainder);
		}

		/// <summary>
		/// Divides this value by <c>2^bitCount</c> and returns both quotient and remainder.
		/// </summary>
		/// <param name="bitCount">
		/// The number of bits of the power of two used as divisor. Must be between 0 and 62 (inclusive).
		/// </param>
		/// <returns>
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when <paramref name="bitCount"/> is in range; otherwise, an error response describing the problem.
		/// </returns>
		public Response<(int Quotient, int Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (bitCount is < 0 or > 62)
				return Response.Get.InvalidData($"Argument '{nameof(bitCount)}' must be between 0 and 62 (inclusive).")
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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="divisor"/> is not zero;
		/// otherwise, an error response indicating that the source value is <c>null</c> or the divisor is invalid.
		/// </returns>
		public Response<(int Quotient, int Remainder)> DivisionAndRemainder(int divisor)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source int is null.")
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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="int"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying value is not <c>null</c> and <paramref name="bitCount"/> is in range;
		/// otherwise, an error response describing the problem.
		/// </returns>
		public Response<(int Quotient, int Remainder)> DivisionByPowerOfTwo(int bitCount)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source long is null.")
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
		/// A <see cref="Response{T}"/> whose payload is a tuple with <see cref="long"/> <c>Quotient</c> and <c>Remainder</c>
		/// when the underlying byte array is not <c>null</c>, has a length between 1 and 8 bytes (inclusive),
		/// and <paramref name="bitCount"/> is in range; otherwise, an error response describing the problem.
		/// </returns>
		public Response<(long Quotient, long Remainder)> DivisionByPowerOfTwo(int bitCount, bool isLittleEndian = true)
		{
			if (me.Value is null)
				return Response.Get.InvalidData("Source byte array is null.")
					.AsPayload<(long Quotient, long Remainder)>();

			if (me.Value.Length is < 1 or > 8)
				return Response.Get.InvalidData("Length must be between 1 and 8 bytes.")
					.AsPayload<(long Quotient, long Remainder)>();

			if (bitCount is < 0 or > 62)
				return Response.Get.InvalidData($"Argument '{nameof(bitCount)}' must be between 0 and 62 (inclusive).")
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

public class MathExtensions<T>(T value) : Extensions<T>(value);

