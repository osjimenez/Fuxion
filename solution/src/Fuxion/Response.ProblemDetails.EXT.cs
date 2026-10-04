using Fuxion.Collections.Generic;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using Fuxion.Text.Json;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ProblemDetailsExtensionKeys
{
	public const string ErrorType = "ErrorType";
	public const string ErrorPayload = "ErrorPayload";
	public const string ErrorException = "ErrorException";
	public const string ErrorInnerErrors = "ErrorInnerErrors";
	public const string ErrorSource = "ErrorSource";
	public const string ProblemType = "ProblemType";
	public const string ProblemTitle = "ProblemTitle";
	public const string ProblemInstance = "ProblemInstance";
}

public static class ResponseProblemDetailsExtensions
{
	extension(Error error)
	{
		public ResponseProblemDetails ToProblemDetails(JsonSerializerOptions? jsonOptions = null)
		{
			var status = error.Type is HttpStatusCode httpStatus
				? (int)httpStatus
				: (int)HttpStatusCode.InternalServerError;

			var problem = new ResponseProblemDetails
			{
				Status = status,
				Title = GetTitle(status),
				Detail = error.Message
			};

			if (error.Type is not null)
				problem.Extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ErrorType)] = error.Type;
			if (error.Payload is not null)
				problem.Extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ErrorPayload)] = error.Payload;
			if (error.Exception is not null)
				problem.Extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ErrorException)] = error.Exception is RemoteException remote
					? remote.ExceptionJson
					: error.Exception.ToExceptionJson(jsonOptions);
			if (error.InnerErrors is not null)
				problem.Extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ErrorInnerErrors)] = error.InnerErrors;
			if (error.Source is not null)
				problem.Extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ErrorSource)] = error.Source;

			foreach (var extension in error.Extensions)
				problem.Extensions[extension.Key] = extension.Value;

			return problem;
		}
	}

	extension(ResponseProblemDetails? problem)
	{
		public Error ToError(JsonSerializerOptions? jsonOptions = null)
		{
			if (problem is null)
				return Error.InternalServerError("The error response body could not be deserialized.");

			var extensions = new ExtensionsDictionary(ErrorConstants.ErrorExtensionsReservedKeys);
			foreach (var extension in problem.Extensions)
				if (!IsReservedProblemExtension(extension.Key))
					extensions[extension.Key] = extension.Value;

			if (problem.Type is not null)
				extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ProblemType)] = problem.Type;
			if (problem.Title is not null)
				extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ProblemTitle)] = problem.Title;
			if (problem.Instance is not null)
				extensions[jsonOptions.ApplyNamingPolicy(ProblemDetailsExtensionKeys.ProblemInstance)] = problem.Instance;

			var type = TryGetExtension(problem, ProblemDetailsExtensionKeys.ErrorType, jsonOptions, out var typeObj)
				? ConvertErrorType(typeObj, jsonOptions)
				: problem.Status is int status
					? (HttpStatusCode)status
					: null;

			object? payload = null;
			if (TryGetExtension(problem, ProblemDetailsExtensionKeys.ErrorPayload, jsonOptions, out var payloadObj))
				payload = payloadObj;

			Exception? exception = null;
			if (TryGetExtension(problem, ProblemDetailsExtensionKeys.ErrorException, jsonOptions, out var exceptionObj))
				exception = ConvertException(exceptionObj, jsonOptions);

			Error[]? innerErrors = null;
			if (TryGetExtension(problem, ProblemDetailsExtensionKeys.ErrorInnerErrors, jsonOptions, out var innerErrorsObj))
				innerErrors = ConvertValue<Error[]>(innerErrorsObj, jsonOptions);

			ErrorSource? source = null;
			if (TryGetExtension(problem, ProblemDetailsExtensionKeys.ErrorSource, jsonOptions, out var sourceObj))
				source = ConvertValue<ErrorSource>(sourceObj, jsonOptions);

			return new Error(problem.Detail, type, payload, exception, extensions, innerErrors, source);
		}
	}

	static bool TryGetExtension(ResponseProblemDetails problem, string key, JsonSerializerOptions? jsonOptions, out object? value)
	{
		var extensionName = jsonOptions.ApplyNamingPolicy(key);
		if (problem.Extensions.TryGetValue(extensionName, out value) || problem.Extensions.TryGetValue(key, out value))
			return true;

		foreach (var extension in problem.Extensions)
			if (string.Equals(extension.Key, extensionName, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(extension.Key, key, StringComparison.OrdinalIgnoreCase))
			{
				value = extension.Value;
				return true;
			}

		value = null;
		return false;
	}

	static bool IsReservedProblemExtension(string key)
		=> IsExtensionName(key, ProblemDetailsExtensionKeys.ErrorType)
			|| IsExtensionName(key, ProblemDetailsExtensionKeys.ErrorPayload)
			|| IsExtensionName(key, ProblemDetailsExtensionKeys.ErrorException)
			|| IsExtensionName(key, ProblemDetailsExtensionKeys.ErrorInnerErrors)
			|| IsExtensionName(key, ProblemDetailsExtensionKeys.ErrorSource);

	static bool IsExtensionName(string key, string name)
		=> string.Equals(key, name, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(key, JsonNamingPolicy.CamelCase.ConvertName(name), StringComparison.OrdinalIgnoreCase);

	static object? ConvertErrorType(object? value, JsonSerializerOptions? jsonOptions)
	{
		if (value is null)
			return null;
		if (value is HttpStatusCode)
			return value;
		if (value is JsonElement json)
		{
			if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var code))
				return (HttpStatusCode)code;
			if (json.ValueKind == JsonValueKind.String)
				return json.GetString();

			var options = jsonOptions ?? new JsonSerializerOptions();
			foreach (var converter in ErrorJsonConverter.UseErrorTypeConverters())
				if (converter.TryRead(json, out var convertedType, options))
					return convertedType;

			return json.Deserialize<object?>(options);
		}

		return value;
	}

	static Exception? ConvertException(object? value, JsonSerializerOptions? jsonOptions)
	{
		if (value is null)
			return null;
		if (value is Exception exception)
			return exception;
		if (value is ExceptionJson exceptionJson)
			return exceptionJson.ToRemoteException();
		if (value is JsonElement json)
			return json.Deserialize<ExceptionJson?>(jsonOptions)?.ToRemoteException();
		return null;
	}

	static T? ConvertValue<T>(object? value, JsonSerializerOptions? jsonOptions)
	{
		if (value is null)
			return default;
		if (value is T typed)
			return typed;
		if (value is JsonElement json)
			return json.Deserialize<T>(jsonOptions);
		return default;
	}

	static string GetTitle(int status)
	{
		var statusName = Enum.IsDefined(typeof(HttpStatusCode), status)
			? ((HttpStatusCode)status).ToString()
			: "Error";

		var builder = new StringBuilder(statusName.Length + 8);
		for (var i = 0; i < statusName.Length; i++)
		{
			var current = statusName[i];
			if (i > 0 && char.IsUpper(current) && !char.IsUpper(statusName[i - 1]))
				builder.Append(' ');
			builder.Append(i == 0 ? char.ToUpperInvariant(current) : char.ToLowerInvariant(current));
		}
		return builder.ToString();
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
