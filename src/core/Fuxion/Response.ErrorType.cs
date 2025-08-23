using System.Text.Json.Serialization;

namespace Fuxion;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
	NotFound,
	PermissionDenied,
	InvalidData,
	Conflict,
	Critical,
	NotSupported,
	Unavailable,
	Timeout
}