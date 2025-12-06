using System.Text.Json.Serialization;

namespace Fuxion;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
	NotFound,
	PermissionDenied, // Authentication vs Authorization
	InvalidData, // Validation
	Conflict,
	Critical,
	NotSupported,
	Unavailable,
	Timeout
}