namespace Test.Responses.Shared;

/// <summary>The logical routes of the shared wire families: every <see cref="IWireHost"/> maps each of them to its own URL.</summary>
public static class Routes
{
	public const string ResponseUnit = "response/unit";
	public const string ResponseNone = "response/none";
	public const string ResponsePayload = "response/payload";
	public const string ResponseNamingPayload = "response/naming-payload";
	public const string ResponseErrorMessage = "response/error-message";
	public const string ResponseErrorType = "response/error-type";
	public const string ResponseTypedError = "response/typed-error";
	public const string ResponseTypedErrorForeign = "response/typed-error-foreign";

	public const string BinaryFile = "binary/file";
	public const string BinaryStream = "binary/stream";
	public const string BinaryNone = "binary/none";
	public const string BinaryError = "binary/error";
	public const string BinaryChunked = "binary/chunked";
	public const string BinarySizedStream = "binary/sized-stream";
	public const string BinaryRangeSource = "binary/range-source";

	public const string NamingEcho = "naming/echo";
	public const string NamingMalformed = "naming/malformed";
	public const string NamingDictionary = "naming/dictionary";

	public const string UndefinablePartial = "undefinable/partial";
	public const string UndefinableEcho = "undefinable/echo";
}
