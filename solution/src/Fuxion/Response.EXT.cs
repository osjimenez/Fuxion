using System;
using Fuxion.Reflection;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ResponseExtensions
{
	extension<TSuccess>(Response<TSuccess> me) where TSuccess : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<Error, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out Error error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
	extension<TSuccess, TError>(Response<TSuccess, TError> me) where TSuccess : notnull where TError : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<TError, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out TError? error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
	extension<TSuccess>(ResponseMaybe<TSuccess> me) where TSuccess : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<Error, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out Error error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
	extension<TSuccess, TError>(ResponseMaybe<TSuccess, TError> me) where TSuccess : notnull where TError : notnull
	{
		public TSuccess SuccessOrThrow()
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			throw new ResponseSuccessException($"The response does not contain a success value of type '{typeof(TSuccess).GetSignature()}'");
		}
		public TSuccess SuccessOrFallback(Func<TError, TSuccess> fallback)
		{
			if (me.TryGetValue(out TSuccess? value))
				return value;
			if (me.TryGetValue(out TError? error))
				return fallback(error);
			throw new InvalidOperationException("The response does not contain a success value or an error value.");
		}
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
