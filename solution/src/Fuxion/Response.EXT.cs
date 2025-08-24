namespace Fuxion;

public static class ResponseExtensions
{
	extension(IResponse me)
	{
		public IResponse<T> AsPayload<T>()
		{
			if (me is IResponse<T> r) return r;
			if (me.IsSuccess) throw new InvalidOperationException("Can't convert a success response to a different payload type.");
			return new Response<T>(me.IsSuccess, default!, me.Message, me.ErrorType, me.Exception)
			{
				Extensions = me.Extensions
			};
		}
	}
	extension(Response me)
	{
		public Response<T> AsPayload<T>()
		{
			if (me is Response<T> r) return r;
			if (me.IsSuccess) throw new InvalidOperationException("Can't convert a success response to a different payload type.");
			return new Response<T>(me.IsSuccess, default!, me.Message, me.ErrorType, me.Exception)
			{
				Extensions = me.Extensions
			};
		}
	}

	extension(IEnumerable<IResponse> me)
	{
		public Response<IEnumerable<IResponse>> CombineResponses(string? message = null)
		{
			if (me.Any(r => r.IsError)) return new(false, me.Where(r => r.IsError), message);
			return new(true, me, message);
		}
	}

	extension(Response me)
	{
		public static Response Success(IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new(true)
			{
				Extensions = extensions?.ToDictionary(t => t.Property, t => t.Value) ?? []
			};
		public static Response SuccessMessage(string message, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new(true, message)
			{
				Extensions = extensions?.ToDictionary(t => t.Property, t => t.Value) ?? []
			};
		public static Response<TPayload> SuccessPayload<TPayload>(TPayload payload, string? message = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new(true, payload, message)
			{
				Extensions = extensions?.ToDictionary(t => t.Property, t => t.Value) ?? []
			};
		public static Response ErrorMessage(string message, object? type = null, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new(false, message, type, exception)
			{
				Extensions = extensions?.ToDictionary(t => t.Property, t => t.Value) ?? []
			};
		public static Response<TPayload> ErrorPayload<TPayload>(
			TPayload payload,
			string? message = null,
			object? type = null,
			Exception? exception = null,
			IEnumerable<(string Property, object? Value)>? extensions = null)
			=> new(false, payload, message, type, exception)
			{
				Extensions = extensions?.ToDictionary(t => t.Property, t => t.Value) ?? []
			};
		public static Response Exception(Exception exception, string? message = null) => new(false, message ?? $"{exception.GetType().Name}: {exception.Message}", exception: exception);

		// Not found
		public static Response NotFound(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.NotFound, exception, extensions);
		public static Response<TPayload> NotFound<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.NotFound, exception, extensions);

		// Permission denied
		public static Response PermissionDenied(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.PermissionDenied, exception, extensions);
		public static Response<TPayload> PermissionDenied<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.PermissionDenied, exception, extensions);

		// Invalid data
		public static Response InvalidData(string message, Exception? exception = null, List<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.InvalidData, exception, extensions);
		public static Response<TPayload> InvalidData<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.InvalidData, exception, extensions);

		// Conflict
		public static Response Conflict(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.Conflict, exception, extensions);
		public static Response<TPayload> Conflict<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.Conflict, exception, extensions);

		// Critical
		public static Response Critical(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.Critical, exception, extensions);
		public static Response<TPayload> Critical<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.Critical, exception, extensions);

		// Not supported
		public static Response NotSupported(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.NotSupported, exception, extensions);
		public static Response<TPayload> NotSupported<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.NotSupported, exception, extensions);

		// Unavailable
		public static Response Unavailable(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.Unavailable, exception, extensions);
		public static Response<TPayload> Unavailable<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.Unavailable, exception, extensions);

		// Timeout
		public static Response Timeout(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, ErrorType.Timeout, exception, extensions);
		public static Response<TPayload> Timeout<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, ErrorType.Timeout, exception, extensions);
	}

	extension(IResponse me)
	{
		public bool IsErrorType(object type) => me.ErrorType?.Equals(type) == true;
		public bool IsNotFound() => me.IsErrorType(ErrorType.NotFound);
		public bool IsPermissionDenied() => me.IsErrorType(ErrorType.PermissionDenied);
		public bool IsInvalidData() => me.IsErrorType(ErrorType.InvalidData);
		public bool IsConflict() => me.IsErrorType(ErrorType.Conflict);
		public bool IsCritical() => me.IsErrorType(ErrorType.Critical);
		public bool IsNotSupported() => me.IsErrorType(ErrorType.NotSupported);
		public bool IsUnavailable() => me.IsErrorType(ErrorType.Unavailable);
		public bool IsTimeout() => me.IsErrorType(ErrorType.Timeout);
	}

#if STANDARD_OR_OLD_FRAMEWORKS
	public class ResponseGetExtensions;

	extension(ResponseGetExtensions me)
	{
		public Response Success(IEnumerable<(string Property, object? Value)>? extensions = null) => Response.Success(extensions);
		public Response SuccessMessage(string message, IEnumerable<(string Property, object? Value)>? extensions = null) => Response.SuccessMessage(message, extensions);
		public Response<TPayload> SuccessPayload<TPayload>(TPayload payload, string? message = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.SuccessPayload(payload, message, extensions);
		public Response ErrorMessage(string message, object? type = null, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorMessage(message, type, exception, extensions);
		public Response<TPayload> ErrorPayload<TPayload>(
			TPayload payload,
			string? message = null,
			object? type = null,
			Exception? exception = null,
			IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.ErrorPayload(payload, message, type, exception, extensions);
		public Response Exception(Exception exception, string? message = null) => Response.Exception(exception, message);
		public Response NotFound(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null) => Response.NotFound(message, exception, extensions);
		public Response<TPayload> NotFound<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.NotFound(message, payload, exception, extensions);
		public Response PermissionDenied(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.PermissionDenied(message, exception, extensions);
		public Response<TPayload> PermissionDenied<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.PermissionDenied(message, payload, exception, extensions);
		public Response InvalidData(string message, Exception? exception = null, List<(string Property, object? Value)>? extensions = null) => Response.InvalidData(message, exception, extensions);
		public Response<TPayload> InvalidData<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.InvalidData(message, payload, exception, extensions);
		public Response Conflict(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null) => Response.Conflict(message, exception, extensions);
		public Response<TPayload> Conflict<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Conflict(message, payload, exception, extensions);
		public Response Critical(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null) => Response.Critical(message, exception, extensions);
		public Response<TPayload> Critical<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Critical(message, payload, exception, extensions);
		public Response NotSupported(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.NotSupported(message, exception, extensions);
		public Response<TPayload> NotSupported<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.NotSupported(message, payload, exception, extensions);
		public Response Unavailable(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Unavailable(message, exception, extensions);
		public Response<TPayload> Unavailable<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Unavailable(message, payload, exception, extensions);
		public Response Timeout(string message, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null) => Response.Timeout(message, exception, extensions);
		public Response<TPayload> Timeout<TPayload>(string message, TPayload payload, Exception? exception = null, IEnumerable<(string Property, object? Value)>? extensions = null)
			=> Response.Timeout(message, payload, exception, extensions);
	}
#endif
}