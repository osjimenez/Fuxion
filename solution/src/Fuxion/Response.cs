using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

static class ResponseConstants
{
	public const string PayloadPropertyName = "Payload";
	public const string ErrorPropertyName = "Error";
	public static readonly HashSet<string> ResponseExtensionsReservedKeys = new([nameof(Response<>.IsSuccess), PayloadPropertyName, ErrorPropertyName], StringComparer.OrdinalIgnoreCase);
	public static readonly HashSet<string> ResponseMaybeExtensionsReservedKeys = new([.. ResponseExtensionsReservedKeys, nameof(ResponseMaybe<>.IsNone)], StringComparer.OrdinalIgnoreCase);

	public static ExtensionsDictionary<IResponse> EnsureResponseReservedKeys(ExtensionsDictionary? extensions)
		=> ExtensionsDictionary.EnsureReservedKeys<IResponse>(extensions, ResponseExtensionsReservedKeys);

	public static ExtensionsDictionary<IResponse> EnsureResponseMaybeReservedKeys(ExtensionsDictionary? extensions)
		=> ExtensionsDictionary.EnsureReservedKeys<IResponse>(extensions, ResponseMaybeExtensionsReservedKeys);

	public static InvalidOperationException ConversionNotAllowed(Type target, string state)
		=> new($"Explicit conversion between this response and {target.GetSignature()} is not allowed because this response is {state}");
}

[Union]
[JsonConverter(typeof(ResponseOfTSuccessJsonConverterFactory))]
public readonly struct Response<TSuccess> : IResponse
	where TSuccess : notnull
{
	private const byte UnsetKind = 0;
	private const byte SuccessKind = 1;
	private const byte ErrorKind = 2;

	private readonly byte _kind;
	private readonly TSuccess? _success;
	private readonly Error? _error;

	static Response()
	{
		if (typeof(TSuccess) == typeof(Error))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(Error)}'.");

		if (typeof(TSuccess) == typeof(None))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(None)}'. Consider using Response<Unit> type instead.");
	}

	[MemberNotNullWhen(true, nameof(_success))]
	public bool IsSuccess => _kind == SuccessKind;

	[MemberNotNullWhen(true, nameof(_error))]
	public bool IsError => _kind == ErrorKind;

	public Response(TSuccess value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value), "Success value cannot be null.");

		_kind = SuccessKind;
		_success = value;
		_error = default;
	}

	public Response(Error value)
	{
		_kind = ErrorKind;
		_success = default;
		_error = value;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool HasValue => _kind != UnsetKind;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object? Value => _kind switch
	{
		SuccessKind => _success,
		ErrorKind => _error,
		_ => null
	};

	public ExtensionsDictionary<IResponse> Extensions
	{
		get => field ?? [with(ResponseConstants.ResponseExtensionsReservedKeys)];
		init => field = ResponseConstants.EnsureResponseReservedKeys(value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue([NotNullWhen(true)] out TSuccess? value)
	{
		if (IsSuccess)
		{
			value = _success;
			return true;
		}

		value = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue(out Error value)
	{
		if (IsError)
		{
			value = _error.Value;
			return true;
		}

		value = default;
		return false;
	}

	public static implicit operator Response<TSuccess>(TSuccess value)
		=> new(value);

	public static implicit operator Response<TSuccess>(Error value)
		=> new(value);

	public static explicit operator TSuccess(Response<TSuccess> value)
		=> value.IsSuccess
			? value._success
			: throw ResponseConstants.ConversionNotAllowed(typeof(TSuccess), "error");

	public static explicit operator Error(Response<TSuccess> value)
		=> value.IsError
			? value._error.Value
			: throw ResponseConstants.ConversionNotAllowed(typeof(Error), "success");
}

[Union]
[JsonConverter(typeof(ResponseOfTSuccessAndTErrorJsonConverterFactory))]
public readonly struct Response<TSuccess, TError> : IResponse
	where TSuccess : notnull
	where TError : notnull
{
	private const byte UnsetKind = 0;
	private const byte SuccessKind = 1;
	private const byte ErrorKind = 2;

	private readonly byte _kind;
	private readonly TSuccess? _success;
	private readonly TError? _error;

	static Response()
	{
		if (typeof(TSuccess) == typeof(TError))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} and {nameof(TError)} cannot be the same type ('{typeof(TSuccess).GetSignature()}').");

		if (typeof(TSuccess) == typeof(None))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(None)}'. Consider using {typeof(Response<Unit, TError>).GetSignature()} type instead.");
		if (typeof(TSuccess) == typeof(Error))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(Error)}'.");

		if (typeof(TError) == typeof(None))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TError)} cannot be '{nameof(None)}'. Consider using {typeof(ResponseMaybe<TSuccess>).GetSignature()} type instead.");
		if (typeof(TError) == typeof(Error))
			throw new ResponseInitializationException($"The {typeof(Response<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TError)} cannot be '{nameof(Error)}'. Consider using {typeof(Response<TSuccess>).GetSignature()} type instead.");
	}

	[MemberNotNullWhen(true, nameof(_success))]
	public bool IsSuccess => _kind == SuccessKind;

	[MemberNotNullWhen(true, nameof(_error))]
	public bool IsError => _kind == ErrorKind;

	public Response(TSuccess value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value), "Success value cannot be null.");

		_kind = SuccessKind;
		_success = value;
		_error = default;
	}

	public Response(TError value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value), "Error value cannot be null.");

		_kind = ErrorKind;
		_success = default;
		_error = value;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool HasValue => _kind != UnsetKind;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object? Value => _kind switch
	{
		SuccessKind => _success,
		ErrorKind => _error,
		_ => null
	};

	public ExtensionsDictionary<IResponse> Extensions
	{
		get => field ?? [with(ResponseConstants.ResponseExtensionsReservedKeys)];
		init => field = ResponseConstants.EnsureResponseReservedKeys(value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue([NotNullWhen(true)] out TSuccess? value)
	{
		if (IsSuccess)
		{
			value = _success;
			return true;
		}

		value = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue([NotNullWhen(true)] out TError? value)
	{
		if (IsError)
		{
			value = _error;
			return true;
		}

		value = default;
		return false;
	}

	public static implicit operator Response<TSuccess, TError>(TSuccess value)
		=> new(value);

	public static implicit operator Response<TSuccess, TError>(TError value)
		=> new(value);

	public static explicit operator TSuccess(Response<TSuccess, TError> value)
		=> value.IsSuccess
			? value._success
			: throw ResponseConstants.ConversionNotAllowed(typeof(TSuccess), "error");

	public static explicit operator TError(Response<TSuccess, TError> value)
		=> value.IsError
			? value._error
			: throw ResponseConstants.ConversionNotAllowed(typeof(TError), "success");
}

[Union]
[JsonConverter(typeof(ResponseMaybeOfTSuccessJsonConverterFactory))]
public readonly struct ResponseMaybe<TSuccess> : IResponseMaybe
	where TSuccess : notnull
{
	private const byte UnsetKind = 0;
	private const byte SuccessKind = 1;
	private const byte NoneKind = 2;
	private const byte ErrorKind = 3;

	private readonly byte _kind;
	private readonly TSuccess? _success;
	private readonly Error? _error;

	static ResponseMaybe()
	{
		if (typeof(TSuccess) == typeof(Error))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(Error)}'.");
		if (typeof(TSuccess) == typeof(None))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(None)}'. Consider using ResponseMaybe<Unit> type instead.");
	}

	public bool IsSuccess => _kind is SuccessKind or NoneKind;

	[MemberNotNullWhen(true, nameof(_error))]
	public bool IsError => _kind == ErrorKind;

	public bool IsNone => _kind == NoneKind;

	public ResponseMaybe(TSuccess value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value), "Success value cannot be null.");

		_kind = SuccessKind;
		_success = value;
		_error = default;
	}

	public ResponseMaybe(Error value)
	{
		_kind = ErrorKind;
		_success = default;
		_error = value;
	}
	public ResponseMaybe(None value)
	{
		_kind = NoneKind;
		_success = default;
		_error = default;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool HasValue => _kind != UnsetKind;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object? Value => _kind switch
	{
		SuccessKind => _success,
		ErrorKind => _error,
		NoneKind => None.Value,
		_ => null
	};

	public ExtensionsDictionary<IResponse> Extensions
	{
		get => field ?? [with(ResponseConstants.ResponseMaybeExtensionsReservedKeys)];
		init => field = ResponseConstants.EnsureResponseMaybeReservedKeys(value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue([NotNullWhen(true)] out TSuccess? value)
	{
		if (IsSuccess && !IsNone)
		{
			value = _success!;
			return true;
		}

		value = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue(out Error value)
	{
		if (IsError)
		{
			value = _error.Value;
			return true;
		}

		value = default;
		return false;
	}
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue(out None value)
	{
		if (IsNone)
		{
			value = None.Value;
			return true;
		}

		value = default;
		return false;
	}

	public static implicit operator ResponseMaybe<TSuccess>(TSuccess value)
		=> new(value);

	public static implicit operator ResponseMaybe<TSuccess>(Error value)
		=> new(value);

	public static implicit operator ResponseMaybe<TSuccess>(None value)
		=> new(value);

	public static explicit operator TSuccess(ResponseMaybe<TSuccess> value)
		=> value.IsError
			? throw ResponseConstants.ConversionNotAllowed(typeof(TSuccess), "error")
			: value.IsNone
				? throw ResponseConstants.ConversionNotAllowed(typeof(TSuccess), "none")
				: value._success!;

	public static explicit operator Error(ResponseMaybe<TSuccess> value)
		=> value.IsNone
			? throw ResponseConstants.ConversionNotAllowed(typeof(Error), "none")
			: value.IsSuccess
				? throw ResponseConstants.ConversionNotAllowed(typeof(Error), "success")
				: value._error!.Value;

	public static explicit operator None(ResponseMaybe<TSuccess> value)
		=> value.IsError
			? throw ResponseConstants.ConversionNotAllowed(typeof(None), "error")
			: value.IsSuccess && !value.IsNone
				? throw ResponseConstants.ConversionNotAllowed(typeof(None), "success")
				: None.Value;
}

[Union]
[JsonConverter(typeof(ResponseMaybeOfTSuccessAndTErrorJsonConverterFactory))]
public readonly struct ResponseMaybe<TSuccess, TError> : IResponseMaybe
	where TSuccess : notnull
	where TError : notnull
{
	private const byte UnsetKind = 0;
	private const byte SuccessKind = 1;
	private const byte NoneKind = 2;
	private const byte ErrorKind = 3;

	private readonly byte _kind;
	private readonly TSuccess? _success;
	private readonly TError? _error;

	static ResponseMaybe()
	{
		if (typeof(TSuccess) == typeof(TError))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} and {nameof(TError)} cannot be the same type ('{typeof(TSuccess).GetSignature()}').");

		if (typeof(TSuccess) == typeof(None))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(None)}'. Consider using {typeof(ResponseMaybe<Unit, TError>).GetSignature()} type instead.");
		if (typeof(TSuccess) == typeof(Error))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TSuccess)} cannot be '{nameof(Error)}'.");

		if (typeof(TError) == typeof(None))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TError)} cannot be '{nameof(None)}'. Consider using {typeof(ResponseMaybe<TSuccess>).GetSignature()} type instead.");
		if (typeof(TError) == typeof(Error))
			throw new ResponseInitializationException($"The {typeof(ResponseMaybe<TSuccess, TError>).GetSignature()} type arguments are invalid: {nameof(TError)} cannot be '{nameof(Error)}'. Consider using {typeof(ResponseMaybe<TSuccess>).GetSignature()} type instead.");
	}

	public bool IsSuccess => _kind is SuccessKind or NoneKind;

	[MemberNotNullWhen(true, nameof(_error))]
	public bool IsError => _kind == ErrorKind;

	public bool IsNone => _kind == NoneKind;

	public ResponseMaybe(TSuccess value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value), "Success value cannot be null.");

		_kind = SuccessKind;
		_success = value;
		_error = default;
	}

	public ResponseMaybe(TError value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value), "Error value cannot be null.");

		_kind = ErrorKind;
		_success = default;
		_error = value;
	}
	public ResponseMaybe(None value)
	{
		_kind = NoneKind;
		_success = default;
		_error = default;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool HasValue => _kind != UnsetKind;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object? Value => _kind switch
	{
		SuccessKind => _success,
		ErrorKind => _error,
		NoneKind => None.Value,
		_ => null
	};

	public ExtensionsDictionary<IResponse> Extensions
	{
		get => field ?? [with(ResponseConstants.ResponseMaybeExtensionsReservedKeys)];
		init => field = ResponseConstants.EnsureResponseMaybeReservedKeys(value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue([NotNullWhen(true)] out TSuccess? value)
	{
		if (IsSuccess && !IsNone)
		{
			value = _success!;
			return true;
		}

		value = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue([NotNullWhen(true)] out TError? value)
	{
		if (IsError)
		{
			value = _error;
			return true;
		}

		value = default;
		return false;
	}
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool TryGetValue(out None value)
	{
		if (IsNone)
		{
			value = None.Value;
			return true;
		}

		value = default;
		return false;
	}

	public static implicit operator ResponseMaybe<TSuccess, TError>(TSuccess value)
		=> new(value);

	public static implicit operator ResponseMaybe<TSuccess, TError>(TError value)
		=> new(value);

	public static implicit operator ResponseMaybe<TSuccess, TError>(None value)
		=> new(value);

	public static explicit operator TSuccess(ResponseMaybe<TSuccess, TError> value)
		=> value.IsError
			? throw ResponseConstants.ConversionNotAllowed(typeof(TSuccess), "error")
			: value.IsNone
				? throw ResponseConstants.ConversionNotAllowed(typeof(TSuccess), "none")
				: value._success!;

	public static explicit operator TError(ResponseMaybe<TSuccess, TError> value)
		=> value.IsNone
			? throw ResponseConstants.ConversionNotAllowed(typeof(TError), "none")
			: value.IsSuccess
				? throw ResponseConstants.ConversionNotAllowed(typeof(TError), "success")
				: value._error!;

	public static explicit operator None(ResponseMaybe<TSuccess, TError> value)
		=> value.IsError
			? throw ResponseConstants.ConversionNotAllowed(typeof(None), "error")
			: value.IsSuccess && !value.IsNone
				? throw ResponseConstants.ConversionNotAllowed(typeof(None), "success")
				: None.Value;
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
