using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ErrorExtensions
{
	extension(Error me)
	{
		public Error Wrap(
			string? message = null,
			object? type = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			=> new(
				message ?? me.Message,
				type ?? me.Type,
				payload,
				exception,
				extensions,
				[me],
				new(callerMemberName, callerFilePath, callerLineNumber));

		public static Error Aggregate(params Error[] innerErrors)
		{
			var message = "One or more errors occurred.";
			if (innerErrors is not null && innerErrors.Length > 0)
				message += string.Concat(innerErrors.Where(e => e.Message.IsNeitherNullNorWhiteSpace()).Select(error => $" ({error.Message})"));

			object? type = HttpStatusCode.InternalServerError;

			if (innerErrors is { Length: > 0 })
			{
				var current = innerErrors[0].Type;
				type = current ?? HttpStatusCode.InternalServerError;
				for (var i = 1; i < innerErrors.Length; i++)
					if (!Equals(current, innerErrors[i].Type))
					{
						type = HttpStatusCode.InternalServerError;
						break;
					}
			}
			return new(
					  message: message,
					  type: type,
					  innerErrors: innerErrors is null || innerErrors.Length == 0 ? null : innerErrors,
					  source: ErrorSource.FromStackTrace());
		}

		public static Error Custom(
			string? message = null,
			object? type = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
				=> message is null && type is null && payload is null && exception is null && extensions is null && innerErrors is null
					? throw new ArgumentException("At least one of the parameters must be provided.")
					: new(message, type, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));

		public static Error NotFound(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.NotFound, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsNotFound => me.Type is HttpStatusCode.NotFound;

		public static Error Forbidden(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.Forbidden, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsForbidden => me.Type is HttpStatusCode.Forbidden;

		public static Error Unauthorized(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.Unauthorized, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsUnauthorized => me.Type is HttpStatusCode.Unauthorized;

		public static Error BadRequest(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.BadRequest, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsBadRequest => me.Type is HttpStatusCode.BadRequest;

		public static Error Conflict(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.Conflict, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsConflict => me.Type is HttpStatusCode.Conflict;

		public static Error InternalServerError(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.InternalServerError, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsInternalServerError => me.Type is HttpStatusCode.InternalServerError;

		public static Error NotImplemented(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.NotImplemented, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsNotImplemented => me.Type is HttpStatusCode.NotImplemented;

		public static Error ServiceUnavailable(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.ServiceUnavailable, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsServiceUnavailable => me.Type is HttpStatusCode.ServiceUnavailable;

		public static Error RequestTimeout(
			string? message = null,
			object? payload = null,
			Exception? exception = null,
			ExtensionsDictionary? extensions = null,
			Error[]? innerErrors = null,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
			 => new(message, HttpStatusCode.RequestTimeout, payload, exception, extensions, innerErrors, new(callerMemberName, callerFilePath, callerLineNumber));
		public bool IsRequestTimeout => me.Type is HttpStatusCode.RequestTimeout;
	}

}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
